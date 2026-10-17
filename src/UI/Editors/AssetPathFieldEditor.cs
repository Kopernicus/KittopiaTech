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
/// Values loaded from a file: textures, meshes, models and other assets. Clicking the path
/// opens a searchable GameData picker, and a thumbnail for textures that are already loaded.
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
    Button path;

    [SerializeField]
    TextMeshProUGUI pathText;

    Popout picker;

    protected internal override void BuildPrefab(PrefabContext ctx)
    {
        EditorLayout.Row(transform);
        var image = new GameObject("Thumbnail", typeof(RectTransform));
        image.transform.SetParent(transform, false);
        thumbnail = image.AddComponent<RawImage>();
        var layout = image.AddComponent<LayoutElement>();
        layout.minWidth = layout.preferredWidth = EditorLayout.RowHeight;
        layout.minHeight = layout.preferredHeight = EditorLayout.RowHeight;

        path = EditorLayout.PathButton(transform, out pathText);
    }

    protected override void Awake()
    {
        path.onClick.AddListener(Browse);
    }

    protected override void OnUnbind() => picker?.Close();

    bool IsTexture =>
        Source.ValueType == typeof(Texture2DParser) || Source.ValueType == typeof(MaterialTextureParser);

    void Browse()
    {
        if (picker != null || Source.IsReadOnly)
            return;
        string title = $"Pick: {Source.Label}";
        string[] extensions = Source.ValueType == typeof(MuParser) ? [".mu"] : [".obj", ".bin", ".mu", ".unity3d"];
        picker = IsTexture
            ? AssetPicker.Open(this, title, AssetKind.Texture, pathText.text, SetPath)
            : AssetPicker.Open(this, title, extensions, pathText.text, SetPath);
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

        var texture = (value as Texture2DParser)?.Value;
        thumbnail.texture = texture;
        thumbnail.color = texture != null ? Color.white : Color.clear;
    }
}
