#pragma warning disable CS0649
#pragma warning disable CS0169
#pragma warning disable CS0414

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
// TODO: return nested
public class PrivateClassTarget
{
    public PrivateClassTarget(int readOnlyField)
    {
        this.readOnlyField = readOnlyField;
        this.GetOnlyProperty = readOnlyField;
        this._refReadOnlyGetOnlyPropertyField = readOnlyField;
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

    int _refGetOnlyPropertyField;
    private ref int RefGetOnlyProperty => ref _refGetOnlyPropertyField;

    int _refReadOnlyGetOnlyPropertyField;
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

    int _refReturnMethodField;
    private ref int RefReturnMethod() => ref _refReturnMethodField;

    int _refReadOnlyReturnMethodField;
    private ref readonly int RefReadOnlyReturnMethod() => ref _refReadOnlyReturnMethodField;

    int _refReturnMethodParameterField;
    private ref int RefReturnMethodParameter(ref int x)
    {
        return ref x;
    }

    // checker
    public (int field, int readOnlyField) GetFields() => (field, readOnlyField);
    public (int property, int getOnlyProperty, int getOnlyPrivate, int setOnlyPrivate, int setOnlyProperty) GetProperties() => (Property, GetOnlyProperty, GetOnlyPrivateProperty, SetOnlyPrivateProperty, _setOnlyPropertyField);

    public (int refGetOnlyPropertyField, int _refReadOnlyGetOnlyPropertyField) GetRefProperties() => (_refGetOnlyPropertyField, _refReadOnlyGetOnlyPropertyField);


    // static

    static int staticField;
    static readonly int staticReadOnlyField = 4444;


    static int StaticProperty { get; set; }
    static int StaticGetOnlyProperty { get; } = 5000;
    public static int StaticGetOnlyPrivateProperty { private get; set; }
    public static int StaticSetOnlyPrivateProperty { get; private set; }

    static int _StaticsetOnlyPropertyField;
    private static int StaticSetOnlyProperty { set => _StaticsetOnlyPropertyField = value; }

    static int _StaticrefGetOnlyPropertyField;
    private static ref int StaticRefGetOnlyProperty => ref _StaticrefGetOnlyPropertyField;

    static int _StaticrefReadOnlyGetOnlyPropertyField;
    private static ref readonly int StaticRefReadOnlyGetOnlyProperty => ref _StaticrefReadOnlyGetOnlyPropertyField;


    public static int staticvoidMethodCalledCount;

    private static void StaticVoidMethod()
    {
        staticvoidMethodCalledCount++;
    }

    static int StaticReturnMethod() => 10;

    static int StaticParameterMethod1(int x) => x;

    static int StaticParameterMethod2(int x, int y) => x + y;

    static void StaticVoidParameter(int x, int y)
    {
        staticvoidMethodCalledCount = x + y;
    }

    static void StaticRefOutInMethod(in int x, ref int y, out int z, ref readonly int xyz)
    {
        y = 40;
        z = 9999;
        staticvoidMethodCalledCount = x + xyz;
    }

    static int _StaticrefReturnMethodField;
    static ref int StaticRefReturnMethod() => ref _StaticrefReturnMethodField;

    static int _StaticrefReadOnlyReturnMethodField;
    static ref readonly int StaticRefReadOnlyReturnMethod() => ref _StaticrefReadOnlyReturnMethodField;

    static ref int StaticRefReturnMethodParameter(ref int x)
    {
        return ref x;
    }
}


#if true
[GeneratePrivateProxy(typeof(PrivateClassTarget))]
public partial struct PrivateClassTargetStructProxy;

#else
[GeneratePrivateProxy(typeof(PrivateClassTarget))]
public partial class PrivateClassTargetStructProxy; // ClassProxy but use same name...
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
        var target = new PrivateClassTarget(5000) { RequiredInitProperty = 0, RequiredProperty = 0 };
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
        var target = new PrivateClassTarget(5000) { RequiredInitProperty = 0, RequiredProperty = 0 };
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
        proxy._refReturnMethodField.Should().Be(1111);

