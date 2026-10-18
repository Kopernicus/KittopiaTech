using System;
using Kopernicus.Configuration.MaterialLoader;

namespace KittopiaTech.Model;

/// <summary>Whether a shader keyword is enabled on a material loader's material.</summary>
public sealed class KeywordSlot(MaterialLoader loader, string keyword) : ISlot
{
    public MaterialLoader Loader { get; } = loader;
    public string Keyword { get; } = keyword;

    public object Owner => Loader;
    public MemberSchema Member => null;
    public string Label => Keyword;
    public bool CanWrite => Loader.Value != null;
    public Type DeclaredType => typeof(bool);

    public object Read() => Loader.GetKeyword(Keyword);

    public void Write(object raw) => Loader.SetKeyword(Keyword, (bool)raw);

    public override bool Equals(object obj) =>
        obj is KeywordSlot other && ReferenceEquals(Loader, other.Loader) && Keyword == other.Keyword;

    public override int GetHashCode() =>
        System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Loader) ^ Keyword.GetHashCode() ^ 0x2545f491;
}
