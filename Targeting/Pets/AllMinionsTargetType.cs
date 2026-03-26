using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Action;
using MinionLib.Models;

namespace MinionLib.Targeting.Pets;

public class AllMinionsTargetType : CustomTargetType
{
    public override bool IsSingleTarget => false;

    public override bool GeneralPredicate(Creature target)
    {
        return target is { IsAlive: true, Side: CombatSide.Player, IsPet: true, Monster: MinionModel };
    }

    public override bool CardPredicate(Creature target, CardModel card)
    {
        return GeneralPredicate(target) && target.PetOwner == card.Owner;
    }

    public override bool PotionPredicate(Creature target, PotionModel potion)
    {
        return GeneralPredicate(target) && target.PetOwner == potion.Owner;
    }

    public override bool ActionPredicate(Creature target, CustomActionModel action, Creature actor)
    {
        return GeneralPredicate(target) && (target.PetOwner == actor.PetOwner || target.PetOwner == actor.Player);
    }
}