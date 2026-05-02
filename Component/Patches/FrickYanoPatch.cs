using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MinionLib.Component.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.FromSerializable))]
public static class FrickYanoPatch
{
    private const string BlobPropertyName = nameof(ComponentsCardModel.MinionLibComponentStateBlob);

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void RestoreSavedComponentState(SerializableCard save, CardModel __result)
    {
        if (__result is not ComponentsCardModel componentsCard) return;
        var savedBlob = save.Props?.intArrays
            ?.Where(prop => prop.name == BlobPropertyName)
            .Select(prop => prop.value)
            .FirstOrDefault();
        if (savedBlob == null) return;
        componentsCard.MinionLibComponentStateBlob = savedBlob.ToArray();
        componentsCard.EnsureComponentsInitialized();
    }
}
