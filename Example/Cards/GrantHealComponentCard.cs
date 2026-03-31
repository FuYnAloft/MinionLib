using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MinionLib.Component;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using MinionLib.Example.Components;

namespace MinionLib.Example.Cards;

[Pool(typeof(ColorlessCardPool))]
public sealed class GrantHealComponentCard() : ComponentsCardModel(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override string CustomPortraitPath => "res://images/packed/card_portraits/beta.png";

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var selectedCard = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            c => c is IComponentsCardModel,
            this)).FirstOrDefault();

        if (selectedCard is not IComponentsCardModel componentsCard) return;

        componentsCard.AddComponent(new HealOwnerComponent { Amount = 3 });
    }
}

