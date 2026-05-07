using MegaCrit.Sts2.Core.Combat;
using MinionLib.Action;
using MinionLib.Commands;
using STS2RitsuLib;

namespace MinionLib.Initialization;

/// <summary>
///     在玩家回合开始和结束时自动重排随从位置。
///     通过 RitsuLib lifecycle 事件实现。
/// </summary>
public static class MinionHookInitializer
{
    private static bool _initialized;
    private static IDisposable? _combatStartingSubscription;
    private static IDisposable? _combatEndedSubscription;
    private static IDisposable? _sideTurnStartedSubscription;

    public static void Initialize()
    {
        if (_initialized) return;

        _combatStartingSubscription =
            RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(OnCombatStarting, replayCurrentState: false);
        _combatEndedSubscription =
            RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(OnCombatEnded, replayCurrentState: false);
        _sideTurnStartedSubscription =
            RitsuLibFramework.SubscribeLifecycle<SideTurnStartedEvent>(OnSideTurnStarted, replayCurrentState: false);

        _initialized = true;
    }

    public static void Deinitialize()
    {
        _combatStartingSubscription?.Dispose();
        _combatEndedSubscription?.Dispose();
        _sideTurnStartedSubscription?.Dispose();

        _combatStartingSubscription = null;
        _combatEndedSubscription = null;
        _sideTurnStartedSubscription = null;
        _initialized = false;
    }

    private static void OnSideTurnStarted(SideTurnStartedEvent evt)
    {
        if (evt.Side == CombatSide.Player)
        {
            _ = MinionAnimCmd.Rearrange();
            return;
        }

        if (evt.Side == CombatSide.Enemy)
        {
            CreatureActionQueueThreshold.Clear();
            _ = MinionAnimCmd.Rearrange();
        }
    }

    private static void OnCombatStarting(CombatStartingEvent evt)
    {
        CreatureActionQueueThreshold.Clear();
        PetOrderSnapshotManager.ClearAllSnapshots();
    }

    private static void OnCombatEnded(CombatEndedEvent evt)
    {
        CreatureActionQueueThreshold.Clear();
        PetOrderSnapshotManager.ClearAllSnapshots();
    }
}
