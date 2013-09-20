using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Principal;
using System.ServiceModel.DomainServices.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace Microsoft.ServiceModel.DomainServices.WindowsAzure.Test
{
    [TestClass]
    public class TableDomainServiceTests
    {
        [TestMethod]
        [Description("Tests that the PartitionKey is passed through to the EntityContext")]
        public void PartitionKey()
        {
            TDST_DomainService domainService = this.CreateDomainService(DomainOperationType.Metadata);

            Assert.AreEqual(domainService.MockPartitionKey, domainService.MockEntityContext.PartitionKey,
                "PartitionKeys should be equal.");
        }

        [TestMethod]
        [Description("Tests that CreateEntityContext is called just one time to create the EntityContext")]
        public void CreateEntityContext()
        {
            TDST_DomainService domainService = this.CreateDomainService(DomainOperationType.Metadata);

            Assert.AreEqual(0, domainService.CreateEntityContextCount,
                "The entity context should not have been created yet.");

            Assert.IsNotNull(domainService.MockEntityContext,
                "The entity context should not be null.");

            Assert.AreEqual(1, domainService.CreateEntityContextCount,
                "The entity context should have been created.");

            Assert.IsNotNull(domainService.MockEntityContext,
                "The entity context should still not be null.");

            Assert.AreEqual(1, domainService.CreateEntityContextCount,
                "The entity context count should still be at 1.");
        }

        [TestMethod]
        [Description("Tests that a supported query can retrieve data")]
        public void SupportedQuery()
        {
            TDST_DomainService domainService = this.CreateDomainService(DomainOperationType.Query);

            IQueryable query = new TDST_MockEntity[0].AsQueryable().Where(e => e.RowKey != "1");
            QueryDescription qd = new QueryDescription(domainService.MockDescription.GetQueryMethod("GetEntities"), new object[0], false, query);

            IEnumerable<ValidationResult> validationErrors;
            int totalCount;

            IEnumerable entities = domainService.Query(qd, out validationErrors, out totalCount);

            Assert.IsNotNull(entities,
                "Query result should not be null.");
            Assert.AreEqual(4, entities.Cast<TDST_MockEntity>().Count(),
                "There should be 4 entities in the filtered collection.");
            Assert.AreEqual("5", entities.Cast<TDST_MockEntity>().Last().RowKey,
                "The RowKey of the final entity should be 5.");
        }

        [TestMethod]
        [Description("Tests that a partially supported query can retrieve data")]
        public void PartiallySupportedQuery()
        {
            TDST_DomainService domainService = this.CreateDomainService(DomainOperationType.Query);

            IQueryable query = new TDST_MockEntity[0].AsQueryable().Where(e => e.RowKey != "1").OrderByDescending(e => e.RowKey);
            QueryDescription qd = new QueryDescription(domainService.MockDescription.GetQueryMethod("GetEntities"), new object[0], false, query);

            IEnumerable<ValidationResult> validationErrors;
            int totalCount;

            IEnumerable entities = domainService.Query(qd, out validationErrors, out totalCount);

            Assert.IsNotNull(entities,
                "Query result should not be null.");
            Assert.AreEqual(4, entities.Cast<TDST_MockEntity>().Count(),
                "There should be 4 entities in the filtered collection.");
            Assert.AreEqual("2", entities.Cast<TDST_MockEntity>().Last().RowKey,
                "The RowKey of the final entity should be 2.");
            // Not an official contract, but the serializer requires entities to be pre-enumerated
            Assert.IsInstanceOfType(entities, typeof(TDST_MockEntity[]),
                "Entities should be a TDST_MockEntity[].");
        }

        [TestMethod]
        [Description("Tests that an unsupported query can retrieve data")]
        public void UnsupportedQuery()
        {
            TDST_DomainService domainService = this.CreateDomainService(DomainOperationType.Query);

            IQueryable query = new TDST_MockEntity[0].AsQueryable().OrderByDescending(e => e.RowKey);
            QueryDescription qd = new QueryDescription(domainService.MockDescription.GetQueryMethod("GetEntities"), new object[0], false, query);

            IEnumerable<ValidationResult> validationErrors;
            int totalCount;

            IEnumerable entities = domainService.Query(qd, out validationErrors, out totalCount);

            Assert.IsNotNull(entities,
                "Query result should not be null.");
            Assert.AreEqual(5, entities.Cast<TDST_MockEntity>().Count(),
                "There should be 5 entities in the filtered collection.");
            Assert.AreEqual("1", entities.Cast<TDST_MockEntity>().Last().RowKey,
                "The RowKey of the final entity should be 1.");
            // Not an official contract, but the serializer requires entities to be pre-enumerated
            Assert.IsInstanceOfType(entities, typeof(TDST_MockEntity[]),
                "Entities should be a TDST_MockEntity[].");
        }

        [TestMethod]
        [Description("Tests that an change can be submitted")]
        public void SubmitChanges()
        {
            TDST_DomainService domainService = this.CreateDomainService(DomainOperationType.Submit);

            TDST_MockEntity entity = new TDST_MockEntity { PartitionKey = "PK", RowKey = "1" };
            ChangeSet changeSet = new ChangeSet(new[] { new ChangeSetEntry(1, entity, null, DomainOperation.Update) { HasMemberChanges = true } });

            Assert.IsTrue(domainService.Submit(changeSet),
                "Batched changes should have been submitted successfully.");
        }

        private TDST_DomainService CreateDomainService(DomainOperationType operation)
        {
            TDST_DomainService domainService = new TDST_DomainService();

            domainService.Initialize(new DomainServiceContext(new MockDataService(new MockUser("mock")), operation));

            return domainService;
        }

        private class MockDataService : IServiceProvider
        {
            private readonly IPrincipal user;

            public MockDataService(IPrincipal user)
            {
                this.user = user;
            }

            #region IServiceProvider Members

            public object GetService(Type serviceType)
            {
                if (serviceType == typeof(IPrincipal))
                {
                    return this.user;
                }
                return null;
            }

            #endregion
        }

        private class MockUser : IPrincipal, IIdentity
        {
            private readonly string _name;
            private readonly IEnumerable<string> _roles;

            public MockUser(string name)
                : this(name, new string[0])
            {
            }

            public MockUser(string name, IEnumerable<string> roles)
            {
                this._name = name;
                this._roles = roles;
            }

            #region IPrincipal Members

            public IIdentity Identity
            {
                get { return this; }
            }

            public bool IsInRole(string role)
            {
                return this._roles.Contains(role);
            }

            #endregion

            #region IIdentity Members

            public string AuthenticationType
            {
                get { return "Mock"; }
            }

            public bool IsAuthenticated
            {
                get { return !string.IsNullOrEmpty(this._name); }
            }

            public string Name
            {
                get { return this._name; }
            }

            #endregion
        }
    }

    public class TDST_MockEntity : TableEntity { }

    public class TDST_EntitySet<TEntity> : TableEntitySet<TEntity> where TEntity : TableEntity
    {
        public TDST_EntitySet(TableServiceContext context)
            : base(context)
        {
        }

        protected override IQueryable<TEntity> CreateQuery()
        {
            return new[] 
            {
                this.CreateEntity("1"),
                this.CreateEntity("2"),
                this.CreateEntity("3"),
                this.CreateEntity("4"),
                this.CreateEntity("5"),
            }.AsQueryable();
        }

        private TEntity CreateEntity(string rowKey)
        {
            TEntity entity = Activator.CreateInstance<TEntity>();
            entity.PartitionKey = this.GetNewEntityPartitionKey();
            entity.RowKey = rowKey;
            return entity;
        }
    }

    public class TDST_EntityContext : TableEntityContext
    {
        public TDST_EntityContext()
            : base("https://mock.table.core.windows.net", new StorageCredentialsAccountAndKey("MockAccountName", new byte[4]))
        {
            this.SetEntitySet<TDST_MockEntity>(new TDST_EntitySet<TDST_MockEntity>(this));
        }

        public TableEntitySet<TDST_MockEntity> MockEntities
        {
            get { return this.GetEntitySet<TDST_MockEntity>(); }
        }

        protected override void EnsureTableExists(string tableName)
        {
            // overridden to avoid creating the table
        }
    }

    public class TDST_DomainService : TableDomainService<TDST_EntityContext>
    {
        public IQueryable<TDST_MockEntity> GetEntities() 
        {
            return this.EntityContext.MockEntities;
        }

        public void UpdateEntity(TDST_MockEntity entity)
        {
            // Doing nothing. Actually taking this action would result in a
            // non-null changeset that we're not prepared to test

            // this.EntityContext.MockEntities.Update(entity);
        }

        public int CreateEntityContextCount { get; set; }

        protected override TDST_EntityContext CreateEntityContext()
        {
            this.CreateEntityContextCount++;
            return base.CreateEntityContext();
        }

        public string MockPartitionKey
        {
            get { return this.PartitionKey; }
        }

        public TDST_EntityContext MockEntityContext
        {
            get { return this.EntityContext; }
        }

        public DomainServiceDescription MockDescription
        {
            get { return this.ServiceDescription; }
        }
    }
}
