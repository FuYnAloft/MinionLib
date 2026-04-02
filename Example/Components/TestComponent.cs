using System.Buffers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MinionLib.Component;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;

namespace MinionLib.Example.Components;

public sealed partial class TestComponent : CardComponent
{
    [ComponentState] public Creature MyClass { get; set; }
}


public sealed partial class MyClass : IGeneratedBinarySerializable
{
    public void Serialize(ArrayBufferWriter<byte> writer)
    {
    }

    public bool Deserialize(ref ReadOnlySpan<byte> reader) => true;

    public MyClass(int arg)
    {
        
    }
}