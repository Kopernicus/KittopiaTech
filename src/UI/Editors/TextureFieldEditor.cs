using System;
using KittopiaTech.Model;
using KittopiaTech.UI.Debug;
using Kopernicus.Configuration.MaterialLoader.Parsing;
using Kopernicus.Configuration.Parsing;
using Kopernicus.ConfigParser.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Editors;

/// <summary>
/// Texture paths: thumbnail, path and size inline, like the MapSO rows. Clicking the path
/// opens the texture picker and clicking the thumbnail opens a larger preview. Material
/// textures are only paths, so their thumbnail shows only textures already in memory.
/// </summary>
[FieldEditor(typeof(Texture2DParser))]
[FieldEditor(typeof(MaterialTextureParser))]
public sealed class TextureFieldEditor : FieldEditor<object>
{
    [SerializeField]
    RawImage thumbnail;

    [SerializeField]
    Button thumbnailButton;

    [SerializeField]
    Button path;

    [SerializeField]
    TextMeshProUGUI pathText;

    [SerializeField]
    TextMeshProUGUI info;

    Popout popout;
    Popout picker;
    RawImage preview;
    AspectRatioFitter previewAspect;
    TextMeshProUGUI previewInfo;
    readonly TexturePreview loaded = new();

    protected internal override void BuildPrefab(PrefabContext ctx)
    {
        EditorLayout.Row(transform);
        var image = new GameObject("Thumbnail", typeof(RectTransform));
        image.transform.SetParent(transform, false);
        thumbnail = image.AddComponent<RawImage>();
        var layout = image.AddComponent<LayoutElement>();
        layout.minWidth = layout.preferredWidth = EditorLayout.RowHeight;
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
        loaded.Dispose();
    }

    void Browse()
    {
        if (picker == null && !Source.IsReadOnly)
            picker = AssetPicker.Open(this, FieldPath, AssetKind.Texture, pathText.text, SetPath);
    }

    void SetPath(string text)
    {
        if (text == ((IWritable)Source.Get())?.ValueToString())
            return;
        try
        {
            var parser = (IParsable)Activator.CreateInstance(Source.ValueType);
            using (ParseStateScope.For(Source.Scope.Context))
                parser.SetFromString(text);
            EditorLayout.MarkInvalid(path, false);
            Commit(parser);
        }
        catch (Exception e)
        {
            EditorLayout.MarkInvalid(path, true);
            UnityEngine.Debug.LogError($"[KittopiaTech] Failed to load {text}: {e.Message}");
        }
    }

    public override void Refresh()
    {
        var value = Source.Get();
        pathText.text = ((IWritable)value)?.ValueToString() ?? "";
        path.interactable = !Source.IsReadOnly;

        // Texture2DParser holds the loaded texture; a material texture is only its path.
        Texture2D texture = value is Texture2DParser parser ? parser.Value : null;
        if (value is MaterialTextureParser && pathText.text != "")
            texture = TexturePreview.Find(pathText.text, out _);

        thumbnail.texture = texture;
        thumbnail.color = texture != null ? Color.white : Color.clear;
        thumbnailButton.interactable = pathText.text != "";
        info.text = texture != null ? $"{texture.width}x{texture.height} {texture.format}"
            : pathText.text == "" ? "none"
            : "not loaded";

        if (popout != null)
            ShowPreview();
    }

    void OpenPreview()
    {
        if (popout != null)
            return;
        popout = Popout.Open(this, pathText.text, new Vector2(540f, 330f));
        popout.Closed += loaded.Dispose;
        var box = new GameObject("Preview", typeof(RectTransform));
        box.transform.SetParent(popout.Content, false);
        var layout = box.AddComponent<LayoutElement>();
        layout.minHeight = layout.preferredHeight = 256f;

        var image = new GameObject("Image", typeof(RectTransform));
        image.transform.SetParent(box.transform, false);
        preview = image.AddComponent<RawImage>();
        previewAspect = image.AddComponent<AspectRatioFitter>();
        previewAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

        previewInfo = DebugSkin.CreateLabel(popout.Content, "");
        previewInfo.enableWordWrapping = false;
        previewInfo.overflowMode = TextOverflowModes.Ellipsis;
        ShowPreview();
    }

    // Loads the path itself, so it previews files the thumbnail can't show without loading.
    void ShowPreview()
    {
        loaded.Load(pathText.text, AssetKind.Texture);
        var texture = loaded.Texture;
        preview.texture = texture;
        preview.color = texture != null ? Color.white : Color.clear;
        if (texture != null)
            previewAspect.aspectRatio = (float)texture.width / texture.height;
        previewInfo.text = loaded.Info;
    }
}
