using System.Buffers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MinionLib.RightClick;

namespace MinionLib.Component.Interfaces;

public partial interface ICardComponent : IGeneratedBinarySerializable
{
    string ComponentId { get; }

    IComponentsCardModel? ComponentsCard { get; }

    CardModel? Card => ComponentsCard as CardModel;

    void Attach(IComponentsCardModel card, bool isInternal = false);

    void Detach(bool isInternal = false);

    ICardComponent DeepClone();

    ICardComponent? MergeWith(ICardComponent incoming);

    DynamicVarSet DynamicVars { get; }

    bool ShouldGlowGoldInternal => false;

    bool ShouldGlowRedInternal => false;

    bool HasTurnEndInHandEffect => false;

    IEnumerable<IHoverTip> HoverTips => [];

    string GetFormattedPrefix();

    string GetFormattedPostfix();
    
    bool CanHandleRightClickLocal(RightClickContext context) => false;
    
    Task OnRightClick(PlayerChoiceContext choiceContext, RightClickContext clickContext) => Task.CompletedTask;
}

/// <summary>
///     Marker return value for MergeWith: keep both components and skip merge replacement.
/// </summary>
public sealed partial class KeepsTwo : ICardComponent
{
    public static KeepsTwo Instance { get; } = new();

    private KeepsTwo()
    {
    }

    public string ComponentId => nameof(KeepsTwo);

    public IComponentsCardModel? ComponentsCard => null;

    public void Attach(IComponentsCardModel card, bool isInternal = false)
    {
    }

    public void Detach(bool isInternal = false)
    {
    }

    public ICardComponent DeepClone()
    {
        return Instance;
    }

    public ICardComponent MergeWith(ICardComponent incoming)
    {
        return Instance;
    }

    public void Serialize(ArrayBufferWriter<byte> writer)
    {
    }

    public bool Deserialize(ref ReadOnlySpan<byte> reader)
    {
        return true;
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