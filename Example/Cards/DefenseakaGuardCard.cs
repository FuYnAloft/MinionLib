using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.DynamicVars;
using MinionLib.Models;
using MinionLib.Utilities;

namespace MinionLib.Example.Cards;

[Pool(typeof(ColorlessCardPool))]
public sealed class DefenseakaGuardCard()
    : CustomMinionBoundCardModel(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override string CustomPortraitPath => "res://images/packed/card_portraits/beta.png";

    protected override bool ShouldGlowRedInternal => this.ResolveBoundMinion() is not { IsAlive: true };

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Ethereal];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BoundMinionBlockVar("BoundPetBlock",0m, ValueProp.Move)];

    public override bool GainsBlock => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var minion = this.ResolveBoundMinion();
        if (minion is not { IsAlive: true }) return;

        var block = minion.GetPowerAmount<DexterityPower>();
        await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade()
    {
    }
}