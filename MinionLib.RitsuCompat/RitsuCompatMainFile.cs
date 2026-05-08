using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MinionLib.Initialization;

namespace MinionLib.RitsuCompat;

[ModInitializer(nameof(Initialize))]
public static class RitsuCompatMainFile
{
    public const string ModId = "MinionLib.RitsuCompat";

    public static void Initialize()
    {
        MinionRuntime.UseLifecycleSource(new RitsuMinionLifecycleSource());
        Log.Info($"[{ModId}] initialized");
    }
}
