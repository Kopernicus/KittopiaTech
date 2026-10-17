using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using KSP.UI.Screens.DebugToolbar;
using TMPro;
using UnityEngine;

namespace KittopiaTech.Export;

/// <summary>
/// Opens BurstPQS's Texture Exporter tab in the Alt+F12 menu with a body selected. BurstPQS
/// is optional and has no public API for this, so everything goes through reflection.
/// </summary>
public static class BurstPQSExporter
{
    const string TabName = "Texture Exporter";
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    static readonly Type ScreenType = AssemblyLoader
        .loadedAssemblies.FirstOrDefault(a => a.name == "BurstPQS")
        ?.assembly.GetType("BurstPQS.UI.DebugUI.TextureExporterScreen");

    public static bool Available => ScreenType != null;

    /// <summary>Why the exporter can't be opened for <paramref name="body"/>, or null if it can.</summary>
    public static string Unavailable(CelestialBody body) =>
        !Available ? "Requires BurstPQS"
        : body == null || body.pqsController == null ? "This body has no terrain to export"
        : null;

    public static void Open(CelestialBody body)
    {
        DebugScreenSpawner.ShowTab(TabName);

        // The tab instantiates its screen when selected; the prefab stays inactive.
        var screen = Resources
            .FindObjectsOfTypeAll(ScreenType)
            .Cast<MonoBehaviour>()
            .FirstOrDefault(s => s.gameObject.activeInHierarchy);
        if (screen == null)
        {
            UnityEngine.Debug.LogWarning("[KittopiaTech] BurstPQS's Texture Exporter didn't open");
            return;
        }

        // The debug screen shares DebugCanvas with our windows and would open behind them.
        screen.GetComponentInParent<DebugScreen>()?.transform.SetAsLastSibling();

        // The screen keeps its bodies in a list and the selection as the dropdown's index.
        var bodies = ScreenType.GetField("_bodies", Private)?.GetValue(screen) as IList;
        var dropdown = ScreenType.GetField("_planetDropdown", Private)?.GetValue(screen) as TMP_Dropdown;
        int index = bodies?.IndexOf(body) ?? -1;
        if (dropdown == null || index < 0)
        {
            UnityEngine.Debug.LogWarning($"[KittopiaTech] Couldn't select {body.name} in BurstPQS's Texture Exporter");
            return;
        }
        dropdown.value = index;
        dropdown.RefreshShownValue();
    }
}
