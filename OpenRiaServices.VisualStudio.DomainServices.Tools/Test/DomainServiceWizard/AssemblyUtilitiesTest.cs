using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using Microsoft.VisualStudio.ServiceModel.DomainServices.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Web.Configuration;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace Microsoft.AppFx.UnitTests.Setup.Wizards
{
    [TestClass]
    public class AssemblyUtilityTests
    {

        [TestMethod]
        [Description("Successfully loads assembly from file")]
        public void AssemblyUtilities_LoadFromFile()
        {
            Assembly a = AssemblyUtilities.LoadAssembly(this.GetType().Assembly.Location, null);
            Assert.IsNotNull(a, "Failed to load assembly from file");
        }

        [TestMethod]
        [Description("Successfully loads assembly from name")]
        public void AssemblyUtilities_LoadFromName()
        {
            AssemblyName name = typeof(System.Xml.XmlNode).Assembly.GetName();
            Assembly a = AssemblyUtilities.LoadAssembly(name, null);
            Assert.IsNotNull(a, "Failed to load assembly by name");
        }

        [TestMethod]
        [Description("Attempting to load bogus assembly logs failure")]
        public void AssemblyUtilities_Bad_Name_Logs()
        {
            string logString = null;
            string expectedErrorMessage = null;
            string fileName = this.GetType().Assembly.Location + "x";
            AssemblyName asmName = null;
            try
            {
                asmName = AssemblyName.GetAssemblyName(fileName);
                Assembly.Load(asmName);
            }
            catch (Exception ex)
            {
                expectedErrorMessage = string.Format(CultureInfo.CurrentCulture, Resources.BusinessLogicClass_Failed_Load, fileName, ex.Message); 
            }

            Assembly a = AssemblyUtilities.LoadAssembly(fileName, (s) => logString = s);
            Assert.IsNull(a, "Should have failed to load assembly by name");
            Assert.IsNotNull(logString, "logger was not called");
            Assert.AreEqual(expectedErrorMessage, logString, "unexpected error message");

            // Verify a null logger does not throw
            a = AssemblyUtilities.LoadAssembly(fileName, null);
            Assert.IsNull(a, "Should have failed to load assembly by name");
        }
    }
}