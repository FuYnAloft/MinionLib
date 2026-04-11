using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Action;

namespace MinionLib.Targeting.Utilities;

public class LambdaTargetType(
    bool isSingleTarget,
    Func<Creature, bool> generalPredicate,
    Func<CardModel, Creature, bool>? cardPredicate = null,
    Func<PotionModel, Creature, bool>? potionPredicate = null,
    Func<ActionModel, Creature, bool>? actionPredicate = null
) : CustomTargetType
{
    public override bool IsSingleTarget => isSingleTarget;


    protected override bool IsValidTarget(Creature target)
    {
        return generalPredicate(target);
    }

    public override bool IsValidTarget(CardModel card, Creature target)
    {
        return cardPredicate?.Invoke(card, target) ?? generalPredicate(target);
    }

    public override bool IsValidTarget(PotionModel potion, Creature target)
    {
        return potionPredicate?.Invoke(potion, target) ?? generalPredicate(target);
    }

    public override bool IsValidTarget(ActionModel action, Creature target)
    {
        return actionPredicate?.Invoke(action, target) ?? generalPredicate(target);
    }
}