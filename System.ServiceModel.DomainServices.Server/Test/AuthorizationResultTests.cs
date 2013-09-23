using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.DomainServices.Server.Test
{
    [TestClass]
    public class AuthorizationResultTests
    {
        [TestMethod]
        [Description("AuthorizationResult ctor validates input parameters and initializes properties")]
        public void AuthorizationResult_Ctor()
        {
            AuthorizationResult result = new AuthorizationResult(null);
            Assert.IsNull(result.ErrorMessage, "Null error message was not respected");

            result = new AuthorizationResult(string.Empty);
            Assert.AreEqual(string.Empty, result.ErrorMessage, "empty error message was not respected");

            result = new AuthorizationResult("hey");
            Assert.AreEqual("hey", result.ErrorMessage, "empty error message was not respected");
        }
    }
}
