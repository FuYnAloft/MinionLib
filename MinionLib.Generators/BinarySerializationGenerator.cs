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
public sealed class BinarySerializationGenerator : IIncrementalGenerator
{
    private const string GeneratedSerializableMetadataName = "MinionLib.Component.Interfaces.IGeneratedBinarySerializable";
    private const string ComponentStateAttributeMetadataName = "MinionLib.Component.Core.ComponentStateAttribute";
    private const string ComponentStateGenericAttributeMetadataName = "MinionLib.Component.Core.ComponentStateAttribute`1";
    private const string NoGeneratedSerializationMetadataName = "MinionLib.Component.Core.NoGeneratedSerializationAttribute";
    private const string CardComponentMetadataName = "MinionLib.Component.CardComponent";
    private const string ICardComponentMetadataName = "MinionLib.Component.Interfaces.ICardComponent";
    private const string PacketSerializableMetadataName = "MegaCrit.Sts2.Core.Multiplayer.Serialization.IPacketSerializable";
    private const string FullyQualifiedGeneratedSerializableMetadataName = "global::" + GeneratedSerializableMetadataName;
    private const string FullyQualifiedComponentStateAttributeMetadataName = "global::" + ComponentStateAttributeMetadataName;
    private const string FullyQualifiedCardComponentMetadataName = "global::" + CardComponentMetadataName;
    private const string FullyQualifiedICardComponentMetadataName = "global::" + ICardComponentMetadataName;
    private const string FullyQualifiedPacketSerializableMetadataName = "global::" + PacketSerializableMetadataName;

