using System;
using System.Linq;
using Kopernicus.Configuration.Parsing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Editors;

/// <summary>A material's shader, picked from the shaders loaded in game.</summary>
[FieldEditor(typeof(ShaderParser))]
public sealed class ShaderFieldEditor : FieldEditor<ShaderParser>
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

    protected override void Awake() => path.onClick.AddListener(Browse);

    protected override void OnUnbind() => picker?.Close();

    void Browse()
    {
        if (picker != null || Source.IsReadOnly)
            return;

        var names = Resources
            .FindObjectsOfTypeAll<Shader>()
            .Select(s => s.name)
            .Where(n => !string.IsNullOrEmpty(n) && !n.StartsWith("Hidden/"))
            .Distinct()
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase);
        picker = AssetPicker.OpenList(this, FieldPath, [.. names], Value?.Value?.name, Pick);
    }

    void Pick(string name)
    {
        if (name == Value?.Value?.name)
            return;
        // Found directly rather than through the parser's Shader.Find, which misses bundle shaders.
        var shader = Resources.FindObjectsOfTypeAll<Shader>().FirstOrDefault(s => s.name == name) ?? Shader.Find(name);
        EditorLayout.MarkInvalid(path, shader == null);
        if (shader != null)
            Commit(new ShaderParser { Value = shader });
    }

    public override void Refresh()
    {
        pathText.text = Value?.Value?.name ?? "";
        path.interactable = !Source.IsReadOnly;
        EditorLayout.MarkInvalid(path, Source.Error != null);
    }
}
