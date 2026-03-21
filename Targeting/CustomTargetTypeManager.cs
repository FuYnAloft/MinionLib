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

    // 原版机制 Patch 应该只用 CustomTargetType，不应该用 BuiltIn 的目标类型应该直接放过让原版游戏处理
    public static bool IsCustomTargetType(TargetType targetType)
    {
        return RegisteredCustomTypes.Contains(targetType);
    }

    // 在部分情况下，可以拿模仿实现的 BuiltIn 目标选择器
    public static bool TryGetCustomTargetType(TargetType targetType,
        [MaybeNullWhen(false)] out CustomTargetType customTargetType)
    {
        return CustomTypeDefinitions.TryGetValue(targetType, out customTargetType);
    }
}