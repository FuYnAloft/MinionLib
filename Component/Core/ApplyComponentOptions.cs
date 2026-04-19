namespace MinionLib.Component.Core;

public readonly record struct ApplyComponentOptions(
    bool AllowMerge = true,
    bool UseSubtractiveMerge = false,
    bool IsUpgrade = false,
    Dictionary<string, object?>? Extra = null
);