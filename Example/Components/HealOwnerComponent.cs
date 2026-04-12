using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Component;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using AmountCardComponent = MinionLib.Component.Utils.AmountCardComponent;

namespace MinionLib.Example.Components;

public sealed partial class HealOwnerComponent : AmountCardComponent
{
    public override async Task OnPlayPrefix(PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (Card == null) return;
        await CreatureCmd.Heal(Card.Owner.Creature, Amount);
    }

    public override bool TryMergeWith(ICardComponent incoming, out ICardComponent? merged)
    {
        if (incoming is not HealOwnerComponent heal)
        {
            merged = null;
            return false;
        }

        Amount += heal.Amount;
        merged = Amount <= 0 ? null : this;
        return true;
    }

    public override bool TrySubtractiveMergeWith(ICardComponent incoming, out ICardComponent? merged)
    {
        if (incoming is not HealOwnerComponent heal)
        {
            merged = null;
            return false;
        }

        Amount -= heal.Amount;
        merged = Amount <= 0 ? null : this;
        return true;
    }
}