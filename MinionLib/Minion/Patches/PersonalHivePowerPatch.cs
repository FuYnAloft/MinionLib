using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MinionLib.Minion.Patches;

/// <summary>
/// 因為PersonalHivePower硬編碼只判斷Osty，需要擴充成所有Pets以防止被Minion攻擊時產生NullReferenceException
/// </summary>
[HarmonyPatch(typeof(PersonalHivePower), nameof(PersonalHivePower.AfterDamageReceived))]
public static class PersonalHivePowerPatch
{
    [HarmonyPrefix]
    public static void Prefix(ref Creature? dealer)
    {
        if (dealer is not null && dealer.Monster is not Osty && dealer.PetOwner?.Creature is not null)
        {
            dealer = dealer.PetOwner.Creature;
        }
    }
}
