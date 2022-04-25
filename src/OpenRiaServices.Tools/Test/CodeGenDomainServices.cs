using System;
using System.Linq;
using OpenRiaServices.Server;
using OpenRiaServices.Server.Test.Utilities;
using Cities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices;
using TestDomainServices.TypeNameConflictResolution;
using TestDomainServices.TypeNameConflictResolution.ExternalConflicts;

using ServerResource = OpenRiaServices.Server.Resource;

namespace OpenRiaServices.Tools.Test
{
    /// <summary>
    /// Summary description for domain service code gen
    /// </summary>
    [TestClass]
    public class CodeGenDomainServices
    {
        private readonly ConsoleLogger _logger;
        public CodeGenDomainServices()
        {
            _logger = new ConsoleLogger();
        }
        [DeploymentItem(@"Baselines\Default\Mocks", "CG_Scenarios_Complex")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_Complex")]
        [DeploymentItem(@"Shared\Test.shared.cs")]
        [TestMethod]
        public void TestClientCodegen_ComplexTypeScenarios()
        {
            string[] sharedFiles = new string[] { 
                TestHelper.GetTestFileName("Test.shared.cs"),
            };

            Type[] types = new[]
            {
                typeof(TestDomainServices.ComplexTypes_TestService),
                typeof(TestDomainServices.ComplexTypes_DomainService),
                typeof(TestDomainServices.ComplexTypes_InvokeOperationsOnly)
            };

            TestHelper.CodeGenValidationOptions options = new TestHelper.CodeGenValidationOptions(@"Default\Mocks", "CG_Scenarios_Complex", "ComplexTypeScenarios.g",
                types, "C#", sharedFiles, false);

            TestHelper.ValidateCodeGen(options);

            options = new TestHelper.CodeGenValidationOptions(@"Default\Mocks", "CG_Scenarios_Complex", "ComplexTypeScenarios.g",
                types, "VB", Enumerable.Empty<string>(), false);

            TestHelper.ValidateCodeGen(options);
        }

        [DeploymentItem(@"Baselines\Default\Mocks", "CG_Scenarios_Complex_RootNs_FullTypeNames")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_Complex_RootNs_FullTypeNames")]
        [DeploymentItem(@"Shared\Test.shared.cs")]
        [TestMethod]
        public void TestClientCodegen_ComplexTypeScenarios_RootNs_FullTypeNames()
        {
            string[] sharedFiles = new string[] { 
                TestHelper.GetTestFileName("Test.shared.cs"),
            };

            Type[] types = new[]
            {
                typeof(TestDomainServices.ComplexTypes_TestService),
                typeof(TestDomainServices.ComplexTypes_DomainService),
                typeof(TestDomainServices.ComplexTypes_InvokeOperationsOnly)
            };

            TestHelper.CodeGenValidationOptions options = new TestHelper.CodeGenValidationOptions(@"Default\Mocks", "CG_Scenarios_Complex_RootNs_FullTypeNames", "ComplexTypeScenarios_RootNs_FullTypeNames.g",
                types, "VB", Enumerable.Empty<string>(), "RootNamespaceName", true);

            TestHelper.ValidateCodeGen(options);
        }

        [DeploymentItem(@"Baselines\Default\Scenarios", "CG_Scenarios_Comp")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_Comp")]
        [TestMethod]
        public void TestClientCodegen_CompositionScenarios()
        {
            Type[] types = new[]
            {
                typeof(TestDomainServices.CompositionScenarios_Explicit),
                typeof(TestDomainServices.CompositionScenarios_Various)
            };

            TestHelper.CodeGenValidationOptions options = new TestHelper.CodeGenValidationOptions(@"Default\Scenarios", "CG_Scenarios_Comp", "CompositionScenarios.g",
                types, string.Empty, Enumerable.Empty<string>(), false);

            TestHelper.ValidateCodeGen(options);
        }

        [DeploymentItem(@"Baselines\Default\Scenarios", "CG_Scenarios_CI")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_CI")]
        [TestMethod]
        public void TestClientCodegen_CompositionInheritanceScenarios()
        {
            Type[] types = new[]
            {
                typeof(TestDomainServices.CompositionInheritanceScenarios),
                typeof(TestDomainServices.AssociationInheritanceScenarios)
            };

            TestHelper.CodeGenValidationOptions options = new TestHelper.CodeGenValidationOptions(@"Default\Scenarios", "CG_Scenarios_CI", "CompositionInheritanceScenarios.g",
                types, string.Empty, Enumerable.Empty<string>(), false);

            TestHelper.ValidateCodeGen(options);
        }

        [TestMethod]
        [WorkItem(205955)]
        [Description("Tests that the scenario where there is an association between parent and other class containing the child type throws")]
        public void TestClientCodeGen_InvalidAssociationScenarioTest()
        {
            string error = string.Format(ServerResource.InvalidAssociation_TypesDoNotAlign, "C_B", typeof(Association_A), typeof(Association_C));
            TestHelper.GenerateCodeAssertFailure("C#", new Type[] { typeof(InvalidAssociationScenarios) }, error);
        }

        [DeploymentItem(@"Baselines\Default\Scenarios", "CG_Scenarios_MP")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_MP")]
        [TestMethod]
        public void TestClientCodegen_MultipleDomainServices()
        {
            string[] sharedFiles = Array.Empty<string>();

            // Default codegen
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"Default\Scenarios", "CG_Scenarios_MP", "MultipleProviderScenarios.g",
                new Type[] { 
                    typeof(TestDomainServices.EF.Northwind), typeof(TestDomainServices.LTS.Northwind)}, "C#", sharedFiles, false));
        }

        [DeploymentItem(@"Baselines\FullTypeNames\Scenarios", "CG_Scenarios_FullTypes")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_FullTypes")]
        [TestMethod]
        public void TestClientCodegen_MultipleDomainServices_FullTypes()
        {
            string[] sharedFiles = Array.Empty<string>();

            // Full type names
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"FullTypeNames\Scenarios", "CG_Scenarios_FullTypes", "MultipleProviderScenarios.g",
                new Type[] { typeof(TestDomainServices.EF.Northwind), typeof(TestDomainServices.LTS.Northwind) }, "C#", sharedFiles, true));
        }

        [DeploymentItem(@"Baselines\Default\Scenarios", "CG_Scenarios_EFDbContext")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_EFDbContext")]
        [TestMethod]
        public void TestClientCodegen_EFDbCtxDomainServices()
        {
            string[] sharedFiles = Array.Empty<string>();

            // Default codegen
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"Default\Scenarios", "CG_Scenarios_EFDbContext", "EFDbContextScenarios.g",
                typeof(TestDomainServices.DbCtx.Northwind), sharedFiles, false));
        }

        [DeploymentItem(@"Baselines\FullTypeNames\Scenarios", "CG_Scenarios_EFDbContext_FullTypes")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_EFDbContext_FullTypes")]
        [TestMethod]
        public void TestClientCodegen_EFDbCtxDomainServices_FullTypes()
        {
            string[] sharedFiles = Array.Empty<string>();

            // Full type names
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"FullTypeNames\Scenarios", "CG_Scenarios_EFDbContext_FullTypes", "EFDbContextScenarios.g",
                typeof(TestDomainServices.DbCtx.Northwind), sharedFiles, true));
        }

