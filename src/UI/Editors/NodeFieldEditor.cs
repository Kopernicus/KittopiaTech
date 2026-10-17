using KittopiaTech.UI.Debug;
using KittopiaTech.UI.Inspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Editors;

/// <summary>
/// The fallback for node members: a foldout whose rows are built from the child
/// object's schema the first time it opens.
/// </summary>
[FieldEditor(typeof(Shapes.Node))]
public sealed class NodeFieldEditor : FieldEditor<object>
{
    [SerializeField]
    Button header;

    [SerializeField]
    TextMeshProUGUI headerText;

    [SerializeField]
    Transform content;

    bool expanded;
    object built;

    public override FieldLayout Layout => FieldLayout.Wide;

    protected internal override void BuildPrefab(PrefabContext ctx)
    {
        CreateFoldout(transform, out header, out headerText, out content);
    }

    /// <summary>A header button over an indented, initially hidden content area.</summary>
    public static void CreateFoldout(
        Transform parent,
        out Button header,
        out TextMeshProUGUI headerText,
        out Transform content
    )
    {
        var vlg = parent.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 2f;

        header = DebugSkin.CreateButton(parent, "");
        EditorLayout.Compact(header.gameObject);
        headerText = header.GetComponentInChildren<TextMeshProUGUI>();
        headerText.alignment = TextAlignmentOptions.MidlineLeft;
        headerText.richText = true;
        headerText.margin = new Vector4(6f, 0f, 6f, 0f);

        var body = DebugSkin.CreateVerticalLayout(parent, 2f);
        body.name = "Content";
        body.padding = new RectOffset(14, 0, 0, 4);
        body.gameObject.SetActive(false);
        content = body.transform;
    }

    protected override void Awake() => header.onClick.AddListener(ToggleExpanded);

    void ToggleExpanded()
    {
        expanded = !expanded;
        Refresh();
    }

    public override void Refresh()
    {
        var value = Source.Get();
        string type = value?.GetType().Name ?? "none";
        headerText.text =
            $"{(expanded && value != null ? "-" : "+")} <b>{Source.Label}</b>  <color=#a3a7ab>{type}</color>";

        if (!ReferenceEquals(value, built))
        {
            InspectorBuilder.Clear(content);
            built = null;
        }

        bool show = expanded && value != null;
        if (show && built == null)
        {
            InspectorBuilder.BuildMembers(content, Source.Scope, value);
            built = value;
        }
        content.gameObject.SetActive(show);
    }
}
