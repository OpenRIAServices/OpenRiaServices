using System.Collections.Generic;
using System.Text;
using System.Web.Ria.ApplicationServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Web.Ria.UnitTests
{
    /// <summary>
    /// Tests <see cref="SilverlightApplication"/> members.
    /// </summary>
    [TestClass]
    public class SilverlightApplicationTest
    {
        /// <summary>
        /// This class provides a public interface for the protected 
        /// <see cref="SilverlightApplication.GetSilverlightParameters"/> and
        /// <see cref="SilverlightApplication.AppendInitParameter"/> methods.
        /// </summary>
        private class MockSilverlightApplication : SilverlightApplication 
        {
            public static void AppendInitParameterMock(StringBuilder initParamsBuilder, string key, string value)
            {
                SilverlightApplication.AppendInitParameter(initParamsBuilder, key, value);
            }

            public IDictionary<string, string> GetSilverlightParametersMock()
            {
                return base.GetSilverlightParameters();
            }
        }

        [TestMethod]
        [Description("Tests accessing and mutating properties")]
        public void Properties()
        {
            SilverlightApplication control = new SilverlightApplication();

            bool enableUserState = !control.EnableUserState;

            control.EnableUserState = enableUserState;

            Assert.AreEqual(enableUserState, control.EnableUserState,
                "EnableUserState values should be the same.");
        }

        // TODO (Kyle): This issue will be resolved once MockWebSettingsDomainService stops
        // registering as the "Settings" DomainIdentifier by default. Unfortunately, once it is
        // registered the profile will be serialized in the ProfileSettingsDomainService in 
        // a way that is not compatible with the test configuration.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Silverlight", Justification = "Erroneous match.")]
        [TestMethod]
        [Ignore]
        [Description("Tests that the keys for SilverlightApplication properties are in the InitParams")]
        public void SilverlightParametersDoesContainKeys()
        {
            MockSilverlightApplication mock = new MockSilverlightApplication();

            IDictionary<string, string> silverlightParameters = mock.GetSilverlightParametersMock();

            Assert.IsTrue(silverlightParameters.ContainsKey("InitParams"),
                "SilverlightParameters should contain InitParams.");
            Assert.IsTrue(silverlightParameters["InitParams"].Contains(UserSerializer.UserKey),
                "InitParams should contain the UserKey.");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Silverlight", Justification = "Erroneous match.")]
        [TestMethod]
        [Description("Tests that the keys for SilverlightApplication properties are not in the InitParams")]
        public void SilverlightParametersDoesNotContainKeys()
        {
            MockSilverlightApplication mock = new MockSilverlightApplication();
            mock.EnableUserState = false;

            IDictionary<string, string> silverlightParameters = mock.GetSilverlightParametersMock();

            // InitParams can be empty (especially if we're not adding to it)
            if (silverlightParameters.ContainsKey("InitParams"))
            {
                Assert.IsFalse(silverlightParameters["InitParams"].Contains(UserSerializer.UserKey),
                    "InitParams should not contain the UserKey.");
            }
        }

        [TestMethod]
        [Description("Tests that AppendInitParameter throws when the builder is null")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AppendBuilderCannotBeNull()
        {
            MockSilverlightApplication.AppendInitParameterMock(null, "key", "value");
        }

        [TestMethod]
        [Description("Tests that AppendInitParameter throws when the key is null")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AppendKeyCannotBeNull()
        {
            MockSilverlightApplication.AppendInitParameterMock(new StringBuilder(), null, "value");
        }

        [TestMethod]
        [Description("Tests that AppendInitParameter throws when the key is empty")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AppendKeyCannotBeEmpty()
        {
            MockSilverlightApplication.AppendInitParameterMock(new StringBuilder(), string.Empty, "value");
        }

        [TestMethod]
        [Description("Tests that AppendInitParameter adds the specified key and value to the builder")]
        public void AppendKeyAndValueAdded()
        {
            StringBuilder builder = new StringBuilder("builder");
            string key = "key";
            string value = "value";

            MockSilverlightApplication.AppendInitParameterMock(builder, key, value);

            Assert.IsTrue(builder.ToString().Contains(key));
            Assert.IsTrue(builder.ToString().Contains(value));
        }
    }
}
