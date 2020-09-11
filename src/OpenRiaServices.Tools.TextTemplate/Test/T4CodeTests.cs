using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using OpenRiaServices.DomainServices.Server.Test.Utilities;
using OpenRiaServices.DomainServices.Tools.Test;
using OpenRiaServices.DomainServices.Tools.TextTemplate.CSharpGenerators;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices;

namespace OpenRiaServices.DomainServices.Tools.TextTemplate.Test
{
    [TestClass]
    public class T4CodeTests
    {
        [TestMethod]
        public void T4CodeGenBasicTest()
        {
            Type[] domainServiceTypes = new Type[] {
                typeof(TestDomainServices.NamedUpdates.NamedUpdate_CustomOnly),
                typeof(TestDomainServices.NamedUpdates.NamedUpdate_CustomAndUpdate)
            };
            Type[] sharedTypes = new Type[] {
                typeof(CustomNamespace.CustomAttribute),
            };
            string[] refAssemblies = new string[] {
                Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName,
                typeof(CustomNamespace.CustomAttribute).Assembly.ManifestModule.FullyQualifiedName
            };

            TestHelper.GenerateAndVerifyCodeGenerators(domainServiceTypes, sharedTypes, refAssemblies);
        }

        [TestMethod]
        public void T4CodeGenTest_Inheritance()
        {
            Type[] domainServiceTypes = new Type[] {
                typeof(TestDomainServices.TestProvider_Inheritance1),             
            };
            Type[] sharedTypes = new Type[] {
                typeof(CustomNamespace.CustomAttribute),
            };
            string[] refAssemblies = new string[] {
                Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName,
                typeof(CustomNamespace.CustomAttribute).Assembly.ManifestModule.FullyQualifiedName
            };

            TestHelper.GenerateAndVerifyCodeGenerators(domainServiceTypes, sharedTypes, refAssemblies);
        }


        [TestMethod]
        public void T4CodeGenBasicTest2()
        {
            Type[] domainServiceTypes = new Type[] {
                typeof(TestDomainServices.TestProvider_Scenarios_CodeGen),             
            };
            Type[] sharedTypes = new Type[] {
                typeof(CustomNamespace.CustomAttribute),
                typeof(TestEnum)
            };
            string[] refAssemblies = new string[] {
                Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName,
                typeof(CustomNamespace.CustomAttribute).Assembly.ManifestModule.FullyQualifiedName
            };

            TestHelper.GenerateAndVerifyCodeGenerators(domainServiceTypes, sharedTypes, refAssemblies);
        }

        [TestMethod]
        public void T4CodeGenBasicTest3()
        {
            Type[] domainServiceTypes = new Type[] {
                typeof(TestDomainServices.EF.Northwind), 
                typeof(TestDomainServices.LTS.Northwind)            
            };
            Type[] sharedTypes = new Type[] {
            };
            string[] refAssemblies = new string[] {
                Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName,
            };

            TestHelper.GenerateAndVerifyCodeGenerators(domainServiceTypes, sharedTypes, refAssemblies);
        }

        [TestMethod]
        public void T4CodeGenTest_SharedEntities()
        {
            Type[] domainServiceTypes = new Type[] {
                typeof(SharedEntities.ExposeChildEntityDomainService),
                typeof(SharedEntities.ExposeParentEntityDomainService)
            };
            Type[] sharedTypes = new Type[] {
                typeof(CustomNamespace.CustomAttribute),
                typeof(TestEnum)
            };
            string[] refAssemblies = new string[] {
                Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName,
                typeof(CustomNamespace.CustomAttribute).Assembly.ManifestModule.FullyQualifiedName
            };

            TestHelper.GenerateAndVerifyCodeGenerators(domainServiceTypes, sharedTypes, refAssemblies);
        }

        [TestMethod]
        public void T4CodeGenTest_Northwind()
        {
            Type[] domainServiceTypes = new Type[] {
                typeof(TestDomainServices.EF.Northwind),
            };            
            Type[] sharedTypes = new Type[] { };
            string[] refAssemblies = new string[] {
                Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName,
            };

            TestHelper.GenerateAndVerifyCodeGenerators(domainServiceTypes, sharedTypes, refAssemblies);
        }

        [TestMethod]
        public void T4CodeGenTest_ComplexTypes()
        {
            Type[] domainServiceTypes = new Type[] {
                typeof(TestDomainServices.ComplexTypes_TestService),
                typeof(TestDomainServices.ComplexTypes_DomainService),
                typeof(TestDomainServices.ComplexTypes_InvokeOperationsOnly)
            };
            Type[] sharedTypes = new Type[] { };

            string[] refAssemblies = new string[] {
                Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName,
           };

            TestHelper.GenerateAndVerifyCodeGenerators(domainServiceTypes, sharedTypes, refAssemblies);
        }

        [TestMethod]
        public void T4CodeGenTest_ConflictTypes()
        {
            Type[] domainServiceTypes = new Type[] {
                typeof(TestDomainServices.TypeNameConflictResolution.ExternalConflicts.DomainServiceScenario1),
                typeof(TestDomainServices.TypeNameConflictResolution.ExternalConflicts.DomainServiceScenario2),
                typeof(TestDomainServices.TypeNameConflictResolution.BaseTypeConflicts)
            };
            Type[] sharedTypes = new Type[] { };

            string[] refAssemblies = new string[] {
                Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName,
           };

            TestHelper.GenerateAndVerifyCodeGenerators(domainServiceTypes, sharedTypes, refAssemblies);
        }

