extern alias SystemWebDomainServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;
using OpenRiaServices.Client.Test;
#if NET472
using OpenRiaServices.EntityFramework;
#else
using OpenRiaServices.Server.EntityFrameworkCore;
#endif
using OpenRiaServices.Hosting.Wcf;
using OpenRiaServices.Server;
using System.Threading;
using Cities;
using OpenRiaServices.LinqToSql;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using TestDomainServices;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;
using TheTypeDescriptorExtensions = SystemWebDomainServices::OpenRiaServices.Server.TypeDescriptorExtensions;


namespace OpenRiaServices.Server.Test
{
    public partial class DomainServiceDescriptionTest
    {

        // Verify that AttachAsModified works correctly when no original values are provided for non-concurrency properties.
        [TestMethod]
        public void ObjectContextExtensions_AttachAsModified()
        {
            TestDomainServices.EF.Northwind nw = new TestDomainServices.EF.Northwind();
            DomainServiceContext ctxt = new DomainServiceContext(new MockDataService(), new MockUser("mathew"), DomainOperationType.Submit);
            nw.Initialize(ctxt);

            var current = new NorthwindModel.Category()
            {
                EntityKey = new System.Data.Entity.Core.EntityKey("NorthwindEntities.Categories", "CategoryID", 1),
                CategoryID = 1,
                CategoryName = "Category",
                Description = "My category"
            };
            var original = new NorthwindModel.Category()
            {
                EntityKey = new System.Data.Entity.Core.EntityKey("NorthwindEntities.Categories", "CategoryID", 1),
                CategoryID = 1,
                CategoryName = "Category"
            };

            ObjectContextExtensions.AttachAsModified(nw.ObjectContext.Categories, current, original);

            var currentEntry = nw.ObjectContext.ObjectStateManager.GetObjectStateEntry(current);

            string[] changedProperties = currentEntry.GetModifiedProperties().ToArray();
            Assert.IsTrue(changedProperties.Contains("Description"));
        }

        /// <summary>
        /// Verify that our EF custom type descriptors work for POCO models
        /// </summary>
        [TestMethod]
        public void DomainServiceDescription_EFPOCO()
        {
            // First create a context manually and verify that POCO metadata is configured correctly
            NorthwindPOCOModel.NorthwindEntities ctxt = new NorthwindPOCOModel.NorthwindEntities();
            EntityType entityType = ctxt.Products.EntitySet.ElementType;
            Assert.IsNotNull(entityType);

            // direct test verifying that our helper methods work for POCO metadata
            entityType = (EntityType)ObjectContextUtilities.GetEdmType(ctxt.MetadataWorkspace, typeof(NorthwindPOCOModel.Product));
            Assert.IsNotNull(entityType);

            // E2E DomainServiceDescription test, verifying that our custom TDs are registered
            DomainServiceDescription desc = DomainServiceDescription.GetDescription(typeof(TestDomainServices.EF.NorthwindPOCO));

            PropertyDescriptor pd = TypeDescriptor.GetProperties(typeof(NorthwindPOCOModel.Product))["ProductID"];
            Assert.IsNotNull(pd.Attributes[typeof(KeyAttribute)]);

            pd = TypeDescriptor.GetProperties(typeof(NorthwindPOCOModel.Product))["Category"];
            AssociationAttribute assocAttrib = (AssociationAttribute)pd.Attributes[typeof(AssociationAttribute)];
            Assert.IsNotNull(assocAttrib);
        }

