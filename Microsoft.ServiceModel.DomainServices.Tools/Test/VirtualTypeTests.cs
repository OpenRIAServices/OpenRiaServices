using System;
using OpenRiaServices.DomainServices.Tools.SharedTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.DomainServices.Tools.Test
{
    /// <summary>
    /// Tests for VirtualType
    /// </summary>
    [TestClass]
    public class VirtualTypeTests
    {
        public VirtualTypeTests()
        {
        }

        [Description("VirtualType constructors work and set known property values")]
        [TestMethod]
        public void VirtualType_ctors()
        {
            VirtualType vt = new VirtualType("name", "namespace", this.GetType().Assembly, this.GetType());
            Assert.AreEqual("name", vt.Name);
            Assert.AreEqual("namespace", vt.Namespace);
            Assert.AreEqual(this.GetType().Assembly, vt.Assembly);
            Assert.AreEqual(this.GetType(), vt.BaseType);

            vt = new VirtualType(this.GetType());
            this.AssertEquivalentTypes(this.GetType(), vt);
        }

        [Description("VirtualType constructs equivalent base type hierarchy")]
        [TestMethod]
        public void VirtualType_BaseTypes()
        {
            VirtualType vt = new VirtualType(typeof(VT_C));
            this.AssertEquivalentTypes(typeof(VT_C), vt);
        }

        private void AssertEquivalentTypes(Type actualType, Type virtualType)
        {
            if (actualType == null)
            {
                Assert.IsNull(virtualType);
                return;
            }
            else
            {
                Assert.IsNotNull(virtualType, "expected virtual type equivalent of " + actualType.Name);
            }
            Assert.AreEqual(actualType.Name, virtualType.Name);
            Assert.AreEqual(actualType.Namespace, virtualType.Namespace);
            Assert.AreEqual(actualType.Assembly.FullName, virtualType.Assembly.FullName);

            this.AssertEquivalentTypes(actualType.BaseType, virtualType.BaseType);
        }
    }

    public class VT_A { }
    public class VT_B : VT_A { }
    public class VT_C : VT_B { }
}


