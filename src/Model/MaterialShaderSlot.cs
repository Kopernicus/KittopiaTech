using System;
using System.Reflection;
using Kopernicus.Configuration.MaterialLoader;
using Kopernicus.Configuration.Parsing;
using UnityEngine;

namespace KittopiaTech.Model;

/// <summary>
/// The shader of the material held in a material loader member. Kopernicus picks the loader
/// class from the shader, so writing one switches the material's shader and puts the loader
/// for that shader in the member: a built-in one if Kopernicus has one, otherwise a custom
/// loader. The new loader wraps the same material, so whatever renders with it keeps it.
/// </summary>
public sealed class MaterialShaderSlot(ISlot holder) : ISlot
{
    // Only in Kopernicus builds that wrap live materials; see ApplyHooks.RefreshOnDemand.
    static readonly MethodInfo CreateExisting = typeof(MaterialLoader).GetMethod(
        "CreateExisting", [typeof(Material)]);

    /// <summary>The member holding the loader; edits are keyed to its owner, which outlives the loader.</summary>
    public ISlot Holder { get; } = holder;

    public object Owner => Holder.Owner;
    public MemberSchema Member => null;
    public string Label => "shader";
    public bool CanWrite => true;
    public Type DeclaredType => typeof(ShaderParser);

    /// <summary>A slot for the loader in <paramref name="holder"/>, or null if its shader can't be switched.</summary>
    public static MaterialShaderSlot For(ISlot holder, object loader) =>
        CreateExisting != null && holder is MemberSlot && loader is MaterialLoader { Value: not null }
            ? new MaterialShaderSlot(holder)
            : null;

    public object Read() =>
        Holder.Read() is MaterialLoader { Value: { } material } ? new ShaderParser { Value = material.shader } : null;

    public void Write(object raw)
    {
        var loader = (MaterialLoader)Holder.Read();
        var material = loader.Value;
        material.shader = ((ShaderParser)raw).Value;

        var replacement = (MaterialLoader)CreateExisting.Invoke(null, [material]);
        replacement.OnDemand = loader.OnDemand;
        // Texture paths still to load on demand, for the properties the new shader shares.
        foreach (var entry in loader.Entries)
        {
            if (material.HasProperty(entry.Key))
                replacement.Entries[entry.Key] = entry.Value;
        }
        Holder.Write(replacement);
    }

    public ISlot Rebind(Func<object, object> map) => new MaterialShaderSlot(Holder.Rebind(map));

    public override bool Equals(object obj) => obj is MaterialShaderSlot other && Holder.Equals(other.Holder);

    public override int GetHashCode() => Holder.GetHashCode() ^ 0x68e31da4;
}
