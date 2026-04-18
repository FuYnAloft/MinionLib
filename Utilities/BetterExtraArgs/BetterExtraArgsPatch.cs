using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MinionLib.Utilities.BetterExtraArgs;

[HarmonyPatch]
public static class BetterExtraArgsPatch
{
    static MethodBase TargetMethod()
    {
        var previewEnumType = AccessTools.Inner(typeof(CardModel), "DescriptionPreviewType");

        return AccessTools.Method(typeof(CardModel), "GetDescriptionForPile", [
            typeof(PileType),
            previewEnumType,
            typeof(Creature)
        ]);
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var targetMethod = AccessTools.Method(typeof(CardModel), "AddExtraArgsToDescription");
        var helperMethod = AccessTools.Method(typeof(BetterExtraArgsPatch), nameof(TryBetterAddExtraArgs));

        var found = false;

        for (var i = 0; i < codes.Count; i++)
        {
            yield return codes[i];

            if (found || !codes[i].Calls(targetMethod)) continue;

            found = true;
            var loadDescriptionInst = codes[i - 1].Clone();
            loadDescriptionInst.labels.Clear();
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return loadDescriptionInst;
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Ldarg_2);
            yield return new CodeInstruction(OpCodes.Ldarg_3);
            yield return new CodeInstruction(OpCodes.Call, helperMethod);
        }
    }

    static void TryBetterAddExtraArgs(
        CardModel thisCard,
        LocString description,
        PileType pileType,
        int previewType,
        Creature? target = null)
    {
        if (thisCard is IBetterAddExtraArgsCard card)
            card.BetterAddExtraArgsToDescription(description, pileType, (DescriptionPreviewType)previewType, target);
    }
}