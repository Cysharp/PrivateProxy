namespace PrivateProxy.Tests;

public class RefStructGenerateTest
{
    [Fact]
    public void CheckRefStructField()
    {
        var diagnostics = CSharpGeneratorRunner.RunGenerator("""
using PrivateProxy;

public ref struct RefStructTest
{
    public int field;                    // RefKind=None
    public readonly int readOnlyField;   // RefKind=None, IsReadOnly=true
    public ref int refField;             // RefKind=Ref
    public ref readonly int refReadOnly; // RefKind=In
    public readonly ref int readonlyRef; // RefKind=Ref, IsReadOnly=true
    public readonly ref readonly int readOnlyRefReadOnly;
}

[GeneratePrivateProxy(typeof(RefStructTest))]
public partial struct Proxy { }
""");

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void CheckRefStructProperty()
    {
        var diagnostics = CSharpGeneratorRunner.RunGenerator("""
using PrivateProxy;

public ref struct RefStructTest
{
    public ref int refField;

    public int Prop0 => 1;
    public readonly int Prop1 => 1;
    public ref int Prop2 => ref refField; // ReturnsByRef:true
    public ref readonly int Prop3 => ref refField; // ReturnsByRefReadOnly:true | can't assign
    public readonly ref int Prop4 => ref refField;  // ReturnsByRef:true
    public readonly ref readonly int Prop5 => ref refField;// ReturnsByRefReadOnly:true | can't assign
}

[GeneratePrivateProxy(typeof(RefStructTest))]
public partial struct Proxy { }
""");

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void CheckRefStructMethod()
    {
        var diagnostics = CSharpGeneratorRunner.RunGenerator("""
using PrivateProxy;

public ref struct RefStructTest
{
    public ref int refField;

    public int Method0() => 1;
    public readonly int Method1() => 1;
    public ref int Method2() => ref refField;
    public ref readonly int Method3() => ref refField;
    public readonly ref int Method4() => ref refField;
    public readonly ref readonly int Method5() => ref refField;
}

[GeneratePrivateProxy(typeof(RefStructTest))]
public partial struct Proxy { }
""");

        diagnostics.Should().BeEmpty();
    }
}
