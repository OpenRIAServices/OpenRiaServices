extern alias SystemWebDomainServices;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using TypeUtility = SystemWebDomainServices::OpenRiaServices.DomainServices.TypeUtility;

namespace OpenRiaServices.DomainServices.Tools.Test
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
            Assembly domainServices = typeof(OpenRiaServices.DomainServices.Server.DomainService).Assembly;
            Assembly dataAnnotations = typeof(System.ComponentModel.DataAnnotations.DisplayAttribute).Assembly;
            Assembly excutingAssembly = Assembly.GetExecutingAssembly();

            Assert.IsTrue(TypeUtility.IsSystemAssembly(mscorlib), "mscorlib");
            Assert.IsTrue(TypeUtility.IsSystemAssembly(system), "system");
            Assert.IsTrue(TypeUtility.IsSystemAssembly(systemCore), "systemCore");
            Assert.IsTrue(TypeUtility.IsSystemAssembly(domainServices), "domainServices");
            Assert.IsTrue(TypeUtility.IsSystemAssembly(dataAnnotations), "dataAnnotations");
            Assert.IsFalse(TypeUtility.IsSystemAssembly(excutingAssembly), "Executing Assembly");
        }

        [TestMethod]
        [WorkItem(810123)]
        [Description("Verifies that only mscorlib is reported as the MsCorlib assembly")]
        public void IsAssemblyMsCorlibTest()
        {
            AssemblyName mscorlib = typeof(object).Assembly.GetName();
            AssemblyName system = typeof(System.Uri).Assembly.GetName();
            AssemblyName systemCore = typeof(System.Linq.IQueryable<>).Assembly.GetName();
            AssemblyName domainServices = typeof(OpenRiaServices.DomainServices.Server.DomainService).Assembly.GetName();
            AssemblyName dataAnnotations = typeof(System.ComponentModel.DataAnnotations.DisplayAttribute).Assembly.GetName();
            AssemblyName executingAssembly = Assembly.GetExecutingAssembly().GetName();

            Assert.IsTrue(AssemblyUtilities.IsAssemblyMsCorlib(mscorlib), "mscorlib");
            Assert.IsFalse(AssemblyUtilities.IsAssemblyMsCorlib(system), "system");
            Assert.IsFalse(AssemblyUtilities.IsAssemblyMsCorlib(systemCore), "systemCore");
            Assert.IsFalse(AssemblyUtilities.IsAssemblyMsCorlib(domainServices), "domainServices");
            Assert.IsFalse(AssemblyUtilities.IsAssemblyMsCorlib(dataAnnotations), "dataAnnotations");
            Assert.IsFalse(AssemblyUtilities.IsAssemblyMsCorlib(executingAssembly), "Executing Assembly");
        }
    }
}
