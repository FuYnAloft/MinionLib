using MinionLib.RightClick.Easy;

namespace MinionLib.RightClick;

public static class RightClickDispatcher
{
    private const string Module = "CardRightClick";

    private static readonly List<IRightClickHandler> Handlers = [
        #if DEBUG
        new LogIdRightClickHandler(),
        #endif
        new EasyRightClickableCardHandler(),
    ];

    public static void Register(IRightClickHandler handler)
    {
        if (Handlers.Contains(handler))
            return;

        Handlers.Add(handler);
        Handlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    public static bool TryDispatch(RightClickContext context)
    {
        foreach (var handler in Handlers)
            if (handler.Handle(context))
                return true;


        Debug(Module, $"No right-click handler matched for {context.Model.Id.Entry}");
        return false;
    }

    private sealed class LogIdRightClickHandler : IRightClickHandler
    {
        public bool Handle(RightClickContext context)
        {
            Debug(Module, $"Right clicked model: {context.Model.Id.Entry}");
            return false;
        }
    }
}