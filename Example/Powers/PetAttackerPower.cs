using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Example.Actions;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MinionLib.Example.Powers;

[RegisterPower]
public sealed class PetAttackerPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomIconPath => "res://Example/MinionTest/orb.png";

    public override string? CustomBigIconPath => "res://Example/MinionTest/orb.png";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!Owner.IsAlive || (Owner.PetOwner != player && Owner.Player != player)) return;

        var applier = Owner.PetOwner?.Creature ?? Owner;
        await PowerCmd.Apply<PetAttackPoint>(choiceContext, [Owner], Amount, applier, null);
    }
}
