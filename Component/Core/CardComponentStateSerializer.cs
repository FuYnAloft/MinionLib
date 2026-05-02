using System.Buffers;
using MinionLib.Component.Interfaces;

namespace MinionLib.Component.Core;

public static class CardComponentStateSerializer
{
    public static int[] Serialize(IReadOnlyList<ICardComponent> components)
    {
        var writer = new ArrayBufferWriter<byte>();
        SerializationUtils.WriteCount(writer, components.Count);

        foreach (var component in components)
        {
            SerializationUtils.WriteString(writer, component.ComponentId);
            SerializationUtils.WriteSerializableBlock(writer, component);
        }

        return SerializationUtils.ToIntArray(writer.WrittenSpan);
    }

    public static List<ICardComponent> Deserialize(int[] state, IComponentsCardModel? owner)
    {
        if (!SerializationUtils.TryFromIntArray(state, out var bytes) || bytes.Length == 0)
            return [];

        var reader = new ReadOnlySpan<byte>(bytes);
        if (!SerializationUtils.TryReadCount(ref reader, out var count))
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

            if (owner != null)
                component.Attach(owner, true);
            result.Add(component);
        }

        return result;
    }

    public static ICardComponent DeepClone(ICardComponent component)
    {
        var owner = component.ComponentsCard;
        var serialized = Serialize([component]);
        var clone = Deserialize(serialized, owner).FirstOrDefault();

        if (clone == null)
            throw new InvalidOperationException($"Failed to clone component {component.GetType().FullName}");
        
        return clone;
    }
}
