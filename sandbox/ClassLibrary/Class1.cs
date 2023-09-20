#pragma warning disable CS8604
#pragma warning disable CS8321
#pragma warning disable CS0414

using System.Runtime.InteropServices.Marshalling;

namespace ClassLibrary
{
    public class MyClass
    {
        private int _privateField;
        private NestedPrivate _nesterPrivate = new NestedPrivate();
        private NestedPublic _nesterPublic = new NestedPublic();
        private InternalEnum _internalEnum = default;

        private void PrivateMethod()
        {
            _privateField++;
        }

        class NestedPrivate
        {

        }

        public class NestedPublic
        {

        }
    }

    internal enum InternalEnum
    {
    }
}
