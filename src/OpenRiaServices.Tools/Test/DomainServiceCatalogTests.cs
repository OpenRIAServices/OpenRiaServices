using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenRiaServices.Client.Test;
using OpenRiaServices.Server;
using OpenRiaServices.Server.Test.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices;

namespace OpenRiaServices.Tools.Test
{
    /// <summary>
    /// Summary description for domain service catalog
    /// </summary>
    [TestClass]
    public class DomainServiceCatalogTests
    {
        public DomainServiceCatalogTests()
        {
        }

        [TestMethod]
        [Description("DomainServiceCatalog ctors work properly")]
        public void DomainServiceCatalog_Ctors()
        {
            IEnumerable<string> empty = Array.Empty<string>();
            ConsoleLogger logger = new ConsoleLogger();

            // Ctor taking assemblies -- null arg tests
            ExceptionHelper.ExpectArgumentNullExceptionStandard(() => new DomainServiceCatalog((IEnumerable<string>)null, logger), "assembliesToLoad");
            ExceptionHelper.ExpectArgumentNullExceptionStandard(() => new DomainServiceCatalog(empty, null), "logger");

            // Ctor taking one type -- null arg tests
            ExceptionHelper.ExpectArgumentNullExceptionStandard(() => new DomainServiceCatalog((Type) null, logger), "domainServiceType");
            ExceptionHelper.ExpectArgumentNullExceptionStandard(() => new DomainServiceCatalog(typeof(DSC_DomainServiceType), null), "logger");

            // Ctor taking multiple types -- null arg tests
            ExceptionHelper.ExpectArgumentNullExceptionStandard(() => new DomainServiceCatalog((IEnumerable<Type>)null, logger), "domainServiceTypes");
            ExceptionHelper.ExpectArgumentNullExceptionStandard(() => new DomainServiceCatalog(new Type[] {typeof(DSC_DomainServiceType)}, null), "logger");

            // Ctor taking assemblies -- legit
            string[] realAssemblies = new string[] { this.GetType().Assembly.Location,
                                                     typeof(string).Assembly.Location };

            // Assembly based ctors are tested more deeply in other test methods

            // Ctor taking one type -- legit
            DomainServiceCatalog dsc = new DomainServiceCatalog(typeof(DSC_DomainServiceType), logger);
            IEnumerable<DomainServiceDescription> descriptions = dsc.DomainServiceDescriptions;
            Assert.IsNotNull(descriptions, "Did not expect null descriptions");
            Assert.AreEqual(1, descriptions.Count(), "Expected exactly one domain service description");

            // Ctor taking multiple type -- legit
            dsc = new DomainServiceCatalog(new Type[] {typeof(DSC_DomainServiceType)}, logger);
            descriptions = dsc.DomainServiceDescriptions;
            Assert.IsNotNull(descriptions, "Did not expect null descriptions");
            Assert.AreEqual(1, descriptions.Count(), "Expected exactly one domain service description");
        }

        [TestMethod]
        [Description("DomainServiceCatalog finds all DomainService subtypes")]
        public void DomainServiceCatalog_Finds_All_DomainServices()
        {
            ConsoleLogger logger = new ConsoleLogger();
            List<string> assemblies = new List<string>();

            // Add our current unit test assembly to those to load
            assemblies.Add(this.GetType().Assembly.Location);

            int expectedDomainServices = 0;
            foreach (Type t in this.GetType().Assembly.GetExportedTypes())
            {
                if (IsDomainService(t))
                {
                    ++expectedDomainServices;
                }
            }

            // Add all our assy references and also count any DomainServices there (don't expect any)
            foreach (AssemblyName an in this.GetType().Assembly.GetReferencedAssemblies())
            {
                Assembly a = Assembly.Load(an);
                assemblies.Add(a.Location);
                foreach (Type t in a.GetExportedTypes())
                {
                    if (IsDomainService(t))
                    {
                        ++expectedDomainServices;
                    }
                }
            }
#if NET6_0_OR_GREATER
            using(var dispatcher = new ClientCodeGenerationDispatcher())
            {
                DomainServiceCatalog dsc = new DomainServiceCatalog(assemblies, logger, dispatcher);
                ICollection<DomainServiceDescription> descriptions = dsc.DomainServiceDescriptions;
                Assert.IsNotNull(descriptions);
                Assert.IsTrue(descriptions.Count >= expectedDomainServices);
            }
#else
            DomainServiceCatalog dsc = new DomainServiceCatalog(assemblies, logger);
            ICollection<DomainServiceDescription> descriptions = dsc.DomainServiceDescriptions;
            Assert.IsNotNull(descriptions);
            Assert.IsTrue(descriptions.Count >= expectedDomainServices);
#endif
        }

