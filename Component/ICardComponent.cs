using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MinionLib.Component;

public interface ICardComponent
{
    string ComponentId { get; }

    IComponentsCardModel? Card { get; }

    void Attach(IComponentsCardModel card);

    void Detach();

    ICardComponent DeepClone();

    ICardComponent? MergeWith(ICardComponent other);

    Task OnPlayPrefix(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        return Task.CompletedTask;
    }


    Task OnPlayPostfix(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        return Task.CompletedTask;
    }

    string GetFormattedPrefix();

    string GetFormattedPostfix();
}

/// <summary>
/// Marker return value for MergeWith: keep both components and skip merge replacement.
/// </summary>
public sealed class KeepsTwo : ICardComponent
{
    public static KeepsTwo Instance { get; } = new KeepsTwo();

    private KeepsTwo()
    {
    }

    public string ComponentId => nameof(KeepsTwo);

    public IComponentsCardModel? Card => null;

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

    public ICardComponent? MergeWith(ICardComponent other)
    {
        return Instance;
    }

    public string GetFormattedPrefix()
    {
        return string.Empty;
    }

    public string GetFormattedPostfix()
    {
        return string.Empty;
    }
}