using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MinionLib.Targeting.Patches;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[HarmonyPatch]
public static class CustomTargetTypeCardPatch
{
    private const string Module = "Targeting";

    private static readonly AccessTools.FieldRef<NTargetManager, TargetType> ValidTargetsTypeRef =
        AccessTools.FieldRefAccess<NTargetManager, TargetType>("_validTargetsType");

    private static readonly MethodInfo? MouseSingleCreatureTargeting =
        AccessTools.Method(typeof(NMouseCardPlay), "SingleCreatureTargeting");

    private static readonly MethodInfo? ControllerSingleCreatureTargeting =
        AccessTools.Method(typeof(NControllerCardPlay), "SingleCreatureTargeting");

    private static readonly MethodInfo? OnCreatureHoverMethod =
        AccessTools.Method(typeof(NCardPlay), "OnCreatureHover");

    private static readonly MethodInfo? OnCreatureUnhoverMethod =
        AccessTools.Method(typeof(NCardPlay), "OnCreatureUnhover");

    private static readonly MethodInfo? TryPlayCardMethod =
        AccessTools.Method(typeof(NCardPlay), "TryPlayCard");

    private static readonly MethodInfo? CardPlayCleanupMethod =
        AccessTools.Method(typeof(NCardPlay), "Cleanup");

    [HarmonyPatch(typeof(ActionTargetExtensions), nameof(ActionTargetExtensions.IsSingleTarget))]
    [HarmonyPostfix]
    private static void IsSingleTargetPostfix(TargetType targetType, ref bool __result)
    {
        if (!CustomTargetTypeManager.TryGetCustomTargetType(targetType, out var customType, false)) return;

        __result = customType.IsSingleTarget;
        Debug(Module, $"IsSingleTarget {targetType} -> {__result}");
    }