        [TestMethod]
        [Description("DomainServiceCatalog catches FileNotFoundException and emits an info message")]
        public void DomainServiceCatalog_Message_FileNotFound()
        {
            string assemblyFileName = @"c:\Nowhere\DontExist.dll";
            ConsoleLogger logger = new ConsoleLogger();
            DomainServiceCatalog dsc = new DomainServiceCatalog(new string[] { assemblyFileName }, logger);
            ICollection<DomainServiceDescription> descriptions = dsc.DomainServiceDescriptions;
            Assert.IsNotNull(descriptions);
            Assert.AreEqual(0, descriptions.Count);
            Assert.AreEqual(0, logger.ErrorMessages.Count);
            Assert.AreEqual(0, logger.WarningMessages.Count);

            // Need to synthesize exactly the same message we'd expect from failed assembly load
            string exceptionMessage = null;
            try
            {
                AssemblyName.GetAssemblyName(assemblyFileName);
            }
            catch (FileNotFoundException fnfe)
            {
                exceptionMessage = fnfe.Message;
            }
            string expectedMessage = string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Assembly_Load_Error, assemblyFileName, exceptionMessage);
            TestHelper.AssertContainsMessages(logger, expectedMessage);
        }

        [TestMethod]
        [Description("DomainServiceCatalog catches FileNotFoundException and emits an info message but continues processing")]
        public void DomainServiceCatalog_Message_FileNotFound_Continues()
        {
            string assemblyFileName = @"c:\Nowhere\DontExist.dll";

            ConsoleLogger logger = new ConsoleLogger();
            IEnumerable<string> assemblies = new string[] { assemblyFileName, this.GetType().Assembly.Location };
            DomainServiceCatalog dsc = new DomainServiceCatalog(assemblies, logger);
            ICollection<DomainServiceDescription> descriptions = dsc.DomainServiceDescriptions;
            Assert.IsNotNull(descriptions);
            
            // Need to synthesize exactly the same message we'd expect from failed assembly load
            string exceptionMessage = null;
            try
            {
                AssemblyName.GetAssemblyName(assemblyFileName);
            }
            catch (FileNotFoundException fnfe)
            {
                exceptionMessage = fnfe.Message;
            }
            string expectedMessage = string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Assembly_Load_Error, assemblyFileName, exceptionMessage);
            TestHelper.AssertContainsMessages(logger, expectedMessage); 
            
            Assert.IsTrue(descriptions.Count > 0);
        }

        [TestMethod]
        [Description("DomainServiceCatalog catches BadImageFormatException and emits an info message")]
        public void DomainServiceCatalog_Message_BadImageFormat()
        {
            // Create fake DLL with bad image 
            string assemblyFileName = Path.Combine(Path.GetTempPath(), (Guid.NewGuid().ToString() + ".dll"));
            File.WriteAllText(assemblyFileName, "neener neener neener");

            ConsoleLogger logger = new ConsoleLogger();
#if NET6_0_OR_GREATER
            var dispatcher = new ClientCodeGenerationDispatcher();
            DomainServiceCatalog dsc = new DomainServiceCatalog(new string[] { assemblyFileName }, logger, dispatcher);
#else
            DomainServiceCatalog dsc = new DomainServiceCatalog(new string[] { assemblyFileName }, logger);
#endif
            ICollection<DomainServiceDescription> descriptions = dsc.DomainServiceDescriptions;
            Assert.IsNotNull(descriptions);
            Assert.AreEqual(0, descriptions.Count);
            Assert.AreEqual(0, logger.ErrorMessages.Count);
            Assert.AreEqual(0, logger.WarningMessages.Count);

            // Need to synthesize exactly the same message we'd expect from failed assembly load
            string exceptionMessage = null;
            try
            {
                AssemblyName.GetAssemblyName(assemblyFileName);
            }
            catch (BadImageFormatException bife)
            {
                exceptionMessage = bife.Message;
            }
            finally
            {
                File.Delete(assemblyFileName);
            }
            string expectedMessage = string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Assembly_Load_Error, assemblyFileName, string.Empty).TrimEnd();
            Assert.IsTrue(logger.InfoMessages.Any(message => message.StartsWith(expectedMessage)));
        }

        /// <summary>
        /// Returns true if the given type is a DomainService
        /// </summary>
        /// <param name="t">The type to test</param>
        /// <returns><c>true</c> if it is a DomainService type</returns>
        private static bool IsDomainService(Type t)
        {
            if (t == null || t.IsAbstract || t.IsGenericTypeDefinition)
            {
                return false;
            }

            if (!typeof(DomainService).IsAssignableFrom(t))
            {
                return false;
            }

            object[] attrs = t.GetCustomAttributes(typeof(EnableClientAccessAttribute), false);

            return attrs.Length > 0;
        }
    }

    [EnableClientAccess]
    public class DSC_DomainServiceType : DomainService
    {
        public IQueryable<DSC_Entity> GetEntities() { return null; }
    }
    public class DSC_Entity
    {
       [Key] public string TheKey {get;set;}
    }
}
