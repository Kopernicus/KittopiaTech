using System;
using System.Collections.Generic;
using System.Linq;
using Kopernicus.Configuration.MaterialLoader;
using Kopernicus.Configuration.MaterialLoader.Parsing;
using UnityEngine;

namespace KittopiaTech.Model;

public readonly struct GradientStop(float time, Color color) : IEquatable<GradientStop>
{
    public float Time { get; } = time;
    public Color Color { get; } = color;

    public bool Equals(GradientStop other) => Time == other.Time && Color == other.Color;

    public override bool Equals(object obj) => obj is GradientStop other && Equals(other);

    public override int GetHashCode() => Time.GetHashCode() ^ Color.GetHashCode();
}

/// <summary>A gradient's stops, sorted by time with no two at the same time.</summary>
public sealed class GradientRamp : IEquatable<GradientRamp>
{
    public IReadOnlyList<GradientStop> Stops { get; }

    public GradientRamp(IEnumerable<GradientStop> stops)
    {
        // Kopernicus keeps stops in a SortedList, which rejects repeated times.
        var sorted = stops.OrderBy(s => s.Time).ToList();
        for (int i = 1; i < sorted.Count; i++)
        {
            if (sorted[i].Time <= sorted[i - 1].Time)
                sorted[i] = new GradientStop(sorted[i - 1].Time + 1e-4f, sorted[i].Color);
        }
        Stops = sorted;
    }

    /// <summary>The colour at <paramref name="time"/>, as Kopernicus bakes it: held past the ends.</summary>
    public Color Evaluate(float time)
    {
        if (Stops.Count == 0)
            return Color.black;
        if (time <= Stops[0].Time)
            return Stops[0].Color;
        for (int i = 1; i < Stops.Count; i++)
        {
            if (time <= Stops[i].Time)
            {
                var (a, b) = (Stops[i - 1], Stops[i]);
                return Color.Lerp(a.Color, b.Color, (time - a.Time) / (b.Time - a.Time));
            }
        }
        return Stops[Stops.Count - 1].Color;
    }

    public bool Equals(GradientRamp other) => other != null && Stops.SequenceEqual(other.Stops);

    public override bool Equals(object obj) => Equals(obj as GradientRamp);

    public override int GetHashCode() => Stops.Count;
}

