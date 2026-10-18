using System;
using System.Linq;
using KittopiaTech.Model;
using KittopiaTech.UI.Debug;
using KittopiaTech.UI.Editors;
using KittopiaTech.UI.Inspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI;

/// <summary>A pop-out that copies a body under a new name, optionally orbiting another body.</summary>
public sealed class AddBodyForm : MonoBehaviour
{
    InspectorWindow window;
    Popout popout;
    CelestialBody template;
    CelestialBody parent; // null: whatever the template orbits
    TextMeshProUGUI templateText;
    TextMeshProUGUI parentText;
    TMP_InputField nameField;
    TextMeshProUGUI status;
    Popout picker;

    public static Popout Open(InspectorWindow window)
    {
        var popout = Popout.Open(window, "Add Body", new Vector2(380f, 190f));
        var form = popout.gameObject.AddComponent<AddBodyForm>();
        form.window = window;
        form.popout = popout;
        form.template = window.Body != null && window.Body.orbitDriver != null ? window.Body : null;
        form.Build(popout.Content);
        form.UpdateLabels();
        return popout;
    }

    void Build(Transform content)
    {
        templateText = Row(content, "Template", () => Pick("Template", true));
        var nameRow = DebugSkin.CreateHorizontalLayout(content, 6f, controlWidth: true);
        Label(nameRow.transform, "Name");
        nameField = DebugSkin.CreateInputField(nameRow.transform);
        nameField.name = "Name";
        nameField.onValueChanged.AddListener(_ => UpdateLabels());
        EditorLayout.Compact(nameField.gameObject);
        parentText = Row(content, "Orbits", () => Pick("Orbits", false));

        status = DebugSkin.CreateLabel(content, "");
        FitTextHeight.Attach(status);

        var footer = DebugSkin.CreateHorizontalLayout(content, 6f, controlWidth: true);
        DebugSkin.CreateSpacer(footer.transform, 0f).flexibleWidth = 1f;
        var create = DebugSkin.CreateButton(footer.transform, "Create", Create);
        create.name = "Create";
        var layout = create.GetComponent<LayoutElement>();
        layout.preferredWidth = 100f;
        layout.flexibleWidth = 0f;
        nameField.ActivateInputField();
    }

    static TextMeshProUGUI Row(Transform content, string label, Action onClick)
    {
        var row = DebugSkin.CreateHorizontalLayout(content, 6f, controlWidth: true);
        Label(row.transform, label);
        var button = EditorLayout.PathButton(row.transform, out var text);
        button.name = label;
        button.onClick.AddListener(() => onClick());
        return text;
    }

    static void Label(Transform row, string text)
    {
        var label = DebugSkin.CreateLabel(row, text);
        var layout = label.transform.parent.GetComponent<LayoutElement>();
        layout.minWidth = layout.preferredWidth = 80f;
        layout.flexibleWidth = 0f;
    }

    void Pick(string title, bool isTemplate)
    {
        if (picker != null)
            return;
        var bodies = FlightGlobals.Bodies.Where(b => !Bodies.IsDeleted(b));
        if (isTemplate)
            bodies = bodies.Where(b => b.orbitDriver != null);
        var names = bodies.Select(b => b.transform.name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase);
        string current = isTemplate ? template?.transform.name : (parent ?? template?.referenceBody)?.transform.name;
        picker = AssetPicker.OpenList(this, title, [.. names], current, name =>
        {
            var body = FlightGlobals.Bodies.FirstOrDefault(b => b.transform.name == name && !Bodies.IsDeleted(b));
            if (isTemplate)
                template = body;
            else
                parent = body;
            UpdateLabels();
        });
    }

    CelestialBody Parent => parent ?? template?.referenceBody;

    void UpdateLabels()
    {
        templateText.text = template != null ? template.transform.name : "Pick a body to copy";
        parentText.text = parent != null ? parent.transform.name
            : template != null ? $"{template.referenceBody.transform.name} (as {template.transform.name})"
            : "";
        status.text = nameField.text.Length > 0 ? Bodies.CantCreate(template, nameField.text.Trim(), Parent) ?? "" : "";
        status.color = FieldRow.ErrorColor;
    }

    void Create()
    {
        string name = nameField.text.Trim();
        string problem = Bodies.CantCreate(template, name, Parent);
        if (problem != null)
        {
            status.text = problem;
            return;
        }
        try
        {
            var body = Bodies.Create(template, name, Parent);
            window.Select(body);
            popout.Close();
        }
        catch (Exception e)
        {
            status.text = "Couldn't create the body, see log";
            UnityEngine.Debug.LogError($"[KittopiaTech] Creating {name} failed: {e}");
        }
    }
}
