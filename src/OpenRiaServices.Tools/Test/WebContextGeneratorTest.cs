using System;
using System.Linq;
using OpenRiaServices.DomainServices.Server.Test.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RootNamespace.TestNamespace;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.DomainServices.Tools.Test
{
    /// <summary>
    /// Tests <see cref="WebContextGenerator"/> members.
    /// </summary>
    [TestClass]
    public class WebContextGeneratorTest
    {
        [DeploymentItem(@"Baselines\Default\WebContext", @"CG_WebContext")]
        [DeploymentItem(@"Baselines\Default\WebContext\WebContext0.g.cs")]
        [DeploymentItem(@"Baselines\Default\WebContext\WebContext0.g.vb")]
        [DeploymentItem(@"ProjectPath.txt", "CG_WebContext")]
        [TestMethod]
        [Description("Tests that the code is generated correctly when no services are present.")]
        public void NoAuthenticationServices()
        {
            // Default
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"Default\WebContext", "CG_WebContext", @"WebContext0.g", Type.EmptyTypes, null, Array.Empty<string>(), "RootNamespace", new ConsoleLogger(), true, false));
        }

        [DeploymentItem(@"Baselines\FullTypeNames\WebContext", @"CG_WebContext_FullTypes")]
        [DeploymentItem(@"ProjectPath.txt", "CG_WebContext_FullTypes")]
        [TestMethod]
        [Description("Tests that the code is generated correctly when no services are present.")]
        public void NoAuthenticationServices_FullTypes()
        {
            // Full type names
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"FullTypeNames\WebContext", "CG_WebContext_FullTypes", @"WebContext0.g", Type.EmptyTypes, null, Array.Empty<string>(), "RootNamespace", new ConsoleLogger(), true, true));
        }

        [DeploymentItem(@"Baselines\FullTypeNames\WebContext", @"CG_WebContext_FullTypes")]
        [DeploymentItem(@"ProjectPath.txt", "CG_WebContext_FullTypes")]
        [TestMethod]
        [Description("Tests that the code is generated correctly when no services are present.")]
        public void NoAuthenticationServices_FullTypes_NoRootNamespace()
        {
            // Full type names
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"FullTypeNames\WebContext", "CG_WebContext_FullTypes", @"WebContext3.g", Type.EmptyTypes, "VB", Array.Empty<string>(), string.Empty, new ConsoleLogger(), true, true));
        }

        [DeploymentItem(@"Baselines\Default\WebContext", @"CG_WebContext")]
        [DeploymentItem(@"ProjectPath.txt", "CG_WebContext")]
        [TestMethod]
        [Description("Tests that the code is generated correctly when a single service is present.")]
        public void OneAuthenticationService()
        {
            // Default
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"Default\WebContext", "CG_WebContext", @"WebContext1.g", new Type[] { typeof(AuthenticationService1) }, null, Array.Empty<string>(), "RootNamespace", new ConsoleLogger(), true, false));
        }

        [DeploymentItem(@"Baselines\FullTypeNames\WebContext", @"CG_WebContext_FullTypes")]
        [DeploymentItem(@"ProjectPath.txt", "CG_WebContext_FullTypes")]
        [TestMethod]
        [Description("Tests that the code is generated correctly when a single service is present.")]
        public void OneAuthenticationService_FullTypes()
        {
            // Full type names
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"FullTypeNames\WebContext", "CG_WebContext_FullTypes", @"WebContext1.g", new Type[] { typeof(AuthenticationService1) }, null, Array.Empty<string>(), "RootNamespace", new ConsoleLogger(), true, true));
        }

        [DeploymentItem(@"Baselines\Default\WebContext", @"CG_WebContext")]
        [DeploymentItem(@"ProjectPath.txt", "CG_WebContext")]
        [TestMethod]
        [Description("Tests that the code is generated correctly when two services are present.")]
        public void TwoAuthenticationServices()
        {
            ConsoleLogger logger = new ConsoleLogger();
            Type[] types = new Type[] { typeof(AuthenticationService1), typeof(AuthenticationService2) };

            // Default
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"Default\WebContext", "CG_WebContext", @"WebContext2.g", types, null, Array.Empty<string>(), "RootNamespace", logger, true, false));
            Assert.IsTrue(logger.InfoMessages.Any(
                s => s.Contains(Resource.WebContext_ManyAuthServices.Substring(0, 20))),
                "There should be a message when multiple authentication services are detected.");
        }

        [DeploymentItem(@"Baselines\FullTypeNames\WebContext", @"CG_WebContext_FullTypes")]
        [DeploymentItem(@"ProjectPath.txt", "CG_WebContext_FullTypes")]
        [TestMethod]
        [Description("Tests that the code is generated correctly when two services are present.")]
        public void TwoAuthenticationServices_FullTypes()
        {
            ConsoleLogger logger = new ConsoleLogger();
            Type[] types = new Type[] { typeof(AuthenticationService1), typeof(AuthenticationService2) };

            // Full type names
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"FullTypeNames\WebContext", "CG_WebContext_FullTypes", @"WebContext2.g", types, null, Array.Empty<string>(), "RootNamespace", logger, true, true));
            Assert.IsTrue(logger.InfoMessages.Any(
                s => s.Contains(Resource.WebContext_ManyAuthServices.Substring(0, 20))),
                "There should be a message when multiple authentication services are detected.");
        }

        [TestMethod]
        [Description("Tests that the code is generated correctly when two services are present.")]
        public void EmptyNamespace()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(AuthenticationService1));
            TestHelper.AssertGeneratedCodeDoesNotContain(generatedCode, "WebContext");
        }
    }
}
