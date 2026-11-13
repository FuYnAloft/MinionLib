using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MinionLib.Commands;
using MinionLib.Layout;

namespace MinionLib.Minion.Patches;

[HarmonyPatch(typeof(NCreature), "SetIsInteractableInternal")]
public static class MinionInteractablePatch2
{
    [HarmonyPrefix]
    private static void Prefix(NCreature __instance, ref bool on, bool ____interactionAllowedBecauseAlive)
    {
        // Keep living local-owner minions clickable without overriding the death animation lock.
        if (____interactionAllowedBecauseAlive && __instance.Entity.IsAlive
            && __instance.Entity.Monster is MinionModel && LocalContext.IsMe(__instance.Entity.PetOwner))
            on = true;
    }
}

[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.AddCreature))]
public static class MinionInteractablePatch
{
    [HarmonyPrefix]
    private static bool Prefix(NCombatRoom __instance, out IReadOnlyList<MinionNodePosition> __state)
    {
        __state = MinionLayoutManager.GetCurrentMinionPositions(__instance);
        return true;
    }

    [HarmonyPostfix]
    private static void Postfix(NCombatRoom __instance, Creature creature, IReadOnlyList<MinionNodePosition> __state)
    {
        MinionAnimCmd.InstantMove(__state);

        if (creature.PetOwner == null || creature.Monster is not MinionModel) return;

        __instance.GetCreatureNode(creature)!.Position =
            __instance.GetCreatureNode(creature.PetOwner.Creature)!.Position;
    }
}
