using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Powers;
using MinionLib.Example.Powers;
using MinionLib.Minion;

namespace MinionLib.Example.Minions;

public sealed class AttackakaMinion : MinionModel
{
    public override int MinInitialHp => 6;

    public override int MaxInitialHp => 6;

    protected override string VisualsPath => "res://Example/MinionTest/scenes/creature_visuals/pettest_attackaka.tscn";

    public override async Task OnSummon(Player owner, Creature self, MinionSummonOptions options)
    {
        if (options.MaxHp is decimal maxHp) await CreatureCmd.SetMaxAndCurrentHp(self, maxHp);

        if (options.PrimaryStatAmount is decimal strength && strength > 0m)
            await PowerCmd.Apply<StrengthPower>(self, strength, owner.Creature, options.Source);

        await PowerCmd.Apply<PetAttackerPower>(self, 1m, owner.Creature, options.Source);
        await PowerCmd.Apply<AttackakaGiftPower>(self, 1m, owner.Creature, options.Source);
    }
}