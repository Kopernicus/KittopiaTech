using System.Collections.Generic;
using System.Linq;
using KittopiaTech.Export;
using KittopiaTech.Model;
using KittopiaTech.UI.Debug;
using KittopiaTech.UI.Inspector;
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
    TextMeshProUGUI pendingLabel;
    TextMeshProUGUI exportLabel;
    Button applyButton;
    Button discardButton;
    Button exportMapsButton;
    Tooltip exportMapsTooltip;
    EditSession session;
    Popout historyPopout;
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

    /// <summary>
    /// Rebuilds the windows showing any of <paramref name="bodies"/> on their next update, once
    /// a hot reload has finished swapping components. Hidden windows rebuild when next shown.
    /// </summary>
    public static void RebuildShowing(ICollection<CelestialBody> bodies)
    {
        foreach (var window in Windows.Where(w => w != null && bodies.Contains(w.Body)))
            window.rebuild = true;
    }

    bool rebuild;

    void Update()
    {
        if (!rebuild)
            return;
        rebuild = false;
        Select(Body);
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
        var search = DebugSkin.CreateInputField(tree.transform);
        search.name = "Search";
        if (search.placeholder is TMP_Text placeholder)
            placeholder.text = "Search bodies";
        search.onValueChanged.AddListener(FilterTree);
        DebugSkin.CreateScrollView(tree.transform, out treeContent);

        DebugSkin.CreateSeparator(split.transform, vertical: true);

        var inspector = DebugSkin.CreateVerticalLayout(split.transform);
        inspector.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        DebugSkin.CreateScrollView(inspector.transform, out inspectorContent);

        DebugSkin.CreateSeparator(content, vertical: false);
        BuildFooter(content);
        BuildTree();
    }

    void BuildFooter(Transform content)
    {
        var footer = DebugSkin.CreateHorizontalLayout(content, 6f, controlWidth: true);
        footer.name = "Footer";
        var exportBody = DebugSkin.CreateButton(footer.transform, "Export body", () => Export(patch: false));
        var exportPatch = DebugSkin.CreateButton(footer.transform, "Export patch", () => Export(patch: true));
        exportMapsButton = DebugSkin.CreateButton(footer.transform, "Export scaled maps", () => BurstPQSExporter.Open(Body));
        Tooltip.Attach(exportMapsButton.gameObject, "Opens BurstPQS's Texture Exporter for this body");
        exportMapsTooltip = exportMapsButton.GetComponent<Tooltip>();
        pendingLabel = DebugSkin.CreateLabel(footer.transform, "");
        pendingLabel.alignment = TextAlignmentOptions.MidlineRight;
        pendingLabel.color = PendingColor;
        var history = DebugSkin.CreateButton(footer.transform, "History", ToggleHistory);
        Tooltip.Attach(history.gameObject, "Restore the body to the state after an earlier Apply");
        discardButton = DebugSkin.CreateButton(footer.transform, "Discard", () => session?.Discard());
        applyButton = DebugSkin.CreateButton(footer.transform, "Apply", () => session?.Apply());
        foreach (var button in new[] { exportBody, exportPatch, exportMapsButton, history, discardButton, applyButton })
        {
            button.name = button.GetComponentInChildren<TextMeshProUGUI>().text.Replace(" ", "");
            var layout = button.GetComponent<LayoutElement>();
            layout.flexibleWidth = 0f;
            layout.preferredWidth = button == exportMapsButton ? 130f : 100f;
        }

        // Its own row under the buttons, which leave too little room for a path.
        exportLabel = DebugSkin.CreateLabel(content, "");
        FitTextHeight.Attach(exportLabel);
        ShowStatus(null);
    }

    /// <summary>Shows the result of an export under the footer, or hides the row for null.</summary>
    void ShowStatus(string text, bool error = false)
    {
        exportLabel.text = text ?? "";
        exportLabel.color = error ? FieldRow.ErrorColor : Color.white;
        exportLabel.transform.parent.gameObject.SetActive(text != null);
    }

    void ToggleHistory()
    {
        if (historyPopout != null)
            historyPopout.Close();
        else if (session != null)
            historyPopout = HistoryWindow.Open(this, session);
    }

    /// <summary>Exports the applied state; pending edits aren't included.</summary>
    void Export(bool patch)
    {
        if (session == null)
            return;

        string skipped = session.PendingCount > 0 ? $" ({session.PendingCount} pending not included)" : "";
        try
        {
            string path;
            string warning = null;
            if (patch)
                path = ExportFiles.ExportPatch(session.Context, out warning);
            else
                path = ExportFiles.ExportBody(session.Context);

            if (path == null)
            {
                ShowStatus(warning ?? "No Template to patch against; use Export body", error: true);
                return;
            }
            ShowStatus($"Saved {path} (full path copied){skipped}" + (warning != null ? $"\n{warning}" : ""));
            if (warning != null)
                UnityEngine.Debug.LogWarning($"[KittopiaTech] Patch for {Body.name}: {warning}");
        }
        catch (System.Exception e)
        {
            ShowStatus("Export failed, see log", error: true);
            UnityEngine.Debug.LogError($"[KittopiaTech] Export failed: {e}");
        }
    }

    public static readonly Color PendingColor = new(1f, 0.71f, 0.28f);

    void UpdateFooter()
    {
        int count = session?.PendingCount ?? 0;
        int errors = session?.ErrorCount ?? 0;
        pendingLabel.text = count == 0
            ? ""
            : errors == 0 ? $"{count} pending" : $"{count} pending, {errors} failed";
        applyButton.interactable = discardButton.interactable = count > 0;
    }

    void OnDestroy()
    {
        if (session != null)
            session.Changed -= UpdateFooter;
        EditSession.AnyChanged -= UpdateTreeEntry;
    }

    void BuildTree()
    {
        var bodies = PSystemManager.Instance.localBodies;
        foreach (var root in bodies.Where(b => b.referenceBody == null || b.referenceBody == b))
            AddTreeEntry(root, 0);

        foreach (var existing in EditSession.All)
            UpdateTreeEntry(existing);
        EditSession.AnyChanged += UpdateTreeEntry;
    }

    /// <summary>Bodies with pending edits are listed in the pending colour.</summary>
    void UpdateTreeEntry(EditSession changed)
    {
        if (treeLabels.TryGetValue(changed.Context.CelestialBody, out var label))
            label.color = changed.PendingCount > 0 ? PendingColor : Color.white;
    }

    void AddTreeEntry(CelestialBody body, int depth)
    {
        var row = DebugSkin.CreateHorizontalLayout(treeContent, 0f, controlWidth: true);
        if (depth > 0)
            DebugSkin.CreateSpacer(row.transform, 0f).minWidth = 12f * depth;

        var button = DebugSkin.CreateButton(row.transform, body.displayName.LocalizeRemoveGender(), () => Select(body));
        button.GetComponent<LayoutElement>().minHeight = 22f;
        DebugSkin.StyleAsSidebarItem(button);
        var label = button.GetComponentInChildren<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.margin = new Vector4(6f, 0f, 6f, 0f);
        treeLabels[body] = label;
        treeRows[body] = row.gameObject;

        var children = body.orbitingBodies
            .Where(b => b != body)
            .OrderBy(b => b.orbit?.semiMajorAxis ?? 0.0);
        foreach (var child in children)
            AddTreeEntry(child, depth + 1);
    }

    readonly Dictionary<CelestialBody, GameObject> treeRows = [];

    /// <summary>Shows the bodies whose name contains every word typed, and what they orbit.</summary>
    void FilterTree(string text)
    {
        var terms = text.ToLowerInvariant().Split([' '], System.StringSplitOptions.RemoveEmptyEntries);
        var shown = new HashSet<CelestialBody>();
        foreach (var (body, label) in treeLabels.Select(kv => (kv.Key, kv.Value)))
        {
            string name = label.text.ToLowerInvariant();
            if (!terms.All(name.Contains))
                continue;
            // Walk up to the root so a match keeps its place in the tree.
            for (var b = body; b != null && shown.Add(b); b = b.referenceBody == b ? null : b.referenceBody) { }
        }
        foreach (var (body, row) in treeRows.Select(kv => (kv.Key, kv.Value)))
            row.SetActive(shown.Contains(body));
    }

    public void Select(CelestialBody body)
    {
        Body = body;
        foreach (var (cb, label) in treeLabels.Select(kv => (kv.Key, kv.Value)))
        {
            label.fontStyle = cb == body ? FontStyles.Bold : FontStyles.Normal;
            DebugSkin.SetSidebarItemSelected(label.GetComponentInParent<Button>(), cb == body);
        }

        string name = body != null ? body.displayName.LocalizeRemoveGender() : "";
        title.text = body != null ? $"Kittopia · {name}" : "Kittopia";

        InspectorBuilder.Clear(inspectorContent);
        if (session != null)
            session.Changed -= UpdateFooter;
        session = null;
        // An open history follows the selection to the new body's history.
        bool showHistory = historyPopout != null;
        if (showHistory)
            historyPopout.Close();

        if (body != null)
        {
            var context = BodyContext.Get(body);
            session = EditSession.For(context);
            session.Changed += UpdateFooter;
            InspectorBuilder.BuildMembers(inspectorContent, session, context.Body);
            if (showHistory)
                historyPopout = HistoryWindow.Open(this, session);
        }
        ShowStatus(null);
        string unavailable = BurstPQSExporter.Unavailable(body);
        exportMapsButton.interactable = unavailable == null;
        exportMapsTooltip.text = unavailable ?? "Opens BurstPQS's Texture Exporter for this body";
        UpdateFooter();
    }
}
