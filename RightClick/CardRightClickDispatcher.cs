using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace MinionLib.RightClick;

public static class CardRightClickDispatcher
{
    private const string Module = "CardRightClick";

    private static readonly List<ICardRightClickHandler> Handlers = [
        #if DEBUG
        new LogCardIdRightClickHandler(),
        #endif
    ];

    public static void Register(ICardRightClickHandler handler)
    {
        if (Handlers.Contains(handler))
            return;

        Handlers.Add(handler);
        Handlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    public static bool TryDispatch(NPlayerHand hand, NCardHolder holder)
    {
        var card = holder.CardModel;
        if (card == null)
        {
            Debug(Module, "Ignored right click because holder has no card");
            return false;
        }

        if (hand.InCardPlay || NTargetManager.Instance.IsInSelection)
        {
            Debug(Module, $"Ignored right click for {card.Id.Entry} because card targeting is in progress");
            return false;
        }

        var context = new CardRightClickContext(hand, holder, card);
        foreach (var handler in Handlers)
            if (handler.Handle(context))
                return true;


        Debug(Module, $"No right-click handler matched for {card.Id.Entry}");
        return false;
    }

    private sealed class LogCardIdRightClickHandler : ICardRightClickHandler
    {
        public bool Handle(CardRightClickContext context)
        {
            Debug(Module, $"Right clicked card: {context.Card.Id.Entry}");
            return false;
        }
    }
}