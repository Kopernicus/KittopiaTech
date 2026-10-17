using System;
using System.Collections.Generic;
using System.Linq;
using KittopiaTech.Model;
using Kopernicus;
using Kopernicus.Configuration;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KittopiaTech.Export;

/// <summary>
/// Builds a ModuleManager patch holding what a body changes relative to its Template. The
/// baseline is the template's stock body, exported with the same exporter and with the
/// template's remove* options applied, so the patch doesn't depend on edit history.
/// </summary>
public static class TemplatePatch
{
    static readonly Dictionary<string, ConfigNode> StockExports = [];

    /// <summary>The body's Body node in the loaded Kopernicus config, if it has one.</summary>
    public static ConfigNode FindBodyConfig(string bodyName) =>
        GameDatabase
            .Instance.GetConfigNodes("Kopernicus")
            .SelectMany(k => k.GetNodes("Body"))
            .FirstOrDefault(b => b.GetValue("name") == bodyName);

    /// <summary>The patch body (the contents of <c>@Body[name]</c>), or null if there's no template.</summary>
    public static ConfigNode Build(BodyContext context, out string warning)
    {
        warning = null;
        var template = FindBodyConfig(context.Body.Name)?.GetNode("Template");
        string templateName = template?.GetValue("name");
        if (templateName == null)
            return null;

        var baseline = StockExport(templateName)?.CreateCopy();
        if (baseline == null)
        {
            warning = $"Couldn't export template {templateName}";
            return null;
        }
        ApplyTemplateOptions(baseline, template);

        var current = ConfigExporter.ExportBody(context);
        var patch = new ConfigNode("patch");
        var removedMods = new List<string>();
        var notes = new List<string>();
        Diff(baseline, current, patch, removedMods, notes, "");

        if (removedMods.Count > 0)
        {
            // Template mods are inherited, so deleting their nodes does nothing; remove them at the template.
            var existing = template.GetValue("removePQSMods");
            var all = (existing?.Split(',').Select(s => s.Trim()) ?? []).Concat(removedMods).Distinct();
            patch.AddNode("%Template").AddValue("%removePQSMods", string.Join(", ", all.ToArray()));
        }
        if (notes.Count > 0)
            warning = string.Join("\n", notes.Distinct().ToArray());
        return patch;
    }

    /// <summary>Exports the stock prefab body by spawning a throwaway copy, cached per name.</summary>
    static ConfigNode StockExport(string name)
    {
        if (StockExports.TryGetValue(name, out var cached))
            return cached;

        var original = Utility.FindBody(Injector.StockSystemPrefab.rootBody, name);
        if (original == null)
            return null;

        var clones = new List<GameObject>();
        try
        {
            // The CelestialBody lives on its own object, not on the PSystemBody.
            var go = Object.Instantiate(original.celestialBody.gameObject, Utility.Deactivator, true);
            clones.Add(go);
            var cb = go.GetComponent<CelestialBody>();
            var scaled = Object.Instantiate(original.scaledVersion, Utility.Deactivator, true);
            clones.Add(scaled);
            cb.scaledBody = scaled;
            if (original.pqsVersion != null)
            {
                var pqs = Object.Instantiate(original.pqsVersion, Utility.Deactivator, true);
                clones.Add(pqs.gameObject);
                cb.pqsController = pqs;
            }
            cb.transform.name = name;
            // Spawning would wire these up; the loaders skip the orbit and AFG without them.
            cb.orbitDriver = go.GetComponent<OrbitDriver>();
            cb.afg = scaled.GetComponentInChildren<AtmosphereFromGround>(true);

            var node = ConfigExporter.ExportBody(BodyContext.Detached(cb));
            StockExports[name] = node;
            return node;
        }
        catch (Exception e)
        {
            Debug.LogError($"[KittopiaTech] Failed to export stock body {name}: {e}");
            return null;
        }
        finally
        {
            foreach (var clone in clones)
                Object.Destroy(clone);
        }
    }

    static void ApplyTemplateOptions(ConfigNode baseline, ConfigNode template)
    {
        bool On(string key) => bool.TryParse(template.GetValue(key), out var v) && v;

        if (On("removePQS"))
        {
            baseline.RemoveNode("PQS");
            baseline.RemoveNode("Ocean");
        }
        if (On("removeAtmosphere"))
            baseline.RemoveNode("Atmosphere");
        if (On("removeOcean"))
            baseline.RemoveNode("Ocean");
        if (template.GetValue("removeBiomes") is not "false" and not "False")
            baseline.GetNode("Properties")?.RemoveNode("Biomes");

        var mods = baseline.GetNode("PQS")?.GetNode("Mods");
        if (mods == null)
            return;
        if (On("removeAllPQSMods"))
            mods.ClearNodes();
        foreach (var entry in template.GetValue("removePQSMods")?.Split(',') ?? [])
        {
            string spec = entry.Trim().Replace("PQSMod_", "");
            string type = spec.Split('[')[0];
            string name = spec.Contains('[') ? spec.Split('[')[1].TrimEnd(']') : null;
            foreach (var mod in mods.GetNodes(type).Where(m => name == null || m.GetValue("name") == name))
                mods.RemoveNode(mod);
        }
    }

