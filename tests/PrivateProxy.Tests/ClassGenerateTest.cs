using Microsoft.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace PrivateProxy.Tests;

// TODO: test inheritance
// TODO: check explicit interface member
// TODO: inheritance class uses new?
// TODO: struct target allows readonly ref readonly
// TODO: ref field for ref struct
// TODO: required?
// TODO: target record, record struct
public class PrivateClassTarget
{
    // TODO: use internal type for return or parameter(detect and should return object).

    public PrivateClassTarget(int readOnlyField)
    {
        this.readOnlyField = readOnlyField;
        this.GetOnlyProperty = readOnlyField;
    }

    // public(no generate)
    public int publicField;
    public int PublicProperty { get; set; }
    public void PublicMethod() { }

    // field
    private int field;
    private readonly int readOnlyField;
    // public required int requiredField;

    // proeprty
    private int Property { get; set; }
    private int GetOnlyProperty { get; }
    public int GetOnlyPrivateProperty { private get; set; }
    public int SetOnlyPrivateProperty { get; private set; }
    public required int RequiredProperty { get; set; }
    public int InitProperty { get; init; }
    public required int RequiredInitProperty { get; init; }

    int _setOnlyPropertyField;
    public int SetOnlyProperty { set => _setOnlyPropertyField = value; }

    int _refGetOnlyPropertyField;
    public ref int RefGetOnlyProperty => ref _refGetOnlyPropertyField;

    int _refReadOnlyGetOnlyPropertyField;
    public ref readonly int RefReadOnlyGetOnlyProperty => ref _refReadOnlyGetOnlyPropertyField;

    // method
    private void VoidMethod()
    {
    }

    private int ReturnMethod() => 10;

    private int ParameterMethod1(int x) => x;

    private int ParameterMethod2(int x, int y) => x + y;

    public void VoidParameter(int x, int y)
    {
    }

    public void RefOutInMethod(in int x, ref int y, out int z, ref readonly int xyz)
    {
        z = 0;
    }

    int _refReturnMethodField;
    private ref int RefReturnMethod() => ref _refReturnMethodField;

    int _refReadOnlyReturnMethodField;
    private ref readonly int RefReadOnlyReturnMethod() => ref _refReadOnlyReturnMethodField;

    int _refReturnMethodParameterField;
    private ref int RefReturnMethodParameter(int x)
    {
        _refReturnMethodParameterField += x;
        return ref _refReturnMethodField;
    }

    // TODO: static



    // checker
    public (int field, int readOnlyField) GetFields() => (field, readOnlyField);
    public (int property, int getOnlyProperty, int setOnlyProperty) GetProperties() => (Property, GetOnlyProperty, _setOnlyPropertyField);



    // kesu.

}


#if true
[GeneratePrivateProxy(typeof(PrivateClassTarget))]
public partial struct PrivateClassTargetStructProxy;

#else
[GeneratePrivateProxy(typeof(PrivateClassTarget))]
public partial class PrivateClassTargetClassProxy;
#endif

public class ClassGenerateTest
{
    [Fact]
    public void Field()
    {
        var target = new PrivateClassTarget(5000) { RequiredInitProperty = 0, RequiredProperty = 0 };
        var proxy = target.AsPrivateProxy();

        proxy.field = 1000;
        
        var (field, readOnlyField) = target.GetFields();

        field.Should().Be(1000).And.Be(proxy.field);
        readOnlyField.Should().Be(5000).And.Be(proxy.readOnlyField);
    }

    [Fact]
    public void Property()
    {
        //private int Property { get; set; }
        //private int GetOnlyProperty { get; }
        //public int GetOnlyPrivateProperty { private get; set; }
        //public int SetOnlyPrivateProperty { get; private set; }
        //public required int RequiredProperty { get; set; }
        //public int InitProperty { get; init; }
        //public required int RequiredInitProperty { get; init; }

        //int _setOnlyPropertyField;
        //public int SetOnlyProperty { set => _setOnlyPropertyField = value; }

        //int _refGetOnlyPropertyField;
        //public ref int RefGetOnlyProperty => ref _refGetOnlyPropertyField;

        //int _refReadOnlyGetOnlyPropertyField;
        //public ref readonly int RefReadOnlyGetOnlyProperty => ref _refReadOnlyGetOnlyPropertyField;

        var target = new PrivateClassTarget(5000) { RequiredInitProperty = 0, RequiredProperty = 0 };
        var proxy = target.AsPrivateProxy();

        proxy.Property = 9999;
        proxy.GetOnlyPrivateProperty = 8888;
        proxy.SetOnlyPrivateProperty = 7777;
        // proxy.RequiredInitProperty
        
        
        // proxy.GetOnlyProperty = 444;

    }

    [Fact]
    public void Method()
    {
        // TODO:
    }

    [Fact]
    public void StaticField()
    {
        // TODO:
    }

    [Fact]
    public void StaticProperty()
    {
        // TODO:
    }

    [Fact]
    public void StaticMethod()
    {
        // TODO:
    }
}