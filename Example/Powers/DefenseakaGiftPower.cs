using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Example.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MinionLib.Example.Powers;

[RegisterPower]
public sealed class DefenseakaGiftPower : ModPowerTemplate
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
            // var owner = Owner.PetOwner;
            // var card = player.CombatState!.CreateCard<DefenseakaGuardCard>(owner);
            // card.BindMinion(Owner);
            // await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, false);
            Debug("DefenseakaGuardCard was removed");
        }

        return Task.CompletedTask;
    }
}
