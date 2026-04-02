#nullable enable
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
        var propertyCandidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => IsCandidateProperty(node),
                static (ctx, _) => GetCandidate(ctx))
            .Where(static x => x != null)
            .Select(static (x, _) => x!);

        var compilationAndProperties = context.CompilationProvider.Combine(propertyCandidates.Collect());

        context.RegisterSourceOutput(compilationAndProperties, static (spc, source) =>
        {
            var (_, properties) = source;
            Emit(spc, properties);
        });
    }

    private static bool IsCandidateProperty(SyntaxNode node)
    {
        return node is PropertyDeclarationSyntax property
               && property.AttributeLists.Count > 0
               && property.Modifiers.Any(SyntaxKind.PartialKeyword);
    }

    private static PropertyCandidate? GetCandidate(GeneratorSyntaxContext context)
    {
        var propertySyntax = (PropertyDeclarationSyntax)context.Node;
        var declaredSymbol = context.SemanticModel.GetDeclaredSymbol(propertySyntax);
        if (declaredSymbol is null)
            return null;
        if (declaredSymbol is not IPropertySymbol propertySymbol)
            return null;

        var componentStateAttribute = propertySymbol
            .GetAttributes()
            .FirstOrDefault(IsComponentStateAttribute);

        if (componentStateAttribute == null)
            return null;

        return new PropertyCandidate(propertySyntax, propertySymbol, componentStateAttribute);
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

    private static void Emit(SourceProductionContext context, ImmutableArray<PropertyCandidate> properties)
    {
        foreach (var candidate in properties.Distinct(PropertyCandidateComparer.Instance))
        {
            if (!TryValidateCandidate(candidate, out var reason))
            {
                if (reason != null)
                    context.ReportDiagnostic(reason);

                continue;
            }

            var hintName = BuildHintName(candidate.Symbol);
            var source = BuildSource(candidate);
            context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
        }
    }

    private static bool TryValidateCandidate(
        PropertyCandidate candidate,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;

        if (!candidate.Syntax.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            diagnostic = Diagnostic.Create(
                PropertyMustBePartial,
                candidate.Syntax.Identifier.GetLocation(),
                candidate.Symbol.Name);
            return false;
        }

        var containingType = candidate.Symbol.ContainingType;
        if (containingType.TypeKind != TypeKind.Class
            || !IsPartialType(containingType))
        {
            diagnostic = Diagnostic.Create(
                ContainingTypeMustBePartial,
                candidate.Syntax.Identifier.GetLocation(),
                containingType.Name);
            return false;
        }

        if (candidate.Symbol.GetMethod == null || candidate.Symbol.SetMethod == null)
            return false;

        return true;
    }

    private static bool HasDynamicVarGenerator(AttributeData attribute)
    {
        var attributeClass = attribute.AttributeClass;
        if (attributeClass == null)
            return false;

        if (attributeClass.IsGenericType)
            return attributeClass.TypeArguments.Length == 1 && attributeClass.TypeArguments[0].SpecialType != SpecialType.System_Object;

        if (attribute.ConstructorArguments.Length == 0)
            return false;

        var firstArg = attribute.ConstructorArguments[0];
        return firstArg.Kind == TypedConstantKind.Type && firstArg.Value is ITypeSymbol;
    }

    private static bool IsPartialType(INamedTypeSymbol type)
    {
        return type.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .Any(s => s.Modifiers.Any(SyntaxKind.PartialKeyword));
    }

    private static string BuildHintName(IPropertySymbol property)
    {
        var containingType = property.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", string.Empty)
            .Replace('<', '_')
            .Replace('>', '_')
            .Replace('.', '_');

        return $"{containingType}_{property.Name}.ComponentStateProperty.g.cs";
    }

    private static string BuildSource(PropertyCandidate candidate)
    {
        var property = candidate.Symbol;
        var namespaceName = property.ContainingNamespace.IsGlobalNamespace
            ? null
            : property.ContainingNamespace.ToDisplayString();

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");

        if (!string.IsNullOrWhiteSpace(namespaceName))
        {
            builder.Append("namespace ").Append(namespaceName).AppendLine(";");
            builder.AppendLine();
        }

        AppendContainingTypeDeclarations(builder, property.ContainingType);

        var propertyType = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var fieldName = "__" + ToCamelCase(property.Name) + "BackingField";

        builder.Append("    private ").Append(propertyType).Append(' ').Append(fieldName).AppendLine(";");
        builder.AppendLine();
        var accessibilityKeyword = GetAccessibilityKeyword(property.DeclaredAccessibility);
        builder.Append("    ");
        if (!string.IsNullOrEmpty(accessibilityKeyword))
            builder.Append(accessibilityKeyword).Append(' ');

        builder.Append("partial ").Append(propertyType).Append(' ').Append(property.Name).AppendLine();
        builder.AppendLine("    {");

        var getAccessorPrefix = GetAccessorAccessibilityKeyword(property.DeclaredAccessibility, property.GetMethod!.DeclaredAccessibility);
        builder.Append("        ");
        if (!string.IsNullOrEmpty(getAccessorPrefix))
            builder.Append(getAccessorPrefix).Append(' ');
        builder.Append("get => ").Append(fieldName).AppendLine(";");

        var setAccessorPrefix = GetAccessorAccessibilityKeyword(property.DeclaredAccessibility, property.SetMethod!.DeclaredAccessibility);
        builder.Append("        ");
        if (!string.IsNullOrEmpty(setAccessorPrefix))
            builder.Append(setAccessorPrefix).Append(' ');
        builder.AppendLine("set");
        builder.AppendLine("        {");
        builder.Append("            ").Append(fieldName).AppendLine(" = value;");
        if (HasDynamicVarGenerator(candidate.Attribute))
            builder.Append("            DynamicVars[\"").Append(property.Name).AppendLine("\"].BaseValue = global::System.Convert.ToDecimal(value);");
        builder.AppendLine("        }");
        builder.AppendLine("    }");

        AppendContainingTypeClosures(builder, property.ContainingType);

        return builder.ToString();
    }

    private static void AppendContainingTypeDeclarations(StringBuilder builder, INamedTypeSymbol containingType)
    {
        var stack = new Stack<INamedTypeSymbol>();
        var current = containingType;
        while (current != null)
        {
            stack.Push(current);
            current = current.ContainingType;
        }

        var indentLevel = 0;
        while (stack.Count > 0)
        {
            var type = stack.Pop();
            var indent = new string(' ', indentLevel * 4);
            builder.Append(indent)
                .Append("partial ")
                .Append(GetTypeKeyword(type))
                .Append(' ')
                .Append(type.Name);

            if (type.TypeParameters.Length > 0)
            {
                builder.Append('<').Append(string.Join(", ", type.TypeParameters.Select(tp => tp.Name))).Append('>');
            }

            builder.AppendLine();
            builder.Append(indent).AppendLine("{");
            indentLevel++;
        }
    }

    private static void AppendContainingTypeClosures(StringBuilder builder, INamedTypeSymbol containingType)
    {
        var depth = 0;
        var current = containingType;
        while (current != null)
        {
            depth++;
            current = current.ContainingType;
        }

        for (var i = depth - 1; i >= 0; i--)
        {
            builder.Append(new string(' ', i * 4)).AppendLine("}");
        }
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

    private static string GetAccessorAccessibilityKeyword(Accessibility propertyAccessibility,
        Accessibility accessorAccessibility)
    {
        // Only emit accessor modifiers when accessor is explicitly more restrictive than property.
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

    private sealed class PropertyCandidate
    {
        public PropertyCandidate(PropertyDeclarationSyntax syntax, IPropertySymbol symbol, AttributeData attribute)
        {
            Syntax = syntax;
            Symbol = symbol;
            Attribute = attribute;
        }

        public PropertyDeclarationSyntax Syntax { get; }
        public IPropertySymbol Symbol { get; }
        public AttributeData Attribute { get; }
    }

    private sealed class PropertyCandidateComparer : IEqualityComparer<PropertyCandidate>
    {
        public static readonly PropertyCandidateComparer Instance = new();

        public bool Equals(PropertyCandidate? x, PropertyCandidate? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (x is null || y is null)
                return false;

            return SymbolEqualityComparer.Default.Equals(x.Symbol, y.Symbol);
        }

        public int GetHashCode(PropertyCandidate obj)
        {
            return SymbolEqualityComparer.Default.GetHashCode(obj.Symbol);
        }
    }
}


