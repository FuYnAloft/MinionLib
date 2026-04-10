using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MinionLib.Component.Interfaces;
using DrawingColor = System.Drawing.Color;

namespace MinionLib.Component.Patches;

[HarmonyPatch(typeof(NHandCardHolder))]
public static class CardGlowColorPatch
{
    [HarmonyPatch(nameof(NHandCardHolder.UpdateCard))]
    [HarmonyPostfix]
    private static void UpdateCardPostfix(NHandCardHolder __instance)
    {
        if (!TryGetGlowColor(__instance, out var glowColor))
            return;

        var highlight = __instance.CardNode?.CardHighlight;
        if (highlight == null)
            return;

        ApplyGlowColor(highlight, glowColor);
    }

    [HarmonyPatch(nameof(NHandCardHolder.Flash))]
    [HarmonyPostfix]
    private static void FlashPostfix(NHandCardHolder __instance)
    {
        if (!TryGetGlowColor(__instance, out var glowColor))
            return;

        var flash = __instance.GetNodeOrNull<Control>("Flash");
        if (flash == null)
            return;

        ApplyGlowColor(flash, glowColor);
    }

    private static bool TryGetGlowColor(NHandCardHolder holder, out Godot.Color glowColor)
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

    private static void ApplyGlowColor(CanvasItem canvasItem, Godot.Color glowColor)
    {
        canvasItem.Modulate = glowColor;
    }

}
