using MegaCrit.Sts2.Core.Combat;
using MinionLib.Initialization;
using STS2RitsuLib;

namespace MinionLib.RitsuCompat;

public sealed class RitsuMinionLifecycleSource : IMinionCombatLifecycleSource
{
    private readonly List<IDisposable> _subscriptions = [];
    private MinionCombatLifecycleSink? _sink;

    public void Initialize(MinionCombatLifecycleSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);

        if (_sink != null)
            return;

        _sink = sink;
        _subscriptions.Add(RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(OnCombatStarting, false));
        _subscriptions.Add(RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(OnCombatEnded, false));
        _subscriptions.Add(RitsuLibFramework.SubscribeLifecycle<SideTurnStartedEvent>(OnSideTurnStarted, false));
        _subscriptions.Add(RitsuLibFramework.SubscribeLifecycle<SideTurnStartingEvent>(OnSideTurnStarting, false));
    }

    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
            subscription.Dispose();

        _subscriptions.Clear();
        _sink = null;
    }

    private void OnCombatStarting(CombatStartingEvent evt)
    {
        _sink?.NotifyCombatStarted();
    }

    private void OnCombatEnded(CombatEndedEvent evt)
    {
        _sink?.NotifyCombatEnded();
    }

    private void OnSideTurnStarted(SideTurnStartedEvent evt)
    {
        if (evt.Side == CombatSide.Player)
            _sink?.NotifyPlayerTurnStarted();
    }

    private void OnSideTurnStarting(SideTurnStartingEvent evt)
    {
        if (evt.Side == CombatSide.Enemy)
            _sink?.NotifyPlayerTurnEnded();
    }
}
