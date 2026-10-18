using System.IO;
using KittopiaTech.Model;
using UnityEngine;

namespace KittopiaTech.Export;

/// <summary>Writes exports to <c>PluginData/Kittopia/&lt;body&gt;.cfg</c> in the KSP folder.</summary>
public static class ExportFiles
{
    const string Pass = "FOR[KittopiaTech]";

    /// <summary>The whole body, replacing its existing config.</summary>
    public static string ExportBody(BodyContext context)
    {
        string name = context.Body.Name;
        var root = new ConfigNode();
        var kopernicus = root.AddNode($"@Kopernicus:{Pass}");
        kopernicus.AddNode($"!Body[{name}]");
        kopernicus.AddNode(ConfigExporter.ExportBody(context));
        return Save(name, root);
    }

    /// <summary>A patch against the body's Template, or null when the body has none.</summary>
    public static string ExportPatch(BodyContext context, out string warning)
    {
        var patch = TemplatePatch.Build(context, out warning);
        if (patch == null)
            return null;
        var wrapped = TemplatePatch.Wrap(context.Body.Name, patch, Pass);
        var template = Bodies.CreatedTemplate(context.CelestialBody);
        if (template == null)
            return Save(context.Body.Name, wrapped);

        // A body made in game has no config to patch, so the file creates it from its template
        // first; ModuleManager runs a pass's patches in file order.
        var root = new ConfigNode();
        var body = root.AddNode($"@Kopernicus:{Pass}").AddNode("Body");
        body.AddValue("name", context.Body.Name);
        body.AddNode(template.CreateCopy());
        root.AddData(wrapped);
        return Save(context.Body.Name, root);
    }

    static string Save(string bodyName, ConfigNode root)
    {
        // Replaces the body's previous export, whichever kind it was.
        string relative = Path.Combine("PluginData/Kittopia", bodyName + ".cfg");
        string path = Path.Combine(KSPUtil.ApplicationRootPath, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        root.Save(path);
        Debug.Log($"[KittopiaTech] Exported {bodyName} to {path}");
        return relative.Replace('\\', '/');
    }
}
