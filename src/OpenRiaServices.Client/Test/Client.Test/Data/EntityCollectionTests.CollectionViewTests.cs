using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using Cities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Description = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.Client.Test
{
    public partial class EntityCollectionTests
    {
        /// <summary>
        /// Verify that EntityCollection works as intended with <see cref="ICollectionView"/> and <see cref="IEditableCollectionView"/> so
        /// it works as intended with WPF bindings.
        /// </summary>
        [TestClass]
        public class CollectionViewTests
        {
            [TestMethod]
            [Description("Tests that create view returns an ICollectionView.")]
            public void ICVF_CreateView()
            {
                EntityCollection<City> entityCollection = CreateEntityCollection();
                Assert.IsNotNull(this.GetICV(entityCollection),
                    "View should not be null.");
            }

            [TestMethod]
            [Description("Tests that calling AddNew and CommitNew on the View adds to the EntityCollection and EntitySet.")]
            public void ICVF_AddNew()
            {
                EntitySet<City> entitySet;
                EntityCollection<City> entityCollection = CreateEntityCollection(out entitySet);
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
                EntityCollection<City> entityCollection = CreateEntityCollection(out entitySet);
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
                EntityCollection<City> entityCollection = CreateEntityCollection();
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
                EntityCollection<City> entityCollection = CreateEntityCollection(out entitySet);
                IEditableCollectionView view = this.GetIECV(entityCollection);

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
            public void ICVF_RemoveAt()
            {
                EntitySet<City> entitySet;
                EntityCollection<City> entityCollection = CreateEntityCollection(out entitySet);
                IEditableCollectionView view = this.GetIECV(entityCollection);

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
            public void ICVF_Enumerate()
            {
                EntityCollection<City> entityCollection = CreateEntityCollection();
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
                EntityCollection<City> entityCollection = CreateEntityCollection(out entitySet);
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

                Assert.IsTrue(view.IsEmpty,
                    "View should be empty after Clear.");

                view.CollectionChanged -= handler;
            }

            [TestMethod]
            [WorkItem(201155)]
            [Description("Tests that the memory leak in ICVF Proxies is fixed.")]
            public void ICVF_MemoryLeakTest()
            {
                // Use NoInline so JIT will not keep temp variable alive 
                [MethodImpl(MethodImplOptions.NoInlining)]
                WeakReference CreateCollectionViewWeakRef(EntityCollection<City> entityCollection)
                    => new WeakReference(this.GetICV(entityCollection));

                EntitySet<City> entitySet;
                EntityCollection<City> entityCollection = CreateEntityCollection(out entitySet);
                WeakReference weakRef = CreateCollectionViewWeakRef(entityCollection);
                System.GC.Collect();
                Assert.IsFalse(weakRef.IsAlive);
            }

            [TestMethod]
            [Description("Tests that IList indexer throws for out-of-range accesses.")]
            public void IList_Indexer_OutOfRange_Throws()
            {
                EntityCollection<City> entityCollection = CreateEntityCollection();
                IList list = entityCollection;

                Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = list[-1]);
                Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = list[0]);

                entityCollection.Add(CreateLocalCity("Out-of-range"));
                Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = list[list.Count]);
            }

            [TestMethod]
            [Description("Tests EntitySet.Clear under sorted view does not throw.")]
            public void SortedView_EntitySetClear_DoesNotThrowNRE()
            {
                EntityCollection<City> entityCollection = CreateEntityCollection(out EntitySet<City> entitySet);
                entityCollection.Add(CreateLocalCity("A"));
                entityCollection.Add(CreateLocalCity("B"));

                ListCollectionView view = (ListCollectionView)this.GetICV(entityCollection);
                view.SortDescriptions.Add(new SortDescription(nameof(City.Name), ListSortDirection.Ascending));
                view.Refresh();

                Exception caught = null;
                try
                {
                    entitySet.Clear();
                }
                catch (Exception exception)
                {
                    caught = exception;
                }

                Assert.IsNull(caught, $"EntitySet.Clear() must not surface NRE through the bound view: {caught}");
                Assert.IsTrue(view.IsEmpty);
            }

            [TestMethod]
            [Description("Tests EntityCollection.Add under sorted view does not throw.")]
            public void SortedView_EntityCollectionAdd_DoesNotThrowNRE()
            {
                EntityCollection<City> entityCollection = CreateEntityCollection();
                entityCollection.Add(CreateLocalCity("Bellevue"));
                entityCollection.Add(CreateLocalCity("Redmond"));

                ListCollectionView view = (ListCollectionView)this.GetICV(entityCollection);
                view.SortDescriptions.Add(new SortDescription(nameof(City.Name), ListSortDirection.Ascending));
                view.Refresh();

                Exception caught = null;
                try
                {
                    entityCollection.Add(CreateLocalCity("Kirkland"));
                }
                catch (Exception exception)
                {
                    caught = exception;
                }

                Assert.IsNull(caught, $"EntityCollection.Add must not surface NRE through AdjustBefore: {caught}");
                Assert.AreEqual(3, view.Count);
            }

            [TestMethod]
            [Description("Tests source set remove under sorted view does not throw.")]
            public void SortedView_SourceSetRemove_DoesNotThrowNRE()
            {
                EntitySet<City> entitySet = null;
                EntityCollection<City> entityCollection = CreateEntityCollection(out entitySet);
                City city = CreateLocalCity("Sammamish");
                entityCollection.Add(city);

                ListCollectionView view = (ListCollectionView)this.GetICV(entityCollection);
                view.SortDescriptions.Add(new SortDescription(nameof(City.Name), ListSortDirection.Ascending));
                view.Refresh();

                Exception caught = null;
                try
                {
                    entitySet.Remove(city);
                }
                catch (Exception exception)
                {
                    caught = exception;
                }

                Assert.IsNull(caught, $"EntitySet.Remove in a sorted EntityCollection view must not NRE: {caught}");
                Assert.IsFalse(view.Contains(city));
            }

            [TestMethod]
            [Description("Tests filter predicate is never invoked with null.")]
            public void FilteredView_FilterPredicate_NeverInvokedWithNull()
            {
                EntityCollection<City> entityCollection = CreateEntityCollection(out EntitySet<City> entitySet);
                entityCollection.Add(CreateLocalCity("A"));
                entityCollection.Add(CreateLocalCity("B"));

                ListCollectionView view = (ListCollectionView)this.GetICV(entityCollection);
                bool sawNull = false;
                view.Filter = o =>
                {
                    if (o is null)
                    {
                        sawNull = true;
                    }

                    return true;
                };
                view.Refresh();

                entityCollection.Add(CreateLocalCity("C"));
                entitySet.Clear();

                foreach (object item in view)
                {
                }

                Assert.IsFalse(sawNull, "Filter predicate must never receive a null item from the EntityCollection.");
            }

            private ICollectionView GetICV(EntityCollection<City> entityCollection)
            {
                return ((ICollectionViewFactory)entityCollection).CreateView();
            }

            private IEditableCollectionView GetIECV(EntityCollection<City> entityCollection)
            {
                return (IEditableCollectionView)this.GetICV(entityCollection);
            }
        }
    }
}