        proxy._refReadOnlyReturnMethodField = 99999;
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
        PrivateClassTargetStructProxy.staticField = 9999;
        PrivateClassTargetStructProxy.staticField.Should().Be(9999);
        PrivateClassTargetStructProxy.staticReadOnlyField.Should().Be(4444);
    }

    [Fact]
    public void StaticProperty()
    {
        PrivateClassTargetStructProxy.StaticProperty = 9999;
        PrivateClassTargetStructProxy.StaticGetOnlyPrivateProperty = 8888;
        PrivateClassTargetStructProxy.StaticSetOnlyPrivateProperty = 7777;

        PrivateClassTargetStructProxy.StaticSetOnlyProperty = 6666;

        ref var refGet = ref PrivateClassTargetStructProxy.StaticRefGetOnlyProperty;
        ref readonly var refRead = ref PrivateClassTargetStructProxy.StaticRefReadOnlyGetOnlyProperty;

        refGet = 5555;

        PrivateClassTargetStructProxy._StaticrefReadOnlyGetOnlyPropertyField = 5000;

        PrivateClassTargetStructProxy.StaticProperty.Should().Be(9999);
        PrivateClassTargetStructProxy.StaticGetOnlyPrivateProperty.Should().Be(8888);
        PrivateClassTargetStructProxy.StaticSetOnlyPrivateProperty.Should().Be(7777);
        PrivateClassTargetStructProxy.StaticGetOnlyProperty.Should().Be(5000);

        PrivateClassTargetStructProxy._StaticsetOnlyPropertyField.Should().Be(6666);

        PrivateClassTargetStructProxy.StaticRefGetOnlyProperty.Should().Be(5555);

        PrivateClassTargetStructProxy._StaticrefGetOnlyPropertyField.Should().Be(5555);
        PrivateClassTargetStructProxy._StaticrefReadOnlyGetOnlyPropertyField.Should().Be(5000).And.Be(refRead);
    }

    [Fact]
    public void StaticMethod()
    {
        PrivateClassTargetStructProxy.StaticVoidMethod();
        PrivateClassTargetStructProxy.StaticVoidMethod();
        PrivateClassTargetStructProxy.StaticVoidMethod();

        PrivateClassTarget.staticvoidMethodCalledCount.Should().Be(3);


        PrivateClassTargetStructProxy.StaticReturnMethod().Should().Be(10);
        PrivateClassTargetStructProxy.StaticParameterMethod1(9999).Should().Be(9999);

        PrivateClassTargetStructProxy.StaticParameterMethod2(10, 20).Should().Be(30);

        PrivateClassTargetStructProxy.StaticVoidParameter(10, 20);
        PrivateClassTarget.staticvoidMethodCalledCount.Should().Be(30);

        var y = 0;
        var zz = 20;
        PrivateClassTargetStructProxy.StaticRefOutInMethod(10, ref y, out var z, in zz);
        y.Should().Be(40);
        z.Should().Be(9999);
        PrivateClassTarget.staticvoidMethodCalledCount.Should().Be(30); // x + xyz

        PrivateClassTargetStructProxy.StaticRefReturnMethod() = 1111;
        PrivateClassTargetStructProxy._StaticrefReturnMethodField.Should().Be(1111);

        PrivateClassTargetStructProxy._StaticrefReadOnlyReturnMethodField = 99999;
        PrivateClassTargetStructProxy.StaticRefReadOnlyReturnMethod().Should().Be(99999);

        var zzzzz = 999;
        ref var xxxxx = ref PrivateClassTargetStructProxy.StaticRefReturnMethodParameter(ref zzzzz);
        xxxxx.Should().Be(999);
        xxxxx = 111;
        zzzzz.Should().Be(111);
    }
}