using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Minion;

namespace MinionLib.DynamicVars;

public sealed class BoundMinionDamageVar(string name, decimal damage, ValueProp props) : DynamicVar(name, damage)
{
    public BoundMinionDamageVar(decimal damage, ValueProp props) : this("BoundMinionDamage", damage, props)
    {
    }

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target,
        bool runGlobalHooks)
    {
        var amount = BaseValue;
        if (!runGlobalHooks || card is not IMinionBoundCard boundCard)
        {
            PreviewValue = amount;
            return;
        }

        var minion = boundCard.ResolveBoundMinion();
        if (minion == null)
        {
            PreviewValue = amount;
            return;
        }

        amount = Hook.ModifyDamage(card.Owner.RunState, card.CombatState, target, minion, amount, props, card,
            ModifyDamageHookType.All, previewMode, out _);
        PreviewValue = amount;
    }
}