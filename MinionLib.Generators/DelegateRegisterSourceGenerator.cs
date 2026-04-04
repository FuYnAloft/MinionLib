#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MinionLib.Generators;

[Generator(LanguageNames.CSharp)]
public sealed class DelegateRegisterSourceGenerator : IIncrementalGenerator
{
    private const string ComponentDelegateAttributeMetadataName = "MinionLib.Component.Core.ComponentDelegateAttribute";

    // Keep nullable annotations in generated generic delegate arguments.
    private static readonly SymbolDisplayFormat FullyQualifiedNullableFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private static readonly DiagnosticDescriptor ContainingTypeMustBePartial = new(
        id: "MLSG203",
        title: "Containing type must be partial",
        messageFormat: "Type '{0}' must be declared as partial to host generated delegate registrations",
        category: "MinionLib.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DelegateMethodMustBeStatic = new(
        id: "MLSG204",
        title: "ComponentDelegate method must be static",
        messageFormat: "Method '{0}' is marked with [ComponentDelegate] but is not static",
        category: "MinionLib.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedDelegateSignature = new(
        id: "MLSG205",
        title: "Unsupported ComponentDelegate signature",
        messageFormat: "Method '{0}' cannot be mapped to Action/Func delegate type: {1}",
        category: "MinionLib.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ComponentDelegateAttributeMetadataName,
                static (node, _) => node is MethodDeclarationSyntax,
                static (ctx, _) => GetCandidate(ctx))
            .Where(static x => x.HasValue)
            .Select(static (x, _) => x.GetValueOrDefault());

        var collected = candidates
            .Collect()
            .Select(static (items, _) => new EquatableArray<DelegateMethodData>(items));

