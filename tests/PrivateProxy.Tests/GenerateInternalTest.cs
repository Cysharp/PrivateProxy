using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrivateProxy.Tests;


public class InternalTarget
{
    InnerClass innerField = new();

    private void Hoge()
    {
    }

    private InnerClass GetInnerClass() => new InnerClass();

    private InternalClass GetInternalClass => new();
    private InternalClass[] GetInternalArray() => [];
    private InternalClass[][] GetInternalNestedArray() => [];
    private Dictionary<int, InternalClass> GetInternalGeneric => [];
    
    private NotApplicableClass GetNotApplicable() => new();
    private NotApplicableClass[] GetNotApplicableArray() => [];
    private NotApplicableClass[][] GetNotApplicableNestedArray() => [];
    private Dictionary<int, NotApplicableClass> GetNotApplicableGeneric => [];

    class InnerClass;
}

internal class InternalClass;
class NotApplicableClass;

[GeneratePrivateProxy(typeof(InternalTarget))]
public partial struct InnerTargetProxy;

public class GenerateInternalTest

{
}
