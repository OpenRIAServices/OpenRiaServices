extern alias SSmDsClient;
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
    public partial class EntitySetTests
    {
#if HAS_COLLECTIONVIEW
        /// <summary>
        /// Verify that EntitySet works as intended with <see cref="ICollectionView"/> and <see cref="IEditableCollectionView"/> so
        /// it works as intended with WPF bindings.
        /// </summary>
        [TestClass]
        public class CollectionViewTests
        {
            [TestMethod]
            [Description("Tests that create view returns an ICollectionView.")]
            public void ICVF_CreateView()
            {
                EntitySet<City> entitySet = CreateEntitySet<City>();
                Assert.IsNotNull(this.GetICV(entitySet),
                    "View should not be null.");
            }

            [TestMethod]
            [Description("Tests that calling AddNew and CommitNew on the View adds to the EntitySet.")]
            public void ICVF_AddNew()
            {
                EntitySet<City> entitySet = CreateEntitySet<City>();
                IEditableCollectionView view = this.GetIECV(entitySet);

                City city = (City)view.AddNew();
                Assert.IsTrue(entitySet.Contains(city),
                    "EntitySet should contain the first entity after AddNew.");
                city.Name = "Tukwila";
                city.CountyName = "King";
                city.StateName = "WA";
                view.CommitNew();
                Assert.IsTrue(entitySet.Contains(city),
                    "EntitySet should contain the first entity after CommitNew.");
            }

            [TestMethod]
            [Description("Tests that calling AddNew on the View adds to the EntitySet and CancelNew removes.")]
            public void ICVF_CancelNew()
            {
                EntitySet<City> entitySet = CreateEntitySet<City>();
                IEditableCollectionView view = this.GetIECV(entitySet);

                City city = (City)view.AddNew();
                Assert.IsTrue(entitySet.Contains(city),
                    "EntitySet should contain the second entity after AddNew.");

                view.CancelNew();
                Assert.IsFalse(entitySet.Contains(city),
                    "EntitySet should no longer contain the second entity after CancelNew.");
            }

            [TestMethod]
            [Description("Tests that CanAddNew and CanRemove on the View are representative of the valid EntitySetOperations.")]
            public void ICVF_CanAddOrRemove()
            {
                EntitySet<City> entitySet = CreateEntitySet<City>(EntitySetOperations.Add | EntitySetOperations.Remove);
                IEditableCollectionView view = this.GetIECV(entitySet);
                Assert.IsTrue(view.CanAddNew,
                    "CanAddNew should be true when Add and Remove are supported.");
                Assert.IsTrue(view.CanRemove,
                    "CanRemove should be true when Add and Remove are supported.");

                // It is a known issue that we cannot differentiate with the current design. Instead
                // we'll see CanAddNew and CanRemove both be true if either should be supported. This
                // leaves the control in the hands of the developers.
                entitySet = CreateEntitySet<City>(EntitySetOperations.Add);
                view = this.GetIECV(entitySet);
                Assert.IsTrue(view.CanAddNew,
                    "CanAddNew should be true when only Add is supported.");
                Assert.IsTrue(view.CanRemove,
                    "CanRemove should be true when only Add is supported.");
                //Assert.IsFalse(view.CanRemove,
                //    "CanRemove should be false when only Add is supported.");

                entitySet = CreateEntitySet<City>(EntitySetOperations.Remove);
                view = this.GetIECV(entitySet);
                Assert.IsTrue(view.CanAddNew,
                    "CanAddNew should be true when only Remove is supported.");
                //Assert.IsFalse(view.CanAddNew,
                //    "CanAddNew should be false when only Remove is supported.");
                Assert.IsTrue(view.CanRemove,
                    "CanRemove should be true when only Remove is supported.");

                entitySet = CreateEntitySet<City>(EntitySetOperations.None);
                view = this.GetIECV(entitySet);
                Assert.IsFalse(view.CanAddNew,
                    "CanAddNew should be true when only neither Add nor Remove is supported.");
                Assert.IsFalse(view.CanRemove,
                    "CanRemove should be true when only neither Add nor Remove is supported.");
            }

            [TestMethod]
            [Description("Tests that calling Remove on the View remove from the EntitySet.")]
            public void ICVF_Remove()
            {
                EntitySet<City> entitySet = CreateEntitySet<City>();
                IEditableCollectionView view = this.GetIECV(entitySet);

                entitySet.Add(CreateLocalCity("Renton"));
                entitySet.Add(CreateLocalCity("Kent"));
                entitySet.Add(CreateLocalCity("Auburn"));

                City city = entitySet.First();
                view.Remove(city);
                Assert.IsFalse(entitySet.Contains(city),
                    "EntitySet should no longer contain the first entity.");
            }

            [TestMethod]
            [Description("Tests that calling RemoveAt on the View remove from the EntitySet.")]
            public void ICVF_RemoveAt()
            {
                EntitySet<City> entitySet = CreateEntitySet<City>();
                IEditableCollectionView view = this.GetIECV(entitySet);

                entitySet.Add(CreateLocalCity("Renton"));
                entitySet.Add(CreateLocalCity("Kent"));
                entitySet.Add(CreateLocalCity("Auburn"));

                City city = entitySet.ElementAt(1);
                view.RemoveAt(1);
                Assert.IsFalse(entitySet.Contains(city),
                    "EntitySet should no longer contain the entity at index 1.");
            }

            [TestMethod]
            [Description("Tests that enumerating the View in its default order matches enumerating the EntitySet.")]
            public void ICVF_Enumerate()
            {
                EntitySet<City> entitySet = CreateEntitySet<City>();
                ICollectionView view = this.GetICV(entitySet);

                IEnumerator entitySetEnumerator = entitySet.GetEnumerator();
                IEnumerator viewEnumerator = view.GetEnumerator();

                while (entitySetEnumerator.MoveNext())
                {
                    Assert.IsTrue(viewEnumerator.MoveNext(),
                        "The view enumerator should be able to move to the next item.");
                    Assert.AreEqual(entitySetEnumerator.Current, viewEnumerator.Current,
                        "Current enumerator items should be equal.");
                }
                Assert.IsFalse(viewEnumerator.MoveNext(),
                    "The view enumerator should not have any more items to move to.");
            }

            [TestMethod]
            [Description("Tests that adding to or removing from the EntitySet is reflected in the View.")]
            public void ICVF_CollectionChanged()
            {
                EntitySet<City> entitySet = CreateEntitySet<City>();
                ICollectionView view = this.GetICV(entitySet);

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
                City city = CreateLocalCity("Maple Valley");
                entitySet.Add(city);

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
                city = CreateLocalCity("Covington");
                entitySet.Add(city);

                Assert.IsTrue(view.Contains(city),
                    "View should contain entity after second Add.");

                eventArgs = null;
                entitySet.Remove(city);

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
                WeakReference CreateCollectionViewWeakRef(EntitySet<City> enitySet)
                    => new WeakReference(this.GetICV(enitySet));

                EntitySet<City> entitySet = CreateEntitySet<City>();
                WeakReference weakRef = CreateCollectionViewWeakRef(entitySet);
                GC.Collect();
                Assert.IsFalse(weakRef.IsAlive);
            }

            private ICollectionView GetICV(EntitySet entitySet)
            {
                return CollectionViewSource.GetDefaultView(entitySet);
            }

            private IEditableCollectionView GetIECV(EntitySet entitySet)
            {
                return (IEditableCollectionView)this.GetICV(entitySet);
            }

        }
#endif
    }
}
