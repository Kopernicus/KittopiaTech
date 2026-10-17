using System;
using KittopiaTech.Model;
using KittopiaTech.UI.Debug;
using Kopernicus.ConfigParser.Interfaces;
using Kopernicus.Configuration.Parsing;
using Kopernicus.OnDemand;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Editors;

/// <summary>
/// All four MapSO parsers: thumbnail, path and size inline, and a larger preview in a pop-out.
/// Previews are only drawn for maps that are already loaded, so opening the inspector never
/// pulls an on-demand map into memory.
/// </summary>
[FieldEditor(typeof(MapSOParserBase<>))]
public sealed class MapSOFieldEditor : FieldEditor<object>
{
    [SerializeField]
    RawImage thumbnail;

    [SerializeField]
    Button path;

    [SerializeField]
    TextMeshProUGUI pathText;

    [SerializeField]
    TextMeshProUGUI info;

    [SerializeField]
    Button thumbnailButton;

    Texture2D thumbTexture;
    Texture2D previewTexture;
    Popout popout;
    Popout picker;
    RawImage preview;

    protected internal override void BuildPrefab(PrefabContext ctx)
    {
        EditorLayout.Row(transform);
        var image = new GameObject("Thumbnail", typeof(RectTransform));
        image.transform.SetParent(transform, false);
        thumbnail = image.AddComponent<RawImage>();
        var layout = image.AddComponent<LayoutElement>();
        layout.minWidth = layout.preferredWidth = 44f;
        layout.minHeight = layout.preferredHeight = EditorLayout.RowHeight;
        thumbnailButton = image.AddComponent<Button>();
        thumbnailButton.targetGraphic = thumbnail;

        path = EditorLayout.PathButton(transform, out pathText);

        info = DebugSkin.CreateLabel(transform, "");
        info.enableWordWrapping = false;
        info.color = new Color(0.64f, 0.65f, 0.67f);
        var infoLayout = info.transform.parent.GetComponent<LayoutElement>();
        infoLayout.flexibleWidth = 0f;
        infoLayout.preferredWidth = 150f;
    }

    protected override void Awake()
    {
        path.onClick.AddListener(Browse);
        thumbnailButton.onClick.AddListener(OpenPreview);
    }

    protected override void OnUnbind()
    {
        popout?.Close();
        picker?.Close();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Destroy(thumbTexture);
        Destroy(previewTexture);
    }

    void Browse()
    {
        if (picker == null && !Source.IsReadOnly)
            picker = AssetPicker.Open(this, $"Pick: {Source.Label}", AssetKind.Map, pathText.text, OnPath);
    }

    void OnPath(string text)
    {
        if (text == ((IWritable)Source.Get())?.ValueToString())
            return;
        try
        {
            // The new parser loads the map now, so a bad path shows up on the row immediately.
            var parser = (IParsable)Activator.CreateInstance(Source.ValueType);
            using (ParseStateScope.For(Source.Scope.Context))
                parser.SetFromString(text);
            EditorLayout.MarkInvalid(path, false);
            Commit(parser);
        }
        catch (Exception e)
        {
            EditorLayout.MarkInvalid(path, true);
            UnityEngine.Debug.LogError($"[KittopiaTech] Failed to load map {text}: {e.Message}");
        }
    }

    public override void Refresh()
    {
        var map = MapOf(Source.Get());
        pathText.text = ((IWritable)Source.Get())?.ValueToString() ?? "";
        path.interactable = !Source.IsReadOnly;

        bool loaded = map != null && (map is not MapSODemand demand || demand.IsLoaded);
        info.text = map == null ? "none" : $"{map.Width}x{map.Height} {map.Depth}{(loaded ? "" : ", not loaded")}";
        thumbTexture = loaded ? Render(thumbTexture, map, 88, 44) : null;
        thumbnail.texture = thumbTexture;
        thumbnail.color = loaded ? Color.white : Color.clear;
        thumbnailButton.interactable = loaded;

        if (popout != null && loaded)
        {
            previewTexture = Render(previewTexture, map, 512, 256);
            preview.texture = previewTexture;
        }
    }

    void OpenPreview()
    {
        if (popout != null)
            return;
        popout = Popout.Open(this, $"Map: {Source.Label}", new Vector2(540f, 330f));
        var image = new GameObject("Preview", typeof(RectTransform));
        image.transform.SetParent(popout.Content, false);
        preview = image.AddComponent<RawImage>();
        var layout = image.AddComponent<LayoutElement>();
        layout.minHeight = layout.preferredHeight = 256f;
        DebugSkin.CreateLabel(popout.Content, pathText.text);
        Refresh();
    }

    static MapSO MapOf(object parser) =>
        parser?.GetType().GetProperty("Value")?.GetValue(parser, null) as MapSO;

    /// <summary>Samples the map into a texture; greyscale maps are shown as grey.</summary>
    internal static Texture2D Render(Texture2D texture, MapSO map, int width, int height)
    {
        if (texture == null || texture.width != width || texture.height != height)
        {
            Destroy(texture);
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        }

        var pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var c = map.GetPixelColor((x + 0.5f) / width, (y + 0.5f) / height);
                c.a = 1f;
                pixels[y * width + x] = c;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}
