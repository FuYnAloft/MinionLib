using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MinionLib.Example.Powers;

public sealed class DefenseakaGiftPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://Example/MinionTest/orb.png";

    public override string CustomBigIconPath => "res://Example/MinionTest/orb.png";

    public override string CustomBigBetaIconPath => "res://Example/MinionTest/orb.png";

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        CombatState combatState)
    {
        if (side != Owner.Side || !Owner.IsAlive || Owner.PetOwner == null) return;

        for (var i = 0; i < Amount; i++)
        {
            // var owner = Owner.PetOwner;
            // var card = combatState.CreateCard<DefenseakaGuardCard>(owner);
            // card.BindMinion(Owner);
            // await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, false);
            Debug("DefenseakaGuardCard was removed");
        }
    }
}