    /// <summary>Writes into <paramref name="patch"/> the operations that turn baseline into current.</summary>
    static void Diff(ConfigNode baseline, ConfigNode current, ConfigNode patch, List<string> removedMods, List<string> notes, string path)
    {
        foreach (ConfigNode.Value value in current.values)
        {
            if (baseline.GetValue(value.name) != value.value)
                patch.AddValue("%" + value.name, value.value);
        }
        foreach (ConfigNode.Value value in baseline.values)
        {
            if (!current.HasValue(value.name))
                patch.AddValue("!" + value.name, "delete");
        }

        foreach (var name in current.nodes.Cast<ConfigNode>().Select(n => n.name).Concat(baseline.nodes.Cast<ConfigNode>().Select(n => n.name)).Distinct().ToList())
        {
            var mine = current.GetNodes(name);
            var theirs = baseline.GetNodes(name);
            bool isList = mine.Length > 1 || theirs.Length > 1 || mine.Concat(theirs).Any(n => n.HasValue("name"));
            if (!isList)
                DiffNode(theirs.FirstOrDefault(), mine.FirstOrDefault(), name, patch, removedMods, notes, path + "/" + name);
            else
                DiffList(theirs, mine, name, patch, removedMods, notes, path);
        }
    }

    static void DiffNode(ConfigNode baseline, ConfigNode current, string name, ConfigNode patch, List<string> removedMods, List<string> notes, string path)
    {
        if (current == null)
        {
            patch.AddNode("!" + name);
            return;
        }
        if (baseline == null)
        {
            patch.AddNode(name, current.CreateCopy());
            return;
        }
        if (HasRepeatedValues(current) || HasRepeatedValues(baseline))
        {
            // Curves and value lists are positional, so replace them whole.
            if (current.ToString() != baseline.ToString())
            {
                patch.AddNode("!" + name);
                patch.AddNode(name, current.CreateCopy());
            }
            return;
        }

        var child = new ConfigNode("%" + name);
        Diff(baseline, current, child, removedMods, notes, path);
        if (child.CountValues > 0 || child.CountNodes > 0)
            patch.AddNode(child);
    }

    /// <summary>List items pair by name, or by position among unnamed items of the same type.</summary>
    static void DiffList(ConfigNode[] baseline, ConfigNode[] current, string type, ConfigNode patch, List<string> removedMods, List<string> notes, string path)
    {
        var unmatched = baseline.ToList();
        int unnamedIndex = 0;
        foreach (var item in current)
        {
            string name = item.GetValue("name");
            var match = name != null
                ? unmatched.FirstOrDefault(b => b.GetValue("name") == name)
                : unmatched.FirstOrDefault(b => !b.HasValue("name"));
            if (match == null)
            {
                patch.AddNode(type, item.CreateCopy());
                continue;
            }
            unmatched.Remove(match);

            string selector = name != null ? $"[{name}]" : $",{unnamedIndex++}";
            if (name == null)
                notes.Add($"{path}/{type}: unnamed items are matched by position");
            var child = new ConfigNode($"%{type}{selector}");
            Diff(match, item, child, removedMods, notes, path + "/" + type);
            if (child.CountValues > 0 || child.CountNodes > 0)
            {
                if (name != null)
                    child.AddValue("%name", name);
                patch.AddNode(child);
            }
        }

        foreach (var removed in unmatched)
        {
            string name = removed.GetValue("name");
            if (path.EndsWith("/PQS/Mods"))
                removedMods.Add(name != null ? $"{type}[{name}]" : type);
            else
                patch.AddNode(name != null ? $"!{type}[{name}]" : $"!{type}");
        }
    }

    static bool HasRepeatedValues(ConfigNode node) =>
        node.values.Cast<ConfigNode.Value>().GroupBy(v => v.name).Any(g => g.Count() > 1);

    /// <summary>The full file contents for a patch.</summary>
    public static ConfigNode Wrap(string bodyName, ConfigNode patch, string pass)
    {
        var root = new ConfigNode();
        var kopernicus = root.AddNode($"@Kopernicus:{pass}");
        var body = kopernicus.AddNode($"@Body[{bodyName}]");
        body.AddData(patch);
        return root;
    }
}
