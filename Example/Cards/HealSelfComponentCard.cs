using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MinionLib.Component;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using MinionLib.Example.Components;

namespace MinionLib.Example.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class HealSelfComponentCard() : CustomComponentsCardModel(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override string CustomPortraitPath => "res://images/packed/card_portraits/beta.png";

    protected override IEnumerable<ICardComponent> CanonicalComponents => [new HealOwnerComponent { Amount = 2 }];

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        AddComponent(new HealOwnerComponent { Amount = 3 });
        GetComponent<HealOwnerComponent>()!.DynamicVars["Amount"].SetWasJustUpgraded();
    }
}
