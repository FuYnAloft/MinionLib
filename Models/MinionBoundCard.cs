using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MinionLib.Models;

public interface IMinionBoundCard
{
    public CardModel AsCardModel => (CardModel)this;

    public uint? BoundMinionCombatId { get; set; }

    public string? BoundMinionNameSnapshot { get; set; }
}

public static class MinionBoundCardExtension
{
    public static Creature? ResolveBoundMinion(this IMinionBoundCard minionBoundCard)
    {
        return minionBoundCard.AsCardModel.CombatState?.GetCreature(minionBoundCard.BoundMinionCombatId);
    }

    public static void BindMinion(this IMinionBoundCard minionBoundCard, Creature minion)
    {
        minionBoundCard.BoundMinionCombatId = minion.CombatId;
        minionBoundCard.BoundMinionNameSnapshot = minion.Name;
    }

    public static void AddBoundNameToDescription(this IMinionBoundCard minionBoundCard, LocString description)
    {
        var deadSuffix = new LocString("cards", "bound_minion_dead_suffix").GetFormattedText();
        var minion = minionBoundCard.ResolveBoundMinion();

        string minionName;
        if (minion != null)
            minionName = minion.Name + (minion.IsAlive ? string.Empty : deadSuffix);
        else if (!string.IsNullOrEmpty(minionBoundCard.BoundMinionNameSnapshot))
            // If the bound minion no longer resolves by combat id, keep showing the last known name as dead.
            minionName = minionBoundCard.BoundMinionNameSnapshot + deadSuffix;
        else
            minionName = "???";

        description.Add("BoundMinionName", minionName);
    }
}

public abstract class CustomMinionBoundCardModel(
    int baseCost,
    CardType type,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true,
    bool autoAdd = true)
    : CustomCardModel(baseCost, type, rarity, target, showInCardLibrary, autoAdd), IMinionBoundCard
{
    public uint? BoundMinionCombatId { get; set; }
    public string? BoundMinionNameSnapshot { get; set; }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        this.AddBoundNameToDescription(description);
    }
}

public abstract class MinionBoundCardModel(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : CardModel(canonicalEnergyCost, type, rarity, targetType,
        shouldShowInCardLibrary), IMinionBoundCard
{
    public uint? BoundMinionCombatId { get; set; }
    public string? BoundMinionNameSnapshot { get; set; }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        this.AddBoundNameToDescription(description);
    }
}