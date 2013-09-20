using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure;

namespace Microsoft.ServiceModel.DomainServices.WindowsAzure.Test
{
    [TestClass]
    public class TableEntityContextTests
    {
        [TestMethod]
        [Description("Tests that the partition key can be modified and will propagate through to the contained entity sets")]
        public void PartitionKey()
        {
            MockTableEntityContext entityContext = new MockTableEntityContext();

            Assert.IsNull(entityContext.PartitionKey,
                "The PartitionKey should be null by default.");
            Assert.IsNull(entityContext.MockEntities1.PartitionKey,
                "The first TableEntitySet PartitionKey should be null by default.");
            Assert.IsNull(entityContext.MockEntities2.PartitionKey,
                "The second TableEntitySet PartitionKey should be null by default.");

            string partitionKey = "PK";
            entityContext.PartitionKey = partitionKey;

            Assert.AreEqual(partitionKey, entityContext.PartitionKey,
                "The PartitionKeys should be equal.");
            Assert.AreEqual(entityContext.PartitionKey, entityContext.MockEntities1.PartitionKey,
                "The first TableEntitySet PartitionKey should be equal to the context PartitionKey.");
            Assert.AreEqual(entityContext.PartitionKey, entityContext.MockEntities2.PartitionKey,
                "The second TableEntitySet PartitionKey should be equal to the context PartitionKey.");

            entityContext.PartitionKey = null;

            Assert.IsNull(entityContext.PartitionKey,
                "The PartitionKey should be null again.");
            Assert.IsNull(entityContext.MockEntities1.PartitionKey,
                "The first TableEntitySet PartitionKey should be null again.");
            Assert.IsNull(entityContext.MockEntities2.PartitionKey,
                "The second TableEntitySet PartitionKey should be null again.");
        }

        [TestMethod]
        [Description("Tests that CreateEntitySet is only called once per entity type")]
        public void GetEntitySetCallsCreateEntitySetOnce()
        {
            MockTableEntityContext entityContext = new MockTableEntityContext();

            Assert.IsFalse(entityContext.CreateEntitySetCounts.ContainsKey(typeof(MockEntity1)),
                "There should not have been any calls to create a MockEntity1 entity set.");
            Assert.IsFalse(entityContext.CreateEntitySetCounts.ContainsKey(typeof(MockEntity2)),
                "There should not have been any calls to create a MockEntity2 entity set.");

            Assert.IsNotNull(entityContext.MockEntities1,
                "The MockEntity1 set should not be null.");
            Assert.IsNotNull(entityContext.MockEntities2,
                "The MockEntity2 set should not be null.");

            Assert.AreEqual(1, entityContext.CreateEntitySetCounts[typeof(MockEntity1)],
                "CreateEntitySet should have been called once for MockEntity1.");
            Assert.AreEqual(1, entityContext.CreateEntitySetCounts[typeof(MockEntity2)],
                "CreateEntitySet should have been called once for MockEntity2.");

            Assert.IsNotNull(entityContext.MockEntities1,
                "The MockEntity1 set should still not be null.");
            Assert.IsNotNull(entityContext.MockEntities2,
                "The MockEntity2 set should still not be null.");

            Assert.AreEqual(1, entityContext.CreateEntitySetCounts[typeof(MockEntity1)],
                "CreateEntitySet should still have been called once for MockEntity1.");
            Assert.AreEqual(1, entityContext.CreateEntitySetCounts[typeof(MockEntity2)],
                "CreateEntitySet should still have been called once for MockEntity2.");
        }

