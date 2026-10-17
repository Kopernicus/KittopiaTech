using System;
using System.IO;
using System.Linq;
using KSPTextureLoader;
using UnityEngine;

namespace KittopiaTech.UI;

/// <summary>
/// Loads a texture or map path for display. Built-in and GameDatabase textures are already in
/// memory; anything else is loaded through KSPTextureLoader, as Kopernicus would load it, and
/// held until the preview is disposed or loads something else.
/// </summary>
internal sealed class TexturePreview : IDisposable
{
    const string Builtin = "BUILTIN/";

    TextureHandle<Texture2D> handle;
    Texture2D owned;

    public Texture2D Texture { get; private set; }

    /// <summary>Size, format and where the texture came from, or why it failed to load.</summary>
    public string Info { get; private set; } = "";

    public void Load(string path, AssetKind kind)
    {
        Dispose();
        string source;
        try
        {
            Texture = LoadTexture(path, kind, out source);
        }
        catch (Exception e)
        {
            Texture = null;
            source = $"failed to load: {e.Message}";
        }
        Info = Texture != null && Texture != owned ? Describe(Texture, source) : source;
    }

    public void Dispose()
    {
        handle?.Dispose();
        handle = null;
        UnityEngine.Object.Destroy(owned);
        owned = null;
        Texture = null;
        Info = "";
    }

    internal static string Describe(Texture texture, string source) =>
        $"{texture.width}x{texture.height} {(texture as Texture2D)?.format}, {source}";

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

    Texture2D LoadTexture(string path, AssetKind kind, out string source)
    {
        if (kind == AssetKind.Map && path.StartsWith(Builtin))
        {
            string name = path.Substring(Builtin.Length);
            source = "built in";
            var map = Resources.FindObjectsOfTypeAll<MapSO>().FirstOrDefault(m => m.name == name);
            if (map == null)
                return null;
            source = $"{map.Width}x{map.Height} {map.Depth}, built in";
            return owned = Editors.MapSOFieldEditor.Render(null, map, 512, 256);
        }

        var found = Find(path, out source);
        if (found != null || path.StartsWith(Builtin))
            return found;

        handle = TextureLoader.LoadTexture<Texture2D>(
            path,
            new TextureLoadOptions { Hint = TextureLoadHint.Synchronous }
        );
        var texture = handle.GetTexture();
        source = handle.AssetBundle != null ? $"bundle {handle.AssetBundle}" : "loaded from disk";
        if (!IsSingleChannel(texture.format))
            return texture;

        source = Describe(texture, source);
        handle.Dispose();
        handle = null;
        return owned = GreyPreview(path, texture.width, texture.height);
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

    static Texture2D GreyPreview(string path, int width, int height)
    {
        using var handle = TextureLoader.LoadCPUTexture(
            path,
            new TextureLoadOptions { Hint = TextureLoadHint.Synchronous }
        );
        var cpu = handle.GetTexture();
        bool alpha = cpu.Format == TextureFormat.Alpha8;

        int w = 512, h = Mathf.Max(1, Mathf.RoundToInt(512f * height / width));
        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var c = cpu.GetPixelBilinear((x + 0.5f) / w, (y + 0.5f) / h);
                float v = alpha ? c.a : c.r;
                pixels[y * w + x] = new Color(v, v, v, 1f);
            }
        }
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}
