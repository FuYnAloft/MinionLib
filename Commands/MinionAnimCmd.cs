using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MinionLib.Utilities;
using static MinionLib.Utilities.MinionPositioningHelper;

namespace MinionLib.Commands;

public static class MinionAnimCmd
{
    public static async Task Rearrange(bool animated = true)
    {
        var room = NCombatRoom.Instance;
        if (room == null) return;

        var dst = CalculateMinionPositions(room);
        if (animated)
            await AnimatedMove(dst);
        else
            InstantMove(dst);
    }

    public static void InstantMove(IReadOnlyList<MinionNodePosition> nodePositions)
    {
        foreach (var (node, position) in nodePositions)
            if (GodotObject.IsInstanceValid(node))
                node.Position = position;
    }
    
    
    private static Tween? _activeTween;
    public static async Task AnimatedMove(IReadOnlyList<MinionNodePosition> nodePositions)
    {
        var room = NCombatRoom.Instance;
        if (room == null) return;

        if (_activeTween != null && _activeTween.IsValid())
        {
            _activeTween.EmitSignal("finished");
            _activeTween.Kill();
        }
        
        var tween = room.CreateTween();
        tween.SetParallel();
        foreach (var (index, (node, position)) in nodePositions.Index())
            if (GodotObject.IsInstanceValid(node))
            {
                tween.TweenProperty(node, "position", position, 0.25f)
                    .SetTrans(Tween.TransitionType.Quad)
                    .SetEase(Tween.EaseType.Out)
                    .SetDelay(index * 0.02f);
            }
        _activeTween = tween;
        
        await room.ToSignal(tween, Tween.SignalName.Finished);
    }

    public static async Task PlayBumpAttackAsync(Creature attacker, Creature target, Action? onHit = null)
    {
        var room = NCombatRoom.Instance;
        var attackerNode = room?.GetCreatureNode(attacker);
        var targetNode = room?.GetCreatureNode(target);

        if (attackerNode == null || targetNode == null) return;

        var sprite = attackerNode.Visuals.GetCurrentBody();
        if (!GodotObject.IsInstanceValid(sprite)) return;

        var start = sprite.GlobalPosition;
        var hitTarget = targetNode.Visuals.VfxSpawnPosition.GlobalPosition;
        var direction = (hitTarget - start).Normalized();

        if (direction == Vector2.Zero) direction = Vector2.Right;

        var pullBackPos = start - direction * 20f;
        var impact = hitTarget - direction * 70f;

        var tween = sprite.CreateTween();

        tween.TweenProperty(sprite, "global_position", pullBackPos, 0.15f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);

        tween.TweenProperty(sprite, "global_position", impact, 0.10f)
            .SetTrans(Tween.TransitionType.Expo)
            .SetEase(Tween.EaseType.In);

        if (onHit != null)
        {
            tween.TweenCallback(Callable.From(onHit));
        }

        tween.TweenProperty(sprite, "global_position", start, 0.3f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);

        if (tween.IsValid())
            await sprite.ToSignal(tween, Tween.SignalName.Finished);
    }
}