using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace MinionLib.RightClick;

public record RightClickContext(Player Player, AbstractModel Model, RightClickContext.Payload Extra = default)
{
    public struct Payload : IPacketSerializable
    {
        public bool IsController { get; private set; }

        public string? Meta { get; private set; }

        public Payload(bool isController = false, string? meta = null)
        {
            IsController = isController;
            Meta = meta;
        }

        public void Serialize(PacketWriter writer)
        {
            writer.WriteBool(IsController);
            writer.WriteBool(Meta != null);
            if (Meta != null)
                writer.WriteString(Meta);
        }

        public void Deserialize(PacketReader reader)
        {
            IsController = reader.ReadBool();
            Meta = reader.ReadBool() ? reader.ReadString() : null;
        }
    }
}

