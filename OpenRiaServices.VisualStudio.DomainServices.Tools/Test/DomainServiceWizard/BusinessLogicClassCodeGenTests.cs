// #define UPDATE_BASELINES    // uncomment to update baselines in bulk
using System;

using System.Collections.Generic;
using System.IO;
using OpenRiaServices.DomainServices.Server.Test.Utilities;
using AdventureWorksModel;
using OpenRiaServices.VisualStudio.DomainServices.Tools.Test.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper = OpenRiaServices.VisualStudio.DomainServices.Tools.Test.Utilities.TestHelper;
using OpenRiaServices.DomainServices.Tools.Test;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools.Test
{
    [TestClass]
    public class BusinessLogicClassCodeGenTests
    {
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            UnitTestTraceListener.Initialize(testContext, true);
            TestHelper.EnsureL2SSupport(true);
        }

        [ClassCleanup()]
        public static void MyClassCleanup()
        {
            UnitTestTraceListener.Reset();
            TestHelper.EnsureL2SSupport(false);
        }

#if UPDATE_BASELINES
        [TestMethod]
        [Description("This test always fails during baseline update to prevent check-in")]
        public void BusinessLogicClass_CodeGen_Fail_UpdateBaselines()
        {
            Assert.Fail("UPDATE_BASELINES is enabled.  This test fails to prevent check-in");
        }
#endif

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies C# codegen baseline for BusinessLogicClass for an empty model")]
        public void BusinessLogicClass_CodeGen_Empty_CSharp()
        {
            this.ValidateCodeGen("C#", null, "Empty_DomainService", new string[0]);
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies VB codegen baseline for BusinessLogicClass for an empty model")]
        public void BusinessLogicClass_CodeGen_Empty_VB()
        {
            this.ValidateCodeGen("VB", null, "Empty_DomainService", new string[0]);
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies C# codegen baseline for BusinessLogicClass for an empty model with an OData endpoing")]
        public void BusinessLogicClass_CodeGen_Empty_OData_CSharp()
        {
            string[] references = new string[] { "OpenRiaServices.DomainServices.Hosting.OData" };
            this.ValidateCodeGen("C#", null, "Empty_DomainService_OData", references, /*oDataEndpoint*/ true);
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies VB codegen baseline for BusinessLogicClass for an empty model with an OData endpoint")]
        public void BusinessLogicClass_CodeGen_Empty_OData_VB()
        {
            string[] references = new string[] { "OpenRiaServices.DomainServices.Hosting.OData" };
            this.ValidateCodeGen("VB", null, "Empty_DomainService_OData", references, /*oDataEndpoint*/ true);
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies C# codegen baseline for BusinessLogicClass for EF Northwind scenario model")]
        public void BusinessLogicClass_CodeGen_EF_Northwind_Scenarios_CSharp()
        {
            string[] references = new string[] { "EntityFramework", "OpenRiaServices.DomainServices.EntityFramework" };
            this.ValidateCodeGen("C#", typeof(DataTests.Scenarios.EF.Northwind.NorthwindEntities_Scenarios), "EF_Northwind_Scenarios", references);
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies VB codegen baseline for BusinessLogicClass for EF Northwind scenario model")]
        public void BusinessLogicClass_CodeGen_EF_Northwind_Scenarios_VB()
        {
            string[] references = new string[] { "EntityFramework", "OpenRiaServices.DomainServices.EntityFramework" };
            this.ValidateCodeGen("VB", typeof(DataTests.Scenarios.EF.Northwind.NorthwindEntities_Scenarios), "EF_Northwind_Scenarios", references);
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies C# codegen baseline for BusinessLogicClass for EF Northwind POCO model")]
        public void BusinessLogicClass_CodeGen_EF_Northwind_POCO_CSharp()
        {
            string[] references = new string[] { "EntityFramework", "OpenRiaServices.DomainServices.EntityFramework" };
            this.ValidateCodeGen("C#", typeof(NorthwindPOCOModel.NorthwindEntities), "EF_Northwind_POCO", references);
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies VB codegen baseline for BusinessLogicClass for EF Northwind POCO model")]
        public void BusinessLogicClass_CodeGen_EF_Northwind_POCO_VB()
        {
            string[] references = new string[] { "EntityFramework", "OpenRiaServices.DomainServices.EntityFramework" };
            this.ValidateCodeGen("VB", typeof(NorthwindPOCOModel.NorthwindEntities), "EF_Northwind_POCO", references);
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies VB codegen baseline for BusinessLogicClass for EF Northwind POCO model with root namespace same as namespace")]
        public void BusinessLogicClass_CodeGen_EF_Northwind_POCO_VB_RootNs()
        {
            string[] references = new string[] { "EntityFramework", "OpenRiaServices.DomainServices.EntityFramework" };
            this.ValidateCodeGen("VB", typeof(NorthwindPOCOModel.NorthwindEntities), "EF_Northwind_POCO_RootNS", references, false, "BizLogic.Test", "BizLogic.Test");
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies C# codegen baseline for BusinessLogicClass for LTS Northwind scenario model")]
        public void BusinessLogicClass_CodeGen_LTS_Northwind_Scenarios_CSharp()
        {
            string[] references = new string[] { "System.Data.Linq", "OpenRiaServices.DomainServices.LinqToSql" };
            this.ValidateCodeGen("C#", typeof(DataTests.Scenarios.LTS.Northwind.NorthwindScenarios), "LTS_Northwind_Scenarios", references);
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies VB codegen baseline for BusinessLogicClass for LTS Northwind scenario model")]
        public void BusinessLogicClass_CodeGen_LTS_Northwind_Scenarios_VB()
        {
            string[] references = new string[] { "System.Data.Linq", "OpenRiaServices.DomainServices.LinqToSql" };
            this.ValidateCodeGen("VB", typeof(DataTests.Scenarios.LTS.Northwind.NorthwindScenarios), "LTS_Northwind_Scenarios", references);
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies C# codegen baseline for BusinessLogicClass for EF Northwind model")]
        public void BusinessLogicClass_CodeGen_EF_Northwind_CSharp()
        {
            string[] references = new string[] { "EntityFramework", "OpenRiaServices.DomainServices.EntityFramework" };
            this.ValidateCodeGen("C#", typeof(NorthwindModel.NorthwindEntities), "EF_Northwind", references);
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies VB codegen baseline for BusinessLogicClass for EF Northwind model")]
        public void BusinessLogicClass_CodeGen_EF_Northwind_VB()
        {
            string[] references = new string[] { "EntityFramework", "OpenRiaServices.DomainServices.EntityFramework" };
            this.ValidateCodeGen("VB", typeof(NorthwindModel.NorthwindEntities), "EF_Northwind", references);
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies C# codegen baseline for BusinessLogicClass for LTS Northwind model")]
        public void BusinessLogicClass_CodeGen_LTS_Northwind_CSharp()
        {
            string[] references = new string[] { "System.Data.Linq", "OpenRiaServices.DomainServices.LinqToSql" };
            this.ValidateCodeGen("C#", typeof(DataTests.Northwind.LTS.NorthwindDataContext), "LTS_Northwind", references);
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies VB codegen baseline for BusinessLogicClass for LTS Northwind model")]
        public void BusinessLogicClass_CodeGen_LTS_Northwind_VB()
        {
            string[] references = new string[] { "System.Data.Linq", "OpenRiaServices.DomainServices.LinqToSql" };
            this.ValidateCodeGen("VB", typeof(DataTests.Northwind.LTS.NorthwindDataContext), "LTS_Northwind", references);
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies VB codegen baseline for BusinessLogicClass for LTS Northwind model with rootnamespace")]
        public void BusinessLogicClass_CodeGen_LTS_Northwind_VB_RootNs()
        {
            string[] references = new string[] { "System.Data.Linq", "OpenRiaServices.DomainServices.LinqToSql" };
            this.ValidateCodeGen("VB", typeof(DataTests.Northwind.LTS.NorthwindDataContext), "LTS_Northwind_RootNs", references, false, "BizLogic.Test", "BizLogic.Test");
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies C# codegen baseline for BusinessLogicClass for EF AdventureWorks model")]
        public void BusinessLogicClass_CodeGen_EF_AdventureWorks_CSharp()
        {
            string[] references = new string[] { "EntityFramework", "OpenRiaServices.DomainServices.EntityFramework" };
            this.ValidateCodeGen("C#", typeof(AdventureWorksEntities), "EF_AdventureWorks", references);
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies VB codegen baseline for BusinessLogicClass for EF AdventureWorks model")]
        public void BusinessLogicClass_CodeGen_EF_AdventureWorks_VB()
        {
            string[] references = new string[] { "EntityFramework", "OpenRiaServices.DomainServices.EntityFramework" };
            this.ValidateCodeGen("VB", typeof(AdventureWorksEntities), "EF_AdventureWorks", references);
        }
        
        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies C# codegen baseline for BusinessLogicClass for EF AdventureWorks model with OData endpoint")]
        public void BusinessLogicClass_CodeGen_EF_AdventureWorks_OData_CSharp()
        {
            string[] references = new string[] { "EntityFramework", "OpenRiaServices.DomainServices.EntityFramework", "OpenRiaServices.DomainServices.Hosting.OData" };
            this.ValidateCodeGen("C#", typeof(AdventureWorksEntities), "EF_AdventureWorks_OData", references, /*oDataEndpoint*/ true);
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies VB codegen baseline for BusinessLogicClass for EF AdventureWorks model with OData endpoint")]
        public void BusinessLogicClass_CodeGen_EF_AdventureWorks_OData_VB()
        {
            string[] references = new string[] { "EntityFramework", "OpenRiaServices.DomainServices.EntityFramework", "OpenRiaServices.DomainServices.Hosting.OData" };
            this.ValidateCodeGen("VB", typeof(AdventureWorksEntities), "EF_AdventureWorks_OData", references, /*oDataEndpoint*/ true);
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies C# codegen baseline for BusinessLogicClass for LTS AdventureWorks model")]
        public void BusinessLogicClass_CodeGen_LTS_AdventureWorks_CSharp()
        {
            string[] references = new string[] { "System.Data.Linq", "OpenRiaServices.DomainServices.LinqToSql" };
            this.ValidateCodeGen("C#", typeof(DataTests.AdventureWorks.LTS.AdventureWorks), "LTS_AdventureWorks", references);
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies VB codegen baseline for BusinessLogicClass for LTS AdventureWorks model")]
        public void BusinessLogicClass_CodeGen_LTS_AdventureWorks_VB()
        {
            string[] references = new string[] { "System.Data.Linq", "OpenRiaServices.DomainServices.LinqToSql" };
            this.ValidateCodeGen("VB", typeof(DataTests.AdventureWorks.LTS.AdventureWorks), "LTS_AdventureWorks", references);
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies C# codegen baseline for BusinessLogicClass for BuddyMetadataScenarios model that contains custom scenarios")]
        public void BusinessLogicClass_CodeGen_LTS_BuddyMetadataScenarios_CSharp()
        {
            string[] references = new string[] { "System.Data.Linq", "OpenRiaServices.DomainServices.LinqToSql" };
            this.ValidateCodeGen("C#", typeof(DataModels.ScenarioModels.BuddyMetadataScenariosDataContext), "BuddyMetadataScenarios", references);
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies VB codegen baseline for BusinessLogicClass for BuddyMetadataScenarios model that contains custom scenarios")]
        public void BusinessLogicClass_CodeGen_LTS_BuddyMetadataScenarios_VB()
        {
            string[] references = new string[] { "System.Data.Linq", "OpenRiaServices.DomainServices.LinqToSql" };
            this.ValidateCodeGen("VB", typeof(DataModels.ScenarioModels.BuddyMetadataScenariosDataContext), "BuddyMetadataScenarios", references);
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies c# codegen baseline for BusinessLogicClass for EF DbContext model")]
        public void BusinessLogicClass_CodeGen_EF_DBContextScenarios_CSharp()
        {
            string[] references = new string[] { "EntityFramework", "EntityFramework" };
            this.ValidateCodeGen("C#", typeof(DbContextModels.Northwind.DbCtxNorthwindEntities), "EF_DBContext_Northwind", references);
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies VB codegen baseline for BusinessLogicClass for EF DbContext model")]
        public void BusinessLogicClass_CodeGen_EF_DBContextScenarios_VB()
        {
            string[] references = new string[] { "EntityFramework", "EntityFramework" };
            this.ValidateCodeGen("VB", typeof(DbContextModels.Northwind.DbCtxNorthwindEntities), "EF_DBContext_Northwind", references);
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies c# codegen baseline for BusinessLogicClass for EF CodeFirst model")]
        public void BusinessLogicClass_CodeGen_EF_CodeFirstScenarios_CSharp()
        {
            string[] references = new string[] { "EntityFramework", "EntityFramework" };
            this.ValidateCodeGen("C#", typeof(CodeFirstModels.EFCFNorthwindEntities), "EF_CF_Context", references);
        }

        [DeploymentItem(@"Microsoft.VisualStudio.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt")]
        [TestMethod]
        [Description("Verifies VB codegen baseline for BusinessLogicClass for EF CodeFirst model")]
        public void BusinessLogicClass_CodeGen_EF_CodeFirstScenarios_VB()
        {
            string[] references = new string[] { "EntityFramework", "EntityFramework" };
            this.ValidateCodeGen("VB", typeof(CodeFirstModels.EFCFNorthwindEntities), "EF_CF_Context", references);
        }

        private void ValidateCodeGen(string language, Type contextType, string bizLogicFileBase, IEnumerable<string> references)
        {
            this.ValidateCodeGen(language, contextType, bizLogicFileBase, references, /*oDataEndpoint*/ false);
        }

        private void ValidateCodeGen(string language, Type contextType, string bizLogicFileBase, IEnumerable<string> references, bool oDataEndpoint)
        {
            this.ValidateCodeGen(language, contextType, bizLogicFileBase, references, oDataEndpoint,
                /* rootNamespace */ language.Equals("C#") ? null : "BizLogic.Test.Root",
                /* namespaceName */ "BizLogic.Test");
        }

        private void ValidateCodeGen(string language, Type contextType, string bizLogicFileBase, IEnumerable<string> references, bool oDataEndpoint, string rootNamespace, string namespaceName)
        {
#if UPDATE_BASELINES
            bool updateBaselines = true;
#else
            bool updateBaselines = false;
#endif
            string projectDir = Path.Combine(TestHelper.GetProjectDir(), @"Baselines");
            string extension = TestHelper.ExtensionFromLanguage(language);

            string bizLogicBaseName = bizLogicFileBase + extension;
            string buddyClassBaseName = bizLogicFileBase + ".metadata" + extension;

            string bizLogicFilePath = Path.Combine(projectDir, bizLogicBaseName);
            string buddyClassFilePath = Path.Combine(projectDir, buddyClassBaseName);

            string generatedBizLogicFileName = Path.GetTempFileName();

            string className = bizLogicFileBase;
            string assemblyName = (contextType == null) ? "NoAssembly" : contextType.Assembly.GetName().Name;

            Type[] contextTypes = (contextType == null) ? new Type[0] : new Type[] { contextType };

            using (BusinessLogicViewModel model = new BusinessLogicViewModel(projectDir, className, language, rootNamespace, assemblyName, contextTypes, /* IVsHelp object */ null))
            {
                // Always get the default, but will have 2 if specified a type
                int expectedCount = contextType == null ? 1 : 2;

                Assert.AreEqual(expectedCount, model.ContextViewModels.Count, "Expected this many view models");

                ContextViewModel expectedViewModel = contextType == null ? model.ContextViewModels[0] : model.ContextViewModels[1];
                Assert.AreEqual(expectedViewModel, model.CurrentContextViewModel, "current not as expected");

                model.CurrentContextViewModel.IsODataEndpointEnabled = oDataEndpoint;

                // Select entities first to allow buddy class code-gen
                foreach (EntityViewModel entity in model.CurrentContextViewModel.Entities)
                {
                    entity.IsIncluded = true;
                    entity.IsEditable = true;
                }

                // Don't generate buddy classes for empty model
                model.IsMetadataClassGenerationRequested = contextType != null;

                // Generate the business logic class
                GeneratedCode generatedCode = model.GenerateBusinessLogicClass(namespaceName);
                File.AppendAllText(generatedBizLogicFileName, generatedCode.SourceCode);
                TestHelper.AssertReferenceListContains(references, generatedCode.References, true);

                // Generate the buddy class
                // Note: we pass in an optional "Buddy" suffix to both the namespace and class names
                // because the compiler would reject an attempt to use 'partial class' on an already
                // compiled class.  We put it in a separate namespace because we still need to import
                // the entity's real namespace, and they cannot be the same.
                //
                string generatedBuddyFileName = null;
                if (model.IsMetadataClassGenerationRequested)
                {
                    generatedBuddyFileName = Path.GetTempFileName();
                    generatedCode = model.GenerateMetadataClasses("Buddy");
                    File.AppendAllText(generatedBuddyFileName, generatedCode.SourceCode);
                }

#if !UPDATE_BASELINES
                // See if both files compile clean against the current project
                string[] files = (model.IsMetadataClassGenerationRequested
                                    ? new string[] { generatedBizLogicFileName, generatedBuddyFileName }
                                    : new string[] { generatedBizLogicFileName });
                this.CompileGeneratedCode(TestHelper.GetProjectPath(), files, language);
#endif

                // Compare files against known baselines.
                // Optionally allow update of baselines rather than comparison
                TestHelper.ValidateFilesEqual(generatedBizLogicFileName, bizLogicFilePath, updateBaselines);

                if (model.IsMetadataClassGenerationRequested)
                {
                    TestHelper.ValidateFilesEqual(generatedBuddyFileName, buddyClassFilePath, updateBaselines);
                }

                // Clean up files.  Won't get here unless test passes
                File.Delete(generatedBizLogicFileName);

                if (model.IsMetadataClassGenerationRequested)
                {
                    File.Delete(generatedBuddyFileName);
                }
            }
        }

        private void CompileGeneratedCode(string projectPath, IEnumerable<string> files, string language)
        {
            List<string> referenceAssemblies = MsBuildHelper.GetReferenceAssemblies(projectPath);

            if (language == "C#")
            {
                CompilerHelper.CompileCSharpSourceFromFiles(files, referenceAssemblies, documentationFile: null);
            }
            else
            {
                CompilerHelper.CompileVisualBasicSourceFromFiles(files, referenceAssemblies, "TheRootNamespace", documentationFile: null);
            }
        }
    }
}
