using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Component;
using MinionLib.Component.Core;

namespace MinionLib.Example.Cards;

public partial class TestComponentsCard()
    : ComponentsCardModel(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    [ComponentDelegate]
    private static bool MyPredicate(CardModel card)
    {
        return card.Id.Entry == "Test";
    }
    [ComponentDelegate]
    private static bool MyPredicate2(CardModel card)
    {
        return card.Id.Entry == "Test";
    }
}