using MegaCrit.Sts2.Core.Entities.Cards;
using MinionLib.Component;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using MinionLib.Example.Components;

namespace MinionLib.Example.Cards;

public sealed class HealSelfComponentCard() : ComponentsCardModel(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    private const string PortraitResourcePath = "res://images/packed/card_portraits/beta.png";

    public override string PortraitPath => PortraitResourcePath;

    public override string BetaPortraitPath => PortraitResourcePath;

    public override IEnumerable<string> AllPortraitPaths => [PortraitResourcePath];

    protected override IEnumerable<ICardComponent> CanonicalComponents => [new HealOwnerComponent { Amount = 2 }];

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        AddComponent(new HealOwnerComponent { Amount = 3 });
        GetComponent<HealOwnerComponent>()!.DynamicVars["Amount"].SetWasJustUpgraded();
    }
}
