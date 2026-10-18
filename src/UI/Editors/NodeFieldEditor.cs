using System;
using System.Linq;
using KittopiaTech.Model;
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

    [SerializeField]
    TMP_Dropdown typePicker;

    [SerializeField]
    Button createButton;

    [SerializeField]
    Button removeButton;

    bool expanded;
    object built;
    Type[] concreteTypes;

    public override FieldLayout Layout => FieldLayout.Wide;

    protected internal override void BuildPrefab(PrefabContext ctx)
    {
        CreateFoldout(transform, out header, out headerText, out content);

        var row = header.transform.parent;
        typePicker = DebugSkin.CreateDropdown(row);
        EditorLayout.Compact(typePicker.gameObject, 0f);
        typePicker.GetComponent<LayoutElement>().preferredWidth = 180f;
        createButton = DebugSkin.CreateButton(row, "Create");
        createButton.name = "Create";
        EditorLayout.Compact(createButton.gameObject, 0f);
        createButton.GetComponent<LayoutElement>().preferredWidth = 70f;
        removeButton = DebugSkin.CreateButton(row, "x");
        removeButton.name = "Remove";
        EditorLayout.Compact(removeButton.gameObject, 0f);
        var removeLayout = removeButton.GetComponent<LayoutElement>();
        removeLayout.minWidth = removeLayout.preferredWidth = EditorLayout.RowHeight;
    }

    protected override void OnBind()
    {
        concreteTypes = Structure.NeedsTypePicker(Source.ValueType)
            ? TypeSchema.GetConcreteTypes(Source.ValueType)
            : null;
        if (concreteTypes != null)
        {
            typePicker.ClearOptions();
            typePicker.AddOptions(concreteTypes.Select(t => t.Name).ToList());
        }
    }

    void Create()
    {
        var type = concreteTypes != null ? concreteTypes[typePicker.value] : Source.ValueType;
        try
        {
            var created = Structure.Create(Source.Scope.Context, type);
            WriteNow(created);
            expanded = true;
            var (slot, context) = (Source.Slot, Source.Scope.Context);
            Source.Scope.MarkCreated(created, Source.Label, value =>
            {
                Structure.Destroy(context, value);
                slot.Write(null);
            });
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"[KittopiaTech] Failed to create {Source.Label}: {e}");
        }
        Refresh();
    }

    /// <summary>A removal is staged as none, so it can be undone until Apply destroys the object.</summary>
    bool RemovalPending => Source.IsModified && Source.Get() == null;

    void Remove()
    {
        if (RemovalPending)
        {
            Source.Reset();
            return;
        }
        try
        {
            var value = Source.Get();
            Source.Scope.DiscardWithin(value);
            // Something not applied yet has nothing to keep, so it goes straight away.
            if (Source.Scope.IsCreated(value))
            {
                Structure.Destroy(Source.Scope.Context, value);
                WriteNow(null);
            }
            else
                Source.Set(null);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"[KittopiaTech] Failed to remove {Source.Label}: {e}");
        }
        Refresh();
    }

    static readonly string PendingHex = ColorUtility.ToHtmlStringRGB(InspectorWindow.PendingColor);

    /// <summary>Header text in the pending colour when something beneath it is modified.</summary>
    public static string PendingLabel(string label, bool pending) =>
        pending ? $"<color=#{PendingHex}>{label}</color>" : label;

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

        // A row, so editors can put buttons beside the header.
        var row = DebugSkin.CreateHorizontalLayout(parent, 4f, controlWidth: true);
        row.name = "Header";
        header = DebugSkin.CreateButton(row.transform, "");
        header.name = "Foldout";
        DebugSkin.StyleAsSidebarItem(header);
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

    /// <summary>Structural edits skip staging and write straight to the member.</summary>
    void WriteNow(object value)
    {
        using (ParseStateScope.For(Source.Scope.Context))
            Source.Slot.Write(value);
        Source.Scope.Invalidate();
    }

    protected override void Awake()
    {
        header.onClick.AddListener(ToggleExpanded);
        createButton.onClick.AddListener(Create);
        removeButton.onClick.AddListener(Remove);
    }

    void ToggleExpanded()
    {
        expanded = !expanded;
        Refresh();
    }

    public override void Refresh()
    {
        var value = Source.Get();
        bool removing = RemovalPending;
        string name = value != null ? Structure.SummaryName(value) : null;
        string type = removing ? "removed on Apply" : value?.GetType().Name ?? "none";
        string summary = string.IsNullOrEmpty(name) ? "" : $" {name}";
        string label = PendingLabel(Source.Label + summary, Source.IsModified || Source.Scope.HasPendingWithin(value));
        headerText.text =
            $"{(expanded && value != null ? "-" : "+")} <b>{label}</b>  <color=#a3a7ab>{type}</color>";

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

        // Untouchable or read-only members can't be created or removed, and list items are
        // added and removed by their list.
        bool editable = !Source.IsReadOnly && Source.Member != null;
        bool canCreate = editable && value == null && !removing && Structure.CanCreate(Source.ValueType);
        typePicker.gameObject.SetActive(canCreate && concreteTypes != null);
        createButton.gameObject.SetActive(canCreate);
        removeButton.gameObject.SetActive(editable && (value != null || removing));
        removeButton.GetComponentInChildren<TextMeshProUGUI>().text = removing ? "Undo" : "x";
        removeButton.GetComponent<LayoutElement>().preferredWidth = removing ? 48f : EditorLayout.RowHeight;

        // An empty member that can't be created (e.g. a non-star's Light) has nothing to show.
        if (Source.Member != null)
            transform.parent.gameObject.SetActive(value != null || canCreate || removing);
    }
}
