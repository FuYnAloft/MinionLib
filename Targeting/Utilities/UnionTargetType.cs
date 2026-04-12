using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Action;

namespace MinionLib.Targeting.Utilities;

public class UnionTargetType(params ICustomTargetType[] targetTypes) : ICustomTargetType
{
    public bool IsSingleTarget => targetTypes.Any(targetType => targetType.IsSingleTarget);


    public bool IsValidTargetPreview(Creature target)
    {
        return targetTypes.Any(targetType => targetType.IsValidTargetPreview(target));
    }

    public bool IsValidTarget(CardModel card, Creature target)
    {
        return targetTypes.Any(targetType => targetType.IsValidTarget(card, target));
    }

    public bool IsValidTarget(PotionModel potion, Creature target)
    {
        return targetTypes.Any(targetType => targetType.IsValidTarget(potion, target));
    }

    public bool IsValidTarget(ActionModel action, Creature target)
    {
        return targetTypes.Any(targetType => targetType.IsValidTarget(action, target));
    }
}