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
public sealed class DynamicVarSourceGenerator : IIncrementalGenerator
{
    private const string ComponentStateAttributeMetadataName = "MinionLib.Component.Core.ComponentStateAttribute";
    private const string ComponentStateGenericAttributeMetadataName = "MinionLib.Component.Core.ComponentStateAttribute`1";
    private const string CardComponentMetadataName = "MinionLib.Component.CardComponent";
    private const string DynamicVarMetadataName = "MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar";
    private const string LocStringMetadataName = "MegaCrit.Sts2.Core.Localization.LocString";
    private const string IListMetadataName = "System.Collections.Generic.IList`1";
    private const string FullyQualifiedCardComponentMetadataName = "global::" + CardComponentMetadataName;
    private const string FullyQualifiedDynamicVarMetadataName = "global::" + DynamicVarMetadataName;
    private const string FullyQualifiedLocStringMetadataName = "global::" + LocStringMetadataName;
    private const string FullyQualifiedIListMetadataName = "global::" + IListMetadataName;

    private static readonly DiagnosticDescriptor CardComponentTypeMustBePartial = new(
        id: "MLSG200",
        title: "CardComponent type must be partial for generated ComponentState bindings",
        messageFormat: "CardComponent subtype '{0}' defines [ComponentState] properties and must be declared partial",
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
        var typeCandidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                static (ctx, _) => GetTypeCandidate(ctx))
            .Where(static x => x != null)
            .Select(static (x, _) => x!);

