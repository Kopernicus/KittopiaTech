using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KittopiaTech.Model;
using KittopiaTech.UI.Debug;
using Kopernicus.ConfigParser.BuiltinTypeParsers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Editors;

/// <summary>
/// Float curves (lists of "key = time value in out"). Inline it's a sparkline; the pop-out has
/// a larger graph and an editable key table. Every change stages the whole curve.
/// </summary>
[FieldEditor(typeof(Shapes.KeyedCurve))]
public sealed class CurveFieldEditor : FieldEditor<object>
{
    static readonly string[] Columns = ["time", "value", "in", "out"];

    [SerializeField]
    RawImage sparkline;

    [SerializeField]
    Button openButton;

    Texture2D sparkTexture;
    Popout popout;
    RawImage graph;
    Texture2D graphTexture;
    Transform table;
    int tableRows = -1;

    public static bool CanEdit(MemberSchema member, Type valueType) =>
        valueType.IsGenericType && valueType.GetGenericArguments()[0] == typeof(NumericCollectionParser<float>);

    protected internal override void BuildPrefab(PrefabContext ctx)
    {
        EditorLayout.Row(transform);
        var image = new GameObject("Sparkline", typeof(RectTransform));
        image.transform.SetParent(transform, false);
        sparkline = image.AddComponent<RawImage>();
        var layout = image.AddComponent<LayoutElement>();
        layout.flexibleWidth = 1f;
        layout.minHeight = layout.preferredHeight = EditorLayout.RowHeight;

        openButton = DebugSkin.CreateButton(transform, "");
        EditorLayout.Compact(openButton.gameObject, 0f);
        openButton.GetComponent<LayoutElement>().preferredWidth = 80f;
    }

    protected override void Awake() => openButton.onClick.AddListener(OpenPopout);

    protected override void OnUnbind() => popout?.Close();

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Destroy(sparkTexture);
        Destroy(graphTexture);
    }

    List<float[]> ReadKeys() =>
        (Source.Get() as IList)?.Cast<NumericCollectionParser<float>>()
            .Select(k => k?.Value?.ToArray() ?? new float[2])
            .ToList() ?? [];

    void CommitKeys(List<float[]> keys) =>
        Commit(keys.Select(k => new NumericCollectionParser<float>(k.ToList())).ToList());

    public override void Refresh()
    {
        var keys = ReadKeys();
        openButton.GetComponentInChildren<TextMeshProUGUI>().text = $"{keys.Count} keys...";
        sparkTexture = CurveGraph.Draw(sparkTexture, keys, 160, 22);
        sparkline.texture = sparkTexture;

        if (popout == null)
            return;
        graphTexture = CurveGraph.Draw(graphTexture, keys, 480, 160);
        graph.texture = graphTexture;
        if (keys.Count != tableRows)
            BuildTable(keys);
        else
            FillTable(keys);
    }

    void OpenPopout()
    {
        if (popout != null)
            return;

        popout = Popout.Open(this, FieldPath, new Vector2(520f, 460f));
        var image = new GameObject("Graph", typeof(RectTransform));
        image.transform.SetParent(popout.Content, false);
        graph = image.AddComponent<RawImage>();
        var layout = image.AddComponent<LayoutElement>();
        layout.minHeight = layout.preferredHeight = 160f;

        var header = DebugSkin.CreateHorizontalLayout(popout.Content, 4f, controlWidth: true);
        foreach (var column in Columns)
            DebugSkin.CreateLabel(header.transform, column);
        DebugSkin.CreateSpacer(header.transform, 0f).minWidth = EditorLayout.RowHeight;

        DebugSkin.CreateScrollView(popout.Content, out table);
        var add = DebugSkin.CreateButton(popout.Content, "Add key", AddKey);
        EditorLayout.Compact(add.gameObject);

        tableRows = -1;
        Refresh();
    }

    void BuildTable(List<float[]> keys)
    {
        for (int i = table.childCount - 1; i >= 0; i--)
            Destroy(table.GetChild(i).gameObject);

        for (int row = 0; row < keys.Count; row++)
        {
            var line = DebugSkin.CreateHorizontalLayout(table, 4f, controlWidth: true);
            for (int col = 0; col < Columns.Length; col++)
            {
                var input = DebugSkin.CreateInputField(line.transform);
                EditorLayout.Compact(input.gameObject);
                int r = row, c = col;
                input.onEndEdit.AddListener(text => OnCell(r, c, text, input));
            }
            int index = row;
            var remove = DebugSkin.CreateButton(line.transform, "x", () => RemoveKey(index));
            EditorLayout.Compact(remove.gameObject, 0f);
            remove.GetComponent<LayoutElement>().minWidth = EditorLayout.RowHeight;
        }
        tableRows = keys.Count;
        FillTable(keys);
    }

    void FillTable(List<float[]> keys)
    {
        for (int row = 0; row < keys.Count && row < table.childCount; row++)
        {
            var inputs = table.GetChild(row).GetComponentsInChildren<TMP_InputField>();
            for (int col = 0; col < inputs.Length; col++)
            {
                inputs[col].text = col < keys[row].Length ? NumberText.Format(keys[row][col]) : "";
                EditorLayout.MarkInvalid(inputs[col], false);
            }
        }
    }

    void OnCell(int row, int col, string text, TMP_InputField input)
    {
        var keys = ReadKeys();
        if (row >= keys.Count)
            return;

        // An empty tangent cell drops the tangents and lets the curve pick smooth ones.
        if (col >= 2 && string.IsNullOrEmpty(text.Trim()))
        {
            keys[row] = keys[row].Take(2).ToArray();
            CommitKeys(keys);
            return;
        }
        if (!NumberText.TryParse(text, typeof(float), out var parsed))
        {
            EditorLayout.MarkInvalid(input, true);
            return;
        }

        var key = keys[row].Concat(new float[4]).Take(Math.Max(keys[row].Length, col + 1)).ToArray();
        key[col] = (float)parsed;
        keys[row] = key;
        CommitKeys(keys);
    }

    void AddKey()
    {
        var keys = ReadKeys();
        var last = keys.LastOrDefault();
        keys.Add(last == null ? [0f, 0f] : [last[0] + 1f, last.Length > 1 ? last[1] : 0f]);
        CommitKeys(keys);
    }

    void RemoveKey(int index)
    {
        var keys = ReadKeys();
        if (index < keys.Count)
            keys.RemoveAt(index);
        CommitKeys(keys);
    }
}
