extern alias SystemWebDomainServices;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace OpenRiaServices.Tools.Test
{
    [TestClass]
    public class AssemblyUtilitiesTest
    {
        [TestMethod]
        [WorkItem(810123)]
        [Description("Verifies that common known system assemblies are reported as such, and others are not")]
        public void IsSystemAssemblyTest()
        {
            Assembly mscorlib = typeof(object).Assembly;
            Assembly system = typeof(System.Uri).Assembly;
            Assembly systemCore = typeof(System.Linq.IQueryable<>).Assembly;
            Assembly domainServices = typeof(OpenRiaServices.Server.DomainService).Assembly;
            Assembly dataAnnotations = typeof(System.ComponentModel.DataAnnotations.DisplayAttribute).Assembly;
            Assembly excutingAssembly = Assembly.GetExecutingAssembly();

            Assert.IsTrue(SystemWebDomainServices::OpenRiaServices.TypeUtility.IsSystemAssembly(mscorlib), "mscorlib");
            Assert.IsTrue(SystemWebDomainServices::OpenRiaServices.TypeUtility.IsSystemAssembly(system), "system");
            Assert.IsTrue(SystemWebDomainServices::OpenRiaServices.TypeUtility.IsSystemAssembly(systemCore), "systemCore");
            Assert.IsTrue(SystemWebDomainServices::OpenRiaServices.TypeUtility.IsSystemAssembly(domainServices), "domainServices");
            Assert.IsTrue(SystemWebDomainServices::OpenRiaServices.TypeUtility.IsSystemAssembly(dataAnnotations), "dataAnnotations");
            Assert.IsFalse(SystemWebDomainServices::OpenRiaServices.TypeUtility.IsSystemAssembly(excutingAssembly), "Executing Assembly");
        }

        [TestMethod]
        [WorkItem(810123)]
        [Description("Verifies that only mscorlib is reported as the MsCorlib assembly")]
        public void IsAssemblyMsCorlibTest()
        {
            AssemblyName mscorlib = typeof(object).Assembly.GetName();
            AssemblyName system = typeof(System.Uri).Assembly.GetName();
            AssemblyName systemCore = typeof(System.Linq.IQueryable<>).Assembly.GetName();
            AssemblyName domainServices = typeof(OpenRiaServices.Server.DomainService).Assembly.GetName();
            AssemblyName dataAnnotations = typeof(System.ComponentModel.DataAnnotations.DisplayAttribute).Assembly.GetName();
            AssemblyName executingAssembly = Assembly.GetExecutingAssembly().GetName();
            
            Assert.IsTrue(AssemblyUtilities.IsAssemblyMsCorlib(mscorlib), "mscorlib");
            Assert.IsFalse(AssemblyUtilities.IsAssemblyMsCorlib(systemCore), "systemCore");
            Assert.IsFalse(AssemblyUtilities.IsAssemblyMsCorlib(system), "system");
            Assert.IsFalse(AssemblyUtilities.IsAssemblyMsCorlib(domainServices), "domainServices");
            Assert.IsFalse(AssemblyUtilities.IsAssemblyMsCorlib(dataAnnotations), "dataAnnotations");
            Assert.IsFalse(AssemblyUtilities.IsAssemblyMsCorlib(executingAssembly), "Executing Assembly");
        }
    }
}
