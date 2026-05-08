using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Commands;
using MinionLib.Minion;
using MinionLib.Targeting;
using MinionLib.Utilities;

namespace MinionLib.Example.Cards;

public sealed class MinionAdvanceCard()
    : CardModel(0, CardType.Skill, CardRarity.Token, MinionTargetTypes.AnyMinion)
{
    private const string PortraitResourcePath = "res://images/packed/card_portraits/beta.png";

    public override string PortraitPath => PortraitResourcePath;

    public override string BetaPortraitPath => PortraitResourcePath;

    public override IEnumerable<string> AllPortraitPaths => [PortraitResourcePath];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target is not { Monster: MinionModel minion, PetOwner: not null }) return;

        await Cmd.Wait(0.20f);

        using var accessor = new PetsOrderAccessor(target.PetOwner);
        if (accessor.Pets == null) return;
        var nextPosition = minion.Position switch
        {
            MinionPosition.Front => MinionPosition.FrontUpper,
            MinionPosition.FrontUpper => MinionPosition.Front,
            MinionPosition.Back => MinionPosition.BackUpper,
            MinionPosition.BackUpper => MinionPosition.Back,
            _ => MinionPosition.Front
        };
        minion.SetPosition(nextPosition);
        accessor.Pets.Remove(target);
        accessor.Pets.Insert(0, target);
        _ = MinionAnimCmd.Rearrange(duration: 0.5f);
        accessor.SetManualRearranged();
    }
}
