using Godot;

namespace MinionLib.Action;

internal static class CreatureActionDebounceGate
{
    private const ulong DebounceDurationMs = 200;
    private static readonly Dictionary<uint, ulong> BlockedUntilByActorCombatId = [];

    public static bool IsBlocked(uint actorCombatId)
    {
        var now = Time.GetTicksMsec();
        if (!BlockedUntilByActorCombatId.TryGetValue(actorCombatId, out var blockedUntil))
            return false;

        if (now < blockedUntil)
            return true;

        BlockedUntilByActorCombatId.Remove(actorCombatId);
        return false;
    }

    public static void MarkBlocked(uint actorCombatId)
    {
        BlockedUntilByActorCombatId[actorCombatId] = Time.GetTicksMsec() + DebounceDurationMs;
    }
}

