using System.Collections.Generic;
using System.Linq;
using KittopiaTech.Model;
using KittopiaTech.UI.Debug;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Editors;

/// <summary>
/// Colour ramps that Kopernicus bakes from gradient stops. Inline it's the ramp. The pop-out
/// shows the ramp over a lane of stop markers that can be dragged, selected and added
/// (double-click), and the selected stop's time and colour. Every change stages the whole ramp.
/// </summary>
[FieldEditor(typeof(GradientRamp))]
public sealed class GradientFieldEditor : FieldEditor<GradientRamp>
{
    const float RampHeight = 40f;
    const float LaneHeight = 18f;
    const float HitRadius = 7f;
    const int Checker = 6;

    static readonly Color32 LaneColor = new Color(0.07f, 0.075f, 0.08f, 1f);

    [SerializeField]
    RawImage strip;

    [SerializeField]
    Button openButton;

    Texture2D stripTexture;
    Popout popout;
    CurveGraphArea area;
    RawImage ramp;
    Texture2D rampTexture;
    TMP_InputField timeField;
    Image swatch;
    Button removeButton;
    ColorPicker picker;

    // The stops being edited; they're only re-read from the source when not dragging.
    List<GradientStop> stops = [];
    int selected = -1;
    int dragIndex = -1;
    bool dragging;
    float pressX;
    float pressTime;

    protected internal override void BuildPrefab(PrefabContext ctx)
    {
        EditorLayout.Row(transform);
        var image = new GameObject("Ramp", typeof(RectTransform));
        image.transform.SetParent(transform, false);
        strip = image.AddComponent<RawImage>();
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
        Destroy(stripTexture);
        Destroy(rampTexture);
    }

    public override void Refresh()
    {
        var value = Value;
        int count = value?.Stops.Count ?? 0;
        openButton.GetComponentInChildren<TextMeshProUGUI>().text = count > 0 ? $"{count} stops..." : "none...";
        openButton.interactable = !Source.IsReadOnly;
        stripTexture = Draw(stripTexture, value, 160, 22, lane: 0, selected: -1);
        strip.texture = stripTexture;

        if (popout == null || dragging)
            return;
        stops = value?.Stops.ToList() ?? [];
        if (selected >= stops.Count)
            selected = stops.Count - 1;
        Redraw();
    }

