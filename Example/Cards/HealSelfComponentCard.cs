using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MinionLib.Component;
using MinionLib.Component.Interfaces;
using MinionLib.Example.Components;

namespace MinionLib.Example.Cards;

[Pool(typeof(ColorlessCardPool))]
public sealed class HealSelfComponentCard() : ComponentsCardModel(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override string CustomPortraitPath => "res://images/packed/card_portraits/beta.png";

    public override IEnumerable<ICardComponent> CanonicalComponents => [new HealOwnerComponent { Amount = 2 }];

    protected override void OnUpgrade()
    {
        AddComponent(new HealOwnerComponent { Amount = 3 });
    }
}

