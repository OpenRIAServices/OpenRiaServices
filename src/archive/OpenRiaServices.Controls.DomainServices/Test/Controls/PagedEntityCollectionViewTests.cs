using System;
using System.Collections.Specialized;
using System.ComponentModel;
using OpenRiaServices.DomainServices.Client;
using Cities;
using DataTests.AdventureWorks.LTS;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.Controls.DomainServices.Test
{
    /// <summary>
    /// Tests the <see cref="PagedEntityCollectionView"/> members.
    /// </summary>
    [TestClass]
    public class PagedEntityCollectionViewTests : ViewTestBase
    {
        [TestMethod]
        [WorkItem(885679)]
        [Description("When a CollectionChanged.Reset event occurs, a Reset event is raised from the PagedEntityCollectionView as well")]
        public void ResetEventRelayed()
        {
            Product first = new Product();
            Product second = new Product();
            NotifyingCollection<Product> source = new NotifyingCollection<Product>() { first, second };
            PagedEntityCollectionView<Product> view = new PagedEntityCollectionView<Product>(source);

            this.AssertCollectionChanged(
                () => source.Reset(),
                view,
                new NotifyCollectionChangedEventArgs[]
                {
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset),
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset), // Bug 706239
                },
                "when resetting the collection.");
        }

        [TestMethod]
        [WorkItem(885679)]
        [Description("When a CollectionChanged.Remove event occurs, a Remove event is raised from the PagedEntityCollectionView as well")]
        public void RemoveEventRelayed()
        {
            Product first = new Product();
            Product second = new Product();
            NotifyingCollection<Product> source = new NotifyingCollection<Product>() { first, second };
            PagedEntityCollectionView<Product> view = new PagedEntityCollectionView<Product>(source);

            this.AssertCollectionChanged(
                () => source.Remove(first),
                view,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, first, 0),
                "when removing the first item.");
        }

        [TestMethod]
        [WorkItem(885679)]
        [Description("When a CollectionChanged.Add event occurs, an Add event is raised from the PagedEntityCollectionView as well")]
        public void AddEventRelayed()
        {
            Product first = new Product();
            Product second = new Product();
            NotifyingCollection<Product> source = new NotifyingCollection<Product>() { first, second };
            PagedEntityCollectionView<Product> view = new PagedEntityCollectionView<Product>(source);

            Product third = new Product();

            this.AssertCollectionChanged(
                () => source.Add(third),
                view,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, third, 2),
                "when adding the third item.");
        }

        [TestMethod]
        [WorkItem(177146)]
        [Description("Tests that currency changes can be avoided when committing an edit.")]
        public void CancelingCurrencyChangeIsRespectedOnCommitEdit()
        {
            City first = new City { Name = "1" };
            City second = new City { Name = "2" };
            MockEntityContainer ec = new MockEntityContainer();
            ec.CreateSet<City>(EntitySetOperations.All);

            IPagedEntityList pagedList = new MockPagedEntityList(ec.GetEntitySet<City>(), null);
            PagedEntityCollectionView view = new PagedEntityCollectionView(pagedList, () => { });

            view.Add(first);
            view.Add(second);

            bool canChangeCurrency = true;
            CurrentChangingEventHandler changingHandler = (sender, e) =>
            {
                if (!canChangeCurrency)
                {
                    Assert.IsTrue(e.IsCancelable,
                        "Event should be cancelable when commiting an edit.");
                    e.Cancel = true;
                }
            };
            EventHandler changedHandler = (sender, e) =>
            {
                if (!canChangeCurrency)
                {
                    Assert.Fail("Currency changes should only occur when canChangeCurrency is true.");
                }
            };
            view.CurrentChanging += changingHandler;
            view.CurrentChanged += changedHandler;

            view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            Assert.IsTrue(view.MoveCurrentTo(second),
                "We should be able to move current to the second item.");

            // Commit an edit but do not allow the currency to change
            canChangeCurrency = false;
            view.EditItem(second);
            second.Name = "0";
            view.CommitEdit();

            Assert.AreEqual(view[1], second,
                "The edited item should not have moved.");

            // Commit an edit and this time allow the currency to change
            canChangeCurrency = true;
            view.EditItem(second);
            second.Name = "00";
            view.CommitEdit();

            Assert.AreEqual(view[0], second,
                "The edited item should have moved to the first position in the view.");
        }

        [TestMethod]
        [WorkItem(177146)]
        [Description("Tests that currency changes can be avoided when committing an addition.")]
        public void CancelingCurrencyChangeIsRespectedOnCommitNew()
        {
            City first = new City { Name = "1" };
            City second = new City { Name = "2" };
            MockEntityContainer ec = new MockEntityContainer();
            ec.CreateSet<City>(EntitySetOperations.All);

            IPagedEntityList pagedList = new MockPagedEntityList(ec.GetEntitySet<City>(), null);
            PagedEntityCollectionView view = new PagedEntityCollectionView(pagedList, () => { });

            view.Add(first);
            view.Add(second);

            bool canChangeCurrency = true;
            CurrentChangingEventHandler changingHandler = (sender, e) =>
            {
                if (!canChangeCurrency)
                {
                    Assert.IsTrue(e.IsCancelable,
                        "Event should be cancelable when committing an addition.");
                    e.Cancel = true;
                }
            };
            EventHandler changedHandler = (sender, e) =>
            {
                Assert.IsTrue(canChangeCurrency,
                    "Currency changes should only occur when canChangeCurrency is true.");
            };
            view.CurrentChanging += changingHandler;
            view.CurrentChanged += changedHandler;

            view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            City third = (City)view.AddNew(); // This gets added to the beginning of the list
            Assert.IsTrue(view.MoveCurrentTo(third),
                "We should be able to move current to the third item.");

            // Commit an add but do not allow the currency to change
            canChangeCurrency = false;
            third.Name = "3";
            view.CommitNew();

            Assert.AreEqual(view[0], third,
                "The edited item should not have moved.");

            // Commit an edit and this time allow the currency to change
            canChangeCurrency = true;
            view.EditItem(third);
            third.Name = "33";
            view.CommitEdit();

            Assert.AreEqual(view[2], third,
                "The edited item should have moved to the last position in the view.");
        }
    }
}
