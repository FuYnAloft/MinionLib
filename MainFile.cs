global using static MinionLib.DebugLogger;
using System.Diagnostics;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Modding;
using MinionLib.Initialization;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Patching.Core;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace MinionLib;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "MinionLib";

    public static Logger Logger { get; private set; } = RitsuLibFramework.CreateLogger(ModId);

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();
        Logger = RitsuLibFramework.CreateLogger(ModId);

        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        var patcher = RitsuLibFramework.CreatePatcher(ModId, "core-patches");
        patcher.RegisterPatches<MinionLibPatches>();
        if (!patcher.PatchAll() || !MinionLibDynamicPatches.ApplyTo(patcher))
            throw new InvalidOperationException($"{ModId} critical patches failed; aborting initialization.");

        MinionHookInitializer.Initialize();

        Debug("Init", $"{ModId} initialized");
    }
}

internal static class DebugLogger
{
    [Conditional("DEBUG")]
    internal static void Debug(string message)
    {
        MainFile.Logger.Info($"[{MainFile.ModId}] {message}");
    }

    [Conditional("DEBUG")]
    internal static void Debug(string module, string message)
    {
        MainFile.Logger.Info($"[{MainFile.ModId}] [{module}] {message}");
    }
}
