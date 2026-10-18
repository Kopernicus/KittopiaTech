using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KittopiaTech.UI.Debug;
using KSPTextureLoader;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI;

/// <summary>What a picker lists and how it previews the selection.</summary>
public enum AssetKind
{
    /// <summary>Files only, no preview.</summary>
    File,

    /// <summary>Textures: files, KSPTextureLoader bundles and BUILTIN/ textures.</summary>
    Texture,

    /// <summary>MapSOs: texture files and bundles, plus BUILTIN/ stock maps.</summary>
    Map,
}

/// <summary>
/// A searchable list of asset paths under GameData. Only the top matches get rows: a fixed
/// pool is refilled as the search changes, so thousands of textures cost nothing to show.
/// Clicking a row previews it; double-clicking or Select picks it. Enter picks the selected
/// row, or the search text as typed when nothing is selected.
/// </summary>
public sealed class AssetPicker : MonoBehaviour
{
    const int MaxRows = 40;
    const float DoubleClickTime = 0.4f;
    const string Builtin = "BUILTIN/";

    public static readonly string[] TextureExtensions = [".dds", ".png", ".jpg", ".jpeg", ".tga", ".truecolor"];

    static readonly Dictionary<string, string[]> Indexes = [];

    string[] entries;
    AssetKind kind;
    TMP_InputField search;
    TextMeshProUGUI status;
    readonly List<Button> rows = [];
    Action<string> onPick;
    Popout popout;

    string selected;
    float selectedAt;
    Button selectButton;
    GameObject previewBox;
    RawImage preview;
    AspectRatioFitter previewAspect;
    TextMeshProUGUI previewInfo;
    readonly TexturePreview loaded = new();

    /// <summary>Opens a picker for files with <paramref name="extensions"/>, searching for <paramref name="initial"/>.</summary>
    public static Popout Open(Component owner, string title, string[] extensions, string initial, Action<string> onPick) =>
        Open(owner, title, AssetKind.File, Index(extensions, AssetKind.File), initial, onPick);

    public static Popout Open(Component owner, string title, AssetKind kind, string initial, Action<string> onPick) =>
        Open(owner, title, kind, Index(TextureExtensions, kind), initial, onPick);

    /// <summary>
    /// Opens a picker over a list of names that aren't files, such as bodies or shaders. Short
    /// lists are browsed, so it starts with every name shown and the current one selected.
    /// </summary>
    public static Popout OpenList(Component owner, string title, string[] entries, string initial, Action<string> onPick) =>
        Open(owner, title, AssetKind.File, entries, initial, onPick, searchInitial: false);

    static Popout Open(
        Component owner,
        string title,
        AssetKind kind,
        string[] entries,
        string initial,
        Action<string> onPick,
        bool searchInitial = true
    )
    {
        var popout = Popout.Open(owner, title, new Vector2(460f, kind == AssetKind.File ? 560f : 720f));
        var picker = popout.gameObject.AddComponent<AssetPicker>();
        picker.popout = popout;
        picker.onPick = onPick;
        picker.kind = kind;
        picker.entries = entries;
        picker.Build(popout.Content);

        // Start on the current value, previewed if it's in the list.
        picker.search.text = searchInitial ? initial ?? "" : "";
        picker.Filter(picker.search.text);
        var current = picker.entries.FirstOrDefault(e => string.Equals(e, initial, StringComparison.OrdinalIgnoreCase));
        if (current != null)
            picker.Click(current);
        return popout;
    }

    void Build(Transform content)
    {
        search = DebugSkin.CreateInputField(content);
        search.onValueChanged.AddListener(Filter);
        search.onSubmit.AddListener(text => Pick(selected ?? text));

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
            button.onClick.AddListener(() => Click(label.text));
            rows.Add(button);
        }

        if (kind != AssetKind.File)
            BuildPreview(content);

