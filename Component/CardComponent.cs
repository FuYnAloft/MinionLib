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

    public virtual bool TryMergeWith(
        ICardComponent incoming,
        ApplyComponentOptions options,
        out ICardComponent? merged
    )
    {
        merged = null;
        return false;
    }

    public virtual bool TrySubtractiveMergeWith(
        ICardComponent incoming,
        ApplyComponentOptions options,
        out ICardComponent? merged
    )
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

    public virtual TargetType? ExtraTargetType => null;

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

        var energyPrefix = (string)loc.Variables["energyPrefix"];
        foreach (var (name, variable) in loc.Variables)
        {
            if (variable is EnergyVar energyVar)
                energyVar.ColorPrefix = energyPrefix;
        }
    }

    protected virtual string FormatPrefix(LocString loc)
    {
        return loc.GetFormattedText();
    }

    protected virtual string FormatPostfix(LocString loc)
    {
        return loc.GetFormattedText();
    }

    public virtual string GetFormattedPrefix(Dictionary<string, object> argsFromCard)
    {
        var loc = PrefixLocString;
        if (!loc.Exists())
            return "";
        foreach (var (name, variable) in argsFromCard)
        {
            loc.AddObj(name, variable);
        }

        SmartAddArgs(loc);
        var formatted = FormatPrefix(loc);
        return formatted;
    }

    public virtual string GetFormattedPostfix(Dictionary<string, object> argsFromCard)
    {
        var loc = PostfixLocString;
        if (!loc.Exists())
            return "";
        foreach (var (name, variable) in argsFromCard)
        {
            loc.AddObj(name, variable);
        }

        SmartAddArgs(loc);
        var formatted = FormatPostfix(loc);
        return formatted.EndsWith('\n') ? "\n" + formatted[..^1] : formatted;
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

    public virtual void OnUpgrade(ComponentContext componentContext) { }

    public virtual void AfterDowngraded(ComponentContext componentContext) { }
}
