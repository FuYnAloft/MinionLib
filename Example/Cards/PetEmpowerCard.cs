using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MinionLib.Models;
using MinionLib.Targeting;

namespace MinionLib.Example.Cards;

[Pool(typeof(ColorlessCardPool))]
public sealed class PetEmpowerCard() : CustomCardModel(0, CardType.Skill, CardRarity.Rare, MinionTargetTypes.AnyMinion, false)
{
    public override string CustomPortraitPath => "res://images/packed/card_portraits/beta.png";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<StrengthPower>(999m), new PowerVar<DexterityPower>(999m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { Monster: MinionModel }) return;

        await PowerCmd.Apply<StrengthPower>(cardPlay.Target, DynamicVars["StrengthPower"].BaseValue, Owner.Creature,
            this);
        await PowerCmd.Apply<DexterityPower>(cardPlay.Target, DynamicVars["DexterityPower"].BaseValue, Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["StrengthPower"].UpgradeValueBy(1000m);
        DynamicVars["DexterityPower"].UpgradeValueBy(1000m);
    }
}