#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MinionLib.Generators;

[Generator(LanguageNames.CSharp)]
public sealed class DynamicVarSourceGenerator : IIncrementalGenerator
{
    private const string ComponentStateAttributeMetadataName = "MinionLib.Component.Core.ComponentStateAttribute";
    private const string ComponentStateGenericAttributeMetadataName = "MinionLib.Component.Core.ComponentStateAttribute`1";
    private const string CardComponentMetadataName = "MinionLib.Component.CardComponent";
    private const string DynamicVarMetadataName = "MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar";

    private static readonly DiagnosticDescriptor CardComponentTypeMustBePartial = new(
        id: "MLSG200",
        title: "CardComponent type must be partial for generated SmartVars",
        messageFormat: "CardComponent subtype '{0}' defines [ComponentState] DynamicVar properties and must be declared partial",
        category: "MinionLib.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DynamicVarTypeMustInheritDynamicVar = new(
        id: "MLSG201",
        title: "ComponentState dynamic var type is invalid",
        messageFormat: "Property '{0}' uses [ComponentState] with type '{1}', but it does not inherit DynamicVar",
        category: "MinionLib.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, static (spc, compilation) => Emit(spc, compilation));
    }

    private static void Emit(SourceProductionContext context, Compilation compilation)
    {
        var cardComponentType = compilation.GetTypeByMetadataName(CardComponentMetadataName);
        var dynamicVarType = compilation.GetTypeByMetadataName(DynamicVarMetadataName);
        if (cardComponentType == null || dynamicVarType == null)
            return;

        var componentStateAttribute = compilation.GetTypeByMetadataName(ComponentStateAttributeMetadataName);
        var componentStateGenericAttribute = compilation.GetTypeByMetadataName(ComponentStateGenericAttributeMetadataName);

        foreach (var type in GetAllTypes(compilation.Assembly.GlobalNamespace))
        {
            if (type.TypeKind != TypeKind.Class)
                continue;
            if (SymbolEqualityComparer.Default.Equals(type, cardComponentType))
                continue;
            if (!InheritsFrom(type, cardComponentType))
                continue;

            var ownRules = GetOwnRules(type, componentStateAttribute, componentStateGenericAttribute, dynamicVarType, context);
            if (ownRules.Length == 0)
                continue;

            if (!IsPartial(type))
            {
                context.ReportDiagnostic(Diagnostic.Create(CardComponentTypeMustBePartial,
                    type.Locations.FirstOrDefault(), type.ToDisplayString()));
                continue;
            }

            var source = BuildSource(type, ownRules);
            context.AddSource(BuildHintName(type), SourceText.From(source, Encoding.UTF8));
        }
    }

    private static ImmutableArray<DynamicVarRule> GetOwnRules(
        INamedTypeSymbol type,
        INamedTypeSymbol? componentStateAttribute,
        INamedTypeSymbol? componentStateGenericAttribute,
        INamedTypeSymbol dynamicVarType,
        SourceProductionContext context)
    {
        var result = ImmutableArray.CreateBuilder<DynamicVarRule>();

        foreach (var property in type.GetMembers().OfType<IPropertySymbol>())
        {
            if (property.IsStatic || property.GetMethod == null)
                continue;
            if (property.Parameters.Length != 0)
                continue;

            var attribute = property.GetAttributes()
                .FirstOrDefault(a => IsComponentStateAttribute(a, componentStateAttribute, componentStateGenericAttribute));
            if (attribute == null)
                continue;

            if (!TryExtractGenerator(attribute, out var generatorType, out var constructorArgs))
                continue;
            if (generatorType == null)
                continue;

            if (!InheritsFrom(generatorType, dynamicVarType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DynamicVarTypeMustInheritDynamicVar,
                    property.Locations.FirstOrDefault(),
                    property.Name,
                    generatorType.ToDisplayString()));
                continue;
            }

            result.Add(new DynamicVarRule(property.Name, generatorType, constructorArgs));
        }

