using System;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;

namespace OpenRiaServices.DomainServices.Client.Test
{
    [TestClass]
    public class DomainIdentifierTests : UnitTestBase
    {
        [TestMethod]
        [Description("DomainIdentifier ctor takes valid name")]
        public void DomainIdentifier_Ctor()
        {
            DomainIdentifierAttribute attr = new DomainIdentifierAttribute("aName");
            Assert.AreEqual("aName", attr.Name);
#if SERVERFX
            Assert.IsNull(attr.CodeProcessor);
#endif
        }

        [TestMethod]
        [Description("DomainIdentifier Name property is settable internally")]
        public void DomainIdentifier_Name_Property_Settable()
        {
            DomainIdentifierAttribute attr = new DomainIdentifierAttribute("aName");
            Assert.AreEqual("aName", attr.Name);

            attr.Name = "otherName";
            Assert.AreEqual("otherName", attr.Name);
        }

#if SERVERFX
        [TestMethod]
        [Description("DomainIdentifier CodeProcessor property is settable.")]
        public void DomainIdentifier_CodeProcessor_Property_Settable()
        {
            Type type1 = typeof(string);
            Type type2 = typeof(int);

            DomainIdentifierAttribute attr = new DomainIdentifierAttribute("aName");
            attr.CodeProcessor = type1;
            Assert.AreEqual<Type>(type1, attr.CodeProcessor);

            attr.CodeProcessor = type2;
            Assert.AreEqual<Type>(type2, attr.CodeProcessor);
        }
#endif
    }
}