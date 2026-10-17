using System;
using System.Collections;

namespace KittopiaTech.Model;

/// <summary>Where a raw (still wrapped) value lives: a member on an object, or a list element.</summary>
public interface ISlot
{
    Type DeclaredType { get; }
    MemberSchema Member { get; }
    string Label { get; }
    bool CanWrite { get; }

    /// <summary>The object that owns this slot; used to key pending edits.</summary>
    object Owner { get; }

    object Read();
    void Write(object raw);
}

public sealed class MemberSlot(object owner, MemberSchema member) : ISlot
{
    public object Owner { get; } = owner;
    public MemberSchema Member { get; } = member;
    public Type DeclaredType => Member.DeclaredType;
    public string Label => Member.Label;
    public bool CanWrite => Member.CanWrite && !Member.Untouchable;

    public object Read() => Member.CanRead ? Member.GetValue(Owner) : null;

    public void Write(object raw) => Member.SetValue(Owner, raw);

    public override bool Equals(object obj) =>
        obj is MemberSlot other && ReferenceEquals(Owner, other.Owner) && Member == other.Member;

    public override int GetHashCode() =>
        System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Owner) ^ Member.GetHashCode();
}

/// <param name="writeBack">
/// Called after a write, for lists whose getter returns a copy that must be set back.
/// </param>
public sealed class ListSlot(IList list, int index, Type elementType, Action writeBack = null)
    : ISlot
{
    public IList List { get; } = list;
    public int Index { get; } = index;
    public object Owner => List;
    public MemberSchema Member => null;
    public Type DeclaredType { get; } = elementType;
    public string Label => $"[{Index}]";
    public bool CanWrite => !List.IsReadOnly;

    public object Read() => Index < List.Count ? List[Index] : null;

    public void Write(object raw)
    {
        List[Index] = raw;
        writeBack?.Invoke();
    }

    public override bool Equals(object obj) =>
        obj is ListSlot other && ReferenceEquals(List, other.List) && Index == other.Index;

    public override int GetHashCode() =>
        System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(List) ^ Index;
}

/// <summary>
/// The order of a list's items, so reordering can be staged. The staged value is a reordered
/// copy; writing it refills the live list in place, since list members are often readonly.
/// </summary>
public sealed class ListOrderSlot(ISlot listSlot, IList live) : ISlot
{
    public ISlot ListSlot { get; } = listSlot;
    public object Owner { get; } = live;
    public MemberSchema Member => ListSlot.Member;
    public Type DeclaredType => ListSlot.DeclaredType;
    public string Label => ListSlot.Label;
    public bool CanWrite => !live.IsReadOnly && !live.IsFixedSize;

    public object Read() => live;

    public void Write(object raw)
    {
        if (ReferenceEquals(raw, live))
            return;
        var items = new ArrayList((IList)raw);
        live.Clear();
        foreach (var item in items)
            live.Add(item);
        if (ListSlot.CanWrite)
            ListSlot.Write(live);
    }

    public override bool Equals(object obj) => obj is ListOrderSlot other && ListSlot.Equals(other.ListSlot);

    public override int GetHashCode() => ListSlot.GetHashCode() ^ 0x5f3759df;
}
