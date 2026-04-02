using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MinionLib.Component.DynamicVarFactories;

public abstract class BlockVarFactory : IDynamicVarFactory
{
    public static DynamicVar Create(string name, object[] parameters)
    {
        var prop = parameters is [ValueProp valueProp, ..] ? valueProp : ValueProp.Move;
        return new BlockVar(name, 0, prop);
    }
}