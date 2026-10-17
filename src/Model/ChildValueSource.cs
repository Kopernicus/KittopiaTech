using System;

namespace KittopiaTech.Model;

/// <summary>
/// One part of a composite value (a vector component, a colour channel). Reads and writes go
/// through the parent source, so staging and errors behave exactly as they do for the parent.
/// </summary>
public sealed class ChildValueSource(
    IValueSource parent,
    string label,
    Type valueType,
    Func<object, object> get,
    Func<object, object, object> set
) : IValueSource
{
    public Type ValueType { get; } = valueType;
    public string Label { get; } = label;
    public IEditScope Scope => parent.Scope;
    public MemberSchema Member => parent.Member;
    public bool IsReadOnly => parent.IsReadOnly;
    public bool IsModified => parent.IsModified;
    public string Error => parent.Error;

    public event Action Changed
    {
        add => parent.Changed += value;
        remove => parent.Changed -= value;
    }

    public object Get() => get(parent.Get());

    /// <summary>Re-reads the parent at the moment of the edit, so other components aren't lost.</summary>
    public void Set(object value, EditPhase phase = EditPhase.Final) =>
        parent.Set(set(parent.Get(), value), phase);

    public void Reset() => parent.Reset();
}
