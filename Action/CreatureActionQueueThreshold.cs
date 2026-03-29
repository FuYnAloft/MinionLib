using MegaCrit.Sts2.Core.Models;

namespace MinionLib.Action;

internal static class CreatureActionQueueThreshold
{
    private static readonly Dictionary<(uint actorCombatId, ModelId actionId), int> QueuedCount = [];

    public static bool IsExhausted(CustomActionModel action)
    {
        var actor = action.Owner;
        if (actor.CombatId == null)
            return true;

        return action.Amount <= GetQueuedCount(actor.CombatId.Value, action.Id);
    }

    public static bool TryReserve(CustomActionModel action)
    {
        var actor = action.Owner;
        if (actor.CombatId == null)
            return false;

        var actorCombatId = actor.CombatId.Value;
        if (action.Amount <= GetQueuedCount(actorCombatId, action.Id))
            return false;

        var key = (actorCombatId, action.Id);
        QueuedCount[key] = GetQueuedCount(actorCombatId, action.Id) + 1;
        return true;
    }

    public static void Release(uint actorCombatId, ModelId actionId)
    {
        var key = (actorCombatId, actionId);
        if (!QueuedCount.TryGetValue(key, out var value))
            return;

        value--;
        if (value <= 0)
            QueuedCount.Remove(key);
        else
            QueuedCount[key] = value;
    }

    public static void Clear()
    {
        QueuedCount.Clear();
    }

    private static int GetQueuedCount(uint actorCombatId, ModelId actionId)
    {
        return QueuedCount.TryGetValue((actorCombatId, actionId), out var value) ? value : 0;
    }
}


