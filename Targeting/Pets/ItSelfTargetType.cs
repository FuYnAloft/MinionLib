using MegaCrit.Sts2.Core.Entities.Creatures;
using MinionLib.Models;

namespace MinionLib.Targeting.Pets;

public class ItselfTargetType : CustomTargetType
{
    public override bool IsSingleTarget => true;

    public override bool GeneralPredicate(Creature target)
    {
        return false;
    }

    public override bool ActionPredicate(Creature target, CustomActionModel action, Creature actor)
    {
        return target == actor;
    }
}