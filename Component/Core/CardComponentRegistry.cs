using MinionLib.Component.Interfaces;

namespace MinionLib.Component.Core;

public static class CardComponentRegistry
{
    private static readonly Dictionary<string, Type> IdToType = [];
    private static readonly Dictionary<string, Func<ICardComponent>> IdToFactory = [];

    public static void Register(string componentId, Type componentType, Func<ICardComponent> factory)
    {
        if (string.IsNullOrWhiteSpace(componentId))
            throw new ArgumentException("Component id cannot be null or empty", nameof(componentId));
        ArgumentNullException.ThrowIfNull(componentType);
        ArgumentNullException.ThrowIfNull(factory);

        if (IdToType.TryGetValue(componentId, out var existing))
            throw new InvalidOperationException(
                $"Duplicate component id '{componentId}' for {componentType.FullName} and {existing.FullName}");
        StringIdPool.Register(componentId);
        IdToType[componentId] = componentType;
        IdToFactory[componentId] = factory;
    }

    public static ICardComponent Create(string componentId)
    {
        if (!IdToFactory.TryGetValue(componentId, out var factory))
            throw new InvalidOperationException($"Unknown component id '{componentId}'");

        var instance = factory();
        return instance ?? throw new InvalidOperationException($"Factory returned null for component id '{componentId}'");
    }
}
