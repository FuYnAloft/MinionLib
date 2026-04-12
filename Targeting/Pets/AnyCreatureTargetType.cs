using MegaCrit.Sts2.Core.Entities.Creatures;

namespace MinionLib.Targeting.Pets;

public class AnyCreatureTargetType : CustomTargetType
{
    public override bool IsSingleTarget => true;

    protected override bool IsValidTarget(Creature target)
    {
        return target.IsAlive;
    }
}