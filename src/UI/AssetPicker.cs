using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KittopiaTech.UI.Debug;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI;

/// <summary>
/// A searchable list of asset paths under GameData. Only the top matches get rows: a fixed
/// pool is refilled as the search changes, so thousands of textures cost nothing to show.
/// </summary>
public sealed class AssetPicker : MonoBehaviour
{
    const int MaxRows = 40;

    public static readonly string[] TextureExtensions = [".dds", ".png", ".jpg", ".jpeg", ".tga", ".truecolor"];

    static readonly Dictionary<string, string[]> Indexes = [];

    string[] entries;
    TMP_InputField search;
    TextMeshProUGUI status;
    readonly List<Button> rows = [];
    Action<string> onPick;
    Popout popout;

    /// <summary>Opens a picker for files with <paramref name="extensions"/>, plus built-in textures if asked.</summary>
    public static void Open(Component owner, string title, string[] extensions, bool builtinTextures, Action<string> onPick)
    {
        var popout = Popout.Open(owner, title, new Vector2(460f, 560f));
        var picker = popout.gameObject.AddComponent<AssetPicker>();
        picker.popout = popout;
        picker.onPick = onPick;
        picker.entries = Index(extensions, builtinTextures);
        picker.Build(popout.Content);
        picker.Filter("");
    }

    void Build(Transform content)
    {
        search = DebugSkin.CreateInputField(content);
        search.onValueChanged.AddListener(Filter);
        status = DebugSkin.CreateLabel(content, "");

        DebugSkin.CreateScrollView(content, out var list);
        for (int i = 0; i < MaxRows; i++)
        {
            var button = DebugSkin.CreateButton(list, "");
            button.GetComponent<LayoutElement>().minHeight = 22f;
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.margin = new Vector4(6f, 0f, 6f, 0f);
            button.onClick.AddListener(() => Pick(label.text));
            rows.Add(button);
        }
        search.ActivateInputField();
    }

    void Filter(string text)
    {
        var terms = text.ToLowerInvariant().Split([' '], StringSplitOptions.RemoveEmptyEntries);
        var matches = entries.Where(e => terms.All(t => e.ToLowerInvariant().Contains(t)));

        int total = 0;
        foreach (var match in matches)
        {
            if (total < rows.Count)
            {
                rows[total].GetComponentInChildren<TextMeshProUGUI>().text = match;
                rows[total].gameObject.SetActive(true);
            }
            total++;
        }
        for (int i = total; i < rows.Count; i++)
            rows[i].gameObject.SetActive(false);

        status.text = total > rows.Count ? $"Showing {rows.Count} of {total} matches" : $"{total} matches";
    }

    void Pick(string path)
    {
        onPick(path);
        popout.Close();
    }

    /// <summary>GameData-relative paths with the given extensions, scanned once per extension set.</summary>
    static string[] Index(string[] extensions, bool builtinTextures)
    {
        string key = string.Join("|", extensions) + builtinTextures;
        if (Indexes.TryGetValue(key, out var cached))
            return cached;

        string root = Path.GetFullPath(Path.Combine(KSPUtil.ApplicationRootPath, "GameData"));
        var paths = Directory
            .GetFiles(root, "*", SearchOption.AllDirectories)
            .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .Select(f => f.Substring(root.Length + 1).Replace('\\', '/'));

        if (builtinTextures)
        {
            // GameDatabase textures are files in GameData, already listed by path.
            var database = new HashSet<string>(
                GameDatabase.Instance.databaseTexture.Select(t => t.name),
                StringComparer.OrdinalIgnoreCase
            );
            paths = paths.Concat(BuiltinTextures.Names.Where(n => !database.Contains(n)).Select(n => "BUILTIN/" + n));
        }

        var index = paths.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
        Indexes[key] = index;
        return index;
    }
}
