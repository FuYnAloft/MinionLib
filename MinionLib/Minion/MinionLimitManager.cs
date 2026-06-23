using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace MinionLib.Minion;

/// <summary>
///     管理每个玩家的随从数量上限。提供默认上限、可注册的修饰器（用于遗物/能力动态调整），
///     以及查询当前随从数量的辅助方法。
///     当上限为 <c>-1</c> 时表示不限制随从数量。
/// </summary>
public static class MinionLimitManager
{
    /// <summary>
    ///     表示不限制随从数量的特殊值。
    /// </summary>
    public const int Unlimited = -1;

    private const int DefaultMaxMinionsValue = 5;

    private static readonly List<Func<Player, int>> MaxModifiers = [];

    /// <summary>
    ///     默认每个玩家最多拥有的随从数量。设为 <c>-1</c>（<see cref="Unlimited" />）表示不限制。
    ///     可通过 <see cref="RegisterMaxModifier" /> 进一步动态调整。
    /// </summary>
    public static int DefaultMax { get; set; } = DefaultMaxMinionsValue;

    /// <summary>
    ///     注册一个修饰器，返回值表示对当前玩家最大随从数的调整量（可为负）。
    ///     最终上限 = <see cref="DefaultMax" /> + 所有修饰器返回值之和。
    ///     若最终上限 ≤ <c>-1</c>，则视为不限制（<see cref="Unlimited" />）。
    /// </summary>
    public static void RegisterMaxModifier(Func<Player, int> modifier)
    {
        ArgumentNullException.ThrowIfNull(modifier);
        MaxModifiers.Add(modifier);
    }

    /// <summary>
    ///     获取指定玩家的最大随从数量。返回 <c>-1</c>（<see cref="Unlimited" />）表示不限制。
    /// </summary>
    public static int GetMaxMinions(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        var max = DefaultMax;
        foreach (var modifier in MaxModifiers)
            max += modifier(player);

        // -1 或更小视为不限制
        return max < 0 ? Unlimited : max;
    }

    /// <summary>
    ///     指定玩家是否不限制随从数量（上限为 <c>-1</c>）。
    /// </summary>
    public static bool IsUnlimited(Player player)
    {
        return GetMaxMinions(player) == Unlimited;
    }

    /// <summary>
    ///     指定玩家是否还能召唤更多随从。不限制时始终返回 <c>true</c>。
    /// </summary>
    public static bool CanSummon(Player player)
    {
        if (IsUnlimited(player)) return true;
        return GetCurrentMinionCount(player) < GetMaxMinions(player);
    }

    /// <summary>
    ///     获取指定玩家当前存活的随从数量。
    /// </summary>
    public static int GetCurrentMinionCount(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return player.PlayerCombatState?.Pets
            .Count(p => p.IsAlive && p.Monster is MinionModel) ?? 0;
    }

    /// <summary>
    ///     获取指定玩家当前存活的随从列表（按 <see cref="PlayerCombatState.Pets" /> 中的顺序，即加入顺序）。
    /// </summary>
    public static IReadOnlyList<Creature> GetAliveMinions(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return player.PlayerCombatState?.Pets
            .Where(p => p.IsAlive && p.Monster is MinionModel)
            .ToList() ?? [];
    }
}
