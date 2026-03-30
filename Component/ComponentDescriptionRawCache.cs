namespace MinionLib.Component;

internal static class ComponentDescriptionRawCache
{
    private static readonly Dictionary<string, string> RawByLocEntryKey = [];

    public static bool TryGet(string locEntryKey, out string rawText)
    {
        lock (RawByLocEntryKey)
            return RawByLocEntryKey.TryGetValue(locEntryKey, out rawText!);
    }

    public static bool Contains(string locEntryKey)
    {
        lock (RawByLocEntryKey)
            return RawByLocEntryKey.ContainsKey(locEntryKey);
    }

    public static void Set(string locEntryKey, string rawText)
    {
        lock (RawByLocEntryKey)
            RawByLocEntryKey[locEntryKey] = rawText;
    }

    public static void Clear()
    {
        lock (RawByLocEntryKey)
            RawByLocEntryKey.Clear();
    }
}

