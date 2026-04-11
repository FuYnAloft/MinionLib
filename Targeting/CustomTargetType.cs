using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Action;

namespace MinionLib.Targeting;

public abstract class CustomTargetType : ICustomTargetType
{
    public abstract bool IsSingleTarget { get; }

    public virtual bool IsRandomTarget => false;

    protected abstract bool IsValidTarget(Creature target);

    public bool IsValidTargetPreview(Creature target)
    {
        return IsValidTarget(target);
    }

    public virtual bool IsValidTarget(CardModel card, Creature target)
    {
        return IsValidTarget(target);
    }

    public virtual bool IsValidTarget(PotionModel potion, Creature target)
    {
        return IsValidTarget(target);
    }

    public virtual bool IsValidTarget(ActionModel action, Creature target)
    {
        return IsValidTarget(target);
    }
}