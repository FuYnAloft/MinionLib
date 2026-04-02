using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace MinionLib.Action.GameActions;

public struct NetExecuteCreatureActionGameAction : INetAction
{
    public uint ActorCombatId;

    public ModelId ActionModelId;

    public uint? TargetCombatId;

    public GameAction ToGameAction(Player player)
    {
        return new ExecuteCreatureActionGameAction(player, ActorCombatId, ActionModelId, TargetCombatId);
    }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteUInt(ActorCombatId, 6);
        writer.WriteModelEntry(ActionModelId);
        writer.WriteBool(TargetCombatId.HasValue);
        if (TargetCombatId.HasValue)
            writer.WriteUInt(TargetCombatId.Value, 6);
    }

    public void Deserialize(PacketReader reader)
    {
        ActorCombatId = reader.ReadUInt(6);
        ActionModelId = reader.ReadModelIdAssumingType<PowerModel>();
        TargetCombatId = reader.ReadBool() ? reader.ReadUInt(6) : null;
    }

    public override string ToString()
    {
        return
            $"{nameof(NetExecuteCreatureActionGameAction)} actor={ActorCombatId} action={ActionModelId.Entry} target={TargetCombatId?.ToString() ?? "null"}";
    }
}