using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Action;

namespace MinionLib.Targeting;

public interface ICustomTargetType
{
    bool IsSingleTarget { get; }
    bool IsRandomTarget { get; }

    bool IsValidTargetPreview(Creature target);

    bool IsValidTarget(CardModel card, Creature target);
    bool IsValidTarget(PotionModel potion, Creature target);
    bool IsValidTarget(ActionModel action, Creature target);
}