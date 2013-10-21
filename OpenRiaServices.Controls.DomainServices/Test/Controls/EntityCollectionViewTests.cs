using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using OpenRiaServices.DomainServices.Client.Test;
using DataTests.AdventureWorks.LTS;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.Controls.DomainServices.Test
{
    [TestClass]
    public class EntityCollectionViewTests : UnitTestBase
    {
        #region Construction, Enumeration, Support and Delegation

        [TestMethod]
        [Description("Cannot construct an EntityCollectionView with a null source")]
        public void CannotConstructWithNullSource()
        {
            ExceptionHelper.ExpectArgumentNullExceptionStandard(() => new EntityCollectionView<Product>(null), "source");
        }

        [TestMethod]
        [Description("Construct an EntityCollectionView and enumerate its data")]
        public void EnumeratorReturnsProvidedEnumerable()
        {
            Product p1 = new Product { Name = "Test" };
            IEnumerable<Product> products = new Product[] { p1 };

            EntityCollectionView<Product> view = new EntityCollectionView<Product>(products);
            Assert.IsTrue(view.SequenceEqual<Product>(products));
        }

        [TestMethod]
        [Description("Ensures default property values for ICollectionView support")]
        public void EnsureDefaultICollectionViewSupportPropertyValues()
        {
            IEnumerable<Product> emptySource = new Product[0];
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(emptySource);

            Assert.IsFalse(view.CanFilter, "CanFilter");
            Assert.IsFalse(view.CanGroup, "CanGroup");
            Assert.IsFalse(view.CanSort, "CanSort");
            Assert.AreEqual<System.Globalization.CultureInfo>(System.Globalization.CultureInfo.CurrentCulture, view.Culture, "Culture");
            Assert.IsNull(view.CurrentItem, "CurrentItem");
            Assert.AreEqual<int>(-1, view.CurrentPosition, "CurrentPosition");
            Assert.IsTrue(view.IsCurrentAfterLast, "IsCurrentAfterLast");
            Assert.IsTrue(view.IsCurrentBeforeFirst, "IsCurrentBeforeFirst");
            Assert.AreEqual<IEnumerable>(emptySource, view.SourceCollection, "SourceCollection");
            Assert.IsFalse(view.CanAddNew, "CanAddNew");
            Assert.IsFalse(view.CanCancelEdit, "CanCancelEdit");
            Assert.IsFalse(view.CanRemove, "CanRemove");
            Assert.IsNull(view.CurrentAddItem, "CurrentAddItem");
            Assert.IsNull(view.CurrentEditItem, "CurrentEditItem");
            Assert.IsFalse(view.IsAddingNew, "IsAddingNew");
            Assert.IsFalse(view.IsEditingItem, "IsEditingItem");
            Assert.AreEqual<System.ComponentModel.NewItemPlaceholderPosition>(System.ComponentModel.NewItemPlaceholderPosition.None, view.NewItemPlaceholderPosition);
        }

        [TestMethod]
        [Description("Ensures the IsEmpty property is true when the source is empty, and false when there are items in the source")]
        public void IsEmptyIsAccurate()
        {
            List<Product> products = new List<Product>();
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(products);
            Assert.IsTrue(view.IsEmpty, "Before Adding Item");

            products.Add(new Product());
            Assert.IsFalse(view.IsEmpty, "After Adding Item");

            products.Clear();
            Assert.IsTrue(view.IsEmpty, "After Removing Item");
        }

        [TestMethod]
        [Description("Ensures InvalidOperationExceptions are thrown when calling members that are not supplied to the view")]
        public void EnsureExpectedInvalidOperationExceptionsForMembersNotProvided()
        {
            Product product = new Product();
            IEnumerable<Product> source = new Product[] { product };

            // We get exceptions when we don't specify the operations as supported
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source);
            ExceptionHelper.ExpectInvalidOperationException(() => view.AddNew(), string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.NotSupported, "AddNew"));
            ExceptionHelper.ExpectInvalidOperationException(() => view.Remove(product), string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.NotSupported, "Remove"));
            ExceptionHelper.ExpectInvalidOperationException(() => view.RemoveAt(0), string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.NotSupported, "RemoveAt"));

            // We still get exceptions when we say the operations are supported but we don't provide implementations
            view = new EntityCollectionView<Product>(source, EntitySetOperations.All);
            ExceptionHelper.ExpectInvalidOperationException(() => view.AddNew(), string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.NotSupported, "AddNew"));
            ExceptionHelper.ExpectInvalidOperationException(() => view.Remove(product), string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.NotSupported, "Remove"));
            ExceptionHelper.ExpectInvalidOperationException(() => view.RemoveAt(0), string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.NotSupported, "RemoveAt"));

            // Cannot use CancelNew if a Remove implementation is not provided
            AppendOnlyCollection<Product> appendOnly = new AppendOnlyCollection<Product>();
            EntityCollectionView<Product>.CollectionViewDelegates delegates = new EntityCollectionView<Product>.CollectionViewDelegates()
            {
                Add = item => appendOnly.Add((Product)item)
            };

            view = new EntityCollectionView<Product>(appendOnly, EntitySetOperations.Add);
            ExceptionHelper.ExpectInvalidOperationException(() => view.CancelNew(), string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.NotSupported, "CancelNew (Remove)"));

            view = new EntityCollectionView<Product>(new Product[0]);

            object x;
            ExceptionHelper.ExpectInvalidOperationException(() => x = view.Filter, string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.NotSupported, "Filter"));
            ExceptionHelper.ExpectInvalidOperationException(() => view.Filter = new Predicate<object>(o => false), string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.NotSupported, "Filter"));
            ExceptionHelper.ExpectInvalidOperationException(() => x = view.GroupDescriptions, string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.NotSupported, "GroupDescriptions"));
            ExceptionHelper.ExpectInvalidOperationException(() => x = view.Groups, string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.NotSupported, "Groups"));
            ExceptionHelper.ExpectInvalidOperationException(() => x = view.SortDescriptions, string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.NotSupported, "SortDescriptions"));
        }

        [TestMethod]
        [Description("Contains uses the provided delegate")]
        public void ContainsUsesProvidedDelegate()
        {
            List<MockEntity> source = new List<MockEntity>();

            bool delegateCalled = false;
            EntityCollectionView<MockEntity>.CollectionViewDelegates delegates = new EntityCollectionView<MockEntity>.CollectionViewDelegates
            {
                Contains = item => { delegateCalled = true; return true; }
            };

            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.None, delegates);

            // Contains is configured above to always return true
            Assert.IsTrue(view.Contains(new object()), "Contains()");
            Assert.IsTrue(delegateCalled, "delegateCalled");
        }

        [TestMethod]
        [Description("IndexOf uses the provided delegate")]
        public void IndexOfUsesProvidedDelegate()
        {
            MockEntity mock = new MockEntity();
            List<MockEntity> source = new List<MockEntity>(new MockEntity[] { mock });

            bool delegateCalled = false;
            EntityCollectionView<MockEntity>.CollectionViewDelegates delegates = new EntityCollectionView<MockEntity>.CollectionViewDelegates
            {
                IndexOf = item => { delegateCalled = true; return -1; }
            };

            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.None, delegates);

            // MoveCurrentTo requires IndexOf.  We always return -1, so MoveCurrentTo will report false.
            Assert.IsFalse(view.MoveCurrentTo(mock), "MoveCurrentTo()");
            Assert.IsTrue(delegateCalled, "delegateCalled");
        }

        [TestMethod]
        [Description("Count uses the provided delegate")]
        public void CountUsesProvidedDelegate()
        {
            MockEntity mock = new MockEntity();
            List<MockEntity> source = new List<MockEntity>(new MockEntity[] { mock });

            bool delegateCalled = false;
            EntityCollectionView<MockEntity>.CollectionViewDelegates delegates = new EntityCollectionView<MockEntity>.CollectionViewDelegates
            {
                Count = () => { delegateCalled = true; return 0; }
            };

            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.None, delegates);

            // MoveCurrentToLast requires Count.  We always return 0, so MoveCurrentToLast will report false
            Assert.IsFalse(view.MoveCurrentToLast(), "MoveCurrentToLast()");
            Assert.IsTrue(delegateCalled, "delegateCalled");
        }

        [TestMethod]
        [Description("Add uses the provided delegate")]
        public void AddUsesProvidedDelegate()
        {
            List<MockEntity> source = new List<MockEntity>();

            bool delegateCalled = false;
            EntityCollectionView<MockEntity>.CollectionViewDelegates delegates = new EntityCollectionView<MockEntity>.CollectionViewDelegates
            {
                Add = item => delegateCalled = true
            };

            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.Add, delegates);
            MockEntity mock = (MockEntity)view.AddNew();

            Assert.IsTrue(delegateCalled, "delegateCalled");
            Assert.IsFalse(source.Contains(mock), "The source should not contain the mock because Add should not have been called as a different delegate was provided");
        }

        [TestMethod]
        [Description("Remove uses the provided delegate")]
        public void RemoveUsesProvidedDelegate()
        {
            MockEntity mock = new MockEntity();
            List<MockEntity> source = new List<MockEntity>(new MockEntity[] { mock });

            bool delegateCalled = false;
            EntityCollectionView<MockEntity>.CollectionViewDelegates delegates = new EntityCollectionView<MockEntity>.CollectionViewDelegates
            {
                Remove = item => delegateCalled = true
            };

            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.Remove, delegates);
            view.Remove(mock);

            Assert.IsTrue(delegateCalled, "delegateCalled");
            Assert.IsTrue(source.Contains(mock), "The source should still contain the mock because Remove should not have been called as a different delegate was provided");
        }

        [TestMethod]
        [Description("RemoveAt uses the provided delegate")]
        public void RemoveAtUsesProvidedDelegate()
        {
            MockEntity mock = new MockEntity();
            List<MockEntity> source = new List<MockEntity>(new MockEntity[] { mock });

            bool delegateCalled = false;
            EntityCollectionView<MockEntity>.CollectionViewDelegates delegates = new EntityCollectionView<MockEntity>.CollectionViewDelegates
            {
                RemoveAt = item => delegateCalled = true
            };

            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.Remove, delegates);
            view.RemoveAt(0);

            Assert.IsTrue(delegateCalled, "delegateCalled");
            Assert.IsTrue(source.Contains(mock), "The source should still contain the mock because RemoveAt should not have been called as a different delegate was provided");
        }

        #endregion

        #region ICollectionView Members

        [TestMethod]
        [Description("Contains returns false for items of the wrong type")]
        public void ContainsReturnsFalseForBadType()
        {
            Product product = new Product();
            List<Product> source = new List<Product>(new Product[] { product });
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source);

            Assert.IsFalse(view.Contains(new object()));
        }

        [TestMethod]
        [Description("CurrentItem is null when collection is empty")]
        public void CurrentItemIsNullWhenCollectionEmpty()
        {
            List<Product> source = new List<Product>(new Product[0]);
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source);

            Assert.IsNull(view.CurrentItem);
        }

        [TestMethod]
        [Description("CurrentItem is set to the first item by default")]
        public void CurrentItemDefaultsToFirstItem()
        {
            Product first = new Product();
            Product second = new Product();
            List<Product> source = new List<Product>(new Product[] { first, second });
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source);

            Assert.AreSame(first, view.CurrentItem);
        }

        [TestMethod]
        [Description("CurrentPosition is -1 when collection is empty")]
        public void CurrentPositionIsNegativeWhenCollectionEmpty()
        {
            List<Product> source = new List<Product>(new Product[0]);
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source);

            Assert.AreEqual<int>(-1, view.CurrentPosition);
        }

        [TestMethod]
        [Description("CurrentPosition is set to the first item by default")]
        public void CurrentPositionDefaultsToFirstItem()
        {
            Product first = new Product();
            Product second = new Product();
            List<Product> source = new List<Product>(new Product[] { first, second });
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source);

            Assert.AreEqual<int>(0, view.CurrentPosition);
        }

        [TestMethod]
        [Description("MoveCurrentTo returns false when the item is not the correct type")]
        public void MoveCurrentToReturnsFalseForBadType()
        {
            Product first = new Product();
            Product second = new Product();
            List<Product> source = new List<Product>(new Product[] { first, second });
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source);

            Assert.IsFalse(view.MoveCurrentTo(new object()));
        }

        [TestMethod]
        [Description("MoveCurrentTo returns false when the item is not in the collection")]
        public void MoveCurrentToReturnsFalseForItemNotInCollection()
        {
            Product first = new Product();
            Product second = new Product();
            List<Product> source = new List<Product>(new Product[] { first, second });
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source);

            Assert.IsFalse(view.MoveCurrentTo(new MockEntity()));
        }

        [TestMethod]
        [Description("MoveCurrentTo, MoveCurrentToFirst, MoveCurrentToLast, MoveCurrentToPrevious, MoveCurrentToNext all succeed when possible")]
        public void MoveCurrentToSucceeds()
        {
            Product first = new Product();
            Product second = new Product();
            List<Product> source = new List<Product>(new Product[] { first, second });
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source);

            Assert.IsTrue(view.MoveCurrentTo(second));
            Assert.AreSame(second, view.CurrentItem);
            Assert.AreEqual<int>(1, view.CurrentPosition);

            view.MoveCurrentToFirst();
            Assert.AreSame(first, view.CurrentItem, "first item");
            Assert.AreEqual<int>(0, view.CurrentPosition, "first position");

            view.MoveCurrentToLast();
            Assert.AreSame(second, view.CurrentItem);
            Assert.AreEqual<int>(1, view.CurrentPosition);

            view.MoveCurrentToPrevious();
            Assert.AreSame(first, view.CurrentItem, "first item");
            Assert.AreEqual<int>(0, view.CurrentPosition, "first position");

            view.MoveCurrentToNext();
            Assert.AreSame(second, view.CurrentItem);
            Assert.AreEqual<int>(1, view.CurrentPosition);
        }

        [TestMethod]
        [Description("MoveCurrentTo, MoveCurrentToFirst, MoveCurrentToLast, MoveCurrentToPrevious, MoveCurrentToNext all return false when the move is invalid")]
        public void MoveCurrentToReturnsFalseWhenInvalid()
        {
            Product first = new Product();
            Product second = new Product();
            List<Product> source = new List<Product>(new Product[] { first, second });
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source);

            // Test MoveCurrentTo with an empty collection, ensuring that currency is cleared
            Assert.IsNotNull(view.CurrentItem, "CurrentItem should be set after constructing the view");
            Assert.AreNotEqual<int>(-1, view.CurrentPosition, "CurrentPosition should be set after constructing the view");
            source.Clear();

            Assert.IsFalse(view.MoveCurrentTo(first), "MoveCurrentTo(first)");
            Assert.IsNull(view.CurrentItem, "CurrentItem after MoveCurrentTo(first)");
            Assert.AreEqual<int>(-1, view.CurrentPosition, "CurrentPosition after MoveCurrentTo(first)");

            // Test MoveCurrentToFirst with an empty collection, ensuring that currency is cleared
            source.Add(first);
            view.MoveCurrentToFirst();
            Assert.IsNotNull(view.CurrentItem, "CurrentItem before MoveCurrentToFirst()");
            Assert.AreNotEqual<int>(-1, view.CurrentPosition, "CurrentPosition before MoveCurrentToFirst()");
            source.Clear();

            Assert.IsFalse(view.MoveCurrentToFirst(), "MoveCurrentToFirst()");
            Assert.IsNull(view.CurrentItem, "CurrentItem after MoveCurrentToFirst()");
            Assert.AreEqual<int>(-1, view.CurrentPosition, "CurrentPosition after MoveCurrentToFirst()");

            // Test MoveCurrentToLast with an empty collection, ensuring that currency is cleared
            source.Add(first);
            view.MoveCurrentToFirst();
            Assert.IsNotNull(view.CurrentItem, "CurrentItem before MoveCurrentToLast()");
            Assert.AreNotEqual<int>(-1, view.CurrentPosition, "CurrentPosition before MoveCurrentToLast()");
            source.Clear();

            Assert.IsFalse(view.MoveCurrentToLast(), "MoveCurrentToLast()");
            Assert.IsNull(view.CurrentItem, "CurrentItem after MoveCurrentToLast()");
            Assert.AreEqual<int>(-1, view.CurrentPosition, "CurrentPosition after MoveCurrentToLast()");

            // Test MoveCurrentToPrevious when on the first item, ensuring that currency is retained, but the move returns false
            source.Add(first);
            source.Add(second);
            view.MoveCurrentToFirst();
            Assert.IsNotNull(view.CurrentItem, "CurrentItem before MoveCurrentToPrevious()");
            Assert.AreNotEqual<int>(-1, view.CurrentPosition, "CurrentPosition before MoveCurrentToPrevious()");

            Assert.IsFalse(view.MoveCurrentToPrevious(), "MoveCurrentToPrevious()");
            Assert.IsNotNull(view.CurrentItem, "CurrentItem after MoveCurrentToPrevious()");
            Assert.AreNotEqual<int>(-1, view.CurrentPosition, "CurrentPosition after MoveCurrentToPrevious()");

            // Test MoveCurrentToNext when on the last item, ensuring that currency is retained, but the move returns false
            view.MoveCurrentToLast();
            Assert.IsNotNull(view.CurrentItem, "CurrentItem before MoveCurrentToNext()");
            Assert.AreNotEqual<int>(-1, view.CurrentPosition, "CurrentPosition before MoveCurrentToNext()");

            Assert.IsFalse(view.MoveCurrentToNext(), "MoveCurrentToNext()");
            Assert.IsNotNull(view.CurrentItem, "CurrentItem after MoveCurrentToNext()");
            Assert.AreNotEqual<int>(-1, view.CurrentPosition, "CurrentPosition after MoveCurrentToNext()");

            // Test MoveCurrentToPosition with an invalid position, ensuring that an exception is thrown
            ExceptionHelper.ExpectArgumentOutOfRangeException(() => view.MoveCurrentToPosition(10), "position", new ArgumentOutOfRangeException("position").Message);
        }

        [TestMethod]
        [Description("MoveCurrentToLast returns false when the collection is empty")]
        public void MoveCurrentToLastReturnsFalseWhenCollectionEmpty()
        {
            Product product = new Product();
            List<Product> source = new List<Product>(new Product[] { product });
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source);

            // We provided an item at first, so the current item could be set, but now we'll clear
            // the collection and adjust currency to first, which should result in no selected item.
            source.Clear();

            Assert.IsFalse(view.MoveCurrentToFirst());
            Assert.IsNull(view.CurrentItem);
            Assert.AreEqual<int>(-1, view.CurrentPosition);
        }

        /// <summary>
        /// EntityCollectionView maintains state regarding currency movements to ensure that CurrentChanging and CurrentChanged
        /// events are paired up.  This test ensures that the state is managed correctly when a CurrentChanging event is
        /// canceled.  As the bug discovered, repeated attempts to move currency when validation errors existed would allow
        /// every other attempt to go through.
        /// </summary>
        [TestMethod]
        [WorkItem(877335)]
        [Description("Repeated attempts to move currency will be rejected when the current item has validation errors.")]
        public void MoveCurrentToFailsWhenEntityIsInvalid()
        {
            Product first = new Product();
            Product second = new Product();
            List<Product> source = new List<Product>(new Product[] { first, second });
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source);

            // Start off on the first entity
            view.MoveCurrentToFirst();

            // We will simulate a form blocking currency movement by always canceling the move
            // And we will also keep track of how many times CurrentChanging and CurrentChanged
            // events are raised.
            int currentChangingEvents = 0;
            int currentChangedEvents = 0;

            view.CurrentChanging += (s, e) =>
                {
                    ++currentChangingEvents;
                    e.Cancel = true;
                };

            view.CurrentChanged += (s, e) => ++currentChangedEvents;

            view.MoveCurrentToLast();
            Assert.AreEqual<int>(1, currentChangingEvents, "CurrentChanging should have been raised from the first MoveCurrentToLast()");
            Assert.AreEqual<int>(0, currentChangedEvents, "CurrentChanged should NOT have been raised from the first MoveCurrentToLast()");

            view.MoveCurrentToLast();
            Assert.AreEqual<int>(2, currentChangingEvents, "CurrentChanging should have been raised from the second MoveCurrentToLast()");
            Assert.AreEqual<int>(0, currentChangedEvents, "CurrentChanged should NOT have been raised from the second MoveCurrentToLast()");
        }

        #endregion

        #region IEditableCollectionView Members

        [TestMethod]
        [Description("Ensures CanAddNew is false when the source doesn't support Add")]
        public void CanAddNewIsTrueWhenSupported()
        {
            Product product = new Product();
            List<Product> source = new List<Product>(new Product[] { product });
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.Add);

            Assert.IsTrue(view.CanAddNew);
        }

        [TestMethod]
        [Description("CanAddNew should be true when in the middle of an Add transaction as the current transaction would be implicitly committed")]
        public void CanAddNewIsTrueWhenAddingItem()
        {
            Product product = new Product();
            List<Product> source = new List<Product>(new Product[] { product });
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.Add);

            Product newProduct = (Product)view.AddNew();
            Assert.IsTrue(view.CanAddNew);
        }

        [TestMethod]
        [Description("CanAddNew should be true when in the middle of an Edit transaction as the current transaction would be implicitly committed")]
        public void CanAddNewIsTrueWhenEditingItem()
        {
            Product product = new Product();
            List<Product> source = new List<Product>(new Product[] { product });
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.All);

            view.EditItem(product);
            Assert.IsTrue(view.CanAddNew, "CanAddNew after starting edit transaction");
        }

        [TestMethod]
        [Description("CanCancelEdit should be true when the Edit operation is supported")]
        public void CanCancelEditRespectsEditSupport()
        {
            Product product = new Product();
            List<Product> source = new List<Product>(new Product[] { product });
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.Edit);

            view.EditItem(product);
            Assert.IsTrue(view.CanCancelEdit,
                "CanCancelEdit should be true when Edit is supported.");
        }

        [TestMethod]
        [Description("CanCancelEdit should only be true when IsEditingItem is true")]
        [WorkItem(179508)]
        public void CanCancelEditIsOnlyTrueWhenEditing()
        {
            Product product = new Product();
            List<Product> source = new List<Product>(new Product[] { product });
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.Edit);

            Assert.IsFalse(view.CanCancelEdit,
                "CanCancelEdit should be false when IsEditingItem is false.");

            view.EditItem(product);

            Assert.IsTrue(view.CanCancelEdit,
                "CanCancelEdit should be true when IsEditingItem is true.");
        }

        [TestMethod]
        [Description("CanRemove should be true when the Remove operation is supported")]
        public void CanRemoveRespectsRemoveSupport()
        {
            Product product = new Product();
            List<Product> source = new List<Product>(new Product[] { product });
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.Remove);

            Assert.IsTrue(view.CanRemove);
        }

        [TestMethod]
        [Description("AddNew begins an edit transaction on the current add item")]
        public void AddNewBeginsEditTransaction()
        {
            List<MockEntity> source = new List<MockEntity>(new MockEntity[0]);
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.All);

            MockEntity mock = (MockEntity)view.AddNew();
            Assert.AreEqual<int>(1, mock.BeginEditCalls);
            Assert.AreSame(mock, view.CurrentAddItem, "CurrentAddItem should be the returned mock");
            Assert.IsNull(view.CurrentEditItem, "CurrentEditItem should remain null");
        }

        [TestMethod]
        [Description("AddNew implicitly commits a pending edit transaction")]
        public void AddNewCommitsPendingEdit()
        {
            MockEntity mock = new MockEntity();
            List<MockEntity> source = new List<MockEntity>(new MockEntity[] { mock });
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.All);

            view.EditItem(mock);
            Assert.AreEqual<int>(0, mock.EndEditCalls, "Before");

            MockEntity newMock = (MockEntity)view.AddNew();
            Assert.AreEqual<int>(1, mock.EndEditCalls, "After");
            Assert.IsNull(view.CurrentEditItem, "CurrentEditItem");
            Assert.AreSame(newMock, view.CurrentAddItem, "CurrentAddItem");
        }

        [TestMethod]
        [Description("AddNew implicitly commits a pending add transaction")]
        public void AddNewCommitsPendingAdd()
        {
            List<MockEntity> source = new List<MockEntity>(new MockEntity[0]);
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.All);

            MockEntity mock = (MockEntity)view.AddNew();
            Assert.AreEqual<int>(0, mock.EndEditCalls, "Before");

            MockEntity newMock = (MockEntity)view.AddNew();
            Assert.AreEqual<int>(1, mock.EndEditCalls, "After");
            Assert.AreSame(newMock, view.CurrentAddItem, "CurrentAddItem");
        }

        [TestMethod]
        [Description("CancelNew will no-op when not adding an item")]
        public void CancelNewNoOpWhenNotAdding()
        {
            List<MockEntity> source = new List<MockEntity>(new MockEntity[0]);
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.Add);

            // Nothing should happen - absence of an exception asserts success
            view.CancelNew();
        }

        [TestMethod]
        [Description("CancelNew cancels the edit transaction on the current add item")]
        public void CancelNewCancelsEditTransaction()
        {
            List<MockEntity> source = new List<MockEntity>(new MockEntity[0]);
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.All);

            MockEntity mock = (MockEntity)view.AddNew();
            Assert.AreEqual<int>(0, mock.CancelEditCalls, "Before");

            view.CancelNew();
            Assert.AreEqual<int>(1, mock.CancelEditCalls, "After");
            Assert.IsNull(view.CurrentAddItem, "CurrentAddItem");
        }

        [TestMethod]
        [Description("CancelNew removes the current add item from the collection")]
        public void CancelNewRemovesCurrentAddItem()
        {
            List<MockEntity> source = new List<MockEntity>(new MockEntity[0]);
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.All);

            MockEntity mock = (MockEntity)view.AddNew();
            Assert.IsTrue(source.Contains(mock));

            view.CancelNew();
            Assert.IsFalse(source.Contains(mock));
        }

        [TestMethod]
        [Description("CommitNew throws when editing an item")]
        public void CommitNewThrowsWhenEditingItem()
        {
            MockEntity mock = new MockEntity();
            List<MockEntity> source = new List<MockEntity>(new MockEntity[] { mock });
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.Edit);

            view.EditItem(mock);
            ExceptionHelper.ExpectInvalidOperationException(() => view.CommitNew(), string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.OperationNotAllowedDuringTransaction, "CommitNew", "EditItem"));
        }

        [TestMethod]
        [Description("CommitNew will no-op when not adding an item")]
        public void CommitNewNoOpWhenNotAdding()
        {
            List<MockEntity> source = new List<MockEntity>(new MockEntity[0]);
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.All);

            // Nothing should happen - absence of an exception asserts success
            view.CommitNew();
        }

        [TestMethod]
        [Description("CommitNew commits the add transaction")]
        public void CommitNewCommitsTransaction()
        {
            List<MockEntity> source = new List<MockEntity>();
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.Add);

            MockEntity mock = (MockEntity)view.AddNew();
            Assert.AreEqual<int>(0, mock.EndEditCalls, "Before");

            view.CommitNew();
            Assert.AreEqual<int>(1, mock.EndEditCalls, "After");
            Assert.IsNull(view.CurrentAddItem, "CurrentAddItem");
        }

        [TestMethod]
        [Description("EditItem throws when item is not of correct type")]
        public void EditItemThrowsWhenItemIsNotCorrectType()
        {
            MockEntity mock = new MockEntity();
            List<MockEntity> source = new List<MockEntity>(new MockEntity[] { mock });
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.Edit);

            ExceptionHelper.ExpectArgumentException(() => view.EditItem(new object()), string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.ItemNotEntityType, typeof(MockEntity).Name), "item");
        }

        [TestMethod]
        [Description("EditItem begins a transaction")]
        public void EditItemBeginsTransaction()
        {
            MockEntity mock = new MockEntity();
            List<MockEntity> source = new List<MockEntity>(new MockEntity[] { mock });
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.Edit);

            view.EditItem(mock);
            Assert.AreEqual<int>(1, mock.BeginEditCalls, "Before");
            Assert.AreSame(mock, view.CurrentEditItem, "CurrentEditItem");
        }

        [TestMethod]
        [Description("EditItem will no-op when attempting to edit the current edit item")]
        public void EditItemNoOpForCurrentEditItem()
        {
            MockEntity mock = new MockEntity();
            List<MockEntity> source = new List<MockEntity>(new MockEntity[] { mock });
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.Edit);

            view.EditItem(mock);
            Assert.AreEqual<int>(1, mock.BeginEditCalls, "Before");

            view.EditItem(mock);
            Assert.AreEqual<int>(1, mock.BeginEditCalls, "After");
        }

        [TestMethod]
        [Description("EditItem will no-op when attempting to edit the current add item")]
        public void EditItemNoOpForCurrentAddItem()
        {
            List<MockEntity> source = new List<MockEntity>(new List<MockEntity>());
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.All);

            MockEntity mock = (MockEntity)view.AddNew();
            Assert.AreEqual<int>(1, mock.BeginEditCalls, "Before");

            view.EditItem(mock);
            Assert.AreEqual<int>(1, mock.BeginEditCalls, "After");
            Assert.IsNull(view.CurrentEditItem);
            Assert.IsFalse(view.IsEditingItem);
        }

        [TestMethod]
        [Description("CancelEdit throws InvalidOperationException when adding a new item")]
        public void CancelEditThrowsWhenAddingItem()
        {
            Product product = new Product();
            List<Product> source = new List<Product>(new Product[] { product });
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.All);

            Product newProduct = (Product)view.AddNew();
            ExceptionHelper.ExpectInvalidOperationException(() => view.CancelEdit(), string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.OperationNotAllowedDuringTransaction, "CancelEdit", "AddNew"));
        }

        [TestMethod]
        [Description("CancelEdit throws InvalidOperationException when the Edit operation is not supported")]
        public void CancelEditThrowsWhenEditNotSupported()
        {
            Product product = new Product();
            List<Product> source = new List<Product>(new Product[] { product });
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.Add);

            ExceptionHelper.ExpectInvalidOperationException(() => view.CancelEdit(), string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.NotSupported, "CancelEdit"));
        }

        [TestMethod]
        [Description("CancelEdit cancels the edit transaction on the current edit item")]
        public void CancelEditCancelsEditTransaction()
        {
            MockEntity mock = new MockEntity();
            List<MockEntity> source = new List<MockEntity>(new MockEntity[] { mock });
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.Edit);

            view.EditItem(mock);
            Assert.AreEqual<int>(0, mock.CancelEditCalls, "Before");

            view.CancelEdit();
            Assert.AreEqual<int>(1, mock.CancelEditCalls, "After");
            Assert.IsNull(view.CurrentEditItem, "CurrentAddItem");
        }

        [TestMethod]
        [Description("CommitEdit throws when adding an item")]
        public void CommitEditThrowsWhenAddingItem()
        {
            List<MockEntity> source = new List<MockEntity>(new MockEntity[0]);
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.All);

            MockEntity mock = (MockEntity)view.AddNew();
            ExceptionHelper.ExpectInvalidOperationException(() => view.CommitEdit(), string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.OperationNotAllowedDuringTransaction, "CommitEdit", "AddNew"));
        }

        [TestMethod]
        [Description("CommitEdit will no-op when not editing an item")]
        public void CommitEditNoOpWhenNotEditing()
        {
            List<MockEntity> source = new List<MockEntity>(new MockEntity[0]);
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.All);

            // Nothing should happen - absence of an exception asserts success
            view.CommitEdit();
        }

        [TestMethod]
        [Description("CommitEdit commits the edit transaction")]
        public void CommitEditCommitsTransaction()
        {
            MockEntity mock = new MockEntity();
            List<MockEntity> source = new List<MockEntity>(new MockEntity[] { mock });
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.Edit);

            view.EditItem(mock);
            Assert.AreEqual<int>(0, mock.EndEditCalls, "Before");

            view.CommitEdit();
            Assert.AreEqual<int>(1, mock.EndEditCalls, "After");
            Assert.IsNull(view.CurrentEditItem, "CurrentEditItem");
        }

        [TestMethod]
        [Description("CanRemove is false when adding an item")]
        public void CanRemoveIsFalseWhenAddingItem()
        {
            List<MockEntity> source = new List<MockEntity>(new List<MockEntity>());
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.All);

            MockEntity mock = (MockEntity)view.AddNew();
            Assert.IsFalse(view.CanRemove);
        }

        [TestMethod]
        [Description("CanRemove is false when editing an item")]
        public void CanRemoveIsFalseWhenEditingItem()
        {
            MockEntity mock = new MockEntity();
            List<MockEntity> source = new List<MockEntity>(new MockEntity[] { mock });
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.Edit);

            view.EditItem(mock);
            Assert.IsFalse(view.CanRemove);
        }

        [TestMethod]
        [Description("Remove throws when adding an item")]
        public void RemoveThrowsWhenAddingItem()
        {
            List<MockEntity> source = new List<MockEntity>(new List<MockEntity>());
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.All);

            MockEntity mock = (MockEntity)view.AddNew();
            ExceptionHelper.ExpectInvalidOperationException(() => view.Remove(mock), string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.OperationNotAllowedDuringAddOrEdit, "Remove"));
        }

        [TestMethod]
        [Description("Remove throws when adding an item")]
        public void RemoveThrowsWhenEditingItem()
        {
            MockEntity mock = new MockEntity();
            List<MockEntity> source = new List<MockEntity>(new MockEntity[] { mock });
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.Remove);

            view.EditItem(mock);
            ExceptionHelper.ExpectInvalidOperationException(() => view.Remove(mock), string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.OperationNotAllowedDuringAddOrEdit, "Remove"));
        }

        [TestMethod]
        [Description("Remove throws when item is not of correct type")]
        public void RemoveThrowsWhenItemIsNotCorrectType()
        {
            MockEntity mock = new MockEntity();
            List<MockEntity> source = new List<MockEntity>(new MockEntity[] { mock });
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.Remove);

            ExceptionHelper.ExpectArgumentException(() => view.Remove(new object()), string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.ItemNotEntityType, typeof(MockEntity).Name), "item");
        }

        [TestMethod]
        [Description("Remove removes the item from the source collection")]
        public void RemoveRemovesFromSource()
        {
            MockEntity mock = new MockEntity();
            List<MockEntity> source = new List<MockEntity>(new MockEntity[] { mock });
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.Remove);

            view.Remove(mock);
            Assert.IsFalse(source.Contains(mock));
        }

        [TestMethod]
        [Description("RemoveAt throws when adding an item")]
        public void RemoveAtThrowsWhenAddingItem()
        {
            List<MockEntity> source = new List<MockEntity>(new List<MockEntity>());
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.All);

            MockEntity mock = (MockEntity)view.AddNew();
            ExceptionHelper.ExpectInvalidOperationException(() => view.RemoveAt(0), string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.OperationNotAllowedDuringAddOrEdit, "RemoveAt"));
        }

        [TestMethod]
        [Description("RemoveAt throws when adding an item")]
        public void RemoveAtThrowsWhenEditingItem()
        {
            MockEntity mock = new MockEntity();
            List<MockEntity> source = new List<MockEntity>(new MockEntity[] { mock });
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.Remove);

            view.EditItem(mock);
            ExceptionHelper.ExpectInvalidOperationException(() => view.RemoveAt(0), string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.OperationNotAllowedDuringAddOrEdit, "RemoveAt"));
        }

        [TestMethod]
        [Description("RemoveAt removes the item from the source collection")]
        public void RemoveAtRemovesFromSource()
        {
            MockEntity mock = new MockEntity();
            List<MockEntity> source = new List<MockEntity>(new MockEntity[] { mock });
            EntityCollectionView<MockEntity> view = new EntityCollectionView<MockEntity>(source, EntitySetOperations.Remove);

            view.RemoveAt(0);
            Assert.IsFalse(source.Contains(mock));
        }

        [TestMethod]
        [Description("Removing the only item in the collection results in a null CurrentItem")]
        public void RemoveOnlyItemClearsCurrency()
        {
            Product product = new Product();
            NotifyingCollection<Product> source = new NotifyingCollection<Product>() { product };
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.All);

            Assert.AreSame(product, view.CurrentItem, "CurrentItem before Remove");
            Assert.AreEqual<int>(0, view.CurrentPosition, "CurrentPosition before Remove");

            this.AssertCurrencyChangeCount(() => view.Remove(product), view, 1);
            Assert.IsNull(view.CurrentItem, "CurrentItem after Remove");
            Assert.AreEqual<int>(-1, view.CurrentPosition, "CurrentPosition after Remove");
        }

        [TestMethod]
        [Description("Removing an item before the CurrentItem adjusts CurrentPosition")]
        public void RemoveItemBeforeCurrentAdjustsPosition()
        {
            Product first = new Product();
            Product second = new Product();
            NotifyingCollection<Product> source = new NotifyingCollection<Product>() { first, second };
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.All);

            this.AssertCurrencyChangeCount(() => view.MoveCurrentToLast(), view, 1);
            Assert.AreSame(second, view.CurrentItem, "CurrentItem before Remove");
            Assert.AreEqual<int>(1, view.CurrentPosition, "CurrentPosition before Remove");

            this.AssertCurrencyChangeCount(() => view.Remove(first), view, 1);
            Assert.AreSame(second, view.CurrentItem, "CurrentItem after Remove");
            Assert.AreEqual<int>(0, view.CurrentPosition, "CurrentPosition after Remove");
        }

        [TestMethod]
        [Description("Removing the CurrentItem will retain CurrentPosition if there are items after the current item")]
        public void RemoveCurrentItemRetainsPosition()
        {
            Product first = new Product();
            Product second = new Product();
            NotifyingCollection<Product> source = new NotifyingCollection<Product>() { first, second };
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.All);

            Assert.AreSame(first, view.CurrentItem, "CurrentItem before Remove");
            Assert.AreEqual<int>(0, view.CurrentPosition, "CurrentPosition before Remove");

            this.AssertCurrencyChangeCount(() => view.Remove(first), view, 1);
            Assert.AreSame(second, view.CurrentItem, "CurrentItem after Remove");
            Assert.AreEqual<int>(0, view.CurrentPosition, "CurrentPosition after Remove");
        }

        [TestMethod]
        [Description("Removing the CurrentItem when it's the last item will set currency to the new last item")]
        public void RemoveCurrentItemAsLastItem()
        {
            Product first = new Product();
            Product second = new Product();
            NotifyingCollection<Product> source = new NotifyingCollection<Product>() { first, second };
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.All);

            this.AssertCurrencyChangeCount(() => view.MoveCurrentToLast(), view, 1);
            Assert.AreSame(second, view.CurrentItem, "CurrentItem before Remove");
            Assert.AreEqual<int>(1, view.CurrentPosition, "CurrentPosition before Remove");

            this.AssertCurrencyChangeCount(() => view.Remove(second), view, 1);
            Assert.AreSame(first, view.CurrentItem, "CurrentItem after Remove");
            Assert.AreEqual<int>(0, view.CurrentPosition, "CurrentPosition after Remove");
        }

        [TestMethod]
        [Description("Removing an item after the CurrentItem does not affect currency")]
        public void RemoveItemAfterCurrentRetainsCurrency()
        {
            Product first = new Product();
            Product second = new Product();
            NotifyingCollection<Product> source = new NotifyingCollection<Product>() { first, second };
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.All);

            Assert.AreSame(first, view.CurrentItem, "CurrentItem before Remove");
            Assert.AreEqual<int>(0, view.CurrentPosition, "CurrentPosition before Remove");

            this.AssertCurrencyChangeCount(() => view.Remove(second), view, 0);
            Assert.AreSame(first, view.CurrentItem, "CurrentItem after Remove");
            Assert.AreEqual<int>(0, view.CurrentPosition, "CurrentPosition after Remove");
        }

        #endregion

        #region INotifyCollectionChanged Synchronization

        [TestMethod]
        [Description("When adding an item, any CollectionChanged event where the add item is removed will throw")]
        public void CollectionChanged_CannotRemoveCurrentAddItem()
        {
            NotifyingCollection<Product> source = new NotifyingCollection<Product>();
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.All);

            Product product = (Product)view.AddNew();

            // Remove notification will result in an exception, but the removal will actually succeed
            ExceptionHelper.ExpectInvalidOperationException(() => source.Remove(product), string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.OperationNotAllowedDuringAddOrEdit, "Removing"));
            source.Add(product);

            // Replace notification will result in an exception, but the replacement will actually succeed
            ExceptionHelper.ExpectInvalidOperationException(() => source[0] = new Product(), string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.OperationNotAllowedDuringAddOrEdit, "Removing"));

            // Reset notifiation will result in an exception
            source.NotifyOnCollectionChanged = false;
            source.Clear();
            ExceptionHelper.ExpectInvalidOperationException(() => source.Reset(), string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.OperationNotAllowedDuringAddOrEdit, "Removing"));
        }

        [TestMethod]
        [Description("When editing an item, any CollectionChanged event where the add item is removed will throw")]
        public void CollectionChanged_CannotRemoveCurrentEditItem()
        {
            Product product = new Product();
            NotifyingCollection<Product> source = new NotifyingCollection<Product>() { product };
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.All);

            view.EditItem(product);

            // Remove notification will result in an exception, but the removal will actually succeed
            ExceptionHelper.ExpectInvalidOperationException(() => source.Remove(product), string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.OperationNotAllowedDuringAddOrEdit, "Removing"));
            source.Add(product);

            // Replace notification will result in an exception, but the replacement will actually succeed
            ExceptionHelper.ExpectInvalidOperationException(() => source[0] = new Product(), string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.OperationNotAllowedDuringAddOrEdit, "Removing"));

            // Reset notifiation will result in an exception
            source.NotifyOnCollectionChanged = false;
            source.Clear();
            ExceptionHelper.ExpectInvalidOperationException(() => source.Reset(), string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.OperationNotAllowedDuringAddOrEdit, "Removing"));
        }

        [TestMethod]
        [Description("When adding an item, CollectionChanged events succeed when the add item remains in the collection")]
        public void CollectionChanged_CanRemoveEntityOtherThanCurrentAddItem()
        {
            Product product = new Product();
            NotifyingCollection<Product> source = new NotifyingCollection<Product>() { product };
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.All);

            Product addItem = (Product)view.AddNew();

            // Nothing to assert in these cases - lack of exception indicates success
            source.Remove(product);
            source.Add(product);
            source[source.IndexOf(product)] = new Product();
            source.Reset();
        }

        [TestMethod]
        [Description("When editing an item, CollectionChanged events succeed when the edit item remains in the collection")]
        public void CollectionChanged_CanRemoveEntityOtherThanCurrentEditItem()
        {
            Product product = new Product();
            Product editItem = new Product();
            NotifyingCollection<Product> source = new NotifyingCollection<Product>() { product, editItem };
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.All);

            view.EditItem(editItem);

            // Nothing to assert in these cases - lack of exception indicates success
            source.Remove(product);
            source.Add(product);
            source[source.IndexOf(product)] = new Product();
            source.Reset();
        }

        [TestMethod]
        [Description("Removing the only item in the collection results in a null CurrentItem")]
        public void CollectionChanged_RemoveOnlyItemClearsCurrency()
        {
            Product product = new Product();
            NotifyingCollection<Product> source = new NotifyingCollection<Product>() { product };
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.All);

            Assert.AreSame(product, view.CurrentItem, "CurrentItem before Remove");
            Assert.AreEqual<int>(0, view.CurrentPosition, "CurrentPosition before Remove");

            this.AssertCurrencyChangeCount(() => source.Remove(product), view, 1);
            Assert.IsNull(view.CurrentItem, "CurrentItem after Remove");
            Assert.AreEqual<int>(-1, view.CurrentPosition, "CurrentPosition after Remove");
        }

        [TestMethod]
        [Description("Removing an item before the CurrentItem adjusts CurrentPosition")]
        public void CollectionChanged_RemoveItemBeforeCurrentAdjustsPosition()
        {
            Product first = new Product();
            Product second = new Product();
            NotifyingCollection<Product> source = new NotifyingCollection<Product>() { first, second };
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.All);

            this.AssertCurrencyChangeCount(() => view.MoveCurrentToLast(), view, 1);
            Assert.AreSame(second, view.CurrentItem, "CurrentItem before Remove");
            Assert.AreEqual<int>(1, view.CurrentPosition, "CurrentPosition before Remove");

            this.AssertCurrencyChangeCount(() => source.Remove(first), view, 1);
            Assert.AreSame(second, view.CurrentItem, "CurrentItem after Remove");
            Assert.AreEqual<int>(0, view.CurrentPosition, "CurrentPosition after Remove");
        }

        [TestMethod]
        [Description("Removing the CurrentItem will retain CurrentPosition if there are items after the current item")]
        public void CollectionChanged_RemoveCurrentItemRetainsPosition()
        {
            Product first = new Product();
            Product second = new Product();
            NotifyingCollection<Product> source = new NotifyingCollection<Product>() { first, second };
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.All);

            Assert.AreSame(first, view.CurrentItem, "CurrentItem before Remove");
            Assert.AreEqual<int>(0, view.CurrentPosition, "CurrentPosition before Remove");

            this.AssertCurrencyChangeCount(() => source.Remove(first), view, 1);
            Assert.AreSame(second, view.CurrentItem, "CurrentItem after Remove");
            Assert.AreEqual<int>(0, view.CurrentPosition, "CurrentPosition after Remove");
        }

        [TestMethod]
        [Description("Removing the CurrentItem when it's the last item will set currency to the new last item")]
        public void CollectionChanged_RemoveCurrentItemAsLastItem()
        {
            Product first = new Product();
            Product second = new Product();
            NotifyingCollection<Product> source = new NotifyingCollection<Product>() { first, second };
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.All);

            this.AssertCurrencyChangeCount(() => view.MoveCurrentToLast(), view, 1);
            Assert.AreSame(second, view.CurrentItem, "CurrentItem before Remove");
            Assert.AreEqual<int>(1, view.CurrentPosition, "CurrentPosition before Remove");

            this.AssertCurrencyChangeCount(() => source.Remove(second), view, 1);
            Assert.AreSame(first, view.CurrentItem, "CurrentItem after Remove");
            Assert.AreEqual<int>(0, view.CurrentPosition, "CurrentPosition after Remove");
        }

        [TestMethod]
        [Description("Removing an item after the CurrentItem does not affect currency")]
        public void CollectionChanged_RemoveItemAfterCurrentRetainsCurrency()
        {
            Product first = new Product();
            Product second = new Product();
            NotifyingCollection<Product> source = new NotifyingCollection<Product>() { first, second };
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.All);

            Assert.AreSame(first, view.CurrentItem, "CurrentItem before Remove");
            Assert.AreEqual<int>(0, view.CurrentPosition, "CurrentPosition before Remove");

            this.AssertCurrencyChangeCount(() => source.Remove(second), view, 0);
            Assert.AreSame(first, view.CurrentItem, "CurrentItem after Remove");
            Assert.AreEqual<int>(0, view.CurrentPosition, "CurrentPosition after Remove");
        }

        [TestMethod]
        [Description("Adding an item to an empty collection will set that item as the current item")]
        public void CollectionChanged_AddFirstItemSetsCurrency()
        {
            NotifyingCollection<Product> source = new NotifyingCollection<Product>();
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.All);

            Assert.IsNull(view.CurrentItem, "CurrentItem before Add");
            Assert.AreEqual<int>(-1, view.CurrentPosition, "CurrentPosition before Add");

            Product product = new Product();
            this.AssertCurrencyChangeCount(() => source.Add(product), view, 1);
            Assert.AreSame(product, view.CurrentItem, "CurrentItem after Add");
            Assert.AreEqual<int>(0, view.CurrentPosition, "CurrentPosition after Add");
        }

        [TestMethod]
        [Description("Adding an item before the CurrentItem adjusts CurrentPosition")]
        public void CollectionChanged_AddItemBeforeCurrentAdjustsPosition()
        {
            Product second = new Product();
            NotifyingCollection<Product> source = new NotifyingCollection<Product>() { second };
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.All);

            Assert.AreSame(second, view.CurrentItem, "CurrentItem before Add");
            Assert.AreEqual<int>(0, view.CurrentPosition, "CurrentPosition before Remove");

            Product first = new Product();

            this.AssertCurrencyChangeCount(() => source.Insert(0, first), view, 1);
            Assert.AreSame(second, view.CurrentItem, "CurrentItem after Remove");
            Assert.AreEqual<int>(1, view.CurrentPosition, "CurrentPosition after Add");
        }

        [TestMethod]
        [Description("Adding an item after the CurrentItem does not affect currency")]
        public void CollectionChanged_MovingCurrentItemAdjustsPosition()
        {
            Product first = new Product();
            Product second = new Product();
            NotifyingCollection<Product> source = new NotifyingCollection<Product>() { first, second };
            EntityCollectionView<Product> view = new EntityCollectionView<Product>(source, EntitySetOperations.All);

            Assert.AreSame(first, view.CurrentItem, "CurrentItem before Move");
            Assert.AreEqual<int>(0, view.CurrentPosition, "CurrentPosition before Move");

            // Move the current item to the end, notifying with a reset
            source.NotifyOnCollectionChanged = false;
            source.Remove(first);
            source.Add(first);

            this.AssertCurrencyChangeCount(() => source.Reset(), view, 1);
            Assert.AreSame(first, view.CurrentItem, "CurrentItem after Remove");
            Assert.AreEqual<int>(1, view.CurrentPosition, "CurrentPosition after Remove");
        }

        #endregion

        #region Helper Methods

        private void AssertCurrencyChangeCount<T>(Action action, EntityCollectionView<T> view, int changeCount) where T : Entity
        {
            int _changingEvents = 0;
            int _changedEvents = 0;

            view.CurrentChanging += (sender, e) => Assert.IsTrue(++_changingEvents > _changedEvents, "A CurrentChanging event was raised after a CurrentChanged event");
            view.CurrentChanged += (sender, e) => Assert.IsTrue(++_changedEvents == _changingEvents, "There is a mismatch between CurrentChanged and CurrentChanging during a CurrentChanged event");

            // Perform the action
            action();

            Assert.AreEqual<int>(changeCount, _changingEvents, "CurrentChanging event count");
            Assert.AreEqual<int>(changeCount, _changedEvents, "CurrentChanged event count");
        }

        #endregion

        #region Helper Classes

        public class AppendOnlyCollection<T> : IEnumerable<T> where T : Entity
        {
            private Collection<T> _collection;

            public AppendOnlyCollection()
            {
                this._collection = new Collection<T>();
            }

            public void Add(T item)
            {
                this._collection.Add(item);
            }

            public IEnumerator<T> GetEnumerator()
            {
                return this._collection.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        public class NotifyingCollection<T> : Collection<T>, INotifyCollectionChanged where T : Entity
        {
            public bool NotifyOnCollectionChanged { get; set; }

            public NotifyingCollection()
            {
                this.NotifyOnCollectionChanged = true;
            }

            protected override void InsertItem(int index, T item)
            {
                base.InsertItem(index, item);

                if (this.NotifyOnCollectionChanged)
                {
                    this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
                }
            }

            protected override void RemoveItem(int index)
            {
                object item = this[index];
                base.RemoveItem(index);

                if (this.NotifyOnCollectionChanged)
                {
                    this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
                }
            }

            protected override void SetItem(int index, T item)
            {
                object oldItem = this[index];
                base.SetItem(index, item);

                if (this.NotifyOnCollectionChanged)
                {
                    this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, oldItem, index));
                }
            }

            public void Reset()
            {
                this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            #region INotifyCollectionChanged Members

            public event NotifyCollectionChangedEventHandler CollectionChanged = delegate { };

            #endregion
        }

        public class MockEntity : Entity, IEditableObject
        {
            private Action _beginEditCallback;
            private Action _cancelEditCallback;
            private Action _endEditCallback;

            public int BeginEditCalls
            {
                get;
                private set;
            }

            public int CancelEditCalls
            {
                get;
                private set;
            }

            public int EndEditCalls
            {
                get;
                private set;
            }

            public MockEntity()
            {
                this.BeginEditCalls = 0;
                this.CancelEditCalls = 0;
                this.EndEditCalls = 0;

                this._beginEditCallback = delegate { };
                this._cancelEditCallback = delegate { };
                this._endEditCallback = delegate { };
            }

            public MockEntity(Action beginEditCallback, Action cancelEditCallback, Action endEditCallback)
                : this()
            {
                this._beginEditCallback = beginEditCallback ?? this._beginEditCallback;
                this._cancelEditCallback = cancelEditCallback ?? this._cancelEditCallback;
                this._endEditCallback = endEditCallback ?? this._endEditCallback;
            }

            void IEditableObject.BeginEdit()
            {
                ++this.BeginEditCalls;
                this._beginEditCallback();
                base.BeginEdit();
            }

            void IEditableObject.CancelEdit()
            {
                ++this.CancelEditCalls;
                this._cancelEditCallback();
                base.CancelEdit();
            }

            void IEditableObject.EndEdit()
            {
                ++this.EndEditCalls;
                this._endEditCallback();
                base.EndEdit();
            }
        }

        #endregion
    }
}
