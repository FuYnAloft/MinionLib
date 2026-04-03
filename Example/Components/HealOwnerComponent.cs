using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Component;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using AmountCardComponent = MinionLib.Component.Utils.AmountCardComponent;

namespace MinionLib.Example.Components;

public sealed class HealOwnerComponent : AmountCardComponent
{
    public override async Task OnPlayPrefix(PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (Card == null) return;
        await CreatureCmd.Heal(Card.Owner.Creature, Amount);
    }

    public override ICardComponent? MergeWith(ICardComponent incoming)
    {
        if (incoming is not HealOwnerComponent heal) return this;

        Amount += heal.Amount;
        return Amount <= 0 ? null : this;
    }
}