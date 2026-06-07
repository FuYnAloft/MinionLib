using Microsoft.CodeAnalysis;

namespace MinionLib.Generators.Adapters;

[Generator(LanguageNames.CSharp)]
public class BaseLibAdaptersGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 增量检查 BaseLib 是否存在
        var isBaseLibPresent = context.CompilationProvider.Select((compilation, _) =>
            AdapterGeneratorHelper.CheckPresence(compilation, "BaseLib", "BaseLib"));

        // 满足条件时释放对应的适配器代码
        context.RegisterSourceOutput(isBaseLibPresent, (spc, isPresent) =>
        {
            if (isPresent) AdapterGeneratorHelper.EmitEmbeddedSources(spc, "EmbeddedSources.BaseLibAdapters.");
        });
    }
}
