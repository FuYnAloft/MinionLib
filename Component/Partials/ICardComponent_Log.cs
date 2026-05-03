using System.Collections;
using System.Reflection;
using System.Text;
using MinionLib.Component.Core;

// ReSharper disable once CheckNamespace
namespace MinionLib.Component.Interfaces;

public partial interface ICardComponent
{
    string IGeneratedBinarySerializable.ToLogString(int depth, string indentChars)
    {
        var sb = new StringBuilder();
        var currentIndent = string.Concat(Enumerable.Repeat(indentChars, depth));
        var properties = GetType().GetProperties(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
        );

        foreach (var prop in properties)
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0) continue;
            if (!prop.IsDefined(typeof(ComponentStateAttribute), true)) continue;

            var value = prop.GetValue(this);
            var propName = prop.Name;

            switch (value)
            {
                case null:
                    sb.AppendLine($"{currentIndent}{propName}: null");
                    break;
                case IGeneratedBinarySerializable child:
                    sb.AppendLine($"{currentIndent}{propName}:");
                    sb.Append(child.ToLogString(depth + 1, indentChars));
                    break;
                case IEnumerable list and not string:
                    sb.AppendLine($"{currentIndent}{propName}: ");
                    foreach (var item in list)
                    {
                        switch (item)
                        {
                            case null:
                                sb.AppendLine($"{currentIndent}{indentChars}- null");
                                break;
                            case IGeneratedBinarySerializable child:
                                sb.AppendLine($"{currentIndent}{indentChars}- ");
                                sb.Append(child.ToLogString(depth + 2, indentChars));
                                break;
                            default:
                                sb.AppendLine($"{currentIndent}{indentChars}- {item}");
                                break;
                        }
                    }

                    break;
                default:
                    sb.AppendLine($"{currentIndent}{propName}: {value}");
                    break;
            }
        }

        return sb.ToString();
    }
}
