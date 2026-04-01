using HarmonyLib;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MinionLib.Component;

public static class ComponentExtensions
{
    private static readonly AccessTools.FieldRef<DynamicVar, bool> WasJustUpgradedRef = 
        AccessTools.FieldRefAccess<DynamicVar, bool>("<WasJustUpgraded>k__BackingField");

    public static void SetWasJustUpgraded(this DynamicVar var, bool value = true)
    {
        WasJustUpgradedRef(var) = value;
    }
}