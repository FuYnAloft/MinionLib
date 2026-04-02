using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;

namespace MinionLib.Component;

public abstract partial class CardComponent : ICardComponent
{
    public string ComponentId => CardComponentRegistry.GetComponentId(GetType());

    public IComponentsCardModel? ComponentsCard { get; private set; }

    public void Attach(IComponentsCardModel card)
    {
        ComponentsCard = card;
        OnAttach();
    }

    protected virtual void OnAttach()
    {
    }

    public void Detach()
    {
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

    public virtual ICardComponent? MergeWith(ICardComponent other)
    {
        return other;
    }

    protected virtual IEnumerable<DynamicVar> ExtraVars => [];

    public DynamicVarSet DynamicVars
    {
        get
        {
            if (field != null)
                return field;
            var smart = SmartDynamicVarsLocArgs.GenerateDynamicVars(this);
            field = new DynamicVarSet(smart.Concat(ExtraVars));
            return field;
        }
    }

    public virtual bool ShouldGlowGoldInternal => false;

    public virtual bool ShouldGlowRedInternal => false;

    public virtual bool HasTurnEndInHandEffect => false;

    public virtual IEnumerable<IHoverTip> HoverTips => [];

    private LocString SmartPrefix()
    {
        var loc = new LocString("cards", ComponentId + ".prefix");
        DynamicVars.AddTo(loc);
        SmartDynamicVarsLocArgs.SmartAddArgs(this, loc);
        return loc;
    }

    private LocString SmartPostfix()
    {
        var loc = new LocString("cards", ComponentId + ".postfix");
        DynamicVars.AddTo(loc);
        SmartDynamicVarsLocArgs.SmartAddArgs(this, loc);
        return loc;
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
}