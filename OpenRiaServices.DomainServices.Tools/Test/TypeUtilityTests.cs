extern alias SystemWebDomainServices;
extern alias TextTemplate;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.DomainServices.Tools.Test
{
    [TestClass]
    public class TypeUtilityTests
    {
        [TestMethod]
        [Description("Checks that Open Ria Services assembly is identified as a system assembly.")]
        public void TestOpenRiaServicesAssemblyIsSystemAssembly()
        {
            var assemblyName = typeof (TextTemplate::OpenRiaServices.DomainServices.Tools.TextTemplate.ClientCodeGenerator).Assembly.FullName;
            bool result = SystemWebDomainServices::OpenRiaServices.DomainServices.TypeUtility.IsSystemAssembly(assemblyName);
            Assert.IsTrue(result, "The assembly " + assemblyName + " is not identified as a system assembly");
        }
    }
}