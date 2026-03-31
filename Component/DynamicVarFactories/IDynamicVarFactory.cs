using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MinionLib.Component.DynamicVarFactories;

public interface IDynamicVarFactory
{
    static abstract DynamicVar Create(string name, object[] parameters);
}