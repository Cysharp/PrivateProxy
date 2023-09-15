// See https://aka.ms/new-console-template for more information
// using PrivateProxy;
using PrivateProxy;
using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;


var test1 = new PrivateClass2();
var proxy = new MyFoo(test1);

proxy._privateField = 9999;

test1.ShowPrivateField();


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

    public void ShowPrivateField()
    {
        Console.WriteLine(_privateField);
    }
}

[GeneratePrivateProxy(typeof(PrivateClass2))]
partial struct MyFoo
{
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




    public ref int PublicField => ref target.PublicField;
    public ref readonly int ReadOnlyPublicField => ref target.ReadOnlyPublicField;
}


