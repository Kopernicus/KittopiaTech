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
/// Float curves (lists of "key = time value in out"). Inline it's a sparkline. The pop-out has a
/// graph where keys can be dragged, selected and added (double-click), the selected key's time,
/// value and tangent mode, and a table of every key for exact numbers. Tangents follow each
/// key's mode, so they only need typing for Free keys. Every change stages the whole curve.
/// </summary>
[FieldEditor(typeof(Shapes.KeyedCurve))]
public sealed class CurveFieldEditor : FieldEditor<object>
{
    static readonly string[] Columns = ["time", "value", "in", "out"];
    static readonly TangentMode[] Modes = (TangentMode[])Enum.GetValues(typeof(TangentMode));
    static readonly Color SelectedColor = new(0.75f, 0.85f, 1f);
    const float HitRadius = 8f;

    [SerializeField]
    RawImage sparkline;

    [SerializeField]
    Button openButton;

    Texture2D sparkTexture;
    Popout popout;
    RawImage graph;
    CurveGraphArea area;
    Texture2D graphTexture;
    TextMeshProUGUI rangeLabel;
    TMP_InputField timeField, valueField, inField, outField;
    readonly List<Button> modeButtons = [];
    Button removeButton;
    Transform table;
    int tableRows = -1;

    // The keys being edited, with their modes, and the config keys they were read from or
    // last committed as, so a refresh from our own commit keeps the modes.
    List<CurveKey> keys = [];
    List<float[]> keysRaw = [];
    int selected = -1;
    int dragIndex = -1;
    bool dragging;
    Vector2 pressPoint;
    CurveKey pressKey;
    bool built;
    CurveView view;

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

    static bool Same(List<float[]> a, List<float[]> b) =>
        a.Count == b.Count && a.Zip(b, (x, y) => x.SequenceEqual(y)).All(same => same);

    /// <summary>Recomputes tangents and stages the keys, if anything changed.</summary>
    void CommitWorking()
    {
        CurveKeys.Update(keys);
        var raw = CurveKeys.ToRaw(keys);
        if (Same(raw, ReadKeys()))
        {
            Refresh();
            return;
        }
        keysRaw = raw;
        Commit(raw.Select(k => new NumericCollectionParser<float>(k.ToList())).ToList());
    }

    public override void Refresh()
    {
        var raw = ReadKeys();
        openButton.GetComponentInChildren<TextMeshProUGUI>().text = $"{raw.Count} keys...";
        sparkTexture = CurveGraph.Draw(sparkTexture, raw, 160, 22);
        sparkline.texture = sparkTexture;

        if (popout == null || dragging)
            return;
        if (!Same(raw, keysRaw))
        {
            keys = CurveKeys.FromRaw(raw);
            keysRaw = raw;
        }
        if (selected >= keys.Count)
            selected = keys.Count - 1;

        Redraw(fit: true);
        if (keys.Count != tableRows)
            BuildTable();
        else
            FillTable();
    }

    void Redraw(bool fit)
    {
        var curve = CurveKeys.ToFloatCurve(keys);
        if (fit)
            view = CurveView.Fit(keys, curve, 0.08f);
        var size = area.Size;
        int width = size.x > 1f ? Mathf.RoundToInt(size.x) : 500, height = size.y > 1f ? Mathf.RoundToInt(size.y) : 220;
        graphTexture = CurveGraph.Draw(graphTexture, keys, curve, view, width, height, selected);
        graph.texture = graphTexture;

        float tStep = CurveGraph.Step(view.T0, view.T1), vStep = CurveGraph.Step(view.V0, view.V1);
        rangeLabel.text =
            $"time {F(view.T0)} to {F(view.T1)} (grid {F(tStep)}), value {F(view.V0)} to {F(view.V1)} (grid {F(vStep)})";
        RefreshPanel();
    }

    /// <summary>About four significant figures, without exponents.</summary>
    static string F(float x)
    {
        int decimals = x == 0f ? 0 : Mathf.Clamp(3 - Mathf.FloorToInt(Mathf.Log10(Mathf.Abs(x))), 0, 7);
        return x.ToString("0." + new string('#', decimals), System.Globalization.CultureInfo.InvariantCulture);
    }

