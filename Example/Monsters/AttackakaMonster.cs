using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Powers;
using MinionLib.Example.Powers;
using MinionLib.Models;

namespace MinionLib.Example.Monsters;

public sealed class AttackakaMonster : MinionModel
{
    public override int MinInitialHp => 6;

    public override int MaxInitialHp => 6;

    protected override string VisualsPath => "res://Example/MinionTest/scenes/creature_visuals/pettest_attackaka.tscn";

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        AnimState idle = new("idle_loop", true);
        AnimState cast = new("cast") { NextState = idle };
        AnimState attack = new("attack") { NextState = idle };
        AnimState hurt = new("hurt") { NextState = idle };
        AnimState die = new("die");
        AnimState deadLoop = new("dead_loop", true);
        AnimState revive = new("revive") { NextState = idle };

        idle.AddBranch("Hit", hurt);
        cast.AddBranch("Hit", hurt);
        attack.AddBranch("Hit", hurt);
        hurt.AddBranch("Hit", hurt);
        die.NextState = deadLoop;

        CreatureAnimator animator = new(idle, controller);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Cast", cast);
        animator.AddAnyState("Dead", die);
        animator.AddAnyState("Revive", revive);
        return animator;
    }

    public override async Task OnSummon(Player owner, Creature self, MinionSummonOptions options)
    {
        if (options.MaxHp is decimal maxHp) await CreatureCmd.SetMaxAndCurrentHp(self, maxHp);

        if (options.PrimaryStatAmount is decimal strength && strength > 0m)
            await PowerCmd.Apply<StrengthPower>(self, strength, owner.Creature, options.Source);

        await PowerCmd.Apply<PetAttackerPower>(self, 1m, owner.Creature, options.Source);
    }
}