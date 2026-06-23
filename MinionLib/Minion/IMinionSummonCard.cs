using MegaCrit.Sts2.Core.Entities.Players;

namespace MinionLib.Minion;

/// <summary>
///     标记一张卡牌为"召唤卡"。实现此接口后，当玩家场上随从数量达到上限时，
///     该卡牌会自动变为无法打出状态。
///     <para>
///         当上限为 <c>-1</c>（<see cref="MinionLimitManager.Unlimited" />）时，不限制随从数量，召唤卡始终可打出。
///     </para>
/// </summary>
public interface IMinionSummonCard
{
    /// <summary>
    ///     返回此卡牌召唤时使用的溢出行为，默认为 <see cref="MinionSummonOverflowBehavior.Fail" />。
    ///     通常无需重写；仅当此卡牌通过非标准途径召唤（如直接调用 <see cref="Commands.MinionCmd.AddMinion{T}" />）
    ///     且需要自定义溢出行为时才需调整。
    /// </summary>
    MinionSummonOverflowBehavior OverflowBehavior => MinionSummonOverflowBehavior.Fail;

    /// <summary>
    ///     判断此卡牌在当前玩家状态下是否受随从上限限制。
    ///     默认实现：当玩家不在战斗中或上限为不限制时返回 <c>false</c>（不受限制）。
    ///     可重写以实现更复杂的条件（如仅在某些状态下限制）。
    /// </summary>
    bool IsLimitedByMinionCap(Player player)
    {
        return player.PlayerCombatState != null && !MinionLimitManager.IsUnlimited(player);
    }
}
