using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MinionLib.Commands;
using MinionLib.Example.Monsters;
using MinionLib.Models;
using MinionLib.Utilities;

namespace MinionLib.Example.Cards;

[Pool(typeof(IroncladCardPool))]
public sealed class SummonDefenseakaCard() : CustomCardModel(0, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new SummonVar(6m), new PowerVar<DexterityPower>(4m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.SummonDynamic, DynamicVars.Summon)];

    public override string? CustomPortraitPath => "res://Example/MinionTest/image.png";

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var pet = await MinionCmd.AddMinion<DefenseakaMonster>(Owner, new MinionSummonOptions(
            DynamicVars.Summon.BaseValue,
            DynamicVars["DexterityPower"].BaseValue,
            Source: this));

        using (var accessor = new PetsOrderAccessor(Owner))
        {
            var pets = accessor.Pets!;
            var last = pets.Last();
            pets.RemoveAt(pets.Count - 1);
            pets.Insert(0, last);
        }

        // Mirror Osty's shield visualization: defender minion displays owner's block ring/status.
        NCombatRoom.Instance?.GetCreatureNode(pet)?.TrackBlockStatus(Owner.Creature);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Summon.UpgradeValueBy(2m);
        DynamicVars["DexterityPower"].UpgradeValueBy(1m);
    }
}