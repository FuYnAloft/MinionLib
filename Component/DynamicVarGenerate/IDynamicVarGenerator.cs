using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MinionLib.Component.DynamicVarGenerate;

public interface IDynamicVarGenerator
{
    static abstract DynamicVar Generate(string name, object[] parameters);
}