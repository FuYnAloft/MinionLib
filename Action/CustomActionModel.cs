using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MinionLib.Targeting;

namespace MinionLib.Action;

public abstract class CustomActionModel : CustomPowerModel
{
    private static readonly IHoverTip ActionHoverTip = new HoverTip(
        new LocString("static_hover_tips", "action.title"),
        new LocString("static_hover_tips", "action.description"));

    public abstract TargetType TargetType { get; }

    public virtual bool AutoRemoveAtTurnEnd => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [ActionHoverTip];

    public virtual bool CanAct(Creature pet, CombatState combatState)
    {
        return Amount > 0m && pet.IsAlive && pet.CombatState == combatState;
    }

    public bool IsValidTarget(CombatState combatState, Creature pet, Creature? target)
    {
        if (target is not { IsAlive: true }) return false;

        if (CustomTargetTypeManager.TryGetCustomTargetType(TargetType, out var customType))
            return customType.ActionPredicate(target, this, pet);

        return false;
    }

    public IReadOnlyList<Creature> GetValidTargets(Creature pet, CombatState combatState)
    {
        return combatState.Creatures
            .Where(target => IsValidTarget(combatState, pet, target))
            .ToList();
    }

    public async Task<bool> TryAct(PlayerChoiceContext choiceContext, Creature pet, Creature? target)
    {
        var combatState = pet.CombatState;
        if (combatState == null || !CanAct(pet, combatState)) return false;

        if (TargetType == TargetType.None)
        {
            await OnAct(choiceContext, pet, null);
            if (CombatManager.Instance.IsInProgress)
                await CombatManager.Instance.CheckWinCondition();
            return true;
        }

        if (TargetType.IsSingleTarget())
        {
            if (!IsValidTarget(combatState, pet, target)) return false;

            await OnAct(choiceContext, pet, target);
            if (CombatManager.Instance.IsInProgress)
                await CombatManager.Instance.CheckWinCondition();
            return true;
        }

        if (GetValidTargets(pet, combatState).Count == 0) return false;

        await OnAct(choiceContext, pet, null);
        if (CombatManager.Instance.IsInProgress)
            await CombatManager.Instance.CheckWinCondition();
        return true;
    }

    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (!AutoRemoveAtTurnEnd || Owner.Side != side || Amount <= 0) return;

        await PowerCmd.Remove(this);
    }

    protected abstract Task OnAct(PlayerChoiceContext choiceContext, Creature actor, Creature? target);
}
