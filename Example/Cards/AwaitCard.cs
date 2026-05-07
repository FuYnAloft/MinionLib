using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MinionLib.Example.Cards;

[RegisterCard(typeof(TokenCardPool))]
public sealed class AwaitCard() : ModCardTemplate(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override string? CustomPortraitPath => "res://images/packed/card_portraits/beta.png";

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Cmd.Wait(3.0f);
    }
}
