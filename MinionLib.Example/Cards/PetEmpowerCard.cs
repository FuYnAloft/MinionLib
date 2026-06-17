using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MinionLib.Minion;
using MinionLib.Targeting;

namespace MinionLib.Example.Cards;

[RegisterCard(typeof(TokenCardPool))]
public sealed class PetEmpowerCard()
    : ExampleCardTemplate(0, CardType.Skill, CardRarity.Rare, MinionTargetTypes.AnyMinion, false)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<StrengthPower>(999m), new PowerVar<DexterityPower>(999m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { Monster: MinionModel }) return;

        await PowerCmd.Apply<StrengthPower>(choiceContext, cardPlay.Target, DynamicVars.Strength.BaseValue,
            Owner.Creature, this);
        await PowerCmd.Apply<DexterityPower>(choiceContext, cardPlay.Target, DynamicVars.Dexterity.BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Strength.UpgradeValueBy(1000m);
        DynamicVars.Dexterity.UpgradeValueBy(1000m);
    }
}
