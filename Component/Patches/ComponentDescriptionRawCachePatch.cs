using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Patching.Models;

namespace MinionLib.Component.Patches;

public static class ComponentDescriptionRawCachePatch
{
    public const string CardsTable = "cards";
    public const string PrefixToken = "{CompPre}";
    public const string PostfixToken = "{CompPost}";
    public const char NoDescriptionMarker = '\uef01';
    public const string NoDescriptionMarkerString = "\uef01";

    private static string InjectCompTokens(string rawText)
    {
        var text = rawText ?? string.Empty;

        if (!text.Contains(PrefixToken, StringComparison.Ordinal))
            text = string.IsNullOrWhiteSpace(text) ? PrefixToken : PrefixToken + text;

        if (!text.Contains(PostfixToken, StringComparison.Ordinal))
            text = string.IsNullOrWhiteSpace(text) ? PostfixToken : text + PostfixToken;

        return text;
    }

    public sealed class DescriptionGetter : IPatchMethod
    {
        public static string PatchId => "component_description_raw_cache_getter";

        public static string Description => "Cache raw component card descriptions before dynamic formatting.";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)];
        }

        private static void Postfix(CardModel __instance, LocString __result)
        {
            if (__instance is not IComponentsCardModel)
                return;

            var locEntryKey = __result.LocEntryKey;
            if (string.IsNullOrWhiteSpace(locEntryKey) || ComponentDescriptionRawCache.Contains(locEntryKey))
                return;

            var rawText = __result.Exists() ? __result.GetRawText() : NoDescriptionMarkerString;
            ComponentDescriptionRawCache.Set(locEntryKey, InjectCompTokens(rawText));
        }
    }

    public sealed class LocStringGetRawText : IPatchMethod
    {
        public static string PatchId => "component_description_locstring_raw_text";

        public static string Description => "Return cached component card raw description text.";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(LocString), nameof(LocString.GetRawText))];
        }

        private static bool Prefix(LocString __instance, ref string __result)
        {
            if (!string.Equals(__instance.LocTable, CardsTable, StringComparison.Ordinal))
                return true;

            if (!ComponentDescriptionRawCache.TryGet(__instance.LocEntryKey, out var cachedRaw))
                return true;

            __result = cachedRaw;
            return false;
        }
    }

    public sealed class LocManagerSetLanguage : IPatchMethod
    {
        public static string PatchId => "component_description_clear_raw_cache";

        public static string Description => "Clear cached component descriptions when language changes.";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(LocManager), nameof(LocManager.SetLanguage))];
        }

        private static void Postfix()
        {
            ComponentDescriptionRawCache.Clear();
        }
    }
}

public static class NoDescriptionMarkerCleanPatch
{
    public static MethodBase TargetMethod()
    {
        var previewEnumType = AccessTools.Inner(typeof(CardModel), "DescriptionPreviewType");

        return AccessTools.Method(typeof(CardModel), "GetDescriptionForPile", [
            typeof(PileType),
            previewEnumType,
            typeof(Creature)
        ]);
    }

    public static void Postfix(ref string __result)
    {
        if (string.IsNullOrEmpty(__result)) return;
        var index = __result.IndexOf(ComponentDescriptionRawCachePatch.NoDescriptionMarker);
        if (index < 0) return;

        var hasAfter = index < __result.Length - 1 && __result[index + 1] == '\n';
        var hasBefore = index > 0 && __result[index - 1] == '\n';

        if (hasAfter)
        {
            __result = __result.Remove(index, 2);
        }
        else if (hasBefore)
        {
            __result = __result.Remove(index - 1, 2);
        }
        else
        {
            __result = __result.Remove(index, 1);
        }
    }
}
