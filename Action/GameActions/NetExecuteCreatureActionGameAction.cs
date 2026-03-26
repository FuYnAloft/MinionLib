using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace MinionLib.Action.GameActions;

public struct NetExecuteCreatureActionGameAction : INetAction, IPacketSerializable
{
    public uint actorCombatId;

    public ModelId actionModelId;

    public uint? targetCombatId;

    public GameAction ToGameAction(Player player)
    {
        return new ExecuteCreatureActionGameAction(player, actorCombatId, actionModelId, targetCombatId);
    }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteUInt(actorCombatId, 6);
        writer.WriteModelEntry(actionModelId);
        writer.WriteBool(targetCombatId.HasValue);
        if (targetCombatId.HasValue)
            writer.WriteUInt(targetCombatId.Value, 6);
    }

    public void Deserialize(PacketReader reader)
    {
        actorCombatId = reader.ReadUInt(6);
        actionModelId = reader.ReadModelIdAssumingType<PowerModel>();
        targetCombatId = reader.ReadBool() ? reader.ReadUInt(6) : null;
    }

    public override string ToString()
    {
        return $"{nameof(NetExecuteCreatureActionGameAction)} actor={actorCombatId} action={actionModelId.Entry} target={targetCombatId?.ToString() ?? "null"}";
    }
}


