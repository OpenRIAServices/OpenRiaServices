extern alias SSmDsClient;

using System.Collections.Generic;
using System.ServiceModel.DomainServices;
using System.ServiceModel.DomainServices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.ServiceModel.DomainServices.Client.Test
{
    using TypeUtility = SSmDsClient::System.ServiceModel.DomainServices.TypeUtility;

    [TestClass]
    public class TypeUtilityTests
    {
        private static Type[] _primitiveTypes = {
            typeof(Boolean),
            typeof(Char),
            typeof(SByte),
            typeof(Byte),
            typeof(Int16),
            typeof(UInt16),
            typeof(Int32),
            typeof(UInt32),
            typeof(Int64),
            typeof(UInt64),
            typeof(Single),
            typeof(Double)
        };

        private static Type[] _simpleTypes = {
            typeof(String),
            typeof(Decimal),
            typeof(DateTime),
            typeof(TimeSpan),
            typeof(Guid)
        };

        [TestMethod]
        [Description("Test whether TypeUtility.IsPredefinedType recognizes all our known types")]
        public void IsPredefinedTypeTests()
        {
            foreach (Type t in _primitiveTypes)
                Assert.IsTrue(TypeUtility.IsPredefinedType(t));

            foreach (Type t in _simpleTypes)
                Assert.IsTrue(TypeUtility.IsPredefinedType(t));
        }

        [TestMethod]
        public void VerifyUnsupportedTypes()
        {
            Assert.IsFalse(TypeUtility.IsPredefinedType(typeof(IEnumerable<IEnumerable<IEnumerable<string>>>)));
            Assert.IsFalse(TypeUtility.IsPredefinedType(typeof(Dictionary<string, object>)));
        }

        [TestMethod]
        [Description("Test TypeUtility.GetElementType against all known types and collections")]
        public void ElementTypeTests()
        {
            this.ValidateElementType<Boolean>();
            this.ValidateElementType<Char>();
            this.ValidateElementType<SByte>();
            this.ValidateElementType<Byte>();
            this.ValidateElementType<Int16>();
            this.ValidateElementType<UInt16>();
            this.ValidateElementType<Int32>();
            this.ValidateElementType<UInt32>();
            this.ValidateElementType<Int64>();
            this.ValidateElementType<UInt64>();
            this.ValidateElementType<Single>();
            this.ValidateElementType<Double>();
            this.ValidateElementType<String>();
            this.ValidateElementType<Decimal>();
            this.ValidateElementType<DateTime>();
            this.ValidateElementType<TimeSpan>();
            this.ValidateElementType<Guid>();

            // Static classes -- no can do
            //this.ValidateElementType<Math>();
            //this.ValidateElementType<Convert>();

            this.ValidateElementType<MyClass>();
            this.ValidateElementType<MyGenericClass<string>>();

            Assert.AreEqual(typeof(string), TypeUtility.GetElementType(typeof(MyEnumerable<string>)));
        }

        // This is just a helper method to allow us to enumerate all known types
        // and validate we can extract the element type
        private void ValidateElementType<T>()
        {
            T x = default(T);
            T[] xArray = new T[0];
            IEnumerable<T> ie = new List<T>();
            MyGenericClass<T> genericGeneric = new MyGenericClass<T>();

            if ((object) x != null) // ref types would yield null ref on GetType
                Assert.AreEqual(typeof(T), TypeUtility.GetElementType(x.GetType()));
            Assert.AreEqual(typeof(T), TypeUtility.GetElementType(xArray.GetType()));
            Assert.AreEqual(typeof(T), TypeUtility.GetElementType(ie.GetType()));

            // Validate GetElementType does NOT go recursive
            Assert.AreEqual(typeof(MyGenericClass<T>), TypeUtility.GetElementType(genericGeneric.GetType()));

        }
    }

    class MyClass
    {
    }
    class MyGenericClass<T>
    {

    }

    class MyEnumerable<T> : List<T>
    {

    }

}
