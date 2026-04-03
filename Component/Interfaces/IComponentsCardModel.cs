using MegaCrit.Sts2.Core.Models;

namespace MinionLib.Component.Interfaces;

public interface IComponentsCardModel
{
    CardModel AsCardModel => (CardModel)this;

    IReadOnlyList<ICardComponent> Components { get; }

    T? AddComponent<T>(T component) where T : ICardComponent;

    bool RemoveComponent<T>() where T : ICardComponent;

    int RemoveComponents<T>() where T : ICardComponent;
    
    bool RefRemoveComponent(ICardComponent component);

    T? GetComponent<T>() where T : ICardComponent;

    IEnumerable<T> GetComponents<T>() where T : ICardComponent;

    void EnsureComponentsInitialized();

    Task ComponentCallBack(string name, params object?[] args);

    bool ComponentPredicate(string name, params object?[] args);

    object? ComponentQuery(string name, params object?[] args);

    Task<object?> ComponentQueryAsync(string name, params object?[] args);
}