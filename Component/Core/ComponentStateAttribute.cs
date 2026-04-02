using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MinionLib.Component.Core;

[AttributeUsage(AttributeTargets.Property)]
public class ComponentStateAttribute : Attribute
{
    public Type? DynamicVarGenerator { get; }
    public object[] Parameters { get; } = [];

    public ComponentStateAttribute()
    {
    }

    public ComponentStateAttribute(Type dynamicVarGenerator, params object[] parameters)
    {
        DynamicVarGenerator = dynamicVarGenerator;
        Parameters = parameters;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class ComponentStateAttribute<T>(params object[] parameters)
    : ComponentStateAttribute(typeof(T), parameters)
    where T : DynamicVar;
