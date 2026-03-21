using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MinionLib.Models;

namespace MinionLib.Utilities;

public static class MinionPositioningHelper
{
    private static readonly Vector2 MinionSize = new(150f, 200f);

    private static readonly IReadOnlyDictionary<MinionPosition, Vector2> PositionOffsets =
        new Dictionary<MinionPosition, Vector2>
        {
            [MinionPosition.Front] = new(200f, 0),
            [MinionPosition.Back] = new(-200f, 0),
            [MinionPosition.FrontUpper] = new(200f, -450f),
            [MinionPosition.BackUpper] = new(-200f, -450f),
            [MinionPosition.Upper] = new(0, -450f)
        };

    private static IReadOnlyList<Vector2> GenerateGridPoints(MinionPosition position, int count)
    {
        if (count <= 0) return [];

        IReadOnlyList<Vector2> result;
        if (position == MinionPosition.Upper)
        {
            var rows = count / 2;
            result = Enumerable.Range(1, count)
                .Select(i => new Vector2(
                    i == count && count % 2 == 1
                        ? 0f
                        : -0.5f + i % 2,
                    -(i - 1) / 2)).ToList();
        }
        else
        {
            if (count == 1) return [new Vector2(0f, 0f)];

            var isFront = position is MinionPosition.Front or MinionPosition.FrontUpper;
            var turningPoint = isFront ? 3f : 1.5f;
            var slope = isFront ? 0.75f : 0.25f;

            var fullWidth = (count - 1) * 0.5f;
            var width = fullWidth <= turningPoint
                ? fullWidth
                : turningPoint + slope * MathF.Log((fullWidth - turningPoint) / slope + 1);
            var last = 0f;
            var first = isFront ? width : -width;
            result = Enumerable.Range(0, count)
                .Select(i => new Vector2(
                    float.Lerp(first, last, (float)i / (count - 1)),
                    -i % 2)).ToList();
        }

        return result;
    }

    private static bool IsMinionNode(NCreature node)
    {
        return node.Entity is { Monster: MinionModel, IsAlive: true, PetOwner: not null };
    }

    private static IReadOnlyList<OwnerWithMinionsNodes> GetMinionOwnerNodePairs(NCombatRoom room)
    {
        var allMinions = room.CreatureNodes.Where(IsMinionNode);
        var grouped = allMinions.GroupBy(c => c.Entity.PetOwner!);
        var result = grouped.Select(g =>
            {
                var player = g.Key;
                var creatureToNode = g.ToDictionary(c => c.Entity, c => c);
                var pets = player.PlayerCombatState!.Pets;
                var orderedMinions = pets
                    .Select(p => creatureToNode.GetValueOrDefault(p))
                    .OfType<NCreature>()
                    .ToList();
                return new OwnerWithMinionsNodes(room.GetCreatureNode(g.Key.Creature)!, orderedMinions);
            })
            .ToList();
        return result;
    }

    public static IReadOnlyList<MinionNodePosition> CalculateMinionPositions(NCombatRoom room)
    {
        return GetMinionOwnerNodePairs(room).SelectMany(pair =>
        {
            var (ownerNode, minionNodes) = pair;

            var grouped = minionNodes.GroupBy(c => ((MinionModel)c.Entity.Monster!).Position);

            var nodePositions = grouped.SelectMany(g =>
            {
                var minionPosition = g.Key;
                var positions = GenerateGridPoints(minionPosition, g.Count())
                    .Select(v => v * MinionSize + PositionOffsets[minionPosition] + ownerNode.Position);
                return g.Zip(positions, (node, position) => new MinionNodePosition(node, position));
            });

            return nodePositions;
        }).ToList();
    }

    public static IReadOnlyList<MinionNodePosition> GetCurrentMinionPositions(NCombatRoom room)
    {
        var minions = room.CreatureNodes.Where(IsMinionNode);
        return minions.Select(c => new MinionNodePosition(c, c.Position)).ToList();
    }
}

public readonly record struct OwnerWithMinionsNodes(NCreature Owner, IReadOnlyList<NCreature> Minions);

public readonly record struct MinionNodePosition(NCreature Node, Vector2 Position);