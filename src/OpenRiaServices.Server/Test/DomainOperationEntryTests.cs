using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using OpenRiaServices.Client.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Server.Test
{
    /// <summary>
    /// Tests the behavior of DomainOperationEntry
    /// </summary>
    [TestClass]
    public class DomainOperationEntryTests
    {
        [TestMethod]
        [Description("All known DomainOperationEntry types can be found in DomainServiceDescription and contain the correct properties")]
        public void DomainOperationEntry_All_Types()
        {
            DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(DomainOperationEntryTestDomainService));

            // Invoke
            DomainOperationEntry entry = description.DomainOperationEntries.SingleOrDefault(p => p.Name == "InvokeMethod");
            Assert.IsNotNull(entry, "Could not locate InvokeMethod");
            Assert.IsNull(entry.AssociatedType, "Invoke should have no associated type");
            Assert.AreEqual(typeof(DomainOperationEntryTestDomainService), entry.DomainServiceType, "Wrong domain service type");
            Assert.AreEqual(DomainOperation.Invoke, entry.Operation, "Wrong domain operation");
            Assert.AreEqual("Invoke", entry.OperationType, "Wrong operation type for this DomainOperationEntry");

            // Invoke with entity
            entry = description.DomainOperationEntries.SingleOrDefault(p => p.Name == "InvokeMethodEntity");
            Assert.IsNotNull(entry, "Could not locate InvokeMethodEntity");
            Assert.AreEqual(typeof(DomainOperationEntryTestEntity), entry.AssociatedType, "Wrong associated type");
            Assert.AreEqual(typeof(DomainOperationEntryTestDomainService), entry.DomainServiceType, "Wrong domain service type");
            Assert.AreEqual(DomainOperation.Invoke, entry.Operation, "Wrong domain operation");
            Assert.AreEqual("Invoke", entry.OperationType, "Wrong operation type for this DomainOperationEntry");
            
            // Query
            entry = description.DomainOperationEntries.SingleOrDefault(p => p.Name == "GetEntities");
            Assert.IsNotNull(entry, "Could not locate GetEntities");
            Assert.AreEqual(typeof(DomainOperationEntryTestEntity), entry.AssociatedType, "Wrong associated type");
            Assert.AreEqual(typeof(DomainOperationEntryTestDomainService), entry.DomainServiceType, "Wrong domain service type");
            Assert.AreEqual(DomainOperation.Query, entry.Operation, "Wrong domain operation");
            Assert.AreEqual("Query", entry.OperationType, "Wrong operation type for this DomainOperationEntry");

            // Insert
            entry = description.DomainOperationEntries.SingleOrDefault(p => p.Name == "InsertMethod");
            Assert.IsNotNull(entry, "Could not locate InsertMethod");
            Assert.AreEqual(typeof(DomainOperationEntryTestEntity), entry.AssociatedType, "Wrong associated type");
            Assert.AreEqual(typeof(DomainOperationEntryTestDomainService), entry.DomainServiceType, "Wrong domain service type");
            Assert.AreEqual(DomainOperation.Insert, entry.Operation, "Wrong domain operation");Assert.AreEqual("Insert", entry.OperationType, "Wrong operation type for this DomainOperationEntry");

            // Update
            entry = description.DomainOperationEntries.SingleOrDefault(p => p.Name == "UpdateMethod");
            Assert.IsNotNull(entry, "Could not locate InvokeMethod");
            Assert.AreEqual(typeof(DomainOperationEntryTestEntity), entry.AssociatedType, "Wrong associated type");
            Assert.AreEqual(typeof(DomainOperationEntryTestDomainService), entry.DomainServiceType, "Wrong domain service type");
            Assert.AreEqual(DomainOperation.Update, entry.Operation, "Wrong domain operation");
            Assert.AreEqual("Update", entry.OperationType, "Wrong operation type for this DomainOperationEntry");

            // Custom Update
            entry = description.DomainOperationEntries.SingleOrDefault(p => p.Name == "CustomMethod");
            Assert.IsNotNull(entry, "Could not locate CustomMethod");
            Assert.AreEqual(typeof(DomainOperationEntryTestEntity), entry.AssociatedType, "Wrong associated type");
            Assert.AreEqual(typeof(DomainOperationEntryTestDomainService), entry.DomainServiceType, "Wrong domain service type");
            Assert.AreEqual(DomainOperation.Custom, entry.Operation, "Wrong domain operation");
            Assert.AreEqual("Update", entry.OperationType, "Wrong operation type for this DomainOperationEntry");

            // Delete
            entry = description.DomainOperationEntries.SingleOrDefault(p => p.Name == "DeleteMethod");
            Assert.IsNotNull(entry, "Could not locate DeleteMethod");
            Assert.AreEqual(typeof(DomainOperationEntryTestEntity), entry.AssociatedType, "Wrong associated type");
            Assert.AreEqual(typeof(DomainOperationEntryTestDomainService), entry.DomainServiceType, "Wrong domain service type");
            Assert.AreEqual(DomainOperation.Delete, entry.Operation, "Wrong domain operation");
            Assert.AreEqual("Delete", entry.OperationType, "Wrong operation type for this DomainOperationEntry");
        }

        [EnableClientAccess]
        public class DomainOperationEntryTestDomainService : DomainService
        {
            [Query]
            public IEnumerable<DomainOperationEntryTestEntity> GetEntities() { return null; }

            [Insert]
            public void InsertMethod(DomainOperationEntryTestEntity entity) { }

            [Update]
            public void UpdateMethod(DomainOperationEntryTestEntity entity) { }

            [EntityAction]
            public void CustomMethod(DomainOperationEntryTestEntity entity) { }

            [Delete]
            public void DeleteMethod(DomainOperationEntryTestEntity entity) { }

            [Invoke]
            public void InvokeMethod() { }

            [Invoke]
            public void InvokeMethodEntity(DomainOperationEntryTestEntity entity) { }
        }

        public class DomainOperationEntryTestEntity
        {
            [Key]
            public string TheValue { get; set; }
        }

    }
}
