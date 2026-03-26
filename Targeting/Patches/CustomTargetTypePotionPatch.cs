using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace MinionLib.Targeting.Patches;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[HarmonyPatch]
public static class CustomTargetTypePotionPatch
{
    private const string Module = "Targeting";

    private static readonly MethodInfo? TargetNodeMethod =
        AccessTools.Method(typeof(NPotionHolder), "TargetNode");

    private static readonly MethodInfo? ShouldCancelTargetingMethod =
        AccessTools.Method(typeof(NPotionHolder), "ShouldCancelTargeting");

    private static readonly AccessTools.FieldRef<NPotionPopup, NPotionPopupButton> UseButtonRef =
        AccessTools.FieldRefAccess<NPotionPopup, NPotionPopupButton>("_useButton");

    [HarmonyPatch(typeof(NPotionHolder), nameof(NPotionHolder.UsePotion))]
    [HarmonyPrefix]
    private static bool UsePotionPrefix(NPotionHolder __instance, ref Task __result)
    {
        var potion = __instance.Potion?.Model;
        if (potion == null || !CustomTargetTypeManager.TryGetCustomTargetType(potion.TargetType, out var customType, includeBuiltin:false))
            return true;

        if (!customType.IsSingleTarget)
        {
            potion.EnqueueManualUse(potion.Owner.Creature);
            __instance.TryGrabFocus();
            __result = Task.CompletedTask;
            return false;
        }

        __result = UseSingleTargetPotion(__instance, potion);
        return false;
    }

    [HarmonyPatch(typeof(NPotionHolder), "TargetNode")]
    [HarmonyPrefix]
    private static bool TargetNodePrefix(NPotionHolder __instance, TargetType targetType, ref Task __result)
    {
        var potion = __instance.Potion?.Model;
        if (potion == null || !CustomTargetTypeManager.TryGetCustomTargetType(targetType, out var customType, includeBuiltin:false))
            return true;

        __result = TargetNodeCustom(__instance, potion, targetType, customType);
        return false;
    }

    [HarmonyPatch(typeof(NPotionPopup), "_Ready")]
    [HarmonyPostfix]
    private static void PopupReadyPostfix(NPotionPopup __instance)
    {
        var potion = AccessTools.Property(typeof(NPotionPopup), "Potion")?.GetValue(__instance) as PotionModel;
        if (potion == null || !CustomTargetTypeManager.TryGetCustomTargetType(potion.TargetType, out var customType, includeBuiltin:false))
            return;

        var useButton = UseButtonRef(__instance);
        useButton.SetLocKey(customType.IsSingleTarget || potion.CanThrowAtAlly()
            ? "POTION_POPUP.throw"
            : "POTION_POPUP.drink");
    }

    private static async Task UseSingleTargetPotion(NPotionHolder holder, PotionModel potion)
    {
        if (TargetNodeMethod == null)
        {
            Debug(Module, "NPotionHolder.TargetNode method missing; canceled custom potion targeting");
            holder.TryGrabFocus();
            return;
        }

        RunManager.Instance.HoveredModelTracker.OnLocalPotionSelected(potion);
        try
        {
            await (Task)TargetNodeMethod.Invoke(holder, [potion.TargetType])!;
        }
        finally
        {
            RunManager.Instance.HoveredModelTracker.OnLocalPotionDeselected();
        }
    }

    private static async Task TargetNodeCustom(NPotionHolder holder, PotionModel potion, TargetType targetType,
        CustomTargetType customType)
    {
        var targetManager = NTargetManager.Instance;
        var isUsingController = NControllerManager.Instance?.IsUsingController ?? false;
        var startPosition = holder.GlobalPosition + Vector2.Right * holder.Size.X * 0.5f + Vector2.Down * 50f;

        Func<bool>? shouldCancel = null;
        if (ShouldCancelTargetingMethod != null)
            shouldCancel = () => (bool)ShouldCancelTargetingMethod.Invoke(holder, null)!;

        // Use potion-specific filtering so owner-locked target types cannot select other players' minions.
        targetManager.StartTargeting(targetType, startPosition,
            isUsingController ? TargetMode.Controller : TargetMode.ClickMouseToTarget,
            shouldCancel,
            node => IsAllowedPotionTargetNode(node, potion, customType));

        if (isUsingController && CombatManager.Instance.IsInProgress)
        {
            var combatState = potion.Owner.Creature.CombatState;
            if (combatState != null)
            {
                var validTargets = combatState.Creatures
                    .Where(c => c.IsAlive && customType.PotionPredicate(c, potion))
                    .Select(c => NCombatRoom.Instance?.GetCreatureNode(c)?.Hitbox)
                    .Where(hitbox => hitbox != null)
                    .Cast<Control>()
                    .ToList();

                if (validTargets.Count > 0)
                {
                    NCombatRoom.Instance?.RestrictControllerNavigation(validTargets);
                    validTargets[0].TryGrabFocus();
                }
            }
        }
        else if (isUsingController)
        {
            var multiplayerContainer = NRun.Instance?.GlobalUi.MultiplayerPlayerContainer;
            if (multiplayerContainer != null)
            {
                var validPlayers = GetValidPlayerStateHitboxes(multiplayerContainer, potion, customType);
                if (validPlayers.Count > 0)
                {
                    validPlayers[0].TryGrabFocus();
                    multiplayerContainer.LockNavigation();
                }
            }
        }

        try
        {
            var node = await targetManager.SelectionFinished();
            if (node == null)
                return;

            var target = ResolveTargetFromNode(node);
            if (target == null || !customType.PotionPredicate(target, potion))
                return;

            potion.EnqueueManualUse(target);
        }
        finally
        {
            NCombatRoom.Instance?.EnableControllerNavigation();
            NRun.Instance?.GlobalUi.MultiplayerPlayerContainer.UnlockNavigation();
            holder.TryGrabFocus();
        }
    }

    private static bool IsAllowedPotionTargetNode(Node node, PotionModel potion, CustomTargetType customType)
    {
        if (node is NCreature creatureNode)
            return customType.PotionPredicate(creatureNode.Entity, potion);

        if (node is NMultiplayerPlayerState playerState)
            return customType.PotionPredicate(playerState.Player.Creature, potion);

        return false;
    }

    private static List<Control> GetValidPlayerStateHitboxes(NMultiplayerPlayerStateContainer container,
        PotionModel potion, CustomTargetType customType)
    {
        var controls = new List<Control>();
        for (var i = 0; i < container.GetChildCount(); i++)
        {
            var state = container.GetChild(i) as NMultiplayerPlayerState;
            if (state == null)
                continue;

            if (customType.PotionPredicate(state.Player.Creature, potion))
                controls.Add(state.Hitbox);
        }

        return controls;
    }

    private static Creature? ResolveTargetFromNode(Node node)
    {
        if (node is NCreature creatureNode)
            return creatureNode.Entity;

        if (node is NMultiplayerPlayerState playerState)
            return playerState.Player.Creature;

        return null;
    }
}

