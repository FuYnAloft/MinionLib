using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace MinionLib.Layout;

public interface IMinionLayout
{
    bool IsActive { get; }
    
    void ApplyLayout(MinionLayoutContext context);
}

public readonly record struct OwnerWithMinionsNodes(NCreature Owner, IReadOnlyList<NCreature> Minions);

public readonly record struct MinionNodePosition(NCreature Node, Vector2 Position);