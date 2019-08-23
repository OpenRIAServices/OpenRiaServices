using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.DomainServices.Tools.Test
{
    /// <summary>
    /// Summary description for NamingTests
    /// </summary>
    [TestClass]
    public class NamingTests
    {
        // Singular and expected plural forms for plural test below
        private static string[] _plurals = 
        {
            "Product", "Products",
            "Entity", "Entities",
            "Unix", "Unixes",
            "Munch", "Munches",
            "Progress", "Progresses",
            "Marsh", "Marshes",
            "Funny", "Funnies",
            "Money", "Moneys",
            "Today", "Todays",
            "Toy", "Toys",
            "Products", "Products"      // already plural
        };

        public NamingTests()
        {
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        [TestMethod]
        [Description("Tests all edge cases of Naming.MakePluralName")]
        public void TestPlurals()
        {
            for (int i = 0; i < _plurals.Length; i += 2)
            {
                string singular = _plurals[i];
                string expectedPlural = _plurals[i+1];
                string generatedPlural = Naming.MakePluralName(singular);
                Assert.AreEqual(expectedPlural, generatedPlural);
            }
        }
    }
}
