using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Action;

namespace MinionLib.Targeting.Utilities;

public class DifferenceTargetType(
    CustomTargetType original,
    CustomTargetType exclude,
    bool? overrideIsSingleTarget = null,
    bool? overrideIsRandomTarger = null) : CustomTargetType
{
    public override bool IsSingleTarget =>
        overrideIsSingleTarget ?? (original.IsSingleTarget || exclude.IsSingleTarget);

    public override bool IsRandomTarget =>
        overrideIsRandomTarger ?? (original.IsRandomTarget || exclude.IsRandomTarget);

    public override bool GeneralPredicate(Creature target)
    {
        return original.GeneralPredicate(target) && !exclude.GeneralPredicate(target);
    }

    public override bool CardPredicate(Creature target, CardModel card)
    {
        return original.CardPredicate(target, card) && !exclude.CardPredicate(target, card);
    }

    public override bool PotionPredicate(Creature target, PotionModel potion)
    {
        return original.PotionPredicate(target, potion) && !exclude.PotionPredicate(target, potion);
    }

    public override bool ActionPredicate(Creature target, CustomActionModel action, Creature actor)
    {
        return original.ActionPredicate(target, action, actor) && !exclude.ActionPredicate(target, action, actor);
    }
}