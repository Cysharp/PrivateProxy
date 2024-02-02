# PrivateProxy
[![GitHub Actions](https://github.com/Cysharp/PrivateProxy/workflows/Build-Debug/badge.svg)](https://github.com/Cysharp/PrivateProxy/actions) [![Releases](https://img.shields.io/github/release/Cysharp/PrivateProxy.svg)](https://github.com/Cysharp/PrivateProxy/releases)
[![NuGet package](https://img.shields.io/nuget/v/PrivateProxy.svg)](https://nuget.org/packages/PrivateProxy)

Source Generator and .NET 8 [UnsafeAccessor](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.unsafeaccessorattribute) based high-performance strongly-typed private accessor for unit testing and runtime.

`[GeneratePrivateProxy(typeof(TargetType))]` generates accessor proxy.

```csharp
using PrivateProxy;

public class Sample
{
    int _field1;
    int PrivateAdd(int x, int y) => x + y;
}

[GeneratePrivateProxy(typeof(Sample))]
public partial struct SampleProxy;
```

```csharp
// Source Generator generate this type
partial struct SampleProxy(Sample target)
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_field1")]
    static extern ref int ___field1__(Sample target);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "PrivateAdd")]
    static extern int __PrivateAdd__(Sample target, int x, int y);

    public ref int _field1 => ref ___field1__(target);
    public int PrivateAdd(int x, int y) => __PrivateAdd__(target, x, y);
}

public static class SamplePrivateProxyExtensions
{
    public static SampleProxy AsPrivateProxy(this Sample target)
    {
        return new SampleProxy(target);
    }
}
```

```csharp
// You can access like this.
var sample = new Sample();
sample.AsPrivateProxy()._field1 = 10;
```

Generated code is fully typed, you can access private filed via IntelliSense and when private field was changed, can check compiler error.

![image](https://github.com/Cysharp/MemoryPack/assets/46207/f6dd22e1-e82e-4acc-ba6e-8895c8c8734b)

* No performance penalty, it can be used not only for unit testing but also for runtime
* No runtime dependency(all codes are included in source generator)
* Private accessors are strongly-typed
* Supports both instance, static and fields, properties, methods
* Supports `ref`, `out`, `in`, and `ref readonly` method parameters
* Supports `readonly` field and property
* Supports `ref` return
* Supports mutable struct
* Supports instance constructor

For example, this is the mutable struct and static, ref return, and constructor sample.

```csharp
using PrivateProxy;

public struct MutableStructSample
{
    int _counter;
    void Increment() => _counter++;

    // static and ref sample
    static ref int GetInstanceCounter(ref MutableStructSample sample) => ref sample._counter;
    
    // constructor sample
    MutalbeStructSample(int x, int y) { /* ... */ }
}

// use ref partial struct
[GeneratePrivateProxy(typeof(MutableStructSample))]
public ref partial struct MutableStructSampleProxy;
```

```csharp
var sample = new MutableStructSample();
var proxy = sample.AsPrivateProxy();
proxy.Increment();
proxy.Increment();
proxy.Increment();

// call private static method.
ref var counter = ref MutableStructSampleProxy.GetInstanceCounter(ref sample);

Console.WriteLine(counter); // 3
counter = 9999;
Console.WriteLine(proxy._counter); // 9999

// call private constructor and create instance.
var sample = MutableStructSampleProxy.CreateMutableStructFromConstructor(111, 222);
```

Installation
---
This library is distributed via NuGet, minimum requirement is .NET 8 and C# 12.

PM> Install-Package [PrivateProxy](https://www.nuget.org/packages/PrivateProxy)

Package provides only analyzer and generated code does not dependent any other libraries.

Supported Members
---
`GeneratePrivateProxy` target type and member

```csharp
public class/* struct */ SupportTarget
{
    // field
    private int field;
    private readonly int readOnlyField;
    
    // property
    private int Property { get; set; }
    private int GetOnlyProperty { get; }
    public int GetOnlyPrivateProperty { private get; set; }
    public int SetOnlyPrivateProperty { get; private set; }
    private int SetOnlyProperty { set => field = value; }
    private ref int RefGetOnlyProperty => ref field;
    private ref readonly int RefReadOnlyGetOnlyProperty => ref field;

    // method
    private void VoidMethod() { }
    private int ReturnMethod() => field;
    private int ParameterMethod(int x, int y) => x + y;
    private void RefOutInMethod(in int x, ref int y, out int z, ref readonly int xyz) { z = field; }
    private ref int RefReturnMethod() => ref field;
    private ref readonly int RefReadOnlyReturnMethod() => ref field;

    // static
    static int staticField;
    static readonly int staticReadOnlyField;
    static int StaticProperty { get; set; }
    static int StaticGetOnlyProperty { get; }
    public static int StaticGetOnlyPrivateProperty { private get; set; }
    public static int StaticSetOnlyPrivateProperty { get; private set; }
    private static int StaticSetOnlyProperty { set => staticField = value; }
    private static ref int StaticRefGetOnlyProperty => ref staticField;
    private static ref readonly int StaticRefReadOnlyGetOnlyProperty => ref staticField;
    private static void StaticVoidMethod() { }
    static int StaticReturnMethod() => staticField;
    static int StaticParameterMethod(int x, int y) => x + y;
    static void StaticRefOutInMethod(in int x, ref int y, out int z, ref readonly int xyz) { z = staticField; }
    static ref int StaticRefReturnMethod() => ref staticField;
    static ref readonly int StaticRefReadOnlyReturnMethod() => ref staticField;
    static ref int StaticRefReturnMethodParameter() => ref staticField;
    
    // constructor
    SupportTarget() { }
    SupportTarget(int x, int y) { }
}
```

Proxy type can be `class` => `class` or `struct`, `struct` => `ref struct`.

```csharp
using PrivateProxy;

public class Sample;

// class proxy type both supports class and struct(recommend is struct)
[GeneratePrivateProxy(typeof(Sample))]
public partial class SampleProxy1;

[GeneratePrivateProxy(typeof(Sample))]
public partial struct SampleProxy2;

public struct SamplleStruct;

// struct only supports ref struct(when use standard struct, analyzer shows error)
[GeneratePrivateProxy(typeof(SamplleStruct))]
public ref partial struct SamplleStructProxy;
```

GeneratePrivateProxyAttribute has two constructor, when use `PrivateProxyGenerateKinds` parameter, can configure generate member kind.

```csharp
public GeneratePrivateProxyAttribute(Type target) // use PrivateProxyGenerateKinds.All
public GeneratePrivateProxyAttribute(Type target, PrivateProxyGenerateKinds generateKinds)

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
```

Limitation
---
Currently, the following features are not supported

* Generics type
  * see: [dotnet/runtime#89439 Implement unbound Generic support for UnsafeAccessorAttribute](https://github.com/dotnet/runtime/issues/89439)
* Static class, Non public return/parameter type(ignore generate)
  * see: [dotnet/runtime#90081 UnsafeAccessorTypeAttribute for static or private type access](https://github.com/dotnet/runtime/issues/90081)
* ref struct
  * ref field can not pass to ref method parameter
* Types from external dll(for example `String`)
  * Probably, by enabling MetadataImportOptions.All in the Source Generator, it should be possible to read it. However, I haven't been able to find a way to do that. I need help.

License
---
This library is licensed under the MIT License.
