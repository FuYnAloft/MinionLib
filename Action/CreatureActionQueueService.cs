using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MinionLib.Action.GameActions;

namespace MinionLib.Action;

internal static class CreatureActionQueueService
{
    public static bool TryEnqueue(ActionModel action, Creature? target)
    {
        var actor = action.Owner;
        if (!CombatManager.Instance.IsInProgress || actor.CombatId == null)
            return false;

        var queueSynchronizer = RunManager.Instance.ActionQueueSynchronizer;
        if (queueSynchronizer.CombatState != ActionSynchronizerCombatState.PlayPhase)
            return false;
        
        if (!CreatureActionQueueThreshold.TryReserve(action))
            return false;

        var queuedAction = new ExecuteCreatureActionGameAction(action, target);

        try
        {
            queueSynchronizer.RequestEnqueue(queuedAction);
        }
        catch
        {
            CreatureActionQueueThreshold.Release(actor.CombatId.Value, action.Id);
            throw;
        } 

        return true;
    }
}

