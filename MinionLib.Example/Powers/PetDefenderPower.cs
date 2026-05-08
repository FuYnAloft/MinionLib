using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Example.Actions;

namespace MinionLib.Example.Powers;

public sealed class PetDefenderPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        ICombatState combatState)
    {
        if (side != Owner.Side || !Owner.IsAlive) return;

        var applier = Owner.PetOwner?.Creature ?? Owner;
        await PowerCmd.Apply<PetDefensePoint>(choiceContext, Owner, Amount, applier, null);
    }
}
