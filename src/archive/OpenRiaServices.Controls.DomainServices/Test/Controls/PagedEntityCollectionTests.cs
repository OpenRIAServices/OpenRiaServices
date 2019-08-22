using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using Cities;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Controls.DomainServices.Test
{
    /// <summary>
    /// Tests the <see cref="PagedEntityCollection"/> class.
    /// </summary>
    [TestClass]
    public class PagedEntityCollectionTests : ViewTestBase
    {
        [TestMethod]
        [WorkItem(781815)]
        [Description("When a CollectionChanged.Reset event occurs, and entities remain in the source set, they also remain in the PagedEntityCollection")]
        public void ResetEventRetainsEntitiesStillInSet()
        {
            PagedEntityCollection pagedCollection = new PagedEntityCollection(p => { return true; });
            MockEntitySet cities = LoadCities(false, pagedCollection);

            cities.ResetCollection();
            Assert.AreEqual<int>(cities.Count, pagedCollection.Count, "pagedCollection.Count should match cities.Count after resetting the collection");
        }

        [TestMethod]
        [WorkItem(781815)]
        [Description("When a CollectionChanged.Reset event occurs, and entities were cleared from the source set, they are also removed from the PagedEntityCollection")]
        public void ResetRemovesClearedEntities()
        {
            PagedEntityCollection pagedCollection = new PagedEntityCollection(p => { return true; });
            MockEntitySet cities = LoadCities(true, pagedCollection);

            // This will clear the set and raise a Reset event
            // This particular scenario is what led to the bug reported
            cities.Clear();
            Assert.AreEqual<int>(0, pagedCollection.Count, "pagedCollection.Count should be 0 after clearing the cities set");
        }

        [TestMethod]
        [WorkItem(781815)]
        [Description("When a CollectionChanged.Reset event occurs, and some entities are removed from the source set, they are also removed from the PagedEntityCollection, but others remain")]
        public void ResetRemovesRemovedEntities()
        {
            PagedEntityCollection pagedCollection = new PagedEntityCollection(p => { return true; });
            MockEntitySet cities = LoadCities(false, pagedCollection);

            Entity toRemove = pagedCollection[0];

            cities.Remove(toRemove);
            Assert.AreEqual<int>(cities.Count + 1, pagedCollection.Count, "pagedCollection.Count should be one more than cities.Count after removing the city from the cities set");
            Assert.IsTrue(pagedCollection.Contains(toRemove), "pagedCollection should still contain the city removed from the cities set until the Reset is raised");

            cities.ResetCollection();
            Assert.AreEqual<int>(cities.Count, pagedCollection.Count, "pagedCollection.Count should match cities.Count after resetting the collection");
            Assert.IsFalse(pagedCollection.Contains(toRemove), "pagedCollection should no longer contain the city removed from the cities set after resetting the collection");
        }

        [TestMethod]
        [WorkItem(781815)]
        [Description("When a CollectionChanged.Reset event occurs, and some entities are added to the source set, they are not added to the PagedEntityCollection if they weren't tracked")]
        public void ResetIgnoresUntrackedAddedEntities()
        {
            PagedEntityCollection pagedCollection = new PagedEntityCollection(p => { return true; });
            MockEntitySet cities = LoadCities(false, pagedCollection);

            City toAdd = new City
            {
                Name = "New City",
                StateName = "ST",
                CountyName = "County"
            };

            cities.Add(toAdd);
            Assert.AreEqual<int>(cities.Count - 1, pagedCollection.Count, "pagedCollection.Count should be one less than cities.Count after adding the city to the cities set");
            Assert.IsFalse(pagedCollection.Contains(toAdd), "pagedCollection should not contain the city added to the cities set before the Reset is raised");

            cities.ResetCollection();
            Assert.AreEqual<int>(cities.Count - 1, pagedCollection.Count, "pagedCollection.Count should be one less than cities.Count after resetting the collection");
            Assert.IsFalse(pagedCollection.Contains(toAdd), "pagedCollection should not contain the city added to the cities set after resetting the collection");
        }

        [TestMethod]
        [WorkItem(781815)]
        [Description("When a CollectionChanged.Reset event occurs, and some entities are added to the source set, they are added to the PagedEntityCollection if they were tracked")]
        public void ResetAddsTrackedAddedEntities()
        {
            PagedEntityCollection pagedCollection = new PagedEntityCollection(p => { return true; });
            MockEntitySet cities = LoadCities(true, pagedCollection);

            // Remove one entity that won't get resurrected
            Entity toRemove = pagedCollection[1];
            cities.Remove(toRemove);

            // And remove another entity that will get resurrected
            Entity toRemoveAndAdd = pagedCollection[0];
            cities.Remove(toRemoveAndAdd);

            // Turn off the events so that the Add isn't raised
            cities.RaiseCollectionChangedEvents = false;
            cities.Add(toRemoveAndAdd);

            // Raise the Reset event to pick up the city that was added back
            cities.ResetCollection();
            Assert.AreEqual<int>(cities.Count, pagedCollection.Count, "pagedCollection.Count should equal the cities count after adding the city back and resetting the collection");
            Assert.IsTrue(pagedCollection.Contains(toRemoveAndAdd), "The city should be back in the pagedCollection after adding it back and resetting the collection");
            Assert.IsFalse(pagedCollection.Contains(toRemove), "The city that was removed but not added back should not be in the pagedCollection");
        }

        [TestMethod]
        [WorkItem(781815)]
        [Description("When a CollectionChanged.Reset event occurs, a Reset event is raised from the PagedEntityCollection as well")]
        public void ResetEventRelayed()
        {
            PagedEntityCollection pagedCollection = new PagedEntityCollection(p => { return true; });
            MockEntitySet cities = LoadCities(false, pagedCollection);

            // Remove an item
            pagedCollection.RemoveAt(0);

            // Add an item
            pagedCollection.Add(new City
                {
                    Name = "Added City",
                    StateName = "ST",
                    CountyName = "County"
                });

            this.AssertCollectionChanged(
                () => cities.ResetCollection(),
                pagedCollection,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset),
                "when resetting the collection.");
        }

        [TestMethod]
        [WorkItem(885679)]
        [Description("When a CollectionChanged.Remove event occurs, a Remove event is raised from the PagedEntityCollection as well")]
        public void RemoveEventRelayed()
        {
            PagedEntityCollection pagedCollection = new PagedEntityCollection(p => { return true; });
            MockEntitySet cities = LoadCities(true, pagedCollection);

            // Remove an item
            pagedCollection.RemoveAt(0);

            // Add an item
            pagedCollection.Add(new City
            {
                Name = "Added City",
                StateName = "ST",
                CountyName = "County"
            });

            City removedCity = pagedCollection.OfType<City>().First();
            this.AssertCollectionChanged(
                () => cities.Remove(removedCity),
                pagedCollection,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedCity, 0),
                "when removing a city.");
        }

        [TestMethod]
        [WorkItem(885679)]
        [Description("When a CollectionChanged.Add event occurs, an Add event is raised from the PagedEntityCollection as well")]
        public void AddEventRelayed()
        {
            PagedEntityCollection pagedCollection = new PagedEntityCollection(p => { return true; });
            MockEntitySet cities = LoadCities(true, pagedCollection);

            // Remove an item
            pagedCollection.RemoveAt(0);

            // Add an item
            pagedCollection.Add(new City
            {
                Name = "Added City",
                StateName = "ST",
                CountyName = "County"
            });

            City removedCity = pagedCollection.OfType<City>().First();
            cities.Remove(removedCity);

            // A new city should not be added to the collection
            City addedCity = new City
            {
                Name = "Added City 2",
                StateName = "ST",
                CountyName = "County"
            };

            this.AssertCollectionChanged(
                () => cities.Add(addedCity),
                pagedCollection,
                (NotifyCollectionChangedEventArgs)null,
                "when adding a new city.");

            // A restored city should be re-added to the collection
            this.AssertCollectionChanged(
                () => cities.Add(removedCity),
                pagedCollection,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, removedCity, pagedCollection.Count - 1),
                "when re-adding a removed city.");
        }

        [TestMethod]
        [WorkItem(749700)]
        [Description("When entities are loaded twice, they should not be repeated in the PagedEntityCollection")]
        public void RepeatedEntitiesNoDuplicated()
        {
            PagedEntityCollection pagedCollection = new PagedEntityCollection(p => { return true; });

            // This will load the cities into the collection the first time
            MockEntitySet cities = LoadCities(false, pagedCollection);

            // Now we'll load the cities into the collection a second time
            pagedCollection.BeginLoad();
            foreach (City city in cities)
            {
                pagedCollection.AddLoadedEntity(city);
            }
            pagedCollection.CompleteLoad();

            Assert.AreEqual(cities.Count, pagedCollection.Count, "The counts should still match");
        }

        /// <summary>
        /// Create a <see cref="MockEntitySet"/> with cities loaded into it.  Load those cities
        /// into the <paramref name="pagedCollection"/>.
        /// </summary>
        /// <param name="raiseCollectionChangedEvents">Whether or not the <see cref="MockEntitySet"/> should raise
        /// <see cref="INotifyCollectionChanged.CollectionChanged"/> events.</param>
        /// <param name="pagedCollection">The <see cref="PagedEntityCollection"/> to hook up to
        /// the loaded <see cref="MockEntitySet"/>.  If <c>null</c>, it will be instantiated.
        /// The cities loaded into the <see cref="MockEntitySet"/> will also be loaded into
        /// the <paramref name="pagedCollection"/>.</param>
        /// <returns>The <see cref="MockEntitySet"/> with cities loaded into it.</returns>
        private MockEntitySet LoadCities(bool raiseCollectionChangedEvents, PagedEntityCollection pagedCollection)
        {
            // Get a mock entity set of cities
            MockEntitySet cities = new MockEntitySet(raiseCollectionChangedEvents);

            // Load a couple of cities into the set
            cities.Add(new City
            {
                Name = "First City",
                StateName = "ST",
                CountyName = "County"
            });

            cities.Add(new City
            {
                Name = "Second City",
                StateName = "ST",
                CountyName = "County"
            });

            // Create a paged collection for the entity set
            pagedCollection.BackingEntitySet = cities;

            // Process the cities as loaded entities
            pagedCollection.BeginLoad();
            foreach (City city in cities)
            {
                pagedCollection.AddLoadedEntity(city);
            }
            pagedCollection.CompleteLoad();

            return cities;
        }

        private class MockEntitySet : EntitySet
        {
            private IList _internalList;

            public bool RaiseCollectionChangedEvents { get; set; }

            public MockEntitySet(bool raiseCollectionChangedEvents)
                : base(typeof(City))
            {
                this.RaiseCollectionChangedEvents = raiseCollectionChangedEvents;

                MockEntityContainer container = new MockEntityContainer();
                container.AddEntitySet(this, EntitySetOperations.All);
            }

            protected override IList CreateList()
            {
                this._internalList = new List<City>();
                return this._internalList;
            }

            protected override Entity CreateEntity()
            {
                return new City();
            }

            protected override void OnCollectionChanged(NotifyCollectionChangedAction action, object affectedObject, int index)
            {
                if (this.RaiseCollectionChangedEvents)
                {
                    base.OnCollectionChanged(action, affectedObject, index);
                }
            }

            public void Add(City city)
            {
                // Cannot use base.Add because the entity set doesn't support Add operations
                // because we have no way to specify the supported operations without an EntityContainer
                // creating an initializing the entity set
                base.Attach(city);
            }

            /// <summary>
            /// Force a <see cref="INotifyCollectionChanged.CollectionChanged"/> event on the collection.
            /// </summary>
            /// <param name="action">The action to specify on the event.</param>
            /// <param name="affectedObject">The object affected by the action on the event.</param>
            /// <param name="index">The index of the affected object on the event.</param>
            public void RaiseCollectionChanged(NotifyCollectionChangedAction action, object affectedObject, int index)
            {
                base.OnCollectionChanged(action, affectedObject, index);
            }

            public void Remove(City city)
            {
                // Cannot use base.Remove because the entity set doesn't support Remove operations
                // because we have no way to specify the supported operations without an EntityContainer
                // creating an initializing the entity set
                base.Detach(city);
            }

            /// <summary>
            /// Force a <see cref="NotifyCollectionChangedAction.Reset"/> event on the collection.
            /// </summary>
            public void ResetCollection()
            {
                base.OnCollectionChanged(NotifyCollectionChangedAction.Reset, null, -1);
            }
        }
    }
}
