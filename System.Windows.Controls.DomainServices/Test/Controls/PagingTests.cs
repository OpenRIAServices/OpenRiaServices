using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using Cities;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;
using TestDomainServices.LTS;

namespace System.Windows.Controls.DomainServices.Test
{
    /// <summary>
    /// Tests the Paging aspects of the <see cref="DomainDataSource"/> feature.
    /// </summary>
    /// <remarks>
    /// Some other test classes also utilize paging to verify other features work
    /// in conjunction with paging.  But these tests verify the core paging feature.
    /// </remarks>
    [TestClass]
    public class PagingTests : DomainDataSourceTestBase
    {
        [TestMethod]
        [Asynchronous]
        [Description("Tests that the PageSize property functions properly.")]
        public void Paging()
        {
            CityDomainContext context = new CityDomainContext();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._pagedCollectionView.PageSize = 10;
                this._dds.Load();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(10, this._view.Count, "this._ecv.Count should be 10 after the initial load");
                Assert.AreEqual(10, context.Cities.Count, "context.Cities.Count should be 10 after the initial load");

                this._view.MoveToNextPage();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(1, this._view.Count, "this._ecv.Count should be 1 after moving to the 2nd page");
                Assert.AreEqual(11, context.Cities.Count, "context.Cities.Count should be 11 after moving to the 2nd page");

                this._dds.PageSize = 0;
                Assert.AreEqual(0, this._dds.PageSize, "DDS PageSize should immediately reflect 0");
                Assert.AreEqual(0, this._view.PageSize, "this._ecv.PageSize should immediately reflect 0");
                Assert.AreEqual(-1, this._view.PageIndex, "this._ecv.PageIndex should immediately reflect -1");

                this._dds.Load();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(11, this._view.Count, "this._ecv.Count should be 11 after turning off paging and reloading");
                Assert.AreEqual(11, context.Cities.Count, "context.Cities.Count should be 11 after turning off paging and reloading");
                Assert.AreEqual(-1, this._view.PageIndex, "this._ecv.PageIndex should remain -1 after reloading");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests the LoadSize property and the PageSize property together with a page size larger than LoadSize.")]
        public void PagingLargePageSize()
        {
            CityDomainContext context = new CityDomainContext();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.LoadSize = 5;
                this._dds.PageSize = 10;
                this._dds.Load();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(10, this._pagedCollectionView.ItemCount, "ItemCount should be 10 after initial load");
                Assert.AreEqual(10, this._view.Count, "Count should be 10 after initial load");

                this._view.MoveToNextPage();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(11, this._pagedCollectionView.ItemCount, "ItemCount should be 11 after moving to the 2nd page");
                Assert.AreEqual(1, this._view.Count, "Count should be 1 after moving to the 2nd page");
                Assert.AreEqual(11, context.Cities.Count, "context.Cities.Count should be 11 after moving to the 2nd page");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests the LoadSize property and the PageSize property together with a page size smaller than LoadSize.")]
        public void PagingSmallPageSize()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.LoadSize = 10;
                this._dds.PageSize = 5;
                this._dds.Load();
                Assert.IsTrue(this._dds.IsLoadingData, "IsLoadingData should be true on the initial load");
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(0, this._view.PageIndex, "PageIndex should be 0 after the initial load");
                Assert.AreEqual(10, this._pagedCollectionView.ItemCount, "ItemCount should be 10 after the initial load");
                Assert.AreEqual(5, this._view.Count, "Count should be 5 after the initial load");

                // Move to the 2nd page, which is already loaded
                this._ddsLoadingDataExpected = 0;
                this._view.MoveToNextPage();
            });

            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(1, this._view.PageIndex, "PageIndex should be 1 on the 2nd page");
                Assert.AreEqual(10, this._pagedCollectionView.ItemCount, "ItemCount should still be 10 on the 2nd page");
                Assert.AreEqual(5, this._view.Count, "Count should be 5 on the 2nd page");

