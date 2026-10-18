using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KittopiaTech.Model;
using KittopiaTech.UI.Debug;
using Kopernicus.ConfigParser.BuiltinTypeParsers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Editors;

/// <summary>
/// Lists of plain strings, such as a body's <c>implements</c> UBIs, as one text box per string
/// plus one to add another. Kopernicus reads <c>implements</c> as comma-separated lines, so a
/// list of StringCollectionParsers is shown flattened and written back as a single line.
/// </summary>
[FieldEditor(typeof(Shapes.Collection), Priority = 1)]
public sealed class StringListFieldEditor : FieldEditor<IList>
{
    [SerializeField]
    Transform rows;

    [SerializeField]
    TMP_InputField add;

    public static bool CanEdit(MemberSchema member, Type valueType) =>
        member?.ElementType == typeof(string) || member?.ElementType == typeof(StringCollectionParser);

    protected internal override void BuildPrefab(PrefabContext ctx)
    {
        var vlg = gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2f;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var rowsGo = new GameObject("Rows", typeof(RectTransform));
        rowsGo.transform.SetParent(transform, false);
        var rowsLayout = rowsGo.AddComponent<VerticalLayoutGroup>();
        rowsLayout.spacing = 2f;
        rowsLayout.childControlWidth = rowsLayout.childControlHeight = true;
        rowsLayout.childForceExpandWidth = true;
        rowsLayout.childForceExpandHeight = false;
        rows = rowsGo.transform;

        add = DebugSkin.CreateInputField(transform);
        add.name = "Add";
        if (add.placeholder is TMP_Text placeholder)
            placeholder.text = "Add";
        EditorLayout.Compact(add.gameObject);
    }

    protected override void Awake() => add.onEndEdit.AddListener(OnAdd);

    List<string> Strings =>
        Value?.Cast<object>()
            .SelectMany(item => item is StringCollectionParser p ? p.Value ?? [] : [item as string])
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList() ?? [];

    void OnAdd(string text)
    {
        text = text.Trim();
        if (text.Length == 0)
            return;
        add.text = "";
        if (!Strings.Contains(text))
            Set([.. Strings, text]);
    }

    void Set(List<string> strings)
    {
        if (strings.SequenceEqual(Strings))
            return;
        IList list = Source.Member.ElementType == typeof(string)
            ? strings
            : new List<StringCollectionParser> { new(strings) };
        Commit(list);
    }

    public override void Refresh()
    {
        // Rows are few and only rebuilt on edits, so build them afresh rather than pooling.
        for (int i = rows.childCount - 1; i >= 0; i--)
            Destroy(rows.GetChild(i).gameObject);

        var strings = Strings;
        for (int i = 0; i < strings.Count; i++)
        {
            int index = i;
            var row = DebugSkin.CreateHorizontalLayout(rows, 4f, controlWidth: true);
            var input = DebugSkin.CreateInputField(row.transform);
            input.text = strings[i];
            input.interactable = !Source.IsReadOnly;
            EditorLayout.Compact(input.gameObject);
            input.onEndEdit.AddListener(text => Replace(index, text));

            var remove = DebugSkin.CreateButton(row.transform, "x", () => Replace(index, ""));
            remove.name = "Remove";
            remove.interactable = !Source.IsReadOnly;
            EditorLayout.Compact(remove.gameObject, 0f);
            remove.GetComponent<LayoutElement>().preferredWidth = 22f;
        }
        add.interactable = !Source.IsReadOnly;
    }

    /// <summary>Changes one string; an empty one is removed.</summary>
    void Replace(int index, string text)
    {
        var strings = Strings;
        if (index >= strings.Count)
            return;
        text = text.Trim();
        if (text.Length == 0)
            strings.RemoveAt(index);
        else
            strings[index] = text;
        Set(strings);
    }
}
