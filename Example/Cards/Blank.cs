using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MinionLib.Component;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MinionLib.Example.Cards;

[RegisterCard(typeof(TokenCardPool))]
public sealed class Blank() : CustomComponentsCardModel(0, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy)
{
    public override string? CustomPortraitPath => "res://images/packed/card_portraits/beta.png";
}
