using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace MinionLib.Utilities;

/// <summary>
/// Legacy hover tip factory replacement.
/// Provides helper methods for creating hover tips.
/// </summary>
public static class HoverTipHelper
{
    /// <summary>
    /// Creates a hover tip from a power type using standard localization keys.
    /// </summary>
    public static IHoverTip FromPower<T>() where T : class
    {
        var typeName = typeof(T).Name;
        return new HoverTip(
            new LocString($"powers/{typeName}", "title"),
            new LocString($"powers/{typeName}", "description"));
    }

    /// <summary>
    /// Creates a static hover tip with the given title and description.
    /// </summary>
    public static IHoverTip Static(LocString title, LocString description)
    {
        return new HoverTip(title, description);
    }

    /// <summary>
    /// Creates a static hover tip with a formatted description.
    /// </summary>
    public static IHoverTip Static(LocString title, params object[] args)
    {
        return new HoverTip(
            new LocString(title.LocTable, title.LocEntryKey + ".title"),
            new LocString(title.LocTable, title.LocEntryKey + ".description"));
    }
}