        [DeploymentItem(@"Baselines\Default\Scenarios", "CG_Scenarios_EFContext")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_EFContext")]
        [TestMethod]
        public void TestClientCodegen_EFCoreDomainServices()
        {
            string[] sharedFiles = Array.Empty<string>();

            // Default codegen
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"Default\Scenarios", "CG_Scenarios_EFContext", "EFCoreContextScenarios.g",
                typeof(TestDomainServices.EFCore.Northwind), sharedFiles, false));
        }

        [DeploymentItem(@"Baselines\FullTypeNames\Scenarios", "CG_Scenarios_EFDbContext_FullTypes")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_EFDbContext_FullTypes")]
        [TestMethod]
        public void TestClientCodegen_EFCoreEFDbCtxDomainServices_FullTypes()
        {
            string[] sharedFiles = Array.Empty<string>();

            // Full type names
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"FullTypeNames\Scenarios", "CG_Scenarios_EFDbContext_FullTypes", "EFCoreDbContextScenarios.g",
                typeof(TestDomainServices.EFCore.Northwind), sharedFiles, true));
        }

        [DeploymentItem(@"Baselines\Default\Scenarios", "CG_Scenarios_EFCFDbContext")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_EFCFDbContext")]
        [TestMethod]
        public void TestClientCodegen_EFCFDomainServices()
        {
            string[] sharedFiles = Array.Empty<string>();

            // Default codegen
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"Default\Scenarios", "CG_Scenarios_EFCFDbContext", "EFCFDbContextScenarios.g",
               typeof(TestDomainServices.EFCF.Northwind), sharedFiles, false));
        }

        [DeploymentItem(@"Baselines\FullTypeNames\Scenarios", "CG_Scenarios_EFCFDbContext_FullTypes")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_EFCFDbContext_FullTypes")]
        [TestMethod]
        public void TestClientCodegen_EFCFDomainServices_FullTypes()
        {
            string[] sharedFiles = Array.Empty<string>();

            // Full type names
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"FullTypeNames\Scenarios", "CG_Scenarios_EFCFDbContext_FullTypes", "EFCFDbContextScenarios.g",
                typeof(TestDomainServices.EFCF.Northwind), sharedFiles, true));
        }

        [DeploymentItem(@"Baselines\Default\Scenarios", "CG_Scenarios_LTSNW")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_LTSNW")]
        [TestMethod]
        public void TestClientCodegen_LTSNorthwindScenarios()
        {
            string[] sharedFiles = Array.Empty<string>();

            // Default
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"Default\Scenarios", "CG_Scenarios_LTSNW", "LTSNorthwindScenarios.g",
                typeof(DataTests.Scenarios.LTS.Northwind.LTS_NorthwindScenarios), sharedFiles, false));
        }

        [DeploymentItem(@"Baselines\Default\Scenarios", "CG_EF_Inheritance")]
        [DeploymentItem(@"ProjectPath.txt", "CG_EF_Inheritance")]
        [TestMethod]
        [Description("Validates baseline of simple inheritance model using LTS model")]
        public void TestClientCodegen_EF_Inheritance()
        {
            string[] sharedFiles = Array.Empty<string>();

            // Default
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"Default\Scenarios", "CG_EF_Inheritance", "EF_Inheritance.g",
                typeof(DataTests.Inheritance.EF.EF_Inheritance_DomainService), sharedFiles, false));
        }

        [DeploymentItem(@"Baselines\Default\Scenarios", "CG_LTS_Inheritance")]
        [DeploymentItem(@"ProjectPath.txt", "CG_LTS_Inheritance")]
        [TestMethod]
        [Description("Validates baseline of simple inheritance model using LTS model")]
        public void TestClientCodegen_LTS_Inheritance()
        {
            string[] sharedFiles = Array.Empty<string>();

            // Default
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"Default\Scenarios", "CG_LTS_Inheritance", "LTS_Inheritance.g",
                typeof(DataTests.Inheritance.LTS.LTS_Inheritance_DomainService), sharedFiles, false));
        }

        [DeploymentItem(@"Baselines\Default\Scenarios", "CG_Scenarios_Include")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_Include")]
        [TestMethod]
        public void TestClientCodegen_IncludeScenarios()
        {
            string[] sharedFiles = Array.Empty<string>();

            // Default
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"Default\Scenarios", "CG_Scenarios_Include", "IncludeScenariosTestProvider.g",
                typeof(TestDomainServices.IncludeScenariosTestProvider), sharedFiles, false));
        }

        [DeploymentItem(@"Baselines\Default\Scenarios", "CG_Scenarios_Inherit")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_Inherit")]
        [TestMethod]
        public void TestClientCodegen_InheritanceScenarios()
        {
            // Before we run the baseline test, verify the service configuration
            // Don't modify any of these existing checks, since validity of the
            // baseline check being performed relies on the exact service configuration
            DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(TestProvider_Inheritance1));
            Assert.IsFalse(description.EntityTypes.Contains(typeof(InheritanceBase)));
            Assert.IsTrue(description.EntityTypes.Contains(typeof(InheritanceT1)));

            // Default
            TestHelper.ValidateLanguageCodeGen(new TestHelper.CodeGenValidationOptions(@"Default\Scenarios", "CG_Scenarios_Inherit", "InheritanceScenarios1.g",
                typeof(TestDomainServices.TestProvider_Inheritance1), "C#", Array.Empty<string>(), false));
        }

        [DeploymentItem(@"Baselines\FullTypeNames\Scenarios", "CG_Scenarios_Inherit_FullTypes")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_Inherit_FullTypes")]
        [TestMethod]
        public void TestClientCodegen_InheritanceScenarios_FullTypes()
        {
            // Before we run the baseline test, verify the service configuration
            // Don't modify any of these existing checks, since validity of the
            // baseline check being performed relies on the exact service configuration
            DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(TestProvider_Inheritance1));
            Assert.IsFalse(description.EntityTypes.Contains(typeof(InheritanceBase)));
            Assert.IsTrue(description.EntityTypes.Contains(typeof(InheritanceT1)));

            // Full type names
            TestHelper.ValidateLanguageCodeGen(new TestHelper.CodeGenValidationOptions(@"FullTypeNames\Scenarios", "CG_Scenarios_Inherit_FullTypes", "InheritanceScenarios1.g",
                typeof(TestDomainServices.TestProvider_Inheritance1), "C#", Array.Empty<string>(), true));
        }

        [DeploymentItem(@"Baselines\Default\Scenarios", "CG_Scenarios_INTF")]
        [DeploymentItem(@"Shared\Mock.shared.cs")]
        [DeploymentItem(@"Shared\Mock.shared.vb")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_INTF")]
        [TestMethod]
        public void TestClientCodegen_InterfaceInheritanceScenarios()
        {
            string[] sharedFiles = new string[]
            {
                TestHelper.GetTestFileName("Mock.shared.cs"),
                TestHelper.GetTestFileName("Mock.shared.vb")
            };

            // Default
            TestHelper.CodeGenValidationOptions options = new TestHelper.CodeGenValidationOptions(@"Default\Scenarios", "CG_Scenarios_INTF", "InterfaceInheritance.g",
                typeof(TestDomainServices.InterfaceInheritanceDomainService), sharedFiles, false);
            options.AddSharedType(typeof(System.Xml.Linq.XElement));
            TestHelper.ValidateCodeGen(options);
        }

        [DeploymentItem(@"Baselines\Default\Scenarios", "CG_Scenarios_Secure")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_Secure")]
        [TestMethod]
        public void TestClientCodegen_RequiresSecureEndpointScenarios()
        {
            // Default
            TestHelper.ValidateLanguageCodeGen(new TestHelper.CodeGenValidationOptions(@"Default\Scenarios", "CG_Scenarios_Secure", "RequiresSecureEndpointScenarios.g",
                typeof(TestDomainServices.TestService_RequiresSecureEndpoint), "C#", Array.Empty<string>(), false));
        }

        [DeploymentItem(@"Baselines\Default\Scenarios", "CG_Scenarios")]
        [DeploymentItem(@"Shared\Mock.shared.cs")]
        [DeploymentItem(@"Shared\Mock.shared.vb")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios")]
        [TestMethod]
        public void TestClientCodegen_Scenarios_CodeGen()
        {
            string[] sharedFiles = new string[]
            {
                TestHelper.GetTestFileName("Mock.shared.cs"),
                TestHelper.GetTestFileName("Mock.shared.vb")
            };

            // Default
            var options = new TestHelper.CodeGenValidationOptions(@"Default\Scenarios", "CG_Scenarios", "TestProvider_Scenarios_CodeGen.g",
                typeof(TestDomainServices.TestProvider_Scenarios_CodeGen), sharedFiles, false);
            // Simulate that this attribute is shared to cause code gen to propagate it
            options.AddSharedType(typeof(CustomNamespace.CustomAttribute));
            options.AddSharedType(typeof(TestEnum));
            TestHelper.ValidateCodeGen(options);
        }

        [DeploymentItem(@"Baselines\FullTypeNames\Scenarios", "CG_Scenarios_FullTypes_1")]
        [DeploymentItem(@"Shared\Mock.shared.cs")]
        [DeploymentItem(@"Shared\Mock.shared.vb")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_FullTypes_1")]
        [TestMethod]
        public void TestClientCodegen_Scenarios_CodeGen_FullTypes()
        {
            string[] sharedFiles = new string[]
            {
                TestHelper.GetTestFileName("Mock.shared.cs"),
                TestHelper.GetTestFileName("Mock.shared.vb")
            };

            // Full types
            var options = new TestHelper.CodeGenValidationOptions(@"FullTypeNames\Scenarios", "CG_Scenarios_FullTypes_1", "TestProvider_Scenarios_CodeGen.g",
                typeof(TestDomainServices.TestProvider_Scenarios_CodeGen), sharedFiles, true);
            // Simulate that this attribute is shared to cause code gen to propagate it
            options.AddSharedType(typeof(CustomNamespace.CustomAttribute));
            options.AddSharedType(typeof(TestEnum));
            TestHelper.ValidateCodeGen(options);
        }

        [DeploymentItem(@"Baselines\Default\Scenarios", "CG_Scenarios_1")]
        [DeploymentItem(@"Shared\Mock.shared.cs")]
        [DeploymentItem(@"Shared\Mock.shared.vb")]
        [DeploymentItem(@"Shared\Test.shared.cs")]
        [DeploymentItem(@"Shared\Test.shared.vb")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_1")]
        [TestMethod]
        public void TestClientCodegen_Scenarios()
        {
            Type[] types = new[]
            {
                typeof(TestDomainServices.TestProvider_Scenarios),
                typeof(TestDomainServices.NamedUpdates.NamedUpdate_CustomOnly),
                typeof(TestDomainServices.NamedUpdates.NamedUpdate_CustomAndUpdate),
                typeof(TestDomainServices.NamedUpdates.NamedUpdate_CustomValidation),
                typeof(TestDomainServices.NamedUpdates.CalculatorDomainService),
            };
            string[] sharedFiles = new string[]
            {
                TestHelper.GetTestFileName("Mock.shared.cs"),
                TestHelper.GetTestFileName("Mock.shared.vb"),
                TestHelper.GetTestFileName("Test.shared.cs"),
                TestHelper.GetTestFileName("Test.shared.vb"),
            };

            // Default
            TestHelper.CodeGenValidationOptions options = new TestHelper.CodeGenValidationOptions(@"Default\Scenarios", "CG_Scenarios_1", "TestProvider_Scenarios.g",
                types, string.Empty, sharedFiles, false);
            // Simulate that this attribute is shared to cause code gen to propagate it
            options.AddSharedType(typeof(CustomNamespace.CustomAttribute));
            options.AddSharedType(typeof(TestEnum));
            options.AddSharedType(typeof(System.Xml.Linq.XElement));

            TestHelper.ValidateCodeGen(options);
        }

        [DeploymentItem(@"Baselines\FullTypeNames\Scenarios", "CG_Scenarios_FullTypes_2")]
        [DeploymentItem(@"Shared\Mock.shared.cs")]
        [DeploymentItem(@"Shared\Mock.shared.vb")]
        [DeploymentItem(@"Shared\Test.shared.cs")]
        [DeploymentItem(@"Shared\Test.shared.vb")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_FullTypes_2")]
        [TestMethod]
        public void TestClientCodegen_Scenarios_FullTypes()
        {
            Type[] types = new[]
            {
                typeof(TestDomainServices.TestProvider_Scenarios),
                typeof(TestDomainServices.NamedUpdates.NamedUpdate_CustomOnly),
                typeof(TestDomainServices.NamedUpdates.NamedUpdate_CustomAndUpdate),
                typeof(TestDomainServices.NamedUpdates.NamedUpdate_CustomValidation),
                typeof(TestDomainServices.NamedUpdates.CalculatorDomainService),
            };
            string[] sharedFiles = new string[]
            {
                TestHelper.GetTestFileName("Mock.shared.cs"),
                TestHelper.GetTestFileName("Mock.shared.vb"),
                TestHelper.GetTestFileName("Test.shared.cs"),
                TestHelper.GetTestFileName("Test.shared.vb"),
            };

            // Full types
            TestHelper.CodeGenValidationOptions options = new TestHelper.CodeGenValidationOptions(@"FullTypeNames\Scenarios", "CG_Scenarios_FullTypes_2", "TestProvider_Scenarios.g",
                types, string.Empty, sharedFiles, true);
            // Simulate that this attribute is shared to cause code gen to propagate it
            options.AddSharedType(typeof(CustomNamespace.CustomAttribute));
            options.AddSharedType(typeof(TestEnum));
            options.AddSharedType(typeof(System.Xml.Linq.XElement));

            TestHelper.ValidateCodeGen(options);
        }

        [DeploymentItem(@"Baselines\Default\Scenarios", "CG_Scenarios_3")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_3")]
        [TestMethod]
        [Description("Generated code should include error messages from attributes that throw exceptions")]
        public void TestClientCodeGen_AttributeThrowing()
        {
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"Default\Scenarios", "CG_Scenarios_3", "AttributeThrowing.g",
                typeof(TestDomainServices.AttributeThrowingDomainService), string.Empty, Array.Empty<string>(), false));
        }

        [DeploymentItem(@"Baselines\Default\Scenarios", "CG_Scenarios_4")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_4")]
        [TestMethod]
        [Description("Generated code in VB should correctly handle root namespace inside VB project")]
        public void VBRootNamespaceTest()
        {
            // Default
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"Default\Scenarios", "CG_Scenarios_4", "VBRootNamespaceScenarios.g",
                new Type[] { typeof(VBRootNamespaceTest.VBRootNamespaceTestDomainService), typeof(VBRootNamespaceTest.Inner.VBRootNamespaceTestProviderInsideInner), typeof(VBRootNamespaceTest2.VBRootNamespaceTestDomainService2) }, "VB", Array.Empty<string>(), "VBRootNamespaceTest", false));
        }

        [DeploymentItem(@"Baselines\FullTypeNames\Scenarios", "CG_Scenarios_FullTypes_5")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_FullTypes_5")]
        [TestMethod]
        [Description("Generated code in VB should correctly handle root namespace inside VB project")]
        public void VBRootNamespaceTest_FullTypes()
        {
            // Full type names
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"FullTypeNames\Scenarios", "CG_Scenarios_FullTypes_5", "VBRootNamespaceScenarios.g",
                new Type[] { typeof(VBRootNamespaceTest.VBRootNamespaceTestDomainService), typeof(VBRootNamespaceTest.Inner.VBRootNamespaceTestProviderInsideInner), typeof(VBRootNamespaceTest2.VBRootNamespaceTestDomainService2), typeof(VBRootNamespaceTest3.VBRootNamespaceTestDomainService3) }, 
                "VB", Array.Empty<string>(), "VBRootNamespaceTest", true));
        }

        [DeploymentItem(@"Baselines\Default\Cities", "CG_Cities")]
        [DeploymentItem(@"Cities\Cities.shared.cs")]
        [DeploymentItem(@"Cities\Cities.shared.vb")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Cities")]
        [TestMethod]
        [Description("Create client proxies for City domain service and compare to known good copy")]
        public void TestCityClientProxies()
        {
            string[] sharedFiles = new string[] { 
                TestHelper.GetTestFileName("Cities.shared.cs"),
                TestHelper.GetTestFileName("Cities.shared.vb")
            };

            // Default
            TestHelper.CodeGenValidationOptions options = new TestHelper.CodeGenValidationOptions(@"Default\Cities", "CG_Cities", "Cities.g", typeof(CityDomainService), sharedFiles, false);
            options.AddSharedType(typeof(Cities.TimeZone));
            TestHelper.ValidateCodeGen(options);
        }

        [DeploymentItem(@"Baselines\Default\Mocks", "CG_Mocks")]
        [DeploymentItem(@"Mocks\MockDomainServices.cs")]
        [DeploymentItem(@"Baselines\Default\Cities")]
        [DeploymentItem(@"Cities\Cities.shared.cs")]
        [DeploymentItem(@"Cities\Cities.shared.vb")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Mocks")]
        [TestMethod]
        [Description("Create client proxies for MockCustomer domain service and compare to known good copy")]
        public void TestMockCustomerProviderClientProxies()
        {
            string[] sharedFiles = new string[] { 
                TestHelper.GetTestFileName("Cities.g.cs"),
                TestHelper.GetTestFileName("Cities.shared.cs"),
                TestHelper.GetTestFileName("Cities.g.vb"),
                TestHelper.GetTestFileName("Cities.shared.vb")
            };

            // Default
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"Default\Mocks", "CG_Mocks", "MockCustomers.g", typeof(MockCustomerDomainService), sharedFiles, false));
        }

        [DeploymentItem(@"Baselines\Default\LTS", "CG_CATLTS")]
        [DeploymentItem(@"ProjectPath.txt", "CG_CATLTS")]
        [TestMethod]
        [Description("Create client proxies for Linq to Sql domain service and compare to known good copy")]
        public void TestCatalogLTSClientProxies()
        {
            // Default
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"Default\LTS", "CG_CATLTS", "Catalog_LTS.g", typeof(TestDomainServices.LTS.Catalog), Array.Empty<string>(), false));
        }

        [DeploymentItem(@"Baselines\Default\LTS", "CG_NWLTS")]
        [DeploymentItem(@"ProjectPath.txt", "CG_NWLTS")]
        [TestMethod]
        public void TestNorthwindLTSClientProxies()
        {
            // Default
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"Default\LTS", "CG_NWLTS", "Northwind_LTS.g", typeof(TestDomainServices.LTS.Northwind), Array.Empty<string>(), false));
        }

        [DeploymentItem(@"Baselines\Default\EF", "CG_NWEF")]
        [DeploymentItem(@"ProjectPath.txt", "CG_NWEF")]
        [TestMethod]
        public void TestNorthwindEFClientProxies()
        {
            // Default
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"Default\EF", "CG_NWEF", "Northwind_EF.g", typeof(TestDomainServices.EF.Northwind), Array.Empty<string>(), false));
        }

        [DeploymentItem(@"Baselines\Default\EF", "CG_NWEF")]
        [DeploymentItem(@"ProjectPath.txt", "CG_NWEF")]
        [TestMethod]
        public void TestNorthwindEFCoreClientProxies()
        {
            // Default
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"Default\EF", "CG_NWEF", "Northwind_EFCore.g", typeof(TestDomainServices.EFCore.Northwind), Array.Empty<string>(), false));
        }

        [DeploymentItem(@"Baselines\Default\EF", "CG_CATEF")]
        [DeploymentItem(@"ProjectPath.txt", "CG_CATEF")]
        [TestMethod]
        [Description("Create client proxies for Linq to Entities domain service and compare to known good copy")]
        public void TestCatalogEFClientProxies()
        {
            // TODO: Test codegeneration (kodgenereringstester) (kopera denna mapp med filer ?)
            // Om samma databasmodell => kopiera genererade file (.g.cs) och skriv tester
            // Validera både för vb och c#
            // Default
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"Default\EF", "CG_CATEF", "Catalog_EF.g", typeof(TestDomainServices.EF.Catalog), Array.Empty<string>(), false));
        }

        [DeploymentItem(@"Baselines\Default\EF", "CG_CATEFDbCtx")]
        [DeploymentItem(@"ProjectPath.txt", "CG_CATEFDbCtx")]
        [TestMethod]
        [Description("Create client proxies for Linq to Entities domain service and compare to known good copy")]
        public void TestCatalogEFDbCtxClientProxies()
        {
            // Default
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"Default\EF", "CG_CATEFDbCtx", "Catalog_EFDbCtx.g", typeof(TestDomainServices.DbCtx.Catalog), Array.Empty<string>(), false));
        }

        [DeploymentItem(@"Baselines\Default\EF", "CG_CATEFDbCtx")]
        [DeploymentItem(@"ProjectPath.txt", "CG_CATEFDbCtx")]
        [TestMethod]
        [Description("Create client proxies for Linq to Entities domain service and compare to known good copy")]
        public void TestCatalogEFCoreClientProxies()
        {
            // Default
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"Default\EF", "CG_CATEFDbCtx", "Catalog_EFCore.g", typeof(TestDomainServices.EFCore.Catalog), Array.Empty<string>(), false));
        }

        [DeploymentItem(@"Baselines\Default\Scenarios", "CG_CONFLICT_RESOLUTION")]
        [DeploymentItem(@"ProjectPath.txt", "CG_CONFLICT_RESOLUTION")]
        [TestMethod]
        [Description("Create client proxies and verifies that entity type conflicts are resolved correctly.")]
        public void TestClientCodeGen_ConflictResolution()
        {
            Type[] providerTypes = new Type[] 
            {
                typeof(DomainServiceScenario1),
                typeof(DomainServiceScenario2),
                typeof(BaseTypeConflicts)
            };

            // Default
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"Default\Scenarios", "CG_CONFLICT_RESOLUTION", "ConflictResolution.EntityConflicts.g", providerTypes, null, Array.Empty<string>(), false));
        }

        [DeploymentItem(@"Baselines\FullTypeNames\Scenarios", "CG_CONFLICT_RESOLUTION_FullTypes")]
        [DeploymentItem(@"ProjectPath.txt", "CG_CONFLICT_RESOLUTION_FullTypes")]
        [TestMethod]
        [Description("Create client proxies and verifies that entity type conflicts are resolved correctly.")]
        public void TestClientCodeGen_ConflictResolution_FullTypes()
        {
            Type[] providerTypes = new Type[] 
            {
                typeof(DomainServiceScenario1),
                typeof(DomainServiceScenario2),
                typeof(BaseTypeConflicts)
            };

            // Full type names
            TestHelper.ValidateCodeGen(new TestHelper.CodeGenValidationOptions(@"FullTypeNames\Scenarios", "CG_CONFLICT_RESOLUTION_FullTypes", "ConflictResolution.EntityConflicts.g", providerTypes, null, Array.Empty<string>(), true));
        }

        [TestMethod]
        [Description("Create client proxies with unavoidable type conflicts.")]
        [WorkItem(706000)]
        public void TestClientCodeGen_ConflictResolution_FullTypeConflicts()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(ForceTypeConflicts));
        }

        [TestMethod]
        [Description("Create client proxies and verifies DomainService member name conflicts are reported correctly.")]
        public void TestClientCodeGen_ConflictResolution_OnlineMethods()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCode("C#", typeof(OnlineMethodConflict), logger);

            // Verify the task failed
            string error = string.Format(Resource.ClientCodeGen_NamingCollision_MemberAlreadyExists, "OnlineMethodConflict", "Entities");
            TestHelper.AssertCodeGenFailure(generatedCode, logger, error);

            // Validate error list
            Assert.AreEqual(1, logger.ErrorMessages.Count);
            Assert.AreEqual(0, logger.WarningMessages.Count);
        }

        [TestMethod]
        [Description("Create client proxies and verifies DomainService member name conflicts are reported correctly.")]
        public void TestClientCodeGen_ConflictResolution_DomainMethods()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCode("C#", typeof(DomainMethodConflict), logger);

            // Verify the task failed
            string error = string.Format(Resource.EntityCodeGen_NamingCollision_EntityCustomMethodNameAlreadyExists, typeof(Entity).FullName, "Name");
            TestHelper.AssertCodeGenFailure(generatedCode, logger, error);

            // Validate error list
            Assert.AreEqual(1, logger.ErrorMessages.Count);
            Assert.AreEqual(0, logger.WarningMessages.Count);
        }

        [TestMethod]
        [Description("Test code gen with incorrect association for bug 626901.")]
        public void TestClientCodeGen_IncorrectAssocation_Bug626901()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCode("C#", typeof(IncorrectAssicationProvider_Bug626901), logger);

            // Verify the task failed
            string error = string.Format(ServerResource.InvalidAssociation_ThisKeyNotFound, "A_Bug626901_B_Bug626901", "TestDomainServices.A_Bug626901", "B_ID");
            TestHelper.AssertCodeGenFailure(generatedCode, logger, error);
        }

        [TestMethod]
        [WorkItem(629280)]
        [Description("Test code gen with type specified in range attribute.")]
        public void TestClientCodeGen_RangeAttributeWithType()
        {
            MockSharedCodeService sts = TestHelper.CreateCommonMockSharedCodeService();
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", new Type[] {typeof(Provider_RangeAttributeWithType_Bug629280)}, null, sts);

            // Validate generated code
            TestHelper.AssertGeneratedCodeContains(
                generatedCode,
                @"[Range(typeof(DateTime), ""1/1/1980"", ""1/1/2001"")] public DateTime RangeWithDateTime",
                @"[Range(((double)(1.1D)), ((double)(1.1D)))] public double RangeWithDouble",
                @"[Range(typeof(double), ""1.1"", ""1.1"")] public double RangeWithDoubleAsString",
                @"[Range(1, 1)] public int RangeWithInteger",
                @"[Range(typeof(int), ""1"", ""1"")] public int RangeWithIntegerAsString",
                @"[Range(typeof(int), null, null)] public int RangeWithNullStrings",
                @"[Range(typeof(int), null, ""1"")] public int RangeWithNullString1",
                @"[Range(typeof(int), ""1"", null)] public int RangeWithNullString2",
                @"[Range(1, 10, ErrorMessage=""Range must be between 1 and 10"")] public int RangeWithErrorMessage",
                @"[Range(1, 10, ErrorMessageResourceType=typeof(SharedResource), ErrorMessageResourceName=""String"")] public int RangeWithResourceMessage");
        }

        [TestMethod]
        [Description("Verifies that errors are reported correctly when domain service types are nested.")]
        public void TestClientCodeGen_NestedDomainServicesProhibited()
        {
            string error = string.Format(Resource.ClientCodeGen_DomainService_CannotBeNested, typeof(TestDomainServices.MockCustomerDomainService_SharedEntityTypes.MockCustomerDomainService_Nested));
            TestHelper.GenerateCodeAssertFailure("C#", new Type[] { typeof(TestDomainServices.MockCustomerDomainService_SharedEntityTypes.MockCustomerDomainService_Nested) }, error);
        }

        [TestMethod]
        [WorkItem(796616)]
        [Description("Verifies that excluded members references aren't codegen'ed in associated property setters.")]
        public void TestClientCodeGen_ExcludedAssociation()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(MockDomainService_ExcludedAssociation));
            ConsoleLogger c = new ConsoleLogger();
            c.LogMessage(generatedCode);
            TestHelper.AssertGeneratedCodeDoesNotContain(
                generatedCode,
                "previous.MockOrderDetails.Remove(this);",
                "value.MockOrderDetails.Add(this);");       
        }

        [TestMethod]
        [WorkItem(851335)]
        [Description("Verifies that domain services and entities in a global namespace raise codegen errors")]
        public void TestClientCodeGen_GlobalNamespace_InvalidDomainService()
        {
            string error1 = string.Format(Resource.ClientCodeGen_Namespace_Required, typeof(GlobalNamespaceTest_DomainService_Invalid));
            string error2 = string.Format(Resource.ClientCodeGen_Namespace_Required, typeof(GlobalNamespaceTest_Entity_Invalid));
            TestHelper.GenerateCodeAssertFailure("C#", typeof(GlobalNamespaceTest_DomainService_Invalid), error1, error2);
        }

        [DeploymentItem(@"Baselines\Default\Scenarios", "CG_Global")]
        [DeploymentItem(@"Shared\Global.shared.cs")]
        [DeploymentItem(@"Shared\Global.shared.vb")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Global")]
        [TestMethod] 
        [WorkItem(851335)]
        [Description("Verifies that codegen succeeds when global types are exposed to all areas of the generator.")]
        public void TestClientCodeGen_GlobalNamespace()
        {
            string[] sharedFiles = new string[]
            {
                TestHelper.GetTestFileName("Global.shared.cs"),
                TestHelper.GetTestFileName("Global.shared.vb"),
            };

            TestHelper.CodeGenValidationOptions options = new TestHelper.CodeGenValidationOptions(
                @"Default\Scenarios",
                "CG_Global",
                "GlobalNamespace.g",
                typeof(GlobalNamespaceTest.GlobalNamespaceTest_DomainService),
                sharedFiles,
                /* useFullTypeNames */ false);

            options.AddSharedType(typeof(GlobalNamespaceTest_Attribute));
            options.AddSharedType(typeof(GlobalNamespaceTest_Enum));
            options.AddSharedType(typeof(GlobalNamespaceTest_Validation));
            options.AddSharedType(typeof(GlobalNamespaceTest_ValidationAttribute));
            options.AddSharedMethod(typeof(GlobalNamespaceTest_Validation).GetMethod("Validate"));

            TestHelper.ValidateCodeGen(options);
        }

        [DeploymentItem(@"Baselines\FullTypeNames\Scenarios", "CG_Global_Full")]
        [DeploymentItem(@"Shared\Global.shared.cs")]
        [DeploymentItem(@"Shared\Global.shared.vb")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Global_Full")]
        [TestMethod]
        [WorkItem(851335)]
        [Description("Verifies that codegen succeeds when global types are exposed to all areas of the generator using full type names.")]
        public void TestClientCodeGen_GlobalNamespace_FullNames()
        {
            string[] sharedFiles = new string[]
            {
                TestHelper.GetTestFileName("Global.shared.cs"),
                TestHelper.GetTestFileName("Global.shared.vb"),
            };

            TestHelper.CodeGenValidationOptions options = new TestHelper.CodeGenValidationOptions(
                @"FullTypeNames\Scenarios",
                "CG_Global_Full",
                "GlobalNamespace.g",
                typeof(GlobalNamespaceTest.GlobalNamespaceTest_DomainService),
                /* language */ null,
                sharedFiles,
                /* useFullTypeNames */ true);

            options.AddSharedType(typeof(GlobalNamespaceTest_Attribute));
            options.AddSharedType(typeof(GlobalNamespaceTest_Enum));
            options.AddSharedType(typeof(GlobalNamespaceTest_Validation));
            options.AddSharedType(typeof(GlobalNamespaceTest_ValidationAttribute));
            options.AddSharedMethod(typeof(GlobalNamespaceTest_Validation).GetMethod("Validate"));

            TestHelper.ValidateCodeGen(options);
        }

        [DeploymentItem(@"Baselines\Default\Scenarios", "CG_Global")]
        [DeploymentItem(@"Shared\Global.shared.cs")]
        [DeploymentItem(@"Shared\Global.shared.vb")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Global")]
        [TestMethod]
        [WorkItem(851335)]
        [Description("Verifies that codegen succeeds when global types are exposed to all areas of the generator when the RootNamespace is empty.")]
        public void TestClientCodeGen_GlobalNamespace_NoRootNamespace()
        {
            string[] sharedFiles = new string[]
            {
                TestHelper.GetTestFileName("Global.shared.cs"),
                TestHelper.GetTestFileName("Global.shared.vb"),
            };

            TestHelper.CodeGenValidationOptions options = new TestHelper.CodeGenValidationOptions(
                @"Default\Scenarios",
                "CG_Global",
                "GlobalNamespace_NoRootNamespace.g",
                new Type[] { typeof(GlobalNamespaceTest.GlobalNamespaceTest_DomainService) },
                /* language */ null,
                sharedFiles,
                /* rootNamespace */ string.Empty,
                /* useFullTypeNames */ false);

            options.AddSharedType(typeof(GlobalNamespaceTest_Attribute));
            options.AddSharedType(typeof(GlobalNamespaceTest_Enum));
            options.AddSharedType(typeof(GlobalNamespaceTest_Validation));
            options.AddSharedType(typeof(GlobalNamespaceTest_ValidationAttribute));
            options.AddSharedMethod(typeof(GlobalNamespaceTest_Validation).GetMethod("Validate"));

            TestHelper.ValidateCodeGen(options);
        }

        [DeploymentItem(@"Baselines\FullTypeNames\Scenarios", "CG_Global_Full")]
        [DeploymentItem(@"Shared\Global.shared.cs")]
        [DeploymentItem(@"Shared\Global.shared.vb")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Global_Full")]
        [TestMethod]
        [WorkItem(851335)]
        [Description("Verifies that codegen succeeds when global types are exposed to all areas of the generator using full type names when the RootNamespace is empty.")]
        public void TestClientCodeGen_GlobalNamespace_NoRootNamespace_FullNames()
        {
            string[] sharedFiles = new string[]
            {
                TestHelper.GetTestFileName("Global.shared.cs"),
                TestHelper.GetTestFileName("Global.shared.vb"),
            };

            TestHelper.CodeGenValidationOptions options = new TestHelper.CodeGenValidationOptions(
                @"FullTypeNames\Scenarios",
                "CG_Global_Full",
                "GlobalNamespace_NoRootNamespace.g",
                new Type[] { typeof(GlobalNamespaceTest.GlobalNamespaceTest_DomainService) },
                /* language */ null,
                sharedFiles,
                /* rootNamespace */ string.Empty,
                /* useFullTypeNames */ true);

            options.AddSharedType(typeof(GlobalNamespaceTest_Attribute));
            options.AddSharedType(typeof(GlobalNamespaceTest_Enum));
            options.AddSharedType(typeof(GlobalNamespaceTest_Validation));
            options.AddSharedType(typeof(GlobalNamespaceTest_ValidationAttribute));
            options.AddSharedMethod(typeof(GlobalNamespaceTest_Validation).GetMethod("Validate"));

            TestHelper.ValidateCodeGen(options);
        }

        [DeploymentItem(@"Baselines\Default\Scenarios", "CG_SystemNamespace")]
        [DeploymentItem(@"Shared\SystemNamespace.shared.cs")]
        [DeploymentItem(@"Shared\SystemNamespace.shared.vb")]
        [DeploymentItem(@"ProjectPath.txt", "CG_SystemNamespace")]
        [TestMethod]
        [WorkItem(810123)]
        [Description("Verifies that codegen succeeds when System namespaces are included in user domain services")]
        public void TestClientCodeGen_SystemNamespace()
        {
            string[] sharedFiles = new string[]
            {
                TestHelper.GetTestFileName("SystemNamespace.shared.cs"),
                TestHelper.GetTestFileName("SystemNamespace.shared.vb"),
            };

            TestHelper.CodeGenValidationOptions options = new TestHelper.CodeGenValidationOptions(
                @"Default\Scenarios",
                "CG_SystemNamespace",
                "SystemNamespace.g",
                new Type[] { typeof(System.SystemDomainService), typeof(System.Subsystem.SubsystemDomainService), typeof(SystemExtensions.SystemExtensionsDomainService) },
                /* language */ null,
                sharedFiles,
                /* rootNamespace */ string.Empty,
                /* useFullTypeNames */ false);

            options.AddSharedType(typeof(System.SystemNamespaceAttribute));
            options.AddSharedType(typeof(System.Subsystem.SubsystemNamespaceAttribute));
            options.AddSharedType(typeof(System.SystemEnum));
            options.AddSharedType(typeof(System.Subsystem.SubsystemEnum));
            options.AddSharedType(typeof(SystemExtensions.SystemExtensionsNamespaceAttribute));
            options.AddSharedType(typeof(SystemExtensions.SystemExtensionsEnum));

            TestHelper.ValidateCodeGen(options);
        }

        [DeploymentItem(@"Baselines\FullTypeNames\Scenarios", "CG_SystemNamespace_Full")]
        [DeploymentItem(@"Shared\SystemNamespace.shared.cs")]
        [DeploymentItem(@"Shared\SystemNamespace.shared.vb")]
        [DeploymentItem(@"ProjectPath.txt", "CG_SystemNamespace_Full")]
        [TestMethod]
        [WorkItem(810123)]
        [Description("Verifies that codegen succeeds when System namespaces are included in user domain services using full names")]
        public void TestClientCodeGen_SystemNamespace_FullNames()
        {
            string[] sharedFiles = new string[]
            {
                TestHelper.GetTestFileName("SystemNamespace.shared.cs"),
                TestHelper.GetTestFileName("SystemNamespace.shared.vb"),
            };

            TestHelper.CodeGenValidationOptions options = new TestHelper.CodeGenValidationOptions(
                @"FullTypeNames\Scenarios",
                "CG_SystemNamespace_Full",
                "SystemNamespace.g",
                new Type[] { typeof(System.SystemDomainService), typeof(System.Subsystem.SubsystemDomainService), typeof(SystemExtensions.SystemExtensionsDomainService) },
                /* language */ null,
                sharedFiles,
                /* rootNamespace */ string.Empty,
                /* useFullTypeNames */ true);

            options.AddSharedType(typeof(System.SystemNamespaceAttribute));
            options.AddSharedType(typeof(System.Subsystem.SubsystemNamespaceAttribute));
            options.AddSharedType(typeof(System.SystemEnum));
            options.AddSharedType(typeof(System.Subsystem.SubsystemEnum));
            options.AddSharedType(typeof(SystemExtensions.SystemExtensionsNamespaceAttribute));
            options.AddSharedType(typeof(SystemExtensions.SystemExtensionsEnum));

            TestHelper.ValidateCodeGen(options);
        }

        [DeploymentItem(@"Baselines\Default\Scenarios", "CG_Scenarios_SharedEntities")]
        [DeploymentItem(@"ProjectPath.txt", "CG_Scenarios_SharedEntities")]
        [TestMethod]
        [Description("Test that entities shared across two domain services can be generated.")]
        public void TestClientCodegen_SharedEntities()
        {
            Type[] types = new[]
            {
                typeof(SharedEntities.ExposeChildEntityDomainService),
                typeof(SharedEntities.ExposeParentEntityDomainService)
            };

            TestHelper.CodeGenValidationOptions options = new TestHelper.CodeGenValidationOptions(@"Default\Scenarios", "CG_Scenarios_SharedEntities", "SharedEntities.g",
                types, string.Empty, Enumerable.Empty<string>(), false);

            TestHelper.ValidateCodeGen(options);
        }

        [TestMethod]
        [Description("Verifies that SybmitChanges methods are not generated if there are no CUD operations")]
        [WorkItem(170442)]
        public void TestClientCodeGen_NoSubmitChangesWithoutCUDOp()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(DomainServiceWithoutCUD));
            TestHelper.AssertGeneratedCodeDoesNotContain(
                generatedCode,
                "IAsyncResult BeginSubmitChanges(IEnumerable<ChangeSetEntry> changeSet, AsyncCallback callback, object asyncState)",
                "IEnumerable<ChangeSetEntry> EndSubmitChanges(IAsyncResult result)");

            generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(DomainServiceWithCreate));
            TestHelper.AssertGeneratedCodeContains(
                generatedCode,
                "IAsyncResult BeginSubmitChanges(IEnumerable<ChangeSetEntry> changeSet, AsyncCallback callback, object asyncState)",
                "IEnumerable<ChangeSetEntry> EndSubmitChanges(IAsyncResult result)");

            generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(DomainServiceWithUpdate));
            TestHelper.AssertGeneratedCodeContains(
                generatedCode,
                "IAsyncResult BeginSubmitChanges(IEnumerable<ChangeSetEntry> changeSet, AsyncCallback callback, object asyncState)",
                "IEnumerable<ChangeSetEntry> EndSubmitChanges(IAsyncResult result)");

            generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(DomainServiceWithDelete));
            TestHelper.AssertGeneratedCodeContains(
                generatedCode,
                "IAsyncResult BeginSubmitChanges(IEnumerable<ChangeSetEntry> changeSet, AsyncCallback callback, object asyncState)",
                "IEnumerable<ChangeSetEntry> EndSubmitChanges(IAsyncResult result)");

            generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(DomainServiceWithNamedUpdate));
            TestHelper.AssertGeneratedCodeContains(
                generatedCode,
                "IAsyncResult BeginSubmitChanges(IEnumerable<ChangeSetEntry> changeSet, AsyncCallback callback, object asyncState)",
                "IEnumerable<ChangeSetEntry> EndSubmitChanges(IAsyncResult result)");
        }

        [TestMethod]
        [WorkItem(180336)]
        [Description("Verifies that codegen fails if there is RoundtripOriginal on Derived type but not on Base type")]
        public void TestClientCodeGen_EntityWithRoundtripOriginalNotOnBase()
        {
            TestHelper.GenerateCodeAssertFailure("C#", typeof(MockDomainService_WithRoundtripOriginalEntities),
                string.Format(Resource.EntityCodeGen_RoundtripOriginalOnBaseType, "TestDomainServices.EntityWithRoundtripOriginal_Derived", "TestDomainServices.EntityWithoutRoundtripOriginal_Base"));
        }

        [TestMethod]
        [WorkItem(180336)]
        [Description("Verifies that codegen succeeds if there is RoundtripOriginal on the unexposed base type then the derived exposed types also have it.")]
        public void TestClientCodeGen_EntityWithRoundtripOriginalOnUnexposedBase()
        {
            TestHelper.GenerateCodeAssertSuccess("C#", typeof(MockDomainService_WithRoundtripOriginalEntities2));
        }

        [TestMethod]
        [Description("Verifies that Indexer properties on the entity (and related attributes like DefaultMemberAttribute) are not generated")]
        [WorkItem(175694)]
        public void TestClientCodeGen_NoIndexerPropertyGenerated()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(DomainServiceWithIndexerEntity));
            TestHelper.AssertGeneratedCodeDoesNotContain(
                generatedCode,
                "public int Item",
                "[DefaultMember(\"Item\")]");
        }

        [TestMethod]
        [WorkItem(180336)]
        [Description("Verifies that codegen fails if there is a RoundtripOriginalAttribute on an association property")]
        public void TestClientCodeGen_RTO_EntityWithRoundtripOriginalOnAssociationProperty()
        {
            TestHelper.GenerateCodeAssertFailure("C#", typeof(MockDomainService_WithRoundtripOriginalEntities3),
                string.Format(ServerResource.InvalidAssociation_RoundTripOriginal, "PropWithPropLevelRTO", "TestDomainServices.RTO_EntityWithRoundtripOriginalOnAssociationProperty"));
        }

        [TestMethod]
        [WorkItem(184732)]
        [Description("Verifies that codegen succeeds if there is RoundtripOriginal on an entity that is part of an association.")]
        public void TestClientCodeGen_RTO_EntityWithRoundtripOriginalOnAssociationPropType()
        {
            TestHelper.GenerateCodeAssertSuccess("C#", typeof(MockDomainService_WithRoundtripOriginalEntities4));
        }

        [TestMethod]
        [WorkItem(184732)]
        [Description("Verifies that codegen fails if there is RoundtripOriginal on an association property even if it is also on the containing entity")]
        public void TestClientCodeGen_RTO_EntityWithRoundtripOriginalOnAssociationPropertyAndOnEntity()
        {
            TestHelper.GenerateCodeAssertFailure("C#", typeof(MockDomainService_WithRoundtripOriginalEntities5),
                string.Format(ServerResource.InvalidAssociation_RoundTripOriginal, "PropWithPropLevelRTO", "TestDomainServices.RTO_EntityWithRoundtripOriginalOnAssociationPropertyAndOnEntity"));
        }
       
        [TestMethod]
        [WorkItem(184735)]
        [Description("Verifies member level RoundtripOriginalAttributes are ignored when it is also applied at the type level")]
        public void TestClientCodeGen_RTO_EntityWithRoundtripOriginalOnMember()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(MockDomainService_WithRoundtripOriginalEntities6));
            TestHelper.AssertGeneratedCodeDoesNotContain(generatedCode,
                    @"[DataMember()] [Editable(false, AllowInitialValue=true)] [Key()] [RoundtripOriginal()] public int ID",
                    @"[DataMember()] [RoundtripOriginal()] public string PropWithPropLevelRTO");
        }
    }
}
