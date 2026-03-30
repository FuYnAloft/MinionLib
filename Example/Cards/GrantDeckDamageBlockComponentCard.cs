using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MinionLib.Component;
using MinionLib.Example.Components;

namespace MinionLib.Example.Cards;

[Pool(typeof(ColorlessCardPool))]
public sealed class GrantDeckDamageBlockComponentCard() : ComponentsCardModel(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        foreach (var componentsCard in PileType.Deck.GetPile(Owner).Cards.OfType<IComponentsCardModel>().ToArray())
            componentsCard.AddComponent(new DamageBlockComponent { DamageAmount = 1, BlockAmount = 1 });

        return Task.CompletedTask;
    }
}

