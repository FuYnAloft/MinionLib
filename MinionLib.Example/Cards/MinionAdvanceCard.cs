using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MinionLib.Commands;
using MinionLib.Minion;
using MinionLib.Targeting;
using MinionLib.Utilities;

namespace MinionLib.Example.Cards;

[RegisterCard(typeof(TokenCardPool))]
public sealed class MinionAdvanceCard()
    : ExampleCardTemplate(0, CardType.Skill, CardRarity.Token, MinionTargetTypes.AnyMinion)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target is not { Monster: MinionModel, PetOwner: not null }) return;

        await Cmd.Wait(0.20f);

        using var accessor = new PetsOrderAccessor(target.PetOwner);
        if (accessor.Pets == null) return;
        accessor.Pets.Remove(target);
        accessor.Pets.Insert(0, target);
        _ = MinionAnimCmd.Rearrange(duration: 0.5f);
        accessor.SetManualRearranged();
    }
}
