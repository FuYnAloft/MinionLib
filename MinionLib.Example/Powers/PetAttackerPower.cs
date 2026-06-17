using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Example.Actions;

namespace MinionLib.Example.Powers;

[RegisterPower]
public sealed class PetAttackerPower : ExamplePowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side || !Owner.IsAlive) return;

        var applier = Owner.PetOwner?.Creature ?? Owner;
        await PowerCmd.Apply<PetAttackPoint>(choiceContext, Owner, Amount, applier, null);
    }
}