        context.RegisterSourceOutput(collected, static (spc, items) => Emit(spc, items.Items));
    }

    private static DelegateMethodData? GetCandidate(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
            return null;

        var attribute = context.Attributes.FirstOrDefault();
        if (attribute == null)
            return null;

        var containingType = methodSymbol.ContainingType;
        if (containingType == null)
            return null;

        var isSupported = TryGetDelegateTypeDisplayName(methodSymbol, out var delegateTypeDisplayName, out var unsupportedReason);

        return new DelegateMethodData(
            HintName: BuildHintName(containingType),
            NamespaceName: containingType.ContainingNamespace.IsGlobalNamespace ? null : containingType.ContainingNamespace.ToDisplayString(),
            ContainingTypes: BuildContainingTypeChain(containingType),
            TypeDisplayName: GetTypeDisplayName(containingType),
            IsContainingTypePartial: IsFullyPartial(containingType),
            TypeLocation: GetSourceLocation(containingType),
            MethodName: methodSymbol.Name,
            RegistrationKey: BuildRegistrationKey(containingType, methodSymbol, attribute),
            IsStatic: methodSymbol.IsStatic,
            MethodLocation: GetSourceLocation(methodSymbol),
            DelegateTypeDisplayName: delegateTypeDisplayName,
            IsSupportedSignature: isSupported,
            UnsupportedReason: unsupportedReason);
    }

    private static string BuildRegistrationKey(INamedTypeSymbol containingType, IMethodSymbol methodSymbol, AttributeData attribute)
    {
        var typeName = GetTypeDisplayName(containingType);
        var args = attribute.ConstructorArguments;

        if (args.Length == 0)
            return typeName + "." + methodSymbol.Name;

        if (args.Length == 1)
            return typeName + "." + GetStringArgument(args[0], methodSymbol.Name);

        if (args.Length == 2)
            return GetStringArgument(args[0], string.Empty) + "." + GetStringArgument(args[1], methodSymbol.Name);

        return typeName + "." + methodSymbol.Name;
    }

    private static string GetStringArgument(TypedConstant constant, string fallback)
    {
        if (constant.IsNull)
            return fallback;

        return constant.Value as string ?? fallback;
    }

    private static void Emit(SourceProductionContext context, ImmutableArray<DelegateMethodData> methods)
    {
        var grouped = methods
            .GroupBy(static x => x.TypeDisplayName, StringComparer.Ordinal)
            .OrderBy(static g => g.Key, StringComparer.Ordinal);

        foreach (var group in grouped)
        {
            var typeMethods = group
                .OrderBy(static x => x.MethodName, StringComparer.Ordinal)
                .ToImmutableArray();

            if (!typeMethods[0].IsContainingTypePartial)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ContainingTypeMustBePartial,
                    typeMethods[0].TypeLocation,
                    typeMethods[0].TypeDisplayName));
                continue;
            }

            foreach (var method in typeMethods.Where(static x => !x.IsStatic))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DelegateMethodMustBeStatic,
                    method.MethodLocation,
                    method.MethodName));
            }

            foreach (var method in typeMethods.Where(static x => !x.IsSupportedSignature))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    UnsupportedDelegateSignature,
                    method.MethodLocation,
                    method.MethodName,
                    method.UnsupportedReason ?? "unknown reason"));
            }

            var validMethods = typeMethods
                .Where(static x => x.IsStatic && x.IsSupportedSignature)
                .ToImmutableArray();

            if (validMethods.Length == 0)
                continue;

            var source = BuildSource(validMethods[0], validMethods);
            context.AddSource(validMethods[0].HintName, SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string BuildSource(DelegateMethodData typeData, ImmutableArray<DelegateMethodData> methods)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");

        if (!string.IsNullOrWhiteSpace(typeData.NamespaceName))
        {
            sb.Append("namespace ").Append(typeData.NamespaceName).AppendLine(";");
            sb.AppendLine();
        }

        AppendContainingTypeDeclarations(sb, typeData.ContainingTypes);

        sb.AppendLine("    [global::System.Runtime.CompilerServices.ModuleInitializer]");
        sb.AppendLine("    internal static void __AutoRegister_Delegates()");
        sb.AppendLine("    {");

        foreach (var method in methods)
        {
            sb.Append("        global::MinionLib.Component.Core.DelegateRegistry.Register<")
                .Append(method.DelegateTypeDisplayName)
                .Append(">(\"")
                .Append(method.RegistrationKey)
                .Append("\", ")
                .Append(method.MethodName)
                .AppendLine(");");
        }

        sb.AppendLine("    }");

        AppendContainingTypeClosures(sb, typeData.ContainingTypes.Length);
        return sb.ToString();
    }

    private static Location? GetSourceLocation(ISymbol symbol)
    {
        return symbol.Locations.FirstOrDefault(static l => l != null && l.IsInSource);
    }

    private static ImmutableArray<ContainingTypeData> BuildContainingTypeChain(INamedTypeSymbol containingType)
    {
        var stack = new Stack<INamedTypeSymbol>();
        for (var current = containingType; current != null; current = current.ContainingType)
            stack.Push(current);

        var builder = ImmutableArray.CreateBuilder<ContainingTypeData>(stack.Count);
        while (stack.Count > 0)
        {
            var type = stack.Pop();
            builder.Add(new ContainingTypeData(
                GetTypeKeyword(type),
                type.Name,
                type.TypeParameters.Select(static x => x.Name).ToImmutableArray()));
        }

        return builder.ToImmutable();
    }

    private static bool IsFullyPartial(INamedTypeSymbol type)
    {
        for (var current = type; current != null; current = current.ContainingType)
        {
            if (!IsPartial(current))
                return false;
        }

        return true;
    }

    private static bool IsPartial(INamedTypeSymbol type)
    {
        return type.DeclaringSyntaxReferences
            .Select(static r => r.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .Any(static s => s.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));
    }

    private static bool TryGetDelegateTypeDisplayName(IMethodSymbol methodSymbol, out string? delegateTypeDisplayName, out string? unsupportedReason)
    {
        delegateTypeDisplayName = null;
        unsupportedReason = null;

        if (methodSymbol.ReturnsByRef || methodSymbol.ReturnsByRefReadonly)
        {
            unsupportedReason = "ref return is not supported";
            return false;
        }

        if (methodSymbol.Parameters.Any(static p => p.RefKind != RefKind.None || p.IsParams))
        {
            unsupportedReason = "ref/out/in/params parameters are not supported";
            return false;
        }

        var typeArguments = new List<string>(methodSymbol.Parameters.Length + 1);
        foreach (var parameter in methodSymbol.Parameters)
            typeArguments.Add(parameter.Type.ToDisplayString(FullyQualifiedNullableFormat));

        var isVoid = methodSymbol.ReturnsVoid;
        if (!isVoid)
            typeArguments.Add(methodSymbol.ReturnType.ToDisplayString(FullyQualifiedNullableFormat));

        if (isVoid)
        {
            if (typeArguments.Count == 0)
            {
                delegateTypeDisplayName = "global::System.Action";
                return true;
            }

            if (typeArguments.Count > 16)
            {
                unsupportedReason = "Action supports at most 16 parameters";
                return false;
            }

            delegateTypeDisplayName = "global::System.Action<" + string.Join(", ", typeArguments) + ">";
            return true;
        }

        if (typeArguments.Count > 17)
        {
            unsupportedReason = "Func supports at most 16 parameters plus return type";
            return false;
        }

        delegateTypeDisplayName = "global::System.Func<" + string.Join(", ", typeArguments) + ">";
        return true;
    }

    private static string BuildHintName(INamedTypeSymbol type)
    {
        var containingType = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", string.Empty)
            .Replace('<', '_')
            .Replace('>', '_')
            .Replace('.', '_');

        return containingType + ".DelegateRegister.g.cs";
    }

    private static string GetTypeDisplayName(INamedTypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty);
    }

    private static string GetTypeKeyword(INamedTypeSymbol symbol)
    {
        if (symbol.IsStatic)
            return "static class";

        if (symbol.IsRecord)
            return symbol.IsValueType ? "record struct" : "record";

        return symbol.TypeKind switch
        {
            TypeKind.Struct => "struct",
            TypeKind.Interface => "interface",
            _ => "class"
        };
    }

    private static void AppendContainingTypeDeclarations(StringBuilder builder, ImmutableArray<ContainingTypeData> containingTypes)
    {
        for (var i = 0; i < containingTypes.Length; i++)
        {
            var type = containingTypes[i];
            var indent = new string(' ', i * 4);
            builder.Append(indent)
                .Append("partial ")
                .Append(type.Keyword)
                .Append(' ')
                .Append(type.Name);

            if (!type.TypeParameters.IsDefaultOrEmpty)
                builder.Append('<').Append(string.Join(", ", type.TypeParameters)).Append('>');

            builder.AppendLine();
            builder.Append(indent).AppendLine("{");
        }
    }

    private static void AppendContainingTypeClosures(StringBuilder builder, int depth)
    {
        for (var i = depth - 1; i >= 0; i--)
            builder.Append(new string(' ', i * 4)).AppendLine("}");
    }

    private readonly record struct DelegateMethodData(
        string HintName,
        string? NamespaceName,
        ImmutableArray<ContainingTypeData> ContainingTypes,
        string TypeDisplayName,
        bool IsContainingTypePartial,
        Location? TypeLocation,
        string MethodName,
        string RegistrationKey,
        bool IsStatic,
        Location? MethodLocation,
        string? DelegateTypeDisplayName,
        bool IsSupportedSignature,
        string? UnsupportedReason);

    private readonly record struct ContainingTypeData(
        string Keyword,
        string Name,
        ImmutableArray<string> TypeParameters);
}

