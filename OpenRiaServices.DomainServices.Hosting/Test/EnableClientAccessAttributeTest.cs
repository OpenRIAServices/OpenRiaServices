using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.DomainServices.Hosting.UnitTests
{
    /// <summary>
    /// Tests <see cref="EnableClientAccessAttribute"/> members.
    /// </summary>
    [TestClass]
    public class EnableClientAccessAttributeTest
    {
        /// <summary>
        /// Tests that the attribute can be correctly placed on a class.
        /// </summary>
        [EnableClientAccess]
        private class TestAttributeUsage { }

        [TestMethod]
        [Description("Tests that the RequiresSecureEndpoint property can be accessed and mutated.")]
        public void RequiresSecureEndpoint()
        {
            EnableClientAccessAttribute attribute = new EnableClientAccessAttribute();

            Assert.IsFalse(attribute.RequiresSecureEndpoint,
                "Default usage should not require a secure endpoint.");

            bool requiresSecureEndpoint = true;
            attribute.RequiresSecureEndpoint = requiresSecureEndpoint;

            Assert.AreEqual(requiresSecureEndpoint, attribute.RequiresSecureEndpoint,
                "States should be equal.");
        }
    }
}
