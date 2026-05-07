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
using MinionLib.Utilities;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MinionLib.Example.Potions;

[RegisterPotion(typeof(SharedPotionPool))]
public sealed class MinionStrengthPotion : ModPotionTemplate
{
    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => MinionTargetTypes.AnyMinion;

    public override string CustomImagePath => "res://Example/MinionTest/minionlib-minion_strength_potion.tres";

    public override string CustomOutlinePath =>
        "res://Example/MinionTest/minionlib-minion_strength_potion_outline.tres";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<StrengthPower>(2m)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipHelper.FromPower<StrengthPower>()];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        await PowerCmd.Apply<StrengthPower>(choiceContext, [target], DynamicVars.Strength.BaseValue, Owner.Creature,
            null);
    }
}