        [TestMethod]
        [Description("Tests that CreateEntitySet is not called when a EntitySet has been added")]
        public void GetEntitySetDoesNotCreateWhenSetExists()
        {
            MockTableEntityContext entityContext = new MockTableEntityContext();

            Assert.IsFalse(entityContext.CreateEntitySetCounts.ContainsKey(typeof(MockEntity3)),
                "There should not have been any calls to create a MockEntity3 entity set.");

            Assert.AreEqual(entityContext.MockEntity3EntitySet, entityContext.MockEntities3,
                "The MockEntity3 set should equal the preset member.");

            Assert.IsFalse(entityContext.CreateEntitySetCounts.ContainsKey(typeof(MockEntity3)),
                "There should still not have been any calls to create a MockEntity3 entity set.");
        }

        [TestMethod]
        [Description("Tests that EnsureTableExists is called once foreach EntitySet that is added")]
        public void EnsureTableExistsIsCalledForEachSet()
        {
            MockTableEntityContext entityContext = new MockTableEntityContext();

            Assert.IsFalse(entityContext.EnsureTableExistsCounts.ContainsKey("MockEntity1"),
                "There should not have been any calls to ensure MockEntity1 exists.");
            Assert.IsFalse(entityContext.EnsureTableExistsCounts.ContainsKey("MockEntity2"),
                "There should not have been any calls to ensure MockEntity2 exists.");
            Assert.AreEqual(1, entityContext.EnsureTableExistsCounts["MockEntity3"],
                "There should have been one call to ensure MockEntity3 exists.");

            Assert.IsNotNull(entityContext.MockEntities1,
                "The MockEntity1 set should not be null.");
            Assert.IsNotNull(entityContext.MockEntities2,
                "The MockEntity2 set should not be null.");
            Assert.IsNotNull(entityContext.MockEntities3,
                "The MockEntity3 set should not be null.");

            Assert.AreEqual(1, entityContext.EnsureTableExistsCounts["MockEntity1"],
                "There should have been one call to ensure MockEntity1 exists.");
            Assert.AreEqual(1, entityContext.EnsureTableExistsCounts["MockEntity2"],
                "There should have been one call to ensure MockEntity2 exists.");
            Assert.AreEqual(1, entityContext.EnsureTableExistsCounts["MockEntity3"],
                "There should still have been one call to ensure MockEntity3 exists.");
        }

        [TestMethod]
        [Description("Tests that ResolveEntityType method returns the type for the specified table")]
        public void ResolveEntityTypes()
        {
            MockTableEntityContext entityContext = new MockTableEntityContext();

            Assert.IsNotNull(entityContext.MockEntities1,
                "The MockEntity1 set should not be null.");
            Assert.IsNotNull(entityContext.MockEntities2,
                "The MockEntity2 set should not be null.");
            Assert.IsNotNull(entityContext.MockEntities3,
                "The MockEntity3 set should not be null.");

            Assert.AreEqual(typeof(MockEntity1), entityContext.ResolveType(entityContext.StorageCredentials.AccountName + "." + entityContext.MockEntities1.TableName),
                "The type for MockEntities1 should be MockEntity1.");
            Assert.AreEqual(typeof(MockEntity2), entityContext.ResolveType(entityContext.StorageCredentials.AccountName + "." + entityContext.MockEntities2.TableName),
                "The type for MockEntities2 should be MockEntity2.");
            Assert.AreEqual(typeof(MockEntity3), entityContext.ResolveType(entityContext.StorageCredentials.AccountName + "." + entityContext.MockEntities3.TableName),
                "The type for MockEntities3 should be MockEntity3.");

            Assert.AreEqual(1, entityContext.ResolveEntityTypeCounts["MockEntity1"],
                "There should have been one call to resolve MockEntity1.");
            Assert.AreEqual(1, entityContext.ResolveEntityTypeCounts["MockEntity2"],
                "There should have been one call to resolve MockEntity2.");
            Assert.AreEqual(1, entityContext.ResolveEntityTypeCounts["MockEntity3"],
                "There should have been one call to resolve MockEntity3.");

            Assert.AreEqual(typeof(MockEntity1), entityContext.ResolveType(entityContext.StorageCredentials.AccountName + "." + entityContext.MockEntities1.TableName),
                "The type for MockEntities1 should be MockEntity1.");
            Assert.AreEqual(typeof(MockEntity2), entityContext.ResolveType(entityContext.StorageCredentials.AccountName + "." + entityContext.MockEntities2.TableName),
                "The type for MockEntities2 should be MockEntity2.");
            Assert.AreEqual(typeof(MockEntity3), entityContext.ResolveType(entityContext.StorageCredentials.AccountName + "." + entityContext.MockEntities3.TableName),
                "The type for MockEntities3 should be MockEntity3.");

            Assert.AreEqual(1, entityContext.ResolveEntityTypeCounts["MockEntity1"],
                "There should only have been one call to resolve MockEntity1.");
            Assert.AreEqual(1, entityContext.ResolveEntityTypeCounts["MockEntity2"],
                "There should only have been one call to resolve MockEntity2.");
            Assert.AreEqual(1, entityContext.ResolveEntityTypeCounts["MockEntity3"],
                "There should only have been one call to resolve MockEntity3.");
        }

