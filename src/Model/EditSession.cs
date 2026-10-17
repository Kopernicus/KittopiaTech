using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace KittopiaTech.Model;

/// <summary>
/// The pending edits for one body. Value edits are staged here and only reach the body on
/// Apply. Every window showing the body shares its session.
/// </summary>
public sealed class EditSession : IEditScope
{
    static readonly Dictionary<BodyContext, EditSession> Sessions = [];

    sealed class Pending
    {
        public ISlot Slot;
        public ValueAdapter Adapter;
        public object Value;
        public string Error;
        public int Order;
    }

    readonly Dictionary<ISlot, Pending> pending = [];
    int nextOrder;

    public BodyContext Context { get; }

    /// <summary>Raised whenever pending edits change or the body changes under them.</summary>
    public event Action Changed;

    public int PendingCount => pending.Count;
    public int ErrorCount => pending.Values.Count(p => p.Error != null);

    EditSession(BodyContext context) => Context = context;

    public static EditSession For(BodyContext context)
    {
        if (!Sessions.TryGetValue(context, out var session))
            Sessions[context] = session = new EditSession(context);
        return session;
    }

    public IValueSource For(ISlot slot, ValueAdapter adapter) => new StagedValueSource(this, slot, adapter);

    public void Invalidate() => Changed?.Invoke();

    internal bool TryGetPending(ISlot slot, out object value, out string error)
    {
        bool found = pending.TryGetValue(slot, out var edit);
        value = edit?.Value;
        error = edit?.Error;
        return found;
    }

    internal object ReadLive(ISlot slot, ValueAdapter adapter)
    {
        using (ParseStateScope.For(Context))
            return adapter.Unwrap(slot.Read());
    }

    /// <summary>Stages a value. Setting a slot back to its live value drops the edit.</summary>
    internal void Stage(ISlot slot, ValueAdapter adapter, object value)
    {
        object live;
        try
        {
            live = ReadLive(slot, adapter);
        }
        catch (Exception)
        {
            live = null;
        }

        if (Equals(live, value))
            pending.Remove(slot);
        else
        {
            int order = pending.TryGetValue(slot, out var existing) ? existing.Order : nextOrder++;
            pending[slot] = new Pending { Slot = slot, Adapter = adapter, Value = value, Order = order };
        }
        Changed?.Invoke();
    }

    internal void Unstage(ISlot slot)
    {
        if (pending.Remove(slot))
            Changed?.Invoke();
    }

    public void Discard()
    {
        pending.Clear();
        Changed?.Invoke();
    }

    /// <summary>Writes every pending edit. Edits that fail stay pending with their error.</summary>
    public void Apply() => Apply(pending.Values.ToList());

    /// <summary>Applies only the edits on objects reachable from <paramref name="root"/>.</summary>
    public void ApplyWithin(object root)
    {
        var owners = Reachable(root);
        Apply(pending.Values.Where(p => owners.Contains(p.Slot.Owner)).ToList());
    }

    /// <summary>Drops edits on objects reachable from <paramref name="root"/>, e.g. before removing it.</summary>
    public void DiscardWithin(object root)
    {
        var owners = Reachable(root);
        foreach (var edit in pending.Values.Where(p => owners.Contains(p.Slot.Owner)).ToList())
            pending.Remove(edit.Slot);
        Changed?.Invoke();
    }

    void Apply(List<Pending> edits)
    {
        if (edits.Count == 0)
            return;

        // PreApply members first, as the parser does; otherwise in the order they were made.
        var ordered = edits.OrderByDescending(p => p.Slot.Member?.PreApply ?? false).ThenBy(p => p.Order);
        using (ParseStateScope.For(Context))
        {
            foreach (var edit in ordered)
            {
                try
                {
                    edit.Slot.Write(edit.Adapter.Wrap(edit.Value));
                    pending.Remove(edit.Slot);
                }
                catch (Exception e)
                {
                    edit.Error = e.Message;
                    Debug.LogError($"[KittopiaTech] Failed to apply {edit.Slot.Label}: {e}");
                }
            }
        }
        Changed?.Invoke();
    }

    /// <summary>The object and every node or list item beneath it.</summary>
    static HashSet<object> Reachable(object root)
    {
        var seen = new HashSet<object>(ReferenceComparer.Instance);
        var queue = new Queue<object>();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == null || !seen.Add(current))
                continue;

            if (current is IList list)
            {
                foreach (var item in list)
                    queue.Enqueue(item);
                continue;
            }

            foreach (var member in TypeSchema.Get(current.GetType()).Members)
            {
                if (member.Shape == MemberShape.Value || !member.CanRead)
                    continue;
                try
                {
                    queue.Enqueue(member.GetValue(current));
                }
                catch (Exception)
                {
                    // Some getters throw on partially set up bodies; skip them.
                }
            }
        }
        return seen;
    }

    sealed class ReferenceComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceComparer Instance = new();

        public new bool Equals(object x, object y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}

/// <summary>A value source whose writes are staged in an <see cref="EditSession"/>.</summary>
public sealed class StagedValueSource(EditSession session, ISlot slot, ValueAdapter adapter) : IValueSource
{
    public IEditScope Scope => session;
    public ISlot Slot { get; } = slot;
    public ValueAdapter Adapter { get; } = adapter;

    public Type ValueType => Adapter.ValueType;
    public MemberSchema Member => Slot.Member;
    public string Label => Slot.Label;
    public bool IsReadOnly => !Slot.CanWrite;
    public bool IsModified => session.TryGetPending(Slot, out _, out _);
    public string Error => session.TryGetPending(Slot, out _, out var error) ? error : null;

    // Forwarded, so unbinding an editor leaves nothing subscribed to the long-lived session.
    public event Action Changed
    {
        add => session.Changed += value;
        remove => session.Changed -= value;
    }

    public object Get() =>
        session.TryGetPending(Slot, out var value, out _) ? value : session.ReadLive(Slot, Adapter);

    public void Set(object value, EditPhase phase = EditPhase.Final)
    {
        if (!IsReadOnly)
            session.Stage(Slot, Adapter, value);
    }

    public void Reset() => session.Unstage(Slot);
}
