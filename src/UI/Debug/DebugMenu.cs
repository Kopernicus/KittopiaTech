using System.Collections.Generic;
using System.Reflection;
using KSP.UI;
using KSP.UI.Screens.DebugToolbar;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Debug;

/// <summary>
/// KittopiaTech's entries in the Alt+F12 menu: "Kittopia", which opens the editor window, with
/// "Kittopia Editors" under it.
/// </summary>
internal static class DebugMenu
{
    const string KittopiaName = HotReloadSupport.ObjectPrefix + "Kittopia";
    const string EditorsName = HotReloadSupport.ObjectPrefix + "Editors";

    static readonly FieldInfo OpenedScreens =
        typeof(DebugScreen).GetField("screens", BindingFlags.NonPublic | BindingFlags.Instance);

    static readonly FieldInfo TreeItems =
        typeof(DebugScreen).GetField("treeItems", BindingFlags.NonPublic | BindingFlags.Static);

    public static void Register()
    {
        // The menu adds these entries the first time it's shown, parents before children.
        var screens = DebugScreenSpawner.Instance?.debugScreens?.screens;
        if (screens == null)
            return;

        // A hot reload leaves the old entries behind, pointing at destroyed prefabs.
        screens.RemoveAll(s => s.name == KittopiaName || s.name == EditorsName);

        // The Alt+F12 window also caches the screens it opened and compares their names when a
        // tab is picked, so a destroyed instance of ours would break every later tab.
        foreach (var debugScreen in Resources.FindObjectsOfTypeAll<DebugScreen>())
            (OpenedScreens.GetValue(debugScreen) as List<RectTransform>)?.RemoveAll(s => s == null);

        var entries = new[]
        {
            new AddDebugScreens.ScreenWrapper
            {
                parentName = null,
                name = KittopiaName,
                text = "Kittopia",
                screen = BuildKittopiaScreen(),
            },
            new AddDebugScreens.ScreenWrapper
            {
                parentName = KittopiaName,
                name = EditorsName,
                text = "Kittopia Editors",
                screen = EditorRegistryScreen.Build(CreateScreen(EditorsName)),
            },
        };
        screens.AddRange(entries);

        // After a hot reload the menu may already have added its entries, so swap ours there too.
        if (TreeItems.GetValue(null) is List<UITreeView.Item> { Count: > 0 } items)
        {
            items.RemoveAll(i => i.name == KittopiaName || i.name == EditorsName);
            foreach (var entry in entries)
                DebugScreen.AddContentScreen(entry.parentName, entry.name, entry.text, entry.screen);
        }
    }

    /// <summary>An inactive, empty page for the menu to copy when its entry is picked.</summary>
    static RectTransform CreateScreen(string name)
    {
        var prefab = new GameObject(name, typeof(RectTransform));
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
        return rect;
    }

    static RectTransform BuildKittopiaScreen()
    {
        var rect = CreateScreen(KittopiaName);
        rect.gameObject.AddComponent<ShowOnEnable>();
        DebugSkin.CreateHeader(rect, "Kittopia");
        DebugSkin.CreateLabel(rect, "Click Kittopia in this list to open the editor window. Ctrl+P also shows or hides it.");
        return rect;
    }

    /// <summary>
    /// The menu re-enables an entry's page every time the entry is clicked, even when it's
    /// already showing, so this opens the window on each click. Reopening the menu re-enables
    /// the current page too, but before the menu counts as shown.
    /// </summary>
    class ShowOnEnable : MonoBehaviour
    {
        void OnEnable()
        {
            if (GetComponentInParent<DebugScreen>()?.isShown == true)
                InspectorWindow.Show();
        }
    }
}
