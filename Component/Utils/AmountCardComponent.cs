using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;

namespace MinionLib.Component.Utils;

public abstract partial class AmountCardComponent : CardComponent
{
    [ComponentState<DynamicVar>] public partial decimal Amount { get; set; }

    public override bool TryMergeWith(ICardComponent incoming, out ICardComponent? merged)
    {
        if (incoming is not AmountCardComponent component)
        {
            merged = null;
            return false;
        }

        Amount += component.Amount;
        merged = Amount == 0 ? null : this;
        return true;
    }

    public override bool TrySubtractiveMergeWith(ICardComponent incoming, out ICardComponent? merged)
    {
        if (incoming is not AmountCardComponent component)
        {
            merged = null;
            return false;
        }

        Amount -= component.Amount;
        merged = Amount == 0 ? null : this;
        return true;
    }
}