        context.RegisterSourceOutput(typeCandidates.Collect(), static (spc, types) => Emit(spc, types));
    }

    private static INamedTypeSymbol? GetTypeCandidate(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classSyntax)
            return null;

        return context.SemanticModel.GetDeclaredSymbol(classSyntax) as INamedTypeSymbol;
    }

    private static void Emit(SourceProductionContext context, ImmutableArray<INamedTypeSymbol> types)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in types)
        {
            var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (!visited.Add(typeName))
                continue;

            if (type.TypeKind != TypeKind.Class)
                continue;
            if (typeName == FullyQualifiedCardComponentMetadataName)
                continue;
            if (!InheritsFromMetadataName(type, FullyQualifiedCardComponentMetadataName))
                continue;

            var ownRules = GetOwnComponentStateRules(type);
            if (ownRules.Length == 0)
                continue;

            if (!IsPartial(type))
            {
                context.ReportDiagnostic(Diagnostic.Create(CardComponentTypeMustBePartial,
                    type.Locations.FirstOrDefault(), type.ToDisplayString()));
                continue;
            }

            var allRules = GetAllComponentStateRules(type);
            if (allRules.Length == 0)
                continue;

            ValidateDynamicVarTypes(context, allRules);

            var smartVarRules = allRules
                .Where(static r => r.GeneratorType != null)
                .OrderBy(static r => r.PropertyName, StringComparer.Ordinal)
                .ToImmutableArray();

            var smartArgRules = ownRules
                .Where(static r => r.GeneratorType == null)
                .OrderBy(static r => r.PropertyName, StringComparer.Ordinal)
                .ToImmutableArray();

            var source = BuildSource(type, smartVarRules, smartArgRules);
            context.AddSource(BuildHintName(type), SourceText.From(source, Encoding.UTF8));
        }
    }

    private static void ValidateDynamicVarTypes(SourceProductionContext context, ImmutableArray<ComponentStateRule> rules)
    {
        foreach (var rule in rules)
        {
            if (rule.GeneratorType == null)
                continue;

            if (!InheritsFromMetadataName(rule.GeneratorType, FullyQualifiedDynamicVarMetadataName))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DynamicVarTypeMustInheritDynamicVar,
                    rule.Property.Locations.FirstOrDefault(),
                    rule.PropertyName,
                    rule.GeneratorType.ToDisplayString()));
            }
        }
    }

    private static ImmutableArray<ComponentStateRule> GetOwnComponentStateRules(INamedTypeSymbol type)
    {
        return type.GetMembers()
            .OfType<IPropertySymbol>()
            .Select(BuildRule)
            .Where(static r => r != null)
            .Select(static r => r!)
            .ToImmutableArray();
    }

    private static ImmutableArray<ComponentStateRule> GetAllComponentStateRules(INamedTypeSymbol type)
    {
        var map = new Dictionary<string, ComponentStateRule>(StringComparer.Ordinal);
        var current = type;
        var chain = new Stack<INamedTypeSymbol>();
        while (current != null)
        {
            chain.Push(current);
            current = current.BaseType;
        }

        while (chain.Count > 0)
        {
            var node = chain.Pop();
            foreach (var property in node.GetMembers().OfType<IPropertySymbol>())
            {
                var rule = BuildRule(property);
                if (rule == null)
                    continue;

                if (!IsAccessibleFromType(rule.Property, type))
                    continue;

                map[rule.PropertyName] = rule;
            }
        }

        return map.Values.ToImmutableArray();
    }

    private static ComponentStateRule? BuildRule(IPropertySymbol property)
    {
        if (property.IsStatic || property.GetMethod == null || property.Parameters.Length != 0)
            return null;

        var attribute = property.GetAttributes()
            .FirstOrDefault(IsComponentStateAttribute);
        if (attribute == null)
            return null;

        if (!TryExtractGenerator(attribute, out var generatorType, out var constructorArgs))
            return null;

        return new ComponentStateRule(property, property.Name, generatorType, constructorArgs);
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

        constructorArgs = NormalizeConstructorArgs(attribute.ConstructorArguments, 0);
        return true;
    }

    private static ImmutableArray<TypedConstant> NormalizeConstructorArgs(ImmutableArray<TypedConstant> args, int startIndex)
    {
        if (args.Length <= startIndex)
            return ImmutableArray<TypedConstant>.Empty;

        if (args.Length == startIndex + 1 && args[startIndex].Kind == TypedConstantKind.Array)
            return args[startIndex].Values;

        return args.Skip(startIndex).ToImmutableArray();
    }

    private static bool IsComponentStateAttribute(AttributeData attribute)
    {
        var originalName = attribute.AttributeClass?.OriginalDefinition
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (string.IsNullOrEmpty(originalName))
            return false;

        return originalName == "global::" + ComponentStateAttributeMetadataName
               || originalName == "global::MinionLib.Component.Core.ComponentStateAttribute<T>";
    }

    private static bool IsAccessibleFromType(IPropertySymbol property, INamedTypeSymbol type)
    {
        if (SymbolEqualityComparer.Default.Equals(property.ContainingType, type))
            return true;

        return property.DeclaredAccessibility switch
        {
            Accessibility.Public => true,
            Accessibility.Internal => true,
            Accessibility.Protected => true,
            Accessibility.ProtectedOrInternal => true,
            Accessibility.ProtectedAndInternal => true,
            _ => false
        };
    }

    private static string BuildSource(
        INamedTypeSymbol type,
        ImmutableArray<ComponentStateRule> smartVarRules,
        ImmutableArray<ComponentStateRule> smartArgRules)
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

        if (smartVarRules.Length > 0)
        {
            sb.AppendLine("    protected override global::System.Collections.Generic.IEnumerable<global::MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar> SmartVars =>");
            sb.AppendLine("    [");
            for (var i = 0; i < smartVarRules.Length; i++)
            {
                var rule = smartVarRules[i];
                sb.Append("        new ")
                    .Append(rule.GeneratorType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                    .Append("(\"")
                    .Append(rule.PropertyName)
                    .Append("\", global::System.Convert.ToDecimal(this.")
                    .Append(rule.PropertyName)
                    .Append(")");

                foreach (var arg in rule.ConstructorArgs)
                    sb.Append(", ").Append(TypedConstantToCode(arg));

                sb.Append(")");
                sb.AppendLine(i == smartVarRules.Length - 1 ? string.Empty : ",");
            }

            sb.AppendLine("    ];");
            sb.AppendLine();
        }

        if (smartArgRules.Length > 0)
        {
            sb.AppendLine("    protected override void SmartAddArgs(global::MegaCrit.Sts2.Core.Localization.LocString loc)");
            sb.AppendLine("    {");
            sb.AppendLine("        base.SmartAddArgs(loc);");
            foreach (var rule in smartArgRules)
            {
                EmitSmartArg(sb, rule);
            }

            sb.AppendLine("    }");
        }

        AppendContainingTypeClosures(sb, type);
        return sb.ToString();
    }

    private static void EmitSmartArg(StringBuilder sb, ComponentStateRule rule)
    {
        var type = rule.Property.Type;
        var valueExpr = "this." + rule.PropertyName;
        var isNullable = type.IsReferenceType || type.NullableAnnotation == NullableAnnotation.Annotated;

        if (isNullable)
        {
            var local = "__v_" + rule.PropertyName;
            sb.Append("        var ").Append(local).Append(" = ").Append(valueExpr).AppendLine(";");
            sb.Append("        if (").Append(local).AppendLine(" == null)");
            sb.Append("            loc.Add(\"").Append(rule.PropertyName).AppendLine("\", 0m);");
            sb.AppendLine("        else");
            EmitNonNullSmartArg(sb, type, rule.PropertyName, local, 3);
            return;
        }

        EmitNonNullSmartArg(sb, type, rule.PropertyName, valueExpr, 2);
    }

    private static void EmitNonNullSmartArg(StringBuilder sb, ITypeSymbol type, string name, string valueExpr,
        int indent)
    {
        var p = new string(' ', indent * 4);

        if (IsTypeOrDerived(type, FullyQualifiedDynamicVarMetadataName))
        {
            sb.Append(p).Append("loc.Add(").Append(valueExpr).AppendLine(");");
            return;
        }

        switch (type.SpecialType)
        {
            case SpecialType.System_Decimal:
            case SpecialType.System_Int32:
            case SpecialType.System_Int64:
            case SpecialType.System_Boolean:
                sb.Append(p).Append("loc.Add(\"").Append(name).Append("\", ").Append(valueExpr).AppendLine(");");
                return;
            case SpecialType.System_Single:
            case SpecialType.System_Double:
                sb.Append(p).Append("loc.Add(\"").Append(name).Append("\", (decimal)").Append(valueExpr).AppendLine(");");
                return;
            case SpecialType.System_String:
                sb.Append(p).Append("loc.Add(\"").Append(name).Append("\", ").Append(valueExpr).AppendLine(");");
                return;
        }

        if (IsTypeOrDerived(type, FullyQualifiedLocStringMetadataName))
        {
            sb.Append(p).Append("loc.Add(\"").Append(name).Append("\", ").Append(valueExpr).AppendLine(");");
            return;
        }

        if (IsIListOfString(type))
        {
            sb.Append(p).Append("loc.Add(\"").Append(name).Append("\", ").Append(valueExpr).AppendLine(");");
            return;
        }

        if (IsNumericLike(type))
        {
            sb.Append(p).Append("loc.Add(\"").Append(name)
                .Append("\", global::System.Convert.ToDecimal(")
                .Append(valueExpr)
                .AppendLine("));");
            return;
        }

        sb.Append(p).Append("loc.AddObj(\"").Append(name).Append("\", ").Append(valueExpr).AppendLine(");");
    }

    private static bool IsNumericLike(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Enum)
            return true;

        return type.SpecialType is SpecialType.System_Byte
            or SpecialType.System_SByte
            or SpecialType.System_Int16
            or SpecialType.System_UInt16
            or SpecialType.System_Int32
            or SpecialType.System_UInt32
            or SpecialType.System_Int64
            or SpecialType.System_UInt64
            or SpecialType.System_Single
            or SpecialType.System_Double
            or SpecialType.System_Decimal;
    }

    private static bool IsIListOfString(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol named)
            return false;

        foreach (var iface in named.AllInterfaces)
        {
            if (!iface.IsGenericType)
                continue;
            if (iface.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) != FullyQualifiedIListMetadataName)
                continue;
            if (iface.TypeArguments.Length != 1)
                continue;
            if (iface.TypeArguments[0].SpecialType == SpecialType.System_String)
                return true;
        }

        return false;
    }

    private static bool IsTypeOrDerived(ITypeSymbol type, string baseTypeMetadataName)
    {
        if (type is not INamedTypeSymbol named)
            return false;

        return InheritsFromMetadataName(named, baseTypeMetadataName);
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

    private static bool InheritsFromMetadataName(INamedTypeSymbol type, string fullyQualifiedMetadataName)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            if (current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == fullyQualifiedMetadataName)
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

    private sealed class ComponentStateRule
    {
        public ComponentStateRule(IPropertySymbol property, string propertyName, INamedTypeSymbol? generatorType,
            ImmutableArray<TypedConstant> constructorArgs)
        {
            Property = property;
            PropertyName = propertyName;
            GeneratorType = generatorType;
            ConstructorArgs = constructorArgs;
        }

        public IPropertySymbol Property { get; }
        public string PropertyName { get; }
        public INamedTypeSymbol? GeneratorType { get; }
        public ImmutableArray<TypedConstant> ConstructorArgs { get; }
    }
}


