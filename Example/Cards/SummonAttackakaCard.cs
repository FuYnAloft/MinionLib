using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MinionLib.Commands;
using MinionLib.Example.Minions;
using MinionLib.Minion;
using MinionLib.Models;

namespace MinionLib.Example.Cards;

[Pool(typeof(ColorlessCardPool))]
public sealed class SummonAttackakaCard() : CustomCardModel(0, CardType.Power, CardRarity.Rare, TargetType.Self, false)
{
    public override string CustomPortraitPath => "res://images/packed/card_portraits/beta.png";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new SummonVar(6m), new PowerVar<StrengthPower>(4m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.SummonDynamic, DynamicVars.Summon)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = await MinionCmd.AddMinion<AttackakaMinion>(Owner, new MinionSummonOptions(
            DynamicVars.Summon.BaseValue,
            DynamicVars["StrengthPower"].BaseValue,
            Source: this,
            Position: MinionPosition.FrontUpper));
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Summon.UpgradeValueBy(2m);
        DynamicVars["StrengthPower"].UpgradeValueBy(1m);
    }
}