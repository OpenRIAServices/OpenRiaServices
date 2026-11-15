using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Tools.Test
{
    /// <summary>
    /// Tests for the <see cref="ValidateDomainServicesTask"/>
    /// </summary>
    [TestClass]
    public class ValidateDomainServicesTaskTests
    {
        [Description("ValidateDomainServicesTask runs succesfully for a well-formed DomainService")]
        [TestMethod]
        [Ignore("Server validation is not yet implemented for .NET")]
        public void ValidateDomainServicesTaskRunsSuccessfully()
        {
            ValidateDomainServicesTask task = CodeGenHelper.CreateValidateDomainServicesTask("");
            Assert.IsTrue(task.Execute(),
                "Validation should have completed without error");
        }
    }
}
