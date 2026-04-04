using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Component.Core;


namespace MinionLib.Component.Utils;

public abstract partial class TimingCardComponent(params Timing[] timings) : CardComponent
{
    [ComponentState] protected Timing[] Timings { get; set; } = timings;

    protected virtual Task OnTimingPrefix(OnTimingContext context)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnTimingPostfix(OnTimingContext context)
    {
        return Task.CompletedTask;
    }
}

// ReSharper disable PreferConcreteValueOverDefault
#pragma warning disable CS8625
public record OnTimingContext(
    Timing Timing,
    int ActIndex = default,
    OrbModel Orb = default,
    Creature Osty = default,
    ActMap Map = default,
    bool WasRemovalPrevented = default,
    MerchantEntry ItemPurchased = default,
    PowerModel Power = default,
    CardPilePosition Position = default,
    AutoPlayType Type = default,
    IEnumerable<Creature> Targets = default,
    Player Shuffler = default,
    IReadOnlyList<Reward> Rewards = default,
    PlayerChoiceContext Context = default,
    AbstractModel Preventer = default,
    Player Player = default,
    PileType OldPileType = default,
    bool FromHandDraw = default,
    CardModel? CardSource = default,
    PileType PileType = default,
    PotionModel Potion = default,
    Creature Blocker = default,
    Creature? Dealer = default,
    decimal Delta = default,
    CardModel Card = default,
    CombatSide Side = default,
    bool IsMimicked = default,
    bool AddedByPlayer = default,
    ValueProp Props = default,
    PlayerChoiceContext ChoiceContext = default,
    decimal ModifiedAmount = default,
    DamageResult Result = default,
    bool CausedByEthereal = default,
    float DeathAnimLength = default,
    CardPlay? CardPlay = default,
    Player Forger = default,
    Creature Creature = default,
    AbstractRoom Room = default,
    AttackCommand Command = default,
    decimal Amount = default,
    Reward Reward = default,
    Player Spender = default,
    Player Gainer = default,
    CombatState CombatState = default,
    Creature? Target = default,
    Creature? Applier = default,
    AbstractModel? Source = default,
    Player Summoner = default,
    int GoldSpent = default,
    IReadOnlyList<Creature> Creatures = default
);