using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MinionLib.Commands;
using MinionLib.Layout;
using STS2RitsuLib.Patching.Models;

namespace MinionLib.Minion.Patches;

public sealed class MinionInteractablePatch2 : IPatchMethod
{
    public static string PatchId => "minion_force_interactable";

    public static string Description => "Keep local-owner minions interactable for action selection.";

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NCreature), nameof(NCreature.ToggleIsInteractable))];
    }

    private static void Prefix(NCreature __instance, ref bool on)
    {
        // Force local-owner companions/minions to stay clickable.
        if (__instance.Entity.Monster is MinionModel && LocalContext.IsMe(__instance.Entity.PetOwner))
            on = true;
    }
}

public sealed class MinionInteractablePatch : IPatchMethod
{
    public static string PatchId => "minion_add_creature_layout";

    public static string Description => "Preserve minion layout when combat creature nodes are added.";

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NCombatRoom), nameof(NCombatRoom.AddCreature))];
    }

    private static bool Prefix(NCombatRoom __instance, out IReadOnlyList<MinionNodePosition> __state)
    {
        __state = MinionLayoutManager.GetCurrentMinionPositions(__instance);
        return true;
    }

    private static void Postfix(NCombatRoom __instance, Creature creature, IReadOnlyList<MinionNodePosition> __state)
    {
        MinionAnimCmd.InstantMove(__state);

        if (creature.PetOwner == null || creature.Monster is not MinionModel) return;

        __instance.GetCreatureNode(creature)!.Position =
            __instance.GetCreatureNode(creature.PetOwner.Creature)!.Position;
    }
}
