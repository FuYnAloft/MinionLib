global using static MinionLib.Example.DebugLogger;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MinionLib.Initialization;
using STS2RitsuLib;
using STS2RitsuLib.Interop;

namespace MinionLib.Example;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "MinionLib.Example";

    public static Logger Logger { get; } = RitsuLibFramework.CreateLogger(ModId);

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        using var registration = RitsuLibFramework.BeginModDataRegistration(ModId);

        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
        MinionHookInitializer.Initialize();
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);

        Harmony harmony = new(ModId);
        harmony.PatchAll(assembly);

        Debug("Init", $"{ModId} initialized");
    }
}

internal static class DebugLogger
{
    [Conditional("DEBUG")]
    internal static void Debug(string message)
    {
        MainFile.Logger.Info(message);
    }

    [Conditional("DEBUG")]
    internal static void Debug(string module, string message)
    {
        MainFile.Logger.Info($"[{module}] {message}");
    }
}
