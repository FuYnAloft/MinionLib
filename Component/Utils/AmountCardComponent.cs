using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;

namespace MinionLib.Component.Utils;

public abstract partial class AmountCardComponent : CardComponent
{
    [ComponentState<DynamicVar>] public partial decimal Amount { get; set; }

    public override bool TryMergeWith(ICardComponent incoming, ApplyComponentOptions options, out ICardComponent? merged)
    {
        if (incoming is not AmountCardComponent component)
        {
            merged = null;
            return false;
        }

        Amount += component.Amount;
        if (options.IsUpgrade)
            foreach (var keyValuePair in DynamicVars)
                keyValuePair.Value.SetWasJustUpgraded();
        merged = Amount == 0 ? null : this;
        return true;
    }

    public override bool TrySubtractiveMergeWith(ICardComponent incoming, ApplyComponentOptions options,
        out ICardComponent? merged)
    {
        if (incoming is not AmountCardComponent component)
        {
            merged = null;
            return false;
        }

        Amount -= component.Amount;
        if (options.IsUpgrade)
            foreach (var keyValuePair in DynamicVars)
                keyValuePair.Value.SetWasJustUpgraded();
        merged = Amount == 0 ? null : this;
        return true;
    }
}