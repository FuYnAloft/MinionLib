using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Component;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;

namespace MinionLib.Example.Components;

public sealed partial class DamageBlockComponent : CardComponent
{
    [ComponentState<DamageVar>(ValueProp.Move)]
    public partial int Damage { get; set; }

    [ComponentState<BlockVar>(ValueProp.Move)]
    public partial int Block { get; set; }

    public override async Task OnPlayPrefix(PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (Card == null) return;
        if (cardPlay.Target != null)
            await CreatureCmd.Damage(choiceContext, cardPlay.Target, DynamicVars.Damage,
                Card.Owner.Creature, Card);
        await CreatureCmd.GainBlock(Card.Owner.Creature, DynamicVars.Block, cardPlay);
    }

    public override ICardComponent? MergeWith(ICardComponent incoming)
    {
        if (incoming is not DamageBlockComponent damageBlock)
            return this;

        Damage += damageBlock.Damage;
        Block += damageBlock.Block;
        return Damage == 0 && Block == 0 ? null : this;
    }

    public override ICardComponent? SubtractiveMergeWith(ICardComponent incoming)
    {
        if (incoming is not DamageBlockComponent damageBlock)
            return null;

        Damage -= damageBlock.Damage;
        Block -= damageBlock.Block;
        return Damage == 0 && Block == 0 ? null : this;
    }
}