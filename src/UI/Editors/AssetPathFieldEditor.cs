using System;
using KittopiaTech.Model;
using KittopiaTech.UI.Debug;
using Kopernicus.Configuration;
using Kopernicus.Configuration.Parsing;
using Kopernicus.ConfigParser.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Editors;

/// <summary>
/// Values loaded from a file: meshes, models and other assets. Clicking the path opens a
/// searchable GameData picker. Textures have their own <see cref="TextureFieldEditor"/>.
/// </summary>
[FieldEditor(typeof(MeshParser))]
[FieldEditor(typeof(MuParser))]
[FieldEditor(typeof(AssetParser<>))]
public sealed class AssetPathFieldEditor : FieldEditor<object>
{
    [SerializeField]
    Button path;

    [SerializeField]
    TextMeshProUGUI pathText;

    Popout picker;

    protected internal override void BuildPrefab(PrefabContext ctx)
    {
        EditorLayout.Row(transform);
        path = EditorLayout.PathButton(transform, out pathText);
    }

    protected override void Awake()
    {
        path.onClick.AddListener(Browse);
    }

    protected override void OnUnbind() => picker?.Close();

    void Browse()
    {
        if (picker != null || Source.IsReadOnly)
            return;
        string title = $"Pick: {Source.Label}";
        string[] extensions = Source.ValueType == typeof(MuParser) ? [".mu"] : [".obj", ".bin", ".mu", ".unity3d"];
        picker = AssetPicker.Open(this, title, extensions, pathText.text, SetPath);
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
        pathText.text = ((IWritable)Source.Get())?.ValueToString() ?? "";
        path.interactable = !Source.IsReadOnly;
    }
}
