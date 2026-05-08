using System.Diagnostics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MinionLib.Example.Cards;
using MinionLib.Example.Potions;

namespace MinionLib.Example;

[ModInitializer(nameof(Initialize))]
public static class ExampleMainFile
{
    private const string ModId = "MinionLib.Example";
    private static bool _contentPoolsRegistered;

    public static void Initialize()
    {
        RegisterContentPools();
        new Harmony(ModId).PatchAll(typeof(ExampleMainFile).Assembly);

        Debug("Init", $"{ModId} initialized");
    }

    private static void RegisterContentPools()
    {
        if (_contentPoolsRegistered)
            return;

        ModHelper.AddModelToPool<TokenCardPool, AwaitCard>();
        ModHelper.AddModelToPool<TokenCardPool, Blank>();
        ModHelper.AddModelToPool<TokenCardPool, GrantDeckDamageBlockComponentCard>();
        ModHelper.AddModelToPool<TokenCardPool, GrantHealComponentCard>();
        ModHelper.AddModelToPool<TokenCardPool, HealSelfComponentCard>();
        ModHelper.AddModelToPool<TokenCardPool, MinionAdvanceCard>();
        ModHelper.AddModelToPool<TokenCardPool, PetEmpowerCard>();
        ModHelper.AddModelToPool<TokenCardPool, SummonAttackakaCard>();
        ModHelper.AddModelToPool<TokenCardPool, SummonDefenseakaCard>();
        ModHelper.AddModelToPool<SharedPotionPool, MinionStrengthPotion>();

        _contentPoolsRegistered = true;
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
