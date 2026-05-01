using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace MinionLib.Component.Core;

public static class StringIdPoolCollectorPatch
{
    [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.InitId))]
    [HarmonyPostfix]
    public static void InitIdPostfix(AbstractModel __instance)
    {
        var id = __instance.Id;
        StringIdPool.Register(id.Category);
        StringIdPool.Register(id.Entry);
        StringIdPool.Register(__instance.GetType().FullName ?? "");
    }
}
