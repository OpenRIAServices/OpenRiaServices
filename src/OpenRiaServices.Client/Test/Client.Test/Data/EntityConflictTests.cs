extern alias SSmDsClient;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using OpenRiaServices.Client.Test;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel.DataAnnotations;
using DataTests.AdventureWorks.LTS;
using OpenRiaServices.Silverlight.Testing;

namespace OpenRiaServices.Client.Test
{
    using Resource = SSmDsClient::OpenRiaServices.Client.Resource;

    [TestClass]
    public class EntityConflictTests : UnitTestBase
    {
        [TestMethod]
        public void EntityConflict_Resolve()
        {
            TestEntityContainer ec = new TestEntityContainer();
            EntitySet<Product> set = ec.Products;
            Product current = new Product { ProductID = 1, Color = "Red", ProductNumber = "1234" };
            set.LoadEntity(current);
            
            // create a conflict and resolve
            current.Color += "X";
            Product store = new Product { ProductID = 1, Color = "Red", ProductNumber = "4321" };
            EntityConflict conflict = new EntityConflict(current, store, new string[] { "ProductNumber" }, false);
            current.EntityConflict = conflict;
            Assert.AreEqual(EntityState.Modified, current.EntityState);
            var originalState = conflict.OriginalEntity.ExtractState();
            Assert.IsTrue((string)originalState["Color"] == "Red" && (string)originalState["ProductNumber"] == "1234");

            conflict.Resolve();

            // verify that calling Resolve multiple
            // times is a no-op
            conflict.Resolve();
            conflict.Resolve();

            // resolve should update the original state
            originalState = current.GetOriginal().ExtractState();
            Assert.IsTrue((string)originalState["Color"] == "Red" && (string)originalState["ProductNumber"] == "4321");

            // current state should not be modified
            Assert.AreEqual("RedX", current.Color);
            Assert.AreEqual("1234", current.ProductNumber);
            
            // the conflict should be cleared out
            Assert.IsNull(current.EntityConflict);
        }

        [TestMethod]
        public void EntityConflict_Resolve_DeleteConflict()
        {
            // create a delete conflict and attempt to resolve
            Product current = new Product { ProductID = 1, Color = "Red", ProductNumber = "1234" };
            EntityConflict conflict = new EntityConflict(current, null, new string[] { "ProductNumber" }, true);
            Assert.IsTrue(conflict.IsDeleted);
            current.EntityConflict = conflict;

            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                conflict.Resolve();
            }, Resource.EntityConflict_CannotResolveDeleteConflict);
        }

        [TestMethod]
        [Description("Verifies the behavior of EntityConflict when 'IsDeleted' is true.")]
        public void EntityConflict_IsDeleted()
        {
            EntityConflict entityConflict = new EntityConflict(new MockEntity(), null, null, true);
            Assert.AreEqual(true, entityConflict.IsDeleted, "Expected 'IsDeleted' to be true.");
            ExceptionHelper.ExpectInvalidOperationException(
                () => entityConflict.PropertyNames.ToArray() /* touch 'PropertyNames' to get an exception */,
                Resource.EntityConflict_IsDeleteConflict);
        }

        [TestMethod]
        [Description("Verifies the behavior of EntityConflict when 'IsDeleted' is false and other ctor arguments are null.")]
        public void EntityConflict_ArgumentNullExceptions()
        {
            MockEntity entity = new MockEntity();
            IEnumerable<string> propertyNames = new[] { "Property1" };

            ExceptionHelper.ExpectArgumentNullException(
                () => new EntityConflict(entity, null, propertyNames, false),
                "storeEntity");

            ExceptionHelper.ExpectArgumentNullException(
                () => new EntityConflict(null, entity, propertyNames, false),
                "currentEntity");

            ExceptionHelper.ExpectArgumentNullException(
                () => new EntityConflict(entity, entity, null, false),
                "propertyNames");
        }

        [TestMethod]
        [Description("Verifies that 'PropertyNames' reports correct information.")]
        public void EntityConflict_PropertyNames()
        {
            string[] propertyNames = new[] { "Property1", "Property2" };
            EntityConflict entityConflict = new EntityConflict(new MockEntity(), new MockEntity(), propertyNames, false);

            Assert.AreEqual(false, entityConflict.IsDeleted, "Expected 'IsDeleted' to be false.");
            Assert.IsNotNull(entityConflict.PropertyNames, "Expected 'PropertyNames' to not be null.");
            Assert.AreEqual(2, entityConflict.PropertyNames.Count(), "Expected 2 properties in 'PropertyNames'.");

            string[] confictMembers = entityConflict.PropertyNames.ToArray();
            for(int i=0;i<confictMembers.Length;i++)
            {
                Assert.AreEqual(propertyNames[i], confictMembers[i]);
            }
        }

        [TestMethod]
        [Description("Verifies that 'PropertyNames' is a read-only collection.")]
        public void EntityConflict_PropertyNames_ReadOnly()
        {
            string[] propertyNames = new[] { "Property1", "Property2", };
            EntityConflict entityConflict = new EntityConflict(new MockEntity(), new MockEntity(), propertyNames, false);
            Assert.IsInstanceOfType(entityConflict.PropertyNames, typeof(ReadOnlyCollection<string>));
        }

        public class MockEntityContainer : EntityContainer
        {
            public MockEntityContainer()
            {
                CreateEntitySet<MockEntity>(EntitySetOperations.All);
            }

            public EntitySet<MockEntity> MockEntities
            {
                get
                {
                    return this.GetEntitySet<MockEntity>();
                }
            }
        }

        public class MockEntity : Entity
        {
            [Key]
            public int ID { get; set; }
            public string Property1 
            { get; set; }
            public string Property2 { get; set; }
        }
    }
}
