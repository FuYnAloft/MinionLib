using System.Buffers;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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
    protected virtual IEnumerable<DynamicVar> SmartVars => [];

    protected virtual IEnumerable<DynamicVar> ExtraVars => [];

    protected virtual LocString PrefixLocString => new("cards", ComponentId + ".prefix");

    protected virtual LocString PostfixLocString => new("cards", ComponentId + ".postfix");
    public abstract string ComponentId { get; }


    public IComponentsCardModel? ComponentsCard { get; private set; }

    public CardModel? Card => ComponentsCard as CardModel;

    public void Attach(IComponentsCardModel card, bool isInternal = false)
    {
        ComponentsCard = card;
        if (!isInternal)
            OnAttach();
    }

    public void Detach(bool isInternal = false)
    {
        if (!isInternal)
            OnDetach();
        ComponentsCard = null;
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

    public virtual void Serialize(ArrayBufferWriter<byte> writer) { }

    public virtual bool Deserialize(ref ReadOnlySpan<byte> reader)
    {
        return true;
    }

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

    public IEnumerable<CardTag> ExtraTags => [];

    public virtual bool IsPlayable => true;

    public virtual CardLocation? GetResultLocationForCardPlay()
    {
        return null;
    }

    public virtual bool HasTurnEndInHandEffect => false;

    public virtual IEnumerable<IHoverTip> HoverTips => [];

    public virtual string GetFormattedPrefix(Dictionary<string, object> argsFromCard)
    {
        var loc = PrefixLocString;
        if (!loc.Exists())
            return "";
        foreach (var (name, variable) in argsFromCard)
            loc.AddObj(name, variable);

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
            loc.AddObj(name, variable);

        SmartAddArgs(loc);
        var formatted = FormatPostfix(loc);
        return formatted;
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

    protected virtual void OnAttach() { }

    protected virtual void OnDetach() { }

    protected virtual void SmartAddArgs(LocString loc)
    {
        DynamicVars.AddTo(loc);

        var energyPrefix = (string)loc.Variables["energyPrefix"];
        foreach (var (name, variable) in loc.Variables)
            if (variable is EnergyVar energyVar)
                energyVar.ColorPrefix = energyPrefix;
    }

    protected virtual string FormatPrefix(LocString loc)
    {
        return loc.GetFormattedText();
    }

    protected virtual string FormatPostfix(LocString loc)
    {
        return loc.GetFormattedText();
    }
}
