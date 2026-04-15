using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;
using MinionLib.Targeting.Utilities;

namespace MinionLib.Targeting;

public static class CustomTargetTypeManager
{
    private static readonly HashSet<TargetType> RegisteredCustomTypes = [];

    private static readonly Dictionary<TargetType, ICustomTargetType>
        CustomTypeDefinitions = new(BuiltInTargetType.All);


    public static TargetType Register(ICustomTargetType customTargetType, string @namespace, string name)
    {
        var targetType = CustomEnums.GenerateKey<TargetType>(@namespace, name);
        RegisteredCustomTypes.Add(targetType);
        CustomTypeDefinitions.Add(targetType, customTargetType);
        return targetType;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static TargetType Register(ICustomTargetType customTargetType,
        [CallerArgumentExpression("customTargetType")]
        string expr = "")
    {
        var stackTrace = new StackTrace();
        var ns = stackTrace.GetFrame(1)?.GetMethod()?.DeclaringType?.FullName?.Split('.').First()
                 ?? throw new InvalidOperationException(
                     "Unable to automatically retrieve the namespace. Please specify it manually.");
        var name = new string(expr.Where(c => !char.IsWhiteSpace(c)).ToArray());
        return Register(customTargetType, ns, name);
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