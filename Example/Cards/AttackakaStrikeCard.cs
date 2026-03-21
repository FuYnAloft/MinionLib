using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Commands;
using MinionLib.DynamicVars;
using MinionLib.Models;

namespace MinionLib.Example.Cards;

[Pool(typeof(ColorlessCardPool))]
public sealed class AttackakaStrikeCard()
    : CustomMinionBoundCardModel(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
{
    protected override bool ShouldGlowRedInternal => ResolveBoundMinion() is not { IsAlive: true };

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Ethereal];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BoundMinionDamageVar("BoundPetDamage", 0m, ValueProp.Move)];

    public override string? CustomPortraitPath => "res://Example/MinionTest/image.png";

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        var minion = ResolveBoundMinion();
        if (minion is not { IsAlive: true }) return;

        await MinionAnimCmd.PlayBumpAttackAsync(minion, cardPlay.Target);
        await CreatureCmd.Damage(choiceContext, cardPlay.Target, DynamicVars["BoundPetDamage"].BaseValue,
            ValueProp.Move, minion, this);
    }

    protected override void OnUpgrade()
    {
    }
}