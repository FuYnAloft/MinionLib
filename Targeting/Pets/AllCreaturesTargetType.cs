using MegaCrit.Sts2.Core.Entities.Creatures;

namespace MinionLib.Targeting.Pets;

public class AllCreaturesTargetType : CustomTargetType
{
    public override bool IsSingleTarget => false;

    protected override bool IsValidTarget(Creature target)
    {
        return target.IsAlive;
    }
}