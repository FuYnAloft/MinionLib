using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using MinionLib.Example.Components;

namespace MinionLib.Example.Cards;

[RegisterCard(typeof(TokenCardPool))]
public sealed class GrantDeckDamageBlockComponentCard()
    : ExampleComponentsCardTemplate(0, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy)
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ComponentContext componentContext)
    {
        foreach (var componentsCard in PileType.Deck.GetPile(Owner).Cards.OfType<IComponentsCardModel>().ToArray())
            componentsCard.AddComponent(new DamageBlockComponent { Damage = 1, Block = 1 });

        foreach (var componentsCard in PileType.Hand.GetPile(Owner).Cards.OfType<IComponentsCardModel>().ToArray())
            componentsCard.AddComponent(new DamageBlockComponent { Damage = 1, Block = 1 });

        return Task.CompletedTask;
    }
}
