using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace PrivateProxy.Generator;

public enum MemberKind
{
    Field, Property, Method, Constructor
}

public class MetaMember
{
    public ITypeSymbol MemberType { get; }
    public string MemberTypeFullName { get; }
    public string Name { get; }
    public bool IsPublic { get; }
    public bool IsStatic { get; }
    public MemberKind MemberKind { get; }

    public bool HasGetMethod { get; } // only for property
    public bool HasSetMethod { get; } // only for property

    public bool IsRequireReadOnly { get; }
    public bool IsRefReturn { get; }

    public ImmutableArray<IParameterSymbol> MethodParameters { get; } // only for method

    public MetaMember(ISymbol symbol)
    {
        this.Name = symbol.Name;
        this.IsStatic = symbol.IsStatic;

        if (symbol is IFieldSymbol f)
        {
            // PrivateProxy only handles assignable(use readonly keyword)

            // int field;                                     // RefKind=None
            // readonly int readOnlyField;                    // RefKind=None, IsReadOnly=true | can't assign
            // ref int refField;                              // RefKind=Ref
            // ref readonly int refReadOnly;                  // RefKind=In | can't assign
            // readonly ref int readonlyRef;                  // RefKind=Ref, IsReadOnly=true
            // readonly ref readonly int readOnlyRefReadOnly; // RefKind=In, IsReadOnly=true | can't assign

            this.MemberType = f.Type;
            this.MemberKind = MemberKind.Field;
            this.IsRequireReadOnly = (f.RefKind, f.IsReadOnly) switch
            {
                (RefKind.None, true) => true,
                (RefKind.In, _) => true,
                _ => false
            };
            this.IsRefReturn = f.RefKind != RefKind.None;
        }
        else if (symbol is IPropertySymbol p)
        {
            // int Prop0 => 1;
            // readonly int Prop1 => 1;
            // ref int Prop2 => ref refField;                    // ReturnsByRef:true
            // ref readonly int Prop3 => ref refField;           // ReturnsByRefReadOnly:true | can't assign
            // readonly ref int Prop4 => ref refField;           // ReturnsByRef:true
            // readonly ref readonly int Prop5 => ref refField;  // ReturnsByRefReadOnly:true | can't assign

            this.MemberType = p.Type;
            this.MemberKind = MemberKind.Property;
            this.HasGetMethod = p.GetMethod != null;
            this.HasSetMethod = p.SetMethod != null;

            (this.IsRefReturn, this.IsRequireReadOnly) = (p.ReturnsByRef, p.ReturnsByRefReadonly) switch
            {
                (true, true) => (true, true), // but maybe don't come here.
                (true, false) => (true, false),
                (false, true) => (true, true),
                (false, false) => (false, false),
            };
        }
        else if (symbol is IMethodSymbol m)
        {
            // same as property
            // int Method0() => 1;
            // readonly int Method1() => 1;
            // ref int Method2() => ref refField;
            // ref readonly int Method3() => ref refField;
            // readonly ref int Method4() => ref refField;
            // readonly ref readonly int Method5() => ref refField;
            
            this.MemberType = m.ReturnType;
            this.MethodParameters = m.Parameters;
            this.MemberKind = m.Name == ".ctor" ? MemberKind.Constructor : MemberKind.Method;

            (this.IsRefReturn, this.IsRequireReadOnly) = (m.ReturnsByRef, m.ReturnsByRefReadonly) switch
            {
                (true, true) => (true, true), // but maybe don't come here.
                (true, false) => (true, false),
                (false, true) => (true, true),
                (false, false) => (false, false),
            };
        }
        else
        {
            throw new InvalidOperationException("Symbol type is invalid. " + symbol.GetType().FullName);
        }
        this.MemberTypeFullName = this.MemberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }
}
