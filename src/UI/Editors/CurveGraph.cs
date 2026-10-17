using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KittopiaTech.UI.Editors;

/// <summary>The time and value range a curve graph shows.</summary>
public struct CurveView(float t0, float t1, float v0, float v1)
{
    public float T0 = t0, T1 = t1, V0 = v0, V1 = v1;

    /// <summary>Fits the keys and the curve between them, padded by <paramref name="pad"/> of each range.</summary>
    public static CurveView Fit(List<CurveKey> keys, FloatCurve curve, float pad)
    {
        if (keys.Count == 0)
            return new CurveView(0f, 1f, 0f, 1f);

        float t0 = keys.Min(k => k.Time), t1 = keys.Max(k => k.Time);
        if (t1 - t0 < 1e-6f)
            t1 = t0 + 1f;
        var values = Enumerable.Range(0, 64).Select(i => curve.Evaluate(Mathf.Lerp(t0, t1, i / 63f)))
            .Concat(keys.Select(k => k.Value)).ToList();
        float v0 = values.Min(), v1 = values.Max();
        if (v1 - v0 < 1e-6f)
        {
            v0 -= 0.5f;
            v1 += 0.5f;
        }

        float dt = (t1 - t0) * pad, dv = (v1 - v0) * pad;
        return new CurveView(t0 - dt, t1 + dt, v0 - dv, v1 + dv);
    }

    public Vector2 ToNormalized(float t, float v) => new((t - T0) / (T1 - T0), (v - V0) / (V1 - V0));

    public Vector2 FromNormalized(Vector2 n) => new(Mathf.Lerp(T0, T1, n.x), Mathf.Lerp(V0, V1, n.y));
}

/// <summary>Draws a float curve into a texture.</summary>
public static class CurveGraph
{
    static readonly Color32 Background = new Color(0.07f, 0.075f, 0.08f, 1f);
    static readonly Color32 Grid = new Color(0.15f, 0.16f, 0.17f, 1f);
    static readonly Color32 Axis = new Color(0.3f, 0.31f, 0.33f, 1f);
    static readonly Color32 Line = new Color(0.62f, 0.89f, 0.42f, 1f);
    static readonly Color32 Point = new Color(1f, 0.71f, 0.28f, 1f);
    static readonly Color32 Selected = Color.white;

    /// <summary>A sparkline: the curve over its keys, without a grid.</summary>
    public static Texture2D Draw(Texture2D texture, List<float[]> raw, int width, int height)
    {
        var keys = CurveKeys.FromRaw(raw);
        var curve = CurveKeys.ToFloatCurve(keys);
        return Draw(texture, keys, curve, CurveView.Fit(keys, curve, 0.05f), width, height, -1, grid: false);
    }

    public static Texture2D Draw(
        Texture2D texture,
        List<CurveKey> keys,
        FloatCurve curve,
        CurveView view,
        int width,
        int height,
        int selected,
        bool grid = true
    )
    {
        if (texture == null || texture.width != width || texture.height != height)
        {
            UnityEngine.Object.Destroy(texture);
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        }

        var pixels = new Color32[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Background;

        int X(float t) => Mathf.RoundToInt(view.ToNormalized(t, 0f).x * (width - 1));
        int Y(float v) => Mathf.RoundToInt(view.ToNormalized(0f, v).y * (height - 1));

        if (grid)
        {
            foreach (var t in Ticks(view.T0, view.T1))
                VLine(pixels, width, height, X(t), Mathf.Abs(t) < 1e-9f ? Axis : Grid);
            foreach (var v in Ticks(view.V0, view.V1))
                HLine(pixels, width, height, Y(v), Mathf.Abs(v) < 1e-9f ? Axis : Grid);
        }

        if (keys.Count > 0)
        {
            int previous = Y(curve.Evaluate(view.T0));
            for (int x = 0; x < width; x++)
            {
                int y = Y(curve.Evaluate(Mathf.Lerp(view.T0, view.T1, x / (width - 1f))));
                for (int yy = Math.Min(y, previous); yy <= Math.Max(y, previous); yy++)
                    Set(pixels, width, height, x, yy, Line);
                previous = y;
            }

            for (int i = 0; i < keys.Count; i++)
            {
                int r = i == selected ? 3 : grid ? 2 : 1;
                for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                    Set(pixels, width, height, X(keys[i].Time) + dx, Y(keys[i].Value) + dy, i == selected ? Selected : Point);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    /// <summary>The grid step for a range: 1, 2 or 5 times a power of ten, about five per range.</summary>
    public static float Step(float min, float max)
    {
        double rough = (max - min) / 5.0;
        double power = Math.Pow(10, Math.Floor(Math.Log10(rough)));
        double step = rough / power < 1.5 ? 1 : rough / power < 3.5 ? 2 : rough / power < 7.5 ? 5 : 10;
        return (float)(step * power);
    }

    static IEnumerable<float> Ticks(float min, float max)
    {
        float step = Step(min, max);
        if (!(step > 0f))
            yield break;
        for (double t = Math.Ceiling(min / step) * step; t <= max; t += step)
            yield return (float)t;
    }

    static void Set(Color32[] pixels, int width, int height, int x, int y, Color32 color)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
            pixels[y * width + x] = color;
    }

    static void VLine(Color32[] pixels, int width, int height, int x, Color32 color)
    {
        for (int y = 0; y < height; y++)
            Set(pixels, width, height, x, y, color);
    }

    static void HLine(Color32[] pixels, int width, int height, int y, Color32 color)
    {
        for (int x = 0; x < width; x++)
            Set(pixels, width, height, x, y, color);
    }
}

/// <summary>Reports presses, drags and double-clicks on a graph, in normalised coordinates.</summary>
public sealed class CurveGraphArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerClickHandler
{
    public Action<Vector2> onPress;
    public Action<Vector2> onDrag;
    public Action onRelease;
    public Action<Vector2> onDoubleClick;
    public Action onResize;

    public Vector2 Size => ((RectTransform)transform).rect.size;

    // The graph is drawn at its on-screen size, so it's redrawn when that changes.
    void OnRectTransformDimensionsChange() => onResize?.Invoke();

    public void OnPointerDown(PointerEventData eventData) => onPress?.Invoke(Normalize(eventData));

    public void OnDrag(PointerEventData eventData) => onDrag?.Invoke(Normalize(eventData));

    public void OnPointerUp(PointerEventData eventData) => onRelease?.Invoke();

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount == 2)
            onDoubleClick?.Invoke(Normalize(eventData));
    }

    Vector2 Normalize(PointerEventData eventData)
    {
        var rect = (RectTransform)transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out var local);
        var r = rect.rect;
        return new Vector2((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height);
    }
}
