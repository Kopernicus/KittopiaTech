using System;
using System.Collections.Generic;
using System.Linq;
using KittopiaTech.Model;
using KittopiaTech.UI.Debug;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Editors;

/// <summary>
/// Up to four nested Number editors, each bound to one component of the value. Fixes to the
/// Number editor therefore reach every vector, quaternion and colour.
/// </summary>
public abstract class CompositeFieldEditor : FieldEditor<object>
{
    const int MaxParts = 4;

    [SerializeField]
    NumberFieldEditor[] parts;

    [SerializeField]
    TextMeshProUGUI[] labels;

    /// <param name="names">One label per component; a string gives one character each.</param>
    protected sealed class Components(
        string[] names,
        Type type,
        Func<object, int, object> get,
        Func<object, int, object, object> set
    )
    {
        public string[] Names { get; } = names;

        public Components(string names, Type type, Func<object, int, object> get, Func<object, int, object, object> set)
            : this(names.Select(c => c.ToString()).ToArray(), type, get, set) { }
        public Type Type { get; } = type;
        public Func<object, int, object> Get { get; } = get;
        public Func<object, int, object, object> Set { get; } = set;
    }

    protected abstract Components For(Type valueType);

    protected internal override void BuildPrefab(PrefabContext ctx)
    {
        EditorLayout.Row(transform, 4f);
        BuildPrefix(transform);

        parts = new NumberFieldEditor[MaxParts];
        labels = new TextMeshProUGUI[MaxParts];
        for (int i = 0; i < MaxParts; i++)
        {
            labels[i] = DebugSkin.CreateLabel(transform, "");
            var labelLayout = labels[i].transform.parent.GetComponent<LayoutElement>();
            labelLayout.minWidth = labelLayout.preferredWidth = 14f;
            labelLayout.flexibleWidth = 0f;
            labels[i].alignment = TextAlignmentOptions.MidlineRight;

            parts[i] = ctx.BuildNested<NumberFieldEditor>(transform);
        }
    }

    /// <summary>Widgets placed before the components, e.g. a colour swatch.</summary>
    protected virtual void BuildPrefix(Transform parent) { }

    protected override void OnBind()
    {
        var components = For(Source.ValueType);
        for (int i = 0; i < MaxParts; i++)
        {
            bool used = i < components.Names.Length;
            labels[i].transform.parent.gameObject.SetActive(used);
            parts[i].gameObject.SetActive(used);
            if (!used)
                continue;

            int index = i;
            labels[i].text = components.Names[i];
            var labelLayout = labels[i].transform.parent.GetComponent<LayoutElement>();
            labelLayout.minWidth = labelLayout.preferredWidth = Mathf.Max(14f, labels[i].preferredWidth + 2f);
            parts[i].Bind(
                new ChildValueSource(
                    Source,
                    components.Names[i],
                    components.Type,
                    v => components.Get(v, index),
                    (v, c) => components.Set(v, index, c)
                )
            );
        }
    }

    protected override void OnUnbind()
    {
        foreach (var part in parts)
            part.Unbind();
    }

    // The nested editors refresh themselves from the shared Changed event.
    public override void Refresh() { }
}

[FieldEditor(typeof(Vector2))]
[FieldEditor(typeof(Vector3))]
[FieldEditor(typeof(Vector4))]
[FieldEditor(typeof(Vector2d))]
[FieldEditor(typeof(Vector3d))]
[FieldEditor(typeof(Quaternion))]
[FieldEditor(typeof(QuaternionD))]
public sealed class VectorFieldEditor : CompositeFieldEditor
{
    static readonly Dictionary<Type, Components> Table = new()
    {
        [typeof(Vector2)] = new("XY", typeof(float), (v, i) => ((Vector2)v)[i], (v, i, c) =>
        {
            var x = (Vector2)v;
            x[i] = (float)c;
            return x;
        }),
        [typeof(Vector3)] = new("XYZ", typeof(float), (v, i) => ((Vector3)v)[i], (v, i, c) =>
        {
            var x = (Vector3)v;
            x[i] = (float)c;
            return x;
        }),
        [typeof(Vector4)] = new("XYZW", typeof(float), (v, i) => ((Vector4)v)[i], (v, i, c) =>
        {
            var x = (Vector4)v;
            x[i] = (float)c;
            return x;
        }),
        [typeof(Vector2d)] = new("XY", typeof(double), (v, i) => i == 0 ? ((Vector2d)v).x : ((Vector2d)v).y, (v, i, c) =>
        {
            var x = (Vector2d)v;
            if (i == 0)
                x.x = (double)c;
            else
                x.y = (double)c;
            return x;
        }),
        [typeof(Vector3d)] = new("XYZ", typeof(double), (v, i) => ((Vector3d)v)[i], (v, i, c) =>
        {
            var x = (Vector3d)v;
            x[i] = (double)c;
            return x;
        }),
        // Quaternions are edited as Euler angles in degrees.
        [typeof(Quaternion)] = new("XYZ", typeof(float), (v, i) => ((Quaternion)v).eulerAngles[i], (v, i, c) =>
        {
            var e = ((Quaternion)v).eulerAngles;
            e[i] = (float)c;
            return Quaternion.Euler(e);
        }),
        [typeof(QuaternionD)] = new("XYZW", typeof(double), (v, i) => ((QuaternionD)v)[i], (v, i, c) =>
        {
            var x = (QuaternionD)v;
            x[i] = (double)c;
            return x;
        }),
    };

