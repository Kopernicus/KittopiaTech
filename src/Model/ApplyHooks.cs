using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kopernicus.Configuration;
using Kopernicus.Components;
using Kopernicus.Configuration.MaterialLoader;
using Kopernicus.Configuration.ModLoader;
using Kopernicus.OnDemand;
using UnityEngine;

namespace KittopiaTech.Model;

/// <summary>
/// Marks a static <c>void Method(BodyContext body, T owner)</c> to run after Apply whenever an edit
/// landed on <paramref name="ownerType"/> or anything beneath it. Other mods can declare their own.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ApplyHookAttribute(Type ownerType) : Attribute
{
    public Type OwnerType { get; } = ownerType;

    /// <summary>Lower runs first.</summary>
    public int Order { get; set; }

    /// <summary>A KittopiaAction on the owner that this hook makes redundant, hidden from the UI.</summary>
    public string Replaces { get; set; }
}

/// <summary>Follow-up rebuilds after Apply, found by scanning loaded assemblies for [ApplyHook].</summary>
public static class ApplyHooks
{
    sealed class Hook(ApplyHookAttribute attribute, MethodInfo method)
    {
        public Type OwnerType { get; } = attribute.OwnerType;
        public int Order { get; } = attribute.Order;
        public string Replaces { get; } = attribute.Replaces;
        public MethodInfo Method { get; } = method;
    }

    static List<Hook> hooks;

    /// <summary>Rescans on next use, after another mod (which may declare hooks) was hot-reloaded.</summary>
    internal static void Reset() => hooks = null;

    static List<Hook> Hooks =>
        hooks ??= AssemblyLoader
            .loadedAssemblies.SelectMany(a => SafeTypes(a.assembly))
            .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .SelectMany(m => m.GetCustomAttributes<ApplyHookAttribute>().Select(a => new Hook(a, m)))
            .OrderBy(h => h.Order)
            .ToList();

