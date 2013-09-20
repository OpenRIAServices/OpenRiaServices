using System.Collections;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Microsoft.ServiceModel.DomainServices.WindowsAzure.Test
{
    [TestClass]
    public class TableEntitySetTests
    {
        [TestMethod]
        [Description("Tests that PartitionKey can be read and written to.")]
        public void PartitionKey()
        {
            MockEntitySet entitySet = new MockEntitySet();

            string partitionKey = "PK";

            Assert.IsNull(entitySet.PartitionKey, "PartitionKey should be null by default.");

            entitySet.PartitionKey = partitionKey;

            Assert.AreEqual(partitionKey, entitySet.PartitionKey,
                "PartitionKey should equal partitionKey.");

            entitySet.PartitionKey = null;

            Assert.IsNull(entitySet.PartitionKey, "PartitionKey should be set back to null.");
        }

        [TestMethod]
        [Description("Tests that PartitionKey is used when adding a new entity.")]
        public void PartitionKeyIsUsedForNewEntity()
        {
            MockEntitySet entitySet = new MockEntitySet();
            entitySet.PartitionKey = "PK";

            MockEntity entity1 = new MockEntity();
            entitySet.Add(entity1);

            Assert.AreEqual(entitySet.PartitionKey, entity1.PartitionKey,
                "First entity PartitionKey should equal the TableEntitySet PartitionKey.");

            MockEntity entity2 = new MockEntity();
            entitySet.Add(entity2);

            Assert.AreEqual(entitySet.PartitionKey, entity2.PartitionKey,
                "Second entity PartitionKey should equal the TableEntitySet PartitionKey.");
        }

        [TestMethod]
        [Description("Tests that a new unique partition key is used when adding a new entity.")]
        public void UniquePartitionKeyIsUsedForNewEntity()
        {
            MockEntitySet entitySet = new MockEntitySet();
            entitySet.PartitionKey = null;

            MockEntity entity1 = new MockEntity();
            entitySet.Add(entity1);

            Assert.IsNotNull(entity1.PartitionKey,
                "First entity PartitionKey should not be null.");

            MockEntity entity2 = new MockEntity();
            entitySet.Add(entity2);

            Assert.IsNotNull(entity2.PartitionKey,
                "Second entity PartitionKey should not be null.");

            Assert.AreNotEqual(entity1.PartitionKey, entity2.PartitionKey,
                "PartitionKeys should be unique.");
        }

        [TestMethod]
        [Description("Tests that a new unique row key is used when adding a new entity.")]
        public void UniqueRowKeyIsUsedForNewEntity()
        {
            MockEntitySet entitySet = new MockEntitySet();

            MockEntity entity1 = new MockEntity();
            entitySet.Add(entity1);

            Assert.IsNotNull(entity1.RowKey,
                "First entity RowKey should not be null.");

            MockEntity entity2 = new MockEntity();
            entitySet.Add(entity2);

            Assert.IsNotNull(entity2.RowKey,
                "Second entity RowKey should not be null.");

            Assert.AreNotEqual(entity1.RowKey, entity2.RowKey,
                "RowKeys should be unique.");
        }

        [TestMethod]
        [Description("Tests that preset values are not overwritten when adding a new entity.")]
        public void PresetValuesAreUsedForNewEntity()
        {
            MockEntitySet entitySet = new MockEntitySet();
            entitySet.PartitionKey = "PK";

            string partitionKey = "MyPartitionKey";
            string rowKey = "MyRowKey";

            MockEntity entity1 = new MockEntity() { PartitionKey = partitionKey, RowKey = rowKey };
            entitySet.Add(entity1);

            Assert.AreEqual(partitionKey, entity1.PartitionKey,
                "First entity PartitionKey should not be overwritten by the TableEntitySet PartitionKey.");
            Assert.AreEqual(rowKey, entity1.RowKey,
                "First entity RowKey should not be overwritten by the TableEntitySet.");

            entitySet.PartitionKey = null;
            rowKey = "MyRowKey2";

            MockEntity entity2 = new MockEntity() { PartitionKey = partitionKey, RowKey = rowKey };
            entitySet.Add(entity2);

            Assert.AreEqual(partitionKey, entity2.PartitionKey,
                "Second entity PartitionKey should not be overwritten by the TableEntitySet PartitionKey.");
            Assert.AreEqual(rowKey, entity2.RowKey,
                "Second entity RowKey should not be overwritten by the TableEntitySet.");
        }

        [TestMethod]
        [Description("Tests that adding a new entity should transition its state in the context.")]
        public void AddNewEntity()
        {
            MockEntitySet entitySet = new MockEntitySet();
            MockEntity entity = new MockEntity();

            Assert.AreEqual(EntityStates.Detached, entitySet.GetEntityState(entity),
                "Entity should start detached.");

            entitySet.Add(entity);

            Assert.AreEqual(EntityStates.Added, entitySet.GetEntityState(entity),
                "Entity should be added.");

            entitySet.Add(entity);

            Assert.AreEqual(EntityStates.Added, entitySet.GetEntityState(entity),
                "Entity should still be added.");

            entitySet.Detach(entity);

            Assert.AreEqual(EntityStates.Detached, entitySet.GetEntityState(entity),
                "Entity should return to detached.");
        }

        [TestMethod]
        [Description("Tests that adding an existing entity should transition its state in the context.")]
        public void AddExistingEntity()
        {
            MockEntitySet entitySet = new MockEntitySet();
            MockEntity entity = new MockEntity() { PartitionKey = "PK", RowKey = "RK" };

            Assert.AreEqual(EntityStates.Detached, entitySet.GetEntityState(entity),
                "Entity should start detached.");

            entitySet.Attach(entity);

            Assert.AreEqual(EntityStates.Unchanged, entitySet.GetEntityState(entity),
                "Entity should now be attached.");

            entitySet.Add(entity);

            Assert.AreEqual(EntityStates.Added, entitySet.GetEntityState(entity),
                "Entity should be added.");

            entitySet.Detach(entity);

            Assert.AreEqual(EntityStates.Detached, entitySet.GetEntityState(entity),
                "Entity should return to detached.");
        }

        [TestMethod]
        [Description("Tests that deleting a new entity should transition its state in the context.")]
        public void DeleteNewEntity()
        {
            MockEntitySet entitySet = new MockEntitySet();
            MockEntity entity = new MockEntity();

            Assert.AreEqual(EntityStates.Detached, entitySet.GetEntityState(entity),
                "Entity should start detached.");

            entitySet.Add(entity);

            Assert.AreEqual(EntityStates.Added, entitySet.GetEntityState(entity),
                "Entity should be added.");

            entitySet.Delete(entity);

            Assert.AreEqual(EntityStates.Detached, entitySet.GetEntityState(entity),
                "Entity should be detached.");
        }

        [TestMethod]
        [Description("Tests that deleting an existing entity should transition its state in the context.")]
        public void DeleteExistingEntity()
        {
            MockEntitySet entitySet = new MockEntitySet();
            MockEntity entity = new MockEntity() { PartitionKey = "PK", RowKey = "RK" };

            Assert.AreEqual(EntityStates.Detached, entitySet.GetEntityState(entity),
                "Entity should start detached.");

            entitySet.Attach(entity);

            Assert.AreEqual(EntityStates.Unchanged, entitySet.GetEntityState(entity),
                "Entity should now be attached.");

            entitySet.Delete(entity);

            Assert.AreEqual(EntityStates.Deleted, entitySet.GetEntityState(entity),
                "Entity should be deleted.");

            entitySet.Detach(entity);

            Assert.AreEqual(EntityStates.Detached, entitySet.GetEntityState(entity),
                "Entity should return to detached.");

            entitySet.Delete(entity);

            Assert.AreEqual(EntityStates.Deleted, entitySet.GetEntityState(entity),
                "Entity should be deleted after being detached.");
        }

        [TestMethod]
        [Description("Tests that updating a new entity should transition its state in the context.")]
        public void UpdateNewEntity()
        {
            MockEntitySet entitySet = new MockEntitySet();
            MockEntity entity = new MockEntity();

            Assert.AreEqual(EntityStates.Detached, entitySet.GetEntityState(entity),
                "Entity should start detached.");

            entitySet.Add(entity);

            Assert.AreEqual(EntityStates.Added, entitySet.GetEntityState(entity),
                "Entity should be added.");

            entitySet.Update(entity);

            Assert.AreEqual(EntityStates.Added, entitySet.GetEntityState(entity),
                "Entity should still be added.");
        }

        [TestMethod]
        [Description("Tests that updating an existing entity should transition its state in the context.")]
        public void UpdateExistingEntity()
        {
            MockEntitySet entitySet = new MockEntitySet();
            MockEntity entity = new MockEntity() { PartitionKey = "PK", RowKey = "RK" };

            Assert.AreEqual(EntityStates.Detached, entitySet.GetEntityState(entity),
                "Entity should start detached.");

            entitySet.Attach(entity);

            Assert.AreEqual(EntityStates.Unchanged, entitySet.GetEntityState(entity),
                "Entity should now be attached.");

            entitySet.Update(entity);

            Assert.AreEqual(EntityStates.Modified, entitySet.GetEntityState(entity),
                "Entity should be modified.");

            entitySet.Detach(entity);

            Assert.AreEqual(EntityStates.Detached, entitySet.GetEntityState(entity),
                "Entity should return to detached.");

            entitySet.Update(entity);

            Assert.AreEqual(EntityStates.Modified, entitySet.GetEntityState(entity),
                "Entity should be modified after being detached.");
        }

        [TestMethod]
        [Description("Tests that different queries are returned based on the partition key.")]
        public void GetQueryForPartitionKey()
        {
            MockEntitySet entitySet = new MockEntitySet();
            entitySet.PartitionKey = null;

            IQueryable<MockEntity> entities = entitySet.GetQuery();

            Assert.IsNotNull(entities,
                "Query should not be null.");

            entitySet.PartitionKey = "PK";

            IQueryable<MockEntity> entities2 = entitySet.GetQuery();

            Assert.IsNotNull(entities2,
                "Query with partition key should not be null.");
            Assert.IsInstanceOfType(entities2.Expression, typeof(MethodCallExpression), 
                "Expression should be a method call.");
            Assert.AreEqual("Where", ((MethodCallExpression)entities2.Expression).Method.Name,
                "Expression should be a Where call.");

            Assert.AreNotEqual(entities, entities2,
                "Queries should not be equal.");

            // We stop short of actually enumerating the query as that would try to resolve
            // our mock uri (and fail)
        }

        [TestMethod]
        [Description("Tests that different queries are returned based on the partition key.")]
        public void QueryAndKeysAreOverridable()
        {
            DerivedEntitySet entitySet = new DerivedEntitySet();

            Assert.AreEqual(entitySet.MockQuery, entitySet.GetQuery(),
                "Queries should be equal.");
            Assert.AreEqual(entitySet.MockQuery.ElementType, ((IQueryable)entitySet).ElementType,
                "Query ElementType should equal entity set type.");
            Assert.AreEqual(entitySet.MockQuery.Expression, ((IQueryable)entitySet).Expression,
                "Query Expression should equal entity set expression.");
            Assert.AreEqual(entitySet.MockQuery.Provider, ((IQueryable)entitySet).Provider,
                "Query Provider should equal entity set provider.");

            IEnumerator enumerator = ((IEnumerable)entitySet).GetEnumerator();

            Assert.IsNotNull(enumerator,
                "The enumerator should not be null.");
            Assert.IsFalse(enumerator.MoveNext(),
                "The enumerator should be empty.");

            IEnumerator<MockEntity> enumerator2 = entitySet.GetEnumerator();

            Assert.IsNotNull(enumerator2,
                "The generic enumerator should not be null.");
            Assert.IsFalse(enumerator2.MoveNext(),
                "The generic enumerator should be empty.");

            MockEntity entity = new MockEntity();
            entitySet.Add(entity);

            Assert.AreEqual(entitySet.NewEntityPartitionKey, entity.PartitionKey,
                "PartitionKeys should be equal.");
            Assert.AreEqual(entitySet.NewEntityRowKey, entity.RowKey,
                "RowKeys should be equal.");
        }

        [TestMethod]
        [Description("Tests that the table name can be set via the constructor.")]
        public void TableName()
        {
            MockEntitySet entitySet = new MockEntitySet();

            Assert.AreEqual(typeof(MockEntity).Name, entitySet.TableName,
                "The table name should equal MockEntity.");

            string tableName = "TableName";
            entitySet = new MockEntitySet(tableName);

            Assert.AreEqual(tableName, entitySet.TableName,
                "The table name should equal tableName.");
        }

        private class MockEntitySet : TableEntitySet<MockEntity>
        {
            public MockEntitySet()
                : base(new TableServiceContext("https://mock.table.core.windows.net", new StorageCredentialsAccountAndKey("MockAccountName", new byte[4])))
            {
            }

            public MockEntitySet(string tableName)
                : base(new TableServiceContext("https://mock.table.core.windows.net", new StorageCredentialsAccountAndKey("MockAccountName", new byte[4])), tableName)
            {
            }

            public void Attach(TableEntity entity)
            {
                base.EnsureAttached(entity);
            }

            public void Detach(TableEntity entity)
            {
                base.EnsureDetached(entity);
            }

            public IQueryable<MockEntity> GetQuery()
            {
                return this.CreateQuery();
            }

            public EntityStates GetEntityState(object entity)
            {
                EntityDescriptor descriptor = this.TableServiceContext.GetEntityDescriptor(entity);
                return (descriptor == null) ? EntityStates.Detached : descriptor.State;
            }
        }

        private class DerivedEntitySet : MockEntitySet
        {
            private readonly IQueryable<MockEntity> _query = new MockEntity[0].AsQueryable();

            public IQueryable<MockEntity> MockQuery { get { return this._query; } }

            public string NewEntityPartitionKey { get { return "NewEntityPartitionKey"; } }

            public string NewEntityRowKey { get { return "NewEntityRowKey"; } }   

            protected override IQueryable<MockEntity> CreateQuery()
            {
                return this.MockQuery;
            }

            protected override string GetNewEntityPartitionKey()
            {
                return this.NewEntityPartitionKey;
            }

            protected override string GetNewEntityRowKey()
            {
                return this.NewEntityRowKey;
            }
        }

        private class MockEntity : TableEntity
        {
        }
    }
}
