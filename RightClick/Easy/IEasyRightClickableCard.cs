using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MinionLib.RightClick.Easy;

public interface IEasyRightClickableCard
{
    bool CanHandleRightClickLocal(RightClickContext context) => true;
    
    Task OnRightClick(PlayerChoiceContext choiceContext, RightClickContext clickContext);
}