using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using KittopiaTech.Model;
using Kopernicus.ConfigParser.Enumerations;
using Kopernicus.ConfigParser.Interfaces;
using UnityEngine;

namespace KittopiaTech.Export;

/// <summary>
/// Registers an <see cref="IConfigWriter"/> for values the generic schema walk can't write.
/// Matched against the value's runtime type and its base classes.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ConfigWriterAttribute(Type type) : Attribute
{
    public Type Type { get; } = type;
}

public interface IConfigWriter
{
    /// <summary>Writes <paramref name="value"/> for <paramref name="member"/> into <paramref name="parent"/>.</summary>
    void Write(ConfigNode parent, MemberSchema member, object value);
}

/// <summary>
/// Writes a loader tree back out as config, walking the same schema the inspector uses. It reads
/// applied values only; pending edits are not included.
/// </summary>
public static class ConfigExporter
{
    static Dictionary<Type, IConfigWriter> writers;

    public static ConfigNode ExportBody(BodyContext context)
    {
        var node = new ConfigNode("Body");
        node.AddValue("name", context.Body.Name);
        using (ParseStateScope.For(context))
            WriteObject(node, context.Body);
        return node;
    }

    public static void WriteObject(ConfigNode node, object obj)
    {
        foreach (var member in TypeSchema.Get(obj.GetType()).Members)
        {
            if (!member.Exported || !member.CanRead)
                continue;
            if (member.FieldName == "name" && node.HasValue("name"))
                continue;

            object value;
            try
            {
                value = member.GetValue(obj);
            }
            catch (Exception)
            {
                // Getters on partially set up loaders can throw; there's nothing to export.
                continue;
            }
            if (value != null)
                WriteMember(node, member, value);
        }
    }

    static void WriteMember(ConfigNode node, MemberSchema member, object value)
    {
        var writer = FindWriter(value.GetType());
        if (writer != null)
        {
            writer.Write(node, member, value);
            return;
        }

        switch (member.Shape)
        {
            case MemberShape.Value:
                // Empty text is how unset maps and textures read back; it carries nothing.
                if (Format(value) is { Length: > 0 } text)
                    node.AddValue(member.FieldName, text);
                break;

            case MemberShape.Node:
                if (value is ConfigNode raw)
                {
                    node.AddNode(member.FieldName, raw.CreateCopy());
                    break;
                }
                // NameSignificance.Type nodes carry the concrete type: "Name:Type".
                string name = member.Target.NameSignificance == NameSignificance.Type
                    ? $"{member.FieldName}:{value.GetType().Name}"
                    : member.FieldName;
                WriteObject(node.AddNode(name), value);
                break;

            case MemberShape.Collection or MemberShape.KeyedCurve:
                if (value is IList { Count: > 0 } list)
                    WriteList(member.FieldName == "self" ? node : node.AddNode(member.FieldName), member, list);
                break;
        }
    }

    static void WriteList(ConfigNode node, MemberSchema member, IList list)
    {
        var target = member.Target;
        bool isNode = MemberSchema.GetConfigType(member.ElementType) == ConfigType.Node;
        foreach (var item in list)
        {
            if (item == null)
                continue;

            if (isNode)
            {
                // The parser only looks at item node names for NameSignificance.Type.
                string name = target.NameSignificance == NameSignificance.Type
                    ? item.GetType().Name
                    : ItemNodeName(member.ElementType);
                WriteObject(node.AddNode(name), item);
            }
            else if (Format(item) is { Length: > 0 } text)
            {
                string key = target.NameSignificance switch
                {
                    NameSignificance.Key => target.Key,
                    NameSignificance.Type => item.GetType().Name,
                    _ => "value",
                };
                node.AddValue(key, text);
            }
        }
    }

    /// <summary>"RingLoader" → "Ring": the conventional node name for untyped list items.</summary>
    static string ItemNodeName(Type elementType)
    {
        string name = elementType.Name;
        return name.EndsWith("Loader") && name.Length > "Loader".Length ? name.Substring(0, name.Length - 6) : name;
    }

    /// <summary>Config text for a value, using invariant culture and skipping localization.</summary>
    public static string Format(object value) =>
        value switch
        {
            null => null,
            string s => s,
            IWritable w => w.ValueToString(),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => null,
        };

    static IConfigWriter FindWriter(Type type)
    {
        writers ??= ScanWriters();
        for (var t = type; t != null; t = t.BaseType)
        {
            if (writers.TryGetValue(t, out var writer))
                return writer;
        }
        return null;
    }

    static Dictionary<Type, IConfigWriter> ScanWriters()
    {
        var result = new Dictionary<Type, IConfigWriter>();
        foreach (var loaded in AssemblyLoader.loadedAssemblies)
        {
            Type[] types;
            try
            {
                types = loaded.assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types.Where(t => t != null).ToArray();
            }

            foreach (var type in types.Where(t => typeof(IConfigWriter).IsAssignableFrom(t) && !t.IsAbstract))
            {
                foreach (var attr in type.GetCustomAttributes<ConfigWriterAttribute>())
                    result[attr.Type] = (IConfigWriter)Activator.CreateInstance(type);
            }
        }
        return result;
    }
}

/// <summary>Kopernicus gradients have no ParserTargets: each stop is a "position = colour" value.</summary>
[ConfigWriter(typeof(Kopernicus.Configuration.Parsing.Gradient))]
public sealed class GradientWriter : IConfigWriter
{
    public void Write(ConfigNode parent, MemberSchema member, object value)
    {
        UnityEngine.Gradient gradient = (Kopernicus.Configuration.Parsing.Gradient)value;
        var node = parent.AddNode(member.FieldName);
        foreach (var key in gradient.colorKeys)
        {
            var c = key.color;
            node.AddValue(
                key.time.ToString("R", CultureInfo.InvariantCulture),
                string.Join(",", new[] { c.r, c.g, c.b, c.a }.Select(f => f.ToString("R", CultureInfo.InvariantCulture)))
            );
        }
    }
}
