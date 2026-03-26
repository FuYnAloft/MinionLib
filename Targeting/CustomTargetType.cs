using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Action;

namespace MinionLib.Targeting;

public abstract class CustomTargetType
{
    public abstract bool IsSingleTarget { get; }

    public virtual bool IsRandomTarget => false;

    public abstract bool GeneralPredicate(Creature target);

    public virtual bool CardPredicate(Creature target, CardModel card)
    {
        return GeneralPredicate(target);
    }

    public virtual bool PotionPredicate(Creature target, PotionModel potion)
    {
        return GeneralPredicate(target);
    }

    public virtual bool ActionPredicate(Creature target, CustomActionModel action, Creature actor)
    {
        return GeneralPredicate(target);
    }
}