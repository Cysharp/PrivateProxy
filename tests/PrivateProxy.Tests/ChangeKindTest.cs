using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PrivateProxy.Tests
{
    public class KindTarget
    {
        private int field;
        private int Property { get; set; }
        int Method() => 3;

        static private int staticField;
        static private int StaticProperty { get; set; }
        static private int StaticMethod() => 3;
    }

    [GeneratePrivateProxy(typeof(KindTarget), PrivateProxyGenerateKinds.Property | PrivateProxyGenerateKinds.Static)]
    public partial struct KindTargetProxy;


    public class ChangeKindTest
    {
        string GenerateCode(PrivateProxyGenerateKinds generateKinds)
        {
            var kind = string.Join(" | ", generateKinds.ToString().Split(',').Select(x => "PrivateProxyGenerateKinds." + x.Trim()));

            var codes = CSharpGeneratorRunner.RunGeneratorCode($$"""
using PrivateProxy;

public class KindTarget
{
    private int field;
    private int Property { get; set; }
    int Method() => 3;

    static private int staticField;
    static private int StaticProperty { get; set; }
    static private int StaticMethod() => 3;
}

[GeneratePrivateProxy(typeof(KindTarget), {{kind}})]
public partial struct KindTargetProxy { }
""");

            var code = codes.First(x => x.FilePath.Contains("g.cs")).Code;
            return code;
        }

        [Fact]
        public void All()
        {
            var all = GenerateCode(PrivateProxyGenerateKinds.All);
            Regex.Count(all, @"\[UnsafeAccessor.+\]").Should().Be(8);
        }

        [Fact]
        public void StaticOnly()
        {
            var all = GenerateCode(PrivateProxyGenerateKinds.Static);
            Regex.Count(all, @"\[UnsafeAccessor.+\]").Should().Be(4);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.Static.+\]").Should().Be(4);
        }

        [Fact]
        public void InstanceOnly()
        {
            var all = GenerateCode(PrivateProxyGenerateKinds.Instance);
            Regex.Count(all, @"\[UnsafeAccessor.+\]").Should().Be(4);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.(?!Static).+\]").Should().Be(4);
        }

        [Fact]
        public void MethodOnly()
        {
            var all = GenerateCode(PrivateProxyGenerateKinds.Method);
            Regex.Count(all, @"\[UnsafeAccessor.+\]").Should().Be(2);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.Static.+\]").Should().Be(1);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.(?!Static).+\]").Should().Be(1);

            all = GenerateCode(PrivateProxyGenerateKinds.Static | PrivateProxyGenerateKinds.Method);
            Regex.Count(all, @"\[UnsafeAccessor.+\]").Should().Be(1);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.Static.+\]").Should().Be(1);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.(?!Static).+\]").Should().Be(0);

            all = GenerateCode(PrivateProxyGenerateKinds.Instance | PrivateProxyGenerateKinds.Method);
            Regex.Count(all, @"\[UnsafeAccessor.+\]").Should().Be(1);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.Static.+\]").Should().Be(0);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.(?!Static).+\]").Should().Be(1);
        }

        [Fact]
        public void PropertyOnly()
        {
            var all = GenerateCode(PrivateProxyGenerateKinds.Property);
            Regex.Count(all, @"\[UnsafeAccessor.+\]").Should().Be(4);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.Static.+\]").Should().Be(2);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.(?!Static).+\]").Should().Be(2);

            all = GenerateCode(PrivateProxyGenerateKinds.Static | PrivateProxyGenerateKinds.Property);
            Regex.Count(all, @"\[UnsafeAccessor.+\]").Should().Be(2);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.Static.+\]").Should().Be(2);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.(?!Static).+\]").Should().Be(0);

            all = GenerateCode(PrivateProxyGenerateKinds.Instance | PrivateProxyGenerateKinds.Property);
            Regex.Count(all, @"\[UnsafeAccessor.+\]").Should().Be(2);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.Static.+\]").Should().Be(0);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.(?!Static).+\]").Should().Be(2);
        }

        [Fact]
        public void FieldOnly()
        {
            var all = GenerateCode(PrivateProxyGenerateKinds.Field);
            Regex.Count(all, @"\[UnsafeAccessor.+\]").Should().Be(2);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.Static.+\]").Should().Be(1);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.(?!Static).+\]").Should().Be(1);

            all = GenerateCode(PrivateProxyGenerateKinds.Static | PrivateProxyGenerateKinds.Field);
            Regex.Count(all, @"\[UnsafeAccessor.+\]").Should().Be(1);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.Static.+\]").Should().Be(1);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.(?!Static).+\]").Should().Be(0);

            all = GenerateCode(PrivateProxyGenerateKinds.Instance | PrivateProxyGenerateKinds.Field);
            Regex.Count(all, @"\[UnsafeAccessor.+\]").Should().Be(1);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.Static.+\]").Should().Be(0);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.(?!Static).+\]").Should().Be(1);
        }

        [Fact]
        public void Mix()
        {
            var all = GenerateCode(PrivateProxyGenerateKinds.Field | PrivateProxyGenerateKinds.Method);
            Regex.Count(all, @"\[UnsafeAccessor.+\]").Should().Be(4);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.Static.+\]").Should().Be(2);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.(?!Static).+\]").Should().Be(2);

            all = GenerateCode(PrivateProxyGenerateKinds.Static| PrivateProxyGenerateKinds.Instance | PrivateProxyGenerateKinds.Field | PrivateProxyGenerateKinds.Method);
            Regex.Count(all, @"\[UnsafeAccessor.+\]").Should().Be(4);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.Static.+\]").Should().Be(2);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.(?!Static).+\]").Should().Be(2);

            all = GenerateCode(PrivateProxyGenerateKinds.Static | PrivateProxyGenerateKinds.Field | PrivateProxyGenerateKinds.Method);
            Regex.Count(all, @"\[UnsafeAccessor.+\]").Should().Be(2);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.Static.+\]").Should().Be(2);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.(?!Static).+\]").Should().Be(0);

            all = GenerateCode(PrivateProxyGenerateKinds.Instance | PrivateProxyGenerateKinds.Field | PrivateProxyGenerateKinds.Method);
            Regex.Count(all, @"\[UnsafeAccessor.+\]").Should().Be(2);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.Static.+\]").Should().Be(0);
            Regex.Count(all, @"\[UnsafeAccessor\(UnsafeAccessorKind.(?!Static).+\]").Should().Be(2);
        }
    }
}