    static IEnumerable<Type> SafeTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t != null);
        }
    }

    /// <summary>Whether a hook already does what <paramref name="action"/> does after every Apply.</summary>
    public static bool Replaces(MethodInfo action) =>
        Hooks.Any(h => h.Replaces == action.Name && action.DeclaringType.IsAssignableFrom(h.OwnerType));

    /// <summary>Runs every hook whose owner contains one of <paramref name="touched"/>.</summary>
    public static void Run(BodyContext context, ICollection<object> touched)
    {
        if (touched.Count == 0)
            return;

        // Walk the body once, marking every hook-owner above a touched object.
        var dirty = new List<object>();
        var ancestors = new List<object>();
        var seen = new HashSet<object>(ReferenceComparer.Instance);
        Walk(context.Body);

        foreach (var hook in Hooks)
        {
            foreach (var owner in dirty.Where(hook.OwnerType.IsInstanceOfType))
            {
                try
                {
                    Debug.Log($"[KittopiaTech] Running {hook.Method.Name} on {context.CelestialBody.name}");
                    using (ParseStateScope.For(context))
                        hook.Method.Invoke(null, [context, owner]);
                }
                catch (Exception e)
                {
                    var inner = e is TargetInvocationException { InnerException: { } i } ? i : e;
                    Debug.LogError($"[KittopiaTech] Apply hook {hook.Method.Name} failed: {inner}");
                }
            }
        }

        void Walk(object node)
        {
            if (node == null || !seen.Add(node))
                return;

            if (touched.Contains(node))
            {
                foreach (var ancestor in ancestors.Append(node))
                {
                    if (!dirty.Contains(ancestor) && Hooks.Any(h => h.OwnerType.IsInstanceOfType(ancestor)))
                        dirty.Add(ancestor);
                }
            }

            ancestors.Add(node);
            foreach (var child in EditSession.Children(node))
                Walk(child);
            ancestors.RemoveAt(ancestors.Count - 1);
        }
    }

    // Core hooks. Orbits first, so a rebuilt sphere sees the final orbit.

    /// <summary>
    /// The loader only sets the orbital elements; Init rebuilds the orbit's frame, which the
    /// body's position and its orbit line come from. It goes first, since FinalizeOrbit then
    /// recomputes the period with the body's own mass included.
    /// </summary>
    [ApplyHook(typeof(OrbitLoader), Order = -10, Replaces = nameof(OrbitLoader.FinalizeOrbit))]
    static void FinalizeOrbit(BodyContext body, OrbitLoader orbit)
    {
        Bodies.SyncParent(orbit.Value);
        orbit.Value.orbit.Init();
        orbit.FinalizeOrbit();
    }

    /// <summary>
    /// The PQS renders with its own copy of the quality material that the loader edits, made
    /// once at setup. Recopy it so the rebuilt quads pick up material edits.
    /// </summary>
    [ApplyHook(typeof(PQSLoader), Order = -1)]
    static void RecopySurfaceMaterial(BodyContext body, PQSLoader pqs)
    {
        var sphere = pqs.Value;
        var old = sphere.surfaceMaterial;
        sphere.ApplyTerrainShaderSettings();

        // Only the per-sphere copy is ours to destroy, not a shared quality material.
        Material[] shared =
        [
            sphere.surfaceMaterial, sphere.ultraQualitySurfaceMaterial, sphere.highQualitySurfaceMaterial,
            sphere.mediumQualitySurfaceMaterial, sphere.lowQualitySurfaceMaterial, sphere.fallbackMaterial,
        ];
        if (old != null && !shared.Contains(old))
            UnityEngine.Object.Destroy(old);
    }

    /// <summary>
    /// On a body that's already built, the ocean's material loader works on a copy of the
    /// ocean's material, which Kopernicus only hands to the ocean while building the body.
    /// Copy the edits onto the materials the ocean renders with, which its quads share.
    /// </summary>
    [ApplyHook(typeof(OceanLoader), Order = -1)]
    static void CopyOceanMaterial(BodyContext body, OceanLoader ocean)
    {
        var edited = ocean.SurfaceMaterial?.Value;
        var sphere = ocean.Value;
        if (edited == null || sphere == null)
            return;

        Material[] live =
        [
            sphere.surfaceMaterial, sphere.ultraQualitySurfaceMaterial, sphere.highQualitySurfaceMaterial,
            sphere.mediumQualitySurfaceMaterial, sphere.lowQualitySurfaceMaterial,
        ];
        foreach (var material in live.Where(m => m != null && m != edited).Distinct())
        {
            material.shader = edited.shader;
            material.CopyPropertiesFromMaterial(edited);
            material.shaderKeywords = edited.shaderKeywords;
        }
    }

    // Kopernicus copies a material's on-demand texture paths into the body's on-demand loaders
    // when it builds the body, and those would put the old textures back over texture edits.
    // RefreshOnDemand is only in Kopernicus builds with MaterialLoader.CreateExisting, the only
    // ones that give spawned bodies material loaders to edit.
    static readonly MethodInfo RefreshScaledOnDemand = typeof(MaterialLoader).GetMethod(
        "RefreshOnDemand", [typeof(GameObject)]);
    static readonly MethodInfo RefreshSurfaceOnDemand = typeof(MaterialLoader).GetMethod(
        "RefreshOnDemand", [typeof(PQS), typeof(PQSMod_OnDemandHandler)]);
    static readonly MethodInfo RefreshModOnDemand = typeof(MaterialLoader).GetMethod(
        "RefreshOnDemand", [typeof(IPQSModWithMaterial), typeof(PQSMod_OnDemandHandler)]);

    [ApplyHook(typeof(ScaledVersionLoader), Order = -1)]
    static void RefreshScaledTextures(BodyContext body, ScaledVersionLoader scaled)
    {
        if (scaled.Material != null)
            RefreshScaledOnDemand?.Invoke(scaled.Material, [scaled.Value.scaledBody]);
    }

    [ApplyHook(typeof(PQSLoader), Order = -2)]
    static void RefreshSurfaceTextures(BodyContext body, PQSLoader pqs)
    {
        var handler = pqs.Value.GetComponentInChildren<PQSMod_OnDemandHandler>(true);
        if (pqs.SurfaceMaterial != null && handler != null)
            RefreshSurfaceOnDemand?.Invoke(pqs.SurfaceMaterial, [pqs.Value, handler]);
    }

    [ApplyHook(typeof(LandControl.LandClassScatterLoader), Order = -2)]
    static void RefreshScatterTextures(BodyContext body, LandControl.LandClassScatterLoader scatter)
    {
        var handler = body.CelestialBody.pqsController?.GetComponentInChildren<PQSMod_OnDemandHandler>(true);
        if (scatter.Material != null && scatter.Scatter != null && handler != null)
            RefreshModOnDemand?.Invoke(scatter.Material, [scatter.Scatter, handler]);
    }

    [ApplyHook(typeof(PQSLoader), Replaces = nameof(PQSLoader.Rebuild))]
    static void RebuildSphere(BodyContext body, PQSLoader pqs) => pqs.Rebuild();

    [ApplyHook(typeof(ScaledVersionLoader), Replaces = nameof(ScaledVersionLoader.RebuildScaledSpace))]
    static void RebuildScaledSpace(BodyContext body, ScaledVersionLoader scaled) => scaled.RebuildScaledSpace();

    [ApplyHook(typeof(RingLoader), Replaces = nameof(RingLoader.RebuildRing))]
    static void RebuildRing(BodyContext body, RingLoader ring) => ring.RebuildRing();
}
