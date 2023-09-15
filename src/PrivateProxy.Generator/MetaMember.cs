using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace PrivateProxy.Generator;

public enum MemberKind
{
    Field, Property, Method
}

public class MetaMember
{
    public ITypeSymbol MemberType { get; }
    public string MemberTypeFullName { get; }
    public string Name { get; }
    public bool IsPublic { get; }
    public bool IsStatic { get; }
    public MemberKind MemberKind { get; }

    public bool IsFieldReadOnly { get; } // only for field
    public ImmutableArray<IParameterSymbol> MethodParameters { get; } // only for method

    public MetaMember(ISymbol symbol)
    {
        this.Name = symbol.Name;
        this.IsStatic = symbol.IsStatic;

        if (symbol is IFieldSymbol f)
        {
            this.MemberType = f.Type;
            this.MemberKind = MemberKind.Field;
            this.IsFieldReadOnly = f.IsReadOnly;
        }
        else if (symbol is IPropertySymbol p)
        {
            this.MemberType = p.Type;
            this.MemberKind = MemberKind.Property;
        }
        else if (symbol is IMethodSymbol m)
        {
            this.MemberType = m.ReturnType;
            this.MethodParameters = m.Parameters;
            this.MemberKind = MemberKind.Method;
        }
        else
        {
            throw new InvalidOperationException("Symbol type is invalid. " + symbol.GetType().FullName);
        }
        this.MemberTypeFullName = this.MemberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }
}
