using MegaCrit.Sts2.Core.Nodes.Combat;
using MinionLib.Minion;

namespace MinionLib.Layout;

public static class NCreatureExtensions
{
    public static bool IsMinionNode(this NCreature node)
    {
        return node.Entity is { Monster: MinionModel, IsAlive: true, PetOwner: not null };
    }
}