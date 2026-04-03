using System.Buffers;
using MinionLib.Component.Interfaces;

namespace MinionLib.Component.Core;

public static class CardComponentStateSerializer
{
    public static int[] Serialize(IReadOnlyList<ICardComponent> components)
    {
        var writer = new ArrayBufferWriter<byte>();
        SerializationUtils.WriteInt32(writer, components.Count);

        foreach (var component in components)
        {
            SerializationUtils.WriteString(writer, component.ComponentId);
            SerializationUtils.WriteSerializableBlock(writer, component);
        }

        return SerializationUtils.ToIntArray(writer.WrittenSpan);
    }

    public static List<ICardComponent> Deserialize(int[] state, IComponentsCardModel owner)
    {
        if (!SerializationUtils.TryFromIntArray(state, out var bytes) || bytes.Length == 0)
            return [];

        var reader = new ReadOnlySpan<byte>(bytes);
        if (!SerializationUtils.TryReadInt32(ref reader, out var count) || count < 0)
            return [];

        var result = new List<ICardComponent>(count);

        for (var i = 0; i < count; i++)
        {
            if (!SerializationUtils.TryReadString(ref reader, out var componentId)
                || string.IsNullOrWhiteSpace(componentId))
                break;

            ICardComponent component;
            try
            {
                component = CardComponentRegistry.Create(componentId);
            }
            catch (Exception ex)
            {
                Debug("Component", $"Skipped unknown component '{componentId}': {ex.Message}");
                if (!SerializationUtils.TrySkipObjectBlock(ref reader))
                    break;
                continue;
            }

            if (!SerializationUtils.TryReadSerializableBlock(ref reader, component))
            {
                Debug("Component", $"Failed to deserialize component '{componentId}', skipped.");
                continue;
            }

            Attach(component, owner);
            result.Add(component);
        }

        return result;
    }

    public static ICardComponent DeepClone(ICardComponent component)
    {
        var owner = component.ComponentsCard;
        var serialized = Serialize([component]);
        var clone = Deserialize(serialized, owner ?? NullOwner.Instance).FirstOrDefault();

        if (clone == null)
            throw new InvalidOperationException($"Failed to clone component {component.GetType().FullName}");

        if (owner == null)
            clone.Detach();

        return clone;
    }

    private static void Attach(object component, IComponentsCardModel owner)
    {
        var attachMethod = component.GetType().GetMethod("Attach", [typeof(IComponentsCardModel)]);
        attachMethod?.Invoke(component, [owner]);
    }

    private sealed class NullOwner : IComponentsCardModel
    {
        public static readonly NullOwner Instance = new();
        public IReadOnlyList<ICardComponent> Components => [];
        public T? AddComponent<T>(T component) where T : ICardComponent => component;
        public bool RemoveComponent<T>() where T : ICardComponent => false;
        public int RemoveComponents<T>() where T : ICardComponent => 0;
        public bool RefRemoveComponent(ICardComponent component) => false;
        public T? GetComponent<T>() where T : ICardComponent => default;
        public IEnumerable<T> GetComponents<T>() where T : ICardComponent => [];
        public void EnsureComponentsInitialized(){}
        public Task ComponentCallBack(string name, params object[] args) => Task.CompletedTask;
    }
}