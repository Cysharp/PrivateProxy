using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using static PrivateProxy.Generator.EmitHelper;

namespace PrivateProxy.Generator;

[Generator(LanguageNames.CSharp)]
public partial class PrivateProxyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(EmitAttributes);

        var source = context.SyntaxProvider.ForAttributeWithMetadataName(
                 "PrivateProxy.GeneratePrivateProxyAttribute",
                 static (node, token) => node is StructDeclarationSyntax or ClassDeclarationSyntax,
                 static (context, token) => context);

        context.RegisterSourceOutput(source, Emit);
    }

    static void EmitAttributes(IncrementalGeneratorPostInitializationContext context)
    {
        // TODO: deny => allow

        context.AddSource("GeneratePrivateProxyAttribute.cs", """
using System;
using System.Collections.Generic;

namespace PrivateProxy
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    internal sealed class GeneratePrivateProxyAttribute : Attribute
    {
        public Type Target { get; } 
        public PrivateProxyGenerateKinds GenerateKinds { get; }
        public IReadOnlyList<string> DenyList { get; }

        public GeneratePrivateProxyAttribute(Type target)
        {
            this.Target = target;
            this.GenerateKinds = PrivateProxyGenerateKinds.All;
            this.DenyList = Array.Empty<string>();
        }

        public GeneratePrivateProxyAttribute(Type target, PrivateProxyGenerateKinds generateKinds, params string[] denyList)
        {
            this.Target = target;
            this.GenerateKinds = generateKinds;
            this.DenyList = denyList;
        }
    }

    [Flags]
    internal enum PrivateProxyGenerateKinds
    {
        None = 0,
        Field = 1,
        Method = 2,
        Property = 4,
        All = Field | Method | Property
    }
}
""");
    }

    [Flags]
    public enum PrivateProxyGenerateKinds
    {
        None = 0,
        Field = 1,
        Method = 2,
        Property = 4,
        All = Field | Method | Property
    }

    static void Emit(SourceProductionContext context, GeneratorAttributeSyntaxContext source)
    {
        if (!Verify(context, (TypeDeclarationSyntax)source.TargetNode, source.TargetSymbol))
        {
            return;
        }

        var attr = source.Attributes[0]; // allowMultiple:false
        GetAttributeParameters(attr, out var targetType, out var kind, out var denyList);

        var members = GetMembers(targetType, kind, denyList);

        if (members.Length == 0)
        {
            return;
        }

        //// Generate Code
        var code = EmitCode((ITypeSymbol)source.TargetSymbol, targetType, members);
        AddSource(context, source.TargetSymbol, code);
    }

    static void GetAttributeParameters(AttributeData attr, out INamedTypeSymbol targetType, out PrivateProxyGenerateKinds kind, out string[] denyList)
    {
        // Extract attribute parameter
        // public GeneratePrivateProxyAttribute(Type target)
        // public GeneratePrivateProxyAttribute(Type target, PrivateProxyGenerateKinds generateKinds, params string[] deny)

        targetType = (INamedTypeSymbol)attr.ConstructorArguments[0].Value!;

        if (attr.ConstructorArguments.Length == 1)
        {
            kind = PrivateProxyGenerateKinds.All;
            denyList = [];
            return;
        }

        kind = (PrivateProxyGenerateKinds)attr.ConstructorArguments[1].Value!;

        if (attr.ConstructorArguments.Length == 2)
        {
            denyList = attr.ConstructorArguments[2].Values.Select(x => (string)x.Value!).ToArray();
        }
        else
        {
            denyList = [];
        }
    }

    static MetaMember[] GetMembers(INamedTypeSymbol targetType, PrivateProxyGenerateKinds kind, string[] denyList)
    {
        // TODO: deny -> allow
        var deny = new HashSet<string>(denyList);
        var members = targetType.GetMembers();

        var list = new List<MetaMember>(members.Length);

        var generateField = kind.HasFlag(PrivateProxyGenerateKinds.Field);
        var generateProperty = kind.HasFlag(PrivateProxyGenerateKinds.Property);
        var generateMethod = kind.HasFlag(PrivateProxyGenerateKinds.Method);

        foreach (var item in members)
        {
            if (!item.CanBeReferencedByName) continue;

            // check deny
            if (deny.Contains(item.Name)) continue;

            // add field/property/method
            if (generateField && item is IFieldSymbol f)
            {
                if (item.DeclaredAccessibility == Accessibility.Public) continue;
                list.Add(new(item));
            }
            else if (generateProperty && item is IPropertySymbol)
            {
                // TODO: check method accessibility
                list.Add(new(item));
            }
            else if (generateMethod && item is IMethodSymbol)
            {
                if (item.DeclaredAccessibility == Accessibility.Public) continue;
                list.Add(new(item));
            }
        }

        return list.ToArray();
    }

    static bool Verify(SourceProductionContext context, TypeDeclarationSyntax typeSyntax, ISymbol targetType)
    {
        var hasError = false;

        // require partial
        if (!typeSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MustBePartial, typeSyntax.Identifier.GetLocation(), targetType.Name));
            hasError = true;
        }

        // TODO: not allow readonly struct
        // TODO:target is ref struct ,must be ref struct.
        // TODO:target is struct and return ref

        return !hasError;
    }

    static string EmitCode(ITypeSymbol proxyType, INamedTypeSymbol targetType, MetaMember[] members)
    {
        var targetTypeFullName = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var code = new StringBuilder();

        var accessibility = proxyType.DeclaredAccessibility.ToCode();

        var structOrClass = proxyType.IsReferenceType ? "class" : "struct";
        var refStruct = proxyType.IsRefLikeType ? "ref " : "";
        var refValueType = targetType.IsValueType ? "ref " : ""; // if valueType, constructor accepts ref and UnsafeAccessor needs ref

        // ref field cannot have type that is ref struct
        // https://github.com/dotnet/csharplang/issues/6149#issuecomment-1185491794
        var refField = proxyType.IsRefLikeType && targetType.IsValueType && !targetType.IsRefLikeType ? "ref " : "";

        code.AppendLine($$"""
{{refStruct}}partial {{structOrClass}} {{proxyType.Name}}
{
    {{refField}}{{targetTypeFullName}} target;

    public {{proxyType.Name}}({{refValueType}}{{targetTypeFullName}} target)
    {
        this.target = {{refField}}target;
    }

""");

        // TODO: check return type is not public type(it should be return object)

        foreach (var item in members)
        {
            // TODO: reflection fallback

            if (item.IsStatic)
            {
                // TODO: static
            }

            var readonlyCode = item.IsRequireReadOnly ? "readonly " : "";
            var refReturn = item.IsRefReturn ? "ref " : "";

            switch (item.MemberKind)
            {
                // expose UnsafeAccessor directly because struct causes CS8347
                // however can't configure readonly
                case MemberKind.Field:
                    code.AppendLine($$"""
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "{{item.Name}}")]
    public static extern ref {{item.MemberTypeFullName}} {{item.Name}}({{refValueType}}{{targetTypeFullName}} target);

""");
                    break;
                case MemberKind.Property:
                    
                    if (item.HasGetMethod)
                    {
                        code.AppendLine($$"""
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_{{item.Name}}")]
    static extern {{refReturn}}{{item.MemberTypeFullName}} __get_{{item.Name}}__({{refValueType}}{{targetTypeFullName}} target);

""");
                    }

                    if (item.HasSetMethod)
                    {
                        code.AppendLine($$"""
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_{{item.Name}}")]
    static extern void __set_{{item.Name}}__({{refValueType}}{{targetTypeFullName}} target, {{item.MemberTypeFullName}} value);

""");
                    }

                    code.AppendLine($$"""
    public {{refReturn}}{{readonlyCode}}{{item.MemberTypeFullName}} {{item.Name}}
    {
""");
                    if (item.HasGetMethod)
                    {
                        code.AppendLine($"        get => {refReturn}__get_{item.Name}__({refValueType}this.target);");
                    }
                    if (item.HasSetMethod)
                    {
                        code.AppendLine($"        set => __set_{item.Name}__({refValueType}this.target, value);");
                    }

                    code.AppendLine("    }"); // close property
                    break;
                case MemberKind.Method:
                    var parameters = string.Join(", ", item.MethodParameters.Select(x => $"{x.RefKind.ToParameterPrefix()}{x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {x.Name}"));
                    var parametersWithComma = (parameters != "") ? ", " + parameters : "";
                    var parametersOnlyName = string.Join(", ", item.MethodParameters.Select(x => $"{x.RefKind.ToParameterPrefix()}{x.Name}"));
                    if (parametersOnlyName != "") parametersOnlyName = ", " + parametersOnlyName;

                    code.AppendLine($$"""
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "{{item.Name}}")]
    static extern {{item.MemberTypeFullName}} __{{item.Name}}__({{refValueType}}{{targetTypeFullName}} target{{parametersWithComma}});
    
    public {{refReturn}}{{readonlyCode}}{{item.MemberTypeFullName}} {{item.Name}}({{parameters}}) => {{refReturn}}__{{item.Name}}__({{refValueType}}this.target{{parametersOnlyName}});

""");
                    break;
                default:
                    break;
            }
        }

        code.AppendLine("}"); // close Proxy partial

        code.AppendLine($$"""

{{accessibility}} static class {{targetType.Name}}PrivateProxyExtensions
{
    public static {{proxyType.ToFullyQualifiedFormatString()}} AsPrivateProxy(this {{refValueType}}{{targetTypeFullName}} target)
    {
        return new {{proxyType.ToFullyQualifiedFormatString()}}({{refValueType}}target);
    }
}
""");

        return code.ToString();
    }

    static void AddSource(SourceProductionContext context, ISymbol targetSymbol, string code, string fileExtension = ".g.cs")
    {
        var fullType = targetSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
          .Replace("global::", "")
          .Replace("<", "_")
          .Replace(">", "_");

        var sb = new StringBuilder();

        sb.AppendLine("""
// <auto-generated/>
#nullable enable
#pragma warning disable CS0108
#pragma warning disable CS0162
#pragma warning disable CS0164
#pragma warning disable CS0219
#pragma warning disable CS8600
#pragma warning disable CS8601
#pragma warning disable CS8602
#pragma warning disable CS8604
#pragma warning disable CS8619
#pragma warning disable CS8620
#pragma warning disable CS8631
#pragma warning disable CS8765
#pragma warning disable CS9074
#pragma warning disable CA1050

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
""");

        var ns = targetSymbol.ContainingNamespace;
        if (!ns.IsGlobalNamespace)
        {
            sb.AppendLine($"namespace {ns} {{");
        }
        sb.AppendLine();

        sb.AppendLine(code);

        if (!ns.IsGlobalNamespace)
        {
            sb.AppendLine($"}}");
        }

        var sourceCode = sb.ToString();
        context.AddSource($"{fullType}{fileExtension}", sourceCode);
    }
}
