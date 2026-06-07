using Microsoft.CodeAnalysis;

namespace MinionLib.Generators.Adapters;

[Generator(LanguageNames.CSharp)]
public class RitsuLibAdaptersGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 增量检查 RitsuLib 是否存在
        var isRitsuPresent = context.CompilationProvider.Select((compilation, _) =>
            AdapterGeneratorHelper.CheckPresence(compilation, "STS2-RitsuLib", "STS2RitsuLib"));

        // 满足条件时释放对应的适配器代码
        context.RegisterSourceOutput(isRitsuPresent, (spc, isPresent) =>
        {
            if (isPresent) AdapterGeneratorHelper.EmitEmbeddedSources(spc, "EmbeddedSources.RitsuAdapters.");
        });
    }
}
