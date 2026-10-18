using System.Linq;
using KittopiaTech.UI.Editors;
using UnityEngine;

namespace KittopiaTech.UI.Debug;

/// <summary>
/// An Alt+F12 page listing every registered field editor, so it's easy to see why an
/// override did or didn't win.
/// </summary>
internal static class EditorRegistryScreen
{
    /// <summary>Fills in the page <see cref="DebugMenu"/> registers for this list.</summary>
    public static RectTransform Build(RectTransform screen)
    {
        DebugSkin.CreateHeader(screen, "Field editors");
        DebugSkin.CreateLabel(screen, "Higher priority wins; ties go to the most derived type.");
        DebugSkin.CreateScrollView(screen, out var content);

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

        return screen;
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
