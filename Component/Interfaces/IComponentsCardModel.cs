using Godot;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Component.Core;

namespace MinionLib.Component.Interfaces;

public interface IComponentsCardModel
{
    CardModel AsCardModel => (CardModel)this;

    IReadOnlyList<ICardComponent> Components { get; }

    ICardComponent? AddComponent<T>(T incoming, bool allowMerge = true,
        bool useSubtractiveMerge = false, bool isUpgrade = false)
        where T : class, ICardComponent
    {
        return AddComponent(incoming, new AddComponentOptions(
            AllowMerge: allowMerge,
            UseSubtractiveMerge: useSubtractiveMerge,
            IsUpgrade: isUpgrade
        ));
    }

    ICardComponent? AddComponent<T>(T incoming, AddComponentOptions options)
        where T : class, ICardComponent;

    bool RemoveComponent<T>() where T : class, ICardComponent;

    int RemoveComponents<T>() where T : class, ICardComponent;

    bool RefRemoveComponent(ICardComponent component);

    T? GetComponent<T>() where T : class, ICardComponent;

    IEnumerable<T> GetComponents<T>() where T : class, ICardComponent;

    void EnsureComponentsInitialized();

    Color? GlowColor => null;

    # region Deprecated

    [Obsolete(
        "This method is deprecated and should not be called or overridden. Use interface constraints or delegate registry instead.",
        false)]
    Task ComponentCallBack(string name, params object?[] args);

    [Obsolete(
        "This method is deprecated and should not be called or overridden. Use interface constraints or delegate registry instead.",
        false)]
    bool ComponentPredicate(string name, params object?[] args);

    [Obsolete(
        "This method is deprecated and should not be called or overridden. Use interface constraints or delegate registry instead.",
        false)]
    object? ComponentQuery(string name, params object?[] args);

    [Obsolete(
        "This method is deprecated and should not be called or overridden. Use interface constraints or delegate registry instead.",
        false)]
    Task<object?> ComponentQueryAsync(string name, params object?[] args);

    #endregion
}