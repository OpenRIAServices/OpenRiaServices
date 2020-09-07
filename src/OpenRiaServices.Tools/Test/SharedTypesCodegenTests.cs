using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenRiaServices.Server.Test.Utilities;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Tools.Test
{
    /// <summary>
    /// Tests for custom build task to generate client proxies
    /// </summary>
    [TestClass]
    public class SharedTypesCodegenTests
    {
        public SharedTypesCodegenTests()
        {
        }

        [Description("CreateOpenRiaClientFilesTask does not codegen shared types or properties on entities and complex types (OpenRiaSharedFilesMode = Copy)")]
        [TestMethod]
        public void SharedTypes_CodeGen_Skips_Shared_Types_And_Properties_Copy()
        {
            SharedTypes_CodeGen_Skips_Shared_Types_And_Properties(OpenRiaSharedFilesMode.Copy);
        }

        [Description("CreateOpenRiaClientFilesTask does not codegen shared types or properties on entities and complex types (OpenRiaSharedFilesMode = Link)")]
        [TestMethod]
        public void SharedTypes_CodeGen_Skips_Shared_Types_And_Properties_Link()
        {
            SharedTypes_CodeGen_Skips_Shared_Types_And_Properties(OpenRiaSharedFilesMode.Link);
        }

        private static void SharedTypes_CodeGen_Skips_Shared_Types_And_Properties(OpenRiaSharedFilesMode sharedFilesMode)
        {
            string expectedGeneratedCodeFile = "ServerClassLib.g.cs";
            string[] expectedSharedFiles = new string[] {
                "TestEntity.shared.cs",         // via server project
                "TestComplexType.shared.cs",    // via server project
                "ServerClassLib2.shared.cs"     // via P2P
            };

            CreateOpenRiaClientFilesTask task = null;
            try
            {
                task = CodeGenHelper.CreateOpenRiaClientFilesTaskInstance(string.Empty, /*includeClientOutputAssembly*/ false);
                MockBuildEngine mockBuildEngine = task.BuildEngine as MockBuildEngine;

                // Work Item 199139:
                // We're stripping ServerClassLib2 from the reference assemblies since we cannot depend on Visual Studio
                // to reliably produce a full set of dependencies. This will force the assembly resolution code to
                // search for ServerClassLib2 during codegen.
                // Note: Our assembly resolution code is only exercised when running against an installed product. When
                // we're running locally, resolution occurs without error.
                task.ServerReferenceAssemblies = task.ServerReferenceAssemblies.Where(item => !item.ItemSpec.Contains("ServerClassLib2")).ToArray();
                task.SharedFilesMode = sharedFilesMode.ToString();
                bool success = task.Execute();
                if (!success)
                {
                    Assert.Fail("CreateOpenRiaClientFilesTask failed:\r\n" + mockBuildEngine.ConsoleLogger.Errors);
                }

                ITaskItem[] outputFiles = task.OutputFiles.ToArray();

                if (sharedFilesMode == OpenRiaSharedFilesMode.Copy)
                {
                    Assert.AreEqual(1 + expectedSharedFiles.Length, outputFiles.Length);
                }
                else
                {
                    Assert.AreEqual(1, outputFiles.Length);
                }

                string generatedFile = CodeGenHelper.GetOutputFile(outputFiles, expectedGeneratedCodeFile);
                CodeGenHelper.AssertOutputContainsFiles(task.SharedFiles, expectedSharedFiles);

                string generatedCode = string.Empty;
                using (StreamReader t1 = new StreamReader(generatedFile))
                {
                    generatedCode = t1.ReadToEnd();
                }

                ConsoleLogger logger = new ConsoleLogger();
                logger.LogMessage(generatedCode);
                CodeGenHelper.AssertGenerated(generatedCode, "public sealed partial class TestEntity : Entity");
                CodeGenHelper.AssertGenerated(generatedCode, "public string TheKey");
                CodeGenHelper.AssertGenerated(generatedCode, "public int TheValue");

                CodeGenHelper.AssertGenerated(generatedCode, "public sealed partial class TestComplexType : ComplexObject");
                CodeGenHelper.AssertGenerated(generatedCode, "public int TheComplexTypeValueProperty");

                // This property is in shared code (via link) and should not have been generated
                CodeGenHelper.AssertNotGenerated(generatedCode, "public int ServerAndClientValue");

                // The automatic property in shared code should not have been generated
                CodeGenHelper.AssertNotGenerated(generatedCode, "public string AutomaticProperty");

                // The server-only IsValid method should have emitted a comment warning it is not shared
                CodeGenHelper.AssertGenerated(generatedCode, "// [CustomValidationAttribute(typeof(ServerClassLib.TestValidatorServer), \"IsValid\")]");

                // The TestDomainSharedService already had a matching TestDomainSharedContext DomainContext
                // pre-built into the client project.  Verify we did *NOT* regenerate a 2nd copy
                CodeGenHelper.AssertNotGenerated(generatedCode, "TestDomainShared");
                CodeGenHelper.AssertNotGenerated(generatedCode, "TestEntity2");

                // This property is in shared code in a p2p referenced assembly and should not have been generated
                CodeGenHelper.AssertNotGenerated(generatedCode, "public int SharedProperty_CL2");

                // Test that we get an informational message about skipping this shared domain context
                string msg = string.Format(CultureInfo.CurrentCulture, Resource.Shared_DomainContext_Skipped, "TestDomainSharedService");
                TestHelper.AssertContainsMessages(mockBuildEngine.ConsoleLogger, msg);
            }
            finally
            {
                CodeGenHelper.DeleteTempFolder(task);
            }
        }

        [DeploymentItem(@"ProjectPath.txt", "STT")]
        [Description("CreateOpenRiaClientFilesTask produces error when detecting existing generated entity")]
        [TestMethod]
        public void SharedTypes_CodeGen_Errors_On_Existing_Generated_Entity()
        {
            CreateOpenRiaClientFilesTask task = null;

            try
            {
                task = CodeGenHelper.CreateOpenRiaClientFilesTaskInstance("STT", /*includeClientOutputAssembly*/ true);
                MockBuildEngine mockBuildEngine = task.BuildEngine as MockBuildEngine;

                bool success = task.Execute();
                Assert.IsFalse(success, "Expected build to fail");
                string entityMsg = string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_EntityTypesCannotBeShared_Reference, "ServerClassLib.TestEntity");
                string complexMsg = string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_ComplexTypesCannotBeShared_Reference, "ServerClassLib.TestComplexType");
                TestHelper.AssertContainsErrors(mockBuildEngine.ConsoleLogger, entityMsg, complexMsg);
            }
            finally
            {
                CodeGenHelper.DeleteTempFolder(task);
            }
        }

        [TestMethod]
        [Description("Codegen emits a warning if an entity property is not shared")]
        public void SharedTypes_CodeGen_Warns_Unshared_Property_Type()
        {
            ConsoleLogger logger = new ConsoleLogger();

            // Create a shared type service that says the entity's attribute is "shared" when asked whether it is shared
            MockSharedCodeService mockSts = new MockSharedCodeService(
                    Array.Empty<Type>(),
                    Array.Empty<MethodBase>(),
                    Array.Empty<string>());

            string generatedCode = TestHelper.GenerateCode("C#", new Type[] { typeof(Mock_CG_Shared_DomainService)}, logger, mockSts);

            Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "Code should have been generated");

            string entityWarning = String.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_PropertyType_Not_Shared, "XElementProperty", typeof(Mock_CG_Shared_Entity).FullName, typeof(System.Xml.Linq.XElement).FullName, "MockProject");
            string complexTypeWarning = String.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_PropertyType_Not_Shared, "ComplexXElementProperty", typeof(Mock_CG_Shared_Complex_Type).FullName, typeof(System.Xml.Linq.XElement).FullName, "MockProject");
            TestHelper.AssertContainsWarnings(logger, entityWarning, complexTypeWarning);
        }
    }

    public class Mock_CG_Shared_DomainService : GenericDomainService<Mock_CG_Shared_Entity>
    {
        public Mock_CG_Shared_Complex_Type Invoke() { return null;  }
    }

    public partial class Mock_CG_Shared_Entity
    {
        public Mock_CG_Shared_Entity() { }

        [Key]
        public string TheKey { get; set; }

        // This property type will be defined as "unshared" in the unit tests
        public System.Xml.Linq.XElement XElementProperty { get; set; }
    }

    public partial class Mock_CG_Shared_Complex_Type
    {
        // This property type will be defined as "unshared" in the unit tests
        public System.Xml.Linq.XElement ComplexXElementProperty { get; set; }
    }
}
