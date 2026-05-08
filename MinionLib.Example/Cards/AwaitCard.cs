using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MinionLib.Example.Cards;

public sealed class AwaitCard() : CardModel(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    private const string PortraitResourcePath = "res://images/packed/card_portraits/beta.png";

    public override string PortraitPath => PortraitResourcePath;

    public override string BetaPortraitPath => PortraitResourcePath;

    public override IEnumerable<string> AllPortraitPaths => [PortraitResourcePath];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Cmd.Wait(3.0f);
    }
}
