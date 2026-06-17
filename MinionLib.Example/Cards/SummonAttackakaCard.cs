using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MinionLib.Commands;
using MinionLib.Example.Minions;
using MinionLib.Minion;

namespace MinionLib.Example.Cards;

[RegisterCard(typeof(TokenCardPool))]
public sealed class SummonAttackakaCard()
    : ExampleCardTemplate(0, CardType.Power, CardRarity.Rare, TargetType.Self, false)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new SummonVar(6m), new PowerVar<StrengthPower>(4m)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.SummonDynamic, DynamicVars.Summon)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = await MinionCmd.AddMinion<AttackakaMinion>(choiceContext, Owner, new MinionSummonOptions(
            DynamicVars.Summon.BaseValue,
            DynamicVars.Strength.BaseValue,
            Source: this,
            Position: MinionPosition.FrontUpper));
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Summon.UpgradeValueBy(2m);
        DynamicVars.Strength.UpgradeValueBy(1m);
    }
}
