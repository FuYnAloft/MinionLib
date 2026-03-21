using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace MinionLib;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "MinionLib"; //At the moment, this is used only for the Logger and harmony names.

    public static Logger Logger { get; } =
        new(ModId, LogType.Generic);

    public static void Initialize()
    {
        Harmony harmony = new(ModId);

        harmony.PatchAll();
        TryLookupScriptsInAssembly();
    }

    private static void TryLookupScriptsInAssembly()
    {
        var bridgeType = Type.GetType("MegaCrit.Sts2.Core.Modding.ScriptManagerBridge, sts2");
        var lookupMethod = bridgeType?.GetMethod("LookupScriptsInAssembly", BindingFlags.Public | BindingFlags.Static);
        lookupMethod?.Invoke(null, [typeof(MainFile).Assembly]);
    }
}