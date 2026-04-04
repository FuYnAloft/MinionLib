using MinionLib.Component.Core;

namespace MinionLib.Component.Utils;

public abstract partial class TimingCardComponent: CardComponent
{
    [ComponentState] protected Timing Timing { get; set; }

    protected abstract Task OnTimingPrefix();

    protected abstract Task OnTimingPostfix();
}