using MegaCrit.Sts2.Core.Helpers;
using MinionLib.Component.Interfaces;

namespace MinionLib.Component.Core;

public static class CardComponentRegistry
{
    private static readonly Dictionary<string, Type> IdToType = [];
    private static readonly Dictionary<Type, string> TypeToId = [];

    static CardComponentRegistry()
    {
        var componentTypes = ReflectionHelper.GetSubtypesInMods<CardComponent>()
             .OrderBy(t => t.FullName, StringComparer.Ordinal)
             .ToList();

        foreach (var componentType in componentTypes)
            Register(componentType);

        Debug("Component", $"Registered {IdToType.Count} card components");
    }

    public static string GetDefaultComponentId(Type type)
    {
        var rootNamespace = type.Namespace;
        if (string.IsNullOrWhiteSpace(rootNamespace))
            return type.Name;

        var splitIndex = rootNamespace.IndexOf('.');
        if (splitIndex < 0)
            splitIndex = rootNamespace.Length;

        return rootNamespace[..splitIndex] + "-" + type.Name;
    }

    public static string GetComponentId(Type type)
    {
        return TypeToId.TryGetValue(type, out var id) ? id : GetDefaultComponentId(type);
    }

    public static ICardComponent Create(string componentId)
    {
        if (!IdToType.TryGetValue(componentId, out var componentType))
            throw new InvalidOperationException($"Unknown component id '{componentId}'");

        return (ICardComponent)(Activator.CreateInstance(componentType)
             ?? throw new InvalidOperationException($"Cannot instantiate component type {componentType}"));
    }

    private static void Register(Type componentType)
    {
        var componentId = GetDefaultComponentId(componentType);

        if (IdToType.TryGetValue(componentId, out var existing))
            throw new InvalidOperationException(
                $"Duplicate component id '{componentId}' for {componentType.FullName} and {existing.FullName}");

        IdToType[componentId] = componentType;
        TypeToId[componentType] = componentId;
    }
}
