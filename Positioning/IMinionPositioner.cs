using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MinionLib.Positioning;

public interface IMinionPositioner
{
    public bool IsActive { get; }

    IEnumerable<MinionNodePosition> CalculatePositions(
        NCombatRoom room);
}

public readonly record struct OwnerWithMinionsNodes(NCreature Owner, IReadOnlyList<NCreature> Minions);

public readonly record struct MinionNodePosition(NCreature Node, Vector2 Position);