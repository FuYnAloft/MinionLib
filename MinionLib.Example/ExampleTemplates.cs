using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace MinionLib.Example;

internal static class ExampleAssets
{
    public const string CardPortrait = "res://images/packed/card_portraits/beta.png";
    public const string OrbIcon = "res://Example/MinionTest/orb.png";
    public const string StrengthPotionImage = "res://Example/MinionTest/minionlib-minion_strength_potion.tres";

    public const string StrengthPotionOutline =
        "res://Example/MinionTest/minionlib-minion_strength_potion_outline.tres";
}

public abstract class ExampleCardTemplate(
    int baseCost,
    CardType type,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : ModCardTemplate(baseCost, type, rarity, target, showInCardLibrary)
{
    public override CardAssetProfile AssetProfile => new(ExampleAssets.CardPortrait);
}

public abstract class ExampleComponentsCardTemplate(
    int baseCost,
    CardType type,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : ModComponentsCardTemplate(baseCost, type, rarity, target, showInCardLibrary)
{
    public override CardAssetProfile AssetProfile => new(ExampleAssets.CardPortrait);
}

public abstract class ExamplePowerTemplate : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => new(ExampleAssets.OrbIcon, ExampleAssets.OrbIcon);
}

public abstract class ExampleActionTemplate : ModActionTemplate
{
    private static readonly IHoverTip ActionHoverTip = new HoverTip(
        new LocString("static_hover_tips", "MinionLib-Action.title"),
        new LocString("static_hover_tips", "MinionLib-Action.description"));

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [ActionHoverTip];

    public override PowerAssetProfile AssetProfile => new(ExampleAssets.OrbIcon, ExampleAssets.OrbIcon);
}

public abstract class ExamplePotionTemplate : ModPotionTemplate
{
    public override PotionAssetProfile AssetProfile =>
        new(ExampleAssets.StrengthPotionImage, ExampleAssets.StrengthPotionOutline);
}

public abstract class ExampleMinionTemplate : ModMinionTemplate
{
    protected abstract string VisualsScenePath { get; }

    public override MonsterAssetProfile AssetProfile => new(VisualsScenePath);
}
