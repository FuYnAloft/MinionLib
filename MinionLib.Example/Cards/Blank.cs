using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace MinionLib.Example.Cards;

[RegisterCard(typeof(TokenCardPool))]
public sealed class Blank() : ExampleComponentsCardTemplate(0, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy);
