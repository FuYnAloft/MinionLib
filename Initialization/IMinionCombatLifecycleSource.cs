namespace MinionLib.Initialization;

public interface IMinionCombatLifecycleSource : IDisposable
{
    void Initialize(MinionCombatLifecycleSink sink);
}
