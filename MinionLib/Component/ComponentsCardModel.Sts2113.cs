using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MinionLib.Component;

public abstract partial class ComponentsCardModel
{
    // Preserve the existing component callbacks and phase ordering when the game
    // invokes the STS2 0.113 hooks. The new source/card arguments are not part of
    // the component API, so existing components do not need to be recompiled.
#pragma warning disable CS0618
    public sealed override Task AfterDeath(PlayerChoiceContext context, Creature creature,
        CardModel? source, bool wasRemovalPrevented, float deathAnimLength) =>
        AfterDeath(context, creature, wasRemovalPrevented, deathAnimLength);

    protected sealed override void AfterThisWasTransformedIntoInternal(CardModel card) => AfterTransformedFrom();
    public sealed override void AfterThisWasTransformedFrom(CardModel card) => AfterTransformedTo();
#pragma warning restore CS0618
}
