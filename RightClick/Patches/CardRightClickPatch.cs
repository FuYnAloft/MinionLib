using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MinionLib.RightClick.Patches;

[HarmonyPatch(typeof(NPlayerHand), "AddCardHolder")]
public static class CardRightClickPatch
{
    [HarmonyPostfix]
    private static void Postfix(NHandCardHolder holder)
    {
        holder.Hitbox.Connect(NClickableControl.SignalName.MousePressed,
            Callable.From<InputEvent>(inputEvent => OnHitboxMousePressed(holder, inputEvent)));
    }

    private static void OnHitboxMousePressed(NCardHolder holder, InputEvent inputEvent)
    {
        if (holder.GetViewport().IsInputHandled())
            return;

        if (inputEvent is not InputEventMouseButton { ButtonIndex: MouseButton.Right } rightClick ||
            !rightClick.IsPressed())
            return;

        var hand = NPlayerHand.Instance;
        if (hand == null)
            return;

        if (CardRightClickDispatcher.TryDispatch(hand, holder))
        {
            holder.GetViewport().SetInputAsHandled();
        }
    }
}