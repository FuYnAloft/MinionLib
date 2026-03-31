namespace MinionLib.Component.Core;

public enum ComponentPhase
{
    Init,
    Prime,
    Prefix,
    Core,
    Postfix,
    Final
}

public static class ComponentPhaseExtensions
{
    public static ComponentPhase NextPhase(this ComponentPhase phase)
    {
        return phase switch
        {
            ComponentPhase.Init => ComponentPhase.Prime,
            ComponentPhase.Prime => ComponentPhase.Prefix,
            ComponentPhase.Prefix => ComponentPhase.Core,
            ComponentPhase.Core => ComponentPhase.Postfix,
            ComponentPhase.Postfix => ComponentPhase.Final,
            ComponentPhase.Final => ComponentPhase.Final,
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null)
        };
    }
}

public sealed class ComponentContext(ComponentPhase phase)
{
    public ComponentPhase Phase { get; set; } = phase;

    public void MoveNextPhase()
    {
        Phase = Phase.NextPhase();
    }
}