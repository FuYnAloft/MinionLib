using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Component.Core;
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

    bool TryMergeWith(ICardComponent incoming, ApplyComponentOptions options, out ICardComponent? merged);

    bool TrySubtractiveMergeWith(ICardComponent incoming, ApplyComponentOptions options, out ICardComponent? merged);

    DynamicVarSet DynamicVars { get; }

    bool ShouldGlowGoldInternal => false;

    bool ShouldGlowRedInternal => false;

    Color? GlowColor => null;

    TargetType? ExtraTargetType => null;

    CardType? CardTypeOverride => null;

    CardRarity? CardRarityOverride => null;

    bool IsPlayable => true;

    PileType? GetResultPileType() => null;

    bool HasTurnEndInHandEffect => false;

    IEnumerable<IHoverTip> HoverTips => [];

    string GetFormattedPrefix(Dictionary<string, object> argsFromCard);

    string GetFormattedPostfix(Dictionary<string, object> argsFromCard);

    bool CanHandleRightClickLocal(RightClickContext context) => CanHandleRightClick(context);

    bool CanHandleRightClick(RightClickContext context) => false;

    Task OnRightClick(PlayerChoiceContext choiceContext, RightClickContext clickContext) => Task.CompletedTask;

    void OnUpgrade(ComponentContext componentContext) { }

    void AfterDowngraded(ComponentContext componentContext) { }
}
