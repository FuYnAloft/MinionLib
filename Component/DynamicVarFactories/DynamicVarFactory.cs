using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MinionLib.Component.DynamicVarFactories;

public abstract class DynamicVarFactory : IDynamicVarFactory
{
    public static DynamicVar Create(string name, object[] parameters)
    {
        return new DynamicVar(name, 0);
    }
}