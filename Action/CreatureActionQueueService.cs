using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MinionLib.Action.GameActions;

namespace MinionLib.Action;

internal static class CreatureActionQueueService
{
    public static bool TryEnqueue(Creature actor, CustomActionModel action, Creature? target)
    {
        if (!CombatManager.Instance.IsInProgress || actor.CombatId == null)
            return false;

        if (CreatureActionDebounceGate.IsBlocked(actor.CombatId.Value))
            return false;

        var owner = ResolveQueueOwner(actor);
        if (owner == null)
            return false;

        var queuedAction = new ExecuteCreatureActionGameAction(owner, actor, action, target);

        RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(queuedAction);

        CreatureActionDebounceGate.MarkBlocked(actor.CombatId.Value);
        return true;
    }

    private static Player? ResolveQueueOwner(Creature actor)
    {
        if (actor.PetOwner != null)
            return actor.PetOwner;

        if (actor.Player != null)
            return actor.Player;

        if (actor.CombatState != null)
            return LocalContext.GetMe(actor.CombatState);

        return null;
    }
}

