using System;
using System.Globalization;
using KittopiaTech.UI.Debug;
using KittopiaTech.UI.Editors;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KittopiaTech.UI;

/// <summary>
/// A colour picker pop-out: a saturation/value square, hue and alpha bars, and RGBA, HSV and
/// hex fields. The gradients are small textures redrawn on the CPU, so no shader is needed.
/// </summary>
public sealed class ColorPicker : MonoBehaviour
{
    const int SquareSize = 128;
    const float AreaSize = 200f;
    const float BarWidth = 22f;

    Popout popout;
    Action<Color, bool> onChange;

    // HSV is the source of truth so hue survives grey and black.
    float h, s, v, a;
    Color color;
    bool drawn;

    Texture2D squareTexture;
    Texture2D hueTexture;
    Texture2D alphaTexture;
    RectTransform squareMarker;
    RectTransform hueMarker;
    RectTransform alphaMarker;
    Image swatch;
    readonly TMP_InputField[] rgba = new TMP_InputField[4];
    readonly TMP_InputField[] hsv = new TMP_InputField[3];
    TMP_InputField hex;

    /// <summary>Opens a picker; <paramref name="onChange"/> gets the colour and whether the edit is final.</summary>
    public static ColorPicker Open(Component owner, string title, Color initial, Action<Color, bool> onChange)
    {
        var popout = Popout.Open(owner, title, new Vector2(330f, 340f));
        var picker = popout.gameObject.AddComponent<ColorPicker>();
        picker.popout = popout;
        picker.onChange = onChange;
        picker.Build(popout.Content);
        picker.SetColor(initial);
        return picker;
    }

    public void Close() => popout.Close();

    /// <summary>Shows <paramref name="value"/> without reporting it back.</summary>
    public void SetColor(Color value)
    {
        if (drawn && value == color)
            return;
        drawn = true;
        Color.RGBToHSV(value, out var nh, out var ns, out var nv);
        // Keep the current hue (and saturation) where the colour doesn't determine them.
        if (ns > 0f && nv > 0f)
            h = nh;
        if (nv > 0f)
            s = ns;
        v = nv;
        a = value.a;
        color = value;
        Redraw(true);
    }

    void Build(Transform content)
    {
        var row = DebugSkin.CreateHorizontalLayout(content, 6f, controlWidth: true);
        squareTexture = NewTexture(SquareSize, SquareSize);
        hueTexture = NewTexture(1, 64);
        alphaTexture = NewTexture(1, 2);
        for (int y = 0; y < 64; y++)
            hueTexture.SetPixel(0, y, Color.HSVToRGB(y / 63f, 1f, 1f));
        hueTexture.Apply();

        squareMarker = Area(row.transform, "Square", squareTexture, AreaSize, p => SetHsv(h, p.x, p.y, a), true);
        hueMarker = Area(row.transform, "Hue", hueTexture, BarWidth, p => SetHsv(p.y, s, v, a), false);
        alphaMarker = Area(row.transform, "Alpha", alphaTexture, BarWidth, p => SetHsv(h, s, v, p.y), false);

        var swatchObject = new GameObject("Swatch", typeof(RectTransform));
        swatchObject.transform.SetParent(row.transform, false);
        swatch = swatchObject.AddComponent<Image>();
        var swatchLayout = swatchObject.AddComponent<LayoutElement>();
        swatchLayout.flexibleWidth = 1f;
        swatchLayout.minHeight = AreaSize;

        FieldRow(content, "RGBA", rgba, (i, x) =>
        {
            var c = color;
            c[i] = x;
            SetColor(c);
            Report(true);
        });
        FieldRow(content, "HSV", hsv, (i, x) =>
        {
            x = Mathf.Max(x, 0f);
            SetHsv(i == 0 ? Mathf.Repeat(x, 1f) : h, i == 1 ? Mathf.Clamp01(x) : s, i == 2 ? x : v, a);
            Report(true);
        });

        var hexRow = DebugSkin.CreateHorizontalLayout(content, 4f, controlWidth: true);
        Label(hexRow.transform, "Hex");
        hex = DebugSkin.CreateInputField(hexRow.transform);
        hex.name = "Hex";
        EditorLayout.Compact(hex.gameObject);
        hex.onEndEdit.AddListener(text =>
        {
            if (ColorUtility.TryParseHtmlString(text.StartsWith("#") ? text : "#" + text, out var parsed))
            {
                SetColor(parsed);
                Report(true);
            }
            else
                Redraw(false);
        });
    }

    void SetHsv(float nh, float ns, float nv, float na)
    {
        bool hueChanged = nh != h;
        (h, s, v, a) = (nh, ns, nv, na);
        color = Color.HSVToRGB(h, s, v, hdr: true);
        color.a = a;
        Redraw(hueChanged);
    }

