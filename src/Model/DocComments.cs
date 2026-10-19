using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace KittopiaTech.Model;

/// <summary>
/// Reads <c>&lt;summary&gt;</c> docs from the XML documentation file a mod ships beside its
/// DLL (<c>Plugins/Mod.dll</c> and <c>Plugins/Mod.xml</c>). Each file is parsed once, on first use.
/// </summary>
public static class DocComments
{
    static readonly Dictionary<Assembly, Dictionary<string, string>> Files = [];
    static readonly Regex Whitespace = new(@"\s+");

    public static string Get(MemberInfo member)
    {
        var type = member.DeclaringType;
        if (type == null)
            return null;
        if (type.IsGenericType)
            type = type.GetGenericTypeDefinition();

        string prefix = member switch
        {
            FieldInfo => "F:",
            PropertyInfo => "P:",
            MethodInfo => "M:",
            Type => "T:",
            _ => null,
        };
        if (prefix == null)
            return null;

        string id = prefix + type.FullName?.Replace('+', '.') + "." + member.Name;
        return For(type.Assembly).TryGetValue(id, out var summary) ? summary : null;
    }

    static Dictionary<string, string> For(Assembly assembly)
    {
        if (Files.TryGetValue(assembly, out var docs))
            return docs;

        docs = [];
        Files[assembly] = docs;
        try
        {
            string location = assembly.Location;
            if (string.IsNullOrEmpty(location))
                return docs;
            string path = Path.ChangeExtension(location, ".xml");
            if (File.Exists(path))
                Load(path, docs);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"[KittopiaTech] Couldn't read docs for {assembly.GetName().Name}: {e.Message}");
        }
        return docs;
    }

    static void Load(string path, Dictionary<string, string> docs)
    {
        using var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true });
        string name = null;
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element)
                continue;
            if (reader.Name == "member")
                name = reader.GetAttribute("name");
            else if (reader.Name == "summary" && name != null)
            {
                using var summary = reader.ReadSubtree();
                docs[name] = Clean(summary);
            }
        }
    }

    /// <summary>Flattens a summary to plain text; <c>see</c> references become their short names.</summary>
    static string Clean(XmlReader summary)
    {
        var text = new StringBuilder();
        while (summary.Read())
        {
            switch (summary.NodeType)
            {
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                    text.Append(summary.Value);
                    break;
                case XmlNodeType.Element when summary.Name is "see" or "seealso" or "paramref" or "typeparamref":
                    var target = summary.GetAttribute("cref") ?? summary.GetAttribute("name") ?? summary.GetAttribute("langword");
                    if (target != null)
                        text.Append(target.Substring(target.LastIndexOfAny([':', '.']) + 1));
                    break;
                case XmlNodeType.Element when summary.Name == "para":
                    text.Append(' ');
                    break;
            }
        }
        return Whitespace.Replace(text.ToString(), " ").Trim();
    }
}
