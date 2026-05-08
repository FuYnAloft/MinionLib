using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace MinionLib.BaseLibCompat;

[ModInitializer(nameof(Initialize))]
public static class BaseLibCompatMainFile
{
    public const string ModId = "MinionLib.BaseLibCompat";

    public static void Initialize()
    {
        new Harmony(ModId).PatchAll(typeof(BaseLibCompatMainFile).Assembly);
        Log.Info($"[{ModId}] initialized");
    }
}
