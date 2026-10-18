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
    HashSet<object> pendingWithin;

    public BodyContext Context { get; }

    /// <summary>Raised whenever pending edits change or the body changes under them.</summary>
    public event Action Changed;

    /// <summary>Raised after <see cref="Changed"/> on any session, e.g. for the body list.</summary>
    public static event Action<EditSession> AnyChanged;

    public int PendingCount => pending.Count;
    public int ErrorCount => pending.Values.Count(p => p.Error != null);

    EditSession(BodyContext context)
    {
        Context = context;
        Changed += () =>
        {
            pendingWithin = null;
            AnyChanged?.Invoke(this);
        };
    }

    public static IEnumerable<EditSession> All => Sessions.Values;

    public static EditSession For(BodyContext context)
    {
        if (!Sessions.TryGetValue(context, out var session))
            Sessions[context] = session = new EditSession(context);
        return session;
    }

    public IValueSource For(ISlot slot, ValueAdapter adapter) => new StagedValueSource(this, slot, adapter);

    public void Invalidate()
    {
        PruneCreated();
        Changed?.Invoke();
    }

    public void MarkCreated(object created, string label, Action undo)
    {
        var slot = new CreatedSlot(created, label, undo);
        pending[slot] = new Pending
        {
            Slot = slot, Adapter = ValueAdapter.Identity(typeof(object)), Order = nextOrder++,
        };
        Changed?.Invoke();
    }

    /// <summary>Drops marks on created objects that have since been removed from the body.</summary>
    void PruneCreated()
    {
        if (!pending.Keys.OfType<CreatedSlot>().Any())
            return;
        var reachable = Reachable(Context.Body);
        foreach (var slot in pending.Keys.OfType<CreatedSlot>().Where(s => !reachable.Contains(s.Owner)).ToList())
            pending.Remove(slot);
    }

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

        if (SameValue(live, value))
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

    /// <summary>Lists compare by their items, so reordering back drops the edit.</summary>
    static bool SameValue(object a, object b)
    {
        if (a is not IList x || b is not IList y)
            return Equals(a, b);
        if (x.Count != y.Count)
            return false;
        for (int i = 0; i < x.Count; i++)
        {
            if (!Equals(x[i], y[i]))
                return false;
        }
        return true;
    }

    /// <summary>Whether a pending edit sits on <paramref name="node"/> or anywhere beneath it.</summary>
    public bool HasPendingWithin(object node)
    {
        if (pending.Count == 0 || node == null)
            return false;
        pendingWithin ??= FindPendingWithin();
        return pendingWithin.Contains(node);
    }

    /// <summary>Every object on a path from the body to an edited object.</summary>
    HashSet<object> FindPendingWithin()
    {
        var owners = new HashSet<object>(pending.Keys.Select(s => s.Owner), ReferenceComparer.Instance);
        var result = new HashSet<object>(ReferenceComparer.Instance);
        var seen = new HashSet<object>(ReferenceComparer.Instance);
        var path = new List<object>();

        void Visit(object node)
        {
            if (!seen.Add(node))
                return;
            path.Add(node);
            if (owners.Contains(node))
                result.UnionWith(path);
            foreach (var child in Children(node))
                Visit(child);
            path.RemoveAt(path.Count - 1);
        }

        Visit(Context.Body);
        return result;
    }

    public void Discard()
    {
        // Newest first, so objects created inside other new objects go before their parents.
        PruneCreated();
        foreach (var slot in pending.Values.OrderByDescending(p => p.Order).Select(p => p.Slot).OfType<CreatedSlot>())
        {
            try
            {
                using (ParseStateScope.For(Context))
                    slot.Undo();
            }
            catch (Exception e)
            {
                Debug.LogError($"[KittopiaTech] Failed to remove new {slot.Label}: {e}");
            }
        }
        pending.Clear();
        Changed?.Invoke();
    }

    /// <summary>Writes every pending edit. Edits that fail stay pending with their error.</summary>
    public void Apply()
    {
        PruneCreated();
        Apply(pending.Values.ToList(), runHooks: true);
    }

    /// <summary>Applies only the edits on objects reachable from <paramref name="root"/>.</summary>
    public void ApplyWithin(object root)
    {
        var owners = Reachable(root);
        // No hooks: this runs ahead of an action, which is usually the rebuild itself.
        Apply(pending.Values.Where(p => owners.Contains(p.Slot.Owner)).ToList(), runHooks: false);
    }

    /// <summary>
    /// Drops edits on objects reachable from <paramref name="root"/>, e.g. before removing it.
    /// Marks on created objects stay until the objects leave the body.
    /// </summary>
    public void DiscardWithin(object root)
    {
        var owners = Reachable(root);
        foreach (var edit in pending.Values.Where(p => p.Slot is not CreatedSlot && owners.Contains(p.Slot.Owner)).ToList())
            pending.Remove(edit.Slot);
        Changed?.Invoke();
    }

    void Apply(List<Pending> edits, bool runHooks)
    {
        if (edits.Count == 0)
            return;

        var touched = new HashSet<object>(ReferenceComparer.Instance);

        // PreApply members first, as the parser does; otherwise in the order they were made.
        // Reorders go last: edits to items of a reordered list land in its staged copy.
        var ordered = edits
            .OrderByDescending(p => p.Slot.Member?.PreApply ?? false)
            .ThenBy(p => p.Slot is ListOrderSlot)
            .ThenBy(p => p.Order);
        using (ParseStateScope.For(Context))
        {
            foreach (var edit in ordered)
            {
                try
                {
                    edit.Slot.Write(edit.Adapter.Wrap(edit.Value));
                    pending.Remove(edit.Slot);
                    touched.Add(edit.Slot.Owner);
                }
                catch (Exception e)
                {
                    edit.Error = e.Message;
                    Debug.LogError($"[KittopiaTech] Failed to apply {edit.Slot.Label}: {e}");
                }
            }
        }

        if (runHooks)
            ApplyHooks.Run(Context, touched);
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
            foreach (var child in Children(current))
                queue.Enqueue(child);
        }
        return seen;
    }

    /// <summary>The node values and list items directly beneath <paramref name="node"/>.</summary>
    internal static IEnumerable<object> Children(object node)
    {
        if (node is IList list)
        {
            foreach (var item in list)
                yield return item;
            yield break;
        }

        foreach (var member in TypeSchema.Get(node.GetType()).Members)
        {
            if (member.Shape == MemberShape.Value || !member.CanRead)
                continue;

            object value;
            try
            {
                value = member.GetValue(node);
            }
            catch (Exception)
            {
                // Some getters throw on partially set up bodies; skip them.
                continue;
            }
            if (value != null)
                yield return value;
        }
    }
}

internal sealed class ReferenceComparer : IEqualityComparer<object>
{
    public static readonly ReferenceComparer Instance = new();

    public new bool Equals(object x, object y) => ReferenceEquals(x, y);

    public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
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
