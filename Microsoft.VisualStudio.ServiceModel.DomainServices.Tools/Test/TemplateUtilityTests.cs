using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio.ServiceModel.DomainServices.Tools.Test
{
    [TestClass]
    public class TemplateUtilityTests
    {
        [TestMethod]
        public void SilverlightToolsVersionStartsWithV()
        {
            // TODO, suwatch: discuss with Kyle and decided that mocking up serviceProvider and testing it
            // is not valuable at this time.
            //string toolsVersion = TemplateUtilities.GetSilverlightVersion(serviceProvider);
            //Assert.IsTrue(toolsVersion.StartsWith("v"));
        }

        [TestMethod]
        public void SilverlightToolsVersionIs4or5()
        {
            // TODO, suwatch: discuss with Kyle and decided that mocking up serviceProvider and testing it
            // is not valuable at this time.
            //string toolsVersion = TemplateUtilities.GetSilverlightVersion(serviceProvider);
            //decimal version = decimal.Parse(toolsVersion.Substring(1));

            //Assert.IsTrue(version == 4.0m || version == 5.0m, "The Silveright Tools version must be v4.0 or v5.0");
        }
    }
}
