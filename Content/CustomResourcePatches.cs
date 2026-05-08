using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;

namespace MinionLib.Content;

internal static class CustomResourcePatchHelper
{
    public static bool TryGetPowerIconPath(PowerModel power, out string path)
    {
        if (power is ICustomPowerResourceProvider { CustomPackedIconPath: { } customPath } &&
            !string.IsNullOrWhiteSpace(customPath))
        {
            path = customPath;
            return true;
        }

        path = string.Empty;
        return false;
    }

    public static bool TryGetPowerBigIconPath(PowerModel power, out string path)
    {
        if (power is ICustomPowerResourceProvider provider)
        {
            if (!string.IsNullOrWhiteSpace(provider.CustomBigIconPath) &&
                ResourceLoader.Exists(provider.CustomBigIconPath))
            {
                path = provider.CustomBigIconPath;
                return true;
            }

            if (!string.IsNullOrWhiteSpace(provider.CustomBigBetaIconPath) &&
                ResourceLoader.Exists(provider.CustomBigBetaIconPath))
            {
                path = provider.CustomBigBetaIconPath;
                return true;
            }
        }

        path = string.Empty;
        return false;
    }

    public static bool TryGetPotionImagePath(PotionModel potion, out string path)
    {
        if (potion is ICustomPotionResourceProvider { CustomPackedImagePath: { } customPath } &&
            !string.IsNullOrWhiteSpace(customPath))
        {
            path = customPath;
            return true;
        }

        path = string.Empty;
        return false;
    }

    public static bool TryGetPotionOutlinePath(PotionModel potion, out string path)
    {
        if (potion is ICustomPotionResourceProvider { CustomPackedOutlinePath: { } customPath } &&
            !string.IsNullOrWhiteSpace(customPath) &&
            ResourceLoader.Exists(customPath))
        {
            path = customPath;
            return true;
        }

        path = string.Empty;
        return false;
    }
}

[HarmonyPatch(typeof(PowerModel), "get_IconPath")]
internal static class PowerModelIconPathPatch
{
    private static void Postfix(PowerModel __instance, ref string __result)
    {
        if (CustomResourcePatchHelper.TryGetPowerIconPath(__instance, out var path))
            __result = path;
    }
}

[HarmonyPatch(typeof(PowerModel), "get_Icon")]
internal static class PowerModelIconPatch
{
    private static bool Prefix(PowerModel __instance, ref Texture2D __result)
    {
        if (!CustomResourcePatchHelper.TryGetPowerIconPath(__instance, out var path))
            return true;

        __result = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
        return false;
    }
}

[HarmonyPatch(typeof(PowerModel), "get_ResolvedBigIconPath")]
internal static class PowerModelResolvedBigIconPathPatch
{
    private static void Postfix(PowerModel __instance, ref string __result)
    {
        if (CustomResourcePatchHelper.TryGetPowerBigIconPath(__instance, out var path))
            __result = path;
    }
}

[HarmonyPatch(typeof(PowerModel), "get_BigIcon")]
internal static class PowerModelBigIconPatch
{
    private static bool Prefix(PowerModel __instance, ref Texture2D __result)
    {
        if (!CustomResourcePatchHelper.TryGetPowerBigIconPath(__instance, out var path))
            return true;

        __result = PreloadManager.Cache.GetTexture2D(path);
        return false;
    }
}

[HarmonyPatch(typeof(PotionModel), "get_ImagePath")]
internal static class PotionModelImagePathPatch
{
    private static void Postfix(PotionModel __instance, ref string __result)
    {
        if (CustomResourcePatchHelper.TryGetPotionImagePath(__instance, out var path))
            __result = path;
    }
}

[HarmonyPatch(typeof(PotionModel), "get_Image")]
internal static class PotionModelImagePatch
{
    private static bool Prefix(PotionModel __instance, ref Texture2D __result)
    {
        if (!CustomResourcePatchHelper.TryGetPotionImagePath(__instance, out var path))
            return true;

        __result = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
        return false;
    }
}

[HarmonyPatch(typeof(PotionModel), "get_OutlinePath")]
internal static class PotionModelOutlinePathPatch
{
    private static void Postfix(PotionModel __instance, ref string? __result)
    {
        if (CustomResourcePatchHelper.TryGetPotionOutlinePath(__instance, out var path))
            __result = path;
    }
}

[HarmonyPatch(typeof(PotionModel), "get_Outline")]
internal static class PotionModelOutlinePatch
{
    private static bool Prefix(PotionModel __instance, ref Texture2D? __result)
    {
        if (!CustomResourcePatchHelper.TryGetPotionOutlinePath(__instance, out var path))
            return true;

        __result = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
        return false;
    }
}
