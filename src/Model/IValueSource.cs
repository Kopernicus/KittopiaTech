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
