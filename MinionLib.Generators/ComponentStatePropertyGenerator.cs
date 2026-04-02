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
public sealed class ComponentStatePropertyGenerator : IIncrementalGenerator
{
    private const string ComponentStateAttributeMetadataName = "MinionLib.Component.Core.ComponentStateAttribute";
    private const string ComponentStateGenericAttributeMetadataName = "MinionLib.Component.Core.ComponentStateAttribute`1";

    private static readonly DiagnosticDescriptor PropertyMustBePartial = new(
        id: "MLSG001",
        title: "ComponentState property must be partial",
        messageFormat: "Property '{0}' must be declared as 'partial' to use source-generated ComponentState backing implementation",
        category: "MinionLib.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ContainingTypeMustBePartial = new(
        id: "MLSG002",
        title: "Containing type must be partial",
        messageFormat: "Type '{0}' must be declared as partial to host generated ComponentState properties",
        category: "MinionLib.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var nonGeneric = context.SyntaxProvider.ForAttributeWithMetadataName(
            ComponentStateAttributeMetadataName,
            static (node, _) => node is PropertyDeclarationSyntax,
            static (ctx, _) => GetCandidate(ctx));

        var generic = context.SyntaxProvider.ForAttributeWithMetadataName(
            ComponentStateGenericAttributeMetadataName,
            static (node, _) => node is PropertyDeclarationSyntax,
            static (ctx, _) => GetCandidate(ctx));

        context.RegisterSourceOutput(
            nonGeneric.Where(static x => x.HasValue).Select(static (x, _) => x.GetValueOrDefault()),
            static (spc, candidate) => Emit(spc, candidate));

