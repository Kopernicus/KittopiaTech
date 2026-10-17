using System;
using Kopernicus.Configuration.MaterialLoader;
using Kopernicus.Configuration.MaterialLoader.Parsing;
using UnityEngine;
using UnityEngine.Rendering;

namespace KittopiaTech.Model;

/// <summary>
/// One shader property on a material loader's material, e.g. <c>_MainTex</c>. Reads and writes
/// go through the loader's accessors, as the raw <c>_X</c> config keys do.
/// </summary>
public sealed class ShaderPropertySlot(MaterialLoader loader, string name, ShaderPropertyType type) : ISlot
{
    public MaterialLoader Loader { get; } = loader;
    public string Name { get; } = name;
    public ShaderPropertyType PropertyType { get; } = type;

    public object Owner => Loader;
    public MemberSchema Member => null;
    public string Label => Name;
    public bool CanWrite => Loader.Value != null;

    public Type DeclaredType =>
        PropertyType switch
        {
            ShaderPropertyType.Color => typeof(Color),
            ShaderPropertyType.Vector => typeof(Vector4),
            ShaderPropertyType.Texture => typeof(MaterialTextureParser),
            _ => typeof(float),
        };

    public object Read() =>
        PropertyType switch
        {
            ShaderPropertyType.Color => Loader.GetColor(Name),
            ShaderPropertyType.Vector => Loader.GetVector(Name),
            ShaderPropertyType.Texture => new MaterialTextureParser(Loader.GetTextureName(Name) ?? ""),
            _ => Loader.GetFloat(Name),
        };

    public void Write(object raw)
    {
        switch (raw)
        {
            case Color color:
                Loader.SetColor(Name, color);
                break;
            case Vector4 vector:
                Loader.SetVector(Name, vector);
                break;
            case float value:
                Loader.SetFloat(Name, value);
                break;
            case MaterialTextureParser texture:
                // On-demand would only record the path and clear the texture until the body
                // is rebuilt, so load it now.
                var onDemand = Loader.OnDemand;
                Loader.OnDemand = false;
                try
                {
                    if (string.IsNullOrEmpty(texture.Path))
                    {
                        Loader.Entries.Remove(Name);
                        Loader.Value.SetTexture(Name, null);
                    }
                    else
                        Loader.SetTexture(Name, texture);
                }
                finally
                {
                    Loader.OnDemand = onDemand;
                }
                break;
        }
    }

    public override bool Equals(object obj) =>
        obj is ShaderPropertySlot other && ReferenceEquals(Loader, other.Loader) && Name == other.Name;

    public override int GetHashCode() =>
        System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Loader) ^ Name.GetHashCode();
}
