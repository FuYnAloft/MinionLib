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
        public required ComponentStateAttribute Attribute { get; init; }
        public required MethodInfo? GeneratorMethod { get; init; }
        public bool HasGenerator => GeneratorMethod != null;
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
                Attribute = p.GetCustomAttribute<ComponentStateAttribute>(inherit: true)
            })
            .Where(x => x.Attribute != null)
            .OrderBy(x => x.Property.Name, StringComparer.Ordinal)
            .Select(x => new ComponentStatePropertyRule
            {
                Property = x.Property,
                Attribute = x.Attribute!,
                GeneratorMethod = ResolveGeneratorMethod(x.Attribute!.DynamicVarGenerator)
            })
            .ToArray();

        ComponentStatePropertyRuleCache[componentType] = rules;
        return rules;
    }

    private static MethodInfo? ResolveGeneratorMethod(Type? generatorType)
    {
        if (generatorType == null)
            return null;

        var generateMethod = generatorType.GetMethod("Generate", BindingFlags.Public | BindingFlags.Static,
            binder: null, [typeof(string), typeof(object[])], modifiers: null);

        if (generateMethod == null || !typeof(DynamicVar).IsAssignableFrom(generateMethod.ReturnType))
            throw new InvalidOperationException(
                $"DynamicVar generator {generatorType.FullName} must define public static DynamicVar Generate(string, object[]).");

        return generateMethod;
    }

    public static DynamicVarSet GenerateDynamicVars(ICardComponent component)
    {
        var rules = GetComponentStatePropertyRules(component.GetType());
        var vars = new List<DynamicVar>();

        foreach (var rule in rules)
        {
            if (!rule.HasGenerator)
                continue;

            DynamicVar dynamicVar;
            try
            {
                dynamicVar = (DynamicVar)rule.GeneratorMethod!.Invoke(null,
                    [rule.Property.Name, rule.Attribute.Parameters])!;
            }
            catch (Exception ex)
            {
                Debug("Component",
                    $"Failed to generate DynamicVar for {component.GetType().Name}.{rule.Property.Name}: {ex.Message}");
                continue;
            }

            var propertyValue = rule.Property.GetValue(component);
            if (TryConvertToDecimal(propertyValue, out var numericValue))
            {
                dynamicVar.BaseValue = numericValue;
            }

            vars.Add(dynamicVar);
        }

        return new DynamicVarSet(vars);
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
            case DynamicVar dynamicVar:
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
                {
                    loc.Add(name, numeric);
                }
                else
                {
                    loc.AddObj(name, value);
                }

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