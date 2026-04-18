using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;

namespace MinionLib.Utilities.BetterExtraArgs;

public interface IBetterAddExtraArgsCard
{
    void BetterAddExtraArgsToDescription(
        LocString description,
        PileType pileType,
        DescriptionPreviewType previewType,
        Creature? target = null);
}