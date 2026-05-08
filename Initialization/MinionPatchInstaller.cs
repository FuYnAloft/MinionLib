using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;

namespace MinionLib.Initialization;

internal static class MinionPatchInstaller
{
    public static bool Install(Harmony harmony, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(harmony);
        ArgumentNullException.ThrowIfNull(assembly);

        var registrations = AccessTools.GetTypesFromAssembly(assembly)
            .Where(HasHarmonyPatch)
            .Select(CreateRegistration)
            .OrderBy(registration => registration.Group)
            .ThenBy(registration => registration.PatchType.FullName, StringComparer.Ordinal)
            .ToList();

        var summary = new Dictionary<MinionPatchGroup, PatchGroupSummary>();
        foreach (var registration in registrations)
        {
            var groupSummary = GetSummary(summary, registration.Group);
            try
            {
                harmony.CreateClassProcessor(registration.PatchType).Patch();
                groupSummary.SuccessCount++;
            }
            catch (Exception ex)
            {
                groupSummary.FailureCount++;
                if (registration.IsCritical)
                    groupSummary.CriticalFailureCount++;

                Log.Error(
                    $"[{MainFile.ModId}] [Patch] {registration.Group} patch {registration.PatchType.FullName} failed: {ex}");
            }
        }

        var criticalFailures = 0;
        foreach (var (group, groupSummary) in summary.OrderBy(entry => entry.Key))
        {
            criticalFailures += groupSummary.CriticalFailureCount;
            Log.Info(
                $"[{MainFile.ModId}] [Patch] {group}: {groupSummary.SuccessCount} succeeded, {groupSummary.FailureCount} failed");
        }

        if (criticalFailures == 0)
            return true;

        Log.Error($"[{MainFile.ModId}] [Patch] {criticalFailures} critical patch(es) failed.");
        return false;
    }

    private static bool HasHarmonyPatch(Type type)
    {
        return type.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0;
    }

    private static MinionPatchRegistration CreateRegistration(Type patchType)
    {
        var group = ResolveGroup(patchType);
        return new(patchType, group, IsCritical(group));
    }

    private static MinionPatchGroup ResolveGroup(Type patchType)
    {
        var ns = patchType.Namespace ?? string.Empty;

        if (ns.Contains(".Targeting.", StringComparison.Ordinal))
            return MinionPatchGroup.Targeting;
        if (ns.Contains(".Action.", StringComparison.Ordinal))
            return MinionPatchGroup.Actions;
        if (ns.Contains(".Component.", StringComparison.Ordinal))
            return MinionPatchGroup.Components;
        if (ns.Contains(".RightClick.", StringComparison.Ordinal))
            return MinionPatchGroup.RightClick;
        if (ns.Contains(".Powers.", StringComparison.Ordinal))
            return MinionPatchGroup.Guardian;
        if (ns.Contains(".Minion.", StringComparison.Ordinal))
            return MinionPatchGroup.Minions;
        if (ns.Contains(".Utilities.", StringComparison.Ordinal))
            return MinionPatchGroup.Utilities;

        return MinionPatchGroup.Core;
    }

    private static bool IsCritical(MinionPatchGroup group)
    {
        return group is MinionPatchGroup.Core
            or MinionPatchGroup.Minions
            or MinionPatchGroup.Actions
            or MinionPatchGroup.Targeting
            or MinionPatchGroup.Guardian;
    }

    private static PatchGroupSummary GetSummary(
        IDictionary<MinionPatchGroup, PatchGroupSummary> summary,
        MinionPatchGroup group)
    {
        if (summary.TryGetValue(group, out var groupSummary))
            return groupSummary;

        groupSummary = new();
        summary[group] = groupSummary;
        return groupSummary;
    }

    private readonly record struct MinionPatchRegistration(
        Type PatchType,
        MinionPatchGroup Group,
        bool IsCritical);

    private sealed class PatchGroupSummary
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public int CriticalFailureCount { get; set; }
    }
}

internal enum MinionPatchGroup
{
    Core,
    Minions,
    Actions,
    Targeting,
    Guardian,
    Components,
    RightClick,
    Utilities
}
