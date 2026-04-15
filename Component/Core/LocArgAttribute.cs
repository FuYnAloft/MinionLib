namespace MinionLib.Component.Core;

[AttributeUsage(AttributeTargets.Property)]
public class LocArgAttribute(string? name = null) : Attribute
{
    public string? Name { get; } = name;
}

[AttributeUsage(AttributeTargets.Property)]
public class NotLocArgAttribute : Attribute;

[AttributeUsage(AttributeTargets.Property)]
public class NestedLocStringAttribute : Attribute;