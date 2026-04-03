#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace MinionLib.Generators;

internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>
    where T : notnull
{
    public EquatableArray(ImmutableArray<T> items)
    {
        Items = items;
    }

    public ImmutableArray<T> Items { get; }

    public bool Equals(EquatableArray<T> other)
    {
        if (Items.Length != other.Items.Length)
            return false;

        for (var i = 0; i < Items.Length; i++)
        {
            if (!EqualityComparer<T>.Default.Equals(Items[i], other.Items[i]))
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is EquatableArray<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            for (var i = 0; i < Items.Length; i++)
                hash = (hash * 31) + EqualityComparer<T>.Default.GetHashCode(Items[i]);
            return hash;
        }
    }
}

