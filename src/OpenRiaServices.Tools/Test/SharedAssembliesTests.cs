using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenRiaServices.Server;
using OpenRiaServices.Server.Test.Utilities;
using OpenRiaServices.Tools.SharedTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServerClassLib;

namespace OpenRiaServices.Tools.Test
{
    /// <summary>
    /// Tests for SharedAssembliesManaged service
    /// </summary>
    [TestClass]
    public class SharedAssembliesTests
    {
        internal virtual SharedAssemblies CreatedSharedAssembliesService(IEnumerable<string> assemblies, IEnumerable<string> assemblySearchPats, ILogger logger)
        {
            return new SharedAssemblies(assemblies, assemblySearchPats, logger);
        }

        [Description("SharedAssembliesManaged service locates shared types between projects")]
        [TestMethod]
        public void SharedAssembliesManaged_Types()
        {
            string projectPath = null;
            string outputPath = null;
            TestHelper.GetProjectPaths("", out projectPath, out outputPath);
            string clientProjectPath = CodeGenHelper.ClientClassLibProjectPath(projectPath);
            List<string> assemblies = CodeGenHelper.ClientClassLibReferences(clientProjectPath, true);

            ConsoleLogger logger = new ConsoleLogger();
            using (var sa = CreatedSharedAssembliesService(assemblies, CodeGenHelper.GetClientAssemblyPaths(), logger))
            {


                string sharedTypeLocation = GetSharedTypeLocation(sa, typeof(TestEntity));
                Assert.IsNotNull(sharedTypeLocation, "Expected TestEntity type to be shared");
                Assert.IsTrue(sharedTypeLocation.Contains("ClientClassLib"), "Expected to find type in client class lib");

                sharedTypeLocation = GetSharedTypeLocation(sa, typeof(TestValidator));
                Assert.IsNotNull(sharedTypeLocation, "Expected TestValidator type to be shared");
                Assert.IsTrue(sharedTypeLocation.Contains("ClientClassLib"), "Expected to find type in client class lib");

                sharedTypeLocation = GetSharedTypeLocation(sa, typeof(DomainService));
                Assert.IsNull(sharedTypeLocation, "Expected DomainService type not to be shared");

                sharedTypeLocation = GetSharedTypeLocation(sa, typeof(TestValidatorServer));
                Assert.IsNull(sharedTypeLocation, "Expected TestValidatorServer type not to be shared");
            }

            TestHelper.AssertNoErrorsOrWarnings(logger);
        }

        [Description("SharedAssembliesManaged service locates shared types between projects")]
        [TestMethod]
        public void SharedAssembliesManaged_Properties()
        {
            string projectPath = null;
            string outputPath = null;
            TestHelper.GetProjectPaths("", out projectPath, out outputPath);
            string clientProjectPath = CodeGenHelper.ClientClassLibProjectPath(projectPath);
            List<string> assemblies = CodeGenHelper.ClientClassLibReferences(clientProjectPath, true);

            ConsoleLogger logger = new ConsoleLogger();
            using (var sa = CreatedSharedAssembliesService(assemblies, CodeGenHelper.GetClientAssemblyPaths(), logger))
            {
                string sharedTypeLocation = GetSharedPropertyLocation(sa, typeof(TestEntity), nameof(TestEntity.ClientAndServerValue));
                Assert.IsNotNull(sharedTypeLocation, "Expected TestEntity type to be shared");
                Assert.IsTrue(sharedTypeLocation.Contains("ClientClassLib"), "Expected to find type in client class lib");

                sharedTypeLocation = GetSharedPropertyLocation(sa, typeof(TestEntity), nameof(TestEntity.ServerAndClientValue));
                Assert.IsNotNull(sharedTypeLocation, "Expected TestEntity type to be shared");
                Assert.IsTrue(sharedTypeLocation.Contains("ClientClassLib"), "Expected to find type in client class lib");

                sharedTypeLocation = GetSharedPropertyLocation(sa, typeof(TestEntity), nameof(TestEntity.TheValue));
                Assert.IsNull(sharedTypeLocation, "Expected TestEntity.TheValue type to not be shared");

                // We should detect properties from derived types
                sharedTypeLocation = GetSharedPropertyLocation(sa, "ServerClassLib.TestDomainSharedContext", "ValidationContext");
                Assert.IsNotNull(sharedTypeLocation, "Expected to detect properties from base classes (DomainContext.ValidationContext)");
                StringAssert.Contains(sharedTypeLocation, "OpenRiaServices.Client");

                // We should not detect internal properties
                sharedTypeLocation = GetSharedPropertyLocation(sa, "OpenRiaServices.Client.Entity", "ValidationErrors");
                Assert.IsNotNull(sharedTypeLocation, "Should detect properties in other assemblies");
                StringAssert.Contains(sharedTypeLocation, "OpenRiaServices.Client");

                sharedTypeLocation = GetSharedPropertyLocation(sa, "OpenRiaServices.Client.Entity", "ParentAssociation");
                Assert.IsNull(sharedTypeLocation, "Expected to not detect internal properties");

                sharedTypeLocation = GetSharedPropertyLocation(sa, "OpenRiaServices.Client.Entity", "IsMergingState");
                Assert.IsNull(sharedTypeLocation, "Expected to not detect protected internal properties");
            }

            TestHelper.AssertNoErrorsOrWarnings(logger);
        }

