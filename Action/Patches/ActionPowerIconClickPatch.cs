using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MinionLib.Action.Patches;

[HarmonyPatch(typeof(NPower), nameof(NPower._Ready))]
public static class ActionPowerIconClickPatch
{
    [HarmonyPostfix]
    private static void Postfix(NPower __instance)
    {
        __instance.Connect(Control.SignalName.GuiInput,
            Callable.From<InputEvent>(inputEvent => OnPowerGuiInput(__instance, inputEvent)));
    }

    private static void OnPowerGuiInput(NPower powerNode, InputEvent inputEvent)
    {
        var triggeredByMouse =
            inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseButton &&
            mouseButton.IsReleased();

        if (!triggeredByMouse) return;

        if (NTargetManager.Instance.IsInSelection) return;

        if (powerNode.Model is not CustomActionModel actionPower) return;

        var actorNode = NCombatRoom.Instance?.GetCreatureNode(actionPower.Owner);
        if (actorNode == null) return;

        Log.Warn(
            $"[MinionLib][MinionAction] Trigger action from icon power={actionPower.Id.Entry} actor={actionPower.Owner.Name}");
        var position = powerNode.GlobalPosition + new Vector2(20, 20);
        TaskHelper.RunSafely(ActionClickPatch.TryUseActionFromIconAsync(actorNode, actionPower, position));
        powerNode.GetViewport().SetInputAsHandled();
    }
}
