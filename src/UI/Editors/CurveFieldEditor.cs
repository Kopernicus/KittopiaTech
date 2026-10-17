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

        popout = Popout.Open(this, $"Curve: {Source.Label}", new Vector2(520f, 460f));
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

/// <summary>Draws a float curve into a texture.</summary>
public static class CurveGraph
{
    static readonly Color Background = new(0.07f, 0.075f, 0.08f, 1f);
    static readonly Color Line = new(0.62f, 0.89f, 0.42f, 1f);
    static readonly Color Point = new(1f, 0.71f, 0.28f, 1f);

    public static Texture2D Draw(Texture2D texture, List<float[]> keys, int width, int height)
    {
        if (texture == null || texture.width != width || texture.height != height)
        {
            UnityEngine.Object.Destroy(texture);
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        }

        var pixels = Enumerable.Repeat(Background, width * height).ToArray();
        if (keys.Count > 0)
        {
            var curve = new FloatCurve();
            foreach (var k in keys)
            {
                if (k.Length >= 4)
                    curve.Add(k[0], k[1], k[2], k[3]);
                else if (k.Length >= 2)
                    curve.Add(k[0], k[1]);
            }

            float t0 = keys.Min(k => k[0]), t1 = keys.Max(k => k[0]);
            if (t1 <= t0)
                t1 = t0 + 1f;
            var samples = Enumerable.Range(0, width).Select(x => curve.Evaluate(Mathf.Lerp(t0, t1, x / (width - 1f)))).ToArray();
            float v0 = samples.Concat(keys.Select(k => k.Length > 1 ? k[1] : 0f)).Min();
            float v1 = samples.Concat(keys.Select(k => k.Length > 1 ? k[1] : 0f)).Max();
            if (v1 <= v0)
                v1 = v0 + 1f;

            int Y(float v) => Mathf.Clamp(Mathf.RoundToInt((v - v0) / (v1 - v0) * (height - 3)) + 1, 0, height - 1);

            int previous = Y(samples[0]);
            for (int x = 0; x < width; x++)
            {
                int y = Y(samples[x]);
                for (int yy = Math.Min(y, previous); yy <= Math.Max(y, previous); yy++)
                    pixels[yy * width + x] = Line;
                previous = y;
            }

            foreach (var k in keys.Where(k => k.Length > 1))
            {
                int px = Mathf.RoundToInt((k[0] - t0) / (t1 - t0) * (width - 1));
                int py = Y(k[1]);
                for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    int x = px + dx, y = py + dy;
                    if (x >= 0 && x < width && y >= 0 && y < height)
                        pixels[y * width + x] = Point;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}
