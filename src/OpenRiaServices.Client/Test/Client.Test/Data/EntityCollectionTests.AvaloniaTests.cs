#if NET
using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Collections;
using Avalonia.Controls;
using Cities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Description = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.Client.Test
{
    public partial class EntityCollectionTests
    {
        /// <summary>
        /// Verify that EntityCollection works as intended with <see cref="ItemsSourceView"/> and <see cref="DataGridCollectionView"/> so
        /// it works as intended with WPF bindings.
        /// </summary>
        [TestClass]
        public class AvaloniaTests
        {
            [TestMethod]
            [Description("Tests that create view returns an ItemsSourceView.")]
            public void CreateView()
            {
                EntityCollection<City> entityCollection = CreateEntityCollection();
                var view = ItemsSourceView.GetOrCreate(entityCollection);

                Assert.IsNotNull(view, "View should not be null.");
                Assert.AreNotSame(ItemsSourceView<City>.Empty, view, "View should not be null.");
            }

            [TestMethod]
            [Description("Tests that calling AddNew and CommitNew on the View adds to the EntityCollection and EntitySet.")]
            public void AddNew()
            {
                EntitySet<City> entitySet;
                EntityCollection<City> entityCollection = CreateEntityCollection(out entitySet);
                DataGridCollectionView view = new(entityCollection);

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
            public void CancelNew()
            {
                EntitySet<City> entitySet;
                EntityCollection<City> entityCollection = CreateEntityCollection(out entitySet);
                DataGridCollectionView view = new(entityCollection);

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
            public void CanAddOrRemove()
            {
                EntityCollection<City> entityCollection = CreateEntityCollection();
                DataGridCollectionView view = new(entityCollection);

                // Justification for these assertions is documented in the EntityCollection source
                Assert.IsTrue(view.CanAddNew,
                    "CanAddNew should be true.");
                Assert.IsTrue(view.CanRemove,
                    "CanRemove should be true.");
            }

            [TestMethod]
            [Description("Tests that calling Remove on the View remove from the EntityCollection and EntitySet.")]
            public void Remove()
            {
                EntitySet<City> entitySet;
                EntityCollection<City> entityCollection = CreateEntityCollection(out entitySet);
                DataGridCollectionView view = new(entityCollection);

                City city = (City)view.AddNew();
                city.Name = "Des Moines";
                city.CountyName = "King";
                city.StateName = "WA";
                view.CommitNew();
                entityCollection.Add(CreateLocalCity("Normandy Park"));
                entityCollection.Add(CreateLocalCity("SeaTac"));

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
            public void RemoveAt()
            {
                EntitySet<City> entitySet;
                EntityCollection<City> entityCollection = CreateEntityCollection(out entitySet);
                DataGridCollectionView view = new(entityCollection);

                City city = (City)view.AddNew();
                city.Name = "Des Moines";
                city.CountyName = "King";
                city.StateName = "WA";
                view.CommitNew();
                entityCollection.Add(CreateLocalCity("Normandy Park"));
                entityCollection.Add(CreateLocalCity("SeaTac"));

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
            public void Enumerate()
            {
                EntityCollection<City> entityCollection = CreateEntityCollection();
                ItemsSourceView view = ItemsSourceView.GetOrCreate(entityCollection);

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
            public void CollectionChanged()
            {
                EntitySet<City> entitySet;
                EntityCollection<City> entityCollection = CreateEntityCollection(out entitySet);
                ItemsSourceView view = ItemsSourceView.GetOrCreate(entityCollection);

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
                City city = CreateLocalCity("Snoqualmie");
                entityCollection.Add(city);

                Assert.IsNotNull(eventArgs,
                    "Event should not be null after adding an entity.");
                Assert.AreEqual(NotifyCollectionChangedAction.Add, eventArgs.Action,
                    "Actions should be equal after adding an entity.");
                Assert.IsNotNull(eventArgs.NewItems,
                    "NewItems should not be null after adding an entity.");
                Assert.HasCount(1, eventArgs.NewItems,
                    "NewItems count should be 1 after adding an entity.");
                Assert.AreEqual(city, eventArgs.NewItems[0],
                    "The new items should be equal.");

                Assert.IsTrue(view.Contains(city),
                    "View should contain entity after Add.");

                // Remove
                eventArgs = null;
                city = CreateLocalCity("Sammamish");
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
                Assert.HasCount(1, eventArgs.OldItems,
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

                Assert.HasCount(0, view,
                    "View should be empty after Clear.");

                view.CollectionChanged -= handler;
            }

            [TestMethod]
            [WorkItem(201155)]
            [Description("Tests that the memory leak in ICVF Proxies is fixed.")]
            public void MemoryLeakTest()
            {
                // Use NoInline so JIT will not keep temp variable alive 
                [MethodImpl(MethodImplOptions.NoInlining)]
                WeakReference CreateCollectionViewWeakRef(EntityCollection<City> entityCollection)
                    => new WeakReference(ItemsSourceView.GetOrCreate(entityCollection));

                EntitySet<City> entitySet;
                EntityCollection<City> entityCollection = CreateEntityCollection(out entitySet);
                WeakReference weakRef = CreateCollectionViewWeakRef(entityCollection);
                System.GC.Collect();
                Assert.IsFalse(weakRef.IsAlive);
            }
        }
    }
}
#endif
