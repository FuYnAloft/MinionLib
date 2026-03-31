using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MinionLib.Component;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using MinionLib.Example.Components;

namespace MinionLib.Example.Cards;

[Pool(typeof(ColorlessCardPool))]
public sealed class GrantDeckDamageBlockComponentCard() : ComponentsCardModel(0, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    public override string CustomPortraitPath => "res://images/packed/card_portraits/beta.png";

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        foreach (var componentsCard in PileType.Deck.GetPile(Owner).Cards.OfType<IComponentsCardModel>().ToArray())
            componentsCard.AddComponent(new DamageBlockComponent { Damage = 1, Block = 1 });

        foreach (var componentsCard in PileType.Hand.GetPile(Owner).Cards.OfType<IComponentsCardModel>().ToArray())
            componentsCard.AddComponent(new DamageBlockComponent { Damage = 1, Block = 1 });

        return Task.CompletedTask;
    }
}

