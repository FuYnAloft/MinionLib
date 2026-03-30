using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace MinionLib.Component;

public abstract class CardComponent : ICardComponent
{
    public string ComponentId => CardComponentRegistry.GetComponentId(GetType());

    [ComponentState]
    public virtual int Amount { get; set; }

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

    public virtual ICardComponent MergeWith(ICardComponent other)
    {
        return other;
    }

    public virtual Task OnPlayPrefix(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnPlayPostfix(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        return Task.CompletedTask;
    }

    public virtual string GetFormattedPrefix()
    {
        var prefix = new LocString("components", ComponentId + ".prefix");
        prefix.Add("amount", Amount);
        return prefix.Exists() ? prefix.GetFormattedText() : string.Empty;
    }

    public virtual string GetFormattedPostfix()
    {
        var postfix = new LocString("components", ComponentId + ".postfix");
        postfix.Add("amount", Amount);
        return postfix.Exists() ? postfix.GetFormattedText() : string.Empty;
    }
}