        var footer = DebugSkin.CreateHorizontalLayout(content, 4f, controlWidth: true);
        status = DebugSkin.CreateLabel(footer.transform, "");
        selectButton = DebugSkin.CreateButton(footer.transform, "Select", () => Pick(selected));
        selectButton.GetComponent<LayoutElement>().preferredWidth = 80f;
        selectButton.GetComponent<LayoutElement>().flexibleWidth = 0f;
        selectButton.interactable = false;
        search.ActivateInputField();
    }

    void BuildPreview(Transform content)
    {
        // Hidden until something is selected, so the list can use the space.
        var box = previewBox = new GameObject("Preview", typeof(RectTransform));
        box.transform.SetParent(content, false);
        var boxLayout = box.AddComponent<LayoutElement>();
        boxLayout.minHeight = boxLayout.preferredHeight = 160f;

        var image = new GameObject("Image", typeof(RectTransform));
        image.transform.SetParent(box.transform, false);
        preview = image.AddComponent<RawImage>();
        preview.material = PreviewMaterial.IgnoreAlpha;
        preview.color = Color.clear;
        previewAspect = image.AddComponent<AspectRatioFitter>();
        previewAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

        previewInfo = DebugSkin.CreateLabel(content, "");
        previewInfo.enableWordWrapping = false;
        previewInfo.overflowMode = TextOverflowModes.Ellipsis;
        previewInfo.color = new Color(0.64f, 0.65f, 0.67f);
        box.SetActive(false);
        previewInfo.transform.parent.gameObject.SetActive(false);
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
        HighlightSelected();
    }

    static readonly Color SelectedRowColor = new(0.75f, 0.85f, 1f);

    void HighlightSelected()
    {
        foreach (var row in rows)
        {
            bool isSelected = row.GetComponentInChildren<TextMeshProUGUI>().text == selected;
            row.targetGraphic.color = isSelected ? SelectedRowColor : Color.white;
        }
    }

    void Click(string path)
    {
        if (path == selected && Time.unscaledTime - selectedAt < DoubleClickTime)
        {
            Pick(path);
            return;
        }
        selected = path;
        selectedAt = Time.unscaledTime;
        selectButton.interactable = true;
        HighlightSelected();
        if (preview != null)
            ShowPreview(path);
    }

    void Pick(string path)
    {
        onPick(path);
        popout.Close();
    }

    void OnDestroy() => loaded.Dispose();

    void ShowPreview(string path)
    {
        previewBox.SetActive(true);
        previewInfo.transform.parent.gameObject.SetActive(true);
        loaded.Load(path, kind, 512);
        var texture = loaded.Texture;
        preview.texture = texture;
        preview.color = texture != null ? Color.white : Color.clear;
        if (texture != null)
            previewAspect.aspectRatio = (float)texture.width / texture.height;
        previewInfo.text = loaded.Info;
    }

    /// <summary>GameData-relative paths with the given extensions, scanned once per kind.</summary>
    static string[] Index(string[] extensions, AssetKind kind)
    {
        string key = string.Join("|", extensions) + kind;
        if (Indexes.TryGetValue(key, out var cached))
            return cached;

        string root = Path.GetFullPath(Path.Combine(KSPUtil.ApplicationRootPath, "GameData"));
        var paths = Directory
            .GetFiles(root, "*", SearchOption.AllDirectories)
            .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .Select(f => f.Substring(root.Length + 1).Replace('\\', '/'))
            .ToList();

        if (kind != AssetKind.File)
        {
            // Bundle entries take priority over loose files when loading, so list each path once.
            var files = new HashSet<string>(paths, StringComparer.OrdinalIgnoreCase);
            paths.AddRange(BundleTextures().Where(files.Add));
        }

        if (kind == AssetKind.Texture)
        {
            // GameDatabase textures are files in GameData, already listed by path.
            var database = new HashSet<string>(
                GameDatabase.Instance.databaseTexture.Select(t => t.name),
                StringComparer.OrdinalIgnoreCase
            );
            paths.AddRange(BuiltinTextures.Names.Where(n => !database.Contains(n)).Select(n => Builtin + n));
        }
        else if (kind == AssetKind.Map)
        {
            paths.AddRange(BuiltinTextures.MapNames.Select(n => Builtin + n));
        }

        var index = paths.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
        Indexes[key] = index;
        return index;
    }

    /// <summary>
    /// Textures inside the bundles registered in KSPTextureLoader configs. Assets are named by
    /// their lowercased GameData path, which is also how configs refer to them.
    /// </summary>
    static IEnumerable<string> BundleTextures()
    {
        var bundles = GameDatabase
            .Instance.GetConfigNodes("KSPTextureLoader")
            .SelectMany(n => n.GetNodes("AssetBundle"))
            .Select(n => n.GetValue("bundle"))
            .Where(b => !string.IsNullOrEmpty(b))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var names = new List<string>();
        foreach (var bundle in bundles)
        {
            try
            {
                using var handle = TextureLoader.LoadAssetBundle(bundle);
                names.AddRange(
                    handle
                        .GetBundle()
                        .GetAllAssetNames()
                        .Where(n => TextureExtensions.Contains(Path.GetExtension(n)))
                );
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[KittopiaTech] Couldn't list textures in {bundle}: {e.Message}");
            }
        }
        return names;
    }
}
