using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Commands;
using MinionLib.Action;
using MinionLib.Targeting;

namespace MinionLib.Example.Actions;

public sealed class PetAttackPoint : CustomActionModel
{
    public override TargetType TargetType => TargetType.AnyEnemy;

    public override bool AutoRemoveAtTurnEnd => true;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://Example/MinionTest/orb.png";

    public override string CustomBigIconPath => "res://Example/MinionTest/orb.png";

    public override string CustomBigBetaIconPath => "res://Example/MinionTest/orb.png";

    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature actor, Creature? target)
    {
        if (target == null) return;
        await MinionAnimCmd.PlayBumpAttackAsync(actor, target,
            () => CreatureCmd.Damage(choiceContext, target, 0m, ValueProp.Move, actor, null));
        await PowerCmd.Decrement(this);
    }
}