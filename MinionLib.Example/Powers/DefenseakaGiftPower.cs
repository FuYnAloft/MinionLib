using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MinionLib.Example.Powers;

public sealed class DefenseakaGiftPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        ICombatState combatState)
    {
        if (side != Owner.Side || !Owner.IsAlive || Owner.PetOwner == null) return;

        for (var i = 0; i < Amount; i++)
        {
            // var owner = Owner.PetOwner;
            // var card = combatState.CreateCard<DefenseakaGuardCard>(owner);
            // card.BindMinion(Owner);
            // await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, false);
            Debug("DefenseakaGuardCard was removed");
        }
    }
}
