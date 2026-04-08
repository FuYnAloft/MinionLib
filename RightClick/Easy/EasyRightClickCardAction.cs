using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MinionLib.RightClick.Easy;

public class EasyRightClickCardAction : GameAction
{
    public Player Player { get; }
    public override ulong OwnerId => Player.NetId;
    public override GameActionType ActionType => GameActionType.CombatPlayPhaseOnly;

    private CardModel? _card;

    public NetCombatCard NetCombatCard { get; }
    public ModelId CardModelId { get; }
    public RightClickContext.Payload Extra { get; }

    public EasyRightClickCardAction(RightClickContext context)
    {
        if (context.Model is not CardModel cardModel)
            throw new ArgumentException("Context model must be a CardModel.", nameof(context));
        Player = context.Player;
        _card = cardModel;
        NetCombatCard = NetCombatCard.FromModel(cardModel);
        CardModelId = cardModel.Id;
        Extra = context.Extra;
    }

    public EasyRightClickCardAction(
        Player player,
        NetCombatCard netCombatCard,
        ModelId cardModelId,
        RightClickContext.Payload extra)
    {
        Player = player;
        NetCombatCard = netCombatCard;
        CardModelId = cardModelId;
        Extra = extra;
    }


    protected override async Task ExecuteAction()
    {
        _card = NetCombatCard.ToCardModel();
        if (_card is not IEasyRightClickableCard rightClickableCard) return;
        if (_card.Pile?.Type != PileType.Hand) return;

        var choiceContext = new GameActionPlayerChoiceContext(this);
        var clickContent = new RightClickContext(Player, _card, Extra);
        await rightClickableCard.OnRightClick(choiceContext, clickContent);
        _card.InvokeExecutionFinished();
    }

    public override INetAction ToNetAction()
    {
        return new NetEasyRightClickCardAction()
        {
            Card = NetCombatCard,
            ModelId = CardModelId,
            Extra = Extra
        };
    }
}