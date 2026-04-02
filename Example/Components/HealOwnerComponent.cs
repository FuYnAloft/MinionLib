using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Component;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;

namespace MinionLib.Example.Components;

public sealed partial class HealOwnerComponent : AmountCardComponent
{
    [ComponentState]
    public int ANumbet { get; set; }
    
    [ComponentState]
    public decimal BNumbet { get; set; }
    
    
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