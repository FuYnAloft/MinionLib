using MinionLib.Action;
using MinionLib.Commands;

namespace MinionLib.Initialization;

/// <summary>
///     在玩家回合开始和结束时自动重排随从位置。
/// </summary>
public static class MinionHookInitializer
{
    public static void Initialize()
    {
        MinionRuntime.UseDefaultLifecycleSource();
    }

    public static void Deinitialize()
    {
        MinionRuntime.Deinitialize();
    }

    internal static void OnPlayerTurnStarted()
    {
        _ = MinionAnimCmd.Rearrange();
    }

    internal static void OnPlayerTurnEnded()
    {
        CreatureActionQueueThreshold.Clear();
        _ = MinionAnimCmd.Rearrange();
    }

    internal static void OnCombatStarted()
    {
        CreatureActionQueueThreshold.Clear();
        PetOrderSnapshotManager.ClearAllSnapshots();
    }

    internal static void OnCombatEnded()
    {
        CreatureActionQueueThreshold.Clear();
        PetOrderSnapshotManager.ClearAllSnapshots();
    }
}
