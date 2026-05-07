using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Patching.Models;

namespace MinionLib.Powers.Patches;

public sealed class MinionGuardianBlockToHpPatch : IPatchMethod
{
    public static string PatchId => "minion_guardian_block_to_hp";

    public static string Description => "Convert guardian minion block gain into max HP and healing.";

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(typeof(CreatureCmd), nameof(CreatureCmd.GainBlock),
            [
                typeof(Creature),
                typeof(decimal),
                typeof(ValueProp),
                typeof(CardPlay),
                typeof(bool)
            ])
        ];
    }

    private static bool Prefix(Creature creature, decimal amount, ValueProp props, CardPlay? cardPlay, bool fast,
        ref Task<decimal> __result)
    {
        if (amount <= 0m || creature.GetPower<MinionGuardianPower>() == null || creature.IsDead) return true;

        __result = GainBlock(creature, amount, props, cardPlay, fast);
        return false;
    }

    public static async Task<decimal> GainBlock(
        Creature creature,
        decimal amount,
        ValueProp props,
        CardPlay? cardPlay,
        bool fast = false)
    {
        if (CombatManager.Instance.IsOverOrEnding)
            return 0m;
        var combatState = creature.CombatState!;
        await Hook.BeforeBlockGained(combatState, creature, amount, props, cardPlay?.Card);
        var modifiedAmount = amount;
        modifiedAmount = Hook.ModifyBlock(combatState, creature, modifiedAmount, props, cardPlay?.Card, cardPlay,
            out var modifiers);
        modifiedAmount = Math.Max(modifiedAmount, 0m);
        await Hook.AfterModifyingBlockAmount(combatState, modifiedAmount, cardPlay?.Card, cardPlay, modifiers);
        if (modifiedAmount > 0m)
        {
            SfxCmd.Play("event:/sfx/block_gain");
            VfxCmd.PlayOnCreatureCenter(creature, "vfx/vfx_block");
            await CreatureCmd.SetMaxHp(creature, creature.MaxHp + modifiedAmount);
            await CreatureCmd.Heal(creature, modifiedAmount, false);
            CombatManager.Instance.History.BlockGained(combatState, creature, (int)modifiedAmount, props, cardPlay);
            if (fast)
                await Cmd.CustomScaledWait(0.0f, 0.03f);
            else
                await Cmd.CustomScaledWait(0.1f, 0.25f);
        }

        await Hook.AfterBlockGained(combatState, creature, modifiedAmount, props, cardPlay?.Card);
        return 0m;
    }
}
