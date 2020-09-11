extern alias TextTemplate;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenRiaServices.Client.Test;
using OpenRiaServices.Server;
using OpenRiaServices.Server.Test.Utilities;
using OpenRiaServices.Hosting;
using OpenRiaServices.Tools.Test.T4Generator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Tools.Test
{
    /// <summary>
    /// Tests the MEF-based dispatcher to choose a code generator
    /// </summary>
    [TestClass]
    public class ClientCodeGenerationDispatcherTests
    {
        public ClientCodeGenerationDispatcherTests()
        {
        }

        [Description("ClientCodeGenerationDispatcher null language throws ArgumentException")]
        [TestMethod]
        public void ClientCodeGenerationDispatcher_Null_Language_Throws()
        {
            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions();
            ICodeGenerationHost host = TestHelper.CreateMockCodeGenerationHost(null, null);

            ExceptionHelper.ExpectArgumentException(() =>
            {
                new ClientCodeGenerationDispatcher().FindCodeGenerator(host, options, /*compositionAssemblies*/ null, /*codeGeneratorName*/ null);
            }, Resource.Null_Language_Property, "options");
        }

        [Description("ClientCodeGenerationDispatcher finds a custom code generator")]
        [TestMethod]
        public void ClientCodeGenerationDispatcher_Finds_Custom()
        {
            ConsoleLogger logger = new ConsoleLogger();
            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions()
            {
                Language = "C#"
            };

            ICodeGenerationHost host = TestHelper.CreateMockCodeGenerationHost(logger, /*sharedTypeService*/ null);

            // Create a new dispatcher and call an internal extensibility point to add ourselves
            // into the MEF composition container
            using (ClientCodeGenerationDispatcher dispatcher = new ClientCodeGenerationDispatcher())
            {
                string[] compositionAssemblies = new string[] { Assembly.GetExecutingAssembly().Location};

                IDomainServiceClientCodeGenerator generator = dispatcher.FindCodeGenerator(host, options, compositionAssemblies, MockCodeGenerator.GeneratorName);
                Assert.IsNotNull(generator, "the dispatcher did not find any code generator");
                Assert.AreEqual(generator.GetType(), typeof(MockCodeGenerator), "dispatcher found " + generator.GetType() + " but should have found MockCodeGenerator");

                string generatedCode = generator.GenerateCode(host, Enumerable.Empty<DomainServiceDescription>(), options);

                Assert.AreEqual(MockCodeGenerator.FakeGeneratedCode, generatedCode, "test code generator did not generate expected code.");
            }
        }

        [Description("ClientCodeGenerationDispatcher finds a custom code generator derived from another")]
        [TestMethod]
        public void ClientCodeGenerationDispatcher_Finds_Derived_Custom()
        {
            ConsoleLogger logger = new ConsoleLogger();
            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions()
            {
                Language = "G#"
            };

            ICodeGenerationHost host = TestHelper.CreateMockCodeGenerationHost(logger, /*sharedTypeService*/ null);

            // Create a new dispatcher and call an internal extensibility point to add ourselves
            // into the MEF composition container
            using (ClientCodeGenerationDispatcher dispatcher = new ClientCodeGenerationDispatcher())
            {
                string[] compositionAssemblies = new string[] { Assembly.GetExecutingAssembly().Location };

                IDomainServiceClientCodeGenerator generator = dispatcher.FindCodeGenerator(host, options, compositionAssemblies, MockGSharpCodeGeneratorDerived.GeneratorName);
                Assert.IsNotNull(generator, "the dispatcher did not find any code generator");
                Assert.AreEqual(generator.GetType(), typeof(MockGSharpCodeGeneratorDerived), "dispatcher found " + generator.GetType() + " but should have found MockGSharpCodeGeneratorDerived");

                string generatedCode = generator.GenerateCode(host, Enumerable.Empty<DomainServiceDescription>(), options);

                Assert.AreEqual(MockGSharpCodeGeneratorDerived.DerivedGeneratedCode, generatedCode, "test code generator did not generate expected code.");
            }
        }

        [Description("ClientCodeGenerationDispatcher custom code generator logs warnings successfully")]
        [TestMethod]
        public void ClientCodeGenerationDispatcher_Custom_Warns_Full()
        {
            ConsoleLogger logger = new ConsoleLogger();
            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions()
            {
                Language = "C#"
            };

            ICodeGenerationHost host = TestHelper.CreateMockCodeGenerationHost(logger, /*sharedTypeService*/ null);

            // Create a new dispatcher and call an internal extensibility point to add ourselves
            // into the MEF composition container
            using (ClientCodeGenerationDispatcher dispatcher = new ClientCodeGenerationDispatcher())
            {
                string[] compositionAssemblies = new string[] { Assembly.GetExecutingAssembly().Location };

                IDomainServiceClientCodeGenerator generator = dispatcher.FindCodeGenerator(host, options, compositionAssemblies, MockCodeGenerator.GeneratorName);
                Assert.IsNotNull(generator, "the dispatcher did not find any code generator");
                Assert.AreEqual(generator.GetType(), typeof(MockCodeGenerator), "dispatcher found " + generator.GetType() + " but should have found MockCodeGenerator");

                // Setting this option makes our custom code generator emit the packet below to test LogWarning
                MockCodeGenerator.LogWarningsFull = true;
                string generatedCode = generator.GenerateCode(host, Enumerable.Empty<DomainServiceDescription>(), options);

                Assert.AreEqual(MockCodeGenerator.FakeGeneratedCode, generatedCode, "test code generator did not generate expected code.");
                TestHelper.AssertContainsWarningPackets(logger, MockCodeGenerator.WarningPacket);
            }
        }

        [Description("ClientCodeGenerationDispatcher custom code generator logs errors successfully")]
        [TestMethod]
        public void ClientCodeGenerationDispatcher_Custom_Errors_Full()
        {
            ConsoleLogger logger = new ConsoleLogger();
            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions()
            {
                Language = "C#"
            };

            ICodeGenerationHost host = TestHelper.CreateMockCodeGenerationHost(logger, /*sharedTypeService*/ null);

            // Create a new dispatcher and call an internal extensibility point to add ourselves
            // into the MEF composition container
            using (ClientCodeGenerationDispatcher dispatcher = new ClientCodeGenerationDispatcher())
            {
                string[] compositionAssemblies = new string[] { Assembly.GetExecutingAssembly().Location };

                IDomainServiceClientCodeGenerator generator = dispatcher.FindCodeGenerator(host, options, compositionAssemblies, MockCodeGenerator.GeneratorName);
                Assert.IsNotNull(generator, "the dispatcher did not find any code generator");
                Assert.AreEqual(generator.GetType(), typeof(MockCodeGenerator), "dispatcher found " + generator.GetType() + " but should have found MockCodeGenerator");

                // The following option makes the custom generator invoke LogError with full file info that
                // matches the packet below.
                MockCodeGenerator.LogErrorsFull = true;
                string generatedCode = generator.GenerateCode(host, Enumerable.Empty<DomainServiceDescription>(), options);

                Assert.AreEqual(MockCodeGenerator.FakeGeneratedCode, generatedCode, "test code generator did not generate expected code.");
                TestHelper.AssertContainsErrorPackets(logger, MockCodeGenerator.ErrorPacket);
            }
        }

        [Description("ClientCodeGenerationDispatcher logs an error for a non-existent custom code generator")]
        [TestMethod]
        public void ClientCodeGenerationDispatcher_Error_Missing_Custom()
        {
            ConsoleLogger logger = new ConsoleLogger();
            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions()
            {
                Language = "C#",
                ClientProjectPath = "ClientProject",
                ServerProjectPath = "ServerProject"
            };

            ICodeGenerationHost host = TestHelper.CreateMockCodeGenerationHost(logger, /*sharedTypeService*/ null);

            // Create a new dispatcher and call an internal extensibility point to add ourselves
            // into the MEF composition container
            using (ClientCodeGenerationDispatcher dispatcher = new ClientCodeGenerationDispatcher())
            {
                string[] compositionAssemblies = new string[] { Assembly.GetExecutingAssembly().Location };

                IDomainServiceClientCodeGenerator generator = dispatcher.FindCodeGenerator(host, options, compositionAssemblies, "NotAGenerator");
                Assert.IsNull(generator, "the dispatcher should not find any code generator");

                string error = string.Format(CultureInfo.CurrentCulture, 
                                            Resource.Code_Generator_Not_Found, 
                                            "NotAGenerator", 
                                            options.Language, 
                                            options.ServerProjectPath, 
                                            options.ClientProjectPath,
                                            CodeDomClientCodeGenerator.GeneratorName);
                TestHelper.AssertContainsErrors(logger, error);
            }
        }

        [Description("ClientCodeGenerationDispatcher finds single custom code generator")]
        [TestMethod]
        public void ClientCodeGenerationDispatcher_Finds_Solitary_Custom()
        {
            ConsoleLogger logger = new ConsoleLogger();
            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions()
            {
                Language = "C#"
            };
            ICodeGenerationHost host = TestHelper.CreateMockCodeGenerationHost(logger, /*sharedTypeService*/ null);

            // Create a new dispatcher and call an internal extensibility point to add ourselves
            // into the MEF composition container
            using (ClientCodeGenerationDispatcher dispatcher = new ClientCodeGenerationDispatcher())
            {
                string[] compositionAssemblies = new string[] { Assembly.GetExecutingAssembly().Location, typeof(TextTemplate::OpenRiaServices.Tools.TextTemplate.ClientCodeGenerator).Assembly.Location };

                IDomainServiceClientCodeGenerator generator = dispatcher.FindCodeGenerator(host, options, compositionAssemblies, /*generatorName*/ null);
                Assert.IsNotNull(generator, "the dispatcher did not find any code generator");
                Assert.AreEqual(generator.GetType(), typeof(MockCodeGenerator), "dispatcher found " + generator.GetType() + " but should have found MockCodeGenerator");

                string generatedCode = generator.GenerateCode(host, Enumerable.Empty<DomainServiceDescription>(), options);

                Assert.AreEqual(MockCodeGenerator.FakeGeneratedCode, generatedCode, "test code generator did not generate expected code.");

                // Expect informational message
                string message = string.Format(CultureInfo.CurrentCulture, Resource.Using_Custom_Code_Generator, MockCodeGenerator.GeneratorName);
                TestHelper.AssertContainsMessages(logger, message);
            }
        }

        [Description("ClientCodeGenerationDispatcher finds a custom code generator in the presence of several")]
        [TestMethod]
        public void ClientCodeGenerationDispatcher_Finds_Custom_Among_Many()
        {
            ConsoleLogger logger = new ConsoleLogger();
            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions()
            {
                Language = "C#"
            };
            ICodeGenerationHost host = TestHelper.CreateMockCodeGenerationHost(logger, /*sharedTypeService*/ null);

            // Create a new dispatcher and call an internal extensibility point to add ourselves
            // into the MEF composition container
            using (ClientCodeGenerationDispatcher dispatcher = new ClientCodeGenerationDispatcher())
            {
                string[] compositionAssemblies = new string[] { Assembly.GetExecutingAssembly().Location, typeof(T4DomainServiceClientCodeGenerator).Assembly.Location };

                IDomainServiceClientCodeGenerator generator = dispatcher.FindCodeGenerator(host, options, compositionAssemblies, MockCodeGenerator.GeneratorName);
                Assert.IsNotNull(generator, "the dispatcher did not find any code generator");
                Assert.AreEqual(generator.GetType(), typeof(MockCodeGenerator), "dispatcher found " + generator.GetType() + " but should have found MockCodeGenerator");

                string generatedCode = generator.GenerateCode(host, Enumerable.Empty<DomainServiceDescription>(), options);

                Assert.AreEqual(MockCodeGenerator.FakeGeneratedCode, generatedCode, "test code generator did not generate expected code.");
            }
        }

        [Description("ClientCodeGenerationDispatcher finds default code generator by name in the presence of several")]
        [TestMethod]
        public void ClientCodeGenerationDispatcher_Finds_Default_By_Name()
        {
            ConsoleLogger logger = new ConsoleLogger();
            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions()
            {
                Language = "C#"
            };

            ICodeGenerationHost host = TestHelper.CreateMockCodeGenerationHost(logger, /*sharedTypeService*/ null);

            // Create a new dispatcher and call an internal extensibility point to add ourselves
            // into the MEF composition container
            using (ClientCodeGenerationDispatcher dispatcher = new ClientCodeGenerationDispatcher())
            {
                string[] compositionAssemblies = new string[] { Assembly.GetExecutingAssembly().Location, typeof(T4DomainServiceClientCodeGenerator).Assembly.Location };

                IDomainServiceClientCodeGenerator generator = dispatcher.FindCodeGenerator(host, options, compositionAssemblies, CodeDomClientCodeGenerator.GeneratorName);
                Assert.IsNotNull(generator, "the dispatcher did not find any code generator");
                Assert.IsTrue(typeof(CodeDomClientCodeGenerator).IsAssignableFrom(generator.GetType()), "dispatcher found " + generator.GetType() + " but should have found CodeDomClientCodeGenerator");

                DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(DispatcherDomainService));
                string generatedCode = generator.GenerateCode(host, new DomainServiceDescription[] { dsd }, options);

                Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "expected code to have been generated");
                Assert.IsTrue(generatedCode.Contains("public sealed partial class DispatcherDomainContext : DomainContext"), "Expected generated code to contain public sealed partial class DispatcherDomainContext : DomainContext");
            }
        }

        [Description("ClientCodeGenerationDispatcher finds T4 custom generator and can generate code")]
        [TestMethod]
        public void ClientCodeGenerationDispatcher_Generate_Using_T4_Custom()
        {
            ConsoleLogger logger = new ConsoleLogger();
            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions()
            {
                Language = "C#"
            };

            ICodeGenerationHost host = TestHelper.CreateMockCodeGenerationHost(logger, /*sharedTypeService*/ null);

            // Create a new dispatcher and call an internal extensibility point to add ourselves
            // into the MEF composition container
            using (ClientCodeGenerationDispatcher dispatcher = new ClientCodeGenerationDispatcher())
            {
                string[] compositionAssemblies = new string[] { typeof(T4DomainServiceClientCodeGenerator).Assembly.Location };

                IDomainServiceClientCodeGenerator generator = dispatcher.FindCodeGenerator(host, options, compositionAssemblies, null);
                Assert.IsNotNull(generator, "the dispatcher did not find any code generator");
                Assert.AreEqual(generator.GetType(), typeof(T4DomainServiceClientCodeGenerator), "dispatcher found " + generator.GetType() + " but should have found T4DomainServiceClientProxyGenerator");

                DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(DispatcherDomainService));
                string generatedCode = generator.GenerateCode(host, new DomainServiceDescription[] { dsd }, options);

                Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "expected T4 generator to generate code");
                TestHelper.AssertGeneratedCodeContains(generatedCode, T4DomainServiceClientCodeGenerator.GeneratedBoilerPlate);
                TestHelper.AssertGeneratedCodeContains(generatedCode, "public class DispatcherEntity : Entity");
                TestHelper.AssertNoErrorsOrWarnings(logger);
            }
        }

        [Description("ClientCodeGenerationDispatcher finds custom generator by assembly qualified name and can generate code")]
        [TestMethod]
        public void ClientCodeGenerationDispatcher_Custom_By_AssemblyQualifiedName()
        {
            ConsoleLogger logger = new ConsoleLogger();
            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions()
            {
                Language = "C#"
            };
            ICodeGenerationHost host = TestHelper.CreateMockCodeGenerationHost(logger, /*sharedTypeService*/ null);

            // Create a new dispatcher and call an internal extensibility point to add ourselves
            // into the MEF composition container
            using (ClientCodeGenerationDispatcher dispatcher = new ClientCodeGenerationDispatcher())
            {
                string[] compositionAssemblies = Array.Empty<string>();

                IDomainServiceClientCodeGenerator generator = dispatcher.FindCodeGenerator(host, options, compositionAssemblies, typeof(T4DomainServiceClientCodeGenerator).AssemblyQualifiedName);
                Assert.IsNotNull(generator, "the dispatcher did not find the code generator");
                Assert.AreEqual(generator.GetType(), typeof(T4DomainServiceClientCodeGenerator), "dispatcher found " + generator.GetType() + " but should have found T4DomainServiceClientProxyGenerator");

                string generatedCode = generator.GenerateCode(host, Enumerable.Empty<DomainServiceDescription>(), options);

                Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "expected T4 generator to generate code");
                TestHelper.AssertGeneratedCodeContains(generatedCode, T4DomainServiceClientCodeGenerator.GeneratedBoilerPlate);
                TestHelper.AssertNoErrorsOrWarnings(logger);
            }
        }

        [Description("ClientCodeGenerationDispatcher logs warning if assembly qualified name is wrong type")]
        [TestMethod]
        public void ClientCodeGenerationDispatcher_Custom_By_AssemblyQualifiedName_Wrong_Type()
        {
            ConsoleLogger logger = new ConsoleLogger();
            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions()
            {
                Language = "C#"
            };

            ICodeGenerationHost host = TestHelper.CreateMockCodeGenerationHost(logger, /*sharedTypeService*/ null);

            string codeGeneratorName = typeof(string).AssemblyQualifiedName;

            // Create a new dispatcher and call an internal extensibility point to add ourselves
            // into the MEF composition container
            using (ClientCodeGenerationDispatcher dispatcher = new ClientCodeGenerationDispatcher())
            {
                string[] compositionAssemblies = Array.Empty<string>();

                IDomainServiceClientCodeGenerator generator = dispatcher.FindCodeGenerator(host, options, compositionAssemblies, codeGeneratorName);
                Assert.IsNull(generator, "the dispatcher should not find the code generator");
                string error = string.Format(CultureInfo.CurrentCulture, Resource.Code_Generator_Incorrect_Type, codeGeneratorName);
                TestHelper.AssertContainsWarnings(logger, error);
            }
        }

        [Description("ClientCodeGenerationDispatcher logs error if generator loaded via assembly qualified name throws during instantiation")]
        [TestMethod]
        public void ClientCodeGenerationDispatcher_Custom_By_AssemblyQualifiedName_Ctor_Throws()
        {
            ConsoleLogger logger = new ConsoleLogger();
            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions()
            {
                Language = "C#"
            };
            ICodeGenerationHost host = TestHelper.CreateMockCodeGenerationHost(logger, /*sharedTypeService*/ null);

            string codeGeneratorName = typeof(ThrowingCtorCodeGenerator).AssemblyQualifiedName;

            // Create a new dispatcher and call an internal extensibility point to add ourselves
            // into the MEF composition container
            using (ClientCodeGenerationDispatcher dispatcher = new ClientCodeGenerationDispatcher())
            {
                string[] compositionAssemblies = Array.Empty<string>();

                IDomainServiceClientCodeGenerator generator = dispatcher.FindCodeGenerator(host, options, compositionAssemblies, codeGeneratorName);
                Assert.IsNull(generator, "the dispatcher should not find the code generator");
                string error = string.Format(CultureInfo.CurrentCulture, Resource.Code_Generator_Instantiation_Error, codeGeneratorName, ThrowingCtorCodeGenerator.ErrorMessage);
                TestHelper.AssertContainsErrors(logger, error);
            }
        }



        [Description("ClientCodeGenerationDispatcher logs error if generator throws during code generation, uses logical name")]
        [TestMethod]
        public void ClientCodeGenerationDispatcher_Throws_Exception_Logical_Name()
        {
            ConsoleLogger logger = new ConsoleLogger();
            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions()
            {
                Language = "C#",
                ClientProjectPath = "SampleProject.csproj"
            };

            ICodeGenerationHost host = TestHelper.CreateMockCodeGenerationHost(logger, /*sharedTypeService*/ null);

            using (ClientCodeGenerationDispatcher dispatcher = new ClientCodeGenerationDispatcher())
            {
                string[] compositionAssemblies = new string[] { Assembly.GetExecutingAssembly().Location };

                string codeGeneratorName = MockCodeGenerator.GeneratorName;

                // Ask our mock to throw
                MockCodeGenerator.ThrowException = true;

                dispatcher.GenerateCode(host, options, Enumerable.Empty<Type>(), compositionAssemblies, codeGeneratorName);
                string error = string.Format(CultureInfo.CurrentCulture, Resource.CodeGenerator_Threw_Exception, codeGeneratorName, options.ClientProjectPath, MockCodeGenerator.Exception.Message);
                TestHelper.AssertContainsErrors(logger, error);
            }
        }

        [Description("ClientCodeGenerationDispatcher logs error if generator throws during code generation, uses fully qualified name")]
        [TestMethod]
        public void ClientCodeGenerationDispatcher_Throws_Exception_Fully_Qualified_Name()
        {
            ConsoleLogger logger = new ConsoleLogger();
            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions()
            {
                Language = "C#",
                ClientProjectPath = "SampleProject.csproj"
            };

            ICodeGenerationHost host = TestHelper.CreateMockCodeGenerationHost(logger, /*sharedTypeService*/ null);

            using (ClientCodeGenerationDispatcher dispatcher = new ClientCodeGenerationDispatcher())
            {
                // Disable MEF for this test
                string[] compositionAssemblies = Array.Empty<string>();

                // And use FQN instead
                string codeGeneratorName = typeof(MockCodeGenerator).AssemblyQualifiedName;

                // Ask our mock to throw
                MockCodeGenerator.ThrowException = true;

                dispatcher.GenerateCode(host, options, Enumerable.Empty<Type>(), compositionAssemblies, codeGeneratorName);
                string error = string.Format(CultureInfo.CurrentCulture, Resource.CodeGenerator_Threw_Exception, codeGeneratorName, options.ClientProjectPath, MockCodeGenerator.Exception.Message);
                TestHelper.AssertContainsErrors(logger, error);
            }
        }

        [Description("ClientCodeGenerationDispatcher logs warning when multiple generators exist")]
        [TestMethod]
        public void ClientCodeGenerationDispatcher_Error_Multiple_Generators()
        {
            ConsoleLogger logger = new ConsoleLogger();
            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions()
            {
                Language = "C#",
                ClientProjectPath = "ClientProject"
            };

            ICodeGenerationHost host = TestHelper.CreateMockCodeGenerationHost(logger, /*sharedTypeService*/ null);

            // Create a new dispatcher and call an internal extensibility point to add ourselves
            // into the MEF composition container
            using (ClientCodeGenerationDispatcher dispatcher = new ClientCodeGenerationDispatcher())
            {
                string[] compositionAssemblies = new string[] { Assembly.GetExecutingAssembly().Location, typeof(T4DomainServiceClientCodeGenerator).Assembly.Location };

                IDomainServiceClientCodeGenerator generator = dispatcher.FindCodeGenerator(host, options, compositionAssemblies, /*generatorName*/ null);
                Assert.IsNotNull(generator, "the dispatcher did not pick a generator");
                Assert.IsTrue(generator is CodeDomClientCodeGenerator, "the dispatcher did not pick the default code generator");

                string errorParam = "    " + MockCodeGenerator.GeneratorName + Environment.NewLine +
                                    "    " + T4DomainServiceClientCodeGenerator.GeneratorName + Environment.NewLine;

                string error = (string.Format(CultureInfo.CurrentCulture, 
                                                Resource.Multiple_Custom_Code_Generators_Using_Default, 
                                                options.Language, 
                                                errorParam, 
                                                options.ClientProjectPath, 
                                                MockCodeGenerator.GeneratorName,
                                                CodeDomClientCodeGenerator.GeneratorName));
                TestHelper.AssertContainsWarnings(logger, error);
            }
        }

        [Description("ClientCodeGenerationDispatcher logs error when multiple generators exist with the same name")]
        [TestMethod]
        public void ClientCodeGenerationDispatcher_Error_Multiple_Generators_Same_Name()
        {
            ConsoleLogger logger = new ConsoleLogger();
            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions()
            {
                Language = "F#",
                ClientProjectPath = "ClientProject",
                ServerProjectPath = "ServerProject"
            };

            ICodeGenerationHost host = TestHelper.CreateMockCodeGenerationHost(logger, /*sharedTypeService*/ null);

            // Create a new dispatcher and call an internal extensibility point to add ourselves
            // into the MEF composition container
            using (ClientCodeGenerationDispatcher dispatcher = new ClientCodeGenerationDispatcher())
            {
                string[] compositionAssemblies = new string[] { Assembly.GetExecutingAssembly().Location };

                string generatorName = MockFSharpCodeGenerator1.GeneratorName;

                IDomainServiceClientCodeGenerator generator = dispatcher.FindCodeGenerator(host, options, compositionAssemblies, generatorName);
                Assert.IsNull(generator, "the dispatcher should not pick a generator");

                string errorParam = "    " + typeof(MockFSharpCodeGenerator1).FullName + Environment.NewLine +
                                    "    " + typeof(MockFSharpCodeGenerator2).FullName + Environment.NewLine;

                string error = string.Format(CultureInfo.CurrentCulture, 
                                                        Resource.Multiple_Named_Code_Generators, 
                                                        generatorName,
                                                        options.Language, 
                                                        errorParam,
                                                        options.ServerProjectPath, 
                                                        options.ClientProjectPath,
                                                        typeof(MockFSharpCodeGenerator1).AssemblyQualifiedName);
                TestHelper.AssertContainsErrors(logger, error);
            }
        }

        [Description("ClientCodeGenerationDispatcher logs an warning and survives a TypeLoadException creating MEF")]
        [TestMethod]
        [DeploymentItem(@"TypeLoadExceptionProject.dll")]
        public void ClientCodeGenerationDispatcher_Error_TypeLoadException()
        {
            ConsoleLogger logger = new ConsoleLogger();
            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions()
            {
                Language = "C#",
            };

            ICodeGenerationHost host = TestHelper.CreateMockCodeGenerationHost(logger, /*sharedTypeService*/ null);

            // Create a new dispatcher and call an internal extensibility point to add ourselves
            // into the MEF composition container
            using (ClientCodeGenerationDispatcher dispatcher = new ClientCodeGenerationDispatcher())
            {
                // We want to include into the MEF container an assembly that will throw TypeLoadException
                // when MEF tries to analyze it.  This is to test our own recovery, which should consist
                // of logging an error making a default container containing only Tools.
                string unitTestAssemblyLocation = Assembly.GetExecutingAssembly().Location;
                string typeLoadExceptionProjectLocation = Path.Combine(Path.GetDirectoryName(unitTestAssemblyLocation), "TypeLoadExceptionProject.dll");

                Assert.IsTrue(File.Exists(typeLoadExceptionProjectLocation), "Expected TypeLoadExceptionProject.dll to coreside with this assembly in test folder");

                // Do what MEF does to load the types so we can capture the exception
                Exception expectedException = null;
                try
                {
                    Assembly assembly = Assembly.LoadFrom(typeLoadExceptionProjectLocation);
                    assembly.GetTypes();
                }
                catch (Exception ex)
                {
                    expectedException = ex;
                }
                Assert.IsNotNull(expectedException, "We did not generate the type load exception we expected");

                string[] compositionAssemblies = new string[] { unitTestAssemblyLocation, typeLoadExceptionProjectLocation };

                IDomainServiceClientCodeGenerator generator = dispatcher.FindCodeGenerator(host, options, compositionAssemblies, /*generatorName*/ null);
                Assert.IsNotNull(generator, "the dispatcher did not pick default generator");
                Assert.IsTrue(generator is CodeDomClientCodeGenerator, "the dispatcher did not pick the default code generator");

                string error = (string.Format(CultureInfo.CurrentCulture,
                                                Resource.Failed_To_Create_Composition_Container,
                                                expectedException.Message));
                TestHelper.AssertContainsWarnings(logger, error);
            }
        }
    }

    [DomainServiceClientCodeGenerator(MockCodeGenerator.GeneratorName, "C#")]
    public class MockCodeGenerator : IDomainServiceClientCodeGenerator
    {
        public const string GeneratorName = "MockCodeGenerator";
        public const string FakeGeneratedCode = "MockCodeGenerator generated this fake code";

        public static Exception Exception = new InvalidOperationException("MockCodeGenerator threw this.");

        // The packet pushed through LogError on demand
        public static readonly ConsoleLogger.LogPacket ErrorPacket = new ConsoleLogger.LogPacket()
        {
            Message = "ErrMsg",
            Subcategory = "ErrSubcat",
            ErrorCode = "ErrErr",
            HelpString = "ErrHelp",
            File = "ErrFile",
            LineNumber = 1,
            ColumnNumber = 2,
            EndLineNumber = 3,
            EndColumnNumber = 4
        };

        // The packet pushed through LogWarning on demand
        public static readonly ConsoleLogger.LogPacket WarningPacket = new ConsoleLogger.LogPacket()
        {
            Message = "WarnMsg",
            Subcategory = "WarnSubcat",
            ErrorCode = "WarnErr",
            HelpString = "WarnHelp",
            File = "WarnFile",
            LineNumber = 1,
            ColumnNumber = 2,
            EndLineNumber = 3,
            EndColumnNumber = 4
        };

        public static bool LogWarningsFull { get; set; }
        public static bool LogErrorsFull { get; set; }
        public static bool ThrowException { get; set; }

        public string GenerateCode(ICodeGenerationHost host, IEnumerable<DomainServiceDescription> descriptions, ClientCodeGenerationOptions options)
        {
            Assert.IsNotNull(host, "host cannot be null when code generator is called");
            Assert.IsNotNull(options, "options cannot be null when code generator is called");
            Assert.IsNotNull(descriptions, "descriptions cannot be null when code generator is called");

            // These 2 test helpers reset each time they are read
            bool logWarningsFull = MockCodeGenerator.LogWarningsFull;
            MockCodeGenerator.LogWarningsFull = false;

            bool logErrorsFull = MockCodeGenerator.LogErrorsFull;
            MockCodeGenerator.LogErrorsFull = false;

            bool throwException = MockCodeGenerator.ThrowException;
            MockCodeGenerator.ThrowException = false;

            if (throwException)
            {
                throw MockCodeGenerator.Exception;
            }

            if (logWarningsFull)
            {
                ConsoleLogger.LogPacket p = MockCodeGenerator.WarningPacket;
                host.LogWarning(p.Message, p.Subcategory, p.ErrorCode, p.HelpString, p.File, p.LineNumber, p.ColumnNumber, p.EndLineNumber, p.EndColumnNumber);
            }
            if (logErrorsFull)
            {
                ConsoleLogger.LogPacket p = MockCodeGenerator.ErrorPacket;
                host.LogError(p.Message, p.Subcategory, p.ErrorCode, p.HelpString, p.File, p.LineNumber, p.ColumnNumber, p.EndLineNumber, p.EndColumnNumber);
            }

            return MockCodeGenerator.FakeGeneratedCode;
        }
    }
    
    // Commented out so MEF does not discover this generator.
    // It is used exclusively for testing fully qualified assembly name loading of generators.
    // [DomainServiceClientCodeGenerator("ThrowingCtorCodeGenerator", "C#")]
    public class ThrowingCtorCodeGenerator : IDomainServiceClientCodeGenerator
    {
        public static string ErrorMessage = "Throws in ctor to demonstrate recovery";
        public ThrowingCtorCodeGenerator()
        {
            throw new InvalidOperationException(ThrowingCtorCodeGenerator.ErrorMessage);
        }

        public string GenerateCode(ICodeGenerationHost codeGenerationHost, IEnumerable<DomainServiceDescription> domainServiceDescriptions, ClientCodeGenerationOptions options)
        {
            throw new NotImplementedException();
        }
    }

    #region generators with same name
    // The following 2 custom generators are used to test multiple generators with the same name
    [DomainServiceClientCodeGenerator(MockFSharpCodeGenerator1.GeneratorName, "F#")]
    public class MockFSharpCodeGenerator1 : IDomainServiceClientCodeGenerator
    {
        public const string GeneratorName = "MockF#Generator";

        public string GenerateCode(ICodeGenerationHost codeGenerationHost, IEnumerable<DomainServiceDescription> domainServiceDescriptions, ClientCodeGenerationOptions options)
        {
            throw new NotImplementedException();
        }
    }
    [DomainServiceClientCodeGenerator(MockFSharpCodeGenerator1.GeneratorName, "F#")]
    public class MockFSharpCodeGenerator2 : IDomainServiceClientCodeGenerator
    {
        public string GenerateCode(ICodeGenerationHost codeGenerationHost, IEnumerable<DomainServiceDescription> domainServiceDescriptions, ClientCodeGenerationOptions options)
        {
            throw new NotImplementedException();
        }
    }
    #endregion


    #region subclass tests
    // The following 2 custom generators are used to test subclasses.
    // The base does not export and should not be found
    public class MockGSharpCodeGeneratorBase : IDomainServiceClientCodeGenerator
    {
        public const string GeneratedCode = "Base generated code";

        public virtual string GenerateCode(ICodeGenerationHost codeGenerationHost, IEnumerable<DomainServiceDescription> domainServiceDescriptions, ClientCodeGenerationOptions options)
        {
            return MockGSharpCodeGeneratorBase.GeneratedCode;
        }
    }

    [DomainServiceClientCodeGenerator(MockGSharpCodeGeneratorDerived.GeneratorName, "G#")]
    public class MockGSharpCodeGeneratorDerived : MockGSharpCodeGeneratorBase
    {
        public const string GeneratorName = "MockG#Generator";
        public const string DerivedGeneratedCode = "Derived generated code";

        public override string GenerateCode(ICodeGenerationHost codeGenerationHost, IEnumerable<DomainServiceDescription> domainServiceDescriptions, ClientCodeGenerationOptions options)
        {
            return MockGSharpCodeGeneratorDerived.DerivedGeneratedCode;
        }
    }
    #endregion

    [EnableClientAccess]
    public class DispatcherDomainService : DomainService
    {
        public IQueryable<DispatcherEntity> GetTheEntities() { return null; }
    }
    public class DispatcherEntity
    {
        [Key]
        public string TheKey { get; set; }
    }
}
