using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Commands;
using MinionLib.Models;
using MinionLib.Powers;

namespace MinionLib.Patches;

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Damage), typeof(PlayerChoiceContext),
    typeof(IEnumerable<Creature>), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel))]
public static class MinionGuardianOverkillPatch
{
    private static readonly AsyncLocal<bool> IsHandling = new();
    public static readonly AsyncLocal<Creature?> SuppressedOwner = new();

    [HarmonyPrefix]
    private static bool Prefix(PlayerChoiceContext choiceContext, IEnumerable<Creature> targets, decimal amount,
        ValueProp props,
        Creature? dealer, CardModel? cardSource, ref Task<IEnumerable<DamageResult>> __result)
    {
        if (IsHandling.Value) return true;

        var targetList = targets.ToList();
        if (targetList.Count != 1) return true;

        var target = targetList[0];

        if (!ShouldHandle(target, props)) return true;

        __result = HandleWithOverkillRedirect(choiceContext, targetList, amount, props, dealer, cardSource);
        return false;
    }

    private static bool ShouldHandle(Creature target, ValueProp props)
    {
        if (!target.IsPlayer || target.Player == null || target.IsDead || target.CombatState == null) return false;

        if (!props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered)) return false;

        return target.Pets.Any(p => p.IsAlive && p.GetPower<MinionGuardianPower>() != null);
    }

    private static async Task<IEnumerable<DamageResult>> HandleWithOverkillRedirect(PlayerChoiceContext choiceContext,
        IReadOnlyList<Creature> targets, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        IsHandling.Value = true;
        try
        {
            var owner = targets[0];
            if (owner.Player == null || owner.CombatState == null)
                return await CreatureCmd.Damage(choiceContext, targets, amount, props, dealer, cardSource);

            var guardianOrder = PetOrderSnapshotManager.GetSnapshot(owner.Player, false)
                .Where(p => p.GetPower<MinionGuardianPower>() != null && p.CombatId.HasValue)
                .Where(p => !(p.Monster is MinionModel minion && minion.Position != MinionPosition.Front))
                .Select(p => p.CombatId!.Value)
                .ToList();

            SuppressedOwner.Value = owner;
            List<DamageResult> initialResults;
            try
            {
                // Let base flow run once so owner's block and first guardian redirect still use vanilla math,
                // but suppress fallback HP loss on owner (it will be redistributed below).
                initialResults = (await CreatureCmd.Damage(choiceContext, targets, amount, props, dealer, cardSource))
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
                (r.Receiver.GetPower<MinionGuardianPower>() != null ||
                 (r.Receiver.CombatId is uint receiverId && guardianOrder.Contains(receiverId))));

            if (firstGuardianResult == null || firstGuardianResult.OverkillDamage <= 0 ||
                !firstGuardianResult.Receiver.CombatId.HasValue) return initialResults;

            List<DamageResult> redirectedResults = [];
            decimal remaining = firstGuardianResult.OverkillDamage;
            var firstGuardianId = firstGuardianResult.Receiver.CombatId.Value;
            var directProps = props | ValueProp.Unpowered;

            var firstGuardianIndex = guardianOrder.IndexOf(firstGuardianId);
            if (firstGuardianIndex < 0)
            {
                if (remaining > 0m)
                {
                    var ownerFinalFallback =
                        (await CreatureCmd.Damage(choiceContext, [owner], remaining, directProps, dealer, cardSource))
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
                if (defender == null || !defender.IsAlive || defender.GetPower<MinionGuardianPower>() == null) continue;

                var defenderResult =
                    (await CreatureCmd.Damage(choiceContext, [defender], remaining, directProps, dealer, cardSource))
                    .FirstOrDefault()
                    ?? new DamageResult(defender, directProps);
                redirectedResults.Add(defenderResult);
                remaining = defenderResult.OverkillDamage;
            }

            if (remaining > 0m)
            {
                var ownerFinal =
                    (await CreatureCmd.Damage(choiceContext, [owner], remaining, directProps, dealer, cardSource))
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
}