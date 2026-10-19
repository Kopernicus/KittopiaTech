using System;

namespace KittopiaTech.Model;

/// <summary>
/// Preview is for in-progress input (slider drags, typing); Final is end-edit or release.
/// </summary>
public enum EditPhase
{
    Preview,
    Final,
}

/// <summary>What a field editor sees: one value it can read and write.</summary>
public interface IValueSource
{
    /// <summary>The type the editor works in, after unwrapping (e.g. double for NumericParser&lt;double&gt;).</summary>
    Type ValueType { get; }

    /// <summary>Creates sources for values nested inside this one.</summary>
    IEditScope Scope { get; }

    /// <summary>Where the value lives. Structural edits write here directly, bypassing staging.</summary>
    ISlot Slot { get; }

    /// <summary>The member being edited; null for list items.</summary>
    MemberSchema Member { get; }

    string Label { get; }
    bool IsReadOnly { get; }

    /// <summary>True when a pending edit exists.</summary>
    bool IsModified { get; }

    /// <summary>Why the last write failed, or null.</summary>
    string Error { get; }

    object Get();
    void Set(object value, EditPhase phase = EditPhase.Final);

    /// <summary>Drops the pending edit, if any.</summary>
    void Reset();

    /// <summary>Raised when the value changed under the editor; editors call Refresh.</summary>
    event Action Changed;
}

/// <summary>Hands out value sources for one body. Implemented by <see cref="EditSession"/>.</summary>
public interface IEditScope
{
    BodyContext Context { get; }

    IValueSource For(ISlot slot, ValueAdapter adapter);

    /// <summary>Tells every source from this scope that the body changed, e.g. after an action ran.</summary>
    void Invalidate();

    /// <summary>Applies pending edits on objects reachable from <paramref name="root"/>.</summary>
    void ApplyWithin(object root);

    /// <summary>Drops pending edits on objects reachable from <paramref name="root"/>.</summary>
    void DiscardWithin(object root);

    /// <summary>
    /// Records an object a structural edit just created, so it shows as pending until Apply
    /// runs its hooks. Discard calls <paramref name="undo"/> with the object to remove it again;
    /// History may call it with a rebuilt copy.
    /// </summary>
    void MarkCreated(object created, string label, Action<object> undo);

    /// <summary>Whether <paramref name="value"/> was created since the last Apply.</summary>
    bool IsCreated(object value);

    /// <summary>Records an edit history can't undo, e.g. a destructive action.</summary>
    void SealHistory(string label);

    /// <summary>Whether a pending edit sits on <paramref name="node"/> or anywhere beneath it.</summary>
    bool HasPendingWithin(object node);
}
