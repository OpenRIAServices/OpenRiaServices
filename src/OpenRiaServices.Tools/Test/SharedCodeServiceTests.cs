using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Globalization;
using System.Reflection;
using OpenRiaServices.Server;
using OpenRiaServices.Server.Test.Utilities;
using OpenRiaServices.Tools.SharedTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServerClassLib;

namespace OpenRiaServices.Tools.Test
{
    /// <summary>
    /// Tests for SharedAssemblies service
    /// </summary>
    [TestClass]
    public class SharedCodeServiceTests
    {
        public SharedCodeServiceTests()
        {
        }

        [Description("SharedCodeServiceParameter properties can be set")]
        [TestMethod]
        public void SharedCodeServiceParameter_Properties()
        {
            SharedCodeServiceParameters parameters = new SharedCodeServiceParameters()
            {
                ClientAssemblies = new string[] { "clientAssembly" },
                ServerAssemblies = new string[] { "serverAssembly" },
                ClientAssemblyPathsNormalized = new string[] { "clientPaths" },
                SharedSourceFiles = new string[] { "sharedSourceFiles" },
                SymbolSearchPaths = new string[] { "symSearch" }
            };

            Assert.AreEqual("clientAssembly", parameters.ClientAssemblies.First());
            Assert.AreEqual("serverAssembly", parameters.ServerAssemblies.First());
            Assert.AreEqual("clientPaths", parameters.ClientAssemblyPathsNormalized.First());
            Assert.AreEqual("sharedSourceFiles", parameters.SharedSourceFiles.First());
            Assert.AreEqual("symSearch", parameters.SymbolSearchPaths.First());
        }


        [Description("SharedCodeService ctors can be called")]
        [TestMethod]
        public void SharedCodeService_Ctor()
        {
            SharedCodeServiceParameters parameters = new SharedCodeServiceParameters()
            {
                ClientAssemblies = new string[] { "clientAssembly" },
                ServerAssemblies = new string[] { "serverAssembly" },
                ClientAssemblyPathsNormalized = new string[] { "clientPaths" },
                SharedSourceFiles = new string[] { "sharedSourceFiles" },
                SymbolSearchPaths = new string[] { "symSearch" }
            };
            ConsoleLogger logger = new ConsoleLogger();

            using (SharedCodeService sts = new SharedCodeService(parameters, logger))
            {
            }
        }

        [Description("SharedCodeService locates shared types between projects")]
        [TestMethod]
        public void SharedCodeService_Types()
        {
            string projectPath = null;
            string outputPath = null;
            TestHelper.GetProjectPaths("", out projectPath, out outputPath);
            string clientProjectPath = CodeGenHelper.ClientClassLibProjectPath(projectPath);

            ConsoleLogger logger = new ConsoleLogger();
            using (SharedCodeService sts = CodeGenHelper.CreateSharedCodeService(clientProjectPath, logger))
            {
                // TestEntity is shared because it is linked
                CodeMemberShareKind shareKind = sts.GetTypeShareKind(typeof(TestEntity).AssemblyQualifiedName);
                Assert.AreEqual(CodeMemberShareKind.SharedByReference, shareKind, "Expected TestEntity type to be shared by reference");

                // TestValidator is shared because it is linked
                shareKind = sts.GetTypeShareKind(typeof(TestValidator).AssemblyQualifiedName);
                Assert.AreEqual(CodeMemberShareKind.SharedByReference, shareKind, "Expected TestValidator type to be shared by reference");

                // SharedClass is shared because it is linked
                shareKind = sts.GetTypeShareKind(typeof(SharedClass).AssemblyQualifiedName);
                Assert.IsTrue(shareKind == CodeMemberShareKind.SharedBySource, "Expected SharedClass type to be shared in source");

                // DomainService exists only in the server and is not shared
                shareKind = sts.GetTypeShareKind(typeof(DomainService).AssemblyQualifiedName);
                Assert.IsTrue(shareKind == CodeMemberShareKind.NotShared, "Expected DomainService type not to be shared");

                // TestValidatorServer exists only on the server and is not shared
                shareKind = sts.GetTypeShareKind(typeof(TestValidatorServer).AssemblyQualifiedName);
                Assert.IsTrue(shareKind == CodeMemberShareKind.NotShared, "Expected TestValidatorServer type not to be shared");

                // CodelessType exists on both server and client, but lacks all user code necessary
                // to determine whether it is shared.  Because it compiles into both projects, it should
                // be considered shared by finding the type in both assemblies
                shareKind = sts.GetTypeShareKind(typeof(CodelessType).AssemblyQualifiedName);
                Assert.IsTrue(shareKind == CodeMemberShareKind.SharedByReference, "Expected CodelessType type to be shared in assembly");
            }
        }

