using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MinionLib.Models;

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

    public virtual Task OnSummon(Player owner, Creature self, MinionSummonOptions options)
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
    Front,
    Back,
    FrontUpper,
    BackUpper,
    Upper
}