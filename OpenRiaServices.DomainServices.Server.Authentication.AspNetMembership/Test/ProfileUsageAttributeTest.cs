using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.DomainServices.Server.Authentication.AspNetMembership.Test
{
    /// <summary>
    /// Tests <see cref="ProfileUsageAttribute"/> members.
    /// </summary>
    [TestClass]
    public class ProfileUsageAttributeTest
    {
        /// <summary>
        /// Tests that the attribute can be correctly placed on a property.
        /// </summary>
        private class TestAttributeUsage
        {
            [ProfileUsage]
            public object Property { get; set; }
        }

        [TestMethod]
        [Description("Tests that the properties can be accessed and mutated.")]
        public void Properties()
        {
            string alias = "alias";
            bool isExcluded = true;

            var attribute = new ProfileUsageAttribute();

            Assert.IsTrue(string.IsNullOrEmpty(attribute.Alias),
                "Aliases should be null or empty by default.");
            Assert.IsFalse(attribute.IsExcluded,
                "Excluded state should be false by default.");

            attribute.Alias = alias;
            attribute.IsExcluded = isExcluded;

            Assert.AreEqual(alias, attribute.Alias,
                "Aliases should be equal.");
            Assert.AreEqual(isExcluded, attribute.IsExcluded,
                "Excluded states should be equal.");
        }
    }
}
