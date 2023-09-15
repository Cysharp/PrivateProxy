using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using static PrivateProxy.Generator.EmitHelper;

namespace PrivateProxy.Generator;

[Generator(LanguageNames.CSharp)]
public partial class Generator : IIncrementalGenerator
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
    public class GeneratePrivateProxyAttribute : Attribute
    {
        public Type Target { get; } 
        public PrivateProxyGenerateKinds generateKinds { get; }
        public IReadOnlyList<string> DenyList { get; }

        public GeneratePrivateProxyAttribute(Type target)
        {
            this.Target = target;
            this.generateKinds = PrivateProxyGenerateKinds.All;
            this.DenyList = Array.Empty<string>();
        }

        public GeneratePrivateProxyAttribute(Type target, PrivateProxyGenerateKinds generateKinds, params string[] deny)
        {
            this.Target = target;
            this.generateKinds = generateKinds;
            this.DenyList = deny;
        }
    }

    [Flags]
    public enum PrivateProxyGenerateKinds
    {
        None = 0,
        Public = 1,
        Private = 2,
        Protected = 4,
        Internal = 8,
        Field = 16,
        Method = 32,
        Property = 64,
        All = Public | Private | Protected | Internal | Field | Method | Property
    }
}
""");
    }

    [Flags]
    public enum PrivateProxyGenerateKinds
    {
        None = 0,
        Public = 1,
        Private = 2,
        Protected = 4,
        Internal = 8,
        Field = 16,
        Method = 32,
        Property = 64,
        All = Public | Private | Protected | Internal | Field | Method | Property
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
        var code = EmitCode(source.TargetSymbol, targetType, members);
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
        var generatePublic = kind.HasFlag(PrivateProxyGenerateKinds.Public);
        var generatePrivate = kind.HasFlag(PrivateProxyGenerateKinds.Private);
        var generateInternal = kind.HasFlag(PrivateProxyGenerateKinds.Internal);
        var generateProtected = kind.HasFlag(PrivateProxyGenerateKinds.Protected);

        foreach (var item in members)
        {
            if (!item.CanBeReferencedByName) continue;

            // check accessibility
            switch (item.DeclaredAccessibility)
            {
                case Accessibility.NotApplicable: // private
                case Accessibility.Private:
                    if (!generatePrivate) continue;
                    break;
                case Accessibility.Protected:
                    if (!generateProtected) continue;
                    break;
                case Accessibility.Internal:
                    if (!generateInternal) continue;
                    break;
                case Accessibility.Public:
                    if (!generatePublic) continue;
                    break;
                case Accessibility.ProtectedAndInternal:
                    if (!(generateProtected && generateInternal)) continue;
                    break;
                case Accessibility.ProtectedOrInternal:
                    if (!(generateProtected || generateInternal)) continue;
                    break;
                default:
                    break;
            }

            // check deny
            if (deny.Contains(item.Name)) continue;

            // add field/property/method
            if (generateField && item is IFieldSymbol f)
            {
                list.Add(new(item));
            }
            else if (generateProperty && item is IPropertySymbol)
            {
                list.Add(new(item));
            }
            else if (generateMethod && item is IMethodSymbol)
            {
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

        return !hasError;
    }

    static string EmitCode(ISymbol proxyType, INamedTypeSymbol targetType, MetaMember[] members)
    {
        var targetTypeFullName = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var code = new StringBuilder();

        // TODO: struct or class?
        // TODO: implicit convert
        // TODO: ref struct
        code.AppendLine($$"""
partial struct {{proxyType.Name}}
{
    {{targetTypeFullName}} target;

    public {{proxyType.Name}}({{targetTypeFullName}} target)
    {
        this.target = target;
    }

""");

        foreach (var item in members)
        {
            if (item.IsPublic)
            {
                // TODO: public
            }

            // TODO: reflection fallback

            if (item.IsStatic)
            {
                // TODO: static
            }

            switch (item.MemberKind)
            {
                case MemberKind.Field:
                    // TODO: readonly
                    code.AppendLine($$"""
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "{{item.Name}}")]
    static extern ref {{item.MemberTypeFullName}} __{{item.Name}}__({{targetTypeFullName}} target);

    public ref {{item.MemberTypeFullName}} {{item.Name}} => ref __{{item.Name}}__(this.target);

""");
                    break;
                case MemberKind.Property:
                    // TODO: get, set, ref property?
                    code.AppendLine($$"""
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_{{item.Name}}")]
    static extern {{item.MemberTypeFullName}} __get_{{item.Name}}__({{targetTypeFullName}} target);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_{{item.Name}}")]
    static extern void __set_{{item.Name}}__({{targetTypeFullName}} target, {{item.MemberTypeFullName}} value);

    public {{item.MemberTypeFullName}} {{item.Name}}
    {
        get => __get_{{item.Name}}__(this.target);
        set => __set_{{item.Name}}__(this.target, value);
    }

""");

                    break;
                case MemberKind.Method:
                    // TODO: ref method?
                    var parameters = string.Join(", ", item.MethodParameters.Select(x => $"{x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {x.Name}"));
                    var parametersWithComma = (parameters != "") ? ", " + parameters : "";
                    var parametersOnlyName = string.Join(", ", item.MethodParameters.Select(x => x.Name));
                    if (parametersOnlyName != "") parametersOnlyName = ", " + parametersOnlyName;

                    code.AppendLine($$"""
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "{{item.Name}}")]
    static extern {{item.MemberTypeFullName}} __{{item.Name}}__({{targetTypeFullName}} target{{parametersWithComma}});
    
    public {{item.MemberTypeFullName}} {{item.Name}}({{parameters}}) => __{{item.Name}}__(this.target{{parametersOnlyName}});

""");
                    break;
                default:
                    break;
            }
        }

        // TODO: AsPrivateProxy extension method

        code.AppendLine("""
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
