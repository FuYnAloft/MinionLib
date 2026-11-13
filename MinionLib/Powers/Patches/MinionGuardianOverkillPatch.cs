using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Commands;
using MinionLib.Minion;

namespace MinionLib.Powers.Patches;

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Damage), typeof(PlayerChoiceContext),
    typeof(IEnumerable<DamageTargetData>), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel),
    typeof(CardPlay), typeof(ModifyDamageHookType))]
public static class MinionGuardianOverkillPatch
{
    private static readonly AsyncLocal<bool> IsHandling = new();
    public static readonly AsyncLocal<Creature?> SuppressedOwner = new();

    [HarmonyPrefix]
    private static bool Prefix(PlayerChoiceContext choiceContext, IEnumerable<DamageTargetData>? targetData, decimal amount,
        ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, ModifyDamageHookType modifyDamageHookType,
        ref Task<IEnumerable<DamageResult>> __result)
    {
        if (IsHandling.Value || targetData == null) return true;

        var targetList = targetData as IReadOnlyList<DamageTargetData> ?? targetData.ToArray();
        if (targetList.Count != 1) return true;

        var target = targetList[0].target;

        if (!ShouldHandle(target, props)) return true;

        __result = HandleWithOverkillRedirect(choiceContext, targetList, amount, props, dealer, cardSource, cardPlay,
            modifyDamageHookType);
        return false;
    }

    private static bool ShouldHandle(Creature target, ValueProp props)
    {
        if (!target.IsPlayer || target.Player == null || target.IsDead || target.CombatState == null) return false;

        if (!props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered)) return false;

        return target.Pets.Any(p => p.IsAlive && IsFrontGuardian(p));
    }

    private static async Task<IEnumerable<DamageResult>> HandleWithOverkillRedirect(PlayerChoiceContext choiceContext,
        IReadOnlyList<DamageTargetData> targets, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource,
        CardPlay? cardPlay, ModifyDamageHookType modifyDamageHookType)
    {
        IsHandling.Value = true;
        try
        {
            var owner = targets[0].target;
            if (owner.Player == null || owner.CombatState == null)
                return await CreatureCmd.Damage(choiceContext, targets, amount, props, dealer, cardSource, cardPlay,
                    modifyDamageHookType);

            var guardianOrder = PetOrderSnapshotManager.GetSnapshot(owner.Player, false)
                .Where(p => IsFrontGuardian(p) && p.CombatId.HasValue)
                .Select(p => p.CombatId!.Value)
                .ToList();

            SuppressedOwner.Value = owner;
            List<DamageResult> initialResults;
            try
            {
                // Let base flow run once so owner's block and first guardian redirect still use vanilla math,
                // but suppress fallback HP loss on owner (it will be redistributed below).
                initialResults = (await CreatureCmd.Damage(choiceContext, targets, amount, props, dealer, cardSource,
                        cardPlay, modifyDamageHookType))
                    .ToList();
            }
            finally
            {
                SuppressedOwner.Value = null;
            }

            // Dead guardians can lose powers before results are inspected.
            // Snapshot combat ids let us still recognize the first redirected guardian reliably.
            var firstGuardianResult = initialResults.FirstOrDefault(r =>
                r.Receiver != owner &&
                r.Receiver.PetOwner == owner.Player &&
                (IsFrontGuardian(r.Receiver) ||
                 (r.Receiver.CombatId is uint receiverId && guardianOrder.Contains(receiverId))));

            if (firstGuardianResult is not { OverkillDamage: > 0 } ||
                !firstGuardianResult.Receiver.CombatId.HasValue) return initialResults;

            List<DamageResult> redirectedResults = [];
            decimal remaining = firstGuardianResult.OverkillDamage;
            var firstGuardianId = firstGuardianResult.Receiver.CombatId.Value;
            var directProps = props | ValueProp.Unpowered;
            // Overflow is already calculated. Only retain damage caps requested by the caller;
            // do not reapply additive/multiplicative hooks or re-enable caps for None.
            var overflowModifiers = modifyDamageHookType & ModifyDamageHookType.Cap;

            var firstGuardianIndex = guardianOrder.IndexOf(firstGuardianId);
            if (firstGuardianIndex < 0)
            {
                if (remaining > 0m)
                {
                    var ownerFinalFallback =
                        (await CreatureCmd.Damage(choiceContext, targets, remaining, directProps, dealer, cardSource,
                            cardPlay, overflowModifiers))
                        .FirstOrDefault()
                        ?? new DamageResult(owner, directProps);
                    redirectedResults.Add(ownerFinalFallback);
                }

                initialResults.AddRange(redirectedResults);
                return initialResults;
            }

            foreach (var guardianId in guardianOrder.Skip(firstGuardianIndex + 1))
            {
                if (remaining <= 0m) break;

                var defender = owner.CombatState.GetCreature(guardianId);
                if (defender is not { IsAlive: true } || !IsFrontGuardian(defender)) continue;

                var defenderResult =
                    (await CreatureCmd.Damage(choiceContext, [targets[0] with { target = defender }], remaining,
                        directProps, dealer, cardSource, cardPlay, overflowModifiers))
                    .FirstOrDefault()
                    ?? new DamageResult(defender, directProps);
                redirectedResults.Add(defenderResult);
                remaining = defenderResult.OverkillDamage;
            }

            if (remaining > 0m)
            {
                var ownerFinal =
                    (await CreatureCmd.Damage(choiceContext, targets, remaining, directProps, dealer, cardSource,
                        cardPlay, overflowModifiers))
                    .FirstOrDefault()
                    ?? new DamageResult(owner, directProps);
                redirectedResults.Add(ownerFinal);
            }

            initialResults.AddRange(redirectedResults);
            return initialResults;
        }
        finally
        {
            IsHandling.Value = false;
        }
    }

    private static bool IsFrontGuardian(Creature creature)
    {
        return creature.GetPower<MinionGuardianPower>() != null &&
               (creature.Monster is not MinionModel minion || minion.Position == MinionPosition.Front);
    }
}
