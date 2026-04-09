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

    private static bool TryGetGlowColor(NHandCardHolder holder, out DrawingColor glowColor)
    {
        glowColor = default;

        if (holder.CardNode?.Model is not IComponentsCardModel componentsCard)
            return false;

        var customGlow = componentsCard.GlowColor;
        if (!customGlow.HasValue || customGlow.Value.IsEmpty)
            return false;

        glowColor = customGlow.Value;
        return true;
    }

    private static void ApplyGlowColor(CanvasItem canvasItem, DrawingColor glowColor)
    {
        canvasItem.Modulate = ToGodotColor(glowColor);
    }

    private static Color ToGodotColor(DrawingColor color)
    {
        return new Color(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
    }
}
