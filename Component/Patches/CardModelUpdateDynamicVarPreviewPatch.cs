using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Patching.Models;

namespace MinionLib.Component.Patches;

public sealed class CardModelUpdateDynamicVarPreviewPatch : IPatchMethod
{
    public static string PatchId => "component_dynamic_var_preview";

    public static string Description => "Update MinionLib component dynamic vars during card preview calculation.";

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(CardModel), nameof(CardModel.UpdateDynamicVarPreview))];
    }

    private static void Postfix(CardModel __instance, object previewMode,
        Creature? target, object dynamicVarSet)
    {
        if (__instance is not IComponentsCardModel componentsCard)
            return;

        componentsCard.EnsureComponentsInitialized();

        var runGlobalHooks = __instance.CombatState != null;

        foreach (var component in componentsCard.Components)
        foreach (var dynVar in component.DynamicVars.Values)
            dynVar.UpdateCardPreview(__instance, (dynamic)previewMode, target, runGlobalHooks);
    }
}
