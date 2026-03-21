using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Models;

namespace MinionLib.Powers;

public sealed class MinionGuardianPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override string CustomPackedIconPath => "res://Example/MinionTest/orb.png";

    public override string CustomBigIconPath => "res://Example/MinionTest/orb.png";

    public override string CustomBigBetaIconPath => "res://Example/MinionTest/orb.png";

    public override Creature ModifyUnblockedDamageTarget(Creature target, decimal amount, ValueProp props,
        Creature? dealer)
    {
        if (Owner.Monster is MinionModel minion && minion.Position != MinionPosition.Front) return target;

        if (target != Owner.PetOwner?.Creature)
        {
            var flag = true;

            if (target.PetOwner == Owner.PetOwner && Owner.PetOwner != null &&
                target.GetPower<MinionGuardianPower>() != null)
            {
                var pets = target.PetOwner!.PlayerCombatState!.Pets;
                if (pets.IndexOf(Owner) < pets.IndexOf(target))
                    flag = false;
            }

            if (flag)
                return target;
        }

        if (Owner.IsDead) return target;

        if (!props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered)) return target;

        return Owner;
    }
}