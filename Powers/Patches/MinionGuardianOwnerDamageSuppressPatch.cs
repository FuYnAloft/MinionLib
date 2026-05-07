using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Patching.Models;

namespace MinionLib.Powers.Patches;

public sealed class MinionGuardianOwnerDamageSuppressPatch : IPatchMethod
{
    public static string PatchId => "minion_guardian_owner_damage_suppress";

    public static string Description => "Suppress temporary owner HP loss while guardian overkill damage is redistributed.";

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(Creature), nameof(Creature.LoseHpInternal), [typeof(decimal), typeof(ValueProp)])];
    }

    private static bool Prefix(Creature __instance, decimal amount, ValueProp props, ref DamageResult __result)
    {
        var suppressedOwner = MinionGuardianOverkillPatch.SuppressedOwner.Value;
        if (suppressedOwner == null || __instance != suppressedOwner || amount <= 0m) return true;

        // Suppress the temporary owner fallback loss in vanilla redirect flow.
        __result = new DamageResult(__instance, props);
        return false;
    }
}