        return result
            .OrderBy(x => x.PropertyName, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    private static bool TryExtractGenerator(
        AttributeData attribute,
        out INamedTypeSymbol? generatorType,
        out ImmutableArray<TypedConstant> constructorArgs)
    {
        generatorType = null;
        constructorArgs = ImmutableArray<TypedConstant>.Empty;

        var attrClass = attribute.AttributeClass;
        if (attrClass == null)
            return false;

        if (attrClass.IsGenericType && attrClass.TypeArguments.Length == 1)
        {
            generatorType = attrClass.TypeArguments[0] as INamedTypeSymbol;
            constructorArgs = NormalizeConstructorArgs(attribute.ConstructorArguments, 0);
            return true;
        }

        if (attribute.ConstructorArguments.Length == 0)
            return true;

        var first = attribute.ConstructorArguments[0];
        if (first.Kind == TypedConstantKind.Type && first.Value is INamedTypeSymbol typeSymbol)
            generatorType = typeSymbol;

        constructorArgs = NormalizeConstructorArgs(attribute.ConstructorArguments, 1);

        return true;
    }

    private static ImmutableArray<TypedConstant> NormalizeConstructorArgs(ImmutableArray<TypedConstant> args, int startIndex)
    {
        if (args.Length <= startIndex)
            return ImmutableArray<TypedConstant>.Empty;

        if (args.Length == startIndex + 1 && args[startIndex].Kind == TypedConstantKind.Array)
            return args[startIndex].Values;

        return args
            .Skip(startIndex)
            .ToImmutableArray();
    }

    private static bool IsComponentStateAttribute(
        AttributeData attribute,
        INamedTypeSymbol? componentStateAttribute,
        INamedTypeSymbol? componentStateGenericAttribute)
    {
        var original = attribute.AttributeClass?.OriginalDefinition;
        if (original == null)
            return false;

        return SymbolEqualityComparer.Default.Equals(original, componentStateAttribute)
               || SymbolEqualityComparer.Default.Equals(original, componentStateGenericAttribute);
    }

    private static string BuildSource(INamedTypeSymbol type, ImmutableArray<DynamicVarRule> rules)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");

        if (!type.ContainingNamespace.IsGlobalNamespace)
        {
            sb.Append("namespace ").Append(type.ContainingNamespace.ToDisplayString()).AppendLine(";");
            sb.AppendLine();
        }

        AppendContainingTypeDeclarations(sb, type);

        sb.AppendLine("    protected override global::System.Collections.Generic.IEnumerable<global::MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar> SmartVars");
        sb.AppendLine("    {");
        sb.AppendLine("        get");
        sb.AppendLine("        {");
        sb.AppendLine("            foreach (var __baseVar in base.SmartVars)");
        sb.AppendLine("                yield return __baseVar;");

        foreach (var rule in rules)
        {
            sb.Append("            yield return new ")
                .Append(rule.DynamicVarType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                .Append("(\"")
                .Append(rule.PropertyName)
                .Append("\", global::System.Convert.ToDecimal(this.")
                .Append(rule.PropertyName)
                .Append(")");

            foreach (var arg in rule.ConstructorArgs)
            {
                sb.Append(", ").Append(TypedConstantToCode(arg));
            }

            sb.AppendLine(");");
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");

        AppendContainingTypeClosures(sb, type);
        return sb.ToString();
    }

    private static string TypedConstantToCode(TypedConstant constant)
    {
        if (constant.IsNull)
            return "null";

        switch (constant.Kind)
        {
            case TypedConstantKind.Primitive:
                return PrimitiveToCode(constant.Type, constant.Value);
            case TypedConstantKind.Enum:
                return "(" + constant.Type!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ")"
                       + PrimitiveToCode(((INamedTypeSymbol)constant.Type!).EnumUnderlyingType, constant.Value);
            case TypedConstantKind.Type:
                return "typeof(" + ((ITypeSymbol)constant.Value!).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ")";
            case TypedConstantKind.Array:
            {
                var elementType = constant.Type is IArrayTypeSymbol arr
                    ? arr.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    : "object";
                var values = string.Join(", ", constant.Values.Select(TypedConstantToCode));
                return "new " + elementType + "[] { " + values + " }";
            }
            default:
                return "default";
        }
    }

    private static string PrimitiveToCode(ITypeSymbol? type, object? value)
    {
        if (value == null)
            return "null";

        return value switch
        {
            bool b => b ? "true" : "false",
            string s => "@\"" + s.Replace("\"", "\"\"") + "\"",
            char c => "'" + (c == '\'' ? "\\'" : c.ToString()) + "'",
            float f => f.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "f",
            double d => d.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "d",
            decimal m => m.ToString(System.Globalization.CultureInfo.InvariantCulture) + "m",
            byte bt => bt.ToString(System.Globalization.CultureInfo.InvariantCulture),
            sbyte sb => "(sbyte)" + sb.ToString(System.Globalization.CultureInfo.InvariantCulture),
            short s16 => "(short)" + s16.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ushort u16 => "(ushort)" + u16.ToString(System.Globalization.CultureInfo.InvariantCulture),
            int i => i.ToString(System.Globalization.CultureInfo.InvariantCulture),
            uint u => u.ToString(System.Globalization.CultureInfo.InvariantCulture) + "u",
            long l => l.ToString(System.Globalization.CultureInfo.InvariantCulture) + "L",
            ulong ul => ul.ToString(System.Globalization.CultureInfo.InvariantCulture) + "UL",
            _ when type?.SpecialType == SpecialType.System_String => "@\"" + value.ToString()!.Replace("\"", "\"\"") + "\"",
            _ => value.ToString() ?? "default"
        };
    }

    private static string BuildHintName(INamedTypeSymbol type)
    {
        var containingType = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", string.Empty)
            .Replace('<', '_')
            .Replace('>', '_')
            .Replace('.', '_');

        return containingType + ".DynamicVars.g.cs";
    }

    private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol ns)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            yield return type;
            foreach (var nested in GetAllNestedTypes(type))
                yield return nested;
        }

        foreach (var child in ns.GetNamespaceMembers())
            foreach (var type in GetAllTypes(child))
                yield return type;
    }

    private static IEnumerable<INamedTypeSymbol> GetAllNestedTypes(INamedTypeSymbol type)
    {
        foreach (var nested in type.GetTypeMembers())
        {
            yield return nested;
            foreach (var child in GetAllNestedTypes(nested))
                yield return child;
        }
    }

    private static bool InheritsFrom(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;
        }

        return false;
    }

    private static bool IsPartial(INamedTypeSymbol type)
    {
        return type.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .Any(s => s.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)));
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
                builder.Append('<').Append(string.Join(", ", type.TypeParameters.Select(tp => tp.Name))).Append('>');

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
            builder.Append(new string(' ', i * 4)).AppendLine("}");
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

    private sealed class DynamicVarRule
    {
        public DynamicVarRule(string propertyName, INamedTypeSymbol dynamicVarType,
            ImmutableArray<TypedConstant> constructorArgs)
        {
            PropertyName = propertyName;
            DynamicVarType = dynamicVarType;
            ConstructorArgs = constructorArgs;
        }

        public string PropertyName { get; }
        public INamedTypeSymbol DynamicVarType { get; }
        public ImmutableArray<TypedConstant> ConstructorArgs { get; }
    }
}



