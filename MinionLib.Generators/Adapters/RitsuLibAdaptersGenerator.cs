using Microsoft.CodeAnalysis;

namespace MinionLib.Generators.Adapters;

[Generator(LanguageNames.CSharp)]
public class RitsuLibAdaptersGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 增量提取所需数据
        var generationData = context.CompilationProvider.Select((compilation, _) => new
        {
            IsPresent = AdapterGeneratorHelper.CheckPresence(compilation, "STS2-RitsuLib", "STS2RitsuLib"),
            compilation.AssemblyName
        });

        // 满足条件时释放对应的适配器代码
        context.RegisterSourceOutput(generationData, (spc, data) =>
        {
            if (data.IsPresent)
                AdapterGeneratorHelper.EmitEmbeddedSources(
                    spc,
                    "EmbeddedSources.RitsuAdapters.",
                    "RitsuAdapters",
                    data.AssemblyName);
        });
    }
}