        [Description("SharedAssembliesManaged service locates shared methods between projects")]
        [TestMethod]
        public void SharedAssembliesManaged_Methods()
        {
            string projectPath = null;
            string outputPath = null;
            TestHelper.GetProjectPaths("", out projectPath, out outputPath);
            string clientProjectPath = CodeGenHelper.ClientClassLibProjectPath(projectPath);
            List<string> assemblies = CodeGenHelper.ClientClassLibReferences(clientProjectPath, true);

            ConsoleLogger logger = new ConsoleLogger();
            using (var sa = CreatedSharedAssembliesService(assemblies, CodeGenHelper.GetClientAssemblyPaths(), logger))
            {
                var sharedMethodLocation = GetSharedMethodLocation(sa, typeof(TestValidator), "IsValid", new[] { typeof(TestEntity), typeof(ValidationContext) });
                Assert.IsNotNull(sharedMethodLocation, "Expected TestValidator.IsValid to be shared");
                Assert.IsTrue(sharedMethodLocation.Contains("ClientClassLib"), "Expected to find method in client class lib");

                sharedMethodLocation = GetSharedMethodLocation(sa, typeof(TestEntity), "ServerAndClientMethod", Type.EmptyTypes);
                Assert.IsNotNull(sharedMethodLocation, "Expected TestEntity.ServerAndClientMethod to be shared");
                Assert.IsTrue(sharedMethodLocation.Contains("ClientClassLib"), "Expected to find method in client class lib");

                sharedMethodLocation = GetSharedMethodLocation(sa, typeof(TestValidator), "ServertMethod", Type.EmptyTypes);
                Assert.IsNull(sharedMethodLocation, "Expected TestValidator.ServerMethod not to be shared");
            }

            TestHelper.AssertNoErrorsOrWarnings(logger);
        }

