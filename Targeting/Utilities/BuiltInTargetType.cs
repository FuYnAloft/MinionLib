using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace MinionLib.Targeting.Utilities;

public static class BuiltInTargetType
{
    internal static readonly Dictionary<TargetType, ICustomTargetType> All = new()
    {
        [TargetType.None] = new LambdaTargetType(false, _ => false),

        [TargetType.Self] = new LambdaTargetType(false,
            _ => false,
            (card, target) => target.IsAlive && target == card.Owner.Creature,
            (potion, target) => target.IsAlive && target == potion.Owner.Creature,
            (action, target) => target is { IsAlive: true, IsPlayer: true } &&
                                (target.Player == action.Owner.PetOwner || target == action.Owner)),

        [TargetType.AnyEnemy] = new LambdaTargetType(true,
            target => target is { IsAlive: true, Side: CombatSide.Enemy }),

        [TargetType.AllEnemies] = new LambdaTargetType(false,
            target => target is { IsAlive: true, Side: CombatSide.Enemy }),

        [TargetType.RandomEnemy] = new LambdaTargetType(false,
            target => target is { IsAlive: true, Side: CombatSide.Enemy }),

        [TargetType.AnyPlayer] = new LambdaTargetType(true,
            target => target is { IsAlive: true, IsPlayer: true }),

        [TargetType.AnyAlly] = new LambdaTargetType(true,
            _ => true,
            (card, target) => target.IsAlive && target != card.Owner.Creature,
            (potion, target) => target.IsAlive && target != potion.Owner.Creature,
            (action, target) => target is { IsAlive: true, IsPlayer: true } &&
                                !(target.Player == action.Owner.PetOwner || target == action.Owner)),

        [TargetType.AllAllies] = new LambdaTargetType(false,
            _ => true,
            (card, target) => target.IsAlive && target != card.Owner.Creature,
            (potion, target) => target.IsAlive && target != potion.Owner.Creature,
            (action, target) => target is { IsAlive: true, IsPlayer: true } &&
                                !(target.Player == action.Owner.PetOwner || target == action.Owner)),

        [TargetType.TargetedNoCreature] = new LambdaTargetType(true, _ => false),

        [TargetType.Osty] = new LambdaTargetType(true,
            target => target is { IsAlive: true, IsPet: true } && target == target.PetOwner?.Osty)
    };

    public static ICustomTargetType From(TargetType targetType)
    {
        return All.TryGetValue(targetType, out var result)
            ? result
            : throw
                new ArgumentOutOfRangeException(nameof(targetType), targetType,
                    $"Unsupported TargetType: {targetType}");
    }
}