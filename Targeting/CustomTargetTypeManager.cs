using System.Diagnostics.CodeAnalysis;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;
using MinionLib.Targeting.Utilities;

namespace MinionLib.Targeting;

public static class CustomTargetTypeManager
{
    private static readonly HashSet<TargetType> RegisteredCustomTypes = [];

    private static readonly Dictionary<TargetType, CustomTargetType> CustomTypeDefinitions = new(BuiltInTargetType.All);


    public static TargetType Register(CustomTargetType customTargetType)
    {
        var targetType = (TargetType)CustomEnums.GenerateKey(typeof(TargetType));
        RegisteredCustomTypes.Add(targetType);
        CustomTypeDefinitions.Add(targetType, customTargetType);
        return targetType;
    }

    public static bool IsCustomTargetType(TargetType targetType)
    {
        return RegisteredCustomTypes.Contains(targetType);
    }

    public static bool TryGetCustomTargetType(TargetType targetType,
        [MaybeNullWhen(false)] out CustomTargetType customTargetType, bool includeBuiltin = true)
    {
        if (includeBuiltin || IsCustomTargetType(targetType))
            return CustomTypeDefinitions.TryGetValue(targetType, out customTargetType);
        customTargetType = null;
        return false;
    }
}