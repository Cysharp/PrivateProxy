using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrivateProxy.Tests;



// TODO: test inheritance
// TODO: check explicit interface member
// TODO: inheritance class uses new?
// TODO: target record, record struct

public class BaseType
{
    int _baseInt;
}

public class InheritType : BaseType
{
    int _inheritInt;
}

[GeneratePrivateProxy(typeof(InheritType))]
public partial class InheritProxy;

public interface TestInterface
{
    int Foo { get; set; }
}

public class ExplitImpl : TestInterface
{
    int a;
    int TestInterface.Foo { get => a; set => a = value; }
}

[GeneratePrivateProxy(typeof(ExplitImpl))]
public partial class ExplitImplProxy;

public record TestRecord(int x, int y)
{
    int z;
}

public record struct TestRecordStruct(int x, int y)
{
    int z;
}

[GeneratePrivateProxy(typeof(TestRecord))]
public partial class TestRecordProxy;

[GeneratePrivateProxy(typeof(TestRecordStruct))]
public ref partial struct TestRecordStructPrxy;

public class OtherTypesTest
{
    public void Foo()
    {
        // new InheritType().AsPrivateProxy()._base
        // new ExplitImpl().AsPrivateProxy().Foo

        //new TestRecord().AsPrivateProxy().z
        // new TestRecordStruct(1, 2).AsPrivateProxy().z;
    }
}
