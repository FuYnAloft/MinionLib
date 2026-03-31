using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Component.Core;

namespace MinionLib.Component.Interfaces;

public interface IComponentsCardModel
{
    IReadOnlyList<ICardComponent> Components { get; }

    IEnumerable<ICardComponent> CanonicalComponents { get; }

    T AddComponent<T>(T component) where T : ICardComponent;

    bool RemoveComponent<T>() where T : ICardComponent;

    int RemoveComponents<T>() where T : ICardComponent;

    T? GetComponent<T>() where T : ICardComponent;

    IEnumerable<T> GetComponents<T>() where T : ICardComponent;

    void EnsureComponentsInitialized();

    Task ComponentCallBack(string name, params object[] args);
}