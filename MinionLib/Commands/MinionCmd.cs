using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;

namespace MinionLib.Commands;

public static class MinionCmd
{
    public static async Task<Creature> AddMinion<T>(PlayerChoiceContext choiceContext, Player player,
        MinionSummonOptions options = default)
        where T : MinionModel
    {
        ArgumentNullException.ThrowIfNull(player);

        await EnforceMinionLimit(player, options);

        var pet = await PlayerCmd.AddPet<T>(player);
        if (pet.Monster is MinionModel minionModel) minionModel.Position = options.Position;
        PetOrderSnapshotManager.TakeSnapshot(player);

        if (pet.Monster is MinionModel minion) await minion.OnSummon(choiceContext, player, options);

        _ = MinionAnimCmd.Rearrange();

        return pet;
    }

    /// <summary>
    ///     根据 <see cref="MinionSummonOptions.OverflowBehavior" /> 处理随从上限。
    ///     当上限为 <c>-1</c>（<see cref="MinionLimitManager.Unlimited" />）时不做任何限制。
    /// </summary>
    private static async Task EnforceMinionLimit(Player player, MinionSummonOptions options)
    {
        if (options.OverflowBehavior == MinionSummonOverflowBehavior.Ignore) return;
        if (MinionLimitManager.IsUnlimited(player)) return;

        var max = MinionLimitManager.GetMaxMinions(player);
        var current = MinionLimitManager.GetCurrentMinionCount(player);
        if (current < max) return;

        if (options.OverflowBehavior == MinionSummonOverflowBehavior.Fail)
            throw new MinionLimitExceededException(player, current, max);

        // ReplaceOldest: 击杀最早召唤的随从以腾出位置
        var minions = MinionLimitManager.GetAliveMinions(player);
        if (minions.Count == 0) return;

        await CreatureCmd.Kill(minions[0], true);
    }
}
