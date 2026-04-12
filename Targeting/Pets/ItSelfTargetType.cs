using MegaCrit.Sts2.Core.Entities.Creatures;
using MinionLib.Action;

namespace MinionLib.Targeting.Pets;

public class ItselfTargetType : CustomTargetType
{
    public override bool IsSingleTarget => true;

    protected override bool IsValidTarget(Creature target)
    {
        return false;
    }

    public override bool IsValidTarget(ActionModel action, Creature target)
    {
        return target == action.Owner;
    }
}