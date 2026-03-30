using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Component;

namespace MinionLib.Example.Components;

public sealed class DamageBlockComponent : CardComponent
{
    [ComponentState]
    public int DamageAmount { get; set; }

    [ComponentState]
    public int BlockAmount { get; set; }

    // This component uses DamageAmount/BlockAmount as its state and ignores base Amount.
    public override decimal Amount
    {
        get => 0m;
        set { }
    }

    public override async Task OnPlayPrefix(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (Card is not CardModel componentCard)
            return;

        if (cardPlay.Target != null)
            await CreatureCmd.Damage(choiceContext, cardPlay.Target, DamageAmount, ValueProp.Move, componentCard.Owner.Creature, componentCard);

        await CreatureCmd.GainBlock(componentCard.Owner.Creature, BlockAmount, ValueProp.Move, cardPlay);
    }

    public override ICardComponent? MergeWith(ICardComponent other)
    {
        if (other is not DamageBlockComponent damageBlock)
            return this;

        DamageAmount += damageBlock.DamageAmount;
        BlockAmount += damageBlock.BlockAmount;
        return this;
    }

    public override string GetFormattedPrefix()
    {
        var prefix = new LocString("cards", ComponentId + ".prefix");
        prefix.Add("damage", DamageAmount);
        prefix.Add("block", BlockAmount);
        return prefix.Exists() ? prefix.GetFormattedText() : string.Empty;
    }
}

