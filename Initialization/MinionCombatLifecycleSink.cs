namespace MinionLib.Initialization;

public sealed class MinionCombatLifecycleSink
{
    public void NotifyPlayerTurnStarted()
    {
        MinionHookInitializer.OnPlayerTurnStarted();
    }

    public void NotifyPlayerTurnEnded()
    {
        MinionHookInitializer.OnPlayerTurnEnded();
    }

    public void NotifyCombatStarted()
    {
        MinionHookInitializer.OnCombatStarted();
    }

    public void NotifyCombatEnded()
    {
        MinionHookInitializer.OnCombatEnded();
    }
}