        [Description("SharedAssembliesManaged matches mscorlib types and methods")]
        [WorkItem(723391)]  // XElement entry below is regression for this
        [TestMethod]
        public void SharedAssembliesManaged_Matches_MsCorLib()
        {
            Type[] sharedTypes = new Type[] {
                typeof(Int32),
                typeof(string),
                typeof(Decimal),
                typeof(List<int>),
                typeof(Dictionary<int, string>)
            };

            Type[] nonSharedTypes = new Type[] {
                typeof(DomainService),
                typeof(List<DomainService>),
                typeof(DomainService[])
                // Below is fomr System.IO.Compression.Filesystem which is not referenced by client
                //typeof(System.Xml.Linq.XElement)
            };

            MethodBase[] sharedMethods = new MethodBase[] {
                // Must qualify argument since multiple CopyTo net 6
                typeof(string).GetMethod("CopyTo", new []{typeof(Int32), typeof(Char[]), typeof(Int32), typeof(Int32) }),
            };

            string projectPath = null;
            string outputPath = null;

            TestHelper.GetProjectPaths("", out projectPath, out outputPath);
            string clientProjectPath = CodeGenHelper.ClientClassLibProjectPath(projectPath);
            List<string> assemblies = CodeGenHelper.ClientClassLibReferences(clientProjectPath, true);

            ConsoleLogger logger = new ConsoleLogger();
            using (var sa = CreatedSharedAssembliesService(assemblies, CodeGenHelper.GetClientAssemblyPaths(), logger))
            {
                foreach (Type t in sharedTypes)
                {
                    var sharedTypeLocation = GetSharedTypeLocation(sa, t);
                    Assert.IsNotNull(sharedTypeLocation, "Expected type " + t.Name + " to be considered shared.");
                }

                foreach (MethodBase m in sharedMethods)
                {
                    Type[] parameterTypes = m.GetParameters().Select(p => p.ParameterType).ToArray();
                    var sharedMethodLocation = GetSharedMethodLocation(sa, m.DeclaringType, m.Name, parameterTypes);
                    Assert.IsNotNull(sharedMethodLocation, "Expected method " + m.DeclaringType.Name + "." + m.Name + " to be considered shared.");
                }

                foreach (Type t in nonSharedTypes)
                {
                    var sType = GetSharedTypeLocation(sa, t);
                    Assert.IsNull(sType, "Expected type " + t.Name + " to be considered *not* shared.");
                }
            }
        }

        [Description("SharedAssembliesManaged service logs an info message for nonexistent assembly file")]
        [TestMethod]
        public void SharedAssembliesManaged_Logs_Message_NonExistent_Assembly()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string file = "DoesNotExist.dll";
            using (var sa = CreatedSharedAssembliesService(new string[] { file }, CodeGenHelper.GetClientAssemblyPaths(), logger))
            {
                var sharedType = GetSharedTypeLocation(sa, typeof(TestEntity));
                Assert.IsNull(sharedType, "Should not have detected any shared type.");

                string message = string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Assembly_Load_Error, file, null);
                TestHelper.AssertHasInfoThatStartsWith(logger, message);
            }
        }

        [Description("SharedAssembliesManaged service logs an info message for bad image format assembly file")]
        [TestMethod]
        public void SharedAssembliesManaged_Logs_Message_BadImageFormat_Assembly()
        {
            // Create fake DLL with bad image 
            string assemblyFileName = Path.Combine(Path.GetTempPath(), (Guid.NewGuid().ToString() + ".dll"));
            File.WriteAllText(assemblyFileName, "neener neener neener");

            ConsoleLogger logger = new ConsoleLogger();

            using (var sa = CreatedSharedAssembliesService(new string[] { assemblyFileName }, CodeGenHelper.GetClientAssemblyPaths(), logger))
            {
                var sharedType = GetSharedTypeLocation(sa, typeof(TestEntity));
                Assert.IsNull(sharedType, "Should not have detected any shared type.");
            }

            string message = string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Assembly_Load_Error, assemblyFileName, null);
            TestHelper.AssertHasInfoThatStartsWith(logger, message);
        }

        private static string GetSharedTypeLocation(SharedAssemblies sa, Type type)
        {
            var key = CodeMemberKey.CreateTypeKey(type.FullName);
            return sa.GetSharedAssemblyPath(key);
        }

        private static string GetSharedMethodLocation(SharedAssemblies sa, Type type, string methodName, Type[] parameterTypes)
        {
            var key = CodeMemberKey.CreateMethodKey(type.AssemblyQualifiedName, methodName, parameterTypes.Select(t => t.AssemblyQualifiedName).ToArray());
            return sa.GetSharedAssemblyPath(key);
        }

        private static string GetSharedPropertyLocation(SharedAssemblies sa, Type type, string propertyName)
        {
            return GetSharedPropertyLocation(sa, type.FullName, propertyName);
        }

        private static string GetSharedPropertyLocation(SharedAssemblies sa, string fullName, string propertyName)
        {
            var key = CodeMemberKey.CreatePropertyKey(fullName, propertyName);
            return sa.GetSharedAssemblyPath(key);
        }
    }
}