    void OpenPopout()
    {
        if (popout != null)
            return;

        popout = Popout.Open(this, FieldPath, new Vector2(480f, 150f));
        var image = new GameObject("Stops", typeof(RectTransform));
        image.transform.SetParent(popout.Content, false);
        ramp = image.AddComponent<RawImage>();
        var layout = image.AddComponent<LayoutElement>();
        layout.minHeight = layout.preferredHeight = RampHeight + LaneHeight;
        area = image.AddComponent<CurveGraphArea>();
        area.onPress = OnPress;
        area.onDrag = OnDrag;
        area.onRelease = OnRelease;
        area.onDoubleClick = OnDoubleClick;
        area.onResize = () =>
        {
            if (ramp != null)
                Redraw();
        };

        var hint = DebugSkin.CreateLabel(popout.Content, "Drag stops to move them. Double-click to add a stop.");
        hint.color = new Color(0.64f, 0.65f, 0.67f);
        FitTextHeight.Attach(hint);
        BuildPanel(popout.Content);

        popout.Closed += () => picker?.Close();
        selected = -1;
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)popout.Content);
        Refresh();
    }

    void BuildPanel(Transform content)
    {
        var row = DebugSkin.CreateHorizontalLayout(content, 4f, controlWidth: true);
        var label = DebugSkin.CreateLabel(row.transform, "time");
        var labelLayout = label.transform.parent.GetComponent<LayoutElement>();
        labelLayout.minWidth = labelLayout.preferredWidth = 34f;
        labelLayout.flexibleWidth = 0f;

        timeField = DebugSkin.CreateInputField(row.transform);
        timeField.name = "time";
        EditorLayout.Compact(timeField.gameObject);
        timeField.onEndEdit.AddListener(text =>
        {
            if (selected < 0 || !timeField.interactable)
                return;
            if (!NumberText.TryParse(text, typeof(float), out var parsed))
            {
                EditorLayout.MarkInvalid(timeField, true);
                return;
            }
            Move(selected, Mathf.Clamp01((float)parsed));
            CommitStops();
        });

        var go = new GameObject("Colour", typeof(RectTransform));
        go.transform.SetParent(row.transform, false);
        swatch = go.AddComponent<Image>();
        var swatchLayout = go.AddComponent<LayoutElement>();
        swatchLayout.minWidth = swatchLayout.preferredWidth = 60f;
        swatchLayout.minHeight = swatchLayout.preferredHeight = EditorLayout.RowHeight - 4f;
        go.AddComponent<Button>().onClick.AddListener(OpenPicker);
        Tooltip.Attach(go, "The selected stop's colour");

        removeButton = DebugSkin.CreateButton(row.transform, "Remove", RemoveSelected);
        EditorLayout.Compact(removeButton.gameObject, 0f);
        removeButton.GetComponent<LayoutElement>().preferredWidth = 70f;
        var add = DebugSkin.CreateButton(row.transform, "Add Stop", AddStop);
        EditorLayout.Compact(add.gameObject, 0f);
        add.GetComponent<LayoutElement>().preferredWidth = 80f;
    }

    void Redraw()
    {
        var size = area.Size;
        int width = size.x > 1f ? Mathf.RoundToInt(size.x) : 460;
        rampTexture = Draw(rampTexture, new GradientRamp(stops), width, Mathf.RoundToInt(RampHeight + LaneHeight),
            Mathf.RoundToInt(LaneHeight), selected);
        ramp.texture = rampTexture;

        bool any = selected >= 0 && selected < stops.Count;
        timeField.interactable = any;
        if (!timeField.isFocused)
            timeField.text = any ? NumberText.Format(stops[selected].Time) : "";
        EditorLayout.MarkInvalid(timeField, false);
        swatch.color = any ? Opaque(stops[selected].Color) : new Color(0.2f, 0.2f, 0.2f);
        swatch.GetComponent<Button>().interactable = any;
        removeButton.interactable = any;
        if (picker != null)
        {
            if (any)
                picker.SetColor(stops[selected].Color);
            else
                picker.Close();
        }
    }

    void CommitStops() => Commit(new GradientRamp(stops));

    #region Stops

    void OnPress(Vector2 n)
    {
        dragIndex = Hit(n);
        pressX = n.x;
        if (dragIndex >= 0)
            pressTime = stops[dragIndex].Time;
        selected = dragIndex;
        Redraw();
    }

    void OnDrag(Vector2 n)
    {
        if (dragIndex < 0 || dragIndex >= stops.Count)
            return;
        dragging = true;
        Move(dragIndex, Mathf.Clamp01(pressTime + n.x - pressX));
        dragIndex = selected;
        Redraw();
    }

    void OnRelease()
    {
        dragIndex = -1;
        if (!dragging)
            return;
        dragging = false;
        CommitStops();
    }

    void OnDoubleClick(Vector2 n)
    {
        if (Hit(n) < 0)
            Insert(Mathf.Clamp01(n.x));
    }

    /// <summary>The stop whose marker is within <see cref="HitRadius"/> pixels of <paramref name="n"/>, or -1.</summary>
    int Hit(Vector2 n)
    {
        float width = area.Size.x;
        int best = -1;
        float bestDistance = HitRadius;
        for (int i = 0; i < stops.Count; i++)
        {
            float d = Mathf.Abs(stops[i].Time - n.x) * width;
            if (d <= bestDistance)
            {
                best = i;
                bestDistance = d;
            }
        }
        return best;
    }

    /// <summary>Moves stop <paramref name="index"/> to <paramref name="time"/>, keeping time order; it stays selected.</summary>
    void Move(int index, float time)
    {
        var stop = new GradientStop(time, stops[index].Color);
        stops.RemoveAt(index);
        int at = stops.FindIndex(s => s.Time > time);
        selected = at < 0 ? stops.Count : at;
        stops.Insert(selected, stop);
    }

    /// <summary>Adds a stop at <paramref name="time"/> in the colour the ramp has there, and selects it.</summary>
    void Insert(float time)
    {
        var color = stops.Count > 0 ? new GradientRamp(stops).Evaluate(time) : Color.white;
        stops.Add(new GradientStop(time, color));
        Move(stops.Count - 1, time);
        CommitStops();
    }

    void AddStop()
    {
        if (stops.Count == 0)
            Insert(0f);
        else if (selected >= 0 && selected + 1 < stops.Count)
            Insert((stops[selected].Time + stops[selected + 1].Time) / 2f);
        else if (stops[stops.Count - 1].Time < 1f)
            Insert((stops[stops.Count - 1].Time + 1f) / 2f);
        else
            Insert(stops[0].Time / 2f);
    }

    void RemoveSelected()
    {
        if (selected < 0 || selected >= stops.Count)
            return;
        stops.RemoveAt(selected);
        selected = Mathf.Min(selected, stops.Count - 1);
        CommitStops();
    }

    void OpenPicker()
    {
        if (picker != null || selected < 0)
            return;
        picker = ColorPicker.Open(this, $"{FieldPath} stop", stops[selected].Color, (c, final) =>
        {
            if (selected < 0 || selected >= stops.Count)
                return;
            stops[selected] = new GradientStop(stops[selected].Time, c);
            CommitStops();
        });

        // Below the stops rather than over them.
        var window = (RectTransform)popout.transform;
        var pickerWindow = (RectTransform)picker.transform;
        pickerWindow.anchoredPosition = window.anchoredPosition
            - new Vector2(0f, (window.rect.height + pickerWindow.rect.height) / 2f + 8f);
    }

    #endregion

    static Color Opaque(Color c) => new(c.r, c.g, c.b, 1f);

    /// <summary>
    /// The ramp over a checkerboard, so alpha shows, above a lane of <paramref name="lane"/>
    /// pixels with a marker in each stop's colour; the selected one is outlined in white.
    /// </summary>
    static Texture2D Draw(Texture2D texture, GradientRamp gradient, int width, int height, int lane, int selected)
    {
        if (texture == null || texture.width != width || texture.height != height)
        {
            Destroy(texture);
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        }

        var pixels = new Color32[width * height];
        for (int x = 0; x < width; x++)
        {
            var c = gradient?.Evaluate(x / (width - 1f)) ?? Color.clear;
            for (int y = 0; y < height; y++)
            {
                if (y < lane)
                {
                    pixels[y * width + x] = LaneColor;
                    continue;
                }
                float check = (x / Checker + y / Checker) % 2 == 0 ? 0.35f : 0.55f;
                pixels[y * width + x] = Color.Lerp(new Color(check, check, check), Opaque(c), c.a);
            }
        }

        for (int i = 0; lane > 0 && gradient != null && i < gradient.Stops.Count; i++)
        {
            var stop = gradient.Stops[i];
            int cx = Mathf.RoundToInt(stop.Time * (width - 1));
            Color32 outline = i == selected ? Color.white : new Color(0.6f, 0.6f, 0.6f);
            for (int dx = -4; dx <= 4; dx++)
            for (int y = 1; y < lane - 1; y++)
            {
                int x = cx + dx;
                if (x < 0 || x >= width)
                    continue;
                bool edge = Mathf.Abs(dx) == 4 || y == 1 || y == lane - 2;
                pixels[y * width + x] = edge ? outline : (Color32)Opaque(stop.Color);
            }
            // A tick up into the ramp shows exactly where the stop sits.
            for (int y = lane; y < Mathf.Min(height, lane + 6); y++)
                pixels[y * width + Mathf.Clamp(cx, 0, width - 1)] = outline;
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }
}
