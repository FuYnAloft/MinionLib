using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MinionLib.Component;

public interface ICardComponent
{
    string ComponentId { get; }

    IComponentsCardModel? Card { get; }

    void Attach(IComponentsCardModel card);

    void Detach();

    ICardComponent DeepClone();

    ICardComponent? MergeWith(ICardComponent other);

    Task OnPlayPrefix(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext);

    Task OnPlayPostfix(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext);

    string GetFormattedPrefix();

    string GetFormattedPostfix();
}
