using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kopernicus.Configuration;
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
}

/// <summary>Follow-up rebuilds after Apply, found by scanning loaded assemblies for [ApplyHook].</summary>
public static class ApplyHooks
{
    sealed class Hook(Type ownerType, int order, MethodInfo method)
    {
        public Type OwnerType { get; } = ownerType;
        public int Order { get; } = order;
        public MethodInfo Method { get; } = method;
    }

    static List<Hook> hooks;

    static List<Hook> Hooks =>
        hooks ??= AssemblyLoader
            .loadedAssemblies.SelectMany(a => SafeTypes(a.assembly))
            .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .SelectMany(m => m.GetCustomAttributes<ApplyHookAttribute>().Select(a => new Hook(a.OwnerType, a.Order, m)))
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

    [ApplyHook(typeof(OrbitLoader), Order = -10)]
    static void FinalizeOrbit(BodyContext body, OrbitLoader orbit) => orbit.FinalizeOrbit();

    [ApplyHook(typeof(PQSLoader))]
    static void RebuildSphere(BodyContext body, PQSLoader pqs) => pqs.Rebuild();

    [ApplyHook(typeof(ScaledVersionLoader))]
    static void RebuildScaledSpace(BodyContext body, ScaledVersionLoader scaled) => scaled.RebuildScaledSpace();

    [ApplyHook(typeof(RingLoader))]
    static void RebuildRing(BodyContext body, RingLoader ring) => ring.RebuildRing();
}
