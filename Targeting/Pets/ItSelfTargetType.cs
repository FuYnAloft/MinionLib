using MegaCrit.Sts2.Core.Entities.Creatures;
using MinionLib.Action;

namespace MinionLib.Targeting.Pets;

public class ItselfTargetType : CustomTargetType
{
    public override bool IsSingleTarget => true;

    public override bool GeneralPredicate(Creature target)
    {
        return false;
    }

    public override bool ActionPredicate(Creature target, ActionModel action)
    {
        return target == action.Owner;
    }
}