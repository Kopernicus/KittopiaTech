using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KittopiaTech.UI.Editors;

/// <summary>How a curve key's tangents are chosen.</summary>
public enum TangentMode
{
    /// <summary>No tangents in the config ("key = time value"); KSP picks them when loading.</summary>
    Auto,

    /// <summary>The slope between the neighbouring keys.</summary>
    Smooth,

    /// <summary>Straight lines to the neighbouring keys.</summary>
    Linear,

    /// <summary>Zero slope.</summary>
    Flat,

    /// <summary>Tangents as typed.</summary>
    Free,
}

/// <summary>One key of a float curve.</summary>
public struct CurveKey(float time, float value, float inTangent, float outTangent, TangentMode mode)
{
    public float Time = time;
    public float Value = value;
    public float In = inTangent;
    public float Out = outTangent;
    public TangentMode Mode = mode;
}

/// <summary>
/// Converts between config keys ("time value [in out]") and <see cref="CurveKey"/>s, and keeps
/// tangents in step with their modes, so nobody has to type tangents unless they want to.
/// </summary>
public static class CurveKeys
{
    /// <summary>Reads config keys, guessing each key's mode from its tangents.</summary>
    public static List<CurveKey> FromRaw(List<float[]> raw)
    {
        var keys = raw.Select(k =>
                k.Length >= 4
                    ? new CurveKey(k[0], k[1], k[2], k[3], TangentMode.Free)
                    : new CurveKey(k.Length > 0 ? k[0] : 0f, k.Length > 1 ? k[1] : 0f, 0f, 0f, TangentMode.Auto)
            )
            .ToList();

        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i].Mode == TangentMode.Auto)
                continue;
            foreach (var mode in new[] { TangentMode.Flat, TangentMode.Smooth, TangentMode.Linear })
            {
                Tangents(keys, i, mode, out var tin, out var tout);
                if (Near(tin, keys[i].In) && Near(tout, keys[i].Out))
                {
                    var key = keys[i];
                    key.Mode = mode;
                    keys[i] = key;
                    break;
                }
            }
        }
        return keys;
    }

    public static List<float[]> ToRaw(List<CurveKey> keys) =>
        keys.Select(k => k.Mode == TangentMode.Auto ? new[] { k.Time, k.Value } : new[] { k.Time, k.Value, k.In, k.Out })
            .ToList();

    /// <summary>Recomputes the tangents of every key whose mode derives them.</summary>
    public static void Update(List<CurveKey> keys)
    {
        for (int i = 0; i < keys.Count; i++)
        {
            var key = keys[i];
            if (key.Mode is TangentMode.Free)
                continue;
            Tangents(keys, i, key.Mode, out key.In, out key.Out);
            keys[i] = key;
        }
    }

    /// <summary>The curve KSP would build from <paramref name="keys"/>, keys in list order.</summary>
    public static FloatCurve ToFloatCurve(List<CurveKey> keys)
    {
        var curve = new FloatCurve();
        foreach (var k in keys)
        {
            if (k.Mode == TangentMode.Auto)
                curve.Add(k.Time, k.Value);
            else
                curve.Add(k.Time, k.Value, k.In, k.Out);
        }
        return curve;
    }

    static void Tangents(List<CurveKey> keys, int i, TangentMode mode, out float tin, out float tout)
    {
        tin = tout = 0f;
        if (mode is TangentMode.Flat or TangentMode.Auto)
            return;

        float? before = i > 0 ? Slope(keys[i - 1], keys[i]) : null;
        float? after = i + 1 < keys.Count ? Slope(keys[i], keys[i + 1]) : null;
        if (mode == TangentMode.Linear)
        {
            tin = before ?? after ?? 0f;
            tout = after ?? before ?? 0f;
        }
        else if (before.HasValue && after.HasValue)
            tin = tout = Slope(keys[i - 1], keys[i + 1]);
        else
            tin = tout = before ?? after ?? 0f;
    }

    static float Slope(CurveKey a, CurveKey b) =>
        Mathf.Approximately(a.Time, b.Time) ? 0f : (b.Value - a.Value) / (b.Time - a.Time);

    // Tangents this class writes read back exactly, so only rounding needs allowing for. Looser
    // matching would claim hand-tuned tangents that are merely close and rewrite them.
    static bool Near(float a, float b) => Math.Abs(a - b) <= 1e-5f * Math.Max(Math.Abs(a), Math.Abs(b)) + 1e-9f;
}
