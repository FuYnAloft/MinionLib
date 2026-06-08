using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;

namespace MinionLib.Minion.Patches;

/// <summary>
/// 0.107.0 版本，官方在 CreatureCmd.KillWithoutCheckingWinCondition() 中移除死亡單位時，
/// 僅針對 Enemy 進行了 CombatManager 與 CombatState 的移除判定，導致友方 Minions 死後
/// 依然殘留在 CombatManager 與 CombatState 中。
/// 本 Patch 在該死亡 Task 結束後，補上友方隨從的清理邏輯。
/// </summary>
[HarmonyPatch(typeof(CreatureCmd), "KillWithoutCheckingWinCondition")]
public static class MinionKillPatch
{
    [HarmonyPostfix]
    private static void Postfix(ref Task __result, Creature creature)
    {
        __result = AwaitAndCleanupAsync(__result, creature);
    }

    private static async Task AwaitAndCleanupAsync(Task originalTask, Creature creature)
    {
        await originalTask;

        if (creature != null && creature.Side == CombatSide.Player && creature.IsPet &&
            creature.Monster is MinionModel && creature.CombatState != null)
        {
            ICombatState combatState = creature.CombatState;
            bool shouldRemoveFromCombat = combatState != null && Hook.ShouldCreatureBeRemovedFromCombatAfterDeath(combatState, creature);

            if (shouldRemoveFromCombat)
            {
                CombatManager.Instance?.RemoveCreature(creature);
                combatState?.RemoveCreature(creature);

                Debug($"[MinionKillPatch] Detected {creature.Name} Died, Removed it from CombatManager and CombatState.");
            }
        }
    }
}
