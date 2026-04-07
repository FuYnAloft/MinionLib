using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace MinionLib.RightClick.Patches;

[HarmonyPatch(typeof(NPlayerHand), "AddCardHolder")]
public static class CardRightClickPatch
{
    private const string Module = "CardRightClickPatch";

    [HarmonyPostfix]
    private static void Postfix(NHandCardHolder holder)
    {
        // Controller actions are routed to the focused holder control.
        holder.Connect(Control.SignalName.GuiInput,
            Callable.From<InputEvent>(inputEvent => OnHolderGuiInput(holder, inputEvent)));

        // Mouse right click comes from hitbox pointer events.
        holder.Hitbox.Connect(Control.SignalName.GuiInput,
            Callable.From<InputEvent>(inputEvent => OnHitboxGuiInput(holder, inputEvent)));
    }

    private static void OnHolderGuiInput(NCardHolder holder, InputEvent inputEvent)
    {
        var triggeredByController =
            inputEvent is InputEventAction { Action: var action } actionEvent &&
            action == MegaInput.cancel &&
            actionEvent.IsPressed() &&
            holder.HasFocus();

        if (!triggeredByController) return;

        TryHandleRightClick(holder, isController: true);
    }

    private static void OnHitboxGuiInput(NCardHolder holder, InputEvent inputEvent)
    {
        var triggeredByMouse =
            inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Right } rightClick &&
            rightClick.IsPressed();

        if (!triggeredByMouse) return;

        TryHandleRightClick(holder, isController: false);
    }

    private static void TryHandleRightClick(NCardHolder holder, bool isController)
    {
        if (holder.GetViewport().IsInputHandled())
            return;

        var hand = NPlayerHand.Instance;
        if (hand == null)
            return;

        var card = holder.CardModel;
        if (card == null)
        {
            Debug(Module, "Ignored right click because holder has no card");
            return;
        }

        if (hand.InCardPlay || NTargetManager.Instance.IsInSelection)
        {
            Debug(Module, $"Ignored right click for {card.Id.Entry} because card targeting is in progress");
            return;
        }

        var context = new RightClickContext(card.Owner, card,
            new RightClickContext.Payload(isController: isController));

        if (RightClickDispatcher.TryDispatch(context))
        {
            holder.GetViewport().SetInputAsHandled();
        }
    }
}