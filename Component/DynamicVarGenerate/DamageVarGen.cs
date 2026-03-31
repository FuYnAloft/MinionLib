using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MinionLib.Component.DynamicVarGenerate;

public class DamageVarGen: IDynamicVarGenerator
{
    public static DynamicVar Generate(string name, object[] parameters)
    {
        var prop = parameters is [ValueProp valueProp, ..] ? valueProp : ValueProp.Move;
        return new DamageVar(name, 0, prop);
    }
}