        private class MockEntity1 : TableEntity { }
        private class MockEntity2 : TableEntity { }
        private class MockEntity3 : TableEntity { }

        private class MockTableEntityContext : TableEntityContext
        {
            public MockTableEntityContext()
                : base("https://mock.table.core.windows.net", new StorageCredentialsAccountAndKey("MockAccountName", new byte[4]))
            {
                this.MockEntity3EntitySet = new TableEntitySet<MockEntity3>(this);

                this.SetEntitySet<MockEntity3>(this.MockEntity3EntitySet);
            }

            private IDictionary<Type, int> _createEntitySetCounts = new Dictionary<Type, int>();

            public IDictionary<Type, int> CreateEntitySetCounts
            {
                get { return this._createEntitySetCounts; }
            }

            private IDictionary<string, int> _ensureTableExistsCounts = new Dictionary<string, int>();

            public IDictionary<string, int> EnsureTableExistsCounts
            {
                get { return this._ensureTableExistsCounts; }
            }

            private IDictionary<string, int> _resolveEntityTypeCounts = new Dictionary<string, int>();

            public IDictionary<string, int> ResolveEntityTypeCounts
            {
                get { return this._resolveEntityTypeCounts; }
            }

            public TableEntitySet<MockEntity3> MockEntity3EntitySet { get; set; }

            public TableEntitySet<MockEntity1> MockEntities1
            {
                get { return this.GetEntitySet<MockEntity1>(); }
            }

            public TableEntitySet<MockEntity2> MockEntities2
            {
                get { return this.GetEntitySet<MockEntity2>(); }
            }

            public TableEntitySet<MockEntity3> MockEntities3
            {
                get { return this.GetEntitySet<MockEntity3>(); }
            }

            protected override TableEntitySet<TEntity> CreateEntitySet<TEntity>()
            {
                if (!this.CreateEntitySetCounts.ContainsKey(typeof(TEntity)))
                {
                    this.CreateEntitySetCounts[typeof(TEntity)] = 0;
                }
                this.CreateEntitySetCounts[typeof(TEntity)]++;

                return base.CreateEntitySet<TEntity>();
            }

            protected override void EnsureTableExists(string tableName)
            {
                if (!this.EnsureTableExistsCounts.ContainsKey(tableName))
                {
                    this.EnsureTableExistsCounts[tableName] = 0;
                }
                this.EnsureTableExistsCounts[tableName]++;
                // overridden to avoid creating the table
            }

            protected override Type ResolveEntityType(string tableName)
            {
                if (!this.ResolveEntityTypeCounts.ContainsKey(tableName))
                {
                    this.ResolveEntityTypeCounts[tableName] = 0;
                }
                this.ResolveEntityTypeCounts[tableName]++;

                return base.ResolveEntityType(tableName);
            }
        }
    }
}
