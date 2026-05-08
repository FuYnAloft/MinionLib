using System.Diagnostics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MinionLib.Content;

namespace MinionLib.Example;

[ModInitializer(nameof(Initialize))]
public static class ExampleMainFile
{
    private const string ModId = "MinionLib.Example";

    public static void Initialize()
    {
        CustomContentRegistry.RegisterAssembly(typeof(ExampleMainFile).Assembly);
        new Harmony(ModId).PatchAll(typeof(ExampleMainFile).Assembly);

        Debug("Init", $"{ModId} initialized");
    }
}

internal static class ExampleDebugLogger
{
    [Conditional("DEBUG")]
    internal static void Debug(string message)
    {
        Log.Info($"[MinionLib.Example] {message}");
    }

    [Conditional("DEBUG")]
    internal static void Debug(string module, string message)
    {
        Log.Info($"[MinionLib.Example] [{module}] {message}");
    }
}
