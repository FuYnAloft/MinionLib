using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using MinionLib.Commands;

namespace MinionLib.Patches;

[HarmonyPatch(typeof(CombatRoom), nameof(CombatRoom.Exit))]
public static class CombatSnapshotCleanupPatch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        PetOrderSnapshotManager.ClearAllSnapshots();
    }
}