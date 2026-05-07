using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.Patching.Models;

namespace MinionLib.Action.Patches;

public sealed class ActionPowerIconClickPatch : IPatchMethod
{
    private const string Module = "MinionAction";

    public static string PatchId => "action_power_icon_click";

    public static bool IsCritical => false;

    public static string Description => "Connect power icon input for MinionLib action powers.";

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NPower), nameof(NPower._Ready))];
    }

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

        if (powerNode.Model is not ActionModel actionPower) return;

        var actorNode = NCombatRoom.Instance?.GetCreatureNode(actionPower.Owner);
        if (actorNode == null) return;

        Debug(Module,
            $"Trigger action from icon power={actionPower.Id.Entry} actor={actionPower.Owner.Name}");
        var position = powerNode.GlobalPosition + new Vector2(20, 20);
        TaskHelper.RunSafely(ActionClickPatch.TryUseActionFromIconAsync(actorNode, actionPower, position));
        powerNode.GetViewport().SetInputAsHandled();
    }
}
