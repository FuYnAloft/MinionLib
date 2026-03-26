using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace MinionLib.Action.GameActions;

public sealed class ExecuteCreatureActionGameAction : GameAction
{
    public override ulong OwnerId => Owner.NetId;

    public override GameActionType ActionType => GameActionType.CombatPlayPhaseOnly;

    private Player Owner { get; }

    private uint ActorCombatId { get; }

    private uint? TargetCombatId { get; }

    private ModelId ActionModelId { get; }

    public ExecuteCreatureActionGameAction(Player owner, Creature actor, CustomActionModel action, Creature? target)
    {
        if (actor.CombatId == null)
            throw new InvalidOperationException("Cannot enqueue creature action without actor combat id.");

        if (target != null && target.CombatId == null)
            throw new InvalidOperationException("Cannot enqueue creature action with target that has no combat id.");

        Owner = owner;
        ActorCombatId = actor.CombatId.Value;
        TargetCombatId = target?.CombatId;
        ActionModelId = action.Id;
    }

    public ExecuteCreatureActionGameAction(Player owner, uint actorCombatId, ModelId actionModelId, uint? targetCombatId)
    {
        Owner = owner;
        ActorCombatId = actorCombatId;
        ActionModelId = actionModelId;
        TargetCombatId = targetCombatId;
    }

    protected override async Task ExecuteAction()
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState == null)
        {
            Cancel();
            return;
        }

        var actor = combatState.GetCreature(ActorCombatId);
        if (actor is not { IsAlive: true })
        {
            Log.Warn($"[MinionLib][MinionAction] Cancel queued action {ActionModelId.Entry} because actor no longer valid");
            Cancel();
            return;
        }

        var action = actor.Powers.OfType<CustomActionModel>().FirstOrDefault(power => power.Id == ActionModelId);
        if (action == null || action.Owner != actor)
        {
            Log.Warn($"[MinionLib][MinionAction] Cancel queued action {ActionModelId.Entry} because action power no longer exists");
            Cancel();
            return;
        }

        if (!action.CanAct(actor, combatState))
        {
            Log.Warn($"[MinionLib][MinionAction] Cancel queued action {ActionModelId.Entry} because CanAct failed");
            Cancel();
            return;
        }

        Creature? target = null;
        if (TargetCombatId.HasValue)
            target = await combatState.GetCreatureAsync(TargetCombatId, 10.0);

        if (action.TargetType.IsSingleTarget())
        {
            if (action.TargetType == TargetType.Self && target == null)
                target = actor;

            if (!action.IsValidTarget(combatState, actor, target))
            {
                Log.Warn($"[MinionLib][MinionAction] Cancel queued action {ActionModelId.Entry} because target is no longer valid");
                Cancel();
                return;
            }
        }
        else if (action.TargetType != TargetType.None && action.GetValidTargets(actor, combatState).Count == 0)
        {
            Log.Warn($"[MinionLib][MinionAction] Cancel queued action {ActionModelId.Entry} because no valid targets remain");
            Cancel();
            return;
        }

        var didAct = await action.TryAct(new GameActionPlayerChoiceContext(this), actor, target);
        if (!didAct)
        {
            Log.Warn($"[MinionLib][MinionAction] Cancel queued action {ActionModelId.Entry} because TryAct returned false");
            Cancel();
        }
    }

    public override INetAction ToNetAction()
    {
        return new NetExecuteCreatureActionGameAction
        {
            actorCombatId = ActorCombatId,
            actionModelId = ActionModelId,
            targetCombatId = TargetCombatId
        };
    }

    public override string ToString()
    {
        return $"{nameof(ExecuteCreatureActionGameAction)} owner={OwnerId} actor={ActorCombatId} action={ActionModelId.Entry} target={TargetCombatId?.ToString() ?? "null"}";
    }
}

