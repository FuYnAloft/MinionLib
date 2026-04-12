using MegaCrit.Sts2.Core.Entities.Cards;
using MinionLib.Targeting.Pets;
using static MinionLib.Targeting.CustomTargetTypeManager;

namespace MinionLib.Targeting;

public static class MinionTargetTypes
{
    public static readonly TargetType AnyMinion = Register(new AnyMinionTargetType());
    public static readonly TargetType AllMinions = Register(new AllMinionsTargetType());
    public static readonly TargetType Itself = Register(new ItselfTargetType());
    public static readonly TargetType AnyCreature = Register(new AnyCreatureTargetType());
    public static readonly TargetType AllCreatures = Register(new AllCreaturesTargetType());
    public static readonly TargetType AnyMinionOrOwner = Register(new AnyMinionOrOwnerTargetType());
    public static readonly TargetType Void = Register(new VoidTargetType());
}