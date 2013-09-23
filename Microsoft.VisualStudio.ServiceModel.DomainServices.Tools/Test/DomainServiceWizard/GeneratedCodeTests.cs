// #define UPDATE_BASELINES    // uncomment to update baselines in bulk
using System.Linq;
using OpenRiaServices.DomainServices.Client.Test;
using Microsoft.VisualStudio.ServiceModel.DomainServices.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.AppFx.UnitTests.Setup.Wizards
{
    [TestClass]
    public class GeneratedCodeTests
    {
        [TestMethod]
        [Description("Verifies GeneratedCode ctor")]
        public void GeneratedCode_ctor()
        {
            GeneratedCode gc = new GeneratedCode("", new string[0]);
            Assert.AreEqual(string.Empty, gc.SourceCode);
            Assert.IsNotNull(gc.References);
            Assert.AreEqual(0, gc.References.Count());

            gc = new GeneratedCode("foo", new string[] {"bar"});
            Assert.AreEqual("foo", gc.SourceCode);
            Assert.IsNotNull(gc.References);
            Assert.AreEqual(1, gc.References.Count());
            Assert.AreEqual("bar", gc.References.First());
        }

        [TestMethod]
        [Description("Verifies GeneratedCode ctor throws for illegal arguments")]
        public void GeneratedCode_ctor_Illegal()
        {
            GeneratedCode gc;
            ExceptionHelper.ExpectArgumentNullExceptionStandard(delegate
            {
                gc = new GeneratedCode(null, new string[0]);
            }, "sourceCode");

            ExceptionHelper.ExpectArgumentNullExceptionStandard(delegate
            {
                gc = new GeneratedCode("foo", null);
            }, "references");

        }
    }
}
