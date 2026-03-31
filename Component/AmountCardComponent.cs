using MinionLib.Component.Core;
using MinionLib.Component.DynamicVarFactories;
using MinionLib.Component.Interfaces;

namespace MinionLib.Component;

public abstract class AmountCardComponent : CardComponent
{
    [ComponentState<DynamicVarFactory>]
    public decimal Amount
    {
        get;
        set
        {
            field = value;
            DynamicVars["Amount"].BaseValue = value;
        }
    }

    public override ICardComponent? MergeWith(ICardComponent other)
    {
        if (other is not AmountCardComponent component) return this;

        Amount += component.Amount;
        return Amount == 0 ? null : this;
    }
}