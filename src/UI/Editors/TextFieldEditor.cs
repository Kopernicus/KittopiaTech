using KittopiaTech.UI.Debug;
using KSP.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Editors;

/// <summary>Plain strings, and the fallback for any IParsable edited as its config text.</summary>
[FieldEditor(typeof(string))]
public sealed class TextFieldEditor : FieldEditor<string>
{
    [SerializeField]
    TMP_InputField input;

    [SerializeField]
    Tooltip tooltip;

    protected internal override void BuildPrefab(PrefabContext ctx)
    {
        EditorLayout.Row(transform);
        input = DebugSkin.CreateInputField(transform);
        input.lineType = TMP_InputField.LineType.SingleLine;
        EditorLayout.Compact(input.gameObject);
        tooltip = input.gameObject.AddComponent<Tooltip>();
    }

    protected override void Awake() => input.onEndEdit.AddListener(OnEndEdit);

    void OnEndEdit(string text)
    {
        if (text != (Value ?? ""))
            Commit(text);
    }

    public override void Refresh()
    {
        input.text = Value ?? ""; // only onValueChanged fires, and nothing listens to it
        input.interactable = !Source.IsReadOnly;
        EditorLayout.MarkInvalid(input, Source.Error != null);
        tooltip.text = Localized(Value);
    }

    /// <summary>What a localization key such as <c>#autoLOC_910049</c> shows in game, or null.</summary>
    public static string Localized(string text) =>
        text != null && text.StartsWith("#") && Localizer.TryGetStringByTag(text, out var value) ? value : null;
}

/// <summary>Shows a value nothing else can edit.</summary>
public sealed class ReadOnlyFieldEditor : FieldEditor<object>
{
    [SerializeField]
    TextMeshProUGUI label;

    [SerializeField]
    Tooltip tooltip;

    protected internal override void BuildPrefab(PrefabContext ctx)
    {
        EditorLayout.Row(transform);
        label = DebugSkin.CreateLabel(transform, "");
        label.fontStyle = FontStyles.Italic;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = true;
        tooltip = label.gameObject.AddComponent<Tooltip>();
    }

    public override void Refresh()
    {
        var value = Source.Get();
        label.text = value switch
        {
            null => "null",
            Kopernicus.ConfigParser.Interfaces.IWritable w => w.ValueToString(),
            _ => value.ToString(),
        };
        tooltip.text = TextFieldEditor.Localized(label.text);
    }
}

/// <summary>Layout helpers shared by editor prefabs.</summary>
public static class EditorLayout
{
    public const float RowHeight = 22f;

    static readonly Color InvalidColor = new(0.85f, 0.33f, 0.31f);

    /// <summary>Makes <paramref name="t"/> a horizontal row whose children share the width.</summary>
    public static HorizontalLayoutGroup Row(Transform t, float spacing = 4f)
    {
        var hlg = t.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = spacing;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        var layout = t.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = RowHeight;
        layout.flexibleWidth = 1f;
        return hlg;
    }

    /// <summary>Shrinks a cloned stock widget to the row height.</summary>
    public static void Compact(GameObject go, float flexibleWidth = 1f)
    {
        var layout = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        layout.minHeight = RowHeight;
        layout.preferredHeight = RowHeight;
        layout.minWidth = 0f;
        layout.flexibleWidth = flexibleWidth;
    }

    /// <summary>A button showing a path, left aligned; clicking it opens a picker.</summary>
    public static Button PathButton(Transform parent, out TextMeshProUGUI text)
    {
        var button = DebugSkin.CreateButton(parent, "");
        button.name = "Path";
        Compact(button.gameObject);
        text = button.GetComponentInChildren<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.margin = new Vector4(4f, 0f, 4f, 0f);
        return button;
    }

    public static void MarkInvalid(Selectable selectable, bool invalid)
    {
        var colors = selectable.colors;
        colors.normalColor = invalid ? InvalidColor : Color.white;
        selectable.colors = colors;
    }
}
