using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Patching.Models;
using DrawingColor = System.Drawing.Color;

namespace MinionLib.Component.Patches;

public static class CardGlowColorPatch
{
    private static bool TryGetGlowColor(NHandCardHolder holder, out Color glowColor)
    {
        glowColor = default;

        if (holder.CardNode?.Model is not IComponentsCardModel componentsCard)
            return false;

        var customGlow = componentsCard.GlowColor;
        if (!customGlow.HasValue)
            return false;

        glowColor = customGlow.Value;
        return true;
    }

    private static void ApplyGlowColor(CanvasItem canvasItem, Color glowColor)
    {
        canvasItem.Modulate = glowColor;
    }

    public sealed class UpdateCard : IPatchMethod
    {
        public static string PatchId => "card_glow_color_update";

        public static bool IsCritical => false;

        public static string Description => "Apply component card glow color when card visuals update.";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NHandCardHolder), nameof(NHandCardHolder.UpdateCard))];
        }

        private static void Postfix(NHandCardHolder __instance)
        {
            if (!TryGetGlowColor(__instance, out var glowColor))
                return;

            var highlight = __instance.CardNode?.CardHighlight;
            if (highlight == null)
                return;

            ApplyGlowColor(highlight, glowColor);
        }
    }

    public sealed class Flash : IPatchMethod
    {
        public static string PatchId => "card_glow_color_flash";

        public static bool IsCritical => false;

        public static string Description => "Apply component card glow color to flash visuals.";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NHandCardHolder), nameof(NHandCardHolder.Flash))];
        }

        private static void Postfix(NHandCardHolder __instance)
        {
            if (!TryGetGlowColor(__instance, out var glowColor))
                return;

            var flash = __instance.GetNodeOrNull<Control>("Flash");
            if (flash == null)
                return;

            ApplyGlowColor(flash, glowColor);
        }
    }
}
