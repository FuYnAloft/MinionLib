using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Component;
using MinionLib.Component.DynamicVarGenerate;

namespace MinionLib.Example.Components;

public sealed class DamageBlockComponent : CardComponent
{
    [ComponentState<DamageVarGen>]
    public int Damage
    {
        get;
        set
        {
            field = value;
            DynamicVars["Damage"].BaseValue = value;
        }
    }

    [ComponentState<BlockVarGen>]
    public int Block
    {
        get;
        set
        {
            field = value;
            DynamicVars["Block"].BaseValue = value;
        }
    }

    // This component uses DamageAmount/BlockAmount as its state and ignores base Amount.
    public override decimal Amount
    {
        get => 0m;
        set { }
    }

    public override async Task OnPlayPrefix(PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (Card is not CardModel componentCard)
            return;

        if (cardPlay.Target != null)
            await CreatureCmd.Damage(choiceContext, cardPlay.Target, Damage, ValueProp.Move,
                componentCard.Owner.Creature, componentCard);

        await CreatureCmd.GainBlock(componentCard.Owner.Creature, Block, ValueProp.Move, cardPlay);
    }

    public override ICardComponent? MergeWith(ICardComponent other)
    {
        if (other is not DamageBlockComponent damageBlock)
            return this;

        Damage += damageBlock.Damage;
        Block += damageBlock.Block;
        return this;
    }
}