        [TestMethod]
        public void T4CodeGenTest_CompositionTypes()
        {
            Type[] domainServiceTypes = new Type[] {
                typeof(TestDomainServices.CompositionInheritanceScenarios),
                typeof(TestDomainServices.AssociationInheritanceScenarios)
            };
            Type[] sharedTypes = new Type[] { };

            string[] refAssemblies = new string[] {
                Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName,
           };

            TestHelper.GenerateAndVerifyCodeGenerators(domainServiceTypes, sharedTypes, refAssemblies);
        }

        [TestMethod]
        public void T4CodeGenTest_VariableNames()
        {
            Type[] domainServiceTypes = new Type[] {
                typeof(TestDomainService1)
            };
            Type[] sharedTypes = new Type[] { };

            string[] refAssemblies = new string[] {
                Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName,
           };

            TestHelper.GenerateAndVerifyCodeGenerators(domainServiceTypes, sharedTypes, refAssemblies);
        }

        [TestMethod]
        public void T4CodeGenTest_VBCodeGenError()
        {
            Type[] domainServiceTypes = new Type[] {
                typeof(TestDomainService1)
            };

            string[] refAssemblies = new string[] {
                Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName,
           };

            using (T4AssemblyGenerator asmGen = new T4AssemblyGenerator(false, /* isCSharp */ false, /* useFullTypeNames */ domainServiceTypes))
            {
                
                foreach (string refAssembly in refAssemblies)
                {
                    asmGen.ReferenceAssemblies.Add(refAssembly);
                }

                string generatedCode = asmGen.GeneratedCode;
                Assert.IsTrue(string.IsNullOrEmpty(generatedCode));
                Assert.IsTrue(asmGen.ConsoleLogger.HasLoggedErrors);
                Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, TextTemplateResource.NonCSharpLanguageNotSupported),
                    asmGen.ConsoleLogger.ErrorMessages.First());
            }
        }

        [TestMethod]
        public void T4CodeGenTest_VBCodeGenTest()
        {
            Type[] domainServiceTypes = new Type[] {};
            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions()
            {
                Language = "VB",
                ClientProjectPath = "MockProject.proj",
                ClientRootNamespace = "TestRootNS",
                UseFullTypeNames = false
            };
            ConsoleLogger consoleLogger = new ConsoleLogger();
            MockSharedCodeService mockSharedCodeService = new MockSharedCodeService(Array.Empty<Type>(), Array.Empty<MethodBase>(), Array.Empty<string>());
            MockCodeGenerationHost host = TestHelper.CreateMockCodeGenerationHost(consoleLogger, mockSharedCodeService);
            ClientCodeGenerator generator = (ClientCodeGenerator)new VBTestClientCodeGenerator();
            DomainServiceCatalog domainServiceCatalog = new DomainServiceCatalog(domainServiceTypes, consoleLogger);
            string generatedCode = generator.GenerateCode(host, domainServiceCatalog.DomainServiceDescriptions, options);

            Assert.IsTrue(string.IsNullOrEmpty(generatedCode));
            Assert.IsTrue(string.IsNullOrEmpty(consoleLogger.Errors));
        }
    }

    public class VBTestClientCodeGenerator : ClientCodeGenerator
    {
        /// <summary>
        /// Gets the C# entity generator.
        /// </summary>
        protected override EntityGenerator EntityGenerator { get { return new TestEntityGenerator(); } }

        /// <summary>
        /// Gets the C# complex object generator.
        /// </summary>
        protected override ComplexObjectGenerator ComplexObjectGenerator { get { return new TestComplexTypeGenerator(); } }

        /// <summary>
        /// Gets the C# DomainContext generator.
        /// </summary>
        protected override DomainContextGenerator DomainContextGenerator { get { return new TestDomainContextGenerator(); } }

        /// <summary>
        /// Gets the C# WebContext generator.
        /// </summary>
        protected override WebContextGenerator WebContextGenerator { get { return new TestWebContextGenerator(); } }

        /// <summary>
        /// Gets the C# enum generator.
        /// </summary>
        protected override EnumGenerator EnumGenerator { get { return new TestEnumGenerator(); } }        

        /// <summary>
        /// Generates proxy class code in C#.
        /// </summary>
        /// <returns>Generated C# code.</returns>
        protected override string GenerateCode()
        {
            if (!this.Options.Language.Equals("VB", StringComparison.OrdinalIgnoreCase))
            {
                this.CodeGenerationHost.LogError("The VBTestClientCodeGenerator supports only VB Code Generation.");
                return null;
            }

            return null;
        }
    }

    public class TestEntityGenerator : EntityGenerator
    {
        protected override string GenerateDataContractProxy()
        {
            throw new NotImplementedException();
        }
    }

    public class TestDomainContextGenerator : DomainContextGenerator
    {
        protected override string GenerateDomainContextClass()
        {
            throw new NotImplementedException();
        }
    }

    public class TestEnumGenerator : EnumGenerator
    {
        protected override string GenerateEnums()
        {
            throw new NotImplementedException();
        }
    }

    public class TestComplexTypeGenerator : ComplexObjectGenerator
    {
        protected override string GenerateDataContractProxy()
        {
            throw new NotImplementedException();
        }
    }

    public class TestWebContextGenerator : WebContextGenerator
    {
        protected override string GenerateWebContextClass()
        {
            throw new NotImplementedException();
        }
    }
}
