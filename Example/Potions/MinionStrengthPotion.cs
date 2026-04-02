using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MinionLib.Targeting;

namespace MinionLib.Example.Potions;

[Pool(typeof(SharedPotionPool))]
public sealed class MinionStrengthPotion : CustomPotionModel
{
    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => MinionTargetTypes.AnyMinion;

    public override string PackedImagePath => "res://Example/MinionTest/minionlib-minion_strength_potion.tres";

    public override string PackedOutlinePath =>
        "res://Example/MinionTest/minionlib-minion_strength_potion_outline.tres";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<StrengthPower>(2m)];

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>()];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        await PowerCmd.Apply<StrengthPower>(target, DynamicVars.Strength.BaseValue, Owner.Creature, null);
    }
}