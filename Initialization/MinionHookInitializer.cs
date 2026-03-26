using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Rooms;
using MinionLib.Action;
using MinionLib.Commands;

namespace MinionLib.Initialization;

/// <summary>
/// 在玩家回合开始和结束时自动重排随从位置。
/// 通过订阅 CombatManager 的全局事件实现。
/// </summary>
public static class MinionHookInitializer
{
	public static void Initialize()
	{
		CombatManager.Instance.TurnStarted += OnTurnStarted;
		CombatManager.Instance.TurnEnded += OnTurnEnded;
		CombatManager.Instance.CombatSetUp += OnCombatSetUp;
		CombatManager.Instance.CombatEnded += OnCombatEnded;
	}

	public static void Deinitialize()
	{
		CombatManager.Instance.TurnStarted -= OnTurnStarted;
		CombatManager.Instance.TurnEnded -= OnTurnEnded;
		CombatManager.Instance.CombatSetUp -= OnCombatSetUp;
		CombatManager.Instance.CombatEnded -= OnCombatEnded;
	}

	private static void OnTurnStarted(CombatState combatState)
	{
		// 玩家回合开始时重排
		if (combatState.CurrentSide == CombatSide.Player)
		{
			_ = MinionAnimCmd.Rearrange();
		}
	}

	private static void OnTurnEnded(CombatState combatState)
	{
		CreatureActionQueueThreshold.Clear();

		// 玩家回合结束时重排
		// TurnEnded 触发时 CurrentSide 已经切换，所以检查是否为 Enemy 来判断刚结束的是玩家回合
		if (combatState.CurrentSide == CombatSide.Enemy)
		{
			_ = MinionAnimCmd.Rearrange();
		}
	}
	
	private static void OnCombatSetUp(CombatState combatState)
	{
		CreatureActionQueueThreshold.Clear();

		// 清理宠物顺序快照，预防内存泄露
		PetOrderSnapshotManager.ClearAllSnapshots();
	}

	private static void OnCombatEnded(CombatRoom combatRoom)
	{
		CreatureActionQueueThreshold.Clear();

		// 清理宠物顺序快照，预防内存泄露
		PetOrderSnapshotManager.ClearAllSnapshots();
	}
}

