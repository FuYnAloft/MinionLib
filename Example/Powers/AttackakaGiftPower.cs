using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Example.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MinionLib.Example.Powers;

[RegisterPower]
public sealed class AttackakaGiftPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomIconPath => "res://Example/MinionTest/orb.png";

    public override string? CustomBigIconPath => "res://Example/MinionTest/orb.png";

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!Owner.IsAlive || Owner.PetOwner != player) return Task.CompletedTask;

        for (var i = 0; i < Amount; i++)
        {
            // var petOwner = Owner.PetOwner;
            // var card = player.CombatState!.CreateCard<AttackakaStrikeCard>(petOwner);
            // card.BindMinion(Owner);
            // await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, false);
            Debug("AttackakaStrikeCard was Removed");
        }

        return Task.CompletedTask;
    }
}
