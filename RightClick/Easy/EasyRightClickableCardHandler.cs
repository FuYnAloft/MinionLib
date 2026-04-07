using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace MinionLib.RightClick.Easy;

public class EasyRightClickableCardHandler : IRightClickHandler
{
    public bool Handle(RightClickContext context)
    {
        if (context.Model is not (CardModel card and IEasyRightClickableCard rightClickableCard)) return false;
        if (!rightClickableCard.CanHandleRightClickLocal(context)) return false;

        var queuedAction = new EasyRightClickCardAction(context);
        RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(queuedAction);
        return true;
    }
}