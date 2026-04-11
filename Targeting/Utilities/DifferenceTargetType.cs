using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Action;

namespace MinionLib.Targeting.Utilities;

public class DifferenceTargetType(
    ICustomTargetType original,
    ICustomTargetType exclude,
    bool? overrideIsSingleTarget = null) : ICustomTargetType
{
    public bool IsSingleTarget =>
        overrideIsSingleTarget ?? (original.IsSingleTarget || exclude.IsSingleTarget);


    public bool IsValidTargetPreview(Creature target)
    {
        return original.IsValidTargetPreview(target) && !exclude.IsValidTargetPreview(target);
    }

    public bool IsValidTarget(CardModel card, Creature target)
    {
        return original.IsValidTarget(card, target) && !exclude.IsValidTarget(card, target);
    }

    public bool IsValidTarget(PotionModel potion, Creature target)
    {
        return original.IsValidTarget(potion, target) && !exclude.IsValidTarget(potion, target);
    }

    public bool IsValidTarget(ActionModel action, Creature target)
    {
        return original.IsValidTarget(action, target) && !exclude.IsValidTarget(action, target);
    }
}