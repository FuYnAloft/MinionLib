using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;
using MinionLib.Targeting.Utilities;

namespace MinionLib.Targeting;

public static class CustomTargetTypeManager
{
    private static int _nextRuntimeTargetType = int.MinValue;
    private static readonly HashSet<TargetType> RegisteredCustomTypes = [];

    private static readonly Dictionary<TargetType, ICustomTargetType> CustomTypeDefinitions = new(BuiltInTargetType.All);


    public static TargetType Register(ICustomTargetType customTargetType, FieldInfo field)
    {
        ArgumentNullException.ThrowIfNull(customTargetType);
        ArgumentNullException.ThrowIfNull(field);

        var targetType = (TargetType)CustomEnums.GenerateKey(field);
        RegisterInternal(targetType, customTargetType);
        return targetType;
    }

    public static TargetType Register(ICustomTargetType customTargetType)
    {
        ArgumentNullException.ThrowIfNull(customTargetType);

        while (CustomTypeDefinitions.ContainsKey((TargetType)_nextRuntimeTargetType))
            _nextRuntimeTargetType++;

        var targetType = (TargetType)_nextRuntimeTargetType++;
        RegisterInternal(targetType, customTargetType);
        return targetType;
    }

    private static void RegisterInternal(TargetType targetType, ICustomTargetType customTargetType)
    {
        RegisteredCustomTypes.Add(targetType);
        CustomTypeDefinitions.Add(targetType, customTargetType);
    }

    public static bool IsCustomTargetType(TargetType targetType)
    {
        return RegisteredCustomTypes.Contains(targetType);
    }

    public static bool TryGetCustomTargetType(TargetType targetType,
        [MaybeNullWhen(false)] out ICustomTargetType customTargetType, bool includeBuiltin = true)
    {
        if (includeBuiltin || IsCustomTargetType(targetType))
            return CustomTypeDefinitions.TryGetValue(targetType, out customTargetType);
        customTargetType = null;
        return false;
    }
}
