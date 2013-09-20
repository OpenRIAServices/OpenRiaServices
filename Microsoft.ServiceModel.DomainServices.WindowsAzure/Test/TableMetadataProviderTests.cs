using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.ServiceModel.DomainServices.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace Microsoft.ServiceModel.DomainServices.WindowsAzure.Test
{
    [TestClass]
    public class TableMetadataProviderTests
    {
        // TODO: it might be better to run these tests using the code generation pipeline, but
        // we don't have that capability right now

        [TestMethod]
        [Description("Tests that the default metadata is applied to entity types available on a TableDomainService")]
        public void DomainServiceDescriptionContainsEntities()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TMPT_DomainService));

            Assert.AreEqual(2, dsd.EntityTypes.Count(),
                "There should be 2 entity types.");
            Assert.IsTrue(dsd.EntityTypes.Contains(typeof(TMPT_MockEntity1)),
                "TMPT_MockEntity1 should be an entity type.");
            Assert.IsTrue(dsd.EntityTypes.Contains(typeof(TMPT_MockEntity2)),
                "TMPT_MockEntity2 should be an entity type.");
        }

        [TestMethod]
        [Description("Tests that the default metadata is applied for the partition key")]
        public void DefaultPartitionKeyMetadata()
        {
            // Create the DSD to register type descriptors
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TMPT_DomainService));

            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(TMPT_MockEntity1));

            PropertyDescriptor descriptor = properties["PartitionKey"];
            Assert.IsNotNull(descriptor,
                "There should be a property descriptor for PartitionKey.");
            Assert.AreEqual(typeof(string), descriptor.PropertyType,
                "The PartitionKey type should be a string.");

            KeyAttribute keyAttribute = (KeyAttribute)descriptor.Attributes[typeof(KeyAttribute)];
            Assert.IsNotNull(keyAttribute,
                "The PartitionKey should have a KeyAttribute.");

            EditableAttribute editableAttribute = (EditableAttribute)descriptor.Attributes[typeof(EditableAttribute)];
            Assert.IsNotNull(editableAttribute,
                "The PartitionKey should have an EditableAttribute.");
            Assert.IsFalse(editableAttribute.AllowEdit,
                "The PartitionKey should not allow editing.");
            Assert.IsTrue(editableAttribute.AllowInitialValue,
                "The PartitionKey should allow an initial value.");

            DisplayAttribute displayAttribute = (DisplayAttribute)descriptor.Attributes[typeof(DisplayAttribute)];
            Assert.IsNotNull(displayAttribute,
                "The PartitionKey should have an DisplayAttribute.");
            Assert.IsFalse(displayAttribute.AutoGenerateField,
                "The PartitionKey should not be included in autogeneration.");
        }

        [TestMethod]
        [Description("Tests that the default metadata is applied for the row key")]
        public void DefaultRowKeyMetadata()
        {
            // Create the DSD to register type descriptors
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TMPT_DomainService));

            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(TMPT_MockEntity1));

            PropertyDescriptor descriptor = properties["RowKey"];
            Assert.IsNotNull(descriptor,
                "There should be a property descriptor for RowKey.");
            Assert.AreEqual(typeof(string), descriptor.PropertyType,
                "The RowKey type should be a string.");

            KeyAttribute keyAttribute = (KeyAttribute)descriptor.Attributes[typeof(KeyAttribute)];
            Assert.IsNotNull(keyAttribute,
                "The RowKey should have a KeyAttribute.");

            EditableAttribute editableAttribute = (EditableAttribute)descriptor.Attributes[typeof(EditableAttribute)];
            Assert.IsNotNull(editableAttribute,
                "The RowKey should have an EditableAttribute.");
            Assert.IsFalse(editableAttribute.AllowEdit,
                "The RowKey should not allow editing.");
            Assert.IsTrue(editableAttribute.AllowInitialValue,
                "The RowKey should allow an initial value.");

            DisplayAttribute displayAttribute = (DisplayAttribute)descriptor.Attributes[typeof(DisplayAttribute)];
            Assert.IsNotNull(displayAttribute,
                "The RowKey should have an DisplayAttribute.");
            Assert.IsFalse(displayAttribute.AutoGenerateField,
                "The RowKey should not be included in autogeneration.");
        }

        [TestMethod]
        [Description("Tests that the default metadata is applied for the timestamp")]
        public void DefaultTimestampMetadata()
        {
            // Create the DSD to register type descriptors
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TMPT_DomainService));

            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(TMPT_MockEntity1));

            PropertyDescriptor descriptor = properties["Timestamp"];
            Assert.IsNotNull(descriptor,
                "There should be a property descriptor for Timestamp.");
            Assert.AreEqual(typeof(DateTime), descriptor.PropertyType,
                "The Timestamp type should be a DateTime.");

            TimestampAttribute timestampAttribute = (TimestampAttribute)descriptor.Attributes[typeof(TimestampAttribute)];
            Assert.IsNotNull(timestampAttribute,
                "The Timestamp should have a TimestampAttribute.");

            EditableAttribute editableAttribute = (EditableAttribute)descriptor.Attributes[typeof(EditableAttribute)];
            Assert.IsNotNull(editableAttribute,
                "The Timestamp should have an EditableAttribute.");
            Assert.IsFalse(editableAttribute.AllowEdit,
                "The Timestamp should not allow editing.");
            Assert.IsFalse(editableAttribute.AllowInitialValue,
                "The Timestamp should not allow an initial value.");

            DisplayAttribute displayAttribute = (DisplayAttribute)descriptor.Attributes[typeof(DisplayAttribute)];
            Assert.IsNotNull(displayAttribute,
                "The Timestamp should have an DisplayAttribute.");
            Assert.IsFalse(displayAttribute.AutoGenerateField,
                "The Timestamp should not be included in autogeneration.");
        }

        [TestMethod]
        [Description("Tests that the metadata for the partition key can be overwritten")]
        public void OverwrittenPartitionKeyMetadata()
        {
            // Create the DSD to register type descriptors
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TMPT_DomainService));

            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(TMPT_MockEntity2));

            PropertyDescriptor descriptor = properties["PartitionKey"];
            Assert.IsNotNull(descriptor,
                "There should be a property descriptor for PartitionKey.");
            Assert.AreEqual(typeof(string), descriptor.PropertyType,
                "The PartitionKey type should be a string.");

            KeyAttribute keyAttribute = (KeyAttribute)descriptor.Attributes[typeof(KeyAttribute)];
            Assert.IsNotNull(keyAttribute,
                "The PartitionKey should have a KeyAttribute.");

            EditableAttribute editableAttribute = (EditableAttribute)descriptor.Attributes[typeof(EditableAttribute)];
            Assert.IsNotNull(editableAttribute,
                "The PartitionKey should have an EditableAttribute.");
            Assert.IsTrue(editableAttribute.AllowEdit,
                "The PartitionKey should allow editing.");
            Assert.IsTrue(editableAttribute.AllowInitialValue,
                "The PartitionKey should allow an initial value.");

            DisplayAttribute displayAttribute = (DisplayAttribute)descriptor.Attributes[typeof(DisplayAttribute)];
            Assert.IsNotNull(displayAttribute,
                "The PartitionKey should have an DisplayAttribute.");
            Assert.AreEqual("PK", displayAttribute.Name,
                "The PartitionKey names should be equal.");
        }

        [TestMethod]
        [Description("Tests that the metadata for the row key can be overwritten")]
        public void OverwrittenRowKeyMetadata()
        {
            // Create the DSD to register type descriptors
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TMPT_DomainService));

            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(TMPT_MockEntity2));

            PropertyDescriptor descriptor = properties["RowKey"];
            Assert.IsNotNull(descriptor,
                "There should be a property descriptor for RowKey.");
            Assert.AreEqual(typeof(string), descriptor.PropertyType,
                "The RowKey type should be a string.");

            KeyAttribute keyAttribute = (KeyAttribute)descriptor.Attributes[typeof(KeyAttribute)];
            Assert.IsNotNull(keyAttribute,
                "The RowKey should have a KeyAttribute.");

            EditableAttribute editableAttribute = (EditableAttribute)descriptor.Attributes[typeof(EditableAttribute)];
            Assert.IsNotNull(editableAttribute,
                "The RowKey should have an EditableAttribute.");
            Assert.IsTrue(editableAttribute.AllowEdit,
                "The RowKey should allow editing.");
            Assert.IsTrue(editableAttribute.AllowInitialValue,
                "The RowKey should allow an initial value.");

            DisplayAttribute displayAttribute = (DisplayAttribute)descriptor.Attributes[typeof(DisplayAttribute)];
            Assert.IsNotNull(displayAttribute,
                "The RowKey should have an DisplayAttribute.");
            Assert.AreEqual("RK", displayAttribute.Name,
                "The RowKey names shoulds be equal.");
        }

        [TestMethod]
        [Description("Tests that the metadata for the timestamp can be overwritten")]
        public void OverwrittenTimestampMetadata()
        {
            // Create the DSD to register type descriptors
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TMPT_DomainService));

            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(TMPT_MockEntity2));

            PropertyDescriptor descriptor = properties["Timestamp"];
            Assert.IsNotNull(descriptor,
                "There should be a property descriptor for Timestamp.");
            Assert.AreEqual(typeof(DateTime), descriptor.PropertyType,
                "The Timestamp type should be a DateTime.");

            TimestampAttribute timestampAttribute = (TimestampAttribute)descriptor.Attributes[typeof(TimestampAttribute)];
            Assert.IsNotNull(timestampAttribute,
                "The Timestamp should have a TimestampAttribute.");

            EditableAttribute editableAttribute = (EditableAttribute)descriptor.Attributes[typeof(EditableAttribute)];
            Assert.IsNotNull(editableAttribute,
                "The Timestamp should have an EditableAttribute.");
            Assert.IsTrue(editableAttribute.AllowEdit,
                "The Timestamp should allow editing.");
            Assert.IsTrue(editableAttribute.AllowInitialValue,
                "The Timestamp should allow an initial value.");

            DisplayAttribute displayAttribute = (DisplayAttribute)descriptor.Attributes[typeof(DisplayAttribute)];
            Assert.IsNotNull(displayAttribute,
                "The Timestamp should have an DisplayAttribute.");
            Assert.AreEqual("T", displayAttribute.Name,
                "The Timestamp names should be equal.");
        }

        [TestMethod]
        [Description("Tests that each entity is given an ETag property")]
        public void ETagProperty()
        {
            // Create the DSD to register type descriptors
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TMPT_DomainService));

            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(TMPT_MockEntity1));

            PropertyDescriptor descriptor = properties["ETag"];
            Assert.IsNotNull(descriptor,
                "There should be a property descriptor for ETag.");
            Assert.AreEqual(typeof(string), descriptor.PropertyType,
                "The ETag type should be a string.");

            EditableAttribute editableAttribute = (EditableAttribute)descriptor.Attributes[typeof(EditableAttribute)];
            Assert.IsNotNull(editableAttribute,
                "The ETag should have an EditableAttribute.");
            Assert.IsFalse(editableAttribute.AllowEdit,
                "The ETag should not allow editing.");
            Assert.IsFalse(editableAttribute.AllowInitialValue,
                "The ETag should not allow an initial value.");

            DisplayAttribute displayAttribute = (DisplayAttribute)descriptor.Attributes[typeof(DisplayAttribute)];
            Assert.IsNotNull(displayAttribute,
                "The ETag should have an DisplayAttribute.");
            Assert.IsFalse(displayAttribute.AutoGenerateField,
                "The ETag should not be included in autogeneration.");

            ConcurrencyCheckAttribute concurrencyCheckAttribute = (ConcurrencyCheckAttribute)descriptor.Attributes[typeof(ConcurrencyCheckAttribute)];
            Assert.IsNotNull(concurrencyCheckAttribute,
                "The ETag should have an ConcurrencyCheckAttribute.");
        }

        [TestMethod]
        [Description("Tests that the ETag property descriptor updates the entity's eTag")]
        public void ETagPropertyUpdatesEntity()
        {
            // Create the DSD to register type descriptors
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TMPT_DomainService));

            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(TMPT_MockEntity1));

            PropertyDescriptor descriptor = properties["ETag"];
            Assert.IsNotNull(descriptor,
                "There should be a property descriptor for ETag.");

            TMPT_MockEntity1 entity = new TMPT_MockEntity1();
            Assert.AreEqual(entity.GetETag(), descriptor.GetValue(entity),
                "The value returned should match the entity.");

            string etag = "etag";
            entity.SetETag(etag);
            Assert.AreEqual(entity.GetETag(), descriptor.GetValue(entity),
                "The value returned should match the updated entity.");

            etag = "etag2";
            descriptor.SetValue(entity, etag);
            Assert.AreEqual(etag, entity.GetETag(),
                "The entity value should match the updated etag.");
            Assert.AreEqual(etag, descriptor.GetValue(entity),
                "The descriptor value should match the updated etag.");

            descriptor.ResetValue(entity);
            Assert.AreEqual(entity.GetETag(), descriptor.GetValue(entity),
                "The reset value returned should match the entity.");
        }
    }

    public class TMPT_MockEntity1 : TableEntity { }

    [MetadataType(typeof(TMPT_MockEntity2.Metadata))]
    public class TMPT_MockEntity2 : TableEntity
    {
        // Override the default metadata
        private class Metadata
        {
            [Editable(true)]
            [Display(Name = "PK")]
            public string PartitionKey { get; set; }

            [Editable(true)]
            [Display(Name = "RK")]
            public string RowKey { get; set; }

            [Editable(true)]
            [Display(Name = "T")]
            public DateTime Timestamp { get; set; }
        }
    }

    public class TMPT_EntityContext : TableEntityContext
    {
        public TMPT_EntityContext()
            : base("https://mock.table.core.windows.net", new StorageCredentialsAccountAndKey("MockAccountName", new byte[4]))
        {
        }
    }

    public class TMPT_DomainService : TableDomainService<TMPT_EntityContext>
    {
        public DomainServiceDescription Description
        {
            get { return this.ServiceDescription; }
        }

        public IQueryable<TMPT_MockEntity1> GetEntity1s() { return null; }
        public IQueryable<TMPT_MockEntity2> GetEntity2s() { return null; }
    }
}