    void Report(bool final) => onChange(color, final);

    void Redraw(bool square)
    {
        if (square)
        {
            var pixels = new Color32[SquareSize * SquareSize];
            for (int y = 0; y < SquareSize; y++)
                for (int x = 0; x < SquareSize; x++)
                    pixels[y * SquareSize + x] = Color.HSVToRGB(h, x / (SquareSize - 1f), y / (SquareSize - 1f));
            squareTexture.SetPixels32(pixels);
            squareTexture.Apply();
        }

        var opaque = new Color(color.r, color.g, color.b, 1f);
        alphaTexture.SetPixel(0, 0, new Color(opaque.r, opaque.g, opaque.b, 0f));
        alphaTexture.SetPixel(0, 1, opaque);
        alphaTexture.Apply();
        swatch.color = opaque;

        Place(squareMarker, s, Mathf.Clamp01(v));
        Place(hueMarker, 0.5f, h);
        Place(alphaMarker, 0.5f, a);

        for (int i = 0; i < 4; i++)
            rgba[i].text = Format(color[i]);
        hsv[0].text = Format(h);
        hsv[1].text = Format(s);
        hsv[2].text = Format(v);
        hex.text = ColorUtility.ToHtmlStringRGBA(color);
    }

    static string Format(float x) => x.ToString("0.###", CultureInfo.InvariantCulture);

    static void Place(RectTransform marker, float x, float y) =>
        marker.anchorMin = marker.anchorMax = new Vector2(x, y);

    RectTransform Area(Transform parent, string name, Texture2D texture, float width, Action<Vector2> pick, bool dot)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<RawImage>().texture = texture;
        var layout = go.AddComponent<LayoutElement>();
        layout.minWidth = layout.preferredWidth = width;
        layout.minHeight = layout.preferredHeight = AreaSize;
        var area = go.AddComponent<PickArea>();
        area.onPick = pick;
        area.onRelease = () => Report(true);
        area.onDrag = () => Report(false);

        // A dark outline with a light centre reads on any colour.
        var marker = new GameObject("Marker", typeof(RectTransform));
        marker.transform.SetParent(go.transform, false);
        marker.AddComponent<Image>().color = Color.black;
        var rect = (RectTransform)marker.transform;
        rect.sizeDelta = dot ? new Vector2(8f, 8f) : new Vector2(width + 4f, 4f);
        var inner = new GameObject("Inner", typeof(RectTransform));
        inner.transform.SetParent(marker.transform, false);
        inner.AddComponent<Image>().color = Color.white;
        var innerRect = (RectTransform)inner.transform;
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(1f, 1f);
        innerRect.offsetMax = new Vector2(-1f, -1f);
        marker.GetComponent<Image>().raycastTarget = inner.GetComponent<Image>().raycastTarget = false;
        return rect;
    }

    static void FieldRow(Transform content, string names, TMP_InputField[] fields, Action<int, float> set)
    {
        var row = DebugSkin.CreateHorizontalLayout(content, 4f, controlWidth: true);
        for (int i = 0; i < fields.Length; i++)
        {
            int index = i;
            Label(row.transform, names[i].ToString());
            fields[i] = DebugSkin.CreateInputField(row.transform);
            fields[i].name = names[i].ToString();
            fields[i].contentType = TMP_InputField.ContentType.DecimalNumber;
            EditorLayout.Compact(fields[i].gameObject);
            fields[i].onEndEdit.AddListener(text =>
            {
                if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var x))
                    set(index, x);
            });
        }
    }

    static void Label(Transform parent, string text)
    {
        var label = DebugSkin.CreateLabel(parent, text);
        var layout = label.transform.parent.GetComponent<LayoutElement>();
        layout.minWidth = layout.preferredWidth = text.Length > 1 ? 30f : 14f;
        layout.flexibleWidth = 0f;
    }

    static Texture2D NewTexture(int width, int height) =>
        new(width, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };

    void OnDestroy()
    {
        Destroy(squareTexture);
        Destroy(hueTexture);
        Destroy(alphaTexture);
    }

    /// <summary>Reports the pointer's normalised position while pressed or dragged.</summary>
    sealed class PickArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Action<Vector2> onPick;
        public Action onDrag;
        public Action onRelease;

        public void OnPointerDown(PointerEventData eventData) => Pick(eventData);

        public void OnDrag(PointerEventData eventData) => Pick(eventData);

        public void OnPointerUp(PointerEventData eventData) => onRelease();

        void Pick(PointerEventData eventData)
        {
            var rect = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out var local);
            var r = rect.rect;
            onPick(new Vector2(Mathf.Clamp01((local.x - r.xMin) / r.width), Mathf.Clamp01((local.y - r.yMin) / r.height)));
            onDrag();
        }
    }
}
