using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KittopiaTech.UI;
using KittopiaTech.UI.Debug;
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
        var builtin = oldAssembly?.GetType(typeof(BuiltinTextures).FullName)?.GetProperty(nameof(BuiltinTextures.Names));
        if (builtin?.GetValue(null, null) is string[] names)
            BuiltinTextures.Names = names;
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
                Object.DestroyImmediate(go);
        }
    }
}
