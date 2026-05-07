using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MinionLib.Example.Powers;
using MinionLib.Minion;

namespace MinionLib.Example.Minions;

public sealed class AttackakaMinion : MinionModel
{
    public override int MinInitialHp => 6;

    public override int MaxInitialHp => 6;

    protected override string VisualsPath => "res://Example/MinionTest/scenes/creature_visuals/pettest_attackaka.tscn";

    public override async Task OnSummon(PlayerChoiceContext? choiceContext, Player owner, Creature self,
        MinionSummonOptions options)
    {
        if (options.MaxHp is decimal maxHp) await CreatureCmd.SetMaxAndCurrentHp(self, maxHp);

        if (choiceContext == null) return;

        if (options.PrimaryStatAmount is decimal strength && strength > 0m)
            await PowerCmd.Apply<StrengthPower>(choiceContext, [self], strength, owner.Creature, options.Source);

        await PowerCmd.Apply<PetAttackerPower>(choiceContext, [self], 1m, owner.Creature, options.Source);
        await PowerCmd.Apply<AttackakaGiftPower>(choiceContext, [self], 1m, owner.Creature, options.Source);
    }
}
