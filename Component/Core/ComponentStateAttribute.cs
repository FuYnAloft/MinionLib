using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MinionLib.Component.Core;

[AttributeUsage(AttributeTargets.Property)]
public class ComponentStateAttribute : Attribute;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ComponentStateAttribute<T>(params object[] parameters): ComponentStateAttribute
    where T : DynamicVar
{
    private readonly object[] _parameters = parameters;
}
