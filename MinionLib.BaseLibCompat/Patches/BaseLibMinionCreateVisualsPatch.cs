using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace MinionLib.BaseLibCompat.Patches;

[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.CreateVisuals))]
internal static class BaseLibMinionCreateVisualsPatch
{
    [HarmonyPrefix]
    private static bool Prefix(MonsterModel __instance, ref NCreatureVisuals? __result)
    {
        if (__instance is not BaseLibMinionModel minion)
            return true;

        __result = minion.CreateCustomVisuals();
        return __result == null;
    }
}
