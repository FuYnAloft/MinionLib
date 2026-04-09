using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MinionLib.Component.Interfaces;

namespace MinionLib.Component.Core;

public static class SerializationUtils
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.General);

    public static void WriteObjectBlock(ArrayBufferWriter<byte> writer, Action<ArrayBufferWriter<byte>> serializePayload)
    {
        var payloadWriter = new ArrayBufferWriter<byte>();
        serializePayload(payloadWriter);

        WriteInt32(writer, payloadWriter.WrittenCount);
        WriteBytes(writer, payloadWriter.WrittenSpan);
    }

    public static bool TryReadObjectBlock(ref ReadOnlySpan<byte> reader, out ReadOnlySpan<byte> payload)
    {
        payload = default;
        if (!TryReadInt32(ref reader, out var length) || length < 0 || reader.Length < length)
            return false;

        payload = reader[..length];
        reader = reader[length..];
        return true;
    }

    public static bool TrySkipObjectBlock(ref ReadOnlySpan<byte> reader)
    {
        return TryReadObjectBlock(ref reader, out _);
    }

    public static void WriteSerializableBlock(ArrayBufferWriter<byte> writer, IGeneratedBinarySerializable serializable)
    {
        WriteObjectBlock(writer, serializable.Serialize);
    }

    public static bool TryReadSerializableBlock(ref ReadOnlySpan<byte> reader, IGeneratedBinarySerializable serializable)
    {
        if (!TryReadObjectBlock(ref reader, out var payload))
            return false;

        var slice = payload;
        if (!serializable.Deserialize(ref slice))
            return false;

        return slice.IsEmpty;
    }

    public static int[] ToIntArray(ReadOnlySpan<byte> bytes)
    {
        var byteLength = bytes.Length;
        var payloadIntCount = (byteLength + 3) / 4;
        var result = new int[payloadIntCount + 1];
        result[0] = byteLength;

        if (byteLength > 0)
        {
            var destination = MemoryMarshal.AsBytes(result.AsSpan(1));
            bytes.CopyTo(destination);
        }

        return result;
    }

    public static bool TryFromIntArray(int[]? source, out byte[] bytes)
    {
        bytes = [];
        if (source == null || source.Length == 0)
            return true;

        var byteLength = source[0];
        var maxBytes = (source.Length - 1) * 4;
        if (byteLength < 0 || byteLength > maxBytes)
            return false;

        bytes = new byte[byteLength];
        if (byteLength == 0)
            return true;

        var sourceBytes = MemoryMarshal.AsBytes(source.AsSpan(1));
        sourceBytes[..byteLength].CopyTo(bytes);
        return true;
    }

    public static void WriteBoolean(ArrayBufferWriter<byte> writer, bool value)
    {
        WriteByte(writer, value ? (byte)1 : (byte)0);
    }

    public static bool TryReadBoolean(ref ReadOnlySpan<byte> reader, out bool value)
    {
        value = false;
        if (reader.Length < 1)
            return false;

        var raw = reader[0];
        if (raw > 1)
            return false;

        value = raw == 1;
        reader = reader[1..];
        return true;
    }

    public static void WriteByte(ArrayBufferWriter<byte> writer, byte value)
    {
        var span = writer.GetSpan(1);
        span[0] = value;
        writer.Advance(1);
    }

    public static bool TryReadByte(ref ReadOnlySpan<byte> reader, out byte value)
    {
        value = 0;
        if (reader.Length < 1)
            return false;

        value = reader[0];
        reader = reader[1..];
        return true;
    }

    public static void WriteInt16(ArrayBufferWriter<byte> writer, short value)
    {
        var span = writer.GetSpan(2);
        BinaryPrimitives.WriteInt16LittleEndian(span, value);
        writer.Advance(2);
    }

    public static bool TryReadInt16(ref ReadOnlySpan<byte> reader, out short value)
    {
        value = 0;
        if (reader.Length < 2)
            return false;

        value = BinaryPrimitives.ReadInt16LittleEndian(reader);
        reader = reader[2..];
        return true;
    }

    public static void WriteUInt16(ArrayBufferWriter<byte> writer, ushort value)
    {
        var span = writer.GetSpan(2);
        BinaryPrimitives.WriteUInt16LittleEndian(span, value);
        writer.Advance(2);
    }

    public static bool TryReadUInt16(ref ReadOnlySpan<byte> reader, out ushort value)
    {
        value = 0;
        if (reader.Length < 2)
            return false;

        value = BinaryPrimitives.ReadUInt16LittleEndian(reader);
        reader = reader[2..];
        return true;
    }

    public static void WriteInt32(ArrayBufferWriter<byte> writer, int value)
    {
        var span = writer.GetSpan(4);
        BinaryPrimitives.WriteInt32LittleEndian(span, value);
        writer.Advance(4);
    }

    public static bool TryReadInt32(ref ReadOnlySpan<byte> reader, out int value)
    {
        value = 0;
        if (reader.Length < 4)
            return false;

        value = BinaryPrimitives.ReadInt32LittleEndian(reader);
        reader = reader[4..];
        return true;
    }

    public static void WriteUInt32(ArrayBufferWriter<byte> writer, uint value)
    {
        var span = writer.GetSpan(4);
        BinaryPrimitives.WriteUInt32LittleEndian(span, value);
        writer.Advance(4);
    }

    public static bool TryReadUInt32(ref ReadOnlySpan<byte> reader, out uint value)
    {
        value = 0;
        if (reader.Length < 4)
            return false;

        value = BinaryPrimitives.ReadUInt32LittleEndian(reader);
        reader = reader[4..];
        return true;
    }

    public static void WriteInt64(ArrayBufferWriter<byte> writer, long value)
    {
        var span = writer.GetSpan(8);
        BinaryPrimitives.WriteInt64LittleEndian(span, value);
        writer.Advance(8);
    }

    public static bool TryReadInt64(ref ReadOnlySpan<byte> reader, out long value)
    {
        value = 0;
        if (reader.Length < 8)
            return false;

        value = BinaryPrimitives.ReadInt64LittleEndian(reader);
        reader = reader[8..];
        return true;
    }

    public static void WriteUInt64(ArrayBufferWriter<byte> writer, ulong value)
    {
        var span = writer.GetSpan(8);
        BinaryPrimitives.WriteUInt64LittleEndian(span, value);
        writer.Advance(8);
    }

    public static bool TryReadUInt64(ref ReadOnlySpan<byte> reader, out ulong value)
    {
        value = 0;
        if (reader.Length < 8)
            return false;

        value = BinaryPrimitives.ReadUInt64LittleEndian(reader);
        reader = reader[8..];
        return true;
    }

    public static void WriteSingle(ArrayBufferWriter<byte> writer, float value)
    {
        WriteInt32(writer, BitConverter.SingleToInt32Bits(value));
    }

    public static bool TryReadSingle(ref ReadOnlySpan<byte> reader, out float value)
    {
        value = 0;
        if (!TryReadInt32(ref reader, out var raw))
            return false;

        value = BitConverter.Int32BitsToSingle(raw);
        return true;
    }

    public static void WriteDouble(ArrayBufferWriter<byte> writer, double value)
    {
        WriteInt64(writer, BitConverter.DoubleToInt64Bits(value));
    }

    public static bool TryReadDouble(ref ReadOnlySpan<byte> reader, out double value)
    {
        value = 0;
        if (!TryReadInt64(ref reader, out var raw))
            return false;

        value = BitConverter.Int64BitsToDouble(raw);
        return true;
    }

    public static void WriteDecimal(ArrayBufferWriter<byte> writer, decimal value)
    {
        var bits = decimal.GetBits(value);
        WriteInt32(writer, bits[0]);
        WriteInt32(writer, bits[1]);
        WriteInt32(writer, bits[2]);
        WriteInt32(writer, bits[3]);
    }

    public static bool TryReadDecimal(ref ReadOnlySpan<byte> reader, out decimal value)
    {
        value = 0;
        if (!TryReadInt32(ref reader, out var b0)
            || !TryReadInt32(ref reader, out var b1)
            || !TryReadInt32(ref reader, out var b2)
            || !TryReadInt32(ref reader, out var b3))
            return false;

        value = new decimal([b0, b1, b2, b3]);
        return true;
    }

    public static void WriteString(ArrayBufferWriter<byte> writer, string? value)
    {
        if (value == null)
        {
            WriteInt32(writer, -1);
            return;
        }

        var byteCount = Encoding.UTF8.GetByteCount(value);
        WriteInt32(writer, byteCount);
        if (byteCount == 0)
            return;

        var span = writer.GetSpan(byteCount);
        var written = Encoding.UTF8.GetBytes(value, span);
        writer.Advance(written);
    }

    public static bool TryReadString(ref ReadOnlySpan<byte> reader, out string? value)
    {
        value = null;
        if (!TryReadInt32(ref reader, out var length))
            return false;

        if (length < -1 || reader.Length < length)
            return false;

        if (length == -1)
            return true;

        value = length == 0 ? string.Empty : Encoding.UTF8.GetString(reader[..length]);
        reader = reader[length..];
        return true;
    }

    public static void WriteJson<T>(ArrayBufferWriter<byte> writer, T value)
    {
        WriteString(writer, JsonSerializer.Serialize(value, JsonOptions));
    }

    public static bool TryReadJson<T>(ref ReadOnlySpan<byte> reader, out T value)
    {
        value = default!;
        if (!TryReadString(ref reader, out var json) || json == null)
            return false;

        try
        {
            var deserialized = JsonSerializer.Deserialize<T>(json, JsonOptions);
            value = deserialized!;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void WriteBytes(ArrayBufferWriter<byte> writer, ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 0)
            return;

        bytes.CopyTo(writer.GetSpan(bytes.Length));
        writer.Advance(bytes.Length);
    }

    public static void WriteIPacketSerializable<T>(ArrayBufferWriter<byte> writer, T value)
        where T : IPacketSerializable, new()
    {
        var packetWriter = new PacketWriter();
        value.Serialize(packetWriter);
        packetWriter.ZeroByteRemainder();

        WriteInt32(writer, packetWriter.BytePosition);
        WriteBytes(writer, packetWriter.Buffer.AsSpan(0, packetWriter.BytePosition));
    }

    public static bool TryReadIPacketSerializable<T>(ref ReadOnlySpan<byte> reader, out T value)
        where T : IPacketSerializable, new()
    {
        value = default!;

        if (!TryReadInt32(ref reader, out var length) || length < 0 || reader.Length < length)
            return false;

        var buffer = reader[..length];
        reader = reader[length..];

        try
        {
            var packetReader = new PacketReader();
            packetReader.Reset(buffer.ToArray());

            value = new T();
            value.Deserialize(packetReader);

            return true;
        }
        catch
        {
            return false;
        }
    }
}