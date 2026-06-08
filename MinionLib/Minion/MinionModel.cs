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
    MinionPosition Position = MinionPosition.Front);

public enum MinionPosition
{
    Front = 0,
    Back,
    FrontUpper,
    BackUpper,
    Upper
}
