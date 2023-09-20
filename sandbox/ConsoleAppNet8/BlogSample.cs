#pragma warning disable CS8604
#pragma warning disable CS8321
#pragma warning disable CS0414
#pragma warning disable CS0169
#pragma warning disable CS0649

using PrivateProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;



//public class Sample
//{
//    int _field1;
//    int _field2;
//    public int PrivateProperty { get; private set; }
//    int PrivateAdd(int x, int y) => x + y;
//}


//[GeneratePrivateProxy(typeof(Sample))]
//public partial struct SampleProxy;


//public struct MutableStructSample
//{
//    int _counter;
//    void Increment() => _counter++;

//    // static and ref sample
//    static ref int GetInstanceCounter(ref MutableStructSample sample) => ref sample._counter;
//}

//// use ref partial struct
//[GeneratePrivateProxy(typeof(MutableStructSample))]
//public ref partial struct MutableStructSampleProxy;


//public static class Calle
//{
//    public static void Foo()
//    {
//var sample = new MutableStructSample();
//var proxy = sample.AsPrivateProxy();
//proxy.Increment();
//proxy.Increment();
//proxy.Increment();

//// call private static method.
//ref var counter = ref MutableStructSampleProxy.GetInstanceCounter(ref sample);

//Console.WriteLine(counter); // 3
//counter = 9999;
//Console.WriteLine(proxy._counter); // 9999
//    }
//}



public class Sample;

// class proxy type both supports class and struct(recommend is struct)
[GeneratePrivateProxy(typeof(Sample))]
public partial class SampleProxy;

[GeneratePrivateProxy(typeof(Sample))]
public partial struct SampleProxy2;

public struct SamplleStruct;

// struct only supports ref struct
[GeneratePrivateProxy(typeof(SamplleStruct))]
public ref partial struct SamplleStructProxy;