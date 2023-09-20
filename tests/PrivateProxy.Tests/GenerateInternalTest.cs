using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrivateProxy.Tests;


public class InternalTarget
{
    InnerClass innerField = new();

    private InnerClass GetInnerClass() => new InnerClass();
    


    class InnerClass
    {
    }
}


[GeneratePrivateProxy(typeof(InternalTarget))]
public partial struct InnerTargetProxy;


public class GenerateInternalTest
{
}
