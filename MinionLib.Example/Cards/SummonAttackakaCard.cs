using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MinionLib.Commands;
using MinionLib.Example.Minions;
using MinionLib.Minion;

namespace MinionLib.Example.Cards;

public sealed class SummonAttackakaCard() : CardModel(0, CardType.Power, CardRarity.Rare, TargetType.Self, false)
{
    private const string PortraitResourcePath = "res://images/packed/card_portraits/beta.png";

    public override string PortraitPath => PortraitResourcePath;

    public override string BetaPortraitPath => PortraitResourcePath;

    public override IEnumerable<string> AllPortraitPaths => [PortraitResourcePath];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new SummonVar(6m), new PowerVar<StrengthPower>(4m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.SummonDynamic, DynamicVars.Summon)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = await MinionCmd.AddMinion<AttackakaMinion>(choiceContext, Owner, new MinionSummonOptions(
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
