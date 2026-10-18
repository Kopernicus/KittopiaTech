using System;
using System.Collections;
using System.Collections.Generic;
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
/// The fallback for collections: a foldout of item rows with a drag grip and remove button, and
/// an Add bar. Adding and removing items are structural edits and happen immediately; reordering
/// is staged like any other edit.
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

    [SerializeField]
    Button resetButton;

    bool expanded;
    IList live;
    IList list; // what's shown: live, or the staged reordered copy
    IValueSource order;
    readonly List<IValueSource> itemSources = [];
    int builtCount = -1;
    Type elementType;
    Type[] concreteTypes;

    public override FieldLayout Layout => FieldLayout.Wide;

    protected internal override void BuildPrefab(PrefabContext ctx)
    {
        NodeFieldEditor.CreateFoldout(transform, out header, out headerText, out content);
        resetButton = DebugSkin.CreateButton(header.transform.parent, "Reset");
        resetButton.name = "Reset";
        EditorLayout.Compact(resetButton.gameObject, 0f);
        resetButton.GetComponent<LayoutElement>().preferredWidth = 48f;
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
        resetButton.onClick.AddListener(() => order?.Reset());
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
        live = list = null;
        order = null;
        builtCount = -1;
    }

    public override void Refresh()
    {
        // Keep the list we built from: getters that return copies would otherwise orphan
        // the item slots (and their pending edits) on every refresh.
        var current = Source.Get() as IList;
        if (live == null || current == null || (!ReferenceEquals(current, live) && current.Count != live.Count))
        {
            live = current;
            order = live != null
                ? Source.Scope.For(new ListOrderSlot(Source.Slot, live), ValueAdapter.Identity(live.GetType()))
                : null;
        }

        var shown = order?.Get() as IList ?? live;
        if (!ReferenceEquals(shown, list) || list?.Count != builtCount)
        {
            list = shown;
            builtCount = -1;
        }

        int count = list?.Count ?? 0;
        bool reordered = order?.IsModified ?? false;
        string label = NodeFieldEditor.PendingLabel(Source.Label, reordered || Source.Scope.HasPendingWithin(live));
        headerText.text =
            $"{(expanded ? "-" : "+")} <b>{label}</b>  <color=#a3a7ab>{elementType?.Name} x{count}</color>";
        resetButton.gameObject.SetActive(reordered);

        if (expanded && builtCount != count)
            Rebuild();
        content.gameObject.SetActive(expanded);
    }

    void Rebuild()
    {
        InspectorBuilder.Clear(items);
        itemSources.Clear();
        builtCount = list?.Count ?? 0;
        for (int i = 0; i < builtCount; i++)
            CreateItem(i);
    }

    void CreateItem(int index)
    {
        var row = DebugSkin.CreateHorizontalLayout(items, 4f, controlWidth: true);
        row.childAlignment = TextAnchor.UpperLeft;

        var grip = ItemButton(row.transform, "Drag", "=", null);
        grip.gameObject.AddComponent<ListDragHandle>()
            .Setup((RectTransform)items, (RectTransform)row.transform, index, Move);
        grip.interactable = list.Count > 1;

        var shape = MemberSchema.GetConfigType(elementType) == ConfigType.Node
            ? MemberShape.Node
            : MemberShape.Value;
        var resolution = FieldEditorRegistry.Resolve(null, elementType, shape);
        var slot = new ListSlot(list, index, elementType, ReferenceEquals(list, live) ? WriteBack : null);
        var source = Source.Scope.For(slot, resolution.Adapter);
        itemSources.Add(source);
        var fieldRow = FieldRow.Create(row.transform, source, resolution);
        fieldRow.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

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
        if (Source.Slot.CanWrite)
        {
            using (ParseStateScope.For(Source.Scope.Context))
                Source.Slot.Write(live);
        }
    }

    void Mutate(Action action)
    {
        try
        {
            // Pending edits on items are keyed by index, so they can't survive a reshuffle.
            if (list != null)
                Source.Scope.DiscardWithin(list);
            if (live != null && !ReferenceEquals(list, live))
                Source.Scope.DiscardWithin(live);
            order?.Reset();
            action();
            WriteBack();
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"[KittopiaTech] Failed to change {Source.Label}: {e}");
        }
        builtCount = -1;
        Source.Scope.Invalidate();
    }

    void Add()
    {
        var type = concreteTypes != null ? concreteTypes[typePicker.value] : elementType;
        object created = null;
        Mutate(() =>
        {
            live ??= (IList)Activator.CreateInstance(Source.ValueType);
            created = Structure.Create(Source.Scope.Context, type);
            live.Add(created);
        });

        // Strings and other values have no hooks to run and nothing to destroy.
        if (created == null || MemberSchema.GetConfigType(created.GetType()) != ConfigType.Node)
            return;
        var (list, slot, context) = (live, Source.Slot, Source.Scope.Context);
        Source.Scope.MarkCreated(created, $"{Source.Label} item", () =>
        {
            Structure.Destroy(context, created);
            int index = IndexOf(list, created);
            if (index >= 0)
                list.RemoveAt(index);
            if (slot.CanWrite)
                slot.Write(list);
        });
    }

    void Remove(int index)
    {
        var item = list[index];
        int liveIndex = ReferenceEquals(list, live) ? index : IndexOf(live, item);
        Mutate(() =>
        {
            Structure.Destroy(Source.Scope.Context, item);
            live.RemoveAt(liveIndex);
        });
    }

    static int IndexOf(IList list, object item)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (ReferenceEquals(list[i], item))
                return i;
        }
        return list.IndexOf(item);
    }

    /// <summary>Stages a reordered copy, carrying pending item values into it.</summary>
    void Move(int from, int to)
    {
        var copy = live.GetType().GetConstructor(Type.EmptyTypes) != null
            ? (IList)Activator.CreateInstance(live.GetType())
            : new ArrayList();
        for (int i = 0; i < list.Count; i++)
        {
            var source = itemSources[i];
            bool carry = source.IsModified && source is StagedValueSource;
            copy.Add(carry ? ((StagedValueSource)source).Adapter.Wrap(source.Get()) : list[i]);
            if (carry)
                source.Reset();
        }

        var item = copy[from];
        copy.RemoveAt(from);
        copy.Insert(to, item);
        order.Set(copy);
    }
}
