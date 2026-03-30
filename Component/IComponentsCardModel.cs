using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MinionLib.Component;

public interface IComponentsCardModel
{
    IReadOnlyList<ICardComponent> Components { get; }

    IEnumerable<ICardComponent> CanonicalComponents { get; }

    T AddComponent<T>(T component) where T : ICardComponent;

    bool RemoveComponent<T>() where T : ICardComponent;

    T? GetComponent<T>() where T : ICardComponent;

    void EnsureComponentsInitialized();

    Task OnPlayPhased(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext);
}