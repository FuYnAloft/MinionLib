using System.Buffers;
using Godot;
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

    public virtual ICardComponent? MergeWith(ICardComponent incoming)
    {
        return incoming;
    }

    public virtual ICardComponent? SubtractiveMergeWith(ICardComponent incoming)
    {
        return null;
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

    public virtual Color? GlowColorInternal => null;

    public virtual bool HasTurnEndInHandEffect => false;

    public virtual IEnumerable<IHoverTip> HoverTips => [];

    protected virtual LocString PrefixLocString => new LocString("cards", ComponentId + ".prefix");

    protected virtual LocString PostfixLocString => new LocString("cards", ComponentId + ".postfix");

    private LocString SmartPrefix()
    {
        var loc = PrefixLocString;
        DynamicVars.AddTo(loc);
        SmartAddArgs(loc);
        return loc;
    }

    private LocString SmartPostfix()
    {
        var loc = PostfixLocString;
        DynamicVars.AddTo(loc);
        SmartAddArgs(loc);
        return loc;
    }

    protected virtual void SmartAddArgs(LocString loc)
    {
    }

    protected virtual string FormatPrefix(LocString loc)
    {
        return loc.GetFormattedText();
    }

    protected virtual string FormatPostfix(LocString loc)
    {
        return loc.GetFormattedText();
    }

    public string GetFormattedPrefix()
    {
        var prefix = SmartPrefix();
        return prefix.Exists() ? FormatPrefix(prefix) : "";
    }

    public string GetFormattedPostfix()
    {
        var postfix = SmartPostfix();
        return postfix.Exists() ? FormatPostfix(postfix) : "";
    }
    
    public virtual bool CanHandleRightClickLocal(RightClickContext context)
    {
        return false;
    }

    public virtual Task OnRightClick(PlayerChoiceContext choiceContext, RightClickContext clickContext)
    {
        return Task.CompletedTask;
    }
}
