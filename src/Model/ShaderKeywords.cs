using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KittopiaTech.Model;

public enum KeywordDrawerKind
{
    /// <summary><c>[Toggle]</c>: the keyword is on while the property is non-zero.</summary>
    Toggle,

    /// <summary><c>[ToggleOff]</c>: the keyword is on while the property is zero.</summary>
    ToggleOff,

    /// <summary><c>[KeywordEnum(A, B)]</c>: the property is an index into its keywords.</summary>
    Enum,
}

/// <summary>
/// A shader property whose material drawer sets keywords. Unity's material inspector runs these
/// drawers in the editor only, so at runtime the property and its keywords are set separately.
/// </summary>
public sealed class KeywordDrawer(KeywordDrawerKind kind, string[] keywords, string[] options)
{
    public KeywordDrawerKind Kind { get; } = kind;
    public string[] Keywords { get; } = keywords;

    /// <summary>The enum's names as written in the shader; empty for toggles.</summary>
    public string[] Options { get; } = options;

    /// <summary>The drawer on property <paramref name="index"/>, or null if it sets no keywords.</summary>
    public static KeywordDrawer For(Shader shader, int index)
    {
        string property = shader.GetPropertyName(index);
        foreach (var attribute in shader.GetPropertyAttributes(index))
        {
            var drawer = Parse(property, attribute);
            if (drawer != null)
                return drawer;
        }
        return null;
    }

    /// <summary>Every keyword some property of <paramref name="shader"/> sets.</summary>
    public static HashSet<string> Owned(Shader shader)
    {
        var owned = new HashSet<string>();
        for (int i = 0; i < shader.GetPropertyCount(); i++)
        {
            var drawer = For(shader, i);
            if (drawer != null)
                owned.UnionWith(drawer.Keywords);
        }
        return owned;
    }

    // Names and defaults follow Unity's MaterialToggleDrawer and MaterialKeywordEnumDrawer.
    static KeywordDrawer Parse(string property, string attribute)
    {
        int open = attribute.IndexOf('(');
        string name = (open < 0 ? attribute : attribute.Substring(0, open)).Trim();
        string[] args =
            open < 0
                ? []
                : attribute
                    .Substring(open + 1)
                    .TrimEnd(')', ' ')
                    .Split(',')
                    .Select(a => a.Trim())
                    .Where(a => a.Length > 0)
                    .ToArray();

        switch (name)
        {
            case "Toggle" or "MaterialToggle":
                return new(KeywordDrawerKind.Toggle, [args.Length > 0 ? args[0] : property.ToUpperInvariant() + "_ON"], []);
            case "ToggleOff":
                return new(KeywordDrawerKind.ToggleOff, [args.Length > 0 ? args[0] : property.ToUpperInvariant() + "_OFF"], []);
            case "KeywordEnum" when args.Length > 0:
                var keywords = args.Select(a => (property + "_" + a).Replace(' ', '_').ToUpperInvariant()).ToArray();
                return new(KeywordDrawerKind.Enum, keywords, args);
            default:
                return null;
        }
    }
}

/// <summary>A <c>[KeywordEnum]</c> property's value: which of its options is selected.</summary>
public readonly struct KeywordChoice(string[] options, int index) : IEquatable<KeywordChoice>
{
    public string[] Options { get; } = options;
    public int Index { get; } = index;

    public bool Equals(KeywordChoice other) => Index == other.Index;

    public override bool Equals(object obj) => obj is KeywordChoice other && Equals(other);

    public override int GetHashCode() => Index;

    public override string ToString() => Index >= 0 && Index < Options.Length ? Options[Index] : Index.ToString();
}
