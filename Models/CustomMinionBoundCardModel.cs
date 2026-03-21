using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;

namespace MinionLib.Models;

public abstract class CustomMinionBoundCardModel(int baseCost, CardType type, CardRarity rarity, TargetType target)
    : CustomCardModel(baseCost, type, rarity, target, false)
{
    public uint? BoundMinionCombatId { get; set; }

    public string? BoundMinionNameSnapshot { get; set; }

    public Creature? ResolveBoundMinion()
    {
        return CombatState?.GetCreature(BoundMinionCombatId);
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);

        var deadSuffix = new LocString("cards", "bound_minion_dead_suffix").GetFormattedText();
        var minion = ResolveBoundMinion();

        string minionName;
        if (minion != null)
            minionName = minion.Name + (minion.IsAlive ? string.Empty : deadSuffix);
        else if (!string.IsNullOrEmpty(BoundMinionNameSnapshot))
            // If the bound minion no longer resolves by combat id, keep showing the last known name as dead.
            minionName = BoundMinionNameSnapshot + deadSuffix;
        else
            minionName = "???";

        description.Add("BoundMinionName", minionName);
    }
}