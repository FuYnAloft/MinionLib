using MegaCrit.Sts2.Core.Entities.Cards;
using MinionLib.Component;

namespace MinionLib.Example.Cards;

public sealed class Blank() : ComponentsCardModel(0, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy)
{
    private const string PortraitResourcePath = "res://images/packed/card_portraits/beta.png";

    public override string PortraitPath => PortraitResourcePath;

    public override string BetaPortraitPath => PortraitResourcePath;

    public override IEnumerable<string> AllPortraitPaths => [PortraitResourcePath];
}
