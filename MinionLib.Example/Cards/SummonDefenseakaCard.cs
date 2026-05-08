using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MinionLib.Commands;
using MinionLib.Example.Minions;
using MinionLib.Minion;

namespace MinionLib.Example.Cards;

public sealed class SummonDefenseakaCard() : CardModel(0, CardType.Power, CardRarity.Rare, TargetType.Self, false)
{
    private const string PortraitResourcePath = "res://images/packed/card_portraits/beta.png";

    public override string PortraitPath => PortraitResourcePath;

    public override string BetaPortraitPath => PortraitResourcePath;

    public override IEnumerable<string> AllPortraitPaths => [PortraitResourcePath];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new SummonVar(6m), new PowerVar<DexterityPower>(4m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.SummonDynamic, DynamicVars.Summon)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var pet = await MinionCmd.AddMinion<DefenseakaMinion>(choiceContext, Owner, new MinionSummonOptions(
            DynamicVars.Summon.BaseValue,
            DynamicVars["DexterityPower"].BaseValue,
            Source: this));

        // Mirror Osty's shield visualization: defender minion displays owner's block ring/status.
        NCombatRoom.Instance?.GetCreatureNode(pet)?.TrackBlockStatus(Owner.Creature);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Summon.UpgradeValueBy(2m);
        DynamicVars["DexterityPower"].UpgradeValueBy(1m);
    }
}