    [HarmonyPatch(typeof(NTargetManager), "AllowedToTargetCreature")]
    [HarmonyPrefix]
    private static bool AllowedToTargetCreaturePrefix(NTargetManager __instance, Creature creature, ref bool __result)
    {
        var targetType = ValidTargetsTypeRef(__instance);
        if (!CustomTargetTypeManager.TryGetCustomTargetType(targetType, out var customType, false)) return true;

        __result = customType.GeneralPredicate(creature);
        Debug(Module, $"AllowedToTargetCreature {targetType} {creature.Name} -> {__result}");
        return false;
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.IsValidTarget))]
    [HarmonyPrefix]
    private static bool IsValidTargetPrefix(CardModel __instance, Creature? target, ref bool __result)
    {
        if (!CustomTargetTypeManager.TryGetCustomTargetType(__instance.TargetType, out var customType, false))
            return true;

        __result = customType.IsSingleTarget
            ? target != null && customType.CardPredicate(target, __instance)
            : target == null || customType.CardPredicate(target, __instance);
        Debug(Module,
            $"CardModel.IsValidTarget card={__instance.Id.Entry} target={target?.Name ?? "null"} -> {__result}");
        return false;
    }

    [HarmonyPatch(typeof(NCardPlay), "TryPlayCard")]
    [HarmonyPrefix]
    private static bool TryPlayCardPrefix(NCardPlay __instance, Creature? target)
    {
        var card = GetCurrentCard(__instance);
        if (card == null ||
            !CustomTargetTypeManager.TryGetCustomTargetType(card.TargetType, out var customType, false)) return true;

        if (customType.IsSingleTarget && target == null)
        {
            __instance.CancelPlayCard();
            Debug(Module,
                $"TryPlayCard canceled: custom single-target card={card.Id.Entry} but target is null");
            return false;
        }

        var resolvedTarget = customType.IsSingleTarget ? target : null;
        if (!card.CanPlayTargeting(resolvedTarget))
        {
            __instance.CancelPlayCard();
            Debug(Module,
                $"TryPlayCard CanPlayTargeting failed card={card.Id.Entry} target={resolvedTarget?.Name ?? "null"}");
            return false;
        }

        if (!card.TryManualPlay(resolvedTarget))
        {
            __instance.CancelPlayCard();
            Debug(Module,
                $"TryPlayCard TryManualPlay failed card={card.Id.Entry} target={resolvedTarget?.Name ?? "null"}");
            return false;
        }

        CardPlayCleanupMethod?.Invoke(__instance, null);
        __instance.EmitSignal(NCardPlay.SignalName.Finished, true);
        NCombatRoom.Instance?.Ui.Hand.TryGrabFocus();
        Debug(Module,
            $"TryPlayCard success card={card.Id.Entry} target={resolvedTarget?.Name ?? "null"}");
        return false;
    }

    [HarmonyPatch(typeof(NCardPlay), "ShowMultiCreatureTargetingVisuals")]
    [HarmonyPostfix]
    private static void ShowMultiCreatureTargetingVisualsPostfix(NCardPlay __instance)
    {
        var card = GetCurrentCard(__instance);
        if (card?.CombatState == null ||
            !CustomTargetTypeManager.TryGetCustomTargetType(card.TargetType, out var customType, false) ||
            customType.IsSingleTarget) return;

        var cardNode = __instance.Holder.CardNode;
        if (cardNode == null) return;

        var validTargets = card.CombatState.Creatures
            .Where(c => c.IsAlive && customType.CardPredicate(c, card))
            .ToList();

        if (validTargets.Count == 1) cardNode.SetPreviewTarget(validTargets[0]);

        cardNode.UpdateVisuals((cardNode.Model?.Pile?.Type).GetValueOrDefault(),
            CardPreviewMode.MultiCreatureTargeting);
        foreach (var validTarget in validTargets)
            NCombatRoom.Instance?.GetCreatureNode(validTarget)?.ShowMultiselectReticle();

        Debug(Module,
            $"ShowMultiCreatureTargetingVisuals custom {card.TargetType}, targets={validTargets.Count}");
    }

    [HarmonyPatch(typeof(NMouseCardPlay), "MultiCreatureTargeting")]
    [HarmonyPrefix]
    private static bool MouseMultiCreatureTargetingPrefix(NMouseCardPlay __instance, TargetMode targetMode,
        ref Task __result)
    {
        var card = GetCurrentCard(__instance);
        if (card == null ||
            !CustomTargetTypeManager.TryGetCustomTargetType(card.TargetType, out var customType, false)) return true;

        if (!customType.IsSingleTarget) return true;

        if (MouseSingleCreatureTargeting == null)
        {
            __result = Task.CompletedTask;
            __instance.CancelPlayCard();
            Debug(Module, "Mouse single-target method missing; canceled");
            return false;
        }

        Debug(Module, $"Mouse MultiCreatureTargeting -> Single {card.TargetType}");
        __result = (Task)MouseSingleCreatureTargeting.Invoke(__instance, [targetMode, card.TargetType])!;
        return false;
    }

    [HarmonyPatch(typeof(NControllerCardPlay), "MultiCreatureTargeting")]
    [HarmonyPrefix]
    private static bool ControllerMultiCreatureTargetingPrefix(NControllerCardPlay __instance)
    {
        var card = GetCurrentCard(__instance);
        if (card == null ||
            !CustomTargetTypeManager.TryGetCustomTargetType(card.TargetType, out var customType, false)) return true;

        if (!customType.IsSingleTarget) return true;

        if (ControllerSingleCreatureTargeting == null)
        {
            __instance.CancelPlayCard();
            Debug(Module, "Controller single-target method missing; canceled");
            return false;
        }

        Debug(Module, $"Controller MultiCreatureTargeting -> Single {card.TargetType}");
        TaskHelper.RunSafely((Task)ControllerSingleCreatureTargeting.Invoke(__instance, [card.TargetType])!);
        return false;
    }

    [HarmonyPatch(typeof(NControllerCardPlay), "SingleCreatureTargeting")]
    [HarmonyPrefix]
    private static bool ControllerSingleCreatureTargetingPrefix(NControllerCardPlay __instance, TargetType targetType,
        ref Task __result)
    {
        if (!CustomTargetTypeManager.TryGetCustomTargetType(targetType, out _, false)) return true;

        __result = ControllerSingleCustomTargeting(__instance, targetType);
        return false;
    }

    private static async Task ControllerSingleCustomTargeting(NControllerCardPlay cardPlay, TargetType targetType)
    {
        var card = GetCurrentCard(cardPlay);
        var cardNode = cardPlay.Holder.CardNode;
        var room = NCombatRoom.Instance;
        if (card == null || cardNode == null || room == null || card.CombatState == null ||
            !CustomTargetTypeManager.TryGetCustomTargetType(targetType, out var customType, false))
        {
            cardPlay.CancelPlayCard();
            return;
        }

        var targetManager = NTargetManager.Instance;
        var onHover = Callable.From<NCreature>(creature => OnCreatureHoverMethod?.Invoke(cardPlay, [creature]));
        var onUnhover = Callable.From<NCreature>(creature => OnCreatureUnhoverMethod?.Invoke(cardPlay, [creature]));

        targetManager.Connect(NTargetManager.SignalName.CreatureHovered, onHover);
        targetManager.Connect(NTargetManager.SignalName.CreatureUnhovered, onUnhover);

        targetManager.StartTargeting(targetType, cardNode, TargetMode.Controller,
            () => !GodotObject.IsInstanceValid(cardPlay) || !(NControllerManager.Instance?.IsUsingController ?? false),
            null);

        var validTargets = card.CombatState.Creatures
            .Where(c => c.IsAlive && customType.CardPredicate(c, card))
            .ToList();

        Debug(Module, $"Controller targetType={targetType}, validTargets={validTargets.Count}");

        if (validTargets.Count == 0)
        {
            targetManager.Disconnect(NTargetManager.SignalName.CreatureHovered, onHover);
            targetManager.Disconnect(NTargetManager.SignalName.CreatureUnhovered, onUnhover);
            cardPlay.CancelPlayCard();
            return;
        }

        var controls = validTargets
            .Select(c => room.GetCreatureNode(c)?.Hitbox)
            .Where(control => control != null)
            .Cast<Control>();

        room.RestrictControllerNavigation(controls);
        room.GetCreatureNode(validTargets.First())?.Hitbox.TryGrabFocus();

        var selectedNode = await targetManager.SelectionFinished();
        if (!GodotObject.IsInstanceValid(cardPlay)) return;

        targetManager.Disconnect(NTargetManager.SignalName.CreatureHovered, onHover);
        targetManager.Disconnect(NTargetManager.SignalName.CreatureUnhovered, onUnhover);

        if (selectedNode is NCreature creatureNode && TryPlayCardMethod != null)
            TryPlayCardMethod.Invoke(cardPlay, [creatureNode.Entity]);
        else
            cardPlay.CancelPlayCard();
    }

    private static CardModel? GetCurrentCard(NCardPlay cardPlay)
    {
        return AccessTools.Property(typeof(NCardPlay), "Card")?.GetValue(cardPlay) as CardModel;
    }
}