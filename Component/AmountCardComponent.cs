using MinionLib.Component.Core;
using MinionLib.Component.DynamicVarFactories;
using MinionLib.Component.Interfaces;

namespace MinionLib.Component;

public abstract partial class AmountCardComponent : CardComponent
{
    [ComponentState<DynamicVarFactory>]
    public partial decimal Amount { get; set; }

    public override ICardComponent? MergeWith(ICardComponent other)
    {
        if (other is not AmountCardComponent component) return this;

        Amount += component.Amount;
        return Amount == 0 ? null : this;
    }
}