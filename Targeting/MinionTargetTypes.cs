using MegaCrit.Sts2.Core.Entities.Cards;
using MinionLib.Targeting.Pets;

namespace MinionLib.Targeting;

public static class MinionTargetTypes
{
    private static TargetType Register(ICustomTargetType type, string name)
    {
        return CustomTargetTypeManager.Register(type, nameof(MinionLib), name);
    }
    
    public static readonly TargetType AnyMinion = Register(new AnyMinionTargetType(), nameof(AnyMinion));
    public static readonly TargetType AllMinions = Register(new AllMinionsTargetType(), nameof(AllMinions));
    public static readonly TargetType Itself = Register(new ItselfTargetType(), nameof(Itself));
    public static readonly TargetType AnyCreature = Register(new AnyCreatureTargetType(), nameof(AnyCreature));
    public static readonly TargetType AllCreatures = Register(new AllCreaturesTargetType(), nameof(AllCreatures));
    public static readonly TargetType AnyMinionOrOwner = Register(new AnyMinionOrOwnerTargetType(), nameof(AnyMinionOrOwner));
    public static readonly TargetType Void = Register(new VoidTargetType(), nameof(Void));
}