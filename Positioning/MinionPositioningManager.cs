using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MinionLib.Models;

namespace MinionLib.Positioning;

public static class MinionPositioningManager
{
    private static readonly List<(IMinionPositioner positioner, int priority, int order)> PositionersWithPriority = [];
    private static int _counter;


    static MinionPositioningManager()
    {
        Register(new DefaultMinionsPositioner(), int.MinValue);
    }

    public static IEnumerable<IMinionPositioner> Positioners
        => PositionersWithPriority.Select(x => x.positioner);

    public static void Register(IMinionPositioner positioner, int priority = 0)
    {
        PositionersWithPriority.Add((positioner, priority, _counter++));

        PositionersWithPriority.Sort((a, b) =>
        {
            var pComp = b.priority.CompareTo(a.priority);
            return pComp != 0 ? pComp : b.order.CompareTo(a.order);
        });
    }

    public static IEnumerable<MinionNodePosition> CalculatePositions(NCombatRoom room)
    {
        foreach (var positioner in Positioners)
            if (positioner.IsActive)
                return positioner.CalculatePositions(room);
        throw new InvalidOperationException("No positioner found");
    }


    public static IReadOnlyList<MinionNodePosition> GetCurrentMinionPositions(NCombatRoom room)
    {
        var minions = room.CreatureNodes.Where(n => n.IsMinionNode());
        return minions.Select(c => new MinionNodePosition(c, c.Position)).ToList();
    }
}

public static class NCreatureExtensions
{
    public static bool IsMinionNode(this NCreature node)
    {
        return node.Entity is { Monster: MinionModel, IsAlive: true, PetOwner: not null };
    }
}