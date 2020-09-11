using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Client.Test
{
    [TestClass]
    public class DomainIdentifierAttributeTests
    {
        [TestMethod]
        [Description("Check that attribute properties can be read and written.")]
        public void PropertyGetAndSet()
        {
            DomainIdentifierAttribute dia = null;

            // Name
            dia = new DomainIdentifierAttribute(null);
            ExceptionHelper.ExpectException<InvalidOperationException>(
                () => Assert.IsNull(dia.Name, "This should fail."));

            string name = "name";
            dia = new DomainIdentifierAttribute(name);

            Assert.AreEqual(name, dia.Name,
                "Names should be equal.");

            // IsApplicationService
            Assert.IsFalse(dia.IsApplicationService,
                "IsApplicationService state should be false by default.");

            bool isApplicationService = true;
            dia.IsApplicationService = isApplicationService;

            Assert.AreEqual(isApplicationService, dia.IsApplicationService,
                "IsApplicationService states should be equal.");
        }
    }
}
