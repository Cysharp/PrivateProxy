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

    // field
    private int field;
    private readonly int readOnlyField = 99999;
    public required int requiredField;

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


}

[GeneratePrivateProxy(typeof(PrivateClassTarget))]
public partial struct PrivateClassTargetProxy { }


public ref struct RefStruct
{
    public ref readonly int field1;
    public readonly ref int field2;
    static int xxx;
    public readonly ref int Foo => ref xxx;
}

public class ClassGenerateTest
{
    [Fact]
    public void Test1()
    {
        var target = new PrivateClassTarget() { requiredField = 0, RequiredInitProperty = 0, RequiredProperty = 0 };
        var proxy = target.AsPrivateProxy();

        proxy.field = 999;


        var refS = new RefStruct();
        //refS.field1 = 10;
        refS.field2 = 10;

        // .Property = 10;
        // proxy.readOnlyField2 = 11;


        var (field, readOnlyField) = target.GetFields();


        field.Should().Be(999).And.Be(proxy.field);



    }
}