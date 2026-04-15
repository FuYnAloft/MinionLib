using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;

namespace MinionLib.Component.Patches;

[HarmonyPatch]
public static class ComponentDescriptionRawCachePatch
{
    private const string CardsTable = "cards";
    private const string PrefixToken = "{CompPre}";
    private const string PostfixToken = "{CompPost}";

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
    [HarmonyPostfix]
    private static void DescriptionGetterPostfix(CardModel __instance, LocString __result)
    {
        if (__instance is not IComponentsCardModel)
            return;

        var locEntryKey = __result.LocEntryKey;
        if (string.IsNullOrWhiteSpace(locEntryKey) || ComponentDescriptionRawCache.Contains(locEntryKey))
            return;

        var rawText = __result.Exists() ? __result.GetRawText() : "";
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