using System;
using System.Collections;
using System.Linq;
using KittopiaTech.Model;
using KittopiaTech.UI.Debug;
using KittopiaTech.UI.Inspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Kopernicus.ConfigParser.Enumerations;

namespace KittopiaTech.UI.Editors;

/// <summary>
/// The fallback for collections: a foldout of item rows with move and remove buttons, and an
/// Add bar. Adding and removing items are structural edits and happen immediately.
/// </summary>
[FieldEditor(typeof(Shapes.Collection))]
public sealed class ListFieldEditor : FieldEditor<object>
{
    [SerializeField]
    Button header;

    [SerializeField]
    TextMeshProUGUI headerText;

    [SerializeField]
    Transform content;

    [SerializeField]
    Transform items;

    [SerializeField]
    TMP_Dropdown typePicker;

    [SerializeField]
    Button addButton;

    bool expanded;
    IList list;
    int builtCount = -1;
    Type elementType;
    Type[] concreteTypes;

    public override FieldLayout Layout => FieldLayout.Wide;

    protected internal override void BuildPrefab(PrefabContext ctx)
    {
        NodeFieldEditor.CreateFoldout(transform, out header, out headerText, out content);
        items = DebugSkin.CreateVerticalLayout(content, 2f).transform;

        var bar = DebugSkin.CreateHorizontalLayout(content, 4f, controlWidth: true);
        typePicker = DebugSkin.CreateDropdown(bar.transform);
        EditorLayout.Compact(typePicker.gameObject);
        addButton = DebugSkin.CreateButton(bar.transform, "Add");
        addButton.name = "Add";
        EditorLayout.Compact(addButton.gameObject, 0f);
        addButton.GetComponent<LayoutElement>().preferredWidth = 80f;
    }

    protected override void Awake()
    {
        header.onClick.AddListener(() =>
        {
            expanded = !expanded;
            Refresh();
        });
        addButton.onClick.AddListener(Add);
    }

    protected override void OnBind()
    {
        var listType = Source.ValueType;
        elementType = listType.IsGenericType ? listType.GetGenericArguments()[0] : typeof(object);
        concreteTypes = Structure.NeedsTypePicker(elementType)
            ? TypeSchema.GetConcreteTypes(elementType)
            : null;

        typePicker.gameObject.SetActive(concreteTypes != null);
        if (concreteTypes != null)
        {
            typePicker.ClearOptions();
            typePicker.AddOptions(concreteTypes.Select(t => t.Name).ToList());
        }
        addButton.interactable = Structure.CanCreate(elementType);
        builtCount = -1;
    }

    public override void Refresh()
    {
        var current = Source.Get() as IList;
        if (!ReferenceEquals(current, list))
            builtCount = -1;
        list = current;

        int count = list?.Count ?? 0;
        headerText.text =
            $"{(expanded ? "-" : "+")} <b>{Source.Label}</b>  <color=#a3a7ab>{elementType?.Name} x{count}</color>";

        if (expanded && builtCount != count)
            Rebuild();
        content.gameObject.SetActive(expanded);
    }

    void Rebuild()
    {
        InspectorBuilder.Clear(items);
        builtCount = list?.Count ?? 0;
        for (int i = 0; i < builtCount; i++)
            CreateItem(i);
    }

    void CreateItem(int index)
    {
        var row = DebugSkin.CreateHorizontalLayout(items, 4f, controlWidth: true);
        row.childAlignment = TextAnchor.UpperLeft;

        var shape = MemberSchema.GetConfigType(elementType) == ConfigType.Node
            ? MemberShape.Node
            : MemberShape.Value;
        var resolution = FieldEditorRegistry.Resolve(null, elementType, shape);
        var slot = new ListSlot(list, index, elementType, WriteBack);
        var fieldRow = FieldRow.Create(row.transform, Source.Scope.For(slot, resolution.Adapter), resolution);
        fieldRow.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        ItemButton(row.transform, "Up", "^", () => Move(index, -1)).interactable = index > 0;
        ItemButton(row.transform, "Down", "v", () => Move(index, 1)).interactable = index < list.Count - 1;
        ItemButton(row.transform, "Remove", "x", () => Remove(index));
    }

    static Button ItemButton(Transform parent, string name, string text, UnityEngine.Events.UnityAction onClick)
    {
        var button = DebugSkin.CreateButton(parent, text, onClick);
        button.name = name;
        EditorLayout.Compact(button.gameObject, 0f);
        var layout = button.GetComponent<LayoutElement>();
        layout.minWidth = layout.preferredWidth = EditorLayout.RowHeight;
        return button;
    }

    /// <summary>Some getters return a copy, so push the list back through the setter if there is one.</summary>
    void WriteBack()
    {
        if (Source.Member is { CanWrite: true } && !Source.IsReadOnly)
            Source.Set(list);
    }

    void Mutate(Action action)
    {
        try
        {
            action();
            WriteBack();
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"[KittopiaTech] Failed to change {Source.Label}: {e}");
        }
        builtCount = -1;
        Refresh();
    }

    void Add()
    {
        var type = concreteTypes != null ? concreteTypes[typePicker.value] : elementType;
        Mutate(() =>
        {
            list ??= (IList)Activator.CreateInstance(Source.ValueType);
            list.Add(Structure.Create(Source.Scope.Context, type));
        });
    }

    void Remove(int index) =>
        Mutate(() =>
        {
            Structure.Destroy(Source.Scope.Context, list[index]);
            list.RemoveAt(index);
        });

    void Move(int index, int delta) =>
        Mutate(() =>
        {
            int other = index + delta;
            (list[index], list[other]) = (list[other], list[index]);
        });
}
