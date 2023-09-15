// See https://aka.ms/new-console-template for more information
// using PrivateProxy;
using PrivateProxy;
using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;


var test1 = new PrivateClass2();
var proxy = new PrivateClass2Proxy(test1);

proxy._privateField = 9999;

proxy.PrivateMyProperty = 100000;

Console.WriteLine(proxy.PrivateMyProperty);

proxy.Show2();
proxy.ShowWithP(10, 2000);

// test1.ShowPrivateField();



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
}

[GeneratePrivateProxy(typeof(PrivateClass2))]
partial struct PrivateClass2Proxy
{
}

public class TakoyakIX
{
    public int get_MyProperty() => 10;
    // public int MyProperty { get; set; }
}


partial struct PrivateClassProxy
{
    PrivateClass target;

    public PrivateClassProxy(PrivateClass target)
    {
        this.target = target;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_privateField")]
    static extern ref int __privateField__(PrivateClass target);
    public ref int PrivateField => ref __privateField__(target);



    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "_privateField")]
    static extern ref int __prop__(PrivateClass target);


    public ref int PublicField => ref target.PublicField;
    public ref readonly int ReadOnlyPublicField => ref target.ReadOnlyPublicField;
}


