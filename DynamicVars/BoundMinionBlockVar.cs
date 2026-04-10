using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Minion;

namespace MinionLib.DynamicVars;

public sealed class BoundMinionBlockVar(string name, decimal value, ValueProp props) : DynamicVar(name, value)
{
    public BoundMinionBlockVar(decimal value, ValueProp props) : this("BoundMinionBlock", value, props)
    {
    }

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target,
        bool runGlobalHooks)
    {
        if (card is not IMinionBoundCard boundCard)
        {
            PreviewValue = BaseValue;
            return;
        }

        var minion = boundCard.ResolveBoundMinion();
        if (minion == null)
        {
            PreviewValue = BaseValue;
            return;
        }

        var amount = minion.GetPowerAmount<DexterityPower>() + BaseValue;
        if (runGlobalHooks)
            amount = Hook.ModifyBlock(card.CombatState!, card.Owner.Creature, amount, props, card, null, out _);

        PreviewValue = amount;
    }
}