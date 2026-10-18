using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KittopiaTech.UI.Editors;
using KSP.UI.Screens.DebugToolbar;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Debug;

/// <summary>
/// An Alt+F12 page listing every registered field editor, so it's easy to see why an
/// override did or didn't win.
/// </summary>
internal static class EditorRegistryScreen
{
    const string ScreenName = HotReloadSupport.ObjectPrefix + "Editors";

    static readonly FieldInfo OpenedScreens =
        typeof(DebugScreen).GetField("screens", BindingFlags.NonPublic | BindingFlags.Instance);

    public static void Register()
    {
        var screens = DebugScreenSpawner.Instance?.debugScreens?.screens;
        if (screens == null)
            return;

        // A hot reload leaves the old entry behind, pointing at a destroyed prefab.
        screens.RemoveAll(s => s.name == ScreenName);

        // The Alt+F12 window also caches the screens it opened and compares their names when a
        // tab is picked, so a destroyed instance of ours would break every later tab.
        foreach (var debugScreen in Resources.FindObjectsOfTypeAll<DebugScreen>())
            (OpenedScreens.GetValue(debugScreen) as List<RectTransform>)?.RemoveAll(s => s == null);

        var prefab = new GameObject(ScreenName, typeof(RectTransform));
        prefab.SetActive(false);
        Object.DontDestroyOnLoad(prefab);
        var rect = (RectTransform)prefab.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var vlg = prefab.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(8, 8, 8, 8);

        DebugSkin.CreateHeader(prefab.transform, "Field editors");
        DebugSkin.CreateLabel(prefab.transform, "Higher priority wins; ties go to the most derived type.");
        DebugSkin.CreateScrollView(prefab.transform, out var content);

        var registrations = FieldEditorRegistry
            .Registrations.OrderBy(r => r.Target.FullName)
            .ThenByDescending(r => r.Priority);
        var own = typeof(EditorRegistryScreen).Assembly;
        foreach (var group in registrations.GroupBy(r => r.Target))
        {
            Line(content, $"<b>{group.Key.Name}</b>  <color=#a3a7ab>{group.Key.Namespace}</color>");
            foreach (var reg in group)
            {
                string source = reg.Editor.Assembly == own ? "" : $", {reg.Editor.Assembly.GetName().Name}";
                Line(content, $"    {reg.Editor.Name}  <color=#a3a7ab>priority {reg.Priority}{source}</color>");
            }
        }

        screens.Add(
            new AddDebugScreens.ScreenWrapper
            {
                parentName = null,
                name = ScreenName,
                text = "Kittopia Editors",
                screen = rect,
            }
        );
    }

    static void Line(Transform parent, string text)
    {
        var label = DebugSkin.CreateLabel(parent, text);
        label.richText = true;
        label.enableWordWrapping = false;
        label.overflowMode = TMPro.TextOverflowModes.Ellipsis;
        label.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
    }
}
