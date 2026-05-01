using System.Diagnostics.CodeAnalysis;

namespace MinionLib.Component.Core;

public static class StringIdPool
{
    private static readonly Dictionary<ulong, string> IdToString = new();
    private static readonly Dictionary<string, ulong> StringToId = new();

    private static ulong Calculate64BitHash(string str)
    {
        var hash = 14695981039346656037UL;
        foreach (var c in str)
        {
            hash ^= c;
            hash *= 1099511628211UL;
        }

        return hash;
    }

    public static ulong Register(string value)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        var id = Calculate64BitHash(value) << 1;
        if (IdToString.TryGetValue(id, out var existing))
        {
            if (existing == value) return id;
            throw new InvalidOperationException($"StringPool Hash Collision! '{value}' and '{existing}'");
        }

        IdToString[id] = value;
        StringToId[value] = id;
        return id;
    }

    public static bool TryGetId(string value, out ulong id)
    {
        return StringToId.TryGetValue(value, out id);
    }

    public static bool TryGetString(ulong id, [MaybeNullWhen(false)] out string value)
    {
        return IdToString.TryGetValue(id, out value);
    }
}
