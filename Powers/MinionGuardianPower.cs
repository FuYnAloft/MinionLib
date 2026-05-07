using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Minion;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MinionLib.Powers;

[RegisterPower]
public sealed class MinionGuardianPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override string? CustomIconPath => "res://images/powers/minion_guardian_power_packed.png";

    public override string? CustomBigIconPath => "res://images/powers/minion_guardian_power.png";

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