        context.RegisterSourceOutput(
            generic.Where(static x => x.HasValue).Select(static (x, _) => x.GetValueOrDefault()),
            static (spc, candidate) => Emit(spc, candidate));
    }

    private static PropertyData? GetCandidate(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not IPropertySymbol propertySymbol)
            return null;

        var propertySyntax = propertySymbol.DeclaringSyntaxReferences
            .Select(static x => x.GetSyntax())
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault();
        if (propertySyntax == null)
            return null;

        var attribute = propertySymbol.GetAttributes().FirstOrDefault(IsComponentStateAttribute);
        if (attribute == null)
            return null;

        var containingType = propertySymbol.ContainingType;
        var propertyName = propertySymbol.Name;
        var typeFqn = propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var namespaceName = propertySymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : propertySymbol.ContainingNamespace.ToDisplayString();
        var location = DiagnosticLocationData.FromLocation(propertySyntax.Identifier.GetLocation());

        return new PropertyData(
            HintName: BuildHintName(containingType, propertyName),
            NamespaceName: namespaceName,
            ContainingTypes: BuildContainingTypeChain(containingType),
            ContainingTypeName: containingType.Name,
            PropertyName: propertyName,
            PropertyTypeFqn: typeFqn,
            FieldName: "__" + ToCamelCase(propertyName) + "BackingField",
            PropertyAccessibility: propertySymbol.DeclaredAccessibility,
            GetAccessibility: propertySymbol.GetMethod?.DeclaredAccessibility,
            SetAccessibility: propertySymbol.SetMethod?.DeclaredAccessibility,
            HasGetter: propertySymbol.GetMethod != null,
            HasSetter: propertySymbol.SetMethod != null,
            IsPropertyPartial: propertySyntax.Modifiers.Any(SyntaxKind.PartialKeyword),
            IsContainingTypePartial: IsPartialType(containingType),
            HasDynamicVarGenerator: HasDynamicVarGenerator(attribute),
            DiagnosticLocation: location);
    }

    private static bool IsComponentStateAttribute(AttributeData attribute)
    {
        var original = attribute.AttributeClass?.OriginalDefinition;
        if (original == null)
            return false;

        var fullName = original.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return fullName == "global::MinionLib.Component.Core.ComponentStateAttribute"
               || fullName == "global::MinionLib.Component.Core.ComponentStateAttribute<T>";
    }

    private static bool HasDynamicVarGenerator(AttributeData attribute)
    {
        var attributeClass = attribute.AttributeClass;
        if (attributeClass == null)
            return false;

        return attributeClass.IsGenericType
               && attributeClass.TypeArguments.Length == 1
               && attributeClass.TypeArguments[0].SpecialType != SpecialType.System_Object;
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

    private static bool IsPartialType(INamedTypeSymbol type)
    {
        return type.DeclaringSyntaxReferences
            .Select(static r => r.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .Any(static s => s.Modifiers.Any(SyntaxKind.PartialKeyword));
    }

    private static void Emit(SourceProductionContext context, PropertyData data)
    {
        if (!data.IsPropertyPartial)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                PropertyMustBePartial,
                data.DiagnosticLocation.ToLocation(),
                data.PropertyName));
            return;
        }

        if (!data.IsContainingTypePartial)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ContainingTypeMustBePartial,
                data.DiagnosticLocation.ToLocation(),
                data.ContainingTypeName));
            return;
        }

        if (!data.HasGetter || !data.HasSetter || data.GetAccessibility is null || data.SetAccessibility is null)
            return;

        var getAccessibility = data.GetAccessibility.GetValueOrDefault();
        var setAccessibility = data.SetAccessibility.GetValueOrDefault();

        var source = BuildSource(data, getAccessibility, setAccessibility);
        context.AddSource(data.HintName, SourceText.From(source, Encoding.UTF8));
    }

    private static string BuildSource(PropertyData data, Accessibility getAccessibility, Accessibility setAccessibility)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");

        if (!string.IsNullOrWhiteSpace(data.NamespaceName))
        {
            builder.Append("namespace ").Append(data.NamespaceName).AppendLine(";");
            builder.AppendLine();
        }

        AppendContainingTypeDeclarations(builder, data.ContainingTypes);

        builder.Append("    private ").Append(data.PropertyTypeFqn).Append(' ').Append(data.FieldName).AppendLine(";");
        builder.AppendLine();

        var accessibilityKeyword = GetAccessibilityKeyword(data.PropertyAccessibility);
        builder.Append("    ");
        if (!string.IsNullOrEmpty(accessibilityKeyword))
            builder.Append(accessibilityKeyword).Append(' ');

        builder.Append("partial ").Append(data.PropertyTypeFqn).Append(' ').Append(data.PropertyName).AppendLine();
        builder.AppendLine("    {");

        var getAccessorPrefix = GetAccessorAccessibilityKeyword(data.PropertyAccessibility, getAccessibility);
        builder.Append("        ");
        if (!string.IsNullOrEmpty(getAccessorPrefix))
            builder.Append(getAccessorPrefix).Append(' ');
        builder.Append("get => ").Append(data.FieldName).AppendLine(";");

        var setAccessorPrefix = GetAccessorAccessibilityKeyword(data.PropertyAccessibility, setAccessibility);
        builder.Append("        ");
        if (!string.IsNullOrEmpty(setAccessorPrefix))
            builder.Append(setAccessorPrefix).Append(' ');
        builder.AppendLine("set");
        builder.AppendLine("        {");
        builder.Append("            ").Append(data.FieldName).AppendLine(" = value;");
        if (data.HasDynamicVarGenerator)
            builder.Append("            DynamicVars[\"").Append(data.PropertyName).AppendLine("\"].BaseValue = global::System.Convert.ToDecimal(value);");
        builder.AppendLine("        }");
        builder.AppendLine("    }");

        AppendContainingTypeClosures(builder, data.ContainingTypes.Length);
        return builder.ToString();
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

    private static string BuildHintName(INamedTypeSymbol containingType, string propertyName)
    {
        var typePart = containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", string.Empty)
            .Replace('<', '_')
            .Replace('>', '_')
            .Replace('.', '_');

        return $"{typePart}_{propertyName}.ComponentStateProperty.g.cs";
    }

    private static string GetTypeKeyword(INamedTypeSymbol symbol)
    {
        if (symbol.IsRecord)
            return symbol.IsValueType ? "record struct" : "record";

        return symbol.TypeKind switch
        {
            TypeKind.Struct => "struct",
            TypeKind.Interface => "interface",
            _ => "class"
        };
    }

    private static string GetAccessibilityKeyword(Accessibility accessibility)
    {
        return accessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Private => "private",
            Accessibility.Internal => "internal",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "private protected",
            _ => string.Empty
        };
    }

    private static string GetAccessorAccessibilityKeyword(Accessibility propertyAccessibility, Accessibility accessorAccessibility)
    {
        return accessorAccessibility == propertyAccessibility
            ? string.Empty
            : GetAccessibilityKeyword(accessorAccessibility);
    }

    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        if (value.Length == 1)
            return value.ToLowerInvariant();
        return char.ToLowerInvariant(value[0]) + value.Substring(1);
    }

    private readonly record struct ContainingTypeData(
        string Keyword,
        string Name,
        ImmutableArray<string> TypeParameters);

    private readonly record struct PropertyData(
        string HintName,
        string? NamespaceName,
        ImmutableArray<ContainingTypeData> ContainingTypes,
        string ContainingTypeName,
        string PropertyName,
        string PropertyTypeFqn,
        string FieldName,
        Accessibility PropertyAccessibility,
        Accessibility? GetAccessibility,
        Accessibility? SetAccessibility,
        bool HasGetter,
        bool HasSetter,
        bool IsPropertyPartial,
        bool IsContainingTypePartial,
        bool HasDynamicVarGenerator,
        DiagnosticLocationData DiagnosticLocation);

    private readonly record struct DiagnosticLocationData(
        string? Path,
        int Start,
        int Length,
        int StartLine,
        int StartCharacter,
        int EndLine,
        int EndCharacter)
    {
        public static DiagnosticLocationData FromLocation(Location? location)
        {
            if (location == null || location == Location.None || !location.IsInSource || location.SourceTree == null)
                return default;

            var span = location.SourceSpan;
            var lineSpan = location.GetLineSpan().Span;
            return new DiagnosticLocationData(
                location.SourceTree.FilePath,
                span.Start,
                span.Length,
                lineSpan.Start.Line,
                lineSpan.Start.Character,
                lineSpan.End.Line,
                lineSpan.End.Character);
        }

        public Location? ToLocation()
        {
            if (string.IsNullOrWhiteSpace(Path) || Length < 0 || Start < 0)
                return null;

            return Location.Create(
                Path!,
                new TextSpan(Start, Length),
                new LinePositionSpan(
                    new LinePosition(StartLine, StartCharacter),
                    new LinePosition(EndLine, EndCharacter)));
        }
    }
}
