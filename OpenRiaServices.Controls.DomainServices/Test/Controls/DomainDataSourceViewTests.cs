using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using OpenRiaServices.DomainServices.Client;
using OpenRiaServices.DomainServices.Client.Test;
using System.Windows.Controls.Test;
using Cities;
using DataTests.Northwind.LTS;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices.LTS;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace System.Windows.Controls.DomainServices.Test
{
    /// <summary>
    /// Tests the <see cref="DomainDataSourceView"/> members.
    /// </summary>
    [TestClass]
    public class DomainDataSourceViewTests : ViewTestBase
    {
        #region View Properties

        [TestMethod]
        [Asynchronous]
        [Description("Tests the PageCount property on the view.")]
        public void PageCount()
        {
            DomainDataSource dds = new DomainDataSource();
            dds.QueryName = "GetPurchaseOrdersQuery";
            dds.DomainContext = new Catalog();

            int pageCountChanged = 0;
            ((INotifyPropertyChanged)dds.DataView).PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == "PageCount")
                {
                    pageCountChanged++;
                }
            };

            Assert.AreEqual(0, dds.DataView.PageCount, "Page count should be 0 by default");

            dds.PageSize = 5;

            Assert.AreEqual(1, dds.DataView.PageCount, "Page count should be 1 once PageSize is set");
            Assert.AreEqual(1, pageCountChanged, "The PageCount should have changed.");
            pageCountChanged = 0;

            bool loaded = false;

            dds.Load();
            dds.LoadedData += (sender, e) => loaded = true;

            this.EnqueueConditional(() => loaded);

            this.EnqueueCallback(() =>
            {
                Assert.AreEqual(5, dds.DataView.Count, "Count after the initial load");
                Assert.AreEqual(0, dds.DataView.PageIndex, "PageIndex after the initial load");
                Assert.IsTrue(dds.DataView.TotalItemCount > dds.DataView.Count, "After the initial load, TotalItemCount should be greater than Count");
                Assert.AreEqual(PagingHelper.CalculatePageCount(dds.DataView.TotalItemCount, dds.DataView.PageSize), dds.DataView.PageCount, "PageCount after the initial load");
                Assert.AreEqual(1, pageCountChanged, "The PageCount should have changed.");
            });

            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Description("Tests the default value and modifies the DataView.Culture property.")]
        public void Culture()
        {
            DomainDataSource dds = new DomainDataSource();
            dds.QueryName = "GetPurchaseOrdersQuery";
            dds.DomainContext = new Catalog();

            ICollectionView collectionView = dds.DataView;

            Assert.AreEqual(CultureInfo.CurrentCulture, collectionView.Culture);
            collectionView.Culture = CultureInfo.InvariantCulture;
            Assert.AreEqual(CultureInfo.InvariantCulture, collectionView.Culture);
        }

        #endregion View Properties

        #region View Methods

        [TestMethod]
        [Description("Tests the CanAdd property and Add method on the view.")]
        public void Add()
        {
            DomainDataSource dds = new DomainDataSource();
            dds.DomainContext = new MockContext<City>(EntitySetOperations.All);
            dds.QueryName = "Basic";

            Assert.IsTrue(dds.DataView.CanAdd, "We should be able to add to the DataView.");
            Assert.AreEqual(0, dds.DataView.Count, "The default DataView count should be 0.");
            dds.DataView.Add(new City() { Name = "New City" });
            Assert.AreEqual(1, dds.DataView.Count, "The new DataView count should be 1.");

            dds = new DomainDataSource();
            dds.DomainContext = new MockContext<City>(EntitySetOperations.None);
            dds.QueryName = "Basic";

            Assert.IsFalse(dds.DataView.CanAdd, "We should not be able to add to the DataView.");
            Assert.AreEqual(0, dds.DataView.Count, "The default DataView count should be 0.");
            ExceptionHelper.ExpectException<InvalidOperationException>(
                () => dds.DataView.Add(new City() { Name = "New City" }));
        }

        [TestMethod]
        [WorkItem(783181)]
        [Description("Calling Refresh should invoke a load")]
        public void RefreshInvokesLoad()
        {
            int loadInvocationCount = 0;
            Action loadCallback = () => loadInvocationCount++;

            ICollectionView view = GetConfigurableView(EntitySetOperations.All, loadCallback);
            view.Refresh();

            Assert.AreEqual<int>(1, loadInvocationCount, "The load callback should have been invoked once when Refresh was called");
        }

        [TestMethod]
        [WorkItem(783181)]
        [Description("Disposing of a DeferRefresh should invoke a load")]
        public void DeferRefreshDisposalInvokesLoad()
        {
            int loadInvocationCount = 0;
            Action loadCallback = () => loadInvocationCount++;

            ICollectionView view = GetConfigurableView(EntitySetOperations.All, loadCallback);
            view.DeferRefresh().Dispose();

            Assert.AreEqual<int>(1, loadInvocationCount, "The load callback should have been invoked once when the DeferRefresh was disposed");
        }

        [TestMethod]
        [WorkItem(753403)]
        [Description("When the EntitySet does not allow editing, we should still be able to call EditItem without an exception")]
        public void NonEditableEntitySetDoesNotThrowFromEditItem()
        {
            DomainDataSourceView dataView = GetConfigurableView(EntitySetOperations.Add | EntitySetOperations.Remove);
            City newCity = new City { Name = "City", StateName = "ST" };
            dataView.Add(newCity);

            // Test is to ensure that no exception is thrown from calling EditItem
            ((IEditableCollectionView)dataView).EditItem(newCity);
        }

        [TestMethod]
        [WorkItem(753403)]
        [Description("When the EntitySet does not allow editing, we should still be able to call CommitEdit without an exception")]
        public void NonEditableEntitySetDoesNotThrowFromCommitEdit()
        {
            DomainDataSourceView dataView = GetConfigurableView(EntitySetOperations.Add | EntitySetOperations.Remove);
            City newCity = new City { Name = "City", StateName = "ST" };
            dataView.Add(newCity);

            IEditableCollectionView iecv = ((IEditableCollectionView)dataView);
            iecv.EditItem(newCity);

            // Test is to ensure that no exception is thrown from calling CommitEdit
            iecv.CommitEdit();
        }

        [TestMethod]
        [WorkItem(885679)]
        [Description("When a CollectionChanged.Reset event occurs, a Reset event is raised from the PagedEntityCollectionView as well")]
        public void ResetEventRelayed()
        {
            Product first = new Product();
            Product second = new Product();
            NotifyingCollection<Product> source = new NotifyingCollection<Product>() { first, second };
            PagedEntityCollectionView<Product> pecView = new PagedEntityCollectionView<Product>(source);
            DomainDataSourceView view = new DomainDataSourceView(pecView);

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
            PagedEntityCollectionView<Product> pecView = new PagedEntityCollectionView<Product>(source);
            DomainDataSourceView view = new DomainDataSourceView(pecView);

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
            PagedEntityCollectionView<Product> pecView = new PagedEntityCollectionView<Product>(source);
            DomainDataSourceView view = new DomainDataSourceView(pecView);

            Product third = new Product();

            this.AssertCollectionChanged(
                () => source.Add(third),
                view,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, third, 2),
                "when adding the third item.");
        }

        #endregion View Methods

        #region Helper Methods

        /// <summary>
        /// Gets a <see cref="DomainDataSourceView"/> that uses a mocked <see cref="PagedEntityList"/>
        /// as its source, configured to support the operations specified.
        /// </summary>
        /// <param name="operationsSupported">The operations to be supported on the mock <see cref="EntitySet"/>.</param>
        /// <returns>The configured <see cref="DomainDataSourceView"/> that uses mocks.</returns>
        private static DomainDataSourceView GetConfigurableView(EntitySetOperations operationsSupported)
        {
            return GetConfigurableView(operationsSupported, () => { });
        }

        /// <summary>
        /// Gets a <see cref="DomainDataSourceView"/> that uses a mocked <see cref="PagedEntityList"/>
        /// as its source, configured to support the operations specified, and using the specified
        /// <paramref name="refreshCallback"/> as the <see cref="Action"/> to invoke when a
        /// <see cref="DomainDataSourceView.Refresh"/> is triggered.
        /// </summary>
        /// <param name="operationsSupported">The operations to be supported on the mock <see cref="EntitySet"/>.</param>
        /// <param name="refreshCallback">The <see cref="Action"/> to call when a refresh occurs.</param>
        /// <returns>The configured <see cref="DomainDataSourceView"/> that uses mocks.</returns>
        private static DomainDataSourceView GetConfigurableView(EntitySetOperations operationsSupported, Action refreshCallback)
        {
            MockEntityContainer ec = new MockEntityContainer();
            ec.CreateSet<City>(operationsSupported);

            IPagedEntityList pagedList = new MockPagedEntityList(ec.GetEntitySet<City>(), null);
            PagedEntityCollectionView ecv = new PagedEntityCollectionView(pagedList, refreshCallback);

            return new DomainDataSourceView(ecv);
        }

        #endregion
    }
}
