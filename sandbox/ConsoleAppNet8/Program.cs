#pragma warning disable CS8604
#pragma warning disable CS8321
#pragma warning disable CS0414
#pragma warning disable CS0169
#pragma warning disable CS0649

// using PrivateProxy;
using PrivateProxy;
using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


Calle.Foo();


static void Bar(ref int x, out int y, in int z)
{
    Foo(ref x, out y, in z);
}


static void Foo(ref int x, out int y, in int z)
{
    y = 10;
}




[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "field")]
static extern ref int UnsafeAccessorTest(ref global::StructTest target);

[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "field")]
static extern ref int UnsafeAccessorTest2(global::MyClass target);

[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "TakoyakiX")]
static extern int UnsafeAccessorTest3(in global::MyClass target, int x, int y);

public partial struct Hoge
{
    static Hoge ____default__ = default(Hoge);

    public void Tako() { }

    public int MyProperty { get; set; }
    static int y;
    public ref readonly int MyProperty2 { get => ref y; }

    public int Foo
    {
        set
        {
        }
    }


    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "TakoyakiX")]
    static extern int UnsafeAccessorTest3(ref global::Hoge target, int x, int y);

    public static int CallTakoyakiX(int x, int y) => UnsafeAccessorTest3(ref ____default__, x, y);

    static int TakoyakiX(int x, int y)
    {
        return x + y;
    }
}


[GeneratePrivateProxy(typeof(ClassLibrary.MyClass))]
public partial struct ClassLibraryMyClassProxy;


public class MyClass
{
    int field;

    public int MyProperty { get; set; }

    public void Show() => Console.WriteLine(field);


    public int Show2 => field;


    public ref readonly int RetField() => ref field;


    static int TakoyakiX(int x, int y)
    {
        return x + y;
    }
}


public static class ExtTest
{
    public static MyClassProxy AsProxy2(this MyClass mc)
    {
        return new MyClassProxy(mc);
    }
}


//public ref struct MyClass
//{

//}

ref partial struct Tako
{
}

public struct MyClassProxy
{
    MyClass target;

    public MyClassProxy(MyClass target)
    {
        this.target = target;
    }
}


public struct TestRef
{
}

public ref struct TestRefRef
{
    ref TestRef target;

    public TestRefRef(ref TestRef target)
    {
        this.target = ref target;
    }



    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "field")]
    static extern ref int __field__(ref global::TestRef target);

    public ref int field => ref __field__(ref target);

    public int field2
    {
        get => __field__(ref target);
        set => __field__(ref target) = value;
    }

}



public struct StructTest
{
    //public StructTest(ref int xxx)
    //{
    //this.refReadOnly = ref xxx;
    //}

    // Roslyn IFieldSymbol behaviour
    public int field;                                     // RefKind=None
    public readonly int readOnlyField;                    // RefKind=None, IsReadOnly=true | can't assign
    //public ref int refField;                              // RefKind=Ref
    //public ref readonly int refReadOnly;                  // RefKind=In | can't assign
    //public readonly ref int readonlyRef;                  // RefKind=Ref, IsReadOnly=true
    //public readonly ref readonly int readOnlyRefReadOnly; // RefKind=In, IsReadOnly=true | can't assign




    // Roslyn IPropertySymbol behaviour
    public int Prop0 => 1;
    public readonly int Prop1 => 1;
    //public ref int Prop2 => ref refField;                 // ReturnsBy
    //public ref readonly int Prop3 => ref refField;
    //public readonly ref int Prop4 => ref refField;
    //public readonly ref readonly int Prop5 => ref refField;

    // Roslyn IMethodSYmbol behaviour
    public int Method0() => 1;
    public readonly int Method1() => 1;
    //public ref int Method2() => ref refField;
    //public ref readonly int Method3() => ref refField;
    //public readonly ref int Method4() => ref refField;
    //public readonly ref readonly int Method5() => ref refField;

    public void Show()
    {
        Console.WriteLine(field);
    }

    static void HogeMogeDayo()
    {
    }
}

//public ref partial struct RefStructTestProxy2
//{
//    RefStructTest target;

//    public RefStructTestProxy2(ref RefStructTest target)
//    {
//        this.target = target;
//    }

//    static global::RefStructTest ____static_instance => default;

//    public RefStructTestProxy2(ref RefStructTest test)
//    {
//        // this.target = MemoryMarshal.GetReference(test;
//    }


//    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "HogeMogeDayo")]
//    static extern void __HogeMogeDayo__(in global::RefStructTest target);

//    public static void HogeMogeDayo()
//    {
//        __HogeMogeDayo__(____static_instance);
//    }


//}




[GeneratePrivateProxy(typeof(StructTest))]
public ref partial struct StructTestProxy;



partial struct PrivateClass2Proxy2
{
    global::PrivateClass2 target;

    public PrivateClass2Proxy2(global::PrivateClass2 target)
    {
        this.target = target;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_privateField")]
    static extern ref int ___privateField__(global::PrivateClass2 target);
    public ref int _privateField => ref ___privateField__(target);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_PrivateMyProperty")]
    static extern int __get_PrivateMyProperty__(global::PrivateClass2 target);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_PrivateMyProperty")]
    static extern void __set_PrivateMyProperty__(global::PrivateClass2 target, int value);

    public int PrivateMyProperty
    {
        get => __get_PrivateMyProperty__(target);
        set => __set_PrivateMyProperty__(target, value);
    }



    public void Tako() => Console.WriteLine("hoge");
}





public class PrivateClass
{
    private int _privateField;
    private static int _privateStaticField;

    private int PrivateMethod() => 10;
    private static int PrivateStaticMethod() => 99;


    public int PublicField;
    public readonly int ReadOnlyPublicField;
}


public class PrivateClass2
{
    private int _privateField;

    private int PrivateMyProperty { get; set; }

    public void ShowPrivateField()
    {


        Console.WriteLine(_privateField);
    }

    void Show2()
    {
        Console.WriteLine("2");
    }



    void ShowWithP(int x, int y)
    {
        Console.WriteLine(x + y);
    }

    int FooBarBaz(ref int x, out int y, in int z) => y = x + z;
}

[GeneratePrivateProxy(typeof(PrivateClass2))]
partial struct PrivateClass2Proxy;

public class TakoyakIX
{
    public int get_MyProperty() => 10;
    // public int MyProperty { get; set; }
}


public struct NonRefStructTest
{
    int field;
}

public ref partial struct NonRefStructTestProxy333
{
    global::NonRefStructTest target;

    public NonRefStructTestProxy333(ref global::NonRefStructTest target)
    {
        this.target = target;
    }

    //[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "field")]
    //static extern ref int __field__(ref global::NonRefStructTest target);

    //public ref int field
    //{
    //    get
    //    {
    //        return ref __field__(ref this.target);
    //    }
    //}

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Prop0")]
    static extern int __get_Prop0__(ref global::NonRefStructTest target);

    public int Prop0
    {
        get => __get_Prop0__(ref this.target);
    }


}