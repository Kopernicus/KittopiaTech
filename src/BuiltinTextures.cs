using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace KittopiaTech;

/// <summary>
/// Records which textures count as BUILTIN/: those in memory when the PSystemSpawn scene loads,
/// before Kopernicus parses any configs, minus the ones GameDatabase loaded from GameData.
/// Later the set would also include planet pack textures and UI art. Stock MapSOs, which
/// configs reference as BUILTIN/ maps, are recorded at the same time.
/// </summary>
[KSPAddon(KSPAddon.Startup.Instantly, once: true)]
public class BuiltinTextures : MonoBehaviour
{
    const string PSystemScene = "PSystemSpawn";

    public static string[] Names { get; internal set; } = [];

    public static string[] MapNames { get; internal set; } = [];

    void Awake()
    {
        // AddonLoader starts PSystemSpawn addons (including Kopernicus's Injector) from its own
        // sceneLoaded handler, registered long before us. Put ours first so it runs before them.
        UnityAction<Scene, LoadSceneMode> handler = OnSceneLoaded;
        var field = typeof(SceneManager).GetField("sceneLoaded", BindingFlags.NonPublic | BindingFlags.Static);
        if (field?.GetValue(null) is Delegate existing)
            field.SetValue(null, Delegate.Combine(handler, existing));
        else
            SceneManager.sceneLoaded += handler;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != PSystemScene)
            return;
        SceneManager.sceneLoaded -= OnSceneLoaded;

        var fromGameData = new HashSet<string>(
            GameDatabase.Instance.databaseTexture.Select(t => t.name),
            StringComparer.OrdinalIgnoreCase
        );
        Names = Resources
            .FindObjectsOfTypeAll<Texture2D>()
            .Select(t => t.name)
            .Where(n => !string.IsNullOrEmpty(n) && !fromGameData.Contains(n))
            .Distinct()
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        MapNames = Resources
            .FindObjectsOfTypeAll<MapSO>()
            .Select(m => m.name)
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct()
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        UnityEngine.Debug.Log($"[KittopiaTech] Recorded {Names.Length} built-in textures, {MapNames.Length} maps");
    }
}
