using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.DomainServices.Client.ApplicationServices.UnitTests
{
    /// <summary>
    /// Tests <see cref="LoginParameters"/> members.
    /// </summary>
    [TestClass]
    public class LoginParametersTest : UnitTestBase
    {
        [TestMethod]
        [Description("Tests the LoginParameters constructor and properties")]
        public void Parameters()
        {
            string userName = "username";
            string password = "password";
            bool isPersistent = true;
            string customData = "customData";

            LoginParameters parameters = new LoginParameters(userName, password, isPersistent, customData);

            Assert.AreEqual(userName, parameters.UserName,
                "UserNames should be equal.");
            Assert.AreEqual(password, parameters.Password,
                "Passwords should be equal.");
            Assert.AreEqual(isPersistent, parameters.IsPersistent,
                "IsPersistent states should be equal.");
            Assert.AreEqual(customData, parameters.CustomData,
                "CustomData should be equal.");
        }

        [TestMethod]
        [Description("Tests the LoginParameters constructor and properties with null")]
        public void NullParameters()
        {
            LoginParameters parameters = new LoginParameters();

            Assert.IsNull(parameters.UserName,
                "UserName should be null.");
            Assert.IsNull(parameters.Password,
                "Password should be null.");
        }
    }
}
