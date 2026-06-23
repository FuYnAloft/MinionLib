using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MinionLib.Minion;

public abstract class MinionModel : MonsterModel
{
    public override string DeathSfx => "event:/sfx/characters/osty/osty_die";

    public override bool HasDeathSfx => true;

    public MinionPosition Position { get; internal set; }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState idle = new("MINION_IDLE", _ => Task.CompletedTask)
        {
            FollowUpState = null
        };
        idle.FollowUpState = idle;
        return new MonsterMoveStateMachine([idle], idle);
    }

    public virtual Task OnSummon(PlayerChoiceContext choiceContext, Player owner, MinionSummonOptions options)
    {
        return Task.CompletedTask;
    }
}

public readonly record struct MinionSummonOptions(
    decimal? MaxHp = null,
    decimal? PrimaryStatAmount = null,
    decimal? SecondaryStatAmount = null,
    decimal? TertiaryStatAmount = null,
    CardModel? Source = null,
    MinionPosition Position = MinionPosition.Front,
    MinionSummonOverflowBehavior OverflowBehavior = MinionSummonOverflowBehavior.Fail);

public enum MinionPosition
{
    Front = 0,
    Back,
    FrontUpper,
    BackUpper,
    Upper
}

/// <summary>
///     当召唤随从时，若玩家已达随从上限，定义如何处理溢出。
/// </summary>
public enum MinionSummonOverflowBehavior
{
    /// <summary>
    ///     直接抛出 <see cref="MinionLimitExceededException" />，由调用方自行处理（默认行为）。
    ///     <para>
    ///         召唤卡通常通过 <see cref="IMinionSummonCard" /> 在打出前检查上限，
    ///         达到上限时卡牌变为无法打出状态，因此正常流程不会触发此异常。
    ///     </para>
    /// </summary>
    Fail,

    /// <summary>
    ///     移除最早召唤的随从以腾出位置。
    /// </summary>
    ReplaceOldest,

    /// <summary>
    ///     忽略上限检查，直接召唤。
    /// </summary>
    Ignore,
}
