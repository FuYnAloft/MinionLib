using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;

namespace MinionLib.Patches;

[HarmonyPatch(typeof(Creature), nameof(Creature.LoseHpInternal), typeof(decimal), typeof(ValueProp))]
public static class MinionGuardianOwnerDamageSuppressPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Creature __instance, decimal amount, ValueProp props, ref DamageResult __result)
    {
        var suppressedOwner = MinionGuardianOverkillPatch.SuppressedOwner.Value;
        if (suppressedOwner == null || __instance != suppressedOwner || amount <= 0m) return true;

        // Suppress the temporary owner fallback loss in vanilla redirect flow.
        __result = new DamageResult(__instance, props);
        return false;
    }
}