                // Load the 3rd page
                this._view.MoveToNextPage();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(2, this._view.PageIndex, "PageIndex should be 2 on the 3rd page");
                Assert.AreEqual(11, this._pagedCollectionView.ItemCount, "ItemCount should be 11 on the 3rd page");
                Assert.AreEqual(1, this._view.Count, "Count should be 1 on the 3rd page");
                Assert.AreEqual(11, this._view.TotalItemCount, "TotalItemCount should be 11 on the 3rd page");

                // Move past the last page, this will reload the final block of cities
                this._view.MoveToNextPage();
            });

            this.AssertLoadingData();
            this.AssertNoPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(2, this._view.PageIndex, "PageIndex should remain 2 after paging past the end");
                Assert.AreEqual(11, this._pagedCollectionView.ItemCount, "ItemCount should be 11 after paging past the end");
                Assert.AreEqual(1, this._view.Count, "Count should remain 1 after paging past the end");
                Assert.AreEqual(11, this._view.TotalItemCount, "TotalItemCount should remain 11 after paging past the end");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests the LoadSize property and the PageSize property together while making use of the DataView's MoveToXXXXPage methods.")]
        public void PagingNavigation()
        {
            // there are only 11 items in the collection
            // so the pages will have counts of {3,3,3,2}.
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.LoadSize = 6;
                this._dds.PageSize = 3;

                // Load 1st and 2nd pages
                this._asyncEventFailureMessage = "Initial Load";
                this._dds.Load();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(0, this._view.PageIndex, "PageIndex should be 0 on Initial Load");
                Assert.AreEqual(3, this._view.Count, "Count is 3 on Initial Load");
                Assert.AreEqual(6, this._pagedCollectionView.ItemCount, "ItemCount is 6 on Initial Load");
                Assert.AreEqual(-1, this._view.TotalItemCount, "TotalItemCount is -1 on Initial Load");

                // No load, navigate to 2nd page
                this._ddsLoadingDataExpected = 0;
                this._asyncEventFailureMessage = "MoveToNextPage()";
                this._view.MoveToNextPage();
            });

            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetPageChanged();

                Assert.AreEqual(1, this._view.PageIndex, "PageIndex should be 1 after MoveToNextPage");
                Assert.AreEqual(3, this._view.Count, "Count is 3 on MoveToNextPage");
                Assert.AreEqual(6, this._pagedCollectionView.ItemCount, "ItemCount is 6 on MoveToNextPage");
                Assert.AreEqual(-1, this._view.TotalItemCount, "TotalItemCount is -1 on MoveToNextPage");

                // No load, navigate to 1st page
                this._asyncEventFailureMessage = "MoveToPreviousPage()";
                this._view.MoveToPreviousPage();
            });

            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(0, this._view.PageIndex, "PageIndex should be 0 after MoveToPreviousPage");
                Assert.AreEqual(3, this._view.Count, "MoveToPreviousPage");
                Assert.AreEqual(6, this._pagedCollectionView.ItemCount, "ItemCount is 6 on MoveToPreviousPage");
                Assert.AreEqual(-1, this._view.TotalItemCount, "TotalItemCount is -1 on MoveToPreviousPage");

                // Load 3rd and 4th pages
                this._asyncEventFailureMessage = "MoveToPage(3)";
                this._view.MoveToPage(3);
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(3, this._view.PageIndex, "PageIndex should be 3 after MoveToPage(3)");
                Assert.AreEqual(2, this._view.Count, "Count is 2 on MoveToPage(3)");
                Assert.AreEqual(11, this._pagedCollectionView.ItemCount, "ItemCount is 11 on MoveToPage(3)");
                Assert.AreEqual(11, this._view.TotalItemCount, "TotalItemCount should be 11 on MoveToPage(3)");

                // No load, navigate to 1st page (which has already been loaded)
                this._ddsLoadingDataExpected = 0;
                this._asyncEventFailureMessage = "MoveToFirstPage()";
                this._view.MoveToFirstPage();
            });

            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetPageChanged();

                Assert.AreEqual(0, this._view.PageIndex, "PageIndex should be 0 after MoveToFirstPage");
                Assert.AreEqual(3, this._view.Count, "Count is 3 on MoveToFirstPage");
                Assert.AreEqual(11, this._pagedCollectionView.ItemCount, "ItemCount is 11 on MoveToFirstPage");
                Assert.AreEqual(11, this._view.TotalItemCount, "TotalItemCount should be 11 on MoveToFirstPage");

                // No load, navigate to 4th page (which has already been loaded)
                this._asyncEventFailureMessage = "MoveToLastPage()";
                this._view.MoveToLastPage();
            });

            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(3, this._view.PageIndex, "PageIndex should be 3 after MoveToLastPage");
                Assert.AreEqual(2, this._view.Count, "Count is 2 on MoveToLastPage");
                Assert.AreEqual(11, this._pagedCollectionView.ItemCount, "ItemCount is 11 on MoveToLastPage");
                Assert.AreEqual(11, this._view.TotalItemCount, "TotalItemCount should be 11 on MoveToLastPage");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(791019)]
        [Description("Paging back to a previously loaded page doesn't invoke a load")]
        public void PagingNavigationToLoadedPage()
        {
            // there are only 11 items in the collection
            // so the pages will have counts of {3,3,3,2}.
            // We are testing a load of the first and second pages,
            // then skipping to the fourth page (loading it), and
            // then moving to the second page (already loaded).
            // Then we'll move to the third page to ensure it's
            // loaded.
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.LoadSize = 6;
                this._dds.PageSize = 3;

                // Load 1st and 2nd pages
                this._asyncEventFailureMessage = "Initial Load";
                this._dds.Load();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual<int>(0, this._view.PageIndex, "PageIndex after Initial Load");

                // Load the 4th page
                this._asyncEventFailureMessage = "MoveToPage(3)";
                this._view.MoveToPage(3);
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual<int>(3, this._view.PageIndex, "PageIndex after MoveToPage(3)");

                // Navigate to the 2nd page, which is already loaded
                this._asyncEventFailureMessage = "MoveToPage(1)";
                this._ddsLoadingDataExpected = 0;
                this._ddsLoadedDataExpected = 0;
                this._view.MoveToPage(1);
            });

            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(1, this._view.PageIndex, "PageIndex after MoveToPage(1)");

                // Navigate to the 3rd page, which is already loaded
                this._asyncEventFailureMessage = "MoveToPage(2)";
                this._view.MoveToPage(2);
            });

            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(2, this._view.PageIndex, "PageIndex after MoveToPage(2)");
            });

            EnqueueTestComplete();
        }


        [TestMethod]
        [Asynchronous]
        [Description("Loading 2 pages at once will pre-load the next page with each server load")]
        public void LoadTwoPagesAtOnce()
        {
            CityDomainContext context = new CityDomainContext();

            EnqueueCallback(() =>
            {
                this._dds.LoadSize = 4;
                this._dds.PageSize = 2;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.Load();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(2, this._view.Count, "this._ecv.Count should be 2 after the initial load");
                Assert.AreEqual(4, context.Cities.Count, "context.Cities.Count should be 4 after the initial load");

                this._ddsLoadingDataExpected = 0;
                this._view.MoveToNextPage();
            });

            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(2, this._view.Count, "this._ecv.Count should be 2 after moving to the 2nd page");
                Assert.AreEqual(4, context.Cities.Count, "context.Cities.Count should still be 4 after moving to the 2nd page");

                this._view.MoveToNextPage();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(2, this._view.Count, "this._ecv.Count should be 2 after moving to the 3rd page");
                Assert.AreEqual(8, context.Cities.Count, "context.Cities.Count should be 8 after moving to the 3rd page");

                this._view.MoveToNextPage();
            });

            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(2, this._view.Count, "this._ecv.Count should be 2 after moving to the 4th page");
                Assert.AreEqual(8, context.Cities.Count, "context.Cities.Count should be 8 after moving to the 4th page");

                this._view.MoveToNextPage();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(2, this._view.Count, "this._ecv.Count should be 2 after moving to the 5th page");
                Assert.AreEqual(11, context.Cities.Count, "context.Cities.Count should be 11 after moving to the 5th page");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Loading 3 pages at once will pre-load with the current page centered")]
        public void LoadThreePagesAtOnce()
        {
            CityDomainContext context = new CityDomainContext();

            EnqueueCallback(() =>
            {
                // Paging and loading pattern: {(2,2,2), (2,2,1)}
                this._dds.LoadSize = 6;
                this._dds.PageSize = 2;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.Load();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(2, this._view.Count, "this._ecv.Count should be 2 after the initial load");
                Assert.AreEqual(6, this._pagedCollectionView.ItemCount, "this._ecv.ItemCount should be 6 after the initial load");
                Assert.AreEqual(6, context.Cities.Count, "context.Cities.Count should be 6 after the initial load");

                this._ddsLoadingDataExpected = 0;
                this._view.MoveToNextPage();
            });

            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetPageChanged();

                Assert.AreEqual(2, this._view.Count, "this._ecv.Count should be 2 after moving to the 2nd page");
                Assert.AreEqual(6, this._pagedCollectionView.ItemCount, "this._ecv.ItemCount should be 6 after moving to the 2nd page");
                Assert.AreEqual(6, context.Cities.Count, "context.Cities.Count should still be 6 after moving to the 2nd page");

                this._view.MoveToNextPage();
            });

            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(2, this._view.Count, "this._ecv.Count should be 2 after moving to the 3rd page");
                Assert.AreEqual(6, this._pagedCollectionView.ItemCount, "this._ecv.ItemCount should be 6 after moving to the 3rd page");
                Assert.AreEqual(6, context.Cities.Count, "context.Cities.Count should be 6 after moving to the 3rd page");

                this._view.MoveToNextPage();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(2, this._view.Count, "this._ecv.Count should be 2 after moving to the 4th page");
                Assert.AreEqual(11, this._pagedCollectionView.ItemCount, "this._ecv.ItemCount should be 11 after moving to the 4th page");
                Assert.AreEqual(11, context.Cities.Count, "context.Cities.Count should be 11 after moving to the 4th page");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Paging past the end of the data, from the last page, will result in the last page being loaded (TotalItemCount is derived)")]
        public void PagingPastEndFromLastPageTotalItemCountDerived()
        {
            CityDomainContext context = new CityDomainContext();

            EnqueueCallback(() =>
            {
                this._dds.PageSize = 6;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.Load();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                string message = "First load";
                Assert.AreEqual(6, this._view.Count, message);
                Assert.AreEqual(0, this._view.PageIndex, message);
                Assert.AreEqual("Redmond", (this._view[0] as City).Name, message);
                Assert.AreEqual("Bellevue", (this._view[1] as City).Name, message);
                Assert.AreEqual("Duvall", (this._view[2] as City).Name, message);
                Assert.AreEqual("Carnation", (this._view[3] as City).Name, message);
                Assert.AreEqual("Everett", (this._view[4] as City).Name, message);
                Assert.AreEqual("Tacoma", (this._view[5] as City).Name, message);

                this._view.MoveToPage(1);
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            Action<string> verifyLastPageLoaded = (string message) =>
            {
                Assert.AreEqual(5, this._view.Count, message);
                Assert.AreEqual(1, this._view.PageIndex, message);
                Assert.AreEqual("Ashland", (this._view[0] as City).Name, message);
                Assert.AreEqual("Santa Barbara", (this._view[1] as City).Name, message);
                Assert.AreEqual("Orange", (this._view[2] as City).Name, message);
                Assert.AreEqual("Oregon", (this._view[3] as City).Name, message);
                Assert.AreEqual("Toledo", (this._view[4] as City).Name, message);
            };

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                verifyLastPageLoaded("After loading page 1");

                this._view.MoveToPage(5);
            });

            // Attempt to load the non-existent page, then load the last page
            this.AssertLoadingData(2);
            this.AssertNoPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                verifyLastPageLoaded("After paging past the end");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(891597)]
        [Description("Tests that the second load started as a result of paging past the last page is cancelable.")]
        public void PagingPastEndTwiceCancelsSecondLoad()
        {
            CityDomainContext context = new CityDomainContext();
            int pageSize = 6;
            int cityCount = new CityData().Cities.Count;
            int lastPageIndex = (cityCount / pageSize) - ((cityCount % pageSize == 0) ? 1 : 0);

            EnqueueCallback(() =>
            {
                this._dds.PageSize = pageSize;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.Load();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                // We need to make sure the second load caused by paging past the last page is cancelable.
                // To do this, we'll load 4 times. The first is caused by moving past the last page. The second
                // is automatically started when the first load does not return any entities and shifts back to
                // the last page. The third and fourth move past and back again and are started such that the
                // third load cancels the second.
                this._dds.LoadingData += (sender, e) =>
                {
                    // We only want to reload once to cancel the second load attempt
                    if (this._ddsLoadingData == 2)
                    {
                        this._view.MoveToPage(lastPageIndex + 2);
                    }
                };
                this._dds.LoadedData += (sender, e) =>
                {
                    Assert.IsTrue((this._ddsLoadedData == 2) == e.Cancelled,
                        "Only the second load should have been canceled.");
                };

                this._view.MoveToPage(lastPageIndex + 1);
            });

            this.AssertLoadingData(4);

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Paging past the end of the data, skipping the last page, will result in the first page being loaded (TotalItemCount is unknown)")]
        public void PagingPastEndSkippingLastPageUnknownTotalItemCount()
        {
            CityDomainContext context = new CityDomainContext();

            EnqueueCallback(() =>
            {
                this._dds.PageSize = 6;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.Load();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            Action<string> verifyFirstPageLoaded = (string message) =>
            {
                Assert.AreEqual(6, this._view.Count, message);
                Assert.AreEqual(0, this._view.PageIndex, message);
                Assert.AreEqual("Redmond", (this._view[0] as City).Name, message);
                Assert.AreEqual("Bellevue", (this._view[1] as City).Name, message);
                Assert.AreEqual("Duvall", (this._view[2] as City).Name, message);
                Assert.AreEqual("Carnation", (this._view[3] as City).Name, message);
                Assert.AreEqual("Everett", (this._view[4] as City).Name, message);
                Assert.AreEqual("Tacoma", (this._view[5] as City).Name, message);
            };

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                verifyFirstPageLoaded("First load");

                this._view.MoveToPage(5);
            });

            // Attempt to load the non-existent page, then load the first page
            this.AssertLoadingData(2);
            this.AssertNoPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                verifyFirstPageLoaded("After paging past the end");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Paging past the end of the data, skipping the last page, will result in the last page being loaded (TotalItemCount is known)")]
        public void PagingPastEndSkippingLastPageKnownTotalItemCount()
        {
            Catalog context = new Catalog();

            EnqueueCallback(() =>
            {
                this._dds.PageSize = 5;
                this._dds.QueryName = "GetPurchaseOrdersQuery";
                this._dds.DomainContext = context;
                this._dds.Load();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            int totalItemCount = 0;

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(5, this._view.Count, "Count after the initial load");
                Assert.AreEqual(0, this._view.PageIndex, "PageIndex after the initial load");
                Assert.IsTrue(this._view.TotalItemCount > this._view.Count, "After the initial load, TotalItemCount is expected to be greater than the Count");
                Assert.IsTrue(this._view.PageCount > 0, "After the initial load, PageCount is expected to be greater than 0");

                totalItemCount = this._view.TotalItemCount;

                // Move well beyond the end
                this._view.MoveToPage(this._view.PageCount + 100);
            });

            this.AssertLoadingData(2);
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                int countOnLastPage = this._view.TotalItemCount % this._view.PageSize;
                if (countOnLastPage == 0)
                {
                    countOnLastPage = this._view.PageSize;
                }

                Assert.AreEqual(countOnLastPage, this._view.Count, "Count after changing page");
                Assert.AreEqual(PagingHelper.CalculatePageCount(this._view.TotalItemCount, this._view.PageSize) - 1, this._view.PageIndex, "PageIndex after changing page");
                Assert.AreEqual(totalItemCount, this._view.TotalItemCount, "TotalItemCount after changing page");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("The expected events occur during a local paging move")]
        public void PagingLocalEventSequence()
        {
            List<string> events = null;
            List<string> expectedEvents = new List<string>
            {
                "PageChanging: 1",
                "PropertyChanged: IsPageChanging(True)",
                "End synchronous events",
                "PropertyChanged: PageIndex(1)",
                "CurrentChanging",
                "CurrentChanged: City : {Pierce,Tacoma,WA}",
                "PropertyChanged: CurrentItem(City : {Pierce,Tacoma,WA})",
                "PropertyChanged: IsPageChanging(False)",
                "PageChanged: 1",
                "CollectionChanged: Reset",
                "CollectionChanged: Reset", // TODO: Change from 2 Reset events to 1 when bug 709185 is fixed and our workaround can be removed
                "PropertyChanged: Count(5)"
            };

            bool hasLoaded = false;
            ((INotifyPropertyChanged)this._view).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Count" && this._view.Count == 5)
                {
                    hasLoaded = true;
                }
            };

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.LoadSize = 20;
                this._dds.PageSize = 5;
                this._dds.Load();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            // Wait until the count is updated as 5 through a PropertyChanged notification
            EnqueueConditional(() => hasLoaded);

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                events = this.TrackEventDetails();
                this._pagedCollectionView.MoveToNextPage();
                events.Add("End synchronous events");
            });

            EnqueueDelay(1000);

            EnqueueCallback(() =>
            {
                Assert.IsTrue(expectedEvents.SequenceEqual(events), this.GetEventDetailMismatch(expectedEvents, events));
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("The expected events occur during a server paging move")]
        public void PagingServerEventSequence()
        {
            List<string> events = null;
            List<string> expectedEvents = new List<string>
            {
                "PageChanging: 1",
                "PropertyChanged: IsPageChanging(True)",
                "End synchronous events",
                "LoadingData: GetCities",
                "PropertyChanged: CanAdd(False)",
                "PropertyChanged: PageIndex(1)",
                "PropertyChanged: CanAdd(True)",
                "CurrentChanging",
                "CurrentChanged: {0}",
                "PropertyChanged: CurrentItem({0})",
                "PropertyChanged: ItemCount",
                "LoadedData: 5",
                "PropertyChanged: IsPageChanging(False)",
                "PageChanged: 1",					
                "CollectionChanged: Reset",
                "CollectionChanged: Reset", // TODO: Change from 2 Reset events to 1 when bug 709185 is fixed and our workaround can be removed
                "PropertyChanged: Count(5)"
            };

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.PageSize = 5;
                this._dds.Load();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                events = this.TrackEventDetails();
                this._pagedCollectionView.MoveToNextPage();
                events.Add("End synchronous events");
            });

            this.AssertLoadingData();
            EnqueueDelay(1000);

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                City currentCity = (City)this._view[0];
                this.AssertEventDetailsMatch(expectedEvents, events, currentCity);
            });

            EnqueueTestComplete();

        }

        [TestMethod]
        [WorkItem(770979)]
        [Description("DomainDataSourceView: CanChangePage should return false when PageSize = 0")]
        public void CanChangePageIsFalseWhenDDSPageSizeIsZero()
        {
            this._dds.AutoLoad = false;
            this._dds.QueryName = "GetCitiesQuery";
            this._dds.DomainContext = new CityDomainContext();

            Assert.IsFalse(this._view.CanChangePage, "CanChangePage should be false when PageSize is 0");

            this._dds.PageSize = 5;
            Assert.IsTrue(this._view.CanChangePage, "CanChangePage should be true when PageSize is not 0");

            this._dds.PageSize = 0;
            Assert.IsFalse(this._view.CanChangePage, "CanChangePage should go back to false after setting PageSize back to 0");
        }

        [TestMethod]
        [WorkItem(770979)]
        [Description("DomainDataSourceView: CanChangePage should return false when PageSize = 0")]
        public void CanChangePageIsFalseWhenViewPageSizeIsZero()
        {
            this._dds.AutoLoad = false;
            this._dds.QueryName = "GetCitiesQuery";
            this._dds.DomainContext = new CityDomainContext();

            Assert.IsFalse(this._view.CanChangePage, "CanChangePage should be false when PageSize is 0");

            this._view.PageSize = 5;
            Assert.IsTrue(this._view.CanChangePage, "CanChangePage should be true when PageSize is not 0");

            this._view.PageSize = 0;
            Assert.IsFalse(this._view.CanChangePage, "CanChangePage should go back to false after setting PageSize back to 0");
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797646)]
        [Description("Turning paging off clears the PageIndex from the View before the collection is reset")]
        public void PageIndexClearedWhenPagingGetsDisabled()
        {
            this.LoadDomainDataSourceControl();

            int allEntities = new CityData().Cities.Count;
            int iteration = 0;

            EventHandler<LoadedDataEventArgs> pagingEnabled = (s, e) =>
            {
                ++iteration;

                // When paging is enabled, the PageIndex should be 0 and the the View should have the number of entities matching the PageSize
                Assert.AreEqual<int>(0, this._view.PageIndex, "PageIndex with Paging enabled.  Iteration: " + iteration.ToString());
                Assert.AreEqual<int>(this._view.PageSize, this._view.Cast<City>().Count(), "View count with Paging enabled.  Iteration: " + iteration.ToString());
            };

            EventHandler<LoadedDataEventArgs> pagingDisabled = (s, e) =>
            {
                ++iteration;

                // When paging is disabled, the PageIndex should be -1 and all entities should be in the view
                Assert.AreEqual<int>(-1, this._view.PageIndex, "PageIndex with Paging disabled.  Iteration: " + iteration.ToString());
                Assert.AreEqual<int>(allEntities, this._view.Cast<City>().Count(), "View count with Paging disabled.  Iteration: " + iteration.ToString());
            };

            this._dds.LoadedData += pagingEnabled;
            this.LoadCities(5, true);

            EnqueueCallback(() =>
            {
                this._dds.LoadedData -= pagingEnabled;
                this._dds.LoadedData += pagingDisabled;

                this._view.PageSize = 0;
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                this._dds.LoadedData -= pagingDisabled;
                this._dds.LoadedData += pagingEnabled;

                this._view.PageSize = 1;
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual<int>(0, this._view.PageIndex);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(185040)]
        [Description("Tests that CanAddNew is false before the first page is loaded.")]
        public void CanAddNewIsFalseBeforeFirstPageIsLoaded()
        {
            this.EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();

                Assert.IsTrue(this._editableCollectionView.CanAddNew,
                    "CanAddNew should be true when paging is not enabled.");
                Assert.IsTrue(this._view.CanAdd,
                    "CanAdd should be true when paging is not enabled.");

                this._dds.PageSize = 3;

                Assert.IsFalse(this._editableCollectionView.CanAddNew,
                    "CanAddNew should be false when paging is enabled before the first page has been loaded.");
                Assert.IsFalse(this._view.CanAdd,
                    "CanAdd should be false when paging is enabled before the first page has been loaded.");

                this._dds.Load();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            this.EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.IsTrue(this._editableCollectionView.CanAddNew,
                    "CanAddNew should be true when paging is enabled after the first page has been loaded.");
                Assert.IsTrue(this._view.CanAdd,
                    "CanAdd should be true when paging is enabled after the first page has been loaded.");
            });

            this.EnqueueTestComplete();
        }
    }
}
