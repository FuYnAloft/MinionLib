using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;

namespace MinionLib.Component;

public abstract class CardComponent : ICardComponent
{
    protected CardComponent()
    {
        DynamicVars = SmartDynamicVarsLocArgs.GenerateDynamicVars(this);
    }

    public string ComponentId => CardComponentRegistry.GetComponentId(GetType());

    public IComponentsCardModel? Card { get; private set; }

    public virtual void Attach(IComponentsCardModel card)
    {
        Card = card;
    }

    public virtual void Detach()
    {
        Card = null;
    }

    public virtual ICardComponent DeepClone()
    {
        return CardComponentStateSerializer.DeepClone(this);
    }

    public virtual ICardComponent? MergeWith(ICardComponent other)
    {
        return other;
    }

    public virtual DynamicVarSet DynamicVars { get; } = new([]);

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

    public virtual Task OnPlayPrefix(PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ComponentContext componentContext)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnPlayPostfix(PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ComponentContext componentContext)
    {
        return Task.CompletedTask;
    }
}