    protected override Components For(Type valueType) => Table[valueType];
}

[FieldEditor(typeof(Color))]
public sealed class ColorFieldEditor : CompositeFieldEditor
{
    static readonly Components Channels = new("RGBA", typeof(float), (v, i) => ((Color)v)[i], (v, i, c) =>
    {
        var x = (Color)v;
        x[i] = (float)c;
        return x;
    });

    [SerializeField]
    Image swatch;

    ColorPicker picker;

    protected override Components For(Type valueType) => Channels;

    protected override void BuildPrefix(Transform parent)
    {
        var go = new GameObject("Swatch", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        swatch = go.AddComponent<Image>();
        var layout = go.AddComponent<LayoutElement>();
        layout.minWidth = layout.preferredWidth = 36f;
        layout.minHeight = layout.preferredHeight = EditorLayout.RowHeight - 4f;
        go.AddComponent<Button>().targetGraphic = swatch;
    }

    protected override void Awake()
    {
        base.Awake();
        swatch.GetComponent<Button>().onClick.AddListener(OpenPicker);
    }

    protected override void OnUnbind()
    {
        base.OnUnbind();
        picker?.Close();
    }

    void OpenPicker()
    {
        if (picker != null || Source.IsReadOnly)
            return;
        var current = Source.Get() is Color color ? color : Color.white;
        picker = ColorPicker.Open(this, FieldPath, current,
            (c, final) => Source.Set(c, final ? EditPhase.Final : EditPhase.Preview));
    }

    public override void Refresh()
    {
        base.Refresh();
        var c = Source.Get() is Color color ? color : Color.white;
        swatch.color = new Color(c.r, c.g, c.b, 1f);
        if (picker != null)
            picker.SetColor(c);
    }
}

/// <summary>
/// Positions as latitude, longitude and altitude. The loaders' getters return copies, so each
/// edit writes a whole new PositionParser back. "Vessel" copies the active vessel's position.
/// </summary>
[FieldEditor(typeof(Kopernicus.Configuration.Parsing.PositionParser))]
public sealed class PositionFieldEditor : CompositeFieldEditor
{
    static readonly Components Parts = new(["Lat", "Lon", "Alt"], typeof(double), Get, Set);

    [SerializeField]
    Button vesselButton;

    protected override Components For(Type valueType) => Parts;

    static object Get(object value, int i)
    {
        var p = value as Kopernicus.Configuration.Parsing.PositionParser;
        return p == null ? 0.0 : i switch
        {
            0 => p.Latitude.Value,
            1 => p.Longitude.Value,
            _ => p.Altitude.Value,
        };
    }

    static object Set(object value, int i, object component) => Make(
        i == 0 ? (double)component : (double)Get(value, 0),
        i == 1 ? (double)component : (double)Get(value, 1),
        i == 2 ? (double)component : (double)Get(value, 2)
    );

    static Kopernicus.Configuration.Parsing.PositionParser Make(double lat, double lon, double alt) =>
        new() { Latitude = lat, Longitude = lon, Altitude = alt };

    protected override void BuildPrefix(Transform parent)
    {
        vesselButton = DebugSkin.CreateButton(parent, "Vessel");
        vesselButton.name = "Vessel";
        EditorLayout.Compact(vesselButton.gameObject, 0f);
        vesselButton.GetComponent<LayoutElement>().preferredWidth = 56f;
    }

    protected override void Awake() => vesselButton.onClick.AddListener(UseVessel);

    void UseVessel()
    {
        var vessel = FlightGlobals.ActiveVessel;
        if (vessel != null && vessel.mainBody == Source.Scope.Context.CelestialBody)
            Commit(Make(vessel.latitude, vessel.longitude, vessel.altitude));
    }

    public override void Refresh()
    {
        var vessel = FlightGlobals.ActiveVessel;
        vesselButton.interactable =
            !Source.IsReadOnly && vessel != null && vessel.mainBody == Source.Scope.Context.CelestialBody;
    }
}
