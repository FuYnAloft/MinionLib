using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Component;
using MinionLib.Component.Core;

namespace MinionLib.Example.Cards;

public class TestComponentsCard()
    : ComponentsCardModel(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    [ModuleInitializer]
    internal static void RegisterDelegates()
    {
        DelegateRegistry.Register("MinionLib.Example.Cards.TestComponentsCard.MyPredicate", MyPredicate);
    }

    private static bool MyPredicate(CardModel card)
    {
        return card.Id.Entry == "Test";
    }
}

// 