using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MinionLib.Component.DynamicVarFactories;

public abstract class EnergyVarFactory : IDynamicVarFactory
{
    public static DynamicVar Create(string name, object[] parameters)
    {
        return new EnergyVar(name, 0);
    }
}