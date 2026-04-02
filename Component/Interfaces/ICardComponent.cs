using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace MinionLib.Component.Interfaces;

public partial interface ICardComponent
{
    string ComponentId { get; }

    IComponentsCardModel? ComponentsCard { get; }

    CardModel? Card => ComponentsCard as CardModel;

    void Attach(IComponentsCardModel card);

    void Detach();

    ICardComponent DeepClone();

    ICardComponent? MergeWith(ICardComponent incoming);

    DynamicVarSet DynamicVars { get; }

    bool ShouldGlowGoldInternal => false;

    bool ShouldGlowRedInternal => false;

    bool HasTurnEndInHandEffect => false;

    IEnumerable<IHoverTip> HoverTips => [];

    string GetFormattedPrefix();

    string GetFormattedPostfix();
}

/// <summary>
///     Marker return value for MergeWith: keep both components and skip merge replacement.
/// </summary>
public sealed class KeepsTwo : ICardComponent
{
    public static KeepsTwo Instance { get; } = new();

    private KeepsTwo()
    {
    }

    public string ComponentId => nameof(KeepsTwo);

    public IComponentsCardModel? ComponentsCard => null;

    public void Attach(IComponentsCardModel card)
    {
    }

    public void Detach()
    {
    }

    public ICardComponent DeepClone()
    {
        return Instance;
    }

    public ICardComponent? MergeWith(ICardComponent incoming)
    {
        return Instance;
    }

    public DynamicVarSet DynamicVars => null!;

    public string GetFormattedPrefix()
    {
        return string.Empty;
    }

    public string GetFormattedPostfix()
    {
        return string.Empty;
    }
}