/// <summary>
/// A ramp texture that Kopernicus can also bake from a gradient node, such as a rim or fog
/// colour ramp. The gradient member is write-only, so the stops are read back from the
/// texture: exactly for ramps baked here, and fitted to its pixels for any other.
/// </summary>
public sealed class GradientSlot(MaterialLoader loader, MemberSchema texture, MemberSchema gradient, string property)
    : ISlot
{
    const int Samples = 256;

    /// <summary>Stops by texture instance, for ramps baked here and textures already fitted.</summary>
    static readonly Dictionary<int, GradientRamp> Known = [];

    /// <summary>Textures baked from stops edited here, which export as a gradient node.</summary>
    static readonly HashSet<int> Baked = [];

    public MaterialLoader Loader { get; } = loader;
    public MemberSchema Gradient { get; } = gradient;
    public string Property { get; } = property;

    public object Owner => Loader;
    public MemberSchema Member { get; } = texture;
    public Type DeclaredType => typeof(GradientRamp);
    public string Label => Member.Label;
    public bool CanWrite => Loader.Value != null;

    /// <summary>
    /// The gradient behind <paramref name="texture"/>, a ramp texture member: a write-only
    /// <c>Gradient</c> member named after it, e.g. RimColorRamp and RimColorRampGradient.
    /// </summary>
    public static GradientSlot For(object owner, MemberSchema texture)
    {
        if (owner is not MaterialLoader { Value: { } material } loader || texture.DeclaredType != typeof(MaterialTextureParser))
            return null;
        var gradient = texture.Owner.Members.FirstOrDefault(m =>
            m.DeclaredType == typeof(Kopernicus.Configuration.Parsing.Gradient) && m.Name == texture.Name + "Gradient");
        string property = "_" + texture.FieldName;
        return gradient != null && gradient.CanWrite && material.HasProperty(property)
            ? new GradientSlot(loader, texture, gradient, property)
            : null;
    }

    /// <summary>
    /// Whether the texture is a ramp with no file behind it, baked here or by Kopernicus from a
    /// config, so it can only be exported as a gradient.
    /// </summary>
    public bool IsBakedRamp
    {
        get
        {
            var current = Loader.GetTexture(Property);
            return current != null && (Baked.Contains(current.GetInstanceID()) || string.IsNullOrEmpty(current.name));
        }
    }

    public object Read()
    {
        var current = Loader.GetTexture(Property);
        if (current == null)
            return null;
        int id = current.GetInstanceID();
        if (!Known.TryGetValue(id, out var ramp))
            Known[id] = ramp = Fit(Sample(current));
        return ramp;
    }

    public void Write(object raw)
    {
        // Otherwise the texture's path would still be exported, and reloaded on demand.
        Loader.Entries.Remove(Property);
        if (raw is not GradientRamp ramp || ramp.Stops.Count == 0)
        {
            Loader.Value.SetTexture(Property, null);
            return;
        }

        var gradient = new Kopernicus.Configuration.Parsing.Gradient();
        foreach (var stop in ramp.Stops)
            gradient.Add(stop.Time, stop.Color);
        Gradient.SetValue(Loader, gradient);

        var baked = Loader.GetTexture(Property);
        if (baked != null)
        {
            Known[baked.GetInstanceID()] = ramp;
            Baked.Add(baked.GetInstanceID());
        }
    }

    /// <summary>The texture's middle row, through the GPU since most textures aren't readable.</summary>
    static Color[] Sample(Texture texture)
    {
        var target = RenderTexture.GetTemporary(Samples, 1, 0, RenderTextureFormat.ARGB32);
        var active = RenderTexture.active;
        var read = new Texture2D(Samples, 1, TextureFormat.RGBA32, false);
        try
        {
            Graphics.Blit(texture, target);
            RenderTexture.active = target;
            read.ReadPixels(new Rect(0, 0, Samples, 1), 0, 0, false);
            return read.GetPixels();
        }
        finally
        {
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.Destroy(read);
        }
    }

    /// <summary>
    /// Few enough stops to edit by hand: the tightest fit, starting at about one colour step,
    /// with at most <see cref="MaxFitted"/> stops.
    /// </summary>
    static GradientRamp Fit(Color[] samples)
    {
        GradientRamp ramp = null;
        for (float tolerance = 1.5f / 255f; ramp == null || ramp.Stops.Count > MaxFitted && tolerance < 0.5f; tolerance *= 2f)
            ramp = Fit(samples, tolerance);
        return ramp;
    }

    const int MaxFitted = 12;

    /// <summary>
    /// The fewest stops whose straight lines stay within <paramref name="tolerance"/> of every
    /// sample (Ramer–Douglas–Peucker), so a baked ramp reads back as roughly the stops it came from.
    /// </summary>
    static GradientRamp Fit(Color[] samples, float tolerance)
    {
        int last = samples.Length - 1;
        var keep = new SortedSet<int> { 0, last };

        void Split(int from, int to)
        {
            int worst = -1;
            float worstError = tolerance;
            for (int i = from + 1; i < to; i++)
            {
                var line = Color.Lerp(samples[from], samples[to], (float)(i - from) / (to - from));
                var d = samples[i] - line;
                float error = Mathf.Max(Mathf.Max(Mathf.Abs(d.r), Mathf.Abs(d.g)), Mathf.Max(Mathf.Abs(d.b), Mathf.Abs(d.a)));
                if (error > worstError)
                {
                    worst = i;
                    worstError = error;
                }
            }
            if (worst < 0)
                return;
            keep.Add(worst);
            Split(from, worst);
            Split(worst, to);
        }

        Split(0, last);
        return new GradientRamp(keep.Select(i => new GradientStop((float)i / last, samples[i])));
    }
}
