using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;

namespace OpenRiaServices.Client.Test
{
    [TestClass]
    public class DataAnnotationsTests : UnitTestBase
    {
        [TestMethod]
        public void TestDefaultDataAnnotationAttributeCtors()
        {
            Type[] _knownAttributeTypes = {
                typeof(KeyAttribute),
                typeof(ConcurrencyCheckAttribute),
                typeof(TimestampAttribute)
            };

            foreach (Type t in _knownAttributeTypes)
            {
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
    }
}
