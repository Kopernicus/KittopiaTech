using System;
using System.IO;
using System.Linq;
using KSPTextureLoader;
using UnityEngine;

namespace KittopiaTech.UI;

/// <summary>
/// Loads a texture or map path for display. Built-in and GameDatabase textures are already in
/// memory; anything else is loaded through KSPTextureLoader, as Kopernicus would load it, and
/// held until the preview is disposed or loads something else. Thumbnails and the picker pass
/// a sample width instead, so files are read on the CPU and only a small copy is uploaded.
/// </summary>
internal sealed class TexturePreview : IDisposable
{
    const string Builtin = "BUILTIN/";

    TextureHandle<Texture2D> handle;
    Texture2D owned;

    public Texture2D Texture { get; private set; }

    /// <summary>The path last loaded, so callers can skip reloading it.</summary>
    public string LoadedPath { get; private set; }

    /// <summary>Size and format of the file, e.g. "2048x1024 DXT5", even when sampled down.</summary>
    public string Size { get; private set; } = "";

    /// <summary>Size, format and where the texture came from, or why it failed to load.</summary>
    public string Info { get; private set; } = "";

    /// <param name="sampleWidth">If above 0, files not already in memory are sampled down to this width.</param>
    public void Load(string path, AssetKind kind, int sampleWidth = 0)
    {
        Dispose();
        LoadedPath = path;
        string source;
        try
        {
            Texture = LoadTexture(path, kind, sampleWidth, out source);
        }
        catch (Exception e)
        {
            Texture = null;
            source = $"failed to load: {e.Message}";
        }
        if (Texture != null && Size == "")
            Size = $"{Texture.width}x{Texture.height} {Texture.format}";
        Info = Texture != null ? $"{Size}, {source}" : source;
    }

    public void Dispose()
    {
        handle?.Dispose();
        handle = null;
        UnityEngine.Object.Destroy(owned);
        owned = null;
        Texture = null;
        LoadedPath = null;
        Size = Info = "";
    }

    /// <summary>A texture already in memory under <paramref name="path"/>, without loading anything.</summary>
    internal static Texture2D Find(string path, out string source)
    {
        if (path.StartsWith(Builtin))
        {
            string name = path.Substring(Builtin.Length);
            source = "built in";
            return Resources.FindObjectsOfTypeAll<Texture2D>().FirstOrDefault(t => t.name == name);
        }
        source = "GameDatabase";
        return GameDatabase.Instance.GetTextureInfo(Path.ChangeExtension(path, null))?.texture;
    }

    Texture2D LoadTexture(string path, AssetKind kind, int sampleWidth, out string source)
    {
        if (kind == AssetKind.Map && path.StartsWith(Builtin))
        {
            string name = path.Substring(Builtin.Length);
            source = "built in";
            var map = Resources.FindObjectsOfTypeAll<MapSO>().FirstOrDefault(m => m.name == name);
            if (map == null)
                return null;
            Size = $"{map.Width}x{map.Height} {map.Depth}";
            int width = sampleWidth > 0 ? sampleWidth : 512;
            return owned = Editors.MapSOFieldEditor.Render(null, map, width, width / 2);
        }

        var found = Find(path, out source);
        if (found != null || path.StartsWith(Builtin))
            return found;
        if (sampleWidth > 0)
            return owned = Sample(path, sampleWidth, out source);

        handle = TextureLoader.LoadTexture<Texture2D>(
            path,
            new TextureLoadOptions { Hint = TextureLoadHint.Synchronous }
        );
        var texture = handle.GetTexture();
        source = handle.AssetBundle != null ? $"bundle {handle.AssetBundle}" : "loaded from disk";
        if (!IsSingleChannel(texture.format))
            return texture;

        handle.Dispose();
        handle = null;
        return owned = Sample(path, 512, out source);
    }

    // Unity samples these as red (or alpha), but maps read them as grey.
    static bool IsSingleChannel(TextureFormat format) =>
        format
            is TextureFormat.R8
                or TextureFormat.R16
                or TextureFormat.RHalf
                or TextureFormat.RFloat
                or TextureFormat.BC4
                or TextureFormat.Alpha8;

    /// <summary>
    /// Reads the file on the CPU and samples its smallest mip at least <paramref name="width"/>
    /// wide, so large textures stay cheap. Single-channel formats come out grey.
    /// </summary>
    Texture2D Sample(string path, int width, out string source)
    {
        using var handle = TextureLoader.LoadCPUTexture(
            path,
            new TextureLoadOptions { Hint = TextureLoadHint.Synchronous }
        );
        var cpu = handle.GetTexture();
        Size = $"{cpu.Width}x{cpu.Height} {cpu.Format}";
        source = handle.AssetBundle != null ? $"bundle {handle.AssetBundle}" : "from disk";

        int mip = 0;
        while (mip + 1 < cpu.MipCount && cpu.Width >> (mip + 1) >= width)
            mip++;
        bool grey = IsSingleChannel(cpu.Format);
        bool alpha = cpu.Format == TextureFormat.Alpha8;

        int w = Mathf.Min(width, cpu.Width);
        int h = Mathf.Max(1, Mathf.RoundToInt((float)w * cpu.Height / cpu.Width));
        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var c = cpu.GetPixelBilinear((x + 0.5f) / w, (y + 0.5f) / h, mip);
                if (grey)
                {
                    float v = alpha ? c.a : c.r;
                    c = new Color(v, v, v, 1f);
                }
                pixels[y * w + x] = c;
            }
        }
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}
