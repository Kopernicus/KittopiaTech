using System;
using KittopiaTech.Model;
using KittopiaTech.UI.Debug;
using Kopernicus.Configuration;
using Kopernicus.Configuration.MaterialLoader.Parsing;
using Kopernicus.Configuration.Parsing;
using Kopernicus.ConfigParser.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Editors;

/// <summary>
/// Values loaded from a file: textures, meshes, models and other assets. A path field with a
/// searchable GameData picker, and a thumbnail for textures that are already loaded.
/// </summary>
[FieldEditor(typeof(Texture2DParser))]
[FieldEditor(typeof(MaterialTextureParser))]
[FieldEditor(typeof(MeshParser))]
[FieldEditor(typeof(MuParser))]
[FieldEditor(typeof(AssetParser<>))]
public sealed class AssetPathFieldEditor : FieldEditor<object>
{
    [SerializeField]
    RawImage thumbnail;

    [SerializeField]
    TMP_InputField path;

    [SerializeField]
    Button browseButton;

    protected internal override void BuildPrefab(PrefabContext ctx)
    {
        EditorLayout.Row(transform);
        var image = new GameObject("Thumbnail", typeof(RectTransform));
        image.transform.SetParent(transform, false);
        thumbnail = image.AddComponent<RawImage>();
        var layout = image.AddComponent<LayoutElement>();
        layout.minWidth = layout.preferredWidth = EditorLayout.RowHeight;
        layout.minHeight = layout.preferredHeight = EditorLayout.RowHeight;

        path = DebugSkin.CreateInputField(transform);
        EditorLayout.Compact(path.gameObject);

        browseButton = DebugSkin.CreateButton(transform, "...");
        browseButton.name = "Browse";
        EditorLayout.Compact(browseButton.gameObject, 0f);
        browseButton.GetComponent<LayoutElement>().preferredWidth = 36f;
    }

    protected override void Awake()
    {
        path.onEndEdit.AddListener(SetPath);
        browseButton.onClick.AddListener(Browse);
    }

    bool IsTexture =>
        Source.ValueType == typeof(Texture2DParser) || Source.ValueType == typeof(MaterialTextureParser);

    void Browse()
    {
        var extensions = IsTexture
            ? AssetPicker.TextureExtensions
            : Source.ValueType == typeof(MuParser) ? [".mu"] : [".obj", ".bin", ".mu", ".unity3d"];
        AssetPicker.Open(this, $"Pick: {Source.Label}", extensions, IsTexture, SetPath);
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
        path.text = ((IWritable)value)?.ValueToString() ?? "";
        path.interactable = browseButton.interactable = !Source.IsReadOnly;

        var texture = (value as Texture2DParser)?.Value;
        thumbnail.texture = texture;
        thumbnail.color = texture != null ? Color.white : Color.clear;
    }
}
