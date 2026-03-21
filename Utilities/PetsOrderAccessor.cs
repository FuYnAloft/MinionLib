using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MinionLib.Commands;

namespace MinionLib.Utilities;

public class PetsOrderAccessor(Player player) : IDisposable
{
    private readonly int _count = GetRawPetsList(player)?.Count ?? 0;
    public readonly List<Creature>? Pets = GetRawPetsList(player);

    public void Dispose()
    {
        if ((Pets?.Count ?? 0) != _count)
            throw new InvalidOperationException("PetsAccessor should not be used for operations other than reordering");
        PetOrderSnapshotManager.TakeSnapshot(player);
        _ = MinionAnimCmd.Rearrange();
        GC.SuppressFinalize(this);
    }

    public static List<Creature>? GetRawPetsList(Player player)
    {
        return (List<Creature>?)player.PlayerCombatState?.Pets;
    }
}