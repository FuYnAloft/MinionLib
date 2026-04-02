using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MinionLib.Component;

public abstract partial class AmountCardComponent : CardComponent
{
    [ComponentState<DynamicVar>] public partial decimal Amount { get; set; }
    [ComponentState] private decimal Aaa { get; set; }

    public override ICardComponent? MergeWith(ICardComponent incoming)
    {
        if (incoming is not AmountCardComponent component) return this;

        Amount += component.Amount;
        return Amount == 0 ? null : this;
    }
}