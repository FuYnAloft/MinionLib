using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace MinionLib.Content;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PoolAttribute(Type poolType) : Attribute
{
    public Type PoolType { get; } = poolType;
}

public interface ICustomModel;

public interface ICustomCardResourceProvider
{
    string? CustomPortraitPath => null;

    Texture2D? CustomPortrait => null;

    Texture2D? CustomFrame => null;
}

public interface ICustomPowerResourceProvider
{
    string? CustomPackedIconPath => null;

    string? CustomBigIconPath => null;

    string? CustomBigBetaIconPath => null;
}

public interface ICustomPotionResourceProvider
{
    string? CustomPackedImagePath => null;

    string? CustomPackedOutlinePath => null;
}

public interface ICustomPower : ICustomModel, ICustomPowerResourceProvider;

public interface ILocalizationProvider
{
    List<(string, string)>? Localization => null;
}

public abstract class CustomCardModel(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : CardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary),
        ICustomModel, ICustomCardResourceProvider, ILocalizationProvider
{
    public virtual Texture2D? CustomFrame => null;

    public virtual string? CustomPortraitPath => null;

    public virtual Texture2D? CustomPortrait => null;

    public virtual List<(string, string)>? Localization => null;

    public override string PortraitPath => CustomPortraitPath ?? base.PortraitPath;

    public override string BetaPortraitPath => CustomPortraitPath ?? base.BetaPortraitPath;

    public override IEnumerable<string> AllPortraitPaths =>
        CustomPortraitPath is { } path ? [path] : base.AllPortraitPaths;
}

public abstract class CustomPowerModel : PowerModel, ICustomPower, ILocalizationProvider
{
    public virtual string? CustomPackedIconPath => null;

    public virtual string? CustomBigIconPath => null;

    public virtual string? CustomBigBetaIconPath => null;

    public virtual List<(string, string)>? Localization => null;
}

public abstract class CustomPotionModel : PotionModel, ICustomModel, ICustomPotionResourceProvider, ILocalizationProvider
{
    public virtual string? CustomPackedImagePath => null;

    public virtual string? CustomPackedOutlinePath => null;

    public virtual List<(string, string)>? Localization => null;
}
