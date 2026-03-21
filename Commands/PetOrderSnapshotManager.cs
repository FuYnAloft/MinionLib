using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace MinionLib.Commands;

public static class PetOrderSnapshotManager
{
    private static readonly object Sync = new();
    private static ConditionalWeakTable<Player, SnapshotEntry> _snapshots = new();

    public static void TakeSnapshot(Player player)
    {
        if (player.PlayerCombatState == null) return;

        var ids = player.PlayerCombatState.Pets
            .Where(p => p.CombatId.HasValue)
            .Select(p => p.CombatId!.Value)
            .ToList();

        lock (Sync)
        {
            _snapshots.Remove(player);
            _snapshots.Add(player, new SnapshotEntry(ids));
        }
    }

    public static IReadOnlyList<Creature> GetSnapshot(Player player, bool onlyAlive = true, bool includeMissing = true)
    {
        if (player.PlayerCombatState == null) return [];

        var currentPets = player.PlayerCombatState.Pets;
        var combatState = player.Creature.CombatState;

        List<uint> orderedIds;
        lock (Sync)
        {
            orderedIds = _snapshots.TryGetValue(player, out var snapshot)
                ? [.. snapshot.CombatIds]
                : [];
        }

        if (includeMissing)
        {
            HashSet<uint> known = [.. orderedIds];
            foreach (var pet in currentPets)
            {
                if (!pet.CombatId.HasValue || known.Contains(pet.CombatId.Value)) continue;

                orderedIds.Add(pet.CombatId.Value);
                known.Add(pet.CombatId.Value);
            }
        }

        List<Creature> result = [];
        HashSet<uint> added = [];

        foreach (var id in orderedIds)
        {
            if (!added.Add(id)) continue;

            var pet = combatState?.GetCreature(id) ?? currentPets.FirstOrDefault(p => p.CombatId == id);
            if (pet == null) continue;

            if (onlyAlive && !pet.IsAlive) continue;

            result.Add(pet);
        }

        // Keep pets without combat id accessible as tail entries when requested.
        if (includeMissing)
            foreach (var pet in currentPets)
            {
                if (pet.CombatId.HasValue) continue;

                if (onlyAlive && !pet.IsAlive) continue;

                result.Add(pet);
            }

        return result;
    }

    public static void ClearAllSnapshots()
    {
        lock (Sync)
        {
            _snapshots = new ConditionalWeakTable<Player, SnapshotEntry>();
        }
    }

    private sealed class SnapshotEntry
    {
        public SnapshotEntry(List<uint> combatIds)
        {
            CombatIds = combatIds;
        }

        public List<uint> CombatIds { get; }
    }
}