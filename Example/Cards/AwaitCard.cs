using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace MinionLib.Example.Cards;

[Pool(typeof(ColorlessCardPool))]
public sealed class AwaitCard() : CustomCardModel(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Cmd.Wait(3.0f);
    }
}


