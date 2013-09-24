using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.DomainServices.Tools.Test
{
    /// <summary>
    /// Tests for the <see cref="ValidateDomainServicesTask"/>
    /// </summary>
    [TestClass]
    public class ValidateDomainServicesTaskTests
    {
        [DeploymentItem(@"OpenRiaServices.DomainServices.Tools\Test\ProjectPath.txt", "VDST1")]
        [Description("ValidateDomainServicesTask runs succesfully for a well-formed DomainService")]
        [TestMethod]
        public void ValidateDomainServicesTaskRunsSuccessfully()
        {
            ValidateDomainServicesTask task = CodeGenHelper.CreateValidateDomainServicesTask("VDST1");
            Assert.IsTrue(task.Execute(),
                "Validation should have completed without error");
        }
    }
}
