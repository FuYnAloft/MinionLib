using System.Reflection;
using System.Text.Json;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MinionLib.Component;

public static class CardComponentStateSerializer
{
    private const string AmountPropertyName = "Amount";

    private sealed class ComponentSnapshot
    {
        public string ComponentId { get; set; } = string.Empty;
        public List<ComponentPropertySnapshot> Properties { get; set; } = [];
    }

    private sealed class ComponentPropertySnapshot
    {
        public string Name { get; set; } = string.Empty;
        public string Json { get; set; } = "null";
    }

    private static readonly Dictionary<Type, PropertyInfo[]> PersistedPropertyCache = [];

    public static string Serialize(IReadOnlyList<ICardComponent> components)
    {
        var snapshots = new List<ComponentSnapshot>(components.Count);

        foreach (var component in components)
        {
            var componentType = component.GetType();
            var properties = GetPersistedProperties(componentType);

            var propertySnapshots = new List<ComponentPropertySnapshot>(properties.Length);
            foreach (var property in properties)
            {
                var value = property.GetValue(component);
                propertySnapshots.Add(new ComponentPropertySnapshot
                {
                    Name = property.Name,
                    Json = JsonSerializer.Serialize(value, property.PropertyType)
                });
            }

            snapshots.Add(new ComponentSnapshot
            {
                ComponentId = GetComponentId(component),
                Properties = propertySnapshots
            });
        }

        return JsonSerializer.Serialize(snapshots);
    }

    public static List<ICardComponent> Deserialize(string state, IComponentsCardModel owner)
    {
        if (string.IsNullOrWhiteSpace(state))
            return [];

        List<ComponentSnapshot>? snapshots;
        try
        {
            snapshots = JsonSerializer.Deserialize<List<ComponentSnapshot>>(state);
        }
        catch (Exception ex)
        {
            Debug("Component", $"Failed to deserialize component state blob: {ex}");
            return [];
        }

        if (snapshots == null || snapshots.Count == 0)
            return [];

        var result = new List<ICardComponent>(snapshots.Count);

        foreach (var snapshot in snapshots)
        {
            ICardComponent component;
            try
            {
                component = CardComponentRegistry.Create(snapshot.ComponentId);
            }
            catch (Exception ex)
            {
                Debug("Component", $"Skipped unknown component '{snapshot.ComponentId}': {ex.Message}");
                continue;
            }

            var componentType = component.GetType();
            var propertyMap = GetPersistedProperties(componentType)
                .ToDictionary(p => p.Name, p => p, StringComparer.Ordinal);

            foreach (var propertySnapshot in snapshot.Properties)
            {
                if (!propertyMap.TryGetValue(propertySnapshot.Name, out var property))
                    continue;

                object? value;
                try
                {
                    value = JsonSerializer.Deserialize(propertySnapshot.Json, property.PropertyType);
                }
                catch (Exception ex)
                {
                    Debug("Component",
                        $"Failed to deserialize property {componentType.Name}.{property.Name}: {ex.Message}");
                    continue;
                }

                property.SetValue(component, value);
            }

            Attach(component, owner);
            result.Add(component);
        }

        return result;
    }

    public static ICardComponent DeepClone(ICardComponent component)
    {
        var owner = component.Card;
        var serialized = Serialize([component]);
        var clone = Deserialize(serialized, owner ?? NullOwner.Instance).FirstOrDefault();

        if (clone == null)
            throw new InvalidOperationException($"Failed to clone component {component.GetType().FullName}");

        if (owner == null)
            clone.Detach();

        return clone;
    }

    private static string GetComponentId(object component)
    {
        var property = component.GetType().GetProperty("ComponentId", BindingFlags.Instance | BindingFlags.Public);
        if (property?.PropertyType != typeof(string))
            throw new InvalidOperationException(
                $"Component {component.GetType().FullName} does not expose string ComponentId.");

        return (string)(property.GetValue(component) ?? string.Empty);
    }

    private static void Attach(object component, IComponentsCardModel owner)
    {
        var attachMethod = component.GetType().GetMethod("Attach", [typeof(IComponentsCardModel)]);
        attachMethod?.Invoke(component, [owner]);
    }

    private static PropertyInfo[] GetPersistedProperties(Type componentType)
    {
        if (PersistedPropertyCache.TryGetValue(componentType, out var cached))
            return cached;

        var properties = componentType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(prop => prop.GetIndexParameters().Length == 0)
            .Where(prop => prop.CanRead && prop.CanWrite)
            .Where(IsPersistedProperty)
            .OrderBy(prop => prop.Name, StringComparer.Ordinal)
            .ToArray();

        PersistedPropertyCache[componentType] = properties;
        return properties;
    }

    private static bool IsPersistedProperty(PropertyInfo property)
    {
        if (property.Name == AmountPropertyName && property.PropertyType == typeof(int))
            return true;

        if (Attribute.IsDefined(property, typeof(ComponentStateAttribute), inherit: true))
            return true;

        return Attribute.IsDefined(property, typeof(SavedPropertyAttribute), inherit: true);
    }

    private sealed class NullOwner : IComponentsCardModel
    {
        public static readonly NullOwner Instance = new();

        public IReadOnlyList<ICardComponent> Components => [];
        public IEnumerable<ICardComponent> CanonicalComponents => [];

        public T AddComponent<T>(T component) where T : ICardComponent => component;
        public bool RemoveComponent<T>() where T : ICardComponent => false;
        public int RemoveComponents<T>() where T : ICardComponent => 0;
        public T? GetComponent<T>() where T : ICardComponent => default;
        public IEnumerable<T> GetComponents<T>() where T : ICardComponent => [];
        public void EnsureComponentsInitialized() { }
        public Task OnPlayPhased(PlayerChoiceContext choiceContext, CardPlay cardPlay,
            ComponentContext componentContext) => Task.CompletedTask;
    }
}