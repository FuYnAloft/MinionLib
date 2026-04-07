namespace MinionLib.RightClick;

public interface IRightClickHandler
{
    int Priority => 0;
    
    bool Handle(RightClickContext context) => false;
}

