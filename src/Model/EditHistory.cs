using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KittopiaTech.Model;

/// <summary>One Apply: what each slot it wrote held before and after.</summary>
public sealed class Revision
{
    internal sealed class Change
    {
        public ISlot Slot;
        public ValueAdapter Adapter;
        public object Before;
        public object After;

        /// <summary>The edit took objects off the body.</summary>
        public bool Removes;

        /// <summary>Copies of the objects it removed, for restoring to before it.</summary>
        public List<RemovedObject> Removed = [];
    }

    internal readonly List<Change> Changes = [];

    public string Label { get; internal set; }
    public DateTime Time { get; } = DateTime.Now;

    /// <summary>An edit since then can't be undone (e.g. a removal), so this can't be restored.</summary>
    public bool Sealed { get; internal set; }

    /// <summary>It destroyed objects it couldn't keep copies of, so the revisions before it can't be restored.</summary>
    internal bool Irreversible { get; set; }

    /// <summary>Restoring past a revision that created objects destroys them, so it can't be redone.</summary>
    internal bool Creates => Changes.Any(c => c.Slot is CreatedSlot);

    public IEnumerable<string> Edited =>
        Changes.Select(c => c.Slot is CreatedSlot ? $"new {c.Slot.Label}" : c.Removes ? $"removed from {c.Slot.Label}" : c.Slot.Label);
}

/// <summary>
/// The revisions applied to one body this game session, starting from the body as loaded.
/// Restoring writes back the values each revision changed and runs the same hooks as Apply.
/// Only the newest <see cref="Limit"/> are kept.
/// </summary>
public sealed class EditHistory(EditSession session)
{
    public const int Limit = 40;

    readonly List<Revision> revisions = [new Revision { Label = "Original" }];

    public IReadOnlyList<Revision> Revisions => revisions;
    public int Current { get; private set; }

    public bool CanRestore(int index) => index >= 0 && index < revisions.Count && index != Current && !revisions[index].Sealed;

    internal void Record(Revision revision)
    {
        // A new revision replaces any that were restored away from.
        revisions.RemoveRange(Current + 1, revisions.Count - Current - 1);
        if (revision.Irreversible)
        {
            foreach (var earlier in revisions)
                earlier.Sealed = true;
        }
        revisions.Add(revision);
        // The oldest kept becomes the earliest state that can be restored.
        if (revisions.Count > Limit)
            revisions.RemoveRange(0, revisions.Count - Limit);
        Current = revisions.Count - 1;
    }

    /// <summary>Starts a new revision that the ones before it can't be restored from.</summary>
    internal void Seal(string label)
    {
        foreach (var revision in revisions)
            revision.Sealed = true;
        Record(new Revision { Label = label });
    }

    /// <summary>Copies lists, which some slots refill in place.</summary>
    internal static object Snapshot(object value) => value is IList list ? new ArrayList(list) : value;

    /// <summary>Discards pending edits, then rewinds or replays revisions until <paramref name="index"/> is current.</summary>
    public void Restore(int index)
    {
        if (!CanRestore(index))
            return;

        session.Discard();
        var touched = new HashSet<object>(ReferenceComparer.Instance);
        int firstCreating = -1;
        using (ParseStateScope.For(session.Context))
        {
            for (; Current > index; Current--)
            {
                var revision = revisions[Current];
                foreach (var change in Enumerable.Reverse(revision.Changes))
                {
                    Rebuild(change, touched);
                    Write(change, change.Before, touched);
                }
                if (revision.Creates)
                    firstCreating = Current;
            }
            for (; Current < index; Current++)
            {
                foreach (var change in revisions[Current + 1].Changes)
                {
                    // Removing them again, as the Apply did.
                    foreach (var removed in change.Removed)
                        Destroy(removed.Current);
                    Write(change, change.After, touched);
                }
            }
        }

        // Objects those revisions made are gone, and later ones may have edited them.
        if (firstCreating >= 0)
            revisions.RemoveRange(firstCreating, revisions.Count - firstCreating);

        ApplyHooks.Run(session.Context, touched);
        session.Invalidate();
    }

    /// <summary>
    /// Rebuilds the objects <paramref name="change"/> removed, and points every revision that
    /// held them, or anything beneath them, at the rebuilt copies.
    /// </summary>
    void Rebuild(Revision.Change change, HashSet<object> touched)
    {
        if (change.Removed.Count == 0)
            return;

        var map = new Dictionary<object, object>(ReferenceComparer.Instance);
        foreach (var removed in change.Removed)
        {
            var old = removed.Current;
            try
            {
                touched.Add(removed.Rebuild(session.Context, map));
            }
            catch (Exception e)
            {
                // Leave it out rather than putting the destroyed object back.
                map[old] = null;
                Debug.LogError($"[KittopiaTech] Failed to rebuild the removed {old.GetType().Name}: {e}");
            }
        }

        object Map(object o) => o != null && map.TryGetValue(o, out var rebuilt) ? rebuilt : o;
        object MapValue(object value) =>
            value is IList list ? new ArrayList(list.Cast<object>().Select(Map).Where(o => o != null).ToList()) : Map(value);

        foreach (var other in revisions.SelectMany(r => r.Changes))
        {
            other.Slot = other.Slot.Rebind(o => Map(o) ?? o);
            other.Before = MapValue(other.Before);
            other.After = MapValue(other.After);
        }
    }

    void Destroy(object value)
    {
        try
        {
            Structure.Destroy(session.Context, value);
        }
        catch (Exception e)
        {
            Debug.LogError($"[KittopiaTech] Failed to remove {value.GetType().Name} again: {e}");
        }
    }

    static void Write(Revision.Change change, object value, HashSet<object> touched)
    {
        try
        {
            if (change.Slot is CreatedSlot created)
                created.Undo();
            else
                change.Slot.Write(change.Adapter.Wrap(Snapshot(value)));
            touched.Add(change.Slot.Owner);
        }
        catch (Exception e)
        {
            Debug.LogError($"[KittopiaTech] Failed to restore {change.Slot.Label}: {e}");
        }
    }
}
