using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrivateProxy.Tests;

public class DiagnosticsTest
{
    void Compile(int id, string code, bool allowMultipleError = false)
    {
        var diagnostics = CSharpGeneratorRunner.RunGenerator(code);
        if (!allowMultipleError)
        {
            diagnostics.Length.Should().Be(1);
            diagnostics[0].Id.Should().Be("PP" + id.ToString("000"));
        }
        else
        {
            diagnostics.Select(x => x.Id).Should().Contain("PP" + id.ToString("000"));
        }
    }

    [Fact]
    public void PP001_MustBePartial()
    {
        Compile(1, """
using PrivateProxy;

public struct Foo
{
    int X;
}

[GeneratePrivateProxy(typeof(Foo))]
public ref struct FooPrivateProxy { }
""");
    }

    [Fact]
    public void PP002_NotAllowReadOnly()
    {
        Compile(2, """
using PrivateProxy;

public struct Foo
{
    int X;
}

[GeneratePrivateProxy(typeof(Foo))]
public readonly ref partial struct FooPrivateProxy { }
""");
    }

    [Fact]
    public void PP003_ClassNotAllowRefStruct()
    {
        Compile(3, """
using PrivateProxy;

public class Foo
{
    int X;
}

[GeneratePrivateProxy(typeof(Foo))]
public ref partial struct FooPrivateProxy { }
""");
    }

    [Fact]
    public void PP004_StructNotAllowClass()
    {
        Compile(4, """
using PrivateProxy;

public struct Foo
{
    int X;
}

[GeneratePrivateProxy(typeof(Foo))]
public partial class FooPrivateProxy { }
""");
    }

    [Fact]
    public void PP005_StructNotAllowStruct()
    {
        Compile(5, """
using PrivateProxy;

public struct Foo
{
    int X;
}

[GeneratePrivateProxy(typeof(Foo))]
public partial struct FooPrivateProxy { }
""");
    }

    [Fact]
    public void PP006_RefStructNotSupported()
    {
        Compile(6, """
using PrivateProxy;

public ref struct Foo
{
    int X;
}

[GeneratePrivateProxy(typeof(Foo))]
public ref partial struct FooPrivateProxy { }
""");
    }

    [Fact]
    public void PP007_GenericsNotSupported()
    {
        Compile(7, """
using PrivateProxy;

public class Foo<T>
{
    T X;
}

[GeneratePrivateProxy(typeof(Foo<int>))]
public partial class FooPrivateProxy { }
""");

        Compile(7, """
using PrivateProxy;

public class Foo<T>
{
    T X;
}

[GeneratePrivateProxy(typeof(Foo<>))]
public partial class FooPrivateProxy { }
""");
    }
}