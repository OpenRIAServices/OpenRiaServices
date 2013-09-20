using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel.DomainServices.Server.Test.Utilities;
using Microsoft.ServiceModel.DomainServices.Tools.SharedTypes;
using Microsoft.ServiceModel.DomainServices.Tools.SourceLocation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServerClassLib;

using Test.Microsoft.VisualStudio.ServiceModel.DomainServices.Intellisense;

namespace Microsoft.ServiceModel.DomainServices.Tools.Test
{
    /// <summary>
    /// Tests SourceInfoAttribute -- a dynamically generated type used in Live Intellisense
    /// to provide file positions for declared members.
    /// </summary>
    [TestClass]
    public class SourceInfoFileLocationTests
    {
        public SourceInfoFileLocationTests()
        {
        }

        [TestMethod]
        [Description("Valid mock SourceInfoAttribute can be located on types and members correctly")]
        public void SourceInfoAttribute_Valid_Usage()
        {
            // Ask for the base type's attributes
            object[] attributes = typeof(SourceInfoTestClass).GetCustomAttributes(false);
            SourceInfo sourceInfo = SourceInfo.GetSourceInfoFromAttributes(attributes);
            Assert.IsNotNull(sourceInfo, "Failed to get SourceInfo from type level attributes");
            Assert.AreEqual("typeLevelFile", sourceInfo.FileName, "Wrong FileName for type level sourceInfo");
            Assert.AreEqual(1, sourceInfo.Line, "Wrong Line for type level sourceInfo");
            Assert.AreEqual(2, sourceInfo.Column, "Wrong Column for type level sourceInfo");

            // And the derived type's attributes -- should *not* inherit
            attributes = typeof(SourceInfoDerivedTestClass).GetCustomAttributes(false);
            sourceInfo = SourceInfo.GetSourceInfoFromAttributes(attributes);
            Assert.IsNotNull(sourceInfo, "Failed to get SourceInfo from type level attributes");
            Assert.AreEqual("typeLevelFileDerived", sourceInfo.FileName, "Wrong FileName for type level sourceInfo");
            Assert.AreEqual(1, sourceInfo.Line, "Wrong Line for type level sourceInfo");
            Assert.AreEqual(2, sourceInfo.Column, "Wrong Column for type level sourceInfo");

            // Validate base ctor
            MethodBase methodBase = typeof(SourceInfoTestClass).GetConstructor(new Type[0]);
            Assert.IsNotNull(methodBase, "Could not find base ctor");
            attributes = methodBase.GetCustomAttributes(false);
            sourceInfo = SourceInfo.GetSourceInfoFromAttributes(attributes);
            Assert.IsNotNull(sourceInfo, "Failed to get SourceInfo from ctor level attributes");
            Assert.AreEqual("ctorLevelFile", sourceInfo.FileName, "Wrong FileName for type level sourceInfo");
            Assert.AreEqual(3, sourceInfo.Line, "Wrong Line for ctor level sourceInfo");
            Assert.AreEqual(4, sourceInfo.Column, "Wrong Column for ctor level sourceInfo");

            // Validate derived ctor
            methodBase = typeof(SourceInfoDerivedTestClass).GetConstructor(new Type[0]);
            Assert.IsNotNull(methodBase, "Could not find derived ctor");
            attributes = methodBase.GetCustomAttributes(false);
            sourceInfo = SourceInfo.GetSourceInfoFromAttributes(attributes);
            Assert.IsNotNull(sourceInfo, "Failed to get SourceInfo from ctor level attributes");
            Assert.AreEqual("ctorLevelFileDerived", sourceInfo.FileName, "Wrong FileName for ctor level sourceInfo");
            Assert.AreEqual(3, sourceInfo.Line, "Wrong Line for ctor level sourceInfo");
            Assert.AreEqual(4, sourceInfo.Column, "Wrong Column for ctor level sourceInfo");

            // Validate base property via derived type
            PropertyInfo propertyInfo = typeof(SourceInfoDerivedTestClass).GetProperty("TheValue");
            Assert.IsNotNull(propertyInfo, "Could not locate TheValue property");
            attributes = propertyInfo.GetCustomAttributes(false);
            sourceInfo = SourceInfo.GetSourceInfoFromAttributes(attributes);
            Assert.IsNotNull(sourceInfo, "Failed to get SourceInfo from ctor level attributes");
            Assert.AreEqual("propertyLevelFile", sourceInfo.FileName, "Wrong FileName for property level level sourceInfo");
            Assert.AreEqual(5, sourceInfo.Line, "Wrong Line for ctor level sourceInfo");
            Assert.AreEqual(6, sourceInfo.Column, "Wrong Column for ctor level sourceInfo");
        }

        [TestMethod]
        [Description("SourceInfoSourceFileLocation service works correctly using SourceInfoAttributes")]
        public void SourceInfoAttribute_LocationService()
        {
            FilenameMap filenameMap = new FilenameMap();

            using (SourceFileLocationService locationService = new SourceFileLocationService(new[] { new SourceInfoSourceFileProviderFactory() }, filenameMap))
            {
                IEnumerable<string> files = locationService.GetFilesForType(typeof(SourceInfoDerivedTestClass));
                Assert.IsNotNull(files, "SourceInfoSourceFileLocator returned null");

                string[] expectedFiles = new string[] {
                    // "ctorLevel",    The base class ctor will not be exposed in the GetConstructors call 
                    "ctorLevelFileDerived",     // only the derived ctor will be found for this type
                    "propertyLevelFile",        // demonstrates we find properties in the base class
                    "propertyLevelFileDerived", //   and in the derived class
                    "methodLevelFile",          // demonstrates same for methods
                    "methodLevelFileDerived"
                };

                foreach (string file in expectedFiles)
                {
                    Assert.IsTrue(files.Contains(file), "Did not find expected file '" + file + "' using SourceInfoSourceFileLocator");
                }
            }
        }
    }

    // Demonstrates:
    // 1. Type level info
    // 2. Ctor and property level info
    [SourceInfo(FileName = "typeLevelFile", Line = 1, Column = 2)]
    public partial class SourceInfoTestClass
    {
        [SourceInfo(FileName = "ctorLevelFile", Line = 3, Column = 4)]
        public SourceInfoTestClass() { }

        [SourceInfo(FileName = "propertyLevelFile", Line = 5, Column = 6)]
        public string TheValue { get; set; }
    }

    // Demonstrates:
    // 1. Method level info and ability to extend partial type
    public partial class SourceInfoTestClass
    {
        [SourceInfo(FileName = "methodLevelFile", Line = 7, Column = 8)]
        public void TheMethod() { }
    }

    // Demonstrates:
    // 1. Derived type possible
    // 2. Derived type overrides type level attributes
    [SourceInfo(FileName = "typeLevelFileDerived", Line = 1, Column = 2)]
    public partial class SourceInfoDerivedTestClass : SourceInfoTestClass
    {
        [SourceInfo(FileName = "ctorLevelFileDerived", Line = 3, Column = 4)]
        public SourceInfoDerivedTestClass() { }

        [SourceInfo(FileName = "propertyLevelFileDerived", Line = 5, Column = 6)]
        public string TheValueDerived { get; set; }

        [SourceInfo(FileName = "methodLevelFileDerived", Line = 7, Column = 8)]
        public void TheMethodDerived() { }
    }
}

// Namespace demonstrates ability to tolerate RootNamespace ("Test") prefix for VB types
namespace Test.Microsoft.VisualStudio.ServiceModel.DomainServices.Intellisense
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    public class SourceInfoAttribute : Attribute
    {
        public string FileName { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }
}
