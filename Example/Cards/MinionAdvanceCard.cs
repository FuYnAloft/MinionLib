using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MinionLib.Commands;
using MinionLib.Minion;
using MinionLib.Targeting;
using MinionLib.Utilities;

namespace MinionLib.Example.Cards;

[Pool(typeof(ColorlessCardPool))]
public sealed class MinionAdvanceCard()
    : CustomCardModel(0, CardType.Skill, CardRarity.Token, MinionTargetTypes.AnyMinion)
{
    public override string CustomPortraitPath => "res://images/packed/card_portraits/beta.png";

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target is not { Monster: MinionModel minion, PetOwner: not null }) return;

        await Cmd.Wait(0.20f);

        using var accessor = new PetsOrderAccessor(target.PetOwner);
        if (accessor.Pets == null) return;
        minion.Position = minion.Position switch
        {
            MinionPosition.Front => MinionPosition.FrontUpper,
            MinionPosition.FrontUpper => MinionPosition.Front,
            MinionPosition.Back => MinionPosition.BackUpper,
            MinionPosition.BackUpper => MinionPosition.Back,
            _ => MinionPosition.Front
        };
        accessor.Pets.Remove(target);
        accessor.Pets.Insert(0, target);
        _ = MinionAnimCmd.Rearrange(duration: 0.5f);
        accessor.SetManualRearranged();
    }
}