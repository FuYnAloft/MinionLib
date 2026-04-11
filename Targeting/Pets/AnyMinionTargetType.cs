using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Action;
using MinionLib.Minion;

namespace MinionLib.Targeting.Pets;

public class AnyMinionTargetType : CustomTargetType
{
    public override bool IsSingleTarget => true;

    protected override bool IsValidTarget(Creature target)
    {
        return target is { IsAlive: true, Side: CombatSide.Player, IsPet: true, Monster: MinionModel };
    }

    public override bool IsValidTarget(CardModel card, Creature target)
    {
        return IsValidTarget(target) && target.PetOwner == card.Owner;
    }

    public override bool IsValidTarget(PotionModel potion, Creature target)
    {
        return IsValidTarget(target) && target.PetOwner == potion.Owner;
    }

    public override bool IsValidTarget(ActionModel action, Creature target)
    {
        var actor = action.Owner;
        return IsValidTarget(target) && (target.PetOwner == actor.PetOwner || target.PetOwner == actor.Player);
    }
}