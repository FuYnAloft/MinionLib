using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Example.Cards;

namespace MinionLib.Example.Powers;

public sealed class AttackakaGiftPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        ICombatState combatState)
    {
        if (side != Owner.Side || !Owner.IsAlive || Owner.PetOwner == null) return;

        for (var i = 0; i < Amount; i++)
        {
            // var petOwner = Owner.PetOwner;
            // var card = combatState.CreateCard<AttackakaStrikeCard>(petOwner);
            // card.BindMinion(Owner);
            // await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, false);
            Debug("AttackakaStrikeCard was Removed");
        }
    }
}
