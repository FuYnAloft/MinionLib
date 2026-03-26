global using static MinionLib.DebugLogger;
using System.Diagnostics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MinionLib.Initialization;
namespace MinionLib;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "MinionLib"; //At the moment, this is used only for the Logger and harmony names.

    public static void Initialize()
    {
        Harmony harmony = new(ModId);

        harmony.PatchAll();

        MinionHookInitializer.Initialize();

        Debug("Init", $"{ModId} initialized");
    }
}

internal static class DebugLogger
{
    [Conditional("DEBUG")]
    internal static void Debug(string message)
    {
        Log.Info($"[{MainFile.ModId}] {message}");
    }

    [Conditional("DEBUG")]
    internal static void Debug(string module, string message)
    {
        Log.Info($"[{MainFile.ModId}] [{module}] {message}");
    }
}