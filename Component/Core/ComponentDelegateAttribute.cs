namespace MinionLib.Component.Core;

[AttributeUsage(AttributeTargets.Method)]
public class ComponentDelegateAttribute : Attribute
{
    public ComponentDelegateAttribute()
    {
    }

    public ComponentDelegateAttribute(string name)
    {
    }

    public ComponentDelegateAttribute(string @namespace, string name)
    {
    }
}