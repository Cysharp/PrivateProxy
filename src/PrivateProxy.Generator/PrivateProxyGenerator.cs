using System.Collections.Immutable;
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

        // NOTE: currently does not provide private metadata from external dll.
        // context.CompilationProvider.Select((x, token) => x.WithOptions(x.Options.WithMetadataImportOptions(MetadataImportOptions.All));

        context.RegisterSourceOutput(source, Emit);
    }

    static void EmitAttributes(IncrementalGeneratorPostInitializationContext context)
    {
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

        public GeneratePrivateProxyAttribute(Type target)
        {
            this.Target = target;
            this.GenerateKinds = PrivateProxyGenerateKinds.All;
        }

        public GeneratePrivateProxyAttribute(Type target, PrivateProxyGenerateKinds generateKinds)
        {
            this.Target = target;
            this.GenerateKinds = generateKinds;
        }
    }

    [Flags]
    internal enum PrivateProxyGenerateKinds
    {
        All = 0, // Field | Method | Property | Instance | Static | Constructor
        Field = 1,
        Method = 2,
        Property = 4,
        Instance = 8,
        Static = 16,
        Constructor = 32,
    }
}
""");
    }

    [Flags]
    public enum PrivateProxyGenerateKinds
    {
        All = 0, // Field | Method | Property | Instance | Static | Constructor
        Field = 1,
        Method = 2,
        Property = 4,
        Instance = 8,
        Static = 16,
        Constructor = 32,
    }

    static void Emit(SourceProductionContext context, GeneratorAttributeSyntaxContext source)
    {
        var attr = source.Attributes[0]; // allowMultiple:false
        GetAttributeParameters(attr, out var targetType, out var kind);

        var members = GetMembers(targetType, kind);

        if (!Verify(context, (TypeDeclarationSyntax)source.TargetNode, (INamedTypeSymbol)source.TargetSymbol, targetType))
        {
            return;
        }

        if (members.Length == 0)
        {
            return;
        }

        //// Generate Code
        var code = EmitCode((ITypeSymbol)source.TargetSymbol, targetType, members);
        AddSource(context, source.TargetSymbol, code);
    }

    static void GetAttributeParameters(AttributeData attr, out INamedTypeSymbol targetType, out PrivateProxyGenerateKinds kind)
    {
        // Extract attribute parameter
        // public GeneratePrivateProxyAttribute(Type target)
        // public GeneratePrivateProxyAttribute(Type target, PrivateProxyGenerateKinds generateKinds)

        targetType = (INamedTypeSymbol)attr.ConstructorArguments[0].Value!;

        if (attr.ConstructorArguments.Length == 1)
        {
            kind = PrivateProxyGenerateKinds.All;
        }
        else
        {
            kind = (PrivateProxyGenerateKinds)attr.ConstructorArguments[1].Value!;
        }
    }

    static MetaMember[] GetMembers(INamedTypeSymbol targetType, PrivateProxyGenerateKinds kind)
    {
        var members = targetType.GetMembers();

        var list = new List<MetaMember>(members.Length);

        kind = (kind == PrivateProxyGenerateKinds.All) ? PrivateProxyGenerateKinds.Field | PrivateProxyGenerateKinds.Method | PrivateProxyGenerateKinds.Property | PrivateProxyGenerateKinds.Instance | PrivateProxyGenerateKinds.Static | PrivateProxyGenerateKinds.Constructor : kind;

        var generateField = kind.HasFlag(PrivateProxyGenerateKinds.Field);
        var generateProperty = kind.HasFlag(PrivateProxyGenerateKinds.Property);
        var generateMethod = kind.HasFlag(PrivateProxyGenerateKinds.Method);
        var generateInstance = kind.HasFlag(PrivateProxyGenerateKinds.Instance);
        var generateStatic = kind.HasFlag(PrivateProxyGenerateKinds.Static);
        var generateConstructor = kind.HasFlag(PrivateProxyGenerateKinds.Constructor);
        
        // If only set Static or Instance, generate all member kind
        if (!generateField && !generateProperty && !generateMethod && !generateConstructor)
        {
            generateField = generateProperty = generateMethod = generateConstructor = true;
        }
        // If only set member kind, generate both static and instance
        if (!generateStatic && !generateInstance)
        {
            generateStatic = generateInstance = true;
        }

        foreach (var item in members)
        {
            if (!item.CanBeReferencedByName && item.Name != ".ctor") continue;

            if (item.IsStatic && !generateStatic) continue;
            if (!item.IsStatic && !generateInstance) continue;

            // add field/property/method
            if (generateField && item is IFieldSymbol f)
            {
                // return type is not public, don't generate
                if (f.Type.DeclaredAccessibility != Accessibility.Public) continue;

                // public member don't generate
                if (f.DeclaredAccessibility == Accessibility.Public) continue;

                list.Add(new(item));
            }
            else if (generateProperty && item is IPropertySymbol p)
            {
                if (p.Type.DeclaredAccessibility != Accessibility.Public) continue;

                if (p.DeclaredAccessibility == Accessibility.Public)
                {
                    var getPublic = true;
                    var setPublic = true;
                    if (p.GetMethod != null)
                    {
                        getPublic = p.GetMethod.DeclaredAccessibility == Accessibility.Public;
                    }
                    if (p.SetMethod != null)
                    {
                        setPublic = p.SetMethod.DeclaredAccessibility == Accessibility.Public;
                    }

                    if (getPublic && setPublic) continue;
                }

                list.Add(new(item));
            }
            else if ((generateMethod || generateConstructor) && item is IMethodSymbol m)
            {
                // both return type and parameter type must be public
                if (m.ReturnType.DeclaredAccessibility != Accessibility.Public) continue;
                foreach (var parameter in m.Parameters)
                {
                    if (parameter.Type.DeclaredAccessibility != Accessibility.Public) continue;
                }

                if (m.DeclaredAccessibility == Accessibility.Public) continue;

                if ((m.Name == ".ctor" && generateConstructor) || generateMethod)
                {
                    list.Add(new(item));
                }
            }
        }
        return list.ToArray();
    }

    static bool Verify(SourceProductionContext context, TypeDeclarationSyntax typeSyntax, INamedTypeSymbol proxyType, INamedTypeSymbol targetType)
    {
        // Type Rule
        // ProxyClass: class -> allows class or struct
        //           : struct -> allows ref struct

        var hasError = false;

        // require partial
        if (!typeSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MustBePartial, typeSyntax.Identifier.GetLocation(), proxyType.Name));
            hasError = true;
        }

        // not allow readonly struct
        if (proxyType.IsValueType && proxyType.IsReadOnly)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.NotAllowReadOnly, typeSyntax.Identifier.GetLocation(), proxyType.Name));
            hasError = true;
        }

        // class, not allow ref struct
        if (targetType.IsReferenceType && proxyType.IsRefLikeType)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ClassNotAllowRefStruct, typeSyntax.Identifier.GetLocation(), proxyType.Name));
            hasError = true;
        }

        // struct, not allow class or struct(only allows ref struct)
        if (targetType.IsValueType)
        {
            if (proxyType.IsReferenceType)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.StructNotAllowClass, typeSyntax.Identifier.GetLocation(), proxyType.Name));
                hasError = true;
            }
            else if (proxyType.IsValueType && !proxyType.IsRefLikeType)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.StructNotAllowStruct, typeSyntax.Identifier.GetLocation(), proxyType.Name));
                hasError = true;
            }
        }

        // target type not allow `ref struct`
        if (targetType.IsValueType && targetType.IsRefLikeType)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.RefStructNotSupported, typeSyntax.Identifier.GetLocation()));
            hasError = true;
        }

        // generics is not supported
        if (targetType.IsGenericType)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.GenericsNotSupported, typeSyntax.Identifier.GetLocation()));
            hasError = true;
        }

        // static class is not supported
        if (targetType.IsStatic)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.StaticNotSupported, typeSyntax.Identifier.GetLocation()));
            hasError = true;
        }

        return !hasError;
    }

    static string EmitCode(ITypeSymbol proxyType, INamedTypeSymbol targetType, MetaMember[] members)
    {
        var targetTypeFullName = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var code = new StringBuilder();

        var accessibility = proxyType.DeclaredAccessibility.ToCode();
        var structOrClass = proxyType.IsReferenceType ? "class" : "struct";
        var refStruct = proxyType.IsRefLikeType ? "ref " : "";

        var hasStatic = members.Any(x => x.IsStatic);

        code.AppendLine($$"""
{{refStruct}}partial {{structOrClass}} {{proxyType.Name}}
{
{{If(hasStatic, $$"""
    static {{targetTypeFullName}} ____static_instance = default!;
""")}}
    {{refStruct}}{{targetTypeFullName}} target;

    public {{proxyType.Name}}({{refStruct}}{{targetTypeFullName}} target)
    {
        this.target = {{refStruct}}target;
    }

""");

        foreach (var item in members)
        {
            var readonlyCode = item.IsRequireReadOnly ? "readonly " : "";
            var refReturn = item.IsRefReturn ? "ref " : "";

            var staticCode = item.IsStatic ? "Static" : "";
            var staticCode2 = item.IsStatic ? "static " : "";
            var targetInstance = item.IsStatic ? $"{refStruct}____static_instance" : $"{refStruct}this.target";
            switch (item.MemberKind)
            {
                case MemberKind.Field:
                    code.AppendLine($$"""
    [UnsafeAccessor(UnsafeAccessorKind.{{staticCode}}Field, Name = "{{item.Name}}")]
    static extern ref {{readonlyCode}}{{item.MemberTypeFullName}} __{{item.Name}}__({{refStruct}}{{targetTypeFullName}} target);

    public {{staticCode2}}ref {{readonlyCode}}{{item.MemberTypeFullName}} {{item.Name}} => ref __{{item.Name}}__({{targetInstance}});
""");
                    break;
                case MemberKind.Property:

                    if (item.HasGetMethod)
                    {
                        code.AppendLine($$"""
    [UnsafeAccessor(UnsafeAccessorKind.{{staticCode}}Method, Name = "get_{{item.Name}}")]
    static extern {{refReturn}}{{item.MemberTypeFullName}} __get_{{item.Name}}__({{refStruct}}{{targetTypeFullName}} target);

""");
                    }

                    if (item.HasSetMethod)
                    {
                        code.AppendLine($$"""
    [UnsafeAccessor(UnsafeAccessorKind.{{staticCode}}Method, Name = "set_{{item.Name}}")]
    static extern void __set_{{item.Name}}__({{refStruct}}{{targetTypeFullName}} target, {{item.MemberTypeFullName}} value);

""");
                    }

                    code.AppendLine($$"""
    public {{staticCode2}}{{refReturn}}{{readonlyCode}}{{item.MemberTypeFullName}} {{item.Name}}
    {
""");
                    if (item.HasGetMethod)
                    {
                        code.AppendLine($"        get => {refReturn}__get_{item.Name}__({targetInstance});");
                    }
                    if (item.HasSetMethod)
                    {
                        code.AppendLine($"        set => __set_{item.Name}__({targetInstance}, value);");
                    }

                    code.AppendLine("    }"); // close property
                    break;
                case MemberKind.Constructor:
                case MemberKind.Method:
                    var parameters = string.Join(", ", item.MethodParameters.Select(x => $"{x.RefKind.ToParameterPrefix()}{x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {x.Name}"));
                    var parametersWithComma = (parameters != "") ? ", " + parameters : "";
                    var useParameters = string.Join(", ", item.MethodParameters.Select(x => $"{x.RefKind.ToUseParameterPrefix()}{x.Name}"));
                    if (useParameters != "") useParameters = ", " + useParameters;

                    if (item.MemberKind == MemberKind.Constructor)
                    {
                        code.AppendLine($$"""
    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    public static extern {{targetTypeFullName}} Create{{targetType.Name}}FromConstructor({{parameters}});

""");
                    }
                    else
                    {
                        code.AppendLine($$"""
    [UnsafeAccessor(UnsafeAccessorKind.{{staticCode}}Method, Name = "{{item.Name}}")]
    static extern {{refReturn}}{{item.MemberTypeFullName}} __{{item.Name}}__({{refStruct}}{{targetTypeFullName}} target{{parametersWithComma}});
    
    public {{staticCode2}}{{refReturn}}{{readonlyCode}}{{item.MemberTypeFullName}} {{item.Name}}({{parameters}}) => {{refReturn}}__{{item.Name}}__({{targetInstance}}{{useParameters}});

""");
                    }
                    break;
                default:
                    break;
            }
        }

        code.AppendLine("}"); // close Proxy partial

        code.AppendLine($$"""

{{accessibility}} static class {{targetType.Name}}PrivateProxyExtensions
{
    public static {{proxyType.ToFullyQualifiedFormatString()}} AsPrivateProxy(this {{refStruct}}{{targetTypeFullName}} target)
    {
        return new {{proxyType.ToFullyQualifiedFormatString()}}({{refStruct}}target);
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
