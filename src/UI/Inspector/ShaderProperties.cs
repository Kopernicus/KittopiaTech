using System;
using KittopiaTech.Model;
using KittopiaTech.UI.Editors;
using Kopernicus.Configuration.MaterialLoader;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace KittopiaTech.UI.Inspector;

/// <summary>
/// A foldout under each material loader listing every property of its shader, including those
/// only reachable through raw <c>_X</c> keys (all of them, for custom shaders). Rows are built
/// when first opened, and rebuilt if the shader changed since.
/// </summary>
public sealed class ShaderProperties : MonoBehaviour
{
    MaterialLoader loader;
    IEditScope scope;
    TextMeshProUGUI headerText;
    Transform content;
    Shader built;
    bool expanded;

    public static void Build(Transform parent, IEditScope scope, object owner)
    {
        if (owner is not MaterialLoader loader)
            return;

        var go = new GameObject("ShaderProperties", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var section = go.AddComponent<ShaderProperties>();
        section.loader = loader;
        section.scope = scope;
        NodeFieldEditor.CreateFoldout(go.transform, out var header, out section.headerText, out section.content);
        header.onClick.AddListener(section.ToggleExpanded);
        section.Refresh();
    }

    void ToggleExpanded()
    {
        expanded = !expanded;
        Refresh();
    }

    void Refresh()
    {
        var shader = loader.Value != null ? loader.Value.shader : null;
        string pending = NodeFieldEditor.PendingLabel("Shader properties", scope.HasPendingWithin(loader));
        headerText.text =
            $"{(expanded ? "-" : "+")} <b>{pending}</b>  <color=#a3a7ab>{(shader != null ? shader.name : "no material")}</color>";

        if (shader != built)
        {
            InspectorBuilder.Clear(content);
            built = null;
        }
        if (expanded && built == null && shader != null)
        {
            BuildRows(shader);
            built = shader;
        }
        content.gameObject.SetActive(expanded);
    }

    void BuildRows(Shader shader)
    {
        for (int i = 0; i < shader.GetPropertyCount(); i++)
        {
            var type = shader.GetPropertyType(i);
            var slot = new ShaderPropertySlot(loader, shader.GetPropertyName(i), type);
            var resolution = FieldEditorRegistry.Resolve(null, slot.DeclaredType, MemberShape.Value);
            try
            {
                var row = FieldRow.Create(content, scope.For(slot, resolution.Adapter), resolution);
                Tooltip.Attach(row.transform.GetChild(0).gameObject, Describe(shader, i, type));
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[KittopiaTech] Failed to build a row for {slot.Name}: {e}");
            }
        }
    }

    /// <summary>The shader's own description of the property, and its limits for ranges.</summary>
    static string Describe(Shader shader, int index, ShaderPropertyType type)
    {
        string text = shader.GetPropertyDescription(index);
        if (type == ShaderPropertyType.Range)
        {
            var limits = shader.GetPropertyRangeLimits(index);
            text += $"\nRange {limits.x} to {limits.y}";
        }
        else if (type == ShaderPropertyType.Texture)
            text += $"\n{shader.GetPropertyTextureDimension(index)} texture";
        return text;
    }
}
