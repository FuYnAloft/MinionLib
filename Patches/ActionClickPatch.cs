using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MinionLib.Models;
using MinionLib.Targeting;

namespace MinionLib.Patches;

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
            var actedNone = await actionPower.TryAct(new BlockingPlayerChoiceContext(), actor, null);
            Log.Warn($"[MinionLib][MinionAction] {actor.Name} no-target action result={actedNone}");
            return;
        }

        if (!singleTarget)
        {
            if (validTargets.Count == 0)
            {
                Log.Warn($"[MinionLib][MinionAction] {actor.Name} has no valid multi-targets");
                return;
            }

            var actedAll = await actionPower.TryAct(new BlockingPlayerChoiceContext(), actor, null);
            Log.Warn($"[MinionLib][MinionAction] {actor.Name} multi-target action result={actedAll}");
            return;
        }

        // For self-target actions, avoid opening the targeting UI and execute immediately.
        if (targetType == TargetType.Self)
        {
            var actedSelf = await actionPower.TryAct(new BlockingPlayerChoiceContext(), actor, actor);
            Log.Warn($"[MinionLib][MinionAction] {actor.Name} self-target action result={actedSelf}");
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

            var acted = await actionPower.TryAct(new BlockingPlayerChoiceContext(), actor, target);
            Log.Warn($"[MinionLib][MinionAction] {actor.Name} targeted {target.Name}, acted={acted}");
        }
        finally
        {
            actionPower.StopPulsing();
            TargetingActors.Remove(actorId);
        }
    }
}