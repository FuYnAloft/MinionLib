using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace MinionLib.RightClick.Easy;

public struct NetEasyRightClickCardAction : INetAction
{
    public NetCombatCard Card;
    public ModelId ModelId;
    public RightClickContext.Payload Extra;

    public void Serialize(PacketWriter writer)
    {
        writer.Write(Card);
        writer.WriteModelEntry(ModelId);
        writer.Write(Extra);
    }

    public void Deserialize(PacketReader reader)
    {
        Card = reader.Read<NetCombatCard>();
        ModelId = reader.ReadModelIdAssumingType<CardModel>();
        Extra = reader.Read<RightClickContext.Payload>();
    }

    public GameAction ToGameAction(Player player)
    {
        return new EasyRightClickCardAction(player, Card, ModelId, Extra);
    }
}