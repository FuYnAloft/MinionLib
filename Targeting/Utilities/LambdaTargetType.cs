using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Action;

namespace MinionLib.Targeting.Utilities;

public class LambdaTargetType(
    bool isSingleTarget,
    Func<Creature, bool> generalPredicate,
    Func<Creature, CardModel, bool>? cardPredicate = null,
    Func<Creature, PotionModel, bool>? potionPredicate = null,
    Func<Creature, ActionModel, bool>? actionPredicate = null,
    bool isRandomTarget = false
) : CustomTargetType
{
    public override bool IsSingleTarget => isSingleTarget;

    public override bool IsRandomTarget => isRandomTarget;

    public override bool GeneralPredicate(Creature target)
    {
        return generalPredicate(target);
    }

    public override bool CardPredicate(Creature target, CardModel card)
    {
        return cardPredicate?.Invoke(target, card) ?? generalPredicate(target);
    }

    public override bool PotionPredicate(Creature target, PotionModel potion)
    {
        return potionPredicate?.Invoke(target, potion) ?? generalPredicate(target);
    }

    public override bool ActionPredicate(Creature target, ActionModel action)
    {
        return actionPredicate?.Invoke(target, action) ?? generalPredicate(target);
    }
}