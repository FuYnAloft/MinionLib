using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Example.Actions;

namespace MinionLib.Example.Powers;

public sealed class PetDefenderPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://Example/MinionTest/orb.png";

    public override string CustomBigIconPath => "res://Example/MinionTest/orb.png";

    public override string CustomBigBetaIconPath => "res://Example/MinionTest/orb.png";

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        CombatState combatState)
    {
        if (side != Owner.Side || !Owner.IsAlive) return;

        var applier = Owner.PetOwner?.Creature ?? Owner;
        await PowerCmd.Apply<PetDefensePoint>(Owner, Amount, applier, null);
    }
}