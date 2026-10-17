using System;
using System.Collections.Generic;
using KittopiaTech.Model;
using UnityEngine;

namespace KittopiaTech.UI.Editors;

/// <summary>
/// Registers a <see cref="FieldEditor"/> for a type. The type may be closed, an open generic
/// definition, a base class, an interface, or one of the <see cref="Shapes"/> markers.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class FieldEditorAttribute(Type type) : Attribute
{
    public Type Type { get; } = type;

    /// <summary>Higher wins. Core editors use 0, so mods can override with a positive value.</summary>
    public int Priority { get; set; }
}

/// <summary>Marker types for registering editors against a member's shape rather than its type.</summary>
public static class Shapes
{
    public sealed class Node;

    public sealed class Collection;

    public sealed class KeyedCurve;
}

public enum FieldLayout
{
    /// <summary>Sits in the row's value column, next to the label.</summary>
    Inline,

    /// <summary>Takes the full row width, below the label.</summary>
    Wide,
}

public sealed class PrefabContext
{
    internal PrefabContext() { }

    /// <summary>
    /// Builds a nested editor under <paramref name="parent"/> as part of the prefab being built.
    /// Store the result in a [SerializeField] field; Instantiate remaps it on the clone.
    /// </summary>
    public T BuildNested<T>(Transform parent)
        where T : FieldEditor => (T)FieldEditorRegistry.Instantiate(typeof(T), parent);
}

/// <summary>
/// A widget that edits one value. BuildPrefab runs once on the prefab; each instance then gets
/// Awake (wire listeners), Bind/Refresh while in use, and Unbind when it goes away.
///
/// Rules for implementers:
/// - Keep widget references in [SerializeField] fields on the concrete, non-generic class.
/// - Don't add listeners in BuildPrefab: Instantiate does not copy runtime listeners.
/// - Refresh must not call Set; use SetValueWithoutNotify, and listen to onEndEdit rather
///   than onValueChanged on input fields (this TMP version has no SetTextWithoutNotify).
/// - Read the current value inside handlers rather than caching it.
///
/// An editor may declare <c>public static bool CanEdit(MemberSchema member, Type valueType)</c>
/// to reject some members after its type matched.
/// </summary>
public abstract class FieldEditor : MonoBehaviour
{
    protected internal abstract void BuildPrefab(PrefabContext ctx);

    protected IValueSource Source { get; private set; }

    public virtual FieldLayout Layout => FieldLayout.Inline;

    protected virtual void Awake() { }

    public void Bind(IValueSource source)
    {
        Unbind();
        Source = source;
        Source.Changed += OnSourceChanged;
        OnBind();
        Refresh();
    }

    public void Unbind()
    {
        if (Source == null)
            return;

        Source.Changed -= OnSourceChanged;
        OnUnbind();
        Source = null;
    }

    void OnSourceChanged()
    {
        if (Source != null)
            Refresh();
    }

    protected virtual void OnDestroy() => Unbind();

    protected virtual void OnBind() { }

    /// <summary>Close pop-outs and release preview resources here.</summary>
    protected virtual void OnUnbind() { }

    /// <summary>Copies the model into the widgets.</summary>
    public abstract void Refresh();

    /// <summary>
    /// The body and the rows down to this field, e.g. "Kerbin > PQS > Mods > _Height > map",
    /// for pop-out titles. List items show their name where they have one.
    /// </summary>
    protected string FieldPath
    {
        get
        {
            var rows = GetComponentsInParent<Inspector.FieldRow>(true);
            var parts = new List<string> { Source.Scope.Context.CelestialBody.bodyName };
            for (int i = rows.Length - 1; i >= 0; i--)
            {
                var source = rows[i].Source;
                var value = source.Member == null ? source.Get() : null;
                string name = value != null ? Structure.SummaryName(value) : null;
                parts.Add(string.IsNullOrEmpty(name) ? source.Label : name);
            }
            return string.Join(" > ", parts);
        }
    }
}

public abstract class FieldEditor<T> : FieldEditor
{
    protected T Value => Source.Get() is T value ? value : default;

    protected void Commit(T value, EditPhase phase = EditPhase.Final) => Source.Set(value, phase);
}
