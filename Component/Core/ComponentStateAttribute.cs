using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MinionLib.Component.Core;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ComponentStateAttribute : Attribute;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ComponentStateAttribute<T>(params object[] parameters): Attribute
    where T : DynamicVar
{
    private readonly object[] _parameters = parameters;
}