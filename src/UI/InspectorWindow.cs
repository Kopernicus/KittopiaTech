using System.Collections.Generic;
using System.Linq;
using KittopiaTech.UI.Debug;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI;

/// <summary>
/// A standalone editor window: body tree on the left, inspector for the selected body on the right.
/// </summary>
public class InspectorWindow : MonoBehaviour
{
    static readonly List<InspectorWindow> Windows = [];

    TextMeshProUGUI title;
    Transform treeContent;
    Transform inspectorContent;
    readonly Dictionary<CelestialBody, TextMeshProUGUI> treeLabels = [];

    public CelestialBody Body { get; private set; }

    /// <summary>Shows or hides every window, opening one if none exist.</summary>
    public static void Toggle()
    {
        Windows.RemoveAll(w => w == null);
        if (Windows.Count == 0)
        {
            Open(FlightGlobals.currentMainBody ?? FlightGlobals.GetHomeBody());
            return;
        }

        bool show = !Windows.Any(w => w.gameObject.activeSelf);
        foreach (var window in Windows)
            window.gameObject.SetActive(show);
    }

    public static InspectorWindow Open(CelestialBody body)
    {
        var parts = DebugSkin.CreateWindow("Kittopia", DebugSkin.Canvas, new Vector2(900f, 640f));
        var window = parts.Window.AddComponent<InspectorWindow>();
        window.title = parts.Title;
        parts.CloseButton.onClick.AddListener(window.Close);
        window.Build(parts.Content);

        // Cascade new windows so they don't stack exactly.
        ((RectTransform)parts.Window.transform).anchoredPosition +=
            new Vector2(24f, -24f) * (Windows.Count % 8);

        Windows.Add(window);
        parts.Window.SetActive(true);
        window.Select(body);
        return window;
    }

    public void Close()
    {
        Windows.Remove(this);
        Destroy(gameObject);
    }

    void Build(Transform content)
    {
        var split = DebugSkin.CreateHorizontalLayout(content, 6f, controlWidth: true);
        split.childForceExpandHeight = true;
        split.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

        var tree = DebugSkin.CreateVerticalLayout(split.transform);
        var treeLayout = tree.gameObject.AddComponent<LayoutElement>();
        treeLayout.preferredWidth = 190f;
        treeLayout.flexibleWidth = 0f;
        DebugSkin.CreateScrollView(tree.transform, out treeContent);

        var inspector = DebugSkin.CreateVerticalLayout(split.transform);
        inspector.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        DebugSkin.CreateScrollView(inspector.transform, out inspectorContent);

        BuildTree();
    }

    void BuildTree()
    {
        var bodies = PSystemManager.Instance.localBodies;
        foreach (var root in bodies.Where(b => b.referenceBody == null || b.referenceBody == b))
            AddTreeEntry(root, 0);
    }

    void AddTreeEntry(CelestialBody body, int depth)
    {
        var row = DebugSkin.CreateHorizontalLayout(treeContent, 0f, controlWidth: true);
        if (depth > 0)
            DebugSkin.CreateSpacer(row.transform, 0f).minWidth = 12f * depth;

        var button = DebugSkin.CreateButton(row.transform, body.displayName.LocalizeRemoveGender(), () => Select(body));
        button.GetComponent<LayoutElement>().minHeight = 22f;
        var label = button.GetComponentInChildren<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.margin = new Vector4(6f, 0f, 6f, 0f);
        treeLabels[body] = label;

        var children = body.orbitingBodies
            .Where(b => b != body)
            .OrderBy(b => b.orbit?.semiMajorAxis ?? 0.0);
        foreach (var child in children)
            AddTreeEntry(child, depth + 1);
    }

    public void Select(CelestialBody body)
    {
        Body = body;
        foreach (var (cb, label) in treeLabels.Select(kv => (kv.Key, kv.Value)))
            label.fontStyle = cb == body ? FontStyles.Bold : FontStyles.Normal;

        string name = body != null ? body.displayName.LocalizeRemoveGender() : "";
        title.text = body != null ? $"Kittopia · {name}" : "Kittopia";

        foreach (Transform child in inspectorContent)
            Destroy(child.gameObject);
        if (body != null)
            DebugSkin.CreateHeader(inspectorContent, name);
    }
}
