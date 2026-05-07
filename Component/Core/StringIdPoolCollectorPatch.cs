using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace MinionLib.Component.Core;

public sealed class StringIdPoolCollectorPatch : IPatchMethod
{
    public static string PatchId => "string_id_pool_collector";

    public static string Description => "Collect model id strings for component localization argument lookup.";

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(AbstractModel), nameof(AbstractModel.InitId))];
    }

    public static void Postfix(AbstractModel __instance)
    {
        var id = __instance.Id;
        StringIdPool.Register(id.Category);
        StringIdPool.Register(id.Entry);
        StringIdPool.Register(__instance.GetType().FullName ?? "");
    }
}
