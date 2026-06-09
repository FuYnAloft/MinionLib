using Microsoft.CodeAnalysis;

namespace MinionLib.Generators.Adapters;

[Generator(LanguageNames.CSharp)]
public class BaseLibAdaptersGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 增量检查 BaseLib 是否存在，并提取程序集名称
        var generationData = context.CompilationProvider.Select((compilation, _) => new
        {
            IsPresent = AdapterGeneratorHelper.CheckPresence(compilation, "BaseLib", "BaseLib"),
            compilation.AssemblyName
        });

        // 满足条件时释放对应的适配器代码
        context.RegisterSourceOutput(generationData, (spc, data) =>
        {
            if (data.IsPresent)
                AdapterGeneratorHelper.EmitEmbeddedSources(
                    spc,
                    "EmbeddedSources.BaseLibAdapters.",
                    "BaseLibAdapters",
                    data.AssemblyName);
        });
    }
}
