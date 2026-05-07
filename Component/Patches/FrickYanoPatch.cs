using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Patching.Models;

namespace MinionLib.Component.Patches;

public sealed class FrickYanoPatch : IPatchMethod
{
    private const string BlobPropertyName = nameof(ComponentsCardModel.MinionLibComponentStateBlob);

    public static string PatchId => "component_restore_saved_state";

    public static string Description => "Restore MinionLib component state after CardModel deserialization.";

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(CardModel), nameof(CardModel.FromSerializable))];
    }

    [HarmonyPriority(Priority.Last)]
    public static void Postfix(SerializableCard save, CardModel __result)
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
