using MegaCrit.Sts2.Core.Models;

namespace MinionLib.Component.Interfaces;

public interface IComponentsCardModel
{
    CardModel AsCardModel => (CardModel)this;

    IReadOnlyList<ICardComponent> Components { get; }

    IEnumerable<ICardComponent> CanonicalComponents { get; }

    T? AddComponent<T>(T component) where T : ICardComponent;

    bool RemoveComponent<T>() where T : ICardComponent;

    int RemoveComponents<T>() where T : ICardComponent;

    T? GetComponent<T>() where T : ICardComponent;

    IEnumerable<T> GetComponents<T>() where T : ICardComponent;

    void EnsureComponentsInitialized();

    Task ComponentCallBack(string name, params object[] args);
}