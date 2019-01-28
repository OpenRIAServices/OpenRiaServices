using System;
using System.Collections.Generic;
using System.Linq;
using AdventureWorksModel;
using NorthwindModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools.Test
{
    [TestClass]
    public class DataObjectTests
    {

        [TestMethod]
        [Description("BusinessLogicData ctor and properties all work")]
        public void BusinessLogicData_Properties()
        {
            string [] assemblyPaths = new string[] { "x", "y", "z" };
            string[] contextTypes = new string[] { typeof(NorthwindEntities).AssemblyQualifiedName, typeof(AdventureWorksEntities).AssemblyQualifiedName };
            BusinessLogicData d = new BusinessLogicData()
            {
                AssemblyPaths = assemblyPaths,
                ContextTypeNames = contextTypes,
                Language = "C#",
                LinqToSqlPath = "ltsPath"
            };

            Assert.AreEqual(assemblyPaths, d.AssemblyPaths, "AssemblyPaths failed");
            Assert.AreEqual(contextTypes, d.ContextTypeNames, "ContextTypeNames failed");
            Assert.AreEqual("C#", d.Language, "Language failed");
            Assert.AreEqual("ltsPath", d.LinqToSqlPath, "LTSPath failed");
        }

        [TestMethod]
        [Description("ContextData ctor and properties all work")]
        public void ContextData_Properties()
        {
            ContextData d = new ContextData()
            {
                IsClientAccessEnabled = true,
                IsODataEndpointEnabled = true,
                Name = "foo",
                ID = 1
            };

            Assert.IsTrue(d.IsClientAccessEnabled, "ClientAccessEnable failed");
            Assert.IsTrue(d.IsODataEndpointEnabled, "OData failed");
            Assert.AreEqual("foo", d.Name, "Name failed");
            Assert.AreEqual(1, d.ID);

            // Flip the bools just in case
            d.IsClientAccessEnabled = false;
            Assert.IsFalse(d.IsClientAccessEnabled, "ClientAccessEnable did not flip");

            d.IsODataEndpointEnabled = false;
            Assert.IsFalse(d.IsODataEndpointEnabled, "OData did not flip");
        }

        [TestMethod]
        [Description("EntityData ctor and properties all work")]
        public void EntityData_Properties()
        {
            EntityData d = new EntityData()
            {
               AssemblyName = "assemblyName",
               CanBeEdited = true,
               CanBeIncluded = true,
               IsIncluded = true,
               IsEditable = true,
               Name = "name"
            };

            Assert.AreEqual("name", d.Name, "Name failed");
            Assert.AreEqual("assemblyName", d.AssemblyName, "AssemblyName failed");
            Assert.IsTrue(d.CanBeIncluded, "CanBeIncluded failed");
            Assert.IsTrue(d.CanBeEdited, "CanBeEdited failed");
            Assert.IsTrue(d.IsIncluded, "IsIncluded failed");
            Assert.IsTrue(d.IsEditable, "IsEditable failed");

            // Flip each and verify it can also go false
            d.CanBeIncluded = false;
            Assert.IsFalse(d.CanBeIncluded, "CanBeIncluded did not flip");

            d.CanBeEdited = false;
            Assert.IsFalse(d.CanBeEdited, "CanBeEditied did not flip");

            d.IsIncluded = false;
            Assert.IsFalse(d.IsIncluded, "IsIncluded did not flip");

            d.IsEditable = false;
            Assert.IsFalse(d.IsEditable, "IsEditable did not flip");
        }
    }
}