using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Component;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using MinionLib.Example.Components;

namespace MinionLib.Example.Cards;

public sealed class GrantDeckDamageBlockComponentCard()
    : ComponentsCardModel(0, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy)
{
    private const string PortraitResourcePath = "res://images/packed/card_portraits/beta.png";

    public override string PortraitPath => PortraitResourcePath;

    public override string BetaPortraitPath => PortraitResourcePath;

    public override IEnumerable<string> AllPortraitPaths => [PortraitResourcePath];

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
