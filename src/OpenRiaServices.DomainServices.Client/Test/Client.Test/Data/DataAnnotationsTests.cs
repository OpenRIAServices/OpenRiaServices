using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;

namespace OpenRiaServices.DomainServices.Client.Test
{
    [TestClass]
    public class DataAnnotationsTests : UnitTestBase
    {
        [TestMethod]
        public void TestDefaultDataAnnotationAttributeCtors()
        {
            Type[] _knownAttributeTypes = {
                typeof(KeyAttribute),
                typeof(AssociationAttribute),
                typeof(ConcurrencyCheckAttribute),
                typeof(TimestampAttribute)
            };

            foreach (Type t in _knownAttributeTypes)
            {
                if (t == typeof(AssociationAttribute)) {
                    // no default constructor defined
                    continue;
                }

                Attribute attr = null;
                string message = string.Empty;
                try
                {
                    attr = (Attribute)Activator.CreateInstance(t);
                }
                catch (Exception ex)
                {
                    message = "\r\n" + ex.Message;
                }
                Assert.IsNotNull(attr, "Default ctor failed for attribute type " + t.GetType().Name + message);
            }
        }

        [TestMethod]
        public void TestAssociationAttribute()
        {
            AssociationAttribute attr = new AssociationAttribute("name", "thisKey", "otherKey");
            attr.IsForeignKey = false;

            Assert.AreEqual("name", attr.Name);
            Assert.AreEqual("thisKey", attr.ThisKey);
            Assert.AreEqual("otherKey", attr.OtherKey);
            Assert.AreEqual(false, attr.IsForeignKey);

            // Verify can reverse polarity of foreign key
            attr.IsForeignKey = true;
            Assert.AreEqual(true, attr.IsForeignKey);

        }
    }
}
