using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Component;
using MinionLib.Component.Core;
using MinionLib.Component.DynamicVarFactories;
using MinionLib.Component.Interfaces;

namespace MinionLib.Example.Components;

public sealed partial class DamageBlockComponent : CardComponent
{
    [ComponentState<DamageVarFactory>]
    public partial int Damage { get; set; }

    [ComponentState<BlockVarFactory>]
    public partial int Block { get; set; }

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