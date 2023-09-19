#pragma warning disable CS0649
#pragma warning disable CS0169
#pragma warning disable CS0414

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrivateProxy.Tests;

public struct PrivateStructTarget
{
    // TODO: use internal type for return or parameter(detect and should return object).

    public PrivateStructTarget(int readOnlyField)
    {
        this.readOnlyField = readOnlyField;
        this.GetOnlyProperty = readOnlyField;
        // this._refReadOnlyGetOnlyPropertyField = readOnlyField;
    }

    // public(no generate)
    public int publicField;
    public int PublicProperty { get; set; }
    public void PublicMethod() { }

    // field
    private int field;
    private readonly int readOnlyField;
    // public required int requiredField;

    // property
    private int Property { get; set; }
    private int GetOnlyProperty { get; }
    public int GetOnlyPrivateProperty { private get; set; }
    public int SetOnlyPrivateProperty { get; private set; }
    public required int RequiredProperty { get; set; }
    public int InitProperty { get; init; }
    public required int RequiredInitProperty { get; init; }

    int _setOnlyPropertyField;
    private int SetOnlyProperty { set => _setOnlyPropertyField = value; }

    public static int _refGetOnlyPropertyField;
    private ref int RefGetOnlyProperty => ref _refGetOnlyPropertyField;

    public static int _refReadOnlyGetOnlyPropertyField;
    private ref readonly int RefReadOnlyGetOnlyProperty => ref _refReadOnlyGetOnlyPropertyField;

    // method
    public int voidMethodCalledCount;

    private void VoidMethod()
    {
        voidMethodCalledCount++;
    }

    private int ReturnMethod() => 10;

    private int ParameterMethod1(int x) => x;

    private int ParameterMethod2(int x, int y) => x + y;

    private void VoidParameter(int x, int y)
    {
        voidMethodCalledCount = x + y;
    }

    private void RefOutInMethod(in int x, ref int y, out int z, ref readonly int xyz)
    {
        y = 40;
        z = 9999;
        voidMethodCalledCount = x + xyz;
    }

    public static int _refReturnMethodField;
    ref int RefReturnMethod() => ref _refReturnMethodField;

    public static int _refReadOnlyReturnMethodField;
    private ref readonly int RefReadOnlyReturnMethod() => ref _refReadOnlyReturnMethodField;

    int _refReturnMethodParameterField;
    private ref int RefReturnMethodParameter(ref int x)
    {
        return ref x;
    }

    // TODO: static



    // checker
    public (int field, int readOnlyField) GetFields() => (field, readOnlyField);
    public (int property, int getOnlyProperty, int getOnlyPrivate, int setOnlyPrivate, int setOnlyProperty) GetProperties() => (Property, GetOnlyProperty, GetOnlyPrivateProperty, SetOnlyPrivateProperty, _setOnlyPropertyField);

    public (int refGetOnlyPropertyField, int _refReadOnlyGetOnlyPropertyField) GetRefProperties() => (_refGetOnlyPropertyField, _refReadOnlyGetOnlyPropertyField);



    

}

[GeneratePrivateProxy(typeof(PrivateStructTarget))]
public ref partial struct PrivateStructTargetStructProxy;



public class StructGenerateTest
{
    [Fact]
    public void Field()
    {
        var target = new PrivateStructTarget(5000) { RequiredInitProperty = 0, RequiredProperty = 0 };
        
        var proxy = target.AsPrivateProxy();

        proxy.field = 1000;

        var (field, readOnlyField) = target.GetFields();

        field.Should().Be(1000).And.Be(proxy.field);
        readOnlyField.Should().Be(5000).And.Be(proxy.readOnlyField);
    }

    [Fact]
    public void Property()
    {
        var target = new PrivateStructTarget(5000) { RequiredInitProperty = 0, RequiredProperty = 0 };
        PrivateStructTarget._refReadOnlyGetOnlyPropertyField = 5000;
        var proxy = target.AsPrivateProxy();

        proxy.Property = 9999;
        proxy.GetOnlyPrivateProperty = 8888;
        proxy.SetOnlyPrivateProperty = 7777;

        proxy.SetOnlyProperty = 6666;

        ref var refGet = ref proxy.RefGetOnlyProperty;
        ref readonly var refRead = ref proxy.RefReadOnlyGetOnlyProperty;

        refGet = 5555;
        // refRead = 100; can't write

        var (prop, getOnly, getOnlyPrivate, setOnlyPrivate, setOnly) = target.GetProperties();

        prop.Should().Be(9999).And.Be(proxy.Property);
        getOnlyPrivate.Should().Be(8888).And.Be(proxy.GetOnlyPrivateProperty);
        setOnlyPrivate.Should().Be(7777).And.Be(proxy.SetOnlyPrivateProperty);
        getOnly.Should().Be(5000).And.Be(proxy.GetOnlyProperty);

        setOnly.Should().Be(6666);

        var (refReadOnly, refReadOnly2) = target.GetRefProperties();

        proxy.RefGetOnlyProperty.Should().Be(5555);


        refGet.Should().Be(5555).And.Be(refReadOnly);
        refReadOnly2.Should().Be(5000).And.Be(refRead);
    }

    [Fact]
    public void Method()
    {
        var target = new PrivateStructTarget(5000) { RequiredInitProperty = 0, RequiredProperty = 0 };
        var proxy = target.AsPrivateProxy();

        proxy.VoidMethod();
        proxy.VoidMethod();
        proxy.VoidMethod();

        target.voidMethodCalledCount.Should().Be(3);


        proxy.ReturnMethod().Should().Be(10);
        proxy.ParameterMethod1(9999).Should().Be(9999);

        proxy.ParameterMethod2(10, 20).Should().Be(30);

        proxy.VoidParameter(10, 20);
        target.voidMethodCalledCount.Should().Be(30);

        var y = 0;
        var zz = 20;
        proxy.RefOutInMethod(10, ref y, out var z, in zz);
        y.Should().Be(40);
        z.Should().Be(9999);
        target.voidMethodCalledCount.Should().Be(30); // x + xyz

        proxy.RefReturnMethod() = 1111;
        PrivateStructTarget._refReturnMethodField.Should().Be(1111);

        PrivateStructTarget._refReadOnlyReturnMethodField = 99999;
        proxy.RefReadOnlyReturnMethod().Should().Be(99999);

        var zzzzz = 999;
        ref var xxxxx = ref proxy.RefReturnMethodParameter(ref zzzzz);
        xxxxx.Should().Be(999);
        xxxxx = 111;
        zzzzz.Should().Be(111);
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