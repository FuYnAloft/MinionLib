using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Action;

namespace MinionLib.Targeting.Utilities;

public class IntersectionTargetType(
    CustomTargetType targetTypeA,
    CustomTargetType targetTypeB,
    bool? overrideIsSingleTarget = null,
    bool? overrideIsRandomTarger = null) : CustomTargetType
{
    public override bool IsSingleTarget =>
        overrideIsSingleTarget ?? (targetTypeA.IsSingleTarget || targetTypeB.IsSingleTarget);

    public override bool IsRandomTarget =>
        overrideIsRandomTarger ?? (targetTypeA.IsRandomTarget || targetTypeB.IsRandomTarget);

    public override bool GeneralPredicate(Creature target)
    {
        return targetTypeA.GeneralPredicate(target) && targetTypeB.GeneralPredicate(target);
    }

    public override bool CardPredicate(Creature target, CardModel card)
    {
        return targetTypeA.CardPredicate(target, card) && targetTypeB.CardPredicate(target, card);
    }

    public override bool PotionPredicate(Creature target, PotionModel potion)
    {
        return targetTypeA.PotionPredicate(target, potion) && targetTypeB.PotionPredicate(target, potion);
    }

    public override bool ActionPredicate(Creature target, CustomActionModel action, Creature actor)
    {
        return targetTypeA.ActionPredicate(target, action, actor) && targetTypeB.ActionPredicate(target, action, actor);
    }
}