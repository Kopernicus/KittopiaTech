using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KittopiaTech.Export;
using KittopiaTech.Model;
using KittopiaTech.UI;
using KittopiaTech.UI.Debug;
using KittopiaTech.UI.Editors;
using UnityEngine;

namespace KittopiaTech;

/// <summary>
/// HotReloadKSP support. On reload the new assembly tears down every object the old one
/// created and rebuilds it with its own types, since caches and prefabs are statics and
/// components from the old assembly can't be reused. Only window placement carries over.
/// </summary>
internal static class HotReloadSupport
{
    /// <summary>Everything KittopiaTech creates is named with this prefix.</summary>
    public const string ObjectPrefix = "KittopiaTech_";

    struct WindowState
    {
        public CelestialBody Body;
        public Vector2 Position;
        public Vector2 Size;
        public bool Visible;
    }

    // Called by HotReloadKSP on the new assembly, before old MonoBehaviours are swapped.
    static void OnHotLoad(Assembly oldAssembly)
    {
        var windows = CaptureWindows(oldAssembly);

        // Recorded once while loading; the new assembly can't record it again.
        var builtin = oldAssembly?.GetType(typeof(BuiltinTextures).FullName);
        if (builtin?.GetProperty(nameof(BuiltinTextures.Names))?.GetValue(null, null) is string[] names)
            BuiltinTextures.Names = names;
        if (builtin?.GetProperty(nameof(BuiltinTextures.MapNames))?.GetValue(null, null) is string[] maps)
            BuiltinTextures.MapNames = maps;
        DestroyOldObjects();

        if (!DebugSkin.Initialize())
            return;
        EditorRegistryScreen.Register();

        foreach (var state in windows)
        {
            var window = InspectorWindow.Open(state.Body);
            var rect = (RectTransform)window.transform;
            rect.anchoredPosition = state.Position;
            rect.sizeDelta = state.Size;
            window.gameObject.SetActive(state.Visible);
        }
    }

    /// <summary>
    /// Called by HotReloadKSP on every other loaded assembly after it reloads one, e.g. a mod
    /// with its own PQSMods. Its components are swapped for the new types, but plain objects
    /// such as Kopernicus loaders keep the old types and point at the destroyed components, so
    /// bodies holding any are wrapped afresh. Their pending edits and history go with them.
    /// </summary>
    static void OnHotReload(Assembly oldAssembly, Assembly newAssembly)
    {
        TypeSchema.Forget(oldAssembly);
        FieldEditorRegistry.Forget(oldAssembly);
        ApplyHooks.Reset();
        ConfigExporter.Reset();

        var stale = BodyContext.All.Where(c => Holds(c.Body, oldAssembly)).ToList();
        foreach (var context in stale)
        {
            Debug.LogWarning(
                $"[KittopiaTech] {context.CelestialBody.name} uses {oldAssembly.GetName().Name}, which was reloaded; "
                    + "rebuilding its inspector without its pending edits or history.");
            EditSession.Forget(context);
            BodyContext.Forget(context.CelestialBody);
        }

        // Not straight away: the old components are only destroyed, and the new ones enabled,
        // after this returns.
        InspectorWindow.RebuildShowing(stale.Select(c => c.CelestialBody).ToList());
    }

    /// <summary>Whether a node or list item beneath <paramref name="root"/> has a type from <paramref name="assembly"/>.</summary>
    static bool Holds(object root, Assembly assembly)
    {
        var seen = new HashSet<object>(ReferenceComparer.Instance);
        var pending = new Stack<object>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            if (node == null || !seen.Add(node))
                continue;
            if (Uses(node.GetType(), assembly))
                return true;
            foreach (var child in EditSession.Children(node))
                pending.Push(child);
        }
        return false;
    }

    /// <summary>Whether <paramref name="type"/>, or a type argument of it, is from <paramref name="assembly"/>.</summary>
    internal static bool Uses(Type type, Assembly assembly) =>
        type.Assembly == assembly || (type.IsGenericType && type.GetGenericArguments().Any(t => Uses(t, assembly)));

    /// <summary>Reads the old windows by reflection, so this works across any two versions.</summary>
    static List<WindowState> CaptureWindows(Assembly oldAssembly)
    {
        var states = new List<WindowState>();
        var oldType = oldAssembly?.GetType(typeof(InspectorWindow).FullName);
        var bodyProperty = oldType?.GetProperty(nameof(InspectorWindow.Body));
        if (bodyProperty == null)
            return states;

        foreach (var window in Resources.FindObjectsOfTypeAll(oldType).OfType<MonoBehaviour>())
        {
            if (!window.gameObject.scene.IsValid())
                continue;

            var rect = (RectTransform)window.transform;
            states.Add(
                new WindowState
                {
                    Body = bodyProperty.GetValue(window, null) as CelestialBody,
                    Position = rect.anchoredPosition,
                    Size = rect.sizeDelta,
                    Visible = window.gameObject.activeSelf,
                }
            );
        }
        return states;
    }

    static void DestroyOldObjects()
    {
        // Scene objects only (DontDestroyOnLoad counts as a scene); never assets.
        var owned = Resources
            .FindObjectsOfTypeAll<GameObject>()
            .Where(go => go.scene.IsValid() && go.name.StartsWith(ObjectPrefix))
            .ToList();
        foreach (var go in owned)
        {
            if (go != null)
                UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
