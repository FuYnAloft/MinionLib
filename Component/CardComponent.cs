using System.Buffers;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using MinionLib.RightClick;

namespace MinionLib.Component;

public abstract partial class CardComponent : ICardComponent
{
    public abstract string ComponentId { get; }


    public IComponentsCardModel? ComponentsCard { get; private set; }

    public CardModel? Card => ComponentsCard as CardModel;

    public void Attach(IComponentsCardModel card, bool isInternal = false)
    {
        ComponentsCard = card;
        if (!isInternal)
            OnAttach();
    }

    protected virtual void OnAttach()
    {
    }

    public void Detach(bool isInternal = false)
    {
        if (!isInternal)
            OnDetach();
        ComponentsCard = null;
    }

    protected virtual void OnDetach()
    {
    }

    public virtual ICardComponent DeepClone()
    {
        return CardComponentStateSerializer.DeepClone(this);
    }

    public virtual bool TryMergeWith(ICardComponent incoming, out ICardComponent? merged)
    {
        merged = null;
        return false;
    }

    public virtual bool TrySubtractiveMergeWith(ICardComponent incoming, out ICardComponent? merged)
    {
        merged = null;
        return false;
    }

    public virtual void Serialize(ArrayBufferWriter<byte> writer)
    {
    }

    public virtual bool Deserialize(ref ReadOnlySpan<byte> reader)
    {
        return true;
    }

    protected virtual IEnumerable<DynamicVar> SmartVars => [];

    protected virtual IEnumerable<DynamicVar> ExtraVars => [];

    public DynamicVarSet DynamicVars
    {
        get
        {
            if (field != null)
                return field;
            field = new DynamicVarSet(SmartVars.Concat(ExtraVars));
            return field;
        }
    }

    public virtual bool ShouldGlowGoldInternal => false;

    public virtual bool ShouldGlowRedInternal => false;

    public virtual Color? GlowColor => null;

    public virtual TargetType? TargetTypeOverride => null;

    public virtual CardType? CardTypeOverride => null;

    public virtual CardRarity? CardRarityOverride => null;

    public virtual bool IsPlayable => true;

    public virtual PileType? GetResultPileType()
    {
        return null;
    }

    public virtual bool HasTurnEndInHandEffect => false;

    public virtual IEnumerable<IHoverTip> HoverTips => [];

    protected virtual LocString PrefixLocString => new LocString("cards", ComponentId + ".prefix");

    protected virtual LocString PostfixLocString => new LocString("cards", ComponentId + ".postfix");

    protected virtual void SmartAddArgs(LocString loc)
    {
        DynamicVars.AddTo(loc);
        AddLocArgsFromCard(loc);
    }

    protected virtual string FormatPrefix(LocString loc)
    {
        return loc.GetFormattedText();
    }

    protected virtual string FormatPostfix(LocString loc)
    {
        return loc.GetFormattedText();
    }

    private void AddLocArgsFromCard(LocString loc)
    {
        if (Card == null) return;
        var upgradeDisplay = Card.IsUpgraded ? UpgradeDisplay.Upgraded : UpgradeDisplay.Normal;
        loc.Add(new IfUpgradedVar(upgradeDisplay));
        var inCombat = CombatManager.Instance.IsInProgress && (Card.Pile?.IsCombatPile ?? false);
        loc.Add("InCombat", inCombat);
        loc.Add("TargetType", Card.TargetType.ToString());
        var energyPrefix = EnergyIconHelper.GetPrefix(Card);
        loc.Add("energyPrefix", energyPrefix);
        loc.Add("singleStarIcon", "[img]res://images/packed/sprite_fonts/star_icon.png[/img]");
        foreach (var keyValuePair in loc.Variables)
            if (keyValuePair.Value is EnergyVar energyVar)
                energyVar.ColorPrefix = energyPrefix;
        // 已知问题：IsTargeting 和 OnTable 未实现；IfUpgradedVar 和 InCombat 的实现不准确。
    }

    public string GetFormattedPrefix(Dictionary<string, object> argsFromCard)
    {
        var loc = PrefixLocString;
        SmartAddArgs(loc);
        return loc.Exists() ? FormatPrefix(loc) : "";
    }

    public string GetFormattedPostfix(Dictionary<string, object> argsFromCard)
    {
        var loc = PostfixLocString;
        SmartAddArgs(loc);
        return loc.Exists() ? FormatPostfix(loc) : "";
    }

    public virtual bool CanHandleRightClickLocal(RightClickContext context)
    {
        return CanHandleRightClick(context);
    }

    public virtual bool CanHandleRightClick(RightClickContext context)
    {
        return false;
    }

    public virtual Task OnRightClick(PlayerChoiceContext choiceContext, RightClickContext clickContext)
    {
        return Task.CompletedTask;
    }
}