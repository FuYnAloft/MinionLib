using MinionLib.Action.Patches;
using MinionLib.Component.Core;
using MinionLib.Component.Patches;
using MinionLib.Minion.Patches;
using MinionLib.Powers.Patches;
using MinionLib.RightClick.Patches;
using MinionLib.Targeting.Patches;
using MinionLib.Utilities.BetterExtraArgs;
using STS2RitsuLib.Patching.Builders;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Patching.Models;

namespace MinionLib.Initialization;

public sealed class MinionLibPatches : IModPatches
{
    public static void AddTo(ModPatcher patcher)
    {
        patcher.RegisterPatch<ActionClickPatch>();
        patcher.RegisterPatch<ActionPowerIconClickPatch>();

        patcher.RegisterPatch<MinionGuardianOverkillPatch>();
        patcher.RegisterPatch<MinionGuardianOwnerDamageSuppressPatch>();
        patcher.RegisterPatch<MinionGuardianBlockToHpPatch>();

        patcher.RegisterPatch<MinionInteractablePatch2>();
        patcher.RegisterPatch<MinionInteractablePatch>();

        patcher.RegisterPatch<CustomTargetTypeCardPatch.IsSingleTarget>();
        patcher.RegisterPatch<CustomTargetTypeCardPatch.AllowedToTargetCreature>();
        patcher.RegisterPatch<CustomTargetTypeCardPatch.CardModelIsValidTarget>();
        patcher.RegisterPatch<CustomTargetTypeCardPatch.TryPlayCard>();
        patcher.RegisterPatch<CustomTargetTypeCardPatch.ShowMultiCreatureTargetingVisuals>();
        patcher.RegisterPatch<CustomTargetTypeCardPatch.MouseMultiCreatureTargeting>();
        patcher.RegisterPatch<CustomTargetTypeCardPatch.ControllerMultiCreatureTargeting>();
        patcher.RegisterPatch<CustomTargetTypeCardPatch.ControllerSingleCreatureTargetingPatch>();

        patcher.RegisterPatch<CustomTargetTypePotionPatch.UsePotion>();
        patcher.RegisterPatch<CustomTargetTypePotionPatch.TargetNode>();
        patcher.RegisterPatch<CustomTargetTypePotionPatch.PopupReady>();

        patcher.RegisterPatch<ComponentDescriptionRawCachePatch.DescriptionGetter>();
        patcher.RegisterPatch<ComponentDescriptionRawCachePatch.LocStringGetRawText>();
        patcher.RegisterPatch<ComponentDescriptionRawCachePatch.LocManagerSetLanguage>();
        patcher.RegisterPatch<CardModelUpdateDynamicVarPreviewPatch>();
        patcher.RegisterPatch<FrickYanoPatch>();
        patcher.RegisterPatch<StringIdPoolCollectorPatch>();
        patcher.RegisterPatch<CardGlowColorPatch.UpdateCard>();
        patcher.RegisterPatch<CardGlowColorPatch.Flash>();
        patcher.RegisterPatch<NetFullCombatStateComponentsLogPatch>();

        patcher.RegisterPatch<CardRightClickPatch>();
    }
}

public static class MinionLibDynamicPatches
{
    public static bool ApplyTo(ModPatcher patcher)
    {
        var builder = new DynamicPatchBuilder("minionlib_dynamic")
            .Add(
                BetterExtraArgsPatch.TargetMethod(),
                transpiler: DynamicPatchBuilder.FromMethod(
                    typeof(BetterExtraArgsPatch),
                    nameof(BetterExtraArgsPatch.Transpiler)),
                isCritical: true,
                description: "Improve component description extra-argument injection.",
                patchId: "better_extra_args")
            .Add(
                NoDescriptionMarkerCleanPatch.TargetMethod(),
                postfix: DynamicPatchBuilder.FromMethod(
                    typeof(NoDescriptionMarkerCleanPatch),
                    nameof(NoDescriptionMarkerCleanPatch.Postfix)),
                isCritical: false,
                description: "Clean no-description marker from pile card descriptions.",
                patchId: "no_description_marker_clean");

        return patcher.ApplyDynamic(builder, rollbackOnCriticalFailure: true);
    }
}
