using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Component.Interfaces;

namespace MinionLib.Component.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.UpdateDynamicVarPreview))]
public static class CardModelUpdateDynamicVarPreviewPatch
{
    [HarmonyPostfix]
    private static void UpdateDynamicVarPreviewPostfix(CardModel __instance, object previewMode,
        Creature? target, object dynamicVarSet)
    {
        if (__instance is not IComponentsCardModel componentsCard)
            return;

        componentsCard.EnsureComponentsInitialized();

        var runGlobalHooks = __instance.CombatState != null;

        foreach (var component in componentsCard.Components)
        {
            foreach (var dynVar in component.DynamicVars.Values)
            {
                dynVar.UpdateCardPreview(__instance, (dynamic)previewMode, target, runGlobalHooks);
            }
        }
    }
}
