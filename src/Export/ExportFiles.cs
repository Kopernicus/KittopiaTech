using System;
using System.IO;
using KittopiaTech.Model;
using UnityEngine;

namespace KittopiaTech.Export;

/// <summary>Writes exports to <c>GameData/KittopiaTech/PluginData/&lt;body&gt;/&lt;time&gt;/&lt;body&gt;.cfg</c>.</summary>
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
        return Save(context.Body.Name, TemplatePatch.Wrap(context.Body.Name, patch, Pass));
    }

    static string Save(string bodyName, ConfigNode root)
    {
        string relative = Path.Combine(
            Path.Combine("GameData/KittopiaTech/PluginData", bodyName),
            Path.Combine(DateTime.Now.ToString("yyyy-MM-dd_HHmmss"), bodyName + ".cfg")
        );
        string path = Path.Combine(KSPUtil.ApplicationRootPath, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        root.Save(path);
        GUIUtility.systemCopyBuffer = Path.GetFullPath(path);
        Debug.Log($"[KittopiaTech] Exported {bodyName} to {path}");
        return relative.Replace('\\', '/');
    }
}
