using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;

namespace MinionLib.Component.Patches;

[HarmonyPatch]
public static class ComponentDescriptionRawCachePatch
{
    public const string CardsTable = "cards";
    public const string PrefixToken = "{CompPre}";
    public const string PostfixToken = "{CompPost}";
    public const char NoDescriptionMarker = '\uef01';
    public const string NoDescriptionMarkerString = "\uef01";

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
    [HarmonyPostfix]
    private static void DescriptionGetterPostfix(CardModel __instance, LocString __result)
    {
        if (__instance is not IComponentsCardModel)
            return;

        var locEntryKey = __result.LocEntryKey;
        if (string.IsNullOrWhiteSpace(locEntryKey) || ComponentDescriptionRawCache.Contains(locEntryKey))
            return;

        var rawText = __result.Exists() ? __result.GetRawText() : NoDescriptionMarkerString;
        ComponentDescriptionRawCache.Set(locEntryKey, InjectCompTokens(rawText));
    }

    [HarmonyPatch(typeof(LocString), nameof(LocString.GetRawText))]
    [HarmonyPrefix]
    private static bool GetRawTextPrefix(LocString __instance, ref string __result)
    {
        if (!string.Equals(__instance.LocTable, CardsTable, StringComparison.Ordinal))
            return true;

        if (!ComponentDescriptionRawCache.TryGet(__instance.LocEntryKey, out var cachedRaw))
            return true;

        __result = cachedRaw;
        return false;
    }

    [HarmonyPatch(typeof(LocManager), nameof(LocManager.SetLanguage))]
    [HarmonyPostfix]
    private static void SetLanguagePostfix()
    {
        ComponentDescriptionRawCache.Clear();
    }

    private static string InjectCompTokens(string rawText)
    {
        var text = rawText ?? string.Empty;

        if (!text.Contains(PrefixToken, StringComparison.Ordinal))
            text = string.IsNullOrWhiteSpace(text) ? PrefixToken : PrefixToken + text;

        if (!text.Contains(PostfixToken, StringComparison.Ordinal))
            text = string.IsNullOrWhiteSpace(text) ? PostfixToken : text + PostfixToken;

        return text;
    }
}

[HarmonyPatch]
public static class NoDescriptionMarkerCleanPatch
{
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        var previewEnumType = AccessTools.Inner(typeof(CardModel), "DescriptionPreviewType");

        return AccessTools.Method(typeof(CardModel), "GetDescriptionForPile", [
            typeof(PileType),
            previewEnumType,
            typeof(Creature)
        ]);
    }

    [HarmonyPostfix]
    private static void Postfix(ref string __result)
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
            __result = "";
        }
    }
}