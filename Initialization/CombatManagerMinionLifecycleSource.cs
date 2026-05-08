using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Rooms;

namespace MinionLib.Initialization;

public sealed class CombatManagerMinionLifecycleSource : IMinionCombatLifecycleSource
{
    private MinionCombatLifecycleSink? _sink;
    private bool _initialized;

    public void Initialize(MinionCombatLifecycleSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);

        if (_initialized)
            return;

        _sink = sink;
        CombatManager.Instance.TurnStarted += OnTurnStarted;
        CombatManager.Instance.TurnEnded += OnTurnEnded;
        CombatManager.Instance.CombatSetUp += OnCombatSetUp;
        CombatManager.Instance.CombatEnded += OnCombatEnded;
        _initialized = true;
    }

    public void Dispose()
    {
        if (!_initialized)
            return;

        CombatManager.Instance.TurnStarted -= OnTurnStarted;
        CombatManager.Instance.TurnEnded -= OnTurnEnded;
        CombatManager.Instance.CombatSetUp -= OnCombatSetUp;
        CombatManager.Instance.CombatEnded -= OnCombatEnded;

        _sink = null;
        _initialized = false;
    }

    private void OnTurnStarted(CombatState combatState)
    {
        if (combatState.CurrentSide == CombatSide.Player)
            _sink?.NotifyPlayerTurnStarted();
    }

    private void OnTurnEnded(CombatState combatState)
    {
        if (combatState.CurrentSide == CombatSide.Enemy)
            _sink?.NotifyPlayerTurnEnded();
    }

    private void OnCombatSetUp(CombatState combatState)
    {
        _sink?.NotifyCombatStarted();
    }

    private void OnCombatEnded(CombatRoom combatRoom)
    {
        _sink?.NotifyCombatEnded();
    }
}
