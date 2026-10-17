using System.Globalization;
using System.Linq;
using KittopiaTech.Model;
using Kopernicus.Configuration.MaterialLoader;
using UnityEngine;
using UnityEngine.Rendering;

namespace KittopiaTech.Export;

/// <summary>
/// Custom materials have no named properties, and Kopernicus builds them from shader defaults,
/// so every shader property is written as a raw <c>_X</c> key, along with the keywords.
/// </summary>
[ConfigWriter(typeof(CustomMaterialLoader))]
public sealed class CustomMaterialWriter : IConfigWriter
{
    public void Write(ConfigNode parent, MemberSchema member, object value)
    {
        var loader = (CustomMaterialLoader)value;
        var node = parent.AddNode(member.FieldName);
        ConfigExporter.WriteObject(node, loader);

        var material = loader.Value;
        if (material == null)
            return;

        var shader = material.shader;
        for (int i = 0; i < shader.GetPropertyCount(); i++)
        {
            string name = shader.GetPropertyName(i);
            string text = shader.GetPropertyType(i) switch
            {
                ShaderPropertyType.Color => Join(loader.GetColor(name)),
                ShaderPropertyType.Vector => Join(loader.GetVector(name)),
                ShaderPropertyType.Texture => loader.GetTextureName(name),
                _ => F(loader.GetFloat(name)),
            };
            if (string.IsNullOrEmpty(text))
                continue;
            node.AddValue(name, text);

            if (shader.GetPropertyType(i) == ShaderPropertyType.Texture)
            {
                Vector2 scale = loader.GetTextureScale(name), offset = loader.GetTextureOffset(name);
                node.AddValue(name + "Scale", $"{F(scale.x)},{F(scale.y)}");
                node.AddValue(name + "Offset", $"{F(offset.x)},{F(offset.y)}");
            }
        }

        foreach (var keyword in material.shaderKeywords)
            node.AddValue("keyword", keyword);
    }

    static string F(float f) => f.ToString("R", CultureInfo.InvariantCulture);

    static string Join(Vector4 v) => string.Join(",", new[] { v.x, v.y, v.z, v.w }.Select(F));
}