    private static readonly DiagnosticDescriptor ImplementationMustBeClass = new(
        id: "MLSG100",
        title: "IGeneratedBinarySerializable implementation must be a class",
        messageFormat: "Type '{0}' implements IGeneratedBinarySerializable but is not a class",
        category: "MinionLib.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ParameterlessCtorMustBePublic = new(
        id: "MLSG103",
        title: "Parameterless constructor must be public",
        messageFormat: "Type '{0}' defines a parameterless constructor but it is not public",
        category: "MinionLib.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor JsonFallbackWarning = new(
        id: "MLSG104",
        title: "ComponentState property falls back to JSON serialization",
        messageFormat: "Property '{0}' on type '{1}' uses JSON fallback serialization",
        category: "MinionLib.Generators",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor TypeMustBePartialForGeneration = new(
        id: "MLSG102",
        title: "Type must be partial for generated serialization",
        messageFormat: "Type '{0}' is eligible for generated serialization but is not declared partial; generation skipped",
        category: "MinionLib.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ICardComponentShouldBeSealedOrAbstract = new(
        id: "MLSG202",
        title: "ICardComponent implementation should be sealed or abstract",
        messageFormat: "Type '{0}' implements ICardComponent and should be declared sealed or abstract",
        category: "MinionLib.Generators",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var typeCandidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is TypeDeclarationSyntax { BaseList: not null },
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
        if (context.Node is not TypeDeclarationSyntax typeSyntax)
            return null;

        if (context.SemanticModel.GetDeclaredSymbol(typeSyntax) is not INamedTypeSymbol type)
            return null;

        if (!Implements(type, FullyQualifiedGeneratedSerializableMetadataName))
            return null;

        if (type.TypeKind == TypeKind.Interface)
            return null;

        var namespaceName = type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString();
        var isClass = type.TypeKind == TypeKind.Class;
        var properties = isClass ? GetStateProperties(type) : ImmutableArray<PropertyGenerationData>.Empty;
        var parameterlessCtorAccessibility = GetParameterlessCtorAccessibility(type);

        return new TypeGenerationData(
            HintName: BuildHintName(type),
            TypeDisplayName: type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            NamespaceName: namespaceName,
            ContainingTypes: BuildContainingTypeChain(type),
            IsClass: isClass,
            ParameterlessCtorAccessibility: parameterlessCtorAccessibility,
            HasNoGeneratedSerializationAttribute: HasAttribute(type, NoGeneratedSerializationMetadataName),
            HasExplicitSerializationMethods: HasExplicitSerializationMethods(type),
            IsPartial: IsPartial(type),
            IsAbstract: type.IsAbstract,
            IsSealed: type.IsSealed,
            ImplementsICardComponent: Implements(type, FullyQualifiedICardComponentMetadataName),
            IsCardComponentDerived: InheritsFrom(type, FullyQualifiedCardComponentMetadataName),
            TypeLocation: GetSourceLocation(type),
            Properties: properties);
    }

    private static Location? GetSourceLocation(ISymbol symbol)
    {
        return symbol.Locations.FirstOrDefault(static l => l != null && l.IsInSource);
    }

    private static ImmutableArray<PropertyGenerationData> GetStateProperties(INamedTypeSymbol type)
    {
        return type.GetMembers().OfType<IPropertySymbol>()
            .Where(static p => !p.IsStatic)
            .Where(static p => p.GetMethod != null && p.SetMethod != null)
            .Where(static p => p.GetAttributes().Any(IsComponentStateAttribute))
            .OrderBy(static p => p.Name, StringComparer.Ordinal)
            .Select(static p => new PropertyGenerationData(
                p.Name,
                GetSourceLocation(p),
                TypeShapeData.FromSymbol(p.Type)))
            .ToImmutableArray();
    }

    private static bool IsComponentStateAttribute(AttributeData attribute)
    {
        var name = attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (name == FullyQualifiedComponentStateAttributeMetadataName)
            return true;

        var original = attribute.AttributeClass?.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return original == "global::MinionLib.Component.Core.ComponentStateAttribute<T>";
    }

    private static void Emit(SourceProductionContext context, ImmutableArray<TypeGenerationData> types)
    {
        foreach (var type in types.OrderBy(static x => x.TypeDisplayName, StringComparer.Ordinal))
        {
            if (!type.IsClass)
            {
                context.ReportDiagnostic(Diagnostic.Create(ImplementationMustBeClass, type.TypeLocation, type.TypeDisplayName));
                continue;
            }

            if (type.ImplementsICardComponent && !type.IsAbstract && !type.IsSealed)
                context.ReportDiagnostic(Diagnostic.Create(ICardComponentShouldBeSealedOrAbstract, type.TypeLocation, type.TypeDisplayName));

            foreach (var property in type.Properties)
            {
                if (!property.Type.UsesJsonFallback)
                    continue;

                context.ReportDiagnostic(Diagnostic.Create(
                    JsonFallbackWarning,
                    property.PropertyLocation,
                    property.PropertyName,
                    type.TypeDisplayName));
            }

            if (type.HasNoGeneratedSerializationAttribute)
                continue;

            var needsCtorGeneration = !type.IsAbstract && type.ParameterlessCtorAccessibility is null;
            var needsMethodGeneration = !type.HasExplicitSerializationMethods && type.Properties.Length > 0;
            if (!needsCtorGeneration && !needsMethodGeneration)
                continue;

            if (needsMethodGeneration
                && !type.IsAbstract
                && type.ParameterlessCtorAccessibility is not null
                && type.ParameterlessCtorAccessibility != Accessibility.Public)
            {
                context.ReportDiagnostic(Diagnostic.Create(ParameterlessCtorMustBePublic, type.TypeLocation, type.TypeDisplayName));
            }

            if (!type.IsPartial)
            {
                context.ReportDiagnostic(Diagnostic.Create(TypeMustBePartialForGeneration, type.TypeLocation, type.TypeDisplayName));
                continue;
            }

            var source = BuildSource(type, needsCtorGeneration, needsMethodGeneration);
            context.AddSource(type.HintName, SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string BuildSource(TypeGenerationData type, bool generateCtor, bool generateMethods)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("#pragma warning disable CS0618");
        sb.AppendLine("#pragma warning disable CS8618");

        if (!string.IsNullOrWhiteSpace(type.NamespaceName))
        {
            sb.Append("namespace ").Append(type.NamespaceName).AppendLine(";");
            sb.AppendLine();
        }

        AppendContainingTypeDeclarations(sb, type.ContainingTypes);

        if (generateCtor)
        {
            sb.AppendLine();
            sb.AppendLine("    [global::System.Obsolete(\"For deserialization only\", false)]");
            sb.Append("    public ").Append(GetInnermostTypeName(type.ContainingTypes)).AppendLine("() { }");
            sb.AppendLine();
        }

        if (generateMethods)
        {
            var methodPrefix = type.IsCardComponentDerived ? "public override" : "public";
            var uniqueId = 0;

            sb.Append("    ").Append(methodPrefix)
                .AppendLine(" void Serialize(global::System.Buffers.ArrayBufferWriter<byte> writer)");
            sb.AppendLine("    {");
            if (type.IsCardComponentDerived)
                sb.AppendLine("        base.Serialize(writer);");
            for (var i = 0; i < type.Properties.Length; i++)
                EmitSerializeForType(sb, type.Properties[i].Type, "this." + type.Properties[i].PropertyName, 2, ref uniqueId);
            sb.AppendLine("    }");
            sb.AppendLine();

            sb.Append("    ").Append(methodPrefix)
                .AppendLine(" bool Deserialize(ref global::System.ReadOnlySpan<byte> reader)");
            sb.AppendLine("    {");
            if (type.IsCardComponentDerived)
                sb.AppendLine("        if (!base.Deserialize(ref reader)) return false;");
            for (var i = 0; i < type.Properties.Length; i++)
                EmitDeserializeForType(sb, type.Properties[i].Type, "this." + type.Properties[i].PropertyName, 2, ref uniqueId);

            sb.AppendLine("        return true;");
            sb.AppendLine("    }");
        }

        AppendContainingTypeClosures(sb, type.ContainingTypes.Length);
        return sb.ToString();
    }

    private static void EmitSerializeForType(StringBuilder sb, TypeShapeData type, string expr, int indent, ref int id)
    {
        var p = Indent(indent);

        if (type.HasNullableWrapper && type.NullableUnderlying != null)
        {
            var has = "__has_" + id++;
            sb.Append(p).Append("var ").Append(has).Append(" = ").Append(expr).AppendLine(" != null;");
            sb.Append(p).Append("global::MinionLib.Component.Core.SerializationUtils.WriteBoolean(writer, ").Append(has).AppendLine(");");
            sb.Append(p).Append("if (").Append(has).AppendLine(")");
            sb.Append(p).AppendLine("{");
            EmitSerializeForType(sb, type.NullableUnderlying, EnsureNullForgiven(expr), indent + 1, ref id);
            sb.Append(p).AppendLine("}");
            return;
        }

        switch (type.Kind)
        {
            case TypeSerializationKind.Array when type.ElementType != null:
            {
                var len = "__len_" + id++;
                var i = "__i_" + id++;
                var notNullExpr = EnsureNullForgiven(expr);
                sb.Append(p).Append("if (").Append(expr).AppendLine(" == null)");
                sb.Append(p).AppendLine("{");
                sb.Append(p).AppendLine("    global::MinionLib.Component.Core.SerializationUtils.WriteInt32(writer, -1);");
                sb.Append(p).AppendLine("}");
                sb.Append(p).AppendLine("else");
                sb.Append(p).AppendLine("{");
                sb.Append(p).Append("    var ").Append(len).Append(" = ").Append(notNullExpr).AppendLine(".Length;");
                sb.Append(p).Append("    global::MinionLib.Component.Core.SerializationUtils.WriteInt32(writer, ").Append(len).AppendLine(");");
                sb.Append(p).Append("    for (var ").Append(i).Append(" = 0; ").Append(i).Append(" < ").Append(len).Append("; ").Append(i)
                    .AppendLine("++)");
                sb.Append(p).AppendLine("    {");
                EmitSerializeForType(sb, type.ElementType, notNullExpr + "[" + i + "]", indent + 2, ref id);
                sb.Append(p).AppendLine("    }");
                sb.Append(p).AppendLine("}");
                return;
            }
            case TypeSerializationKind.List when type.ElementType != null:
            {
                var len = "__len_" + id++;
                var i = "__i_" + id++;
                var notNullExpr = EnsureNullForgiven(expr);
                sb.Append(p).Append("if (").Append(expr).AppendLine(" == null)");
                sb.Append(p).AppendLine("{");
                sb.Append(p).AppendLine("    global::MinionLib.Component.Core.SerializationUtils.WriteInt32(writer, -1);");
                sb.Append(p).AppendLine("}");
                sb.Append(p).AppendLine("else");
                sb.Append(p).AppendLine("{");
                sb.Append(p).Append("    var ").Append(len).Append(" = ").Append(notNullExpr).AppendLine(".Count;");
                sb.Append(p).Append("    global::MinionLib.Component.Core.SerializationUtils.WriteInt32(writer, ").Append(len).AppendLine(");");
                sb.Append(p).Append("    for (var ").Append(i).Append(" = 0; ").Append(i).Append(" < ").Append(len).Append("; ").Append(i)
                    .AppendLine("++)");
                sb.Append(p).AppendLine("    {");
                EmitSerializeForType(sb, type.ElementType, notNullExpr + "[" + i + "]", indent + 2, ref id);
                sb.Append(p).AppendLine("    }");
                sb.Append(p).AppendLine("}");
                return;
            }
            case TypeSerializationKind.String:
                sb.Append(p).Append("global::MinionLib.Component.Core.SerializationUtils.WriteString(writer, ").Append(expr).AppendLine(");");
                return;
            case TypeSerializationKind.Primitive:
                EmitPrimitiveSerialize(sb, type.PrimitiveKind, expr, indent);
                return;
            case TypeSerializationKind.Enum when type.EnumUnderlying != null:
                EmitSerializeForType(sb, type.EnumUnderlying, "(" + type.EnumUnderlying.DisplayName + ")" + expr, indent, ref id);
                return;
            case TypeSerializationKind.Serializable:
                sb.Append(p).Append("if (").Append(expr).AppendLine(" == null)");
                sb.Append(p).AppendLine("{");
                sb.Append(p).AppendLine("    global::MinionLib.Component.Core.SerializationUtils.WriteBoolean(writer, false);");
                sb.Append(p).AppendLine("}");
                sb.Append(p).AppendLine("else");
                sb.Append(p).AppendLine("{");
                sb.Append(p).AppendLine("    global::MinionLib.Component.Core.SerializationUtils.WriteBoolean(writer, true);");
                sb.Append(p).Append("    global::MinionLib.Component.Core.SerializationUtils.WriteSerializableBlock(writer, ")
                    .Append(EnsureNullForgiven(expr)).AppendLine(");");
                sb.Append(p).AppendLine("}");
                return;
            case TypeSerializationKind.PacketSerializable:
                sb.Append(p).Append("if ((object?)").Append(expr).AppendLine(" == null)");
                sb.Append(p).AppendLine("{");
                sb.Append(p).AppendLine("    global::MinionLib.Component.Core.SerializationUtils.WriteBoolean(writer, false);");
                sb.Append(p).AppendLine("}");
                sb.Append(p).AppendLine("else");
                sb.Append(p).AppendLine("{");
                sb.Append(p).AppendLine("    global::MinionLib.Component.Core.SerializationUtils.WriteBoolean(writer, true);");
                sb.Append(p).Append("    global::MinionLib.Component.Core.SerializationUtils.WriteIPacketSerializable<")
                    .Append(type.DisplayName).Append(">(writer, ").Append(EnsureNullForgiven(expr)).AppendLine(");");
                sb.Append(p).AppendLine("}");
                return;
            default:
                sb.Append(p).Append("global::MinionLib.Component.Core.SerializationUtils.WriteJson<")
                    .Append(type.DisplayName).Append(">(writer, ").Append(EnsureNullForgiven(expr)).AppendLine(");");
                return;
        }
    }

    private static void EmitDeserializeForType(StringBuilder sb, TypeShapeData type, string targetExpr, int indent, ref int id)
    {
        var p = Indent(indent);

        if (type.HasNullableWrapper && type.NullableUnderlying != null)
        {
            var has = "__has_" + id++;
            sb.Append(p).Append("if (!global::MinionLib.Component.Core.SerializationUtils.TryReadBoolean(ref reader, out var ")
                .Append(has).AppendLine("))");
            sb.Append(p).AppendLine("    return false;");
            sb.Append(p).Append("if (!").Append(has).AppendLine(")");
            sb.Append(p).AppendLine("{");
            sb.Append(p).Append(targetExpr).AppendLine(" = default;");
            sb.Append(p).AppendLine("}");
            sb.Append(p).AppendLine("else");
            sb.Append(p).AppendLine("{");
            EmitDeserializeForType(sb, type.NullableUnderlying, targetExpr, indent + 1, ref id);
            sb.Append(p).AppendLine("}");
            return;
        }

        switch (type.Kind)
        {
            case TypeSerializationKind.Array when type.ElementType != null:
            {
                var len = "__len_" + id++;
                var arr = "__arr_" + id++;
                var i = "__i_" + id++;
                var item = "__item_" + id++;

                sb.Append(p).Append("if (!global::MinionLib.Component.Core.SerializationUtils.TryReadInt32(ref reader, out var ")
                    .Append(len).AppendLine("))");
                sb.Append(p).AppendLine("    return false;");
                sb.Append(p).Append("if (").Append(len).AppendLine(" < -1)");
                sb.Append(p).AppendLine("    return false;");
                sb.Append(p).Append("if (").Append(len).AppendLine(" == -1)");
                sb.Append(p).AppendLine("{");
                if (type.CanAssignNullOnDeserialize)
                    sb.Append(p).Append("    ").Append(targetExpr).AppendLine(" = null;");
                else
                    sb.Append(p).AppendLine("    return false;");
                sb.Append(p).AppendLine("}");
                sb.Append(p).AppendLine("else");
                sb.Append(p).AppendLine("{");
                sb.Append(p).Append("    var ").Append(arr).Append(" = ").Append(BuildJaggedArrayCreationExpression(type, len)).AppendLine(";");
                sb.Append(p).Append("    for (var ").Append(i).Append(" = 0; ").Append(i).Append(" < ").Append(len).Append("; ").Append(i)
                    .AppendLine("++)");
                sb.Append(p).AppendLine("    {");
                sb.Append(p).Append("        ").Append(type.ElementType.DisplayName).Append(' ').Append(item)
                    .Append(GetLocalInitializer(type.ElementType)).AppendLine();
                EmitDeserializeForType(sb, type.ElementType, item, indent + 2, ref id);
                sb.Append(p).Append("        ").Append(arr).Append("[").Append(i).Append("] = ").Append(item).AppendLine(";");
                sb.Append(p).AppendLine("    }");
                sb.Append(p).Append("    ").Append(targetExpr).Append(" = ").Append(arr).AppendLine(";");
                sb.Append(p).AppendLine("}");
                return;
            }
            case TypeSerializationKind.List when type.ElementType != null && type.ListTypeName != null:
            {
                var len = "__len_" + id++;
                var list = "__list_" + id++;
                var i = "__i_" + id++;
                var item = "__item_" + id++;

                sb.Append(p).Append("if (!global::MinionLib.Component.Core.SerializationUtils.TryReadInt32(ref reader, out var ")
                    .Append(len).AppendLine("))");
                sb.Append(p).AppendLine("    return false;");
                sb.Append(p).Append("if (").Append(len).AppendLine(" < -1)");
                sb.Append(p).AppendLine("    return false;");
                sb.Append(p).Append("if (").Append(len).AppendLine(" == -1)");
                sb.Append(p).AppendLine("{");
                if (type.CanAssignNullOnDeserialize)
                    sb.Append(p).Append("    ").Append(targetExpr).AppendLine(" = null;");
                else
                    sb.Append(p).AppendLine("    return false;");
                sb.Append(p).AppendLine("}");
                sb.Append(p).AppendLine("else");
                sb.Append(p).AppendLine("{");
                sb.Append(p).Append("    var ").Append(list).Append(" = new ").Append(type.ListTypeName).Append("(").Append(len).AppendLine(");");
                sb.Append(p).Append("    for (var ").Append(i).Append(" = 0; ").Append(i).Append(" < ").Append(len).Append("; ").Append(i)
                    .AppendLine("++)");
                sb.Append(p).AppendLine("    {");
                sb.Append(p).Append("        ").Append(type.ElementType.DisplayName).Append(' ').Append(item)
                    .Append(GetLocalInitializer(type.ElementType)).AppendLine();
                EmitDeserializeForType(sb, type.ElementType, item, indent + 2, ref id);
                sb.Append(p).Append("        ").Append(list).Append(".Add(").Append(item).AppendLine(");");
                sb.Append(p).AppendLine("    }");
                sb.Append(p).Append("    ").Append(targetExpr).Append(" = ").Append(list).AppendLine(";");
                sb.Append(p).AppendLine("}");
                return;
            }
            case TypeSerializationKind.String:
            {
                var strVar = "__str_" + id++;
                sb.Append(p).Append("if (!global::MinionLib.Component.Core.SerializationUtils.TryReadString(ref reader, out var ")
                    .Append(strVar).AppendLine("))");
                sb.Append(p).AppendLine("    return false;");
                if (type.CanAssignNullOnDeserialize)
                {
                    sb.Append(p).Append(targetExpr).Append(" = ").Append(strVar).AppendLine(";");
                }
                else
                {
                    sb.Append(p).Append("if (").Append(strVar).AppendLine(" == null)");
                    sb.Append(p).AppendLine("    return false;");
                    sb.Append(p).Append(targetExpr).Append(" = ").Append(strVar).AppendLine("!;");
                }

                return;
            }
            case TypeSerializationKind.Primitive:
                EmitPrimitiveDeserialize(sb, type.PrimitiveKind, targetExpr, indent, ref id);
                return;
            case TypeSerializationKind.Enum when type.EnumUnderlying != null:
            {
                var raw = "__enum_" + id++;
                sb.Append(p).Append(type.EnumUnderlying.DisplayName).Append(' ').Append(raw).AppendLine(" = default;");
                EmitDeserializeForType(sb, type.EnumUnderlying, raw, indent, ref id);
                sb.Append(p).Append(targetExpr).Append(" = (").Append(type.DisplayName).Append(")").Append(raw).AppendLine(";");
                return;
            }
            case TypeSerializationKind.Serializable:
            {
                var has = "__has_" + id++;
                var obj = "__obj_" + id++;
                sb.Append(p).Append("if (!global::MinionLib.Component.Core.SerializationUtils.TryReadBoolean(ref reader, out var ")
                    .Append(has).AppendLine("))");
                sb.Append(p).AppendLine("    return false;");
                sb.Append(p).Append("if (!").Append(has).AppendLine(")");
                sb.Append(p).AppendLine("{");
                if (type.CanAssignNullOnDeserialize)
                    sb.Append(p).Append("    ").Append(targetExpr).AppendLine(" = null;");
                else
                    sb.Append(p).AppendLine("    return false;");
                sb.Append(p).AppendLine("}");
                sb.Append(p).AppendLine("else");
                sb.Append(p).AppendLine("{");
                sb.Append(p).Append("    var ").Append(obj).Append(" = new ").Append(type.DisplayName).AppendLine("();");
                sb.Append(p).Append("    if (!global::MinionLib.Component.Core.SerializationUtils.TryReadSerializableBlock(ref reader, ")
                    .Append(obj).AppendLine("))");
                sb.Append(p).AppendLine("        return false;");
                sb.Append(p).Append("    ").Append(targetExpr).Append(" = ").Append(obj).AppendLine(";");
                sb.Append(p).AppendLine("}");
                return;
            }
            case TypeSerializationKind.PacketSerializable:
            {
                var has = "__has_" + id++;
                var obj = "__obj_" + id++;
                sb.Append(p).Append("if (!global::MinionLib.Component.Core.SerializationUtils.TryReadBoolean(ref reader, out var ")
                    .Append(has).AppendLine("))");
                sb.Append(p).AppendLine("    return false;");
                sb.Append(p).Append("if (!").Append(has).AppendLine(")");
                sb.Append(p).AppendLine("{");
                if (type.CanAssignNullOnDeserialize)
                    sb.Append(p).Append("    ").Append(targetExpr).AppendLine(" = null;");
                else
                    sb.Append(p).AppendLine("    return false;");
                sb.Append(p).AppendLine("}");
                sb.Append(p).AppendLine("else");
                sb.Append(p).AppendLine("{");
                sb.Append(p).Append("    if (!global::MinionLib.Component.Core.SerializationUtils.TryReadIPacketSerializable<")
                    .Append(type.DisplayName).Append(">(ref reader, out var ").Append(obj).AppendLine("))");
                sb.Append(p).AppendLine("        return false;");
                sb.Append(p).Append("    ").Append(targetExpr).Append(" = ").Append(obj).AppendLine(";");
                sb.Append(p).AppendLine("}");
                return;
            }
            default:
            {
                var jsonVar = "__json_" + id++;
                sb.Append(p).Append("if (!global::MinionLib.Component.Core.SerializationUtils.TryReadJson<").Append(type.DisplayName)
                    .Append(">(ref reader, out var ").Append(jsonVar).AppendLine("))");
                sb.Append(p).AppendLine("    return false;");
                if (type.CanAssignNullOnDeserialize)
                {
                    sb.Append(p).Append(targetExpr).Append(" = ").Append(jsonVar).AppendLine(";");
                }
                else
                {
                    sb.Append(p).Append("if (").Append(jsonVar).AppendLine(" == null)");
                    sb.Append(p).AppendLine("    return false;");
                    sb.Append(p).Append(targetExpr).Append(" = ").Append(jsonVar).AppendLine("!;");
                }
                return;
            }
        }
    }

    private static void EmitPrimitiveSerialize(StringBuilder sb, PrimitiveSerializationKind primitiveKind, string expr, int indent)
    {
        var p = Indent(indent);
        switch (primitiveKind)
        {
            case PrimitiveSerializationKind.Boolean:
                sb.Append(p).Append("global::MinionLib.Component.Core.SerializationUtils.WriteBoolean(writer, ").Append(expr).AppendLine(");");
                return;
            case PrimitiveSerializationKind.Byte:
                sb.Append(p).Append("global::MinionLib.Component.Core.SerializationUtils.WriteByte(writer, ").Append(expr).AppendLine(");");
                return;
            case PrimitiveSerializationKind.SByte:
                sb.Append(p).Append("global::MinionLib.Component.Core.SerializationUtils.WriteByte(writer, unchecked((byte)").Append(expr)
                    .AppendLine("));");
                return;
            case PrimitiveSerializationKind.Int16:
                sb.Append(p).Append("global::MinionLib.Component.Core.SerializationUtils.WriteInt16(writer, ").Append(expr).AppendLine(");");
                return;
            case PrimitiveSerializationKind.UInt16:
            case PrimitiveSerializationKind.Char:
                sb.Append(p).Append("global::MinionLib.Component.Core.SerializationUtils.WriteUInt16(writer, (ushort)").Append(expr)
                    .AppendLine(");");
                return;
            case PrimitiveSerializationKind.Int32:
                sb.Append(p).Append("global::MinionLib.Component.Core.SerializationUtils.WriteInt32(writer, ").Append(expr).AppendLine(");");
                return;
            case PrimitiveSerializationKind.UInt32:
                sb.Append(p).Append("global::MinionLib.Component.Core.SerializationUtils.WriteUInt32(writer, ").Append(expr).AppendLine(");");
                return;
            case PrimitiveSerializationKind.Int64:
                sb.Append(p).Append("global::MinionLib.Component.Core.SerializationUtils.WriteInt64(writer, ").Append(expr).AppendLine(");");
                return;
            case PrimitiveSerializationKind.UInt64:
                sb.Append(p).Append("global::MinionLib.Component.Core.SerializationUtils.WriteUInt64(writer, ").Append(expr).AppendLine(");");
                return;
            case PrimitiveSerializationKind.Single:
                sb.Append(p).Append("global::MinionLib.Component.Core.SerializationUtils.WriteSingle(writer, ").Append(expr).AppendLine(");");
                return;
            case PrimitiveSerializationKind.Double:
                sb.Append(p).Append("global::MinionLib.Component.Core.SerializationUtils.WriteDouble(writer, ").Append(expr).AppendLine(");");
                return;
            case PrimitiveSerializationKind.Decimal:
                sb.Append(p).Append("global::MinionLib.Component.Core.SerializationUtils.WriteDecimal(writer, ").Append(expr).AppendLine(");");
                return;
            default:
                return;
        }
    }

    private static void EmitPrimitiveDeserialize(StringBuilder sb, PrimitiveSerializationKind primitiveKind, string targetExpr, int indent,
        ref int id)
    {
        var p = Indent(indent);
        var temp = "__v_" + id++;

        string? readCall = primitiveKind switch
        {
            PrimitiveSerializationKind.Boolean => "TryReadBoolean",
            PrimitiveSerializationKind.Byte => "TryReadByte",
            PrimitiveSerializationKind.Int16 => "TryReadInt16",
            PrimitiveSerializationKind.UInt16 => "TryReadUInt16",
            PrimitiveSerializationKind.Int32 => "TryReadInt32",
            PrimitiveSerializationKind.UInt32 => "TryReadUInt32",
            PrimitiveSerializationKind.Int64 => "TryReadInt64",
            PrimitiveSerializationKind.UInt64 => "TryReadUInt64",
            PrimitiveSerializationKind.Single => "TryReadSingle",
            PrimitiveSerializationKind.Double => "TryReadDouble",
            PrimitiveSerializationKind.Decimal => "TryReadDecimal",
            _ => null
        };

        if (primitiveKind == PrimitiveSerializationKind.SByte)
        {
            sb.Append(p).Append("if (!global::MinionLib.Component.Core.SerializationUtils.TryReadByte(ref reader, out var ").Append(temp)
                .AppendLine("))");
            sb.Append(p).AppendLine("    return false;");
            sb.Append(p).Append(targetExpr).Append(" = unchecked((sbyte)").Append(temp).AppendLine(");");
            return;
        }

        if (primitiveKind == PrimitiveSerializationKind.Char)
        {
            sb.Append(p).Append("if (!global::MinionLib.Component.Core.SerializationUtils.TryReadUInt16(ref reader, out var ").Append(temp)
                .AppendLine("))");
            sb.Append(p).AppendLine("    return false;");
            sb.Append(p).Append(targetExpr).Append(" = (char)").Append(temp).AppendLine(";");
            return;
        }

        if (readCall == null)
            return;

        sb.Append(p).Append("if (!global::MinionLib.Component.Core.SerializationUtils.").Append(readCall)
            .Append("(ref reader, out var ").Append(temp).AppendLine("))");
        sb.Append(p).AppendLine("    return false;");
        sb.Append(p).Append(targetExpr).Append(" = ").Append(temp).AppendLine(";");
    }

    private static string BuildJaggedArrayCreationExpression(TypeShapeData arrayType, string firstLengthExpr)
    {
        var baseType = arrayType;
        var depth = 0;
        while (baseType.Kind == TypeSerializationKind.Array && baseType.ElementType != null)
        {
            depth++;
            baseType = baseType.ElementType;
        }

        return $"new {baseType.DisplayName}[{firstLengthExpr}]{string.Concat(Enumerable.Repeat("[]", depth - 1))}";
    }

    private static string GetLocalInitializer(TypeShapeData type)
    {
        return type.CanAssignNullOnDeserialize ? " = default;" : " = default!;";
    }

    private static string EnsureNullForgiven(string expr)
    {
        return expr.EndsWith("!", StringComparison.Ordinal) ? expr : expr + "!";
    }

    private static bool Implements(INamedTypeSymbol type, string fullyQualifiedInterfaceMetadataName)
    {
        return type.AllInterfaces.Any(i => i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == fullyQualifiedInterfaceMetadataName);
    }

    private static Accessibility? GetParameterlessCtorAccessibility(INamedTypeSymbol type)
    {
        if (type.InstanceConstructors.Length == 0)
            return null;

        var ctor = type.InstanceConstructors.FirstOrDefault(static c => c.Parameters.Length == 0 && !c.IsStatic);
        return ctor?.DeclaredAccessibility;
    }


    private static bool HasAttribute(ISymbol symbol, string attributeMetadataName)
    {
        var qualified = "global::" + attributeMetadataName;
        return symbol.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == qualified
            || a.AttributeClass?.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == qualified);
    }

    private static bool IsPartial(INamedTypeSymbol type)
    {
        return type.DeclaringSyntaxReferences
            .Select(static r => r.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .Any(static s => s.Modifiers.Any(static m => m.Text == "partial"));
    }

    private static bool HasExplicitSerializationMethods(INamedTypeSymbol type)
    {
        var serialize = type.GetMembers("Serialize").OfType<IMethodSymbol>().Any(static m => !m.IsStatic && m.Parameters.Length == 1);
        var deserialize = type.GetMembers("Deserialize").OfType<IMethodSymbol>().Any(static m => !m.IsStatic && m.Parameters.Length == 1);
        return serialize && deserialize;
    }

    private static bool InheritsFrom(INamedTypeSymbol type, string fullyQualifiedBaseTypeMetadataName)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            if (current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == fullyQualifiedBaseTypeMetadataName)
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

    private static string BuildHintName(INamedTypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                   .Replace("global::", string.Empty)
                   .Replace('<', '_')
                   .Replace('>', '_')
                   .Replace('.', '_') + ".BinarySerialization.g.cs";
    }

    private static string GetInnermostTypeName(ImmutableArray<ContainingTypeData> containingTypes)
    {
        return containingTypes[containingTypes.Length - 1].Name;
    }

    private static void AppendContainingTypeDeclarations(StringBuilder builder, ImmutableArray<ContainingTypeData> containingTypes)
    {
        for (var i = 0; i < containingTypes.Length; i++)
        {
            var type = containingTypes[i];
            var indent = new string(' ', i * 4);
            builder.Append(indent).Append("partial ").Append(type.Keyword).Append(' ').Append(type.Name);

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

    private static string Indent(int level) => new(' ', level * 4);

    private readonly record struct TypeGenerationData(
        string HintName,
        string TypeDisplayName,
        string? NamespaceName,
        ImmutableArray<ContainingTypeData> ContainingTypes,
        bool IsClass,
        Accessibility? ParameterlessCtorAccessibility,
        bool HasNoGeneratedSerializationAttribute,
        bool HasExplicitSerializationMethods,
        bool IsPartial,
        bool IsAbstract,
        bool IsSealed,
        bool ImplementsICardComponent,
        bool IsCardComponentDerived,
        Location? TypeLocation,
        ImmutableArray<PropertyGenerationData> Properties);

    private readonly record struct PropertyGenerationData(
        string PropertyName,
        Location? PropertyLocation,
        TypeShapeData Type);

    private readonly record struct ContainingTypeData(
        string Keyword,
        string Name,
        ImmutableArray<string> TypeParameters);

    private sealed record TypeShapeData(
        string DisplayName,
        TypeSerializationKind Kind,
        PrimitiveSerializationKind PrimitiveKind,
        bool CanAssignNullOnDeserialize,
        bool SuppressJsonFallbackWarning,
        bool HasNullableWrapper,
        TypeShapeData? NullableUnderlying,
        TypeShapeData? ElementType,
        string? ListTypeName,
        TypeShapeData? EnumUnderlying)
    {
        public bool UsesJsonFallback =>
            (Kind == TypeSerializationKind.Json && !SuppressJsonFallbackWarning)
            || (NullableUnderlying?.UsesJsonFallback ?? false)
            || (ElementType?.UsesJsonFallback ?? false)
            || (EnumUnderlying?.UsesJsonFallback ?? false);

        public static TypeShapeData FromSymbol(ITypeSymbol type)
        {
            var canAssignNull = ComputeCanAssignNullOnDeserialize(type);

            if (TryGetNullableValueUnderlyingType(type, out var nullableValueUnderlying))
            {
                var normalizedUnderlying = nullableValueUnderlying.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
                var underlyingShape = FromSymbol(normalizedUnderlying);
                return new TypeShapeData(
                    DisplayName: normalizedUnderlying.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    Kind: underlyingShape.Kind,
                    PrimitiveKind: underlyingShape.PrimitiveKind,
                    CanAssignNullOnDeserialize: canAssignNull,
                    SuppressJsonFallbackWarning: underlyingShape.SuppressJsonFallbackWarning,
                    HasNullableWrapper: true,
                    NullableUnderlying: underlyingShape,
                    ElementType: null,
                    ListTypeName: null,
                    EnumUnderlying: null);
            }

            if (type.NullableAnnotation == NullableAnnotation.Annotated
                && !type.IsValueType
                && type is not INamedTypeSymbol { SpecialType: SpecialType.System_String })
            {
                return FromSymbol(type.WithNullableAnnotation(NullableAnnotation.NotAnnotated)) with
                {
                    CanAssignNullOnDeserialize = canAssignNull
                };
            }

            if (type is IArrayTypeSymbol array && array.Rank == 1)
            {
                var elementShape = FromSymbol(array.ElementType);
                return new TypeShapeData(
                    DisplayName: type.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
                        .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    Kind: TypeSerializationKind.Array,
                    PrimitiveKind: PrimitiveSerializationKind.None,
                    CanAssignNullOnDeserialize: canAssignNull,
                    SuppressJsonFallbackWarning: false,
                    HasNullableWrapper: false,
                    NullableUnderlying: null,
                    ElementType: elementShape,
                    ListTypeName: null,
                    EnumUnderlying: null);
            }

            if (TryGetListElementType(type, out var listElement))
            {
                var elementShape = FromSymbol(listElement!);
                return new TypeShapeData(
                    DisplayName: type.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
                        .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    Kind: TypeSerializationKind.List,
                    PrimitiveKind: PrimitiveSerializationKind.None,
                    CanAssignNullOnDeserialize: canAssignNull,
                    SuppressJsonFallbackWarning: false,
                    HasNullableWrapper: false,
                    NullableUnderlying: null,
                    ElementType: elementShape,
                    ListTypeName: type.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
                        .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    EnumUnderlying: null);
            }

            if (type.SpecialType == SpecialType.System_String)
            {
                return new TypeShapeData(
                    DisplayName: "global::System.String",
                    Kind: TypeSerializationKind.String,
                    PrimitiveKind: PrimitiveSerializationKind.None,
                    CanAssignNullOnDeserialize: canAssignNull,
                    SuppressJsonFallbackWarning: false,
                    HasNullableWrapper: false,
                    NullableUnderlying: null,
                    ElementType: null,
                    ListTypeName: null,
                    EnumUnderlying: null);
            }

            if (TryMapPrimitive(type, out var primitiveKind))
            {
                return new TypeShapeData(
                    DisplayName: type.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
                        .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    Kind: TypeSerializationKind.Primitive,
                    PrimitiveKind: primitiveKind,
                    CanAssignNullOnDeserialize: canAssignNull,
                    SuppressJsonFallbackWarning: false,
                    HasNullableWrapper: false,
                    NullableUnderlying: null,
                    ElementType: null,
                    ListTypeName: null,
                    EnumUnderlying: null);
            }

            if (type.TypeKind == TypeKind.Enum && type is INamedTypeSymbol enumType && enumType.EnumUnderlyingType != null)
            {
                var enumUnderlying = FromSymbol(enumType.EnumUnderlyingType);
                return new TypeShapeData(
                    DisplayName: type.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
                        .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    Kind: TypeSerializationKind.Enum,
                    PrimitiveKind: PrimitiveSerializationKind.None,
                    CanAssignNullOnDeserialize: canAssignNull,
                    SuppressJsonFallbackWarning: false,
                    HasNullableWrapper: false,
                    NullableUnderlying: null,
                    ElementType: null,
                    ListTypeName: null,
                    EnumUnderlying: enumUnderlying);
            }

            if (ImplementsSerializable(type))
            {
                return new TypeShapeData(
                    DisplayName: type.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
                        .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    Kind: TypeSerializationKind.Serializable,
                    PrimitiveKind: PrimitiveSerializationKind.None,
                    CanAssignNullOnDeserialize: canAssignNull,
                    SuppressJsonFallbackWarning: false,
                    HasNullableWrapper: false,
                    NullableUnderlying: null,
                    ElementType: null,
                    ListTypeName: null,
                    EnumUnderlying: null);
            }

            if (ImplementsPacketSerializable(type))
            {
                return new TypeShapeData(
                    DisplayName: type.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
                        .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    Kind: TypeSerializationKind.PacketSerializable,
                    PrimitiveKind: PrimitiveSerializationKind.None,
                    CanAssignNullOnDeserialize: canAssignNull,
                    SuppressJsonFallbackWarning: false,
                    HasNullableWrapper: false,
                    NullableUnderlying: null,
                    ElementType: null,
                    ListTypeName: null,
                    EnumUnderlying: null);
            }

            return new TypeShapeData(
                DisplayName: type.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                Kind: TypeSerializationKind.Json,
                PrimitiveKind: PrimitiveSerializationKind.None,
                CanAssignNullOnDeserialize: canAssignNull,
                SuppressJsonFallbackWarning: false,
                HasNullableWrapper: false,
                NullableUnderlying: null,
                ElementType: null,
                ListTypeName: null,
                EnumUnderlying: null);
        }


        private static bool TryGetNullableValueUnderlyingType(ITypeSymbol type, out ITypeSymbol underlying)
        {
            if (type is INamedTypeSymbol named
                && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                underlying = named.TypeArguments[0];
                return true;
            }

            underlying = type;
            return false;
        }

        private static bool TryGetListElementType(ITypeSymbol type, out ITypeSymbol? elementType)
        {
            elementType = null;
            if (type is not INamedTypeSymbol named || !named.IsGenericType)
                return false;

            if (named.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                != "global::System.Collections.Generic.List<T>")
                return false;

            elementType = named.TypeArguments[0];
            return true;
        }

        private static bool TryMapPrimitive(ITypeSymbol type, out PrimitiveSerializationKind primitiveKind)
        {
            primitiveKind = type.SpecialType switch
            {
                SpecialType.System_Boolean => PrimitiveSerializationKind.Boolean,
                SpecialType.System_Byte => PrimitiveSerializationKind.Byte,
                SpecialType.System_SByte => PrimitiveSerializationKind.SByte,
                SpecialType.System_Int16 => PrimitiveSerializationKind.Int16,
                SpecialType.System_UInt16 => PrimitiveSerializationKind.UInt16,
                SpecialType.System_Char => PrimitiveSerializationKind.Char,
                SpecialType.System_Int32 => PrimitiveSerializationKind.Int32,
                SpecialType.System_UInt32 => PrimitiveSerializationKind.UInt32,
                SpecialType.System_Int64 => PrimitiveSerializationKind.Int64,
                SpecialType.System_UInt64 => PrimitiveSerializationKind.UInt64,
                SpecialType.System_Single => PrimitiveSerializationKind.Single,
                SpecialType.System_Double => PrimitiveSerializationKind.Double,
                SpecialType.System_Decimal => PrimitiveSerializationKind.Decimal,
                _ => PrimitiveSerializationKind.None
            };

            return primitiveKind != PrimitiveSerializationKind.None;
        }

        private static bool ImplementsSerializable(ITypeSymbol type)
        {
            return type is INamedTypeSymbol named
                   && named.AllInterfaces.Any(i => i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                                                   == FullyQualifiedGeneratedSerializableMetadataName);
        }

        private static bool ImplementsPacketSerializable(ITypeSymbol type)
        {
            return type is INamedTypeSymbol named
                   && named.AllInterfaces.Any(i => i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                                                   == FullyQualifiedPacketSerializableMetadataName);
        }

        private static bool ComputeCanAssignNullOnDeserialize(ITypeSymbol type)
        {
            if (type.IsValueType)
                return type is INamedTypeSymbol named
                       && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

            return type.NullableAnnotation != NullableAnnotation.NotAnnotated;
        }

    }

    private enum TypeSerializationKind
    {
        Primitive,
        String,
        Enum,
        Array,
        List,
        Serializable,
        PacketSerializable,
        Json
    }

    private enum PrimitiveSerializationKind
    {
        None,
        Boolean,
        Byte,
        SByte,
        Int16,
        UInt16,
        Char,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Single,
        Double,
        Decimal
    }
}
