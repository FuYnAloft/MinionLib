using System.Buffers;

namespace MinionLib.Component.Interfaces;

public interface IGeneratedBinarySerializable
{
    void Serialize(ArrayBufferWriter<byte> writer);

    bool Deserialize(ref ReadOnlySpan<byte> reader);
}

