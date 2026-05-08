namespace MinionLib.Initialization;

public static class MinionRuntime
{
    private static readonly Lock SyncRoot = new();
    private static readonly MinionCombatLifecycleSink LifecycleSink = new();
    private static IMinionCombatLifecycleSource? _lifecycleSource;

    public static void UseLifecycleSource(IMinionCombatLifecycleSource lifecycleSource)
    {
        ArgumentNullException.ThrowIfNull(lifecycleSource);

        lock (SyncRoot)
        {
            _lifecycleSource?.Dispose();
            _lifecycleSource = lifecycleSource;
            _lifecycleSource.Initialize(LifecycleSink);
        }
    }

    public static void UseDefaultLifecycleSource()
    {
        UseLifecycleSource(new CombatManagerMinionLifecycleSource());
    }

    public static void Deinitialize()
    {
        lock (SyncRoot)
        {
            _lifecycleSource?.Dispose();
            _lifecycleSource = null;
        }
    }
}
