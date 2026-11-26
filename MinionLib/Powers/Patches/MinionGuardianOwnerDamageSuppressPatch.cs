using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace MinionLib.Powers.Patches;

[HarmonyPatch(typeof(CreatureCmd), "GetDamageResults")]
public static class MinionGuardianOwnerDamageSuppressPatch
{
    [HarmonyPostfix]
    private static void Postfix(Creature target, ref DamageResults __result)
    {
        if (!MinionGuardianOverkillPatch.TryDeferOwnerOverflow(target, __result.unblockedRedirect)) return;

        // Keep the owner's block result. If overflow ultimately reaches the owner, its HP loss and
        // modifiers are recalculated by the follow-up damage command.
        var original = __result.original;
        __result.original = new DamageResult(original.Receiver, original.Props)
        {
            OriginalTarget = original.OriginalTarget,
            BlockedDamage = original.BlockedDamage,
            WasBlockBroken = original.WasBlockBroken,
            WasFullyBlocked = original.WasFullyBlocked
        };
        __result.afterOstyHpLossModifiers2.Clear();
    }
}
