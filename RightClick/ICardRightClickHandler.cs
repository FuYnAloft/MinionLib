namespace MinionLib.RightClick;

public interface ICardRightClickHandler
{
    int Priority => 0;
    
    bool Handle(CardRightClickContext context) => false;
}

