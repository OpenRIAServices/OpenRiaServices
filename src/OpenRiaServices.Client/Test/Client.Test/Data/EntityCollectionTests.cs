using System;
using System.ComponentModel;
using System.Linq;
using Cities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using Description = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.Client.Test
{
    [TestClass]
    public partial class EntityCollectionTests : UnitTestBase
    {
        [TestMethod]
        [Description("EntityAdded and EntityRemoved events should be raised when adding and removing from an EntityCollection")]
        public void EntityEventsOnDirectUpdate()
        {
            EntitySet<City> entitySet;
            EntityCollection<City> entityCollection = CreateEntityCollection(out entitySet);

            EntityCollectionChangedEventArgs<City> eventArgs = null;
            EventHandler<EntityCollectionChangedEventArgs<City>> handler = (sender, e) =>
            {
                Assert.IsNull(eventArgs,
                    "Only a single event should have occurred.");
                eventArgs = e;
            };

            entityCollection.EntityAdded += handler;
            entityCollection.EntityRemoved += handler;

            // Add
            eventArgs = null;
            City city = CreateLocalCity("Newcastle");
            entityCollection.Add(city);

            Assert.IsTrue(entityCollection.Contains(city),
                "EntityCollection should contain entity after add.");
            Assert.IsNotNull(eventArgs,
                "Event should not be null after adding an entity.");
            Assert.AreEqual(city, eventArgs.Entity,
                "Entities should be equal after adding.");

            // Remove
            eventArgs = null;
            city = CreateLocalCity("Medina");
            entityCollection.Add(city);

            Assert.IsTrue(entityCollection.Contains(city),
                "EntityCollection should contain entity after second add.");

            eventArgs = null;
            entityCollection.Remove(city);

            Assert.IsFalse(entityCollection.Contains(city),
                "EntityCollection should not contain entity after remove.");
            Assert.IsNotNull(eventArgs,
                "Event should not be null after removing an entity.");
            Assert.AreEqual(city, eventArgs.Entity,
                "Entities shoudl be equal after removing.");
        }

        [TestMethod]
        [Description("EntityAdded and EntityRemoved events should be raised when adding and removing from an EntitySet")]
        public void EntityEventsOnIndirectUpdate()
        {
            EntitySet<City> entitySet;
            EntityCollection<City> entityCollection = CreateEntityCollection(out entitySet);

            EntityCollectionChangedEventArgs<City> eventArgs = null;
            EventHandler<EntityCollectionChangedEventArgs<City>> handler = (sender, e) =>
            {
                Assert.IsNull(eventArgs,
                    "Only a single event should have occurred.");
                eventArgs = e;
            };

            entityCollection.EntityAdded += handler;
            entityCollection.EntityRemoved += handler;

            // Add
            eventArgs = null;
            City city = CreateLocalCity("Enumclaw");
            entitySet.Attach(city);

            Assert.IsTrue(entityCollection.Contains(city),
                "EntityCollection should contain entity after add.");
            Assert.IsNotNull(eventArgs,
                "Event should not be null after adding an entity.");
            Assert.AreEqual(city, eventArgs.Entity,
                "Entities should be equal after adding.");

            // Remove
            eventArgs = null;
            city = CreateLocalCity("Duvall");
            entitySet.Attach(city);

            Assert.IsTrue(entityCollection.Contains(city),
                "EntityCollection should contain entity after second add.");

            eventArgs = null;
            entitySet.Detach(city);

            Assert.IsFalse(entityCollection.Contains(city),
                "EntityCollection should not contain entity after remove.");
            Assert.IsNotNull(eventArgs,
                "Event should not be null after removing an entity.");
            Assert.AreEqual(city, eventArgs.Entity,
                "Entities shoudl be equal after removing.");
        }


        [TestMethod]
        [Description("Tests that ListCollectionViewProxy returns correct index")]
        public void IList_Add_And_Index()
        {
            EntitySet<City> entitySet;
            EntityCollection<City> entityCollection = CreateEntityCollection(out entitySet);
            System.Collections.IList list = entityCollection;

            for (int i = 0; i < 3; ++i)
            {
                var city = new City() { ZoneID = i };

                Assert.DoesNotContain(city, list);
                int idx = list.Add(city);

                Assert.AreEqual(i, idx);
                Assert.AreSame(city, list[idx]);
                Assert.AreSame(city, entityCollection[idx]);
                Assert.Contains(city, list);
                Assert.IsTrue(entityCollection.Contains(city));
            }
        }

        private static EntityCollection<City> CreateEntityCollection()
        {
            EntitySet<City> entitySet;
            return CreateEntityCollection(EntitySetOperations.All, out entitySet);
        }

        private static EntityCollection<City> CreateEntityCollection(out EntitySet<City> entitySet)
        {
            return CreateEntityCollection(EntitySetOperations.All, out entitySet);
        }

        private static EntityCollection<City> CreateEntityCollection(EntitySetOperations operations, out EntitySet<City> entitySet)
        {
            DynamicEntityContainer container = new DynamicEntityContainer();
            County county = new County { Name = "King", StateName = "WA" };
            entitySet = container.AddEntitySet<City>(operations);
            container.AddEntitySet<County>(operations).Attach(county);
            int count = county.Cities.Count; // Makes sure the collection monitors the EntitySet
            return county.Cities;
        }

        private static City CreateLocalCity(string name)
        {
            return new City { Name = name, CountyName = "King", StateName = "WA" };
        }

        /// <summary>
        /// An dynamic EntityContainer class that allows external configuration of
        /// EntitySets for testing purposes.
        /// </summary>
        private class DynamicEntityContainer : EntityContainer
        {
            public EntitySet<T> AddEntitySet<T>(EntitySetOperations operations) where T : Entity
            {
                base.CreateEntitySet<T>(operations);
                return GetEntitySet<T>();
            }
        }
    }
}
