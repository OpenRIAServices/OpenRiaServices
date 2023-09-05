extern alias SSmDsClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using DataTests.AdventureWorks.LTS;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using TestDomainServices;
using Cities;
using Description = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.Client.Test
{

    [TestClass]
    public class EntityCollectionTests : UnitTestBase
    {
#if HAS_COLLECTIONVIEW
        [TestMethod]
        [Description("Tests that create view returns an ICollectionView.")]
        public void ICVF_CreateView()
        {
            EntityCollection<City> entityCollection = this.CreateEntityCollection();
            Assert.IsNotNull(this.GetICV(entityCollection),
                "View should not be null.");
        }

        [TestMethod]
        [Description("Tests that calling AddNew and CommitNew on the View adds to the EntityCollection and EntitySet.")]
        public void ICVF_AddNew()
        {
            EntitySet<City> entitySet;
            EntityCollection <City> entityCollection = this.CreateEntityCollection(out entitySet);
            IEditableCollectionView view = this.GetIECV(entityCollection);

            City city = (City)view.AddNew();
            Assert.IsTrue(entityCollection.Contains(city),
                "EntityCollection should contain the first entity after AddNew.");
            Assert.IsTrue(entitySet.Contains(city),
                "EntitySet should contain the first entity after AddNew.");
            city.Name = "Burien";
            city.CountyName = "King";
            city.StateName = "WA";
            view.CommitNew();
            Assert.IsTrue(entityCollection.Contains(city),
                "EntityCollection should contain the first entity after CommitNew.");
            Assert.IsTrue(entitySet.Contains(city),
                "EntitySet should contain the first entity after CommitNew.");
        }

        [TestMethod]
        [Description("Tests that calling AddNew on the View adds to the EntityCollection and EntitySet and CancelNew removes both.")]
        public void ICVF_CancelNew()
        {
            EntitySet<City> entitySet;
            EntityCollection<City> entityCollection = this.CreateEntityCollection(out entitySet);
            IEditableCollectionView view = this.GetIECV(entityCollection);

            City city = (City)view.AddNew();
            Assert.IsTrue(entityCollection.Contains(city),
                "EntityCollection should contain the second entity after AddNew.");
            Assert.IsTrue(entitySet.Contains(city),
                "EntitySet should contain the second entity after AddNew.");

            view.CancelNew();
            Assert.IsFalse(entityCollection.Contains(city),
                "EntityCollection should no longer contain the second entity after CancelNew.");
            Assert.IsFalse(entitySet.Contains(city),
                "EntitySet should no longer contain the second entity after CancelNew.");
        }

        [TestMethod]
        [Description("Tests that CanAddNew and CanRemove on the View are always true.")]
        public void ICVF_CanAddOrRemove()
        {
            EntityCollection<City> entityCollection = this.CreateEntityCollection();
            IEditableCollectionView view = this.GetIECV(entityCollection);

            // Justification for these assertions is documented in the EntityCollection source
            Assert.IsTrue(view.CanAddNew,
                "CanAddNew should be true.");
            Assert.IsTrue(view.CanRemove,
                "CanRemove should be true.");
        }

        [TestMethod]
        [Description("Tests that calling Remove on the View remove from the EntityCollection and EntitySet.")]
        public void ICVF_Remove()
        {
            EntitySet<City> entitySet;
            EntityCollection<City> entityCollection = this.CreateEntityCollection(out entitySet);
            IEditableCollectionView view = this.GetIECV(entityCollection);

            City city = (City)view.AddNew();
            city.Name = "Des Moines";
            city.CountyName = "King";
            city.StateName = "WA";
            view.CommitNew();
            entityCollection.Add(this.CreateLocalCity("Normandy Park"));
            entityCollection.Add(this.CreateLocalCity("SeaTac"));

            // This one was added through the view and will be removed from both
            view.Remove(city);
            Assert.IsFalse(entityCollection.Contains(city),
                "EntityCollection should no longer contain the first entity.");
            Assert.IsFalse(entitySet.Contains(city),
                "EntitySet should no longer contain the first entity.");

            // This one was added directly and will only be removed for the collection
            city = entityCollection.ElementAt(1);
            view.Remove(city);
            Assert.IsFalse(entityCollection.Contains(city),
                "EntityCollection should no longer contain the entity at index 1.");
            Assert.IsTrue(entitySet.Contains(city),
                "EntitySet should still contain the entity at index 1.");
        }

        [TestMethod]
        [Description("Tests that calling RemoveAt on the View remove from the EntityCollection and EntitySet.")]
        public void ICVF_RemoveAt()
        {
            EntitySet<City> entitySet;
            EntityCollection<City> entityCollection = this.CreateEntityCollection(out entitySet);
            IEditableCollectionView view = this.GetIECV(entityCollection);

            City city = (City)view.AddNew();
            city.Name = "Des Moines";
            city.CountyName = "King";
            city.StateName = "WA";
            view.CommitNew();
            entityCollection.Add(this.CreateLocalCity("Normandy Park"));
            entityCollection.Add(this.CreateLocalCity("SeaTac"));

            // This one was added through the view and will be removed from both
            city = entityCollection.ElementAt(0);
            view.RemoveAt(0);
            Assert.IsFalse(entityCollection.Contains(city),
                "EntityCollection should no longer contain the entity at index 0.");
            Assert.IsFalse(entitySet.Contains(city),
                "EntitySet should no longer contain the entity at index 0.");

            // This one was added directly and will only be removed for the collection
            city = entityCollection.ElementAt(1);
            view.RemoveAt(1);
            Assert.IsFalse(entityCollection.Contains(city),
                "EntityCollection should no longer contain the entity at index 1.");
            Assert.IsTrue(entitySet.Contains(city),
                "EntitySet should still contain the entity at index 1.");
        }

        [TestMethod]
        [Description("Tests that enumerating the View in its default order matches enumerating the EntityCollection.")]
        public void ICVF_Enumerate()
        {
            EntityCollection<City> entityCollection = this.CreateEntityCollection();
            ICollectionView view = this.GetICV(entityCollection);

            IEnumerator entityCollectionEnumerator = entityCollection.GetEnumerator();
            IEnumerator viewEnumerator = view.GetEnumerator();

            while (entityCollectionEnumerator.MoveNext())
            {
                Assert.IsTrue(viewEnumerator.MoveNext(),
                    "The view enumerator should be able to move to the next item.");
                Assert.AreEqual(entityCollectionEnumerator.Current, viewEnumerator.Current,
                    "Current enumerator items should be equal.");
            }
            Assert.IsFalse(viewEnumerator.MoveNext(),
                "The view enumerator should not have any more items to move to.");
        }

        [TestMethod]
        [Description("Tests that adding to or removing from the EntityCollection is reflected in the View.")]
        public void ICVF_CollectionChanged()
        {
            EntitySet<City> entitySet;
            EntityCollection<City> entityCollection = this.CreateEntityCollection(out entitySet);
            ICollectionView view = this.GetICV(entityCollection);

            NotifyCollectionChangedEventArgs eventArgs = null;
            NotifyCollectionChangedEventHandler handler = (sender, e) =>
            {
                Assert.IsNull(eventArgs,
                    "Only a single event should have occurred.");
                eventArgs = e;
            };

            view.CollectionChanged += handler;

            // Add
            eventArgs = null;
            City city = this.CreateLocalCity("Snoqualmie");
            entityCollection.Add(city);

            Assert.IsNotNull(eventArgs,
                "Event should not be null after adding an entity.");
            Assert.AreEqual(NotifyCollectionChangedAction.Add, eventArgs.Action,
                "Actions should be equal after adding an entity.");
            Assert.IsNotNull(eventArgs.NewItems,
                "NewItems should not be null after adding an entity.");
            Assert.AreEqual(1, eventArgs.NewItems.Count,
                "NewItems count should be 1 after adding an entity.");
            Assert.AreEqual(city, eventArgs.NewItems[0],
                "The new items should be equal.");

            Assert.IsTrue(view.Contains(city),
                "View should contain entity after Add.");

            // Remove
            eventArgs = null;
            city = this.CreateLocalCity("Sammamish");
            entityCollection.Add(city);

            Assert.IsTrue(view.Contains(city),
                "View should contain entity after second Add.");

            eventArgs = null;
            entityCollection.Remove(city);

            Assert.IsNotNull(eventArgs,
                "Event should not be null after removing an entity.");
            Assert.AreEqual(NotifyCollectionChangedAction.Remove, eventArgs.Action,
                "Actions should be equal after removing  an entity.");
            Assert.IsNotNull(eventArgs.OldItems,
                "OldItems should not be null after removing  an entity.");
            Assert.AreEqual(1, eventArgs.OldItems.Count,
                "OldItems count should be 1 after removing  an entity.");
            Assert.AreEqual(city, eventArgs.OldItems[0],
                "The old items should be equal.");

            Assert.IsFalse(view.Contains(city),
                "View should not contain entity after Remove.");

            // Reset
            eventArgs = null;
            entitySet.Clear();

            Assert.IsNotNull(eventArgs,
                "Event should not be null after clearing the EntitySet.");
            Assert.AreEqual(NotifyCollectionChangedAction.Reset, eventArgs.Action,
                "Actions should be equal after clearing the EntitySet.");

            Assert.IsTrue(view.IsEmpty,
                "View should be empty after Clear.");

            view.CollectionChanged -= handler;
        }
#endif

        [TestMethod]
        [Description("EntityAdded and EntityRemoved events should be raised when adding and removing from an EntityCollection")]
        public void EntityEventsOnDirectUpdate()
        {
            EntitySet<City> entitySet;
            EntityCollection<City> entityCollection = this.CreateEntityCollection(out entitySet);

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
            City city = this.CreateLocalCity("Newcastle");
            entityCollection.Add(city);

            Assert.IsTrue(entityCollection.Contains(city),
                "EntityCollection should contain entity after add.");
            Assert.IsNotNull(eventArgs,
                "Event should not be null after adding an entity.");
            Assert.AreEqual(city, eventArgs.Entity,
                "Entities should be equal after adding.");

            // Remove
            eventArgs = null;
            city = this.CreateLocalCity("Medina");
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
            EntityCollection<City> entityCollection = this.CreateEntityCollection(out entitySet);

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
            City city = this.CreateLocalCity("Enumclaw");
            entitySet.Attach(city);

            Assert.IsTrue(entityCollection.Contains(city),
                "EntityCollection should contain entity after add.");
            Assert.IsNotNull(eventArgs,
                "Event should not be null after adding an entity.");
            Assert.AreEqual(city, eventArgs.Entity,
                "Entities should be equal after adding.");

            // Remove
            eventArgs = null;
            city = this.CreateLocalCity("Duvall");
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
#if HAS_COLLECTIONVIEW
        [TestMethod]
        [WorkItem(201155)]
        [Description("Tests that the memory leak in ICVF Proxies is fixed.")]
        public void ICVF_MemoryLeakTest()
        {
            EntitySet<City> entitySet;
            EntityCollection<City> entityCollection = this.CreateEntityCollection(out entitySet);
            WeakReference weakRef = new WeakReference(this.GetICV(entityCollection));
            System.GC.Collect();
            Assert.IsFalse(weakRef.IsAlive);
        }
#endif

        private EntityCollection<City> CreateEntityCollection()
        {
            EntitySet<City> entitySet;
            return this.CreateEntityCollection(EntitySetOperations.All, out entitySet);
        }

        private EntityCollection<City> CreateEntityCollection(out EntitySet<City> entitySet)
        {
            return this.CreateEntityCollection(EntitySetOperations.All, out entitySet);
        }

        private EntityCollection<City> CreateEntityCollection(EntitySetOperations operations, out EntitySet<City> entitySet)
        {
            DynamicEntityContainer container = new DynamicEntityContainer();
            County county = new County { Name = "King", StateName = "WA" };
            entitySet = container.AddEntitySet<City>(operations);
            container.AddEntitySet<County>(operations).Attach(county);
            int count = county.Cities.Count; // Makes sure the collection monitors the EntitySet
            return county.Cities;
        }

        private City CreateLocalCity(string name)
        {
            return new City { Name = name, CountyName = "King", StateName = "WA" };
        }

#if HAS_COLLECTIONVIEW
        private ICollectionView GetICV(EntityCollection<City> entityCollection)
        {
            return ((ICollectionViewFactory)entityCollection).CreateView();
        }

        private IEditableCollectionView GetIECV(EntityCollection<City> entityCollection)
        {
            return (IEditableCollectionView)this.GetICV(entityCollection);
        }
#endif

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
