using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MinionLib.Minion;
using MinionLib.Targeting;

namespace MinionLib.Example.Cards;

public sealed class PetEmpowerCard()
    : CardModel(0, CardType.Skill, CardRarity.Rare, MinionTargetTypes.AnyMinion, false)
{
    private const string PortraitResourcePath = "res://images/packed/card_portraits/beta.png";

    public override string PortraitPath => PortraitResourcePath;

    public override string BetaPortraitPath => PortraitResourcePath;

    public override IEnumerable<string> AllPortraitPaths => [PortraitResourcePath];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<StrengthPower>(999m), new PowerVar<DexterityPower>(999m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { Monster: MinionModel }) return;

        await PowerCmd.Apply<StrengthPower>(choiceContext, cardPlay.Target, DynamicVars["StrengthPower"].BaseValue, Owner.Creature,
            this);
        await PowerCmd.Apply<DexterityPower>(choiceContext, cardPlay.Target, DynamicVars["DexterityPower"].BaseValue, Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["StrengthPower"].UpgradeValueBy(1000m);
        DynamicVars["DexterityPower"].UpgradeValueBy(1000m);
    }
}
