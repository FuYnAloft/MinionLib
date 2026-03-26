using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MinionLib.Targeting;

namespace MinionLib.Action.Patches;

[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
public static class ActionClickPatch
{
    private static readonly HashSet<uint> TargetingActors = [];

    [HarmonyPostfix]
    private static void Postfix(NCreature __instance)
    {
        __instance.Hitbox.Connect(Control.SignalName.GuiInput,
            Callable.From<InputEvent>(inputEvent => OnGuiInput(__instance, inputEvent)));

        Log.Warn($"[MinionLib][MinionAction] Connected input handler for creature {__instance.Entity.Name}");
    }

    private static void OnGuiInput(NCreature actorNode, InputEvent inputEvent)
    {
        var triggeredByMouse =
            inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseButton &&
            mouseButton.IsReleased();

        var triggeredByController =
            inputEvent is InputEventAction { Action: var action } actionEvent &&
            action == MegaInput.select &&
            actionEvent.IsPressed() &&
            actorNode.Hitbox.HasFocus();

        if (!triggeredByMouse && !triggeredByController) return;

        var targetManager = NTargetManager.Instance;
        if (targetManager.IsInSelection) return;

        if (triggeredByMouse && targetManager.LastTargetingFinishedFrame == actorNode.GetTree().GetFrame())
        {
            // Ignore the same-frame release that just confirmed another creature's targeting selection.
            Log.Warn($"[MinionLib][MinionAction] Ignore chained click on {actorNode.Entity.Name}");
            return;
        }

        TaskHelper.RunSafely(TryUseActionAsync(actorNode, triggeredByController, null));
        actorNode.GetViewport().SetInputAsHandled();
    }

    public static Task TryUseActionFromIconAsync(NCreature actorNode, CustomActionModel actionPower, Vector2 position)
    {
        return TryUseActionAsync(actorNode, false, actionPower, position);
    }

    private static async Task TryUseActionAsync(NCreature actorNode, bool useController,
        CustomActionModel? preferredAction, Vector2? overrideStartPosition = null)
    {
        var actor = actorNode.Entity;
        if (!actor.IsAlive || actor.CombatId == null) return;

        if (!CombatManager.Instance.IsInProgress || CombatManager.Instance.PlayerActionsDisabled) return;

        if (CreatureActionDebounceGate.IsBlocked(actor.CombatId.Value))
        {
            Log.Warn($"[MinionLib][MinionAction] Ignore click for {actor.Name} due to debounce window");
            return;
        }

        if (actor.PetOwner != null && !LocalContext.IsMe(actor.PetOwner)) return;

        if (actor.CombatState == null || actor.CombatState.CurrentSide != actor.Side) return;

        var combatState = actor.CombatState;
        var actionPower = preferredAction;
        if (actionPower == null || actionPower.Owner != actor)
            actionPower = actor.Powers.OfType<CustomActionModel>().FirstOrDefault();

        if (actionPower == null)
        {
            Log.Warn($"[MinionLib][MinionAction] {actor.Name} clicked with no action power");
            return;
        }

        if (!actionPower.CanAct(actor, combatState))
        {
            Log.Warn($"[MinionLib][MinionAction] {actor.Name} action {actionPower.Id.Entry} cannot act");
            return;
        }

        var targetType = actionPower.TargetType;
        var singleTarget = targetType.IsSingleTarget();
        var validTargets = actionPower.GetValidTargets(actor, combatState);

        Log.Warn(
            $"[MinionLib][MinionAction] {actor.Name} using action {actionPower.Id.Entry}, targetType={targetType}, single={singleTarget}, targets={validTargets.Count}");

        if (targetType == TargetType.None)
        {
            var enqueuedNone = CreatureActionQueueService.TryEnqueue(actor, actionPower, null);
            Log.Warn($"[MinionLib][MinionAction] {actor.Name} enqueue no-target action result={enqueuedNone}");
            return;
        }

        if (!singleTarget)
        {
            if (validTargets.Count == 0)
            {
                Log.Warn($"[MinionLib][MinionAction] {actor.Name} has no valid multi-targets");
                return;
            }

            var enqueuedAll = CreatureActionQueueService.TryEnqueue(actor, actionPower, null);
            Log.Warn($"[MinionLib][MinionAction] {actor.Name} enqueue multi-target action result={enqueuedAll}");
            return;
        }

        // For self-target actions, avoid opening the targeting UI and execute immediately.
        if (targetType == TargetType.Self)
        {
            var enqueuedSelf = CreatureActionQueueService.TryEnqueue(actor, actionPower, actor);
            Log.Warn($"[MinionLib][MinionAction] {actor.Name} enqueue self-target action result={enqueuedSelf}");
            return;
        }

        if (validTargets.Count == 0)
        {
            Log.Warn($"[MinionLib][MinionAction] {actor.Name} has no valid single-targets");
            return;
        }

        var actorId = actor.CombatId.Value;
        if (!TargetingActors.Add(actorId)) return;

        try
        {
            var targetMode = useController ? TargetMode.Controller : TargetMode.ClickMouseToTarget;
            var startPosition = overrideStartPosition ?? actorNode.Hitbox.GlobalPosition + actorNode.Hitbox.Size / 2f;

            Log.Warn(
                $"[MinionLib][MinionAction] Start targeting for {actor.Name}, mode={targetMode}, targetType={targetType}");
            actionPower.StartPulsing();

            if (CustomTargetTypeManager.IsCustomTargetType(targetType) &&
                CustomTargetTypeManager.TryGetCustomTargetType(targetType, out var customTargetType))
                NTargetManager.Instance.StartTargeting(MinionTargetTypes.AnyEntity, startPosition, targetMode,
                    () => !GodotObject.IsInstanceValid(actorNode) || !actor.IsAlive, node =>
                    {
                        if (node is not NCreature creatureNode) return false;
                        var target = creatureNode.Entity;
                        return customTargetType.ActionPredicate(target, actionPower, actor);
                    });
            else
                NTargetManager.Instance.StartTargeting(targetType, startPosition, targetMode,
                    () => !GodotObject.IsInstanceValid(actorNode) || !actor.IsAlive, null);

            var selectedNode = await NTargetManager.Instance.SelectionFinished();
            if (selectedNode is not NCreature targetNode)
            {
                Log.Warn("[MinionLib][MinionAction] Targeting canceled");
                return;
            }

            var target = targetNode.Entity;
            if (!actionPower.IsValidTarget(combatState, actor, target))
            {
                Log.Warn($"[MinionLib][MinionAction] Invalid selected target {target.Name}");
                return;
            }

            var enqueued = CreatureActionQueueService.TryEnqueue(actor, actionPower, target);
            Log.Warn($"[MinionLib][MinionAction] {actor.Name} targeted {target.Name}, enqueued={enqueued}");
        }
        finally
        {
            actionPower.StopPulsing();
            TargetingActors.Remove(actorId);
        }
    }
}
