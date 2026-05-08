using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MinionLib.Targeting.Utilities;

namespace MinionLib.Targeting;

public static class CustomTargetTypeManager
{
    private const uint CustomTargetTypePrefix = 0x40000000u;
    private const uint CustomTargetTypePayloadMask = 0x3fffffffu;
    private static readonly HashSet<TargetType> RegisteredCustomTypes = [];
    private static readonly Dictionary<string, TargetType> AllocatedTargetTypes = new(StringComparer.Ordinal);
    private static readonly Dictionary<TargetType, string> AllocatedTargetTypeKeys = [];

    private static readonly Dictionary<TargetType, ICustomTargetType>
        CustomTypeDefinitions = new(BuiltInTargetType.All);


    public static TargetType Register(ICustomTargetType customTargetType, string @namespace, string name)
    {
        var key = $"{@namespace}:{name}";
        var targetType = AllocateTargetType(key);
        RegisteredCustomTypes.Add(targetType);
        if (!CustomTypeDefinitions.TryAdd(targetType, customTargetType))
        {
            throw new InvalidOperationException(
                $"TargetType '{targetType}' is already registered for '{AllocatedTargetTypeKeys.GetValueOrDefault(targetType, targetType.ToString())}'.");
        }

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

    private static TargetType AllocateTargetType(string key)
    {
        if (AllocatedTargetTypes.TryGetValue(key, out var existing))
            return existing;

        for (var salt = 0; salt < int.MaxValue; salt++)
        {
            var candidate = CreateTargetTypeCandidate(key, salt);
            if (CustomTypeDefinitions.ContainsKey(candidate) || AllocatedTargetTypeKeys.ContainsKey(candidate))
                continue;

            AllocatedTargetTypes.Add(key, candidate);
            AllocatedTargetTypeKeys.Add(candidate, key);
            return candidate;
        }

        throw new InvalidOperationException($"Unable to allocate a TargetType for '{key}'.");
    }

    private static TargetType CreateTargetTypeCandidate(string key, int salt)
    {
        var hash = ComputeFnv1AHash(salt == 0 ? key : $"{key}#{salt}");
        var value = CustomTargetTypePrefix | (hash & CustomTargetTypePayloadMask);
        return unchecked((TargetType)(int)value);
    }

    private static uint ComputeFnv1AHash(string value)
    {
        const uint offsetBasis = 2166136261u;
        const uint prime = 16777619u;

        var hash = offsetBasis;
        foreach (var b in Encoding.UTF8.GetBytes(value))
        {
            hash ^= b;
            hash *= prime;
        }

        return hash;
    }
}
