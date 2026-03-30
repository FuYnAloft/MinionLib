using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Component;

namespace MinionLib.Example.Components;

public sealed class HealOwnerComponent : CardComponent
{
    public override async Task OnPlayPrefix(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (Card is not CardModel componentCard) return;

        await CreatureCmd.Heal(componentCard.Owner.Creature, Amount);
    }

    public override ICardComponent? MergeWith(ICardComponent other)
    {
        if (other is not HealOwnerComponent heal) return this;

        Amount += heal.Amount;
        return this;
    }
}

