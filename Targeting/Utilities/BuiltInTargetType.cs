using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace MinionLib.Targeting.Utilities;

public static class BuiltInTargetType
{
    internal static readonly Dictionary<TargetType, CustomTargetType> All = new()
    {
        [TargetType.None] = new LambdaTargetType(false, _ => false),

        [TargetType.Self] = new LambdaTargetType(false,
            _ => false,
            (target, card) => target.IsAlive && target == card.Owner.Creature,
            (target, potion) => target.IsAlive && target == potion.Owner.Creature,
            (target, _, actor) => target.IsAlive && target == actor.PetOwner?.Creature),

        [TargetType.AnyEnemy] = new LambdaTargetType(true,
            target => target.IsAlive && target.Side == CombatSide.Enemy),

        [TargetType.AllEnemies] = new LambdaTargetType(false,
            target => target.IsAlive && target.Side == CombatSide.Enemy),

        [TargetType.RandomEnemy] = new LambdaTargetType(false,
            target => target.IsAlive && target.Side == CombatSide.Enemy,
            isRandomTarget: true),

        [TargetType.AnyPlayer] = new LambdaTargetType(true,
            target => target.IsAlive && target.IsPlayer),

        [TargetType.AnyAlly] = new LambdaTargetType(true,
            _ => true,
            (target, card) => target.IsAlive && target != card.Owner.Creature,
            (target, potion) => target.IsAlive && target != potion.Owner.Creature,
            (target, _, actor) => target.IsAlive && target != actor.PetOwner?.Creature),

        [TargetType.AllAllies] = new LambdaTargetType(false,
            _ => true,
            (target, card) => target.IsAlive && target != card.Owner.Creature,
            (target, potion) => target.IsAlive && target != potion.Owner.Creature,
            (target, _, actor) => target.IsAlive && target != actor.PetOwner?.Creature),

        [TargetType.TargetedNoCreature] = new LambdaTargetType(true, _ => false),

        [TargetType.Osty] = new LambdaTargetType(true,
            target => target.IsAlive && target.IsPet && target == target.PetOwner?.Osty)
    };

    public static CustomTargetType From(TargetType targetType)
    {
        return All.TryGetValue(targetType, out var result)
            ? result
            : throw
                new ArgumentOutOfRangeException(nameof(targetType), targetType,
                    $"Unsupported TargetType: {targetType}");
    }
}