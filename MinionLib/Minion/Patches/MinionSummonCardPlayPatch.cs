using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Minion;

namespace MinionLib.Minion.Patches;

/// <summary>
///     拦截 <see cref="CardModel.CanPlay()" />，当卡牌实现 <see cref="IMinionSummonCard" /> 且玩家场上随从已达上限时，
///     将卡牌标记为无法打出。
///     <para>
///         当上限为 <c>-1</c>（<see cref="MinionLimitManager.Unlimited" />）时不做任何限制，召唤卡始终可打出。
///     </para>
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.CanPlay))]
public static class MinionSummonCardPlayPatch
{
    [HarmonyPostfix]
    private static void Postfix(CardModel __instance, ref bool __result)
    {
        if (!__result) return;
        if (__instance is not IMinionSummonCard summonCard) return;

        var owner = __instance.Owner;
        if (owner is null) return;
        if (!summonCard.IsLimitedByMinionCap(owner)) return;

        if (!MinionLimitManager.CanSummon(owner))
            __result = false;
    }
}
