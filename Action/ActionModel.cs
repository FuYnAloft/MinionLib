using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Targeting;

namespace MinionLib.Action;

public abstract class ActionModel : PowerModel
{
    private static readonly IHoverTip ActionHoverTip = new HoverTip(
        new LocString("static_hover_tips", "action.title"),
        new LocString("static_hover_tips", "action.description"));

    public abstract TargetType TargetType { get; }

    public virtual bool AutoRemoveAtTurnEnd => false;

    public virtual bool DecrementAfterAct => true;

    public virtual bool OnlyRespondIconClick => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ActionHoverTip];

    public new void Flash()
    {
        base.Flash();
    }

    public virtual bool CanAct(CombatState combatState)
    {
        var actor = Owner;
        return Amount > 0m && actor.IsAlive && actor.CombatState == combatState;
    }

    public bool IsValidTarget(CombatState combatState, Creature? target)
    {
        if (target is not { IsAlive: true }) return false;

        if (CustomTargetTypeManager.TryGetCustomTargetType(TargetType, out var customType))
            return customType.ActionPredicate(target, this);

        return false;
    }

    public IReadOnlyList<Creature> GetValidTargets(CombatState combatState)
    {
        return combatState.Creatures
            .Where(target => IsValidTarget(combatState, target))
            .ToList();
    }

    public async Task<bool> TryAct(PlayerChoiceContext choiceContext, Creature? target)
    {
        var actor = Owner;
        var combatState = actor.CombatState;
        if (combatState == null || !CanAct(combatState)) return false;

        if (TargetType == TargetType.None)
        {
            await ExecuteAct(choiceContext, null);
            return true;
        }

        if (TargetType.IsSingleTarget())
        {
            if (!IsValidTarget(combatState, target)) return false;

            await ExecuteAct(choiceContext, target);
            return true;
        }

        if (GetValidTargets(combatState).Count == 0) return false;

        await ExecuteAct(choiceContext, null);
        return true;
    }

    private async Task ExecuteAct(PlayerChoiceContext choiceContext, Creature? target)
    {
        await OnAct(choiceContext, target);
        if (DecrementAfterAct)
            await PowerCmd.Decrement(this);
        if (CombatManager.Instance.IsInProgress)
            await CombatManager.Instance.CheckWinCondition();
    }

    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (!AutoRemoveAtTurnEnd || Owner.Side != side || Amount <= 0) return;

        await PowerCmd.Remove(this);
    }

    protected abstract Task OnAct(PlayerChoiceContext choiceContext, Creature? target);
}

public abstract class CustomActionModel : ActionModel, ICustomPower
{
    public virtual string? CustomPackedIconPath => null;

    public virtual string? CustomBigIconPath => null;

    public virtual string? CustomBigBetaIconPath => null;
}