        [TestMethod]
        [Description("Asserts that underdefined EF properties with null facet values are successfully handled during timestamp comparison.")]
        [WorkItem(857020)]
        public void EF_TimestampComparisonHandlesNullFacetValues()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(DataTests.Scenarios.EF.Northwind.EF_NorthwindScenarios_TimestampComparison));
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(DataTests.Scenarios.EF.Northwind.EntityWithNullFacetValuesForTimestampComparison));

            PropertyDescriptor property = properties["StringWithoutConcurrencyMode"];
            Assert.IsNotNull(property,
                "StringWithoutConcurrencyMode property should exist.");
            Assert.IsNull(property.Attributes[typeof(TimestampAttribute)],
                "StringWithoutConcurrencyMode property is not a timestamp.");

            property = properties["StringWithoutFixedLength"];
            Assert.IsNotNull(property,
                "StringWithoutFixedLength property should exist.");
            Assert.IsNull(property.Attributes[typeof(TimestampAttribute)],
                "StringWithoutFixedLength property is not a timestamp.");

            property = properties["StringWithoutMaxLength"];
            Assert.IsNotNull(property,
                "StringWithoutMaxLength property should exist.");
            Assert.IsNull(property.Attributes[typeof(TimestampAttribute)],
                "StringWithoutMaxLength property is not a timestamp.");

            property = properties["StringWithoutComputed"];
            Assert.IsNotNull(property,
                "StringWithoutComputed property should exist.");
            Assert.IsNull(property.Attributes[typeof(TimestampAttribute)],
                "StringWithoutComputed property is not a timestamp.");

            property = properties["ConcurrencyTimestamp"];
            Assert.IsNotNull(property,
                "ConcurrencyTimestamp property should exist.");
            Assert.IsNotNull(property.Attributes[typeof(TimestampAttribute)],
                "ConcurrencyTimestamp property is a timestamp.");
        }

        [TestMethod]
        public void RequiredAttribute_DALInference()
        {
            // Verify that members of EF model marked non Nullable have RequiredAttribute
            var dsd = DomainServiceDescription.GetDescription(typeof(DataTests.Scenarios.EF.Northwind.EF_NorthwindScenarios_RequiredAttribute));
            var properties = TypeDescriptor.GetProperties(typeof(DataTests.Scenarios.EF.Northwind.RequiredAttributeTestEntity));

            var property = properties["RequiredString"];
            Assert.IsNotNull(property);
            var requiredAttribute = (RequiredAttribute)property.Attributes[typeof(RequiredAttribute)];
            Assert.IsNotNull(requiredAttribute);
            Assert.AreEqual<bool>(requiredAttribute.AllowEmptyStrings, false);

            property = properties["RequiredStringOverride"];
            Assert.IsNotNull(property);
            requiredAttribute = (RequiredAttribute)property.Attributes[typeof(RequiredAttribute)];
            Assert.IsNotNull(requiredAttribute);
            Assert.AreEqual<bool>(requiredAttribute.AllowEmptyStrings, true);

            property = properties["RequiredInt32"];
            Assert.IsNotNull(property);
            requiredAttribute = (RequiredAttribute)property.Attributes[typeof(RequiredAttribute)];
            Assert.IsNull(requiredAttribute);

            property = properties["OptionalString"];
            Assert.IsNotNull(property);
            requiredAttribute = (RequiredAttribute)property.Attributes[typeof(RequiredAttribute)];
            Assert.IsNull(requiredAttribute);

            property = properties["OptionalInt32"];
            Assert.IsNotNull(property);
            requiredAttribute = (RequiredAttribute)property.Attributes[typeof(RequiredAttribute)];
            Assert.IsNull(requiredAttribute);

            // Verify that members of LTS model marked non Nullable have RequiredAttribute
            dsd = DomainServiceDescription.GetDescription(typeof(DataTests.Scenarios.LTS.Northwind.LTS_NorthwindScenarios));
            properties = TypeDescriptor.GetProperties(typeof(DataTests.Scenarios.LTS.Northwind.RequiredAttributeTestEntity));

            property = properties["RequiredString"];
            Assert.IsNotNull(property);
            requiredAttribute = (RequiredAttribute)property.Attributes[typeof(RequiredAttribute)];
            Assert.IsNotNull(requiredAttribute);
            Assert.AreEqual<bool>(requiredAttribute.AllowEmptyStrings, false);

            property = properties["RequiredStringOverride"];
            Assert.IsNotNull(property);
            requiredAttribute = (RequiredAttribute)property.Attributes[typeof(RequiredAttribute)];
            Assert.IsNotNull(requiredAttribute);
            Assert.AreEqual<bool>(requiredAttribute.AllowEmptyStrings, true);

            property = properties["RequiredInt32"];
            Assert.IsNotNull(property);
            requiredAttribute = (RequiredAttribute)property.Attributes[typeof(RequiredAttribute)];
            Assert.IsNull(requiredAttribute);

            property = properties["RequiredNullableInt32"];
            Assert.IsNotNull(property);
            requiredAttribute = (RequiredAttribute)property.Attributes[typeof(RequiredAttribute)];
            Assert.IsNotNull(requiredAttribute);
            Assert.AreEqual<bool>(requiredAttribute.AllowEmptyStrings, false);

            property = properties["OptionalString"];
            Assert.IsNotNull(property);
            requiredAttribute = (RequiredAttribute)property.Attributes[typeof(RequiredAttribute)];
            Assert.IsNull(requiredAttribute);

            property = properties["OptionalInt32"];
            Assert.IsNotNull(property);
            requiredAttribute = (RequiredAttribute)property.Attributes[typeof(RequiredAttribute)];
            Assert.IsNull(requiredAttribute);

            property = properties["OptionalNullableInt32"];
            Assert.IsNotNull(property);
            requiredAttribute = (RequiredAttribute)property.Attributes[typeof(RequiredAttribute)];
            Assert.IsNull(requiredAttribute);
        }

    }
}
