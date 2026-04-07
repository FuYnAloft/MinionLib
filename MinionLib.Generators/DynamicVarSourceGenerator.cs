#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
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
    private const string LocArgAttributeMetadataName = "MinionLib.Component.Core.LocArgAttribute";
    private const string NotLocArgAttributeMetadataName = "MinionLib.Component.Core.NotLocArgAttribute";
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
            .Where(static x => x.HasValue)
            .Select(static (x, _) => x.GetValueOrDefault());

        var collected = typeCandidates
            .Collect()
            .Select(static (items, _) => new EquatableArray<TypeGenerationData>(items));

        context.RegisterSourceOutput(collected, static (spc, types) => Emit(spc, types.Items));
    }

    private static TypeGenerationData? GetTypeCandidate(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classSyntax)
            return null;

        if (context.SemanticModel.GetDeclaredSymbol(classSyntax) is not INamedTypeSymbol type)
            return null;

        if (type.TypeKind != TypeKind.Class)
            return null;

        var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (typeName == FullyQualifiedCardComponentMetadataName)
            return null;

        if (!InheritsFromMetadataName(type, FullyQualifiedCardComponentMetadataName))
            return null;

        var ownRules = GetOwnComponentStateRules(type);
        if (ownRules.Length == 0)
            return null;

        var allRules = GetAllComponentStateRules(type);
        if (allRules.Length == 0)
            return null;

        var invalidRules = allRules
            .Where(static r => r.GeneratorTypeName != null && !r.GeneratorTypeIsDynamicVar)
            .Select(static r => new InvalidDynamicVarRuleData(r.PropertyName, r.GeneratorTypeName!, r.Location))
            .OrderBy(static x => x.PropertyName, StringComparer.Ordinal)
            .ToImmutableArray();

        var namespaceName = type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString();

        return new TypeGenerationData(
            HintName: BuildHintName(type),
            TypeDisplayName: typeName,
            NamespaceName: namespaceName,
            ContainingTypes: BuildContainingTypeChain(type),
            IsPartial: IsPartial(type),
            TypeLocation: GetSourceLocation(type),
            OwnRules: ownRules,
            AllRules: allRules,
            InvalidRules: invalidRules);
    }

    private static ImmutableArray<ComponentStateRuleData> GetOwnComponentStateRules(INamedTypeSymbol type)
    {
        return type.GetMembers()
            .OfType<IPropertySymbol>()
            .Select(BuildRule)
            .Where(static r => r.HasValue)
            .Select(static r => r.GetValueOrDefault())
            .OrderBy(static r => r.PropertyName, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    private static ImmutableArray<ComponentStateRuleData> GetAllComponentStateRules(INamedTypeSymbol type)
    {
        var map = new Dictionary<string, ComponentStateRuleData>(StringComparer.Ordinal);
        var chain = new Stack<INamedTypeSymbol>();
        for (var current = type; current != null; current = current.BaseType)
            chain.Push(current);

        while (chain.Count > 0)
        {
            var node = chain.Pop();
            foreach (var property in node.GetMembers().OfType<IPropertySymbol>())
            {
                var rule = BuildRule(property);
                if (!rule.HasValue)
                    continue;

                var ruleData = rule.GetValueOrDefault();

                if (!IsAccessibleFromType(property, type))
                    continue;

                map[ruleData.PropertyName] = ruleData;
            }
        }

        return map.Values
            .OrderBy(static r => r.PropertyName, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    private static ComponentStateRuleData? BuildRule(IPropertySymbol property)
    {
        if (property.IsStatic || property.GetMethod == null || property.Parameters.Length != 0)
            return null;

        var attributes = property.GetAttributes();
        var componentStateAttribute = attributes.FirstOrDefault(IsComponentStateAttribute);
        var locArgAttribute = attributes.FirstOrDefault(IsLocArgAttribute);
        var hasNotLocArg = attributes.Any(IsNotLocArgAttribute);

        if (componentStateAttribute == null && locArgAttribute == null)
            return null;

        string? generatorTypeName = null;
        var generatorTypeIsDynamicVar = false;
        var constructorArgs = ImmutableArray<string>.Empty;

        if (componentStateAttribute != null &&
            !TryExtractGenerator(componentStateAttribute, out generatorTypeName, out generatorTypeIsDynamicVar, out constructorArgs))
            return null;

        var hasLocArgName = TryExtractLocArgName(locArgAttribute, out var locArgName);
        var smartArgName = hasLocArgName && !string.IsNullOrWhiteSpace(locArgName)
            ? locArgName
            : property.Name;

        var hasNonGenericComponentState = componentStateAttribute != null && !IsGenericComponentStateAttribute(componentStateAttribute);
        var includeInSmartArgs = !hasNotLocArg && (hasNonGenericComponentState || locArgAttribute != null);

        return new ComponentStateRuleData(
            PropertyName: property.Name,
            SmartArgName: smartArgName,
            TypeInfo: PropertyTypeInfo.FromType(property.Type),
            HasComponentState: componentStateAttribute != null,
            IncludeInSmartArgs: includeInSmartArgs,
            GeneratorTypeName: generatorTypeName,
            GeneratorTypeIsDynamicVar: generatorTypeIsDynamicVar,
            ConstructorArgCode: constructorArgs,
            Location: GetSourceLocation(property));
    }

    private static bool TryExtractLocArgName(AttributeData? attribute, out string name)
    {
        name = string.Empty;
        if (attribute == null)
            return false;

        if (attribute.ConstructorArguments.Length == 0)
            return true;

        var arg = attribute.ConstructorArguments[0];
        if (arg.Kind == TypedConstantKind.Primitive && arg.Value is string provided && !string.IsNullOrWhiteSpace(provided))
            name = provided;

        return true;
    }

    private static Location? GetSourceLocation(ISymbol symbol)
    {
        return symbol.Locations.FirstOrDefault(static l => l != null && l.IsInSource);
    }

    private static bool TryExtractGenerator(
        AttributeData attribute,
        out string? generatorTypeName,
        out bool generatorTypeIsDynamicVar,
        out ImmutableArray<string> constructorArgs)
    {
        generatorTypeName = null;
        generatorTypeIsDynamicVar = false;
        constructorArgs = ImmutableArray<string>.Empty;

        var attrClass = attribute.AttributeClass;
        if (attrClass == null)
            return false;

        if (attrClass.IsGenericType && attrClass.TypeArguments.Length == 1)
        {
            if (attrClass.TypeArguments[0] is INamedTypeSymbol typeArg)
            {
                generatorTypeName = typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                generatorTypeIsDynamicVar = InheritsFromMetadataName(typeArg, FullyQualifiedDynamicVarMetadataName);
            }

            constructorArgs = NormalizeConstructorArgs(attribute.ConstructorArguments, 0)
                .Select(TypedConstantToCode)
                .ToImmutableArray();
            return true;
        }

        constructorArgs = NormalizeConstructorArgs(attribute.ConstructorArguments, 0)
            .Select(TypedConstantToCode)
            .ToImmutableArray();
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
        var originalName = attribute.AttributeClass?.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return originalName == "global::" + ComponentStateAttributeMetadataName
               || originalName == "global::MinionLib.Component.Core.ComponentStateAttribute<T>";
    }

    private static bool IsGenericComponentStateAttribute(AttributeData attribute)
    {
        var originalName = attribute.AttributeClass?.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return originalName == "global::" + ComponentStateGenericAttributeMetadataName
               || originalName == "global::MinionLib.Component.Core.ComponentStateAttribute<T>";
    }

    private static bool IsLocArgAttribute(AttributeData attribute)
    {
        var originalName = attribute.AttributeClass?.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return originalName == "global::" + LocArgAttributeMetadataName;
    }

    private static bool IsNotLocArgAttribute(AttributeData attribute)
    {
        var originalName = attribute.AttributeClass?.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return originalName == "global::" + NotLocArgAttributeMetadataName;
    }

    private static bool IsAccessibleFromType(IPropertySymbol property, INamedTypeSymbol targetType)
    {
        if (SymbolEqualityComparer.Default.Equals(property.ContainingType, targetType))
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

    private static void Emit(SourceProductionContext context, ImmutableArray<TypeGenerationData> types)
    {
        foreach (var type in types.OrderBy(static x => x.TypeDisplayName, StringComparer.Ordinal))
        {
            if (!type.IsPartial)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    CardComponentTypeMustBePartial,
                    type.TypeLocation,
                    type.TypeDisplayName));
                continue;
            }

            foreach (var invalid in type.InvalidRules)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DynamicVarTypeMustInheritDynamicVar,
                    invalid.Location,
                    invalid.PropertyName,
                    invalid.GeneratorTypeName));
            }

            var smartVarRules = type.AllRules
                .Where(static r => r.HasComponentState && r.GeneratorTypeName != null)
                .OrderBy(static r => r.PropertyName, StringComparer.Ordinal)
                .ToImmutableArray();

            var smartArgRules = type.OwnRules
                .Where(static r => r.IncludeInSmartArgs)
                .OrderBy(static r => r.PropertyName, StringComparer.Ordinal)
                .ToImmutableArray();

            var source = BuildSource(type, smartVarRules, smartArgRules);
            context.AddSource(type.HintName, SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string BuildSource(
        TypeGenerationData type,
        ImmutableArray<ComponentStateRuleData> smartVarRules,
        ImmutableArray<ComponentStateRuleData> smartArgRules)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");

        if (!string.IsNullOrWhiteSpace(type.NamespaceName))
        {
            sb.Append("namespace ").Append(type.NamespaceName).AppendLine(";");
            sb.AppendLine();
        }

        AppendContainingTypeDeclarations(sb, type.ContainingTypes);

        if (smartVarRules.Length > 0)
        {
            sb.AppendLine("    protected override global::System.Collections.Generic.IEnumerable<global::MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar> SmartVars =>");
            sb.AppendLine("    [");
            for (var i = 0; i < smartVarRules.Length; i++)
            {
                var rule = smartVarRules[i];
                sb.Append("        new ")
                    .Append(rule.GeneratorTypeName)
                    .Append("(\"")
                    .Append(rule.PropertyName)
                    .Append("\", global::System.Convert.ToDecimal(this.")
                    .Append(rule.PropertyName)
                    .Append(")");

                for (var j = 0; j < rule.ConstructorArgCode.Length; j++)
                    sb.Append(", ").Append(rule.ConstructorArgCode[j]);

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

            for (var i = 0; i < smartArgRules.Length; i++)
                EmitSmartArg(sb, smartArgRules[i]);

            sb.AppendLine("    }");
        }

        AppendContainingTypeClosures(sb, type.ContainingTypes.Length);
        return sb.ToString();
    }

    private static void EmitSmartArg(StringBuilder sb, ComponentStateRuleData rule)
    {
        var valueExpr = "this." + rule.PropertyName;
        if (rule.TypeInfo.IsNullable)
        {
            var local = "__v_" + rule.PropertyName;
            sb.Append("        var ").Append(local).Append(" = ").Append(valueExpr).AppendLine(";");
            sb.Append("        if (").Append(local).AppendLine(" == null)");
            sb.Append("            loc.Add(\"").Append(rule.SmartArgName).AppendLine("\", 0m);");
            sb.AppendLine("        else");
            EmitNonNullSmartArg(sb, rule.SmartArgName, local, rule.TypeInfo, 3);
            return;
        }

        EmitNonNullSmartArg(sb, rule.SmartArgName, valueExpr, rule.TypeInfo, 2);
    }

    private static void EmitNonNullSmartArg(StringBuilder sb, string name, string valueExpr, PropertyTypeInfo typeInfo, int indent)
    {
        var p = new string(' ', indent * 4);

        if (typeInfo.IsDynamicVar)
        {
            sb.Append(p).Append("loc.Add(").Append(valueExpr).AppendLine(");");
            return;
        }

        switch (typeInfo.SpecialType)
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

        if (typeInfo.IsLocString || typeInfo.IsIListString)
        {
            sb.Append(p).Append("loc.Add(\"").Append(name).Append("\", ").Append(valueExpr).AppendLine(");");
            return;
        }

        if (typeInfo.IsNumericLike)
        {
            sb.Append(p).Append("loc.Add(\"").Append(name)
                .Append("\", global::System.Convert.ToDecimal(")
                .Append(valueExpr)
                .AppendLine("));");
            return;
        }

        sb.Append(p).Append("loc.AddObj(\"").Append(name).Append("\", ").Append(valueExpr).AppendLine(");");
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

    private static bool IsPartial(INamedTypeSymbol type)
    {
        return type.DeclaringSyntaxReferences
            .Select(static r => r.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .Any(static s => s.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));
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
                       + PrimitiveToCode(((INamedTypeSymbol)constant.Type).EnumUnderlyingType, constant.Value);
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
            float f => f.ToString("R", CultureInfo.InvariantCulture) + "f",
            double d => d.ToString("R", CultureInfo.InvariantCulture) + "d",
            decimal m => m.ToString(CultureInfo.InvariantCulture) + "m",
            byte bt => bt.ToString(CultureInfo.InvariantCulture),
            sbyte sb => "(sbyte)" + sb.ToString(CultureInfo.InvariantCulture),
            short s16 => "(short)" + s16.ToString(CultureInfo.InvariantCulture),
            ushort u16 => "(ushort)" + u16.ToString(CultureInfo.InvariantCulture),
            int i => i.ToString(CultureInfo.InvariantCulture),
            uint u => u.ToString(CultureInfo.InvariantCulture) + "u",
            long l => l.ToString(CultureInfo.InvariantCulture) + "L",
            ulong ul => ul.ToString(CultureInfo.InvariantCulture) + "UL",
            _ when type?.SpecialType == SpecialType.System_String => "@\"" + value.ToString()!.Replace("\"", "\"\"") + "\"",
            _ => value.ToString() ?? "default"
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

    private readonly record struct TypeGenerationData(
        string HintName,
        string TypeDisplayName,
        string? NamespaceName,
        ImmutableArray<ContainingTypeData> ContainingTypes,
        bool IsPartial,
        Location? TypeLocation,
        ImmutableArray<ComponentStateRuleData> OwnRules,
        ImmutableArray<ComponentStateRuleData> AllRules,
        ImmutableArray<InvalidDynamicVarRuleData> InvalidRules);

    private readonly record struct ComponentStateRuleData(
        string PropertyName,
        string SmartArgName,
        PropertyTypeInfo TypeInfo,
        bool HasComponentState,
        bool IncludeInSmartArgs,
        string? GeneratorTypeName,
        bool GeneratorTypeIsDynamicVar,
        ImmutableArray<string> ConstructorArgCode,
        Location? Location);

    private readonly record struct InvalidDynamicVarRuleData(
        string PropertyName,
        string GeneratorTypeName,
        Location? Location);

    private readonly record struct PropertyTypeInfo(
        SpecialType SpecialType,
        bool IsNullable,
        bool IsReferenceType,
        bool IsDynamicVar,
        bool IsLocString,
        bool IsIListString,
        bool IsNumericLike)
    {
        public static PropertyTypeInfo FromType(ITypeSymbol type)
        {
            var isDynamicVar = type is INamedTypeSymbol namedDynamic && InheritsFromMetadataName(namedDynamic, FullyQualifiedDynamicVarMetadataName);
            var isLocString = type is INamedTypeSymbol namedLoc && InheritsFromMetadataName(namedLoc, FullyQualifiedLocStringMetadataName);
            var isIListString = IsIListOfString(type);
            var isNullable = type.IsReferenceType || type.NullableAnnotation == NullableAnnotation.Annotated;
            var isNumericLike = ComputeNumericLike(type);

            return new PropertyTypeInfo(
                type.SpecialType,
                isNullable,
                type.IsReferenceType,
                isDynamicVar,
                isLocString,
                isIListString,
                isNumericLike);
        }

        private static bool ComputeNumericLike(ITypeSymbol type)
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
    }

    private readonly record struct ContainingTypeData(
        string Keyword,
        string Name,
        ImmutableArray<string> TypeParameters);
}
