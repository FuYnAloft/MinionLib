using System.Reflection;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MinionLib.Component.Interfaces;

namespace MinionLib.Component.Core;

public static class SmartDynamicVarsLocArgs
{
    private static readonly Dictionary<Type, ComponentStatePropertyRule[]> ComponentStatePropertyRuleCache = [];

    private sealed class ComponentStatePropertyRule
    {
        public required PropertyInfo Property { get; init; }
        public required bool HasGenerator { get; init; }
    }

    private static ComponentStatePropertyRule[] GetComponentStatePropertyRules(Type componentType)
    {
        if (ComponentStatePropertyRuleCache.TryGetValue(componentType, out var cached))
            return cached;

        var rules = componentType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(p => p.GetIndexParameters().Length == 0)
            .Select(p => new
            {
                Property = p,
                Attribute = p.GetCustomAttribute<ComponentStateAttribute>(true)
            })
            .Where(x => x.Attribute != null)
            .OrderBy(x => x.Property.Name, StringComparer.Ordinal)
            .Select(x => new ComponentStatePropertyRule
            {
                Property = x.Property,
                HasGenerator = x.Attribute!.DynamicVarGenerator != null
            })
            .ToArray();

        ComponentStatePropertyRuleCache[componentType] = rules;
        return rules;
    }


    public static void SmartAddArgs(ICardComponent component, LocString loc)
    {
        var rules = GetComponentStatePropertyRules(component.GetType());

        foreach (var rule in rules)
        {
            if (rule.HasGenerator)
                continue;

            var value = rule.Property.GetValue(component);
            AddLocArg(loc, rule.Property.Name, value);
        }
    }

    private static void AddLocArg(LocString loc, string name, object? value)
    {
        if (value == null)
        {
            loc.Add(name, 0m);
            return;
        }

        switch (value)
        {
            case MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar dynamicVar:
                loc.Add(dynamicVar);
                return;
            case decimal dec:
                loc.Add(name, dec);
                return;
            case int i:
                loc.Add(name, i);
                return;
            case long l:
                loc.Add(name, l);
                return;
            case float f:
                loc.Add(name, (decimal)f);
                return;
            case double d:
                loc.Add(name, (decimal)d);
                return;
            case bool b:
                loc.Add(name, b);
                return;
            case string s:
                loc.Add(name, s);
                return;
            case IList<string> list:
                loc.Add(name, list);
                return;
            case LocString locString:
                loc.Add(name, locString);
                return;
            default:
                if (TryConvertToDecimal(value, out var numeric))
                    loc.Add(name, numeric);
                else
                    loc.AddObj(name, value);

                return;
        }
    }

    private static bool TryConvertToDecimal(object? value, out decimal result)
    {
        try
        {
            if (value == null)
            {
                result = 0;
                return false;
            }

            result = Convert.ToDecimal(value);
            return true;
        }
        catch
        {
            result = 0;
            return false;
        }
    }
}