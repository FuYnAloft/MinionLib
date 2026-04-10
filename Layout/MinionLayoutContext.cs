using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MinionLib.Layout;

public class MinionLayoutContext
{
    public NCombatRoom Room { get; }
    
    public IReadOnlyList<NCreature> AllMinions { get; }
    
    public Dictionary<NCreature, Vector2> Positions { get; }
    
    public IEnumerable<NCreature> UnhandledMinions => AllMinions.Where(m => !Positions.ContainsKey(m));

    public MinionLayoutContext(NCombatRoom room)
    {
        Room = room;
        AllMinions = room.CreatureNodes.Where(n => n.IsMinionNode()).ToList();
        Positions = new Dictionary<NCreature, Vector2>();
    }
}