        [DeploymentItem("ProjectPath.txt")]
        [Description("SharedCodeService locates shared properties between projects")]
        [TestMethod]
        public void SharedCodeService_Properties()
        {
            string projectPath = null;
            string outputPath = null;
            TestHelper.GetProjectPaths("", out projectPath, out outputPath);
            string clientProjectPath = CodeGenHelper.ClientClassLibProjectPath(projectPath);

            ConsoleLogger logger = new ConsoleLogger();
            using (SharedCodeService sts = CodeGenHelper.CreateSharedCodeService(clientProjectPath, logger))
            {
                CodeMemberShareKind shareKind = sts.GetPropertyShareKind(typeof(TestEntity).AssemblyQualifiedName, "ServerAndClientValue");
                Assert.AreEqual(CodeMemberShareKind.SharedByReference, shareKind, "Expected TestEntity.ServerAndClientValue property to be shared by reference.");

                shareKind = sts.GetPropertyShareKind(typeof(TestEntity).AssemblyQualifiedName, "TheValue");
                Assert.AreEqual(CodeMemberShareKind.NotShared, shareKind, "Expected TestEntity.TheValue property not to be shared in source.");
            }
        }

        [Description("SharedCodeService locates shared methods between projects")]
        [TestMethod]
        public void SharedCodeService_Methods()
        {
            string projectPath = null;
            string outputPath = null;
            TestHelper.GetProjectPaths("", out projectPath, out outputPath);
            string clientProjectPath = CodeGenHelper.ClientClassLibProjectPath(projectPath);

            ConsoleLogger logger = new ConsoleLogger();
            using (SharedCodeService sts = CodeGenHelper.CreateSharedCodeService(clientProjectPath, logger))
            {
                CodeMemberShareKind shareKind = sts.GetMethodShareKind(typeof(TestValidator).AssemblyQualifiedName, "IsValid", new string[] { typeof(TestEntity).AssemblyQualifiedName, typeof(ValidationContext).AssemblyQualifiedName });
                Assert.AreEqual(CodeMemberShareKind.SharedByReference, shareKind, "Expected TestValidator.IsValid to be shared by reference");

                shareKind = sts.GetMethodShareKind(typeof(TestEntity).AssemblyQualifiedName, "ServerAndClientMethod", Array.Empty<string>());
                Assert.AreEqual(CodeMemberShareKind.SharedByReference, shareKind, "Expected TestValidator.ServerAndClientMethod to be shared by reference");

                shareKind = sts.GetMethodShareKind(typeof(TestEntity).AssemblyQualifiedName, "ServerMethod", Array.Empty<string>());
                Assert.AreEqual(CodeMemberShareKind.NotShared, shareKind, "Expected TestValidator.ServerMethod not to be shared");

                shareKind = sts.GetMethodShareKind(typeof(TestValidatorServer).AssemblyQualifiedName, "IsValid", new string[] { typeof(TestEntity).AssemblyQualifiedName, typeof(ValidationContext).AssemblyQualifiedName });
                Assert.AreEqual(CodeMemberShareKind.NotShared, shareKind, "Expected TestValidator.IsValid not to be shared");

                TestHelper.AssertNoErrorsOrWarnings(logger);
            }
        }

        [Description("SharedCodeService locates shared ctors between projects")]
        [TestMethod]
        public void SharedCodeService_Ctors()
        {
            string projectPath = null;
            string outputPath = null;
            TestHelper.GetProjectPaths("", out projectPath, out outputPath);
            string clientProjectPath = CodeGenHelper.ClientClassLibProjectPath(projectPath);

            ConsoleLogger logger = new ConsoleLogger();
            using (SharedCodeService sts = CodeGenHelper.CreateSharedCodeService(clientProjectPath, logger))
            {
                ConstructorInfo ctor = typeof(TestValidator).GetConstructor(new Type[] { typeof(string) });
                Assert.IsNotNull("Failed to find string ctor on TestValidator");
                CodeMemberShareKind shareKind = sts.GetMethodShareKind(typeof(TestValidator).AssemblyQualifiedName, ctor.Name, new string[] { typeof(string).AssemblyQualifiedName });
                Assert.AreEqual(CodeMemberShareKind.SharedByReference, shareKind, "Expected TestValidator ctor to be shared by reference");
                TestHelper.AssertNoErrorsOrWarnings(logger);
            }
        }
    }
}
