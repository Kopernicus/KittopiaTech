using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Kopernicus.ConfigParser.Attributes;
using Kopernicus.ConfigParser.Enumerations;
using Kopernicus.ConfigParser.Interfaces;
using Kopernicus.UI;

namespace KittopiaTech.Model;

public enum MemberShape
{
    /// <summary>A single config value.</summary>
    Value,

    /// <summary>A child node with its own schema.</summary>
    Node,

    /// <summary>A list of values or nodes.</summary>
    Collection,

    /// <summary>A list of <c>key = ...</c> values, i.e. a float curve.</summary>
    KeyedCurve,
}

/// <summary>A ParserTarget field or property on a loader type.</summary>
public sealed class MemberSchema
{
    public MemberInfo Member { get; }
    public TypeSchema Owner { get; }
    public Type DeclaredType { get; }
    public ParserTarget[] Targets { get; }
    public ParserTarget Target => Targets[0];
    public string FieldName => Target.FieldName;

    /// <summary>
    /// The name shown in the UI. A "self" collection is read from repeated keys in the owner's
    /// own node, so it's labelled by that key instead.
    /// </summary>
    public string Label =>
        FieldName == "self" ? (Target as ParserTargetCollection)?.Key ?? Member.Name : FieldName;
    public MemberShape Shape { get; }

    /// <summary>The list element type for collections, otherwise null.</summary>
    public Type ElementType { get; }

    public string Description { get; }

    /// <summary>The member's XML doc summary if its mod ships one, else its KittopiaDescription.</summary>
    public string Documentation => documentation ??= DocComments.Get(Member) ?? Description ?? "";
    string documentation;

    public bool Hidden { get; }
    public bool Exported { get; }
    public bool Untouchable { get; }
    public bool PreApply { get; }
    public bool CanRead { get; }
    public bool CanWrite { get; }
    public bool IsStatic { get; }

    public bool IsCollection => Shape is MemberShape.Collection or MemberShape.KeyedCurve;

    internal MemberSchema(TypeSchema owner, MemberInfo member, ParserTarget[] targets)
    {
        Owner = owner;
        Member = member;
        Targets = targets;

        switch (member)
        {
            case FieldInfo field:
                DeclaredType = field.FieldType;
                CanRead = true;
                CanWrite = !field.IsInitOnly;
                IsStatic = field.IsStatic;
                break;
            case PropertyInfo property:
                DeclaredType = property.PropertyType;
                CanRead = property.GetGetMethod(true) != null;
                CanWrite = property.GetSetMethod(true) != null;
                IsStatic = (property.GetGetMethod(true) ?? property.GetSetMethod(true)).IsStatic;
                break;
        }

        var hide = member.GetCustomAttribute<KittopiaHideOption>();
        Hidden = hide != null && !hide.Show;
        Exported = hide?.Export ?? true;
        Untouchable = member.IsDefined(typeof(KittopiaUntouchable), true);
        PreApply = member.IsDefined(typeof(PreApply), true);
        Description = member.GetCustomAttribute<KittopiaDescription>()?.Description;

        if (Target is ParserTargetCollection)
        {
            ElementType = GetElementType(DeclaredType);
            bool keyed = Target.NameSignificance == NameSignificance.Key;
            Shape = keyed ? MemberShape.KeyedCurve : MemberShape.Collection;
        }
        else
        {
            Shape = GetConfigType(DeclaredType) == ConfigType.Value
                ? MemberShape.Value
                : MemberShape.Node;
        }
    }

    public string Name => Member.Name;

    public object GetValue(object owner)
    {
        object target = IsStatic ? null : owner;
        return Member switch
        {
            FieldInfo field => field.GetValue(target),
            PropertyInfo property when CanRead => property.GetValue(target, null),
            _ => null,
        };
    }

    public void SetValue(object owner, object value)
    {
        if (!CanWrite)
            throw new InvalidOperationException($"{Owner.Type.Name}.{Name} is read-only");

        object target = IsStatic ? null : owner;
        try
        {
            switch (Member)
            {
                case FieldInfo field:
                    field.SetValue(target, value);
                    break;
                case PropertyInfo property:
                    property.SetValue(target, value, null);
                    break;
            }
        }
        catch (TargetInvocationException e) when (e.InnerException != null)
        {
            throw e.InnerException;
        }
    }

    public override string ToString() => $"{Owner.Type.Name}.{Name} ({FieldName}, {Shape})";

    /// <summary>
    /// The config type the parser expects. Unlike Kopernicus.UI.Tools this never throws:
    /// anything that can't be parsed from a string is treated as a node.
    /// </summary>
    public static ConfigType GetConfigType(Type type)
    {
        if (type == typeof(string))
            return ConfigType.Value;
        if (type == typeof(ConfigNode))
            return ConfigType.Node;

        var attr = type.GetCustomAttributes(typeof(RequireConfigType), true)
            .Cast<RequireConfigType>()
            .FirstOrDefault();
        if (attr != null)
            return attr.Type;

        return typeof(IParsable).IsAssignableFrom(type) ? ConfigType.Value : ConfigType.Node;
    }

    static Type GetElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();
        if (type.IsGenericType && typeof(IList).IsAssignableFrom(type))
            return type.GetGenericArguments()[0];
        return typeof(object);
    }
}
