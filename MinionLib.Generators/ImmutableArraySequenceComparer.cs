#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;

namespace MinionLib.Generators;

internal sealed class ImmutableArraySequenceComparer<T> : IEqualityComparer<ImmutableArray<T>>
    where T : notnull
{
    public static readonly ImmutableArraySequenceComparer<T> Instance = new();

    public bool Equals(ImmutableArray<T> x, ImmutableArray<T> y)
    {
        if (x.Length != y.Length)
            return false;

        for (var i = 0; i < x.Length; i++)
        {
            if (!EqualityComparer<T>.Default.Equals(x[i], y[i]))
                return false;
        }

        return true;
    }

    public int GetHashCode(ImmutableArray<T> obj)
    {
        unchecked
        {
            var hash = 17;
            for (var i = 0; i < obj.Length; i++)
                hash = (hash * 31) + EqualityComparer<T>.Default.GetHashCode(obj[i]);
            return hash;
        }
    }
}

