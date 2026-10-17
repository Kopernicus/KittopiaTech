using System;
using UnityEngine;

namespace KittopiaTech.Model;

/// <summary>
/// An <see cref="IValueSource"/> that writes straight to its slot on a final edit.
/// Edits made through it are not staged.
/// </summary>
public sealed class DirectValueSource(BodyContext context, ISlot slot, ValueAdapter adapter)
    : IValueSource
{
    public BodyContext Context { get; } = context;
    public ISlot Slot { get; } = slot;
    public ValueAdapter Adapter { get; } = adapter;

    public Type ValueType => Adapter.ValueType;
    public MemberSchema Member => Slot.Member;
    public string Label => Slot.Label;
    public bool IsReadOnly => !Slot.CanWrite;
    public bool IsModified => false;
    public string Error { get; private set; }

    public event Action Changed;

    public object Get()
    {
        using (ParseStateScope.For(Context))
            return Adapter.Unwrap(Slot.Read());
    }

    public void Set(object value, EditPhase phase = EditPhase.Final)
    {
        if (phase != EditPhase.Final || IsReadOnly)
            return;

        try
        {
            using (ParseStateScope.For(Context))
                Slot.Write(Adapter.Wrap(value));
            Error = null;
        }
        catch (Exception e)
        {
            Error = e.Message;
            Debug.LogError($"[KittopiaTech] Failed to set {Slot.Label}: {e}");
        }
        Changed?.Invoke();
    }

    public void Reset() { }

    public void NotifyChanged() => Changed?.Invoke();
}
