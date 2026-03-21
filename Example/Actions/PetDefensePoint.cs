using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Models;
using MinionLib.Targeting;
using MinionLib.Utilities;

namespace MinionLib.Example.Actions;

public sealed class PetDefensePoint : CustomActionModel
{
    public override TargetType TargetType => MinionTargetTypes.AnyMinionOrOwner;

    public override bool AutoRemoveAtTurnEnd => true;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://Example/MinionTest/orb.png";

    public override string CustomBigIconPath => "res://Example/MinionTest/orb.png";

    public override string CustomBigBetaIconPath => "res://Example/MinionTest/orb.png";

    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature actor, Creature? target)
    {
        if (target == null) return;

        var block = actor.GetPowerAmount<DexterityPower>();
        await CreatureCmd.GainBlock(target, block, ValueProp.Move, null);
        await PowerCmd.Decrement(this);
    }
}