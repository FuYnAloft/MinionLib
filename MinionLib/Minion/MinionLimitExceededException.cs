using MegaCrit.Sts2.Core.Entities.Players;

namespace MinionLib.Minion;

/// <summary>
///     当 <see cref="MinionSummonOverflowBehavior.Fail" /> 模式下召唤随从而玩家已达上限时抛出。
/// </summary>
public class MinionLimitExceededException : InvalidOperationException
{
    public Player Player { get; }
    public int CurrentCount { get; }
    public int MaxCount { get; }

    public MinionLimitExceededException(Player player, int currentCount, int maxCount)
        : base($"Cannot summon minion: player already has {currentCount}/{maxCount} minions.")
    {
        Player = player;
        CurrentCount = currentCount;
        MaxCount = maxCount;
    }
}
