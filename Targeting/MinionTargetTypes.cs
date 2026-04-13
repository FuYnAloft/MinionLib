using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MinionLib.Targeting.Pets;
using static MinionLib.Targeting.CustomTargetTypeManager;

namespace MinionLib.Targeting;

public static class MinionTargetTypes
{
    public static readonly TargetType AnyMinion = Register(new AnyMinionTargetType(), GetField(nameof(AnyMinion)));
    public static readonly TargetType AllMinions = Register(new AllMinionsTargetType(), GetField(nameof(AllMinions)));
    public static readonly TargetType Itself = Register(new ItselfTargetType(), GetField(nameof(Itself)));
    public static readonly TargetType AnyCreature = Register(new AnyCreatureTargetType(), GetField(nameof(AnyCreature)));
    public static readonly TargetType AllCreatures = Register(new AllCreaturesTargetType(), GetField(nameof(AllCreatures)));
    public static readonly TargetType AnyMinionOrOwner = Register(new AnyMinionOrOwnerTargetType(), GetField(nameof(AnyMinionOrOwner)));
    public static readonly TargetType Void = Register(new VoidTargetType(), GetField(nameof(Void)));

    private static FieldInfo GetField(string name)
    {
        return typeof(MinionTargetTypes).GetField(name, BindingFlags.Public | BindingFlags.Static)
               ?? throw new MissingFieldException(typeof(MinionTargetTypes).FullName, name);
    }
}
