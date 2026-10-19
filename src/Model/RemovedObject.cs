using System;
using System.Collections;
using System.Collections.Generic;
using KittopiaTech.Export;
using Kopernicus.ConfigParser;
using Kopernicus.ConfigParser.Enumerations;

namespace KittopiaTech.Model;

/// <summary>
/// An object an Apply removed, kept as config so History can bring it back. Its destructor
/// can't be undone, so it's rebuilt instead: created the way the inspector creates objects,
/// then loaded from the config the way Kopernicus loads a body.
/// </summary>
internal sealed class RemovedObject
{
    readonly Type type;
    readonly ConfigNode config;

    // Every object in the current instance's tree, by member path, to match up with a rebuild.
    Dictionary<string, object> tree;

    /// <summary>The removed object, or its latest rebuild.</summary>
    public object Current { get; private set; }

    RemovedObject(object removed, ConfigNode config)
    {
        type = removed.GetType();
        this.config = config;
        Current = removed;
        tree = Tree(removed);
    }

    /// <summary>A snapshot of <paramref name="removed"/>, or null if it can't be rebuilt.</summary>
    public static RemovedObject TrySnapshot(BodyContext context, object removed)
    {
        if (!TypeSchema.Get(removed.GetType()).CanConstruct)
            return null;
        try
        {
            var config = new ConfigNode("Removed");
            using (ParseStateScope.For(context))
                ConfigExporter.WriteObject(config, removed);
            return new RemovedObject(removed, config);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"[KittopiaTech] Couldn't keep a copy of the removed {removed.GetType().Name}: {e}");
            return null;
        }
    }

    /// <summary>
    /// Builds a new copy and records in <paramref name="map"/> the copy's counterpart of each
    /// object in the replaced instance's tree, so references to them can be moved over.
    /// </summary>
    public object Rebuild(BodyContext context, Dictionary<object, object> map)
    {
        var rebuilt = Structure.Create(context, type);
        using (ParseStateScope.For(context))
            Parser.LoadObjectFromConfigurationNode(rebuilt, config, "Kopernicus");

        var fresh = Tree(rebuilt);
        foreach (var entry in tree)
        {
            if (fresh.TryGetValue(entry.Key, out var counterpart) && !ReferenceEquals(entry.Value, counterpart))
                map[entry.Value] = counterpart;
        }
        tree = fresh;
        Current = rebuilt;
        return rebuilt;
    }

    /// <summary>The nodes and lists beneath <paramref name="root"/>, by their path from it.</summary>
    static Dictionary<string, object> Tree(object root)
    {
        var result = new Dictionary<string, object>();
        var seen = new HashSet<object>(ReferenceComparer.Instance);
        var pending = new Stack<(object Node, string Path)>();
        pending.Push((root, ""));
        while (pending.Count > 0)
        {
            var (node, path) = pending.Pop();
            if (node == null || !seen.Add(node))
                continue;
            result[path] = node;

            if (node is IList list)
            {
                // Values (numbers, strings) can be shared, so only items that are nodes count.
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] is { } item && MemberSchema.GetConfigType(item.GetType()) == ConfigType.Node)
                        pending.Push((item, $"{path}[{i}]"));
                }
                continue;
            }

            foreach (var member in TypeSchema.Get(node.GetType()).Members)
            {
                if (member.Shape == MemberShape.Value || !member.CanRead)
                    continue;
                try
                {
                    pending.Push((member.GetValue(node), $"{path}.{member.Name}"));
                }
                catch (Exception)
                {
                    // Getters on partially set up loaders can throw; there's nothing to match.
                }
            }
        }
        return result;
    }
}
