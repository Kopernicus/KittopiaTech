using KittopiaTech.Model;
using KittopiaTech.UI.Debug;
using KittopiaTech.UI.Editors;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Inspector;

/// <summary>One labelled member in the inspector: a label and the editor bound to it.</summary>
public sealed class FieldRow : MonoBehaviour
{
    public const float LabelWidth = 160f;

    static readonly Color ErrorColor = new(0.95f, 0.45f, 0.42f);

    public FieldEditor Editor { get; private set; }
    public IValueSource Source { get; private set; }

    TextMeshProUGUI label;
    Color labelColor;
    Button resetButton;

    public static FieldRow Create(Transform parent, IValueSource source, FieldEditorRegistry.Resolution resolution)
    {
        var go = new GameObject($"Row({source.Label})", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var row = go.AddComponent<FieldRow>();
        row.Source = source;

        // Instantiate first: the editor's Layout decides how the row is arranged.
        var editor = FieldEditorRegistry.Instantiate(resolution.Editor, go.transform);
        row.Editor = editor;

        if (editor.Layout == FieldLayout.Wide)
        {
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 2f;
            // Wide editors (nodes, lists) draw their own header with the label.
        }
        else
        {
            var hlg = EditorLayout.Row(go.transform, 6f);
            hlg.padding = new RectOffset(4, 0, 0, 0);
            row.label = CreateLabel(go.transform, source);
            row.label.transform.parent.SetAsFirstSibling();
            row.labelColor = row.label.color;

            row.resetButton = DebugSkin.CreateButton(go.transform, "Reset", source.Reset);
            EditorLayout.Compact(row.resetButton.gameObject, 0f);
            row.resetButton.GetComponent<LayoutElement>().preferredWidth = 48f;

            source.Changed += row.UpdateState;
            row.UpdateState();
        }

        editor.Bind(source);
        return row;
    }

    /// <summary>Amber for a pending edit, red when applying it failed.</summary>
    void UpdateState()
    {
        if (this == null || Source == null)
            return;

        bool modified = Source.IsModified;
        label.color = Source.Error != null ? ErrorColor : modified ? InspectorWindow.PendingColor : labelColor;
        resetButton.gameObject.SetActive(modified);
    }

    void OnDestroy()
    {
        if (label != null && Source != null)
            Source.Changed -= UpdateState;
    }

    static TextMeshProUGUI CreateLabel(Transform parent, IValueSource source)
    {
        var label = DebugSkin.CreateLabel(parent, source.Label);
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;

        var layout = label.transform.parent.GetComponent<LayoutElement>();
        layout.minWidth = layout.preferredWidth = LabelWidth;
        layout.flexibleWidth = 0f;
        layout.minHeight = EditorLayout.RowHeight;
        return label;
    }
}

/// <summary>Builds the rows for an object's members from its schema.</summary>
public static class InspectorBuilder
{
    public static void BuildMembers(Transform parent, IEditScope scope, object owner)
    {
        var schema = TypeSchema.Get(owner.GetType());
        foreach (var member in schema.Members)
        {
            if (member.Hidden)
                continue;

            var resolution = FieldEditorRegistry.Resolve(member, member.DeclaredType, member.Shape);
            var source = scope.For(new MemberSlot(owner, member), resolution.Adapter);
            try
            {
                FieldRow.Create(parent, source, resolution);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[KittopiaTech] Failed to build a row for {member}: {e}");
            }
        }

        ActionBar.Build(parent, scope, owner);
    }

    public static void Clear(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Object.Destroy(parent.GetChild(i).gameObject);
    }
}