    void OpenPopout()
    {
        if (popout != null)
            return;

        popout = Popout.Open(this, FieldPath, new Vector2(560f, 620f));
        var image = new GameObject("Graph", typeof(RectTransform));
        image.transform.SetParent(popout.Content, false);
        graph = image.AddComponent<RawImage>();
        var layout = image.AddComponent<LayoutElement>();
        layout.minHeight = layout.preferredHeight = 220f;
        area = image.AddComponent<CurveGraphArea>();
        area.onPress = OnPress;
        area.onDrag = OnDrag;
        area.onRelease = OnRelease;
        area.onDoubleClick = OnDoubleClick;
        area.onResize = () =>
        {
            if (built)
                Redraw(fit: !dragging);
        };
        built = false;

        rangeLabel = DebugSkin.CreateLabel(popout.Content, "");
        rangeLabel.enableWordWrapping = false;
        rangeLabel.color = new Color(0.64f, 0.65f, 0.67f);
        var hint = DebugSkin.CreateLabel(popout.Content, "Drag keys to move them; double-click to add one.");
        hint.color = rangeLabel.color;

        modeButtons.Clear();
        BuildPanel(popout.Content);

        var header = DebugSkin.CreateHorizontalLayout(popout.Content, 4f, controlWidth: true);
        foreach (var column in Columns)
            DebugSkin.CreateLabel(header.transform, column);
        DebugSkin.CreateSpacer(header.transform, 0f).minWidth = EditorLayout.RowHeight;

        DebugSkin.CreateScrollView(popout.Content, out table);
        var add = DebugSkin.CreateButton(popout.Content, "Add key", AddKey);
        EditorLayout.Compact(add.gameObject);

        keysRaw = [];
        selected = -1;
        tableRows = -1;
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)popout.Content);
        built = true;
        Refresh();
    }

    #region Graph

    void OnPress(Vector2 n)
    {
        dragIndex = Hit(n);
        pressPoint = view.FromNormalized(n);
        if (dragIndex >= 0)
            pressKey = keys[dragIndex];
        Select(dragIndex);
    }

    void OnDrag(Vector2 n)
    {
        if (dragIndex < 0 || dragIndex >= keys.Count)
            return;

        // The view stays put while dragging, so the key doesn't chase the pointer. Keys move by
        // the pointer's offset, so a vertical drag leaves the time exactly as it was.
        dragging = true;
        var offset = view.FromNormalized(new Vector2(Mathf.Clamp01(n.x), Mathf.Clamp01(n.y))) - pressPoint;
        var p = new Vector2(pressKey.Time, pressKey.Value) + offset;
        float gap = (view.T1 - view.T0) * 1e-3f;
        float lo = dragIndex > 0 ? keys[dragIndex - 1].Time + gap : float.NegativeInfinity;
        float hi = dragIndex + 1 < keys.Count ? keys[dragIndex + 1].Time - gap : float.PositiveInfinity;

        var key = keys[dragIndex];
        key.Time = lo < hi ? Mathf.Clamp(p.x, lo, hi) : key.Time;
        key.Value = p.y;
        keys[dragIndex] = key;
        CurveKeys.Update(keys);
        Redraw(fit: false);
    }

    void OnRelease()
    {
        dragIndex = -1;
        if (!dragging)
            return;
        dragging = false;
        CommitWorking();
    }

    void OnDoubleClick(Vector2 n)
    {
        if (Hit(n) >= 0)
            return;
        var p = view.FromNormalized(n);
        Insert(p.x, p.y);
    }

    /// <summary>The key within <see cref="HitRadius"/> pixels of <paramref name="n"/>, or -1.</summary>
    int Hit(Vector2 n)
    {
        var size = area.Size;
        int best = -1;
        float bestDistance = HitRadius;
        for (int i = 0; i < keys.Count; i++)
        {
            var d = Vector2.Scale(view.ToNormalized(keys[i].Time, keys[i].Value) - n, size).magnitude;
            if (d <= bestDistance)
            {
                best = i;
                bestDistance = d;
            }
        }
        return best;
    }

    #endregion

    #region Keys

    void Select(int index)
    {
        selected = index;
        Redraw(fit: false);
        FillTable();
    }

    /// <summary>Adds a key at <paramref name="time"/>, in time order, and selects it.</summary>
    void Insert(float time, float value)
    {
        // Curves written without tangents stay that way; otherwise new keys are smooth.
        var mode = keys.Count > 0 && keys.All(k => k.Mode == TangentMode.Auto) ? TangentMode.Auto : TangentMode.Smooth;
        int index = keys.FindIndex(k => k.Time > time);
        if (index < 0)
            index = keys.Count;
        keys.Insert(index, new CurveKey(time, value, 0f, 0f, mode));
        selected = index;
        CommitWorking();
    }

    void AddKey()
    {
        if (keys.Count == 0)
        {
            Insert(0f, 0f);
            return;
        }
        var curve = CurveKeys.ToFloatCurve(keys);
        if (selected >= 0 && selected + 1 < keys.Count)
        {
            float t = (keys[selected].Time + keys[selected + 1].Time) / 2f;
            Insert(t, curve.Evaluate(t));
            return;
        }
        var last = keys[keys.Count - 1];
        float step = keys.Count > 1 ? (last.Time - keys[0].Time) / (keys.Count - 1) : 1f;
        Insert(last.Time + (step > 0f ? step : 1f), last.Value);
    }

    void RemoveKey(int index)
    {
        if (index < 0 || index >= keys.Count)
            return;
        keys.RemoveAt(index);
        if (selected >= keys.Count)
            selected = keys.Count - 1;
        CommitWorking();
    }

    /// <summary>Applies <paramref name="edit"/> to key <paramref name="index"/>, keeping time order.</summary>
    void EditKey(int index, Func<CurveKey, CurveKey> edit)
    {
        if (index < 0 || index >= keys.Count)
            return;
        var key = keys[index] = edit(keys[index]);
        var sorted = keys.Select((k, i) => (k, i)).OrderBy(x => x.k.Time).ThenBy(x => x.i).Select(x => x.k).ToList();
        if (index == selected)
            selected = sorted.FindIndex(k => k.Equals(key));
        keys = sorted;
        CommitWorking();
    }

    #endregion

    #region Selected key

    void BuildPanel(Transform content)
    {
        var row = DebugSkin.CreateHorizontalLayout(content, 4f, controlWidth: true);
        timeField = PanelField(row.transform, "time", (k, x) => { k.Time = x; return k; });
        valueField = PanelField(row.transform, "value", (k, x) => { k.Value = x; return k; });
        removeButton = DebugSkin.CreateButton(row.transform, "Remove", () => RemoveKey(selected));
        EditorLayout.Compact(removeButton.gameObject, 0f);
        removeButton.GetComponent<LayoutElement>().preferredWidth = 70f;

        var modes = DebugSkin.CreateHorizontalLayout(content, 4f, controlWidth: true);
        foreach (var mode in Modes)
        {
            var m = mode;
            var button = DebugSkin.CreateButton(modes.transform, mode.ToString(), () => EditKey(selected, k => { k.Mode = m; return k; }));
            EditorLayout.Compact(button.gameObject, 0f);
            button.GetComponent<LayoutElement>().preferredWidth = 54f;
            modeButtons.Add(button);
        }
        // Typing a tangent makes the key Free.
        inField = PanelField(modes.transform, "in", (k, x) => { k.In = x; k.Mode = TangentMode.Free; return k; });
        outField = PanelField(modes.transform, "out", (k, x) => { k.Out = x; k.Mode = TangentMode.Free; return k; });
    }

    TMP_InputField PanelField(Transform row, string label, Func<CurveKey, float, CurveKey> set)
    {
        var text = DebugSkin.CreateLabel(row, label);
        var layout = text.transform.parent.GetComponent<LayoutElement>();
        layout.minWidth = layout.preferredWidth = 34f;
        layout.flexibleWidth = 0f;

        var field = DebugSkin.CreateInputField(row);
        field.name = label;
        EditorLayout.Compact(field.gameObject);
        field.onEndEdit.AddListener(input =>
        {
            if (selected < 0 || !field.interactable || input == "auto")
                return;
            if (!NumberText.TryParse(input, typeof(float), out var parsed))
            {
                EditorLayout.MarkInvalid(field, true);
                return;
            }
            EditKey(selected, k => set(k, (float)parsed));
        });
        return field;
    }

    void RefreshPanel()
    {
        bool any = selected >= 0 && selected < keys.Count;
        var key = any ? keys[selected] : default;
        bool auto = any && key.Mode == TangentMode.Auto;
        SetField(timeField, any, any ? NumberText.Format(key.Time) : "");
        SetField(valueField, any, any ? NumberText.Format(key.Value) : "");
        SetField(inField, any, !any ? "" : auto ? "auto" : NumberText.Format(key.In));
        SetField(outField, any, !any ? "" : auto ? "auto" : NumberText.Format(key.Out));
        removeButton.interactable = any;
        for (int i = 0; i < modeButtons.Count; i++)
        {
            modeButtons[i].interactable = any;
            modeButtons[i].targetGraphic.color = any && key.Mode == Modes[i] ? SelectedColor : Color.white;
        }
    }

    static void SetField(TMP_InputField field, bool interactable, string text)
    {
        field.interactable = interactable;
        if (!field.isFocused)
            field.text = text;
        EditorLayout.MarkInvalid(field, false);
    }

    #endregion

    #region Table

    void BuildTable()
    {
        // Destroy waits for the end of the frame, so detach the old rows for FillTable.
        for (int i = table.childCount - 1; i >= 0; i--)
        {
            var child = table.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }

        for (int row = 0; row < keys.Count; row++)
        {
            var line = DebugSkin.CreateHorizontalLayout(table, 4f, controlWidth: true);
            int r = row;
            for (int col = 0; col < Columns.Length; col++)
            {
                var input = DebugSkin.CreateInputField(line.transform);
                EditorLayout.Compact(input.gameObject);
                int c = col;
                input.onSelect.AddListener(_ => { if (selected != r) Select(r); });
                input.onEndEdit.AddListener(text => OnCell(r, c, text, input));
            }
            var remove = DebugSkin.CreateButton(line.transform, "x", () => RemoveKey(r));
            EditorLayout.Compact(remove.gameObject, 0f);
            remove.GetComponent<LayoutElement>().minWidth = EditorLayout.RowHeight;
        }
        tableRows = keys.Count;
        FillTable();
    }

    void FillTable()
    {
        for (int row = 0; row < keys.Count && row < table.childCount; row++)
        {
            var key = keys[row];
            var inputs = table.GetChild(row).GetComponentsInChildren<TMP_InputField>();
            bool auto = key.Mode == TangentMode.Auto;
            string[] cells =
            [
                NumberText.Format(key.Time),
                NumberText.Format(key.Value),
                auto ? "" : NumberText.Format(key.In),
                auto ? "" : NumberText.Format(key.Out),
            ];
            for (int col = 0; col < inputs.Length; col++)
            {
                if (!inputs[col].isFocused)
                    inputs[col].text = cells[col];
                var colors = inputs[col].colors;
                colors.normalColor = row == selected ? SelectedColor : Color.white;
                inputs[col].colors = colors;
            }
        }
    }

    void OnCell(int row, int col, string text, TMP_InputField input)
    {
        if (row >= keys.Count)
            return;

        // An empty tangent cell drops the tangents and lets KSP pick them.
        if (col >= 2 && string.IsNullOrEmpty(text.Trim()))
        {
            if (keys[row].Mode != TangentMode.Auto)
                EditKey(row, k => { k.Mode = TangentMode.Auto; return k; });
            return;
        }
        if (!NumberText.TryParse(text, typeof(float), out var parsed))
        {
            EditorLayout.MarkInvalid(input, true);
            return;
        }

        float x = (float)parsed;
        EditKey(row, k =>
        {
            switch (col)
            {
                case 0: k.Time = x; break;
                case 1: k.Value = x; break;
                case 2: k.In = x; k.Mode = TangentMode.Free; break;
                default: k.Out = x; k.Mode = TangentMode.Free; break;
            }
            return k;
        });
    }

    #endregion
}
