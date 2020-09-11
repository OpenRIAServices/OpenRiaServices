using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using OpenRiaServices.Client;
using OpenRiaServices.Client.Test;
using Cities;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices.LTS;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.Controls.DomainServices.Test
{
    /// <summary>
    /// Tests the load blocking feature of the <see cref="DomainDataSource"/>.
    /// </summary>
    [TestClass]
    public class LoadBlockingTests : DomainDataSourceTestBase
    {
        #region CanLoad Property

        [TestMethod]
        [Description("CanLoad defaults to true")]
        public void CanLoadDefaultsToTrue()
        {
            DomainDataSource dds = new DomainDataSource();
            Assert.IsTrue(dds.CanLoad);
        }

        [TestMethod]
        [Description("CanLoad throws an exception if set")]
        public void CanLoadIsReadOnly()
        {
            DomainDataSource dds = new DomainDataSource();
            ExceptionHelper.ExpectInvalidOperationException(() => dds.SetValue(DomainDataSource.CanLoadProperty, false), string.Format(DomainDataSourceResources.UnderlyingPropertyIsReadOnly, "CanLoad"));
        }

        [TestMethod]
        [Asynchronous]
        [Description("CanLoad is false when HasChanges is true")]
        public void CanLoadIsFalseWhenHasChangesIsTrue()
        {
            CityDomainContext context = this.LoadCities();

            EnqueueCallback(() =>
            {
                City city = context.Cities.First();
                context.Cities.Remove(city);

                Assert.IsTrue(this._dds.HasChanges);
                Assert.IsFalse(this._dds.CanLoad);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("CanLoad is false when IsSubmittingChanges is true")]
        public void CanLoadIsFalseWhenIsSubmittingChangesIsTrue()
        {
            CityDomainContext context = new CityDomainContext();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCities";
                this._dds.AutoLoad = false;
                this._dds.DomainContext = context;
                this._dds.Load();

                Assert.IsFalse(this._dds.IsSubmittingChanges, "IsSubmittingChanges immediately after Load()");
                Assert.IsTrue(this._dds.CanLoad, "CanLoad immediately after Load()");
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                Assert.IsFalse(this._dds.IsSubmittingChanges, "IsSubmittingChanges before submitting changes");
                Assert.IsTrue(this._dds.CanLoad, "CanLoad before submitting changes");

                // Pend up some changes
                this._view.RemoveAt(0);

                this._dds.SubmitChanges();
                Assert.IsTrue(this._dds.IsSubmittingChanges, "IsSubmittingChanges after submitting changes");
                Assert.IsFalse(this._dds.CanLoad, "CanLoad after submitting changes");
            });

            this.AssertSubmittingChanges();

            EnqueueTestComplete();
        }

        #endregion CanLoad Property

        #region Property Setters Blocked

        [TestMethod]
        [Asynchronous]
        [Description("Cannot change the DomainDataSource.PageSize property when CanLoad is false")]
        public void PageSizePropertySetterBlocked()
        {
            this.LoadCities();
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._dds.PageSize = 3, string.Format(DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_PropertySetter, "PageSize"));
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Cannot change the DomainDataSource.DataView.PageSize property when CanLoad is false")]
        public void PageSizeViewPropertySetterBlocked()
        {
            this.LoadCities();
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._view.PageSize = 3, string.Format(DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_PropertySetter, "PageSize"));
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Cannot change the LoadSize property when CanLoad is false")]
        public void LoadSizePropertySetterBlocked()
        {
            this.LoadCities();
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._dds.LoadSize = 3, string.Format(DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_PropertySetter, "LoadSize"));
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Cannot change the QueryName property when CanLoad is false")]
        public void QueryNamePropertySetterBlocked()
        {
            this.LoadCities();
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._dds.QueryName = "GetCitiesInState", string.Format(DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_PropertySetter, "QueryName"));
            });

            EnqueueTestComplete();
        }

        #endregion Property Setters Blocked

        #region Loading Blocked

        [TestMethod]
        [Asynchronous]
        [Description("Calling Load is blocked when CanLoad is false, throwing an exception")]
        public void LoadBlocked()
        {
            this.LoadCities();
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._dds.Load(), DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("A pending load is cancelled when CanLoad becomes false")]
        public void PendingLoadCancelledWhenCanLoadBecomesFalse()
        {
            this.LoadCities();

            EnqueueCallback(() =>
            {
                // We're going to invoke a load, but we expect the LoadedData event to say it was cancelled
                this._dds.LoadedData += (s, e) =>
                {
                    Assert.IsTrue(e.Cancelled);
                };

                // Begin a load
                Assert.IsTrue(this._dds.CanLoad, "CanLoad should be true before invoking the 2nd load");
                this._dds.Load();

                // Change data
                this._view.RemoveAt(0);
                Assert.IsFalse(this._dds.CanLoad, "CanLoad should be false after changing the data");
            });

            // Ensure the loading event happens, and then the loaded event will happen as well, and it
            // will be cancelled per the handler above
            this.AssertLoadingData();

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Progressive loading is disabled when CanLoad becomes false")]
        public void ProgressiveLoadingDisabledWhenCanLoadBecomesFalse()
        {
            this.LoadCities(() =>
            {
                this._dds.LoadSize = 3;
                this._dds.LoadInterval = defaultLoadInterval;
            });

            this.ModifyData();

            // Wait for the load interval plus the load delay to ensure the progressive loading didn't occur
            this.AssertNoLoadingData(defaultLoadInterval.Milliseconds + EnsureNoEventsTimeout);

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Progressive loading enabled when CanLoad becomes true")]
        public void ProgressiveLoadingEnabledWhenCanLoadBecomesTrue()
        {
            this.LoadCities(() =>
            {
                this._dds.LoadSize = 3;
                this._dds.LoadInterval = defaultLoadInterval;
            });

            this.ModifyData();

            EnqueueCallback(() =>
            {
                this._dds.RejectChanges();
                Assert.IsTrue(this._dds.CanLoad, "CanLoad should be true after rejecting changes");
            });

            // The progressive loading should now occur
            this.AssertLoadingData();

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("A pending auto load is aborted when CanLoad becomes false")]
        public void PendingAutoLoadAbortedWhenCanLoadBecomesFalse()
        {
            this.LoadDomainDataSourceControl();
            this.LoadCitiesInState("WA", () => this._dds.LoadDelay = defaultLoadInterval, true);

            EnqueueCallback(() =>
            {
                // Queue up the auto load
                this._dds.QueryParameters[0].Value = "CA";
            });

            this.ModifyData();

            // Wait for the load interval plus the load delay to ensure the auto loading didn't occur
            this.AssertNoLoadingData(defaultLoadInterval.Milliseconds + EnsureNoEventsTimeout);

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Automatic refreshes based on a RefreshInterval are paused when CanLoad is false")]
        public void AutomaticRefreshPaused()
        {
            // Short refresh intervals cause this test to intermittently fail, so going
            // with a 5-second interval.  The problem seems to be latency between the
            // EnqueueCallback delegates and time passing between the load being completed
            // and execution continuing through the test.  When the refresh begins before we
            // want it to, the test gets into a bad state and will either hang or fail.
            TimeSpan refreshInterval = TimeSpan.FromSeconds(5);

            this.LoadCities(() =>
            {
                this._dds.RefreshInterval = refreshInterval;
            });

            // Initial load
            this.AssertLoadingData();

            // Ensure that the refresh interval recurs
            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._asyncEventFailureMessage = "After modifying the data";
            });

            this.ModifyData();

            // The refresh should not occur
            this.AssertNoLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                this._dds.RejectChanges();
                Assert.IsTrue(this._dds.CanLoad, "CanLoad should be true after rejecting the changes");
            });

            // Refreshes should resume
            this.AssertLoadingData();

            // Ensure that the refresh interval recurs
            this.AssertLoadingData();

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(783188)]
        [Description("Refresh blocked when CanLoad is false, throwing an exception")]
        public void RefreshBlocked()
        {
            this.LoadCities();
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._collectionView.Refresh(), DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Refresh);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("DeferRefresh creation blocked when CanLoad is false, throwing an exception")]
        public void DeferRefreshBlocked()
        {
            this.LoadCities();
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._collectionView.DeferRefresh(), DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Refresh);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("DeferRefresh disposal blocked when CanLoad is false, throwing an exception")]
        public void DeferRefreshDisposalBlocked()
        {
            CityDomainContext context = this.LoadCities();

            IDisposable defer = null;
            EnqueueCallback(() =>
            {
                defer = this._collectionView.DeferRefresh();

                // Cannot use this.ModifyData here because changes to the data in the view are
                // blocked during DeferRefresh, so we have to use a back door to get
                // HasChanges to be true
                City toChange = context.Cities.First();
                toChange.StateName = "ST";
            });

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => defer.Dispose(), DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Refresh);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("DeferLoad creation blocked when CanLoad is false, throwing an exception")]
        public void DeferLoadBlocked()
        {
            this.LoadCities();
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._dds.DeferLoad(), DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_DeferLoad);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("DeferLoad disposal blocked when CanLoad is false, throwing an exception")]
        public void DeferLoadDisposalBlocked()
        {
            this.LoadCities();

            IDisposable defer = null;
            EnqueueCallback(() => defer = this._dds.DeferLoad());

            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => defer.Dispose(), DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_DeferLoad);
            });

            EnqueueTestComplete();
        }

        #endregion Loading Blocked

        #region Paging Blocked

        [TestMethod]
        [Asynchronous]
        [Description("MoveToPage is blocked when CanLoad is false, throwing an exception")]
        public void MoveToPageBlocked()
        {
            // Catalog supports paging better than cities as it returns the total item count
            this.LoadData<Catalog>("GetProducts", () => this._dds.PageSize = 3, false);
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._view.MoveToPage(1), DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Paging);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("MoveToFirstPage is blocked when CanLoad is false, throwing an exception")]
        public void MoveToFirstPageBlocked()
        {
            // Catalog supports paging better than cities as it returns the total item count
            this.LoadData<Catalog>("GetProducts", () => this._dds.PageSize = 3, false);

            EnqueueCallback(() =>
            {
                // Need to be somewhere other than the first page to move to previous page
                this._view.MoveToLastPage();
            });

            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();
            });

            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._view.MoveToFirstPage(), DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Paging);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("MoveToLastPage is blocked when CanLoad is false, throwing an exception")]
        public void MoveToLastPageBlocked()
        {
            // Catalog supports paging better than cities as it returns the total item count
            this.LoadData<Catalog>("GetProducts", () => this._dds.PageSize = 3, false);
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._view.MoveToLastPage(), DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Paging);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("MoveToNextPage is blocked when CanLoad is false, throwing an exception")]
        public void MoveToNextPageBlocked()
        {
            // Catalog supports paging better than cities as it returns the total item count
            this.LoadData<Catalog>("GetProducts", () => this._dds.PageSize = 3, false);
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._view.MoveToNextPage(), DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Paging);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("MoveToPreviousPage is blocked when CanLoad is false, throwing an exception")]
        public void MoveToPreviousPageBlocked()
        {
            // Catalog supports paging better than cities as it returns the total item count
            this.LoadData<Catalog>("GetProducts", () => this._dds.PageSize = 3, false);

            EnqueueCallback(() =>
            {
                // Need to be somewhere other than the first page to move to previous page
                this._view.MoveToLastPage();
            });

            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();
            });

            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._view.MoveToPreviousPage(), DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Paging);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("The CanChangePage property of the DataView changes to respect the CanLoad property")]
        public void CanChangePageRespectsCanLoad()
        {
            // Catalog supports paging better than cities as it returns the total item count
            this.LoadData<Catalog>("GetProducts", () => this._dds.PageSize = 3, false);

            EnqueueCallback(() =>
            {
                Assert.IsTrue(this._view.CanChangePage, "CanChangePage should be true before modifying the data");
            });

            this.ModifyData();

            EnqueueCallback(() =>
            {
                Assert.IsFalse(this._view.CanChangePage, "CanChangePage should be false after modifying the data");

                this._dds.RejectChanges();
                Assert.IsTrue(this._view.CanChangePage, "CanChangePage should be true after rejecting changes");
            });

            EnqueueTestComplete();
        }

        #endregion Paging Blocked

        #region Sorting Blocked

        [TestMethod]
        [Asynchronous]
        [Description("Adding a new sort descriptor (through the DDS) is blocked when CanLoad is false, throwing an exception")]
        public void SortDescriptorAddBlocked()
        {
            this.LoadCities();
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending)), DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Sorting);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Adding a new sort description (through the view) is blocked when CanLoad is false, throwing an exception")]
        public void SortDescriptionAddBlocked()
        {
            this.LoadCities();
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._collectionView.SortDescriptions.Add(new System.ComponentModel.SortDescription("Name", System.ComponentModel.ListSortDirection.Ascending)), DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Sorting);
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Changing an existing sort descriptor is blocked.
        /// </summary>
        /// <remarks>
        /// Note that we cannot test changing a sort description through the view as the DDS doesn't get notifications
        /// for those changes, but UI controls don't update existing sort descriptions, they replace them.
        /// </remarks>
        [TestMethod]
        [Asynchronous]
        [Description("Modifying a sort descriptor (through the DDS) is blocked when CanLoad is false, throwing an exception")]
        public void SortDescriptorChangeBlocked()
        {
            SortDescriptor sort = new SortDescriptor("Name", ListSortDirection.Ascending);

            this.LoadCities(() => this._dds.SortDescriptors.Add(sort));
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => sort.Direction = ListSortDirection.Descending, DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Sorting);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Removing a sort descriptor (through the DDS) is blocked when CanLoad is false, throwing an exception")]
        public void SortDescriptorRemoveBlocked()
        {
            SortDescriptor sort = new SortDescriptor("Name", ListSortDirection.Ascending);

            this.LoadCities(() => this._dds.SortDescriptors.Add(sort));
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._dds.SortDescriptors.Remove(sort), DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Sorting);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Removing a sort description (through the view) is blocked when CanLoad is false, throwing an exception")]
        public void SortDescriptionRemoveBlocked()
        {
            System.ComponentModel.SortDescription sort = new System.ComponentModel.SortDescription("Name", System.ComponentModel.ListSortDirection.Ascending);

            this.LoadCities(() => this._collectionView.SortDescriptions.Add(sort));
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._collectionView.SortDescriptions.Remove(sort), DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Sorting);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("The CanSort property respects the CanLoad property value")]
        public void CanSortRespectsCanLoad()
        {
            this.LoadCities();
            this.ModifyData();

            EnqueueCallback(() =>
            {
                Assert.IsFalse(this._collectionView.CanSort, "CanSort should be false when CanLoad is false");
                this._dds.RejectChanges();
                Assert.IsTrue(this._collectionView.CanSort, "CanSort should be true after rejecting changes");
            });

            EnqueueTestComplete();
        }

        #endregion Sorting Blocked

        #region Filtering Blocked

        [TestMethod]
        [Asynchronous]
        [Description("Adding a new FilterDescriptor is blocked when CanLoad is false, throwing an exception")]
        public void FilterDescriptorAddBlocked()
        {
            this.LoadCities();
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._dds.FilterDescriptors.Add(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA")), DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Filtering);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Modifying an existing FilterDescriptor is blocked when CanLoad is false, throwing an exception")]
        public void FilterDescriptorChangeBlocked()
        {
            this.LoadCities(() =>
            {
                this._dds.FilterDescriptors.Add(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA"));
            });

            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._dds.FilterDescriptors[0].Value = "CA", DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Filtering);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("A ControlParameter based FilterDescriptor update is blocked when CanLoad is false, throwing an exception and preventing the auto load from occurring")]
        public void FilterDescriptorChangeThroughControlParameterBlocked()
        {
            this.LoadDomainDataSourceControl();
            this.LoadTextBoxControl();

            this.LoadCities(() =>
            {
                this._textBox.Text = "WA";
                this._dds.FilterDescriptors.Add(this.CreateValueBoundFilterDescriptor("StateName", FilterOperator.IsEqualTo, this._textBox, "Text"));
            }, true);

            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._textBox.Text = "CA", DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Filtering);
            });

            this.AssertNoLoadingData();

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Removing an existing FilterDescriptor is blocked when CanLoad is false, throwing an exception")]
        public void FilterDescriptorRemoveBlocked()
        {
            this.LoadCities(() =>
            {
                this._dds.FilterDescriptors.Add(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA"));
            });

            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._dds.FilterDescriptors.RemoveAt(0), DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Filtering);
            });

            EnqueueTestComplete();
        }

        #endregion Filtering Blocked

        #region Grouping Blocked

        [TestMethod]
        [Asynchronous]
        [Description("Adding a new group descriptor (through the DDS) is blocked when CanLoad is false, throwing an exception")]
        public void GroupDescriptorAddBlocked()
        {
            this.LoadCities();
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._dds.GroupDescriptors.Add(new GroupDescriptor("StateName")), DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Grouping);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Adding a new group description (through the view) is blocked when CanLoad is false, throwing an exception")]
        public void GroupDescriptionAddBlocked()
        {
            this.LoadCities();
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._collectionView.GroupDescriptions.Add(new PropertyGroupDescription("StateName")), DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Grouping);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Modifying an existing group descriptor (through the DDS) is blocked when CanLoad is false, throwing an exception")]
        public void GroupDescriptorChangeBlocked()
        {
            GroupDescriptor group = new GroupDescriptor("StateName");
            this.LoadCities(() => this._dds.GroupDescriptors.Add(group));
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => group.PropertyPath = "Name", DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Grouping);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Modifying an existing group description (through the view) is blocked when CanLoad is false, throwing an exception")]
        public void GroupDescriptionChangeBlocked()
        {
            PropertyGroupDescription group = new PropertyGroupDescription("StateName");
            this.LoadCities(() => this._collectionView.GroupDescriptions.Add(group));
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => group.PropertyName = "Name", DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Grouping);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Removing an existing group descriptor (through the DDS) is blocked when CanLoad is false, throwing an exception")]
        public void GroupDescriptorRemoveBlocked()
        {
            this.LoadCities(() => this._dds.GroupDescriptors.Add(new GroupDescriptor("StateName")));
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._dds.GroupDescriptors.RemoveAt(0), DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Grouping);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Removing an existing group description (through the view) is blocked when CanLoad is false, throwing an exception")]
        public void GroupDescriptionRemoveBlocked()
        {
            this.LoadCities(() => this._collectionView.GroupDescriptions.Add(new PropertyGroupDescription("StateName")));
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._collectionView.GroupDescriptions.RemoveAt(0), DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Grouping);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("The CanGroup property always returns true, to ensure existing groups are applied")]
        public void CanGroupReturnsTrueWhenCanLoadIsFalse()
        {
            this.LoadCities();
            this.ModifyData();

            EnqueueCallback(() =>
            {
                Assert.IsTrue(this._collectionView.CanGroup, "CanGroup should be true after modifying the data");
            });

            EnqueueTestComplete();
        }

        #endregion Grouping Blocked

        #region QueryParameters Blocked

        [TestMethod]
        [Asynchronous]
        [Description("Adding a QueryParameter is blocked when CanLoad is false, throwing an exception")]
        public void QueryParameterAddBlocked()
        {
            CityDomainContext context = this.LoadCitiesInState("WA");

            EnqueueCallback(() =>
            {
                this._dds.QueryParameters.RemoveAt(0);

                // Cannot use this.ModifyData() because this._view is empty after removing the query parameter
                // So we will modify the data in the context directly
                City city = context.Cities.First();
                context.Cities.Remove(city);

                ExceptionHelper.ExpectInvalidOperationException(() => this._dds.QueryParameters.Add(new Parameter { ParameterName = "state", Value = "CA" }), DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_QueryParameters);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Changing a QueryParameter is blocked when CanLoad is false, throwing an exception")]
        public void QueryParameterChangeBlocked()
        {
            this.LoadCitiesInState("WA");
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._dds.QueryParameters[0].Value = "CA", DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_QueryParameters);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Changing a QueryParameter through a ControlParameter is blocked when CanLoad is false, throwing an exception")]
        public void QueryParameterChangeThroughControlParameterBlocked()
        {
            this.LoadDomainDataSourceControl();
            this.LoadTextBoxControl();

            EnqueueCallback(() =>
            {
                this._textBox.Text = "WA";
            });

            this.LoadCitiesInState(this.CreateValueBoundParameter("state", this._textBox, "Text"), null, true);
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._textBox.Text = "CA", DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_QueryParameters);
            });

            this.AssertNoLoadingData();

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Replacing a QueryParameter is blocked when CanLoad is false, throwing an exception")]
        public void QueryParameterReplaceBlocked()
        {
            this.LoadCitiesInState("WA");
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._dds.QueryParameters[0] = new Parameter { ParameterName = "state", Value = "CA" }, DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_QueryParameters);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Removing a QueryParameter is blocked when CanLoad is false, throwing an exception")]
        public void QueryParameterRemoveBlocked()
        {
            this.LoadCitiesInState("WA");
            this.ModifyData();

            EnqueueCallback(() =>
            {
                ExceptionHelper.ExpectInvalidOperationException(() => this._dds.QueryParameters.RemoveAt(0), DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_QueryParameters);
            });

            EnqueueTestComplete();
        }

        #endregion QueryParameters Blocked

        #region Helper Methods

        private T LoadData<T>(string queryName, Action initialization, bool autoLoad) where T : DomainContext, new()
        {
            T context = new T();

            EnqueueCallback(() =>
            {
                if (initialization != null)
                {
                    initialization();
                }

                this._dds.QueryName = queryName;
                this._dds.DomainContext = context;
                this._dds.AutoLoad = autoLoad;

                if (!autoLoad)
                {
                    this._dds.Load();
                }
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();
            });

            return context;
        }

        private CityDomainContext LoadCities()
        {
            return this.LoadCities(null, false);
        }

        private CityDomainContext LoadCities(Action initialization)
        {
            return this.LoadCities(initialization, false);
        }

        private CityDomainContext LoadCities(Action initialization, bool autoLoad)
        {
            return LoadData<CityDomainContext>("GetCities", initialization, autoLoad);
        }

        private CityDomainContext LoadCitiesInState(string state)
        {
            return this.LoadCitiesInState(state, null);
        }

        private CityDomainContext LoadCitiesInState(string state, Action initialization)
        {
            return this.LoadCitiesInState(state, initialization, false);
        }

        private CityDomainContext LoadCitiesInState(string state, Action initialization, bool autoload)
        {
            return this.LoadCitiesInState(new Parameter { ParameterName = "state", Value = state }, initialization, autoload);
        }

        private CityDomainContext LoadCitiesInState(Parameter stateParameter, Action initialization, bool autoload)
        {
            return LoadData<CityDomainContext>("GetCitiesInState", () =>
            {
                if (initialization != null)
                {
                    initialization();
                }

                this._dds.QueryParameters.Add(stateParameter);
            }, autoload);
        }

        private void ModifyData()
        {
            EnqueueCallback(() =>
            {
                this._view.RemoveAt(0);
                Assert.IsFalse(this._dds.CanLoad, "CanLoad should be false after modifying data");
            });
        }

        #endregion Helper Methods
    }
}
