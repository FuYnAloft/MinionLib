using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Context;

namespace MinionLib.Action.GameActions;

public sealed class ExecuteCreatureActionGameAction : GameAction
{
    private const string Module = "MinionAction";

    public override ulong OwnerId => Owner.NetId;

    public override GameActionType ActionType => GameActionType.CombatPlayPhaseOnly;

    private Player Owner { get; }

    private uint ActorCombatId { get; }

    private uint? TargetCombatId { get; }

    private ModelId ActionModelId { get; }

    public ExecuteCreatureActionGameAction(ActionModel action, Creature? target)
    {
        var actor = action.Owner;
        if (actor.CombatId == null)
            throw new InvalidOperationException("Cannot enqueue creature action without actor combat id.");

        var owner = ResolveQueueOwner(actor) ??
                    throw new InvalidOperationException("Cannot enqueue creature action without queue owner.");

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

    private static Player? ResolveQueueOwner(Creature actor)
    {
        if (actor.PetOwner != null)
            return actor.PetOwner;

        if (actor.Player != null)
            return actor.Player;

        if (actor.CombatState != null)
            return LocalContext.GetMe(actor.CombatState);

        return null;
    }

    protected override async Task ExecuteAction()
    {
        try
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
                Debug(Module, $"Cancel queued action {ActionModelId.Entry} because actor no longer valid");
                Cancel();
                return;
            }

            var action = actor.Powers.OfType<ActionModel>().FirstOrDefault(power => power.Id == ActionModelId);
            if (action == null || action.Owner != actor)
            {
                Debug(Module, $"Cancel queued action {ActionModelId.Entry} because action power no longer exists");
                Cancel();
                return;
            }

            if (!action.CanAct(combatState))
            {
                Debug(Module, $"Cancel queued action {ActionModelId.Entry} because CanAct failed");
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

                if (!action.IsValidTarget(combatState, target))
                {
                    Debug(Module, $"Cancel queued action {ActionModelId.Entry} because target is no longer valid");
                    Cancel();
                    return;
                }
            }
            else if (action.TargetType != TargetType.None && action.GetValidTargets(combatState).Count == 0)
            {
                Debug(Module, $"Cancel queued action {ActionModelId.Entry} because no valid targets remain");
                Cancel();
                return;
            }

            var didAct = await action.TryAct(new GameActionPlayerChoiceContext(this), target);
            if (!didAct)
            {
                Debug(Module, $"Cancel queued action {ActionModelId.Entry} because TryAct returned false");
                Cancel();
            }
        }
        finally
        {
            CreatureActionQueueThreshold.Release(ActorCombatId, ActionModelId);
        }
    }

    public override INetAction ToNetAction()
    {
        return new NetExecuteCreatureActionGameAction
        {
            ActorCombatId = this.ActorCombatId,
            ActionModelId = this.ActionModelId,
            TargetCombatId = this.TargetCombatId
        };
    }

    public override string ToString()
    {
        return $"{nameof(ExecuteCreatureActionGameAction)} owner={OwnerId} actor={ActorCombatId} action={ActionModelId.Entry} target={TargetCombatId?.ToString() ?? "null"}";
    }
}

