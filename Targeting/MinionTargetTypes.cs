using MegaCrit.Sts2.Core.Entities.Cards;
using MinionLib.Targeting.Pets;

namespace MinionLib.Targeting;

public static class MinionTargetTypes
{
    public static readonly TargetType AnyMinion = CustomTargetTypeManager.Register(new AnyMinionTargetType());
    public static readonly TargetType AllMinions = CustomTargetTypeManager.Register(new AllMinionsTargetType());
    public static readonly TargetType Itself = CustomTargetTypeManager.Register(new ItselfTargetType());
    public static readonly TargetType AnyEntity = CustomTargetTypeManager.Register(new AnyEntityTargetType());
}