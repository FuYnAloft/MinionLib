using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Action;
using MinionLib.Models;

namespace MinionLib.Targeting.Pets;

public class AnyMinionOrOwnerTargetType : CustomTargetType
{
    public override bool IsSingleTarget => true;
    public override bool GeneralPredicate(Creature target)
    {
        return target.IsAlive && (target.IsPlayer || target is
            { Side: CombatSide.Player, IsPet: true, Monster: MinionModel });
    }

    public override bool CardPredicate(Creature target, CardModel card)
    {
        return GeneralPredicate(target) && (target.PetOwner == card.Owner || target.Player == card.Owner);
    }

    public override bool PotionPredicate(Creature target, MegaCrit.Sts2.Core.Models.PotionModel potion)
    {
        return GeneralPredicate(target) && (target.PetOwner == potion.Owner || target.Player == potion.Owner);
    }

    public override bool ActionPredicate(Creature target, CustomActionModel action, Creature actor)
    {
        return GeneralPredicate(target) && (target == actor || target.PetOwner == actor.Player ||
                                            target.Player == actor.PetOwner || target.PetOwner == actor.PetOwner);
    }
}