using System;
using System.Linq;
using KittopiaTech.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Editors;

/// <summary>Text members that name a body, such as an orbit's referenceBody, picked from the bodies in game.</summary>
[FieldEditor(typeof(string), Priority = 1)]
public sealed class BodyFieldEditor : FieldEditor<string>
{
    [SerializeField]
    Button path;

    [SerializeField]
    TextMeshProUGUI pathText;

    Popout picker;

    public static bool CanEdit(MemberSchema member, Type valueType) =>
        valueType == typeof(string) && member?.FieldName is "referenceBody" or "body" or "mainMenuBody";

    protected internal override void BuildPrefab(PrefabContext ctx)
    {
        EditorLayout.Row(transform);
        path = EditorLayout.PathButton(transform, out pathText);
    }

    protected override void Awake() => path.onClick.AddListener(Browse);

    protected override void OnUnbind() => picker?.Close();

    void Browse()
    {
        if (picker != null || Source.IsReadOnly)
            return;

        var bodies = FlightGlobals.Bodies.AsEnumerable();
        // A body can't orbit itself or its own moons.
        if (Source.Member?.FieldName == "referenceBody")
            bodies = bodies.Where(b => !Bodies.Orbits(b, Source.Scope.Context.CelestialBody));
        var names = bodies.Select(b => b.transform.name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase);
        picker = AssetPicker.OpenList(this, FieldPath, [.. names], Value, Pick);
    }

    void Pick(string name)
    {
        if (name != Value)
            Commit(name);
    }

    public override void Refresh()
    {
        pathText.text = Value ?? "";
        path.interactable = !Source.IsReadOnly;
        EditorLayout.MarkInvalid(path, Source.Error != null);
    }
}
