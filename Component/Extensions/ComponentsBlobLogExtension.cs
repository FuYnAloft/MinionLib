using System.Text;
using MegaCrit.Sts2.Core.Saves.Runs;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;

namespace MinionLib.Component.Extensions;

public static class ComponentsBlobLogExtension
{
    private const string BlobPropertyName = nameof(ComponentsCardModel.MinionLibComponentStateBlob);

    extension(SerializableCard save)
    {
        public int[]? MinionLibComponentStateBlob => save.Props?.intArrays
            ?.Where(prop => prop.name == BlobPropertyName)
            .Select(prop => prop.value)
            .FirstOrDefault();

        public IReadOnlyList<ICardComponent>? Components
        {
            get
            {
                var blob = save.MinionLibComponentStateBlob;
                return blob is null ? null : CardComponentStateSerializer.Deserialize(blob, null);
            }
        }

        public string GetComponentsLogString(int depth = 0, string indentChars = "    ", bool showEmpty = false)
        {
            var components = save.Components;
            if (components is null) return "";
            var currentIndent = string.Concat(Enumerable.Repeat(indentChars, depth));
            if (components.Count == 0) return showEmpty ? $"{currentIndent}Components: [Empty]\n" : "";
            return $"{currentIndent}Components: \n{components.ToLogString(depth + 1, indentChars)}";
        }
    }

    extension(IEnumerable<ICardComponent> components)
    {
        public string ToLogString(int depth = 0, string indentChars = "    ")
        {
            var currentIndent = string.Concat(Enumerable.Repeat(indentChars, depth));
            var sb = new StringBuilder();
            foreach (var component in components)
            {
                sb.AppendLine($"{currentIndent}- {component.ComponentId}:");
                sb.Append(component.ToLogString(depth + 1, indentChars));
            }

            return sb.ToString();
        }
    }
}
