using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.ServiceModel.DomainServices.Client;
using System.Windows.Common;
using System.Windows.Data;
using Cities;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices;
using TestDomainServices.LTS;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace System.Windows.Controls.DomainServices.Test
{
    /// <summary>
    /// Tests <see cref="DomainDataSource"/> members.
    /// </summary>
    [TestClass]
    public class DomainDataSourceTests : DomainDataSourceTestBase
    {
        #region Read Only Properties

        [TestMethod]
        [Description("Tests setting read only dependency properties.")]
        public void SettingReadOnlyDependencyProperties()
        {
            AssertExpectedException(
                new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DomainDataSourceResources.UnderlyingPropertyIsReadOnly,
                        "HasChanges")),
                () =>
                {
                    this._dds.SetValue(DomainDataSource.HasChangesProperty, true);
                });

            AssertExpectedException(
                new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DomainDataSourceResources.UnderlyingPropertyIsReadOnly,
                        "IsBusy")),
                () =>
                {
                    this._dds.SetValue(DomainDataSource.IsBusyProperty, true);
                });

            AssertExpectedException(
                new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DomainDataSourceResources.UnderlyingPropertyIsReadOnly,
                        "IsLoadingData")),
                () =>
                {
                    this._dds.SetValue(DomainDataSource.IsLoadingDataProperty, true);
                });

            AssertExpectedException(
                new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DomainDataSourceResources.UnderlyingPropertyIsReadOnly,
                        "IsSubmittingChanges")),
                () =>
                {
                    this._dds.SetValue(DomainDataSource.IsSubmittingChangesProperty, true);
                });

            List<int> intList = new List<int>();

            AssertExpectedException(
                new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DomainDataSourceResources.UnderlyingPropertyIsReadOnly,
                        "Data")),
                () =>
                {
                    this._dds.SetValue(DomainDataSource.DataProperty, intList);
                });

            AssertExpectedException(
                new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DomainDataSourceResources.UnderlyingPropertyIsReadOnly,
                        "DataView")),
                () =>
                {
                    this._dds.SetValue(DomainDataSource.DataViewProperty, null);
                });
        }

        #endregion

        #region Writable Properties

        [TestMethod]
        [Description("Validate the DataView.NewItemPlaceholderPosition property.")]
        public void CannotChangeNewItemPlaceholderPositionTests()
        {
            Assert.AreEqual(System.ComponentModel.NewItemPlaceholderPosition.None, this._editableCollectionView.NewItemPlaceholderPosition);

            AssertExpectedException(
                new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture,
                            "The value of argument '{0}' ({1}) is invalid for Enum type '{2}'.",
                            "value",
                            "123",
                            typeof(System.ComponentModel.NewItemPlaceholderPosition).Name)),
                delegate
                {
                    this._editableCollectionView.NewItemPlaceholderPosition = (System.ComponentModel.NewItemPlaceholderPosition)123;
                });
        }

        [TestMethod]
        [WorkItem(852802)]
        [WorkItem(879828)]
        [Description("DomainDataSource.DesignData should not be browsable in the Properties window or IntelliSense or available for binding")]
        public void DesignDataNotBrowsable()
        {
            System.Reflection.PropertyInfo designData = typeof(DomainDataSource).GetProperty("DesignData");
            Assert.IsNotNull(designData, "The DesignData property was null!?");

            BrowsableAttribute browsable = (BrowsableAttribute)designData.GetCustomAttributes(typeof(BrowsableAttribute), false).SingleOrDefault();
            EditorBrowsableAttribute editorBrowsable = (EditorBrowsableAttribute)designData.GetCustomAttributes(typeof(EditorBrowsableAttribute), false).SingleOrDefault();
            BindableAttribute bindable = (BindableAttribute)designData.GetCustomAttributes(typeof(BindableAttribute), false).SingleOrDefault();

            Assert.IsNotNull(browsable, "There was no [Browsable] attribute");
            Assert.IsNotNull(editorBrowsable, "There was no [EditorBrowsable] attribute");
            Assert.IsNotNull(bindable, "There was no [Bindable] attribute");

            Assert.IsFalse(browsable.Browsable, "Browsable");
            Assert.AreEqual<EditorBrowsableState>(EditorBrowsableState.Never, editorBrowsable.State, "EditorBrowsableState");
            Assert.IsFalse(bindable.Bindable, "Bindable");
        }

        #endregion

        #region Loading

        [TestMethod]
        [Description("Tests invalid DomainContext and LoadMethodName properties.")]
        public void SettingInvalidDomainContextAndLoadMethodNameProperties()
        {
            this._dds.QueryName = String.Empty;
            Assert.IsNull(this._dds.DomainContext);

            ////checking MethodAccessStatus.NameNotFound (MemberNotFound) exception:
            this._dds.QueryName = "NonExistentMethod";
            Assert.AreEqual("NonExistentMethod", this._dds.QueryName);
            AssertExpectedException(
                new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DomainDataSourceResources.MemberNotFound,
                        "CityDomainContext",
                        DomainDataSourceResources.Method,
                        "NonExistentMethod")),
                () =>
                {
                    this._dds.DomainContext = new CityDomainContext();
                });
            Assert.IsNull(this._dds.DomainContext);

            // Changing to valid strings should not raise any exceptions.
            this._dds.QueryName = "GetCitiesQuery";
            this._dds.DomainContext = new CityDomainContext();

            // Changing the properties to empty strings should not raise any exceptions.
            this._dds.QueryName = String.Empty;

            // Changing the DomainContext to another value should raise an InvalidOperationException.
            AssertExpectedException(
                new InvalidOperationException(DomainDataSourceResources.DomainContextAlreadySet),
                () =>
                {
                    this._dds.DomainContext = new CityDomainContext();
                });

            // Even when the new value is null
            AssertExpectedException(
                new InvalidOperationException(DomainDataSourceResources.DomainContextAlreadySet),
                () =>
                {
                    this._dds.DomainContext = null;
                });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that data is loaded correctly from the mock database.")]
        public void LoadData()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
                Assert.IsTrue(this._dds.IsBusy);
                Assert.IsTrue(this._dds.IsLoadingData);
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.IsFalse(this._dds.IsBusy);
                Assert.IsFalse(this._dds.IsLoadingData);
                Assert.AreEqual(11, this._dds.Data.Cast<Entity>().Count());
                Assert.AreEqual(11, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that currency is initialized for newly loaded data.")]
        public void InitializeCurrency()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
                Assert.AreEqual(-1, this._view.CurrentPosition,
                    "Currency should be -1 in CollectionViews by default.");
            });

            this.AssertLoadingData();

            EventHandler<LoadedDataEventArgs> initializeCurrentLoadedData = (sender, e) => this._view.MoveCurrentToPosition(1);

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                Assert.AreEqual(0, this._view.CurrentPosition,
                    "Currency should be initialized to 0 for newly loaded data.");
                this._view.MoveCurrentToPosition(-1);
                this._dds.Load();
                this._dds.LoadedData += initializeCurrentLoadedData;
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.LoadedData -= initializeCurrentLoadedData;
                Assert.AreEqual(1, this._view.CurrentPosition,
                    "Currency should not have been reset.");
                this._view.MoveCurrentToPosition(-1);
                this._dds.LoadSize = 6;
                this._dds.Load();
            });

            this.AssertLoadingData(2);

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                Assert.AreEqual(0, this._view.CurrentPosition,
                    "Currency should not have been modified by subsequent loads.");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that a load operation can be cancelled.")]
        public void CancelLoad()
        {
            EnqueueCallback(() =>
            {
                this._ddsLoadingDataExpected = 2;
                this._dds.AutoLoad = false;
                this._dds.QueryParameters.Add(new Parameter { ParameterName = "state", Value = "WA" });
                this._dds.QueryName = "GetCitiesInStateQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
                Assert.IsTrue(this._dds.IsBusy);
                Assert.IsTrue(this._dds.IsLoadingData);
                this._dds.CancelLoad();
                Assert.IsFalse(this._dds.IsBusy);
                Assert.IsFalse(this._dds.IsLoadingData);
                this._dds.QueryParameters[0].Value = "OR";
                this._dds.Load();
                Assert.IsTrue(this._dds.IsBusy);
                Assert.IsTrue(this._dds.IsLoadingData);
            });

            this.AssertLoadedData(2);
            // TODO:
            // If we had a better mock, the following tests would be better. As it is,
            // I'm not convinced they don't have the potential for failure.
            ////this.AssertLoadedData();
            ////EnqueueCallback(() =>
            ////{
            ////    this.ResetLoadState();
            ////    Assert.AreEqual(0, this._view.Count);
            ////});
            ////this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                Assert.IsFalse(this._dds.IsBusy);
                Assert.IsFalse(this._dds.IsLoadingData);
                Assert.AreEqual(1, this._view.Count);
                Assert.AreEqual("OR", ((City)this._view.GetItemAt(0)).StateName);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Setting the LoadingDataEventArgs.LoadBehavior to a valid value.")]
        public void LoadDataWithValidLoadBehavior()
        {
            ProgressiveLoadTimer timer = null;

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.LoadInterval = defaultLoadInterval;
                this._dds.LoadSize = (new CityData().Cities.Count / 2) + 1; // --> 2 progressive loads
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.LoadingData += (sender, e) => e.LoadBehavior = LoadBehavior.RefreshCurrent;

                timer = this.UseProgressiveLoadTimer();
                timer.ExpectedStartCount = 1;
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                timer.AssertAndResetCounts("after loading data.");
                timer.AssertLoadingDataOnTick();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                timer.AssertAndResetCounts("after loading data a second time.");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that a server-side Linq query can be defined in a LoadingData event handler.")]
        public void LinqQueryWhenLoadingData()
        {
            EnqueueCallback(() =>
            {
                CityDomainContext context = new CityDomainContext();
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.LoadingData += (object sender, System.Windows.Controls.LoadingDataEventArgs e) => e.Query = context.GetCitiesQuery().Where(c => c.StateName == "WA");
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(6, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that a load can be cancelled in a LoadingData event handler.")]
        public void CancelLoadingData()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.LoadingData += (object sender, System.Windows.Controls.LoadingDataEventArgs e) => e.Cancel = true;
                this._dds.Load();
            });

            this.AssertLoadingData(false /*expectLoadedData*/);

            EnqueueCallback(() =>
            {
                Assert.AreEqual(0, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that a load can be cancelled and parameters reset in a LoadingData event handler.")]
        public void CancelLoadingDataResetParameters()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesInStateQuery";
                this._dds.QueryParameters.Add(new Parameter { ParameterName = "state", Value = "WA" });
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "CountyName", Operator = FilterOperator.IsEqualTo, Value = "King" });
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                Assert.AreEqual(5, this._view.Count);

                this._dds.QueryParameters[0].Value = "CA";
                this._dds.SortDescriptors[0].PropertyPath = "CountyName";
                this._dds.SortDescriptors[0].Direction = ListSortDirection.Descending;
                this._dds.FilterDescriptors[0].PropertyPath = "Name";
                this._dds.FilterDescriptors[0].Value = "Los Angeles";
                this._dds.LoadingData += (object sender, System.Windows.Controls.LoadingDataEventArgs e) =>
                {
                    e.RestoreLoadSettings = true;
                    e.Cancel = true;
                };
                this._dds.Load();
            });

            this.AssertLoadingData(false);

            EnqueueCallback(() =>
            {
                Assert.AreEqual("WA", this._dds.QueryParameters[0].Value);
                Assert.AreEqual("Name", this._dds.SortDescriptors[0].PropertyPath);
                Assert.AreEqual(ListSortDirection.Ascending, this._dds.SortDescriptors[0].Direction);
                Assert.AreEqual("CountyName", this._dds.FilterDescriptors[0].PropertyPath);
                Assert.AreEqual("King", this._dds.FilterDescriptors[0].Value);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that a LoadedData event with the error is raised when an exception is thrown from within the LoadingData event handler")]
        public void LoadErrorRaisedWhenExceptionThrownInLoadingData()
        {
            this.LoadDomainDataSourceControl();
            NotSupportedException thrownAndExpected = new NotSupportedException("This should cause a LoadedData event with the error to occur");

            this._dds.LoadingData += (s, e) =>
            {
                throw thrownAndExpected;
            };

            this.AssertExpectedLoadError(typeof(NotSupportedException), thrownAndExpected.Message, () =>
            {
                this._dds.AutoLoad = true;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verifies that calling Load while another load is in progress will cancel the existing load and continue")]
        public void DoubleLoadCancelsLoadAlreadyInProgress()
        {
            bool isFirst = true;

            EventHandler<LoadedDataEventArgs> firstLoadedDataIsCancelled = (s, e) =>
            {
                Assert.AreEqual<bool>(isFirst, e.Cancelled, "Cancelled should be true for the first load");
                isFirst = false;
            };

            EnqueueCallback(() =>
            {
                this._ddsLoadingDataExpected = 2;
                this._ddsLoadedDataExpected = 2;

                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCities";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.LoadedData += firstLoadedDataIsCancelled;
                this._dds.Load();

                this._dds.FilterDescriptors.Add(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA"));
                this._dds.Load();
            });

            this.AssertLoadedData(2);

            EnqueueCallback(() =>
            {
                int expectedCount = new CityData().Cities.Count(c => c.StateName == "WA");
                Assert.AreEqual(expectedCount, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the DeferLoad method functions properly.")]
        public void DeferLoad()
        {
            IDisposable deferLoad = null;

            this.LoadDomainDataSourceControl();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesInStateQuery";
                this._dds.QueryParameters.Add(new Parameter { ParameterName = "state", Value = "WA" });
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "CountyName", Operator = FilterOperator.IsEqualTo, Value = "King" });
                this._dds.DomainContext = new CityDomainContext();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                // call DeferLoad() and then change Filter/Sort params
                deferLoad = this._dds.DeferLoad();
                this._dds.FilterDescriptors[0].Value = "King";
                this._dds.SortDescriptors.Add(new SortDescriptor("CountyName", ListSortDirection.Ascending));
            });

            // verify that because we called DeferLoad, we won't load data
            this.AssertNoLoadingData();

            EnqueueCallback(() =>
            {
                deferLoad.Dispose();
            });

            this.AssertLoadingData();

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(785916)]
        [Description("Calling Load inside a DeferLoad should throw an exception")]
        public void DeferLoadWithLoadInside()
        {
            EnqueueCallback(() =>
            {
                using (IDisposable defer = this._dds.DeferLoad())
                {
                    this._dds.QueryName = "GetCitiesQuery";
                    this._dds.DomainContext = new CityDomainContext();

                    AssertExpectedException(new InvalidOperationException(DomainDataSourceResources.LoadWithinDeferLoad), () => this._dds.Load());
                }
            });

            this.AssertLoadingData();

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that DeferRefresh on the DataView will unconditionally trigger a load when disposed")]
        public void DeferRefresh()
        {
            IDisposable deferRefresh = null;

            this.LoadDomainDataSourceControl();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesInStateQuery";
                this._dds.QueryParameters.Add(new Parameter { ParameterName = "state", Value = "WA" });
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
                this._dds.DomainContext = new CityDomainContext();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(6, this._view.Count, "Count should be 6 after the refresh");
                Assert.AreEqual("Bellevue", ((City)this._view.CurrentItem).Name, "Bellevue should have currency after the initial load");
                Assert.AreEqual(0, this._view.CurrentPosition, "CurrentPosition should be 0 after the initial load");
            });

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                // Set the load delay to 50 milliseconds, and assume that if no LoadingData event has
                // occurred after a second, that no event will occur.
                this._dds.LoadDelay = TimeSpan.FromMilliseconds(50);

                deferRefresh = this._collectionView.DeferRefresh();
                this._dds.SortDescriptors.Clear();
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Descending));
            });

            AssertNoLoadingData(EnsureNoEventsTimeout, "LoadingData should not have been raised until the DeferRefresh was disposed.");

            // Now we'll expect the event
            EnqueueCallback(() =>
            {
                deferRefresh.Dispose();
            });

            AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(6, this._view.Count, "Count should be 6 after the refresh");
                Assert.AreEqual("Bellevue", ((City)this._view.CurrentItem).Name, "Bellevue should have retained currency after the refresh");
                Assert.AreEqual(5, this._view.CurrentPosition, "CurrentPosition should be 5 after the refresh");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(785916)]
        [Description("Calling Refresh inside a DeferRefresh should throw an exception")]
        public void DeferRefreshWithRefreshInside()
        {
            this.LoadDomainDataSourceControl();

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = true;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                using (IDisposable defer = this._collectionView.DeferRefresh())
                {

                    AssertExpectedException(new InvalidOperationException(EntityCollectionViewResources.RefreshWithinDeferRefresh), () => this._collectionView.Refresh());
                }
            });

            this.AssertLoadedData();

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that DeferRefresh being disposed doesn't trigger a load if inside a DeferLoad")]
        public void DeferRefreshInsideDeferLoad()
        {
            IDisposable deferLoad = null;

            EnqueueCallback(() =>
            {
                this._dds.PageSize = 2; // Will force currency to change
                this._dds.QueryName = "GetCitiesInStateQuery";
                this._dds.QueryParameters.Add(new Parameter { ParameterName = "state", Value = "WA" });
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadedData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
                Assert.AreEqual("Bellevue", ((City)this._view.CurrentItem).Name);
            });

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                // Set the load delay to 50 milliseconds, and assume that if no LoadingData event has
                // occurred after a second, that no event will occur.
                this._dds.LoadDelay = TimeSpan.FromMilliseconds(50);

                deferLoad = this._dds.DeferLoad();

                using (this._collectionView.DeferRefresh())
                {
                    this._dds.SortDescriptors.Clear();
                    this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Descending));
                }
            });

            AssertNoLoadingData(EnsureNoEventsTimeout, "LoadingData should not have been raised until the DeferLoad was disposed.");

            // Now we'll expect the event
            EnqueueCallback(() =>
            {
                deferLoad.Dispose();
            });

            AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
                Assert.AreEqual("Tacoma", ((City)this._view.CurrentItem).Name);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that DeferLoad being disposed doesn't trigger a load if inside a DeferRefresh")]
        public void DeferLoadInsideDeferRefresh()
        {
            IDisposable deferRefresh = null;

            EnqueueCallback(() =>
            {
                this._dds.PageSize = 2; // Will force Bellevue off the page when we re-sort
                this._dds.QueryName = "GetCitiesInStateQuery";
                this._dds.QueryParameters.Add(new Parameter { ParameterName = "state", Value = "WA" });
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadedData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(2, this._view.Count);
                Assert.AreEqual("Bellevue", ((City)this._view.CurrentItem).Name);

                // Set the load delay to 50 milliseconds, and assume that if no LoadingData event has
                // occurred after a second, that no event will occur.
                this._dds.LoadDelay = TimeSpan.FromMilliseconds(50);

                deferRefresh = this._collectionView.DeferRefresh();

                using (this._dds.DeferLoad())
                {
                    this._dds.SortDescriptors.Clear();
                    this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Descending));
                }
            });

            AssertNoLoadingData(EnsureNoEventsTimeout, "LoadingData should not have been raised until the DeferRefresh was disposed.");

            // Now we'll expect the event
            EnqueueCallback(() =>
            {
                deferRefresh.Dispose();
            });

            AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(2, this._view.Count);
                Assert.AreEqual("Tacoma", ((City)this._view.CurrentItem).Name);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Changing PageSize inside a DeferRefresh doesn't trigger a load until the DeferRefresh is disposed.")]
        public void DeferRefreshWithPageSizeChange()
        {
            IDisposable deferRefresh = null;

            this.LoadDomainDataSourceControl();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesInStateQuery";
                this._dds.QueryParameters.Add(new Parameter { ParameterName = "state", Value = "WA" });
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
                this._dds.PageSize = 3;
                this._dds.DomainContext = new CityDomainContext();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(3, this._view.Count);
                Assert.AreEqual("Bellevue", ((City)this._view.CurrentItem).Name);
            });

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                // Set the load delay to 50 milliseconds, and assume that if no LoadingData event has
                // occurred after a second, that no event will occur.
                this._dds.LoadDelay = TimeSpan.FromMilliseconds(50);

                deferRefresh = this._collectionView.DeferRefresh();
                this._dds.PageSize = 5;
            });

            this.AssertNoLoadingData(EnsureNoEventsTimeout, "LoadingData should not have been raised until the DeferRefresh was disposed.");

            // Now we'll expect the event
            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();
                deferRefresh.Dispose();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(5, this._view.Count);
                Assert.AreEqual("Bellevue", ((City)this._view.CurrentItem).Name);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Calling MoveToPage inside a DeferRefresh doesn't trigger a page change until the DeferRefresh is disposed.")]
        public void DeferRefreshWithMoveToPage()
        {
            IDisposable deferRefresh = null;

            this.LoadDomainDataSourceControl();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesInStateQuery";
                this._dds.QueryParameters.Add(new Parameter { ParameterName = "state", Value = "WA" });
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
                this._dds.PageSize = 3;
                this._dds.DomainContext = new CityDomainContext();
            });

            this.AssertLoadedData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(3, this._view.Count);
                Assert.AreEqual("Bellevue", ((City)this._view.CurrentItem).Name);

                // Set the load delay to 50 milliseconds, and assume that if no LoadingData event has
                // occurred after a second, that no event will occur.
                this._dds.LoadDelay = TimeSpan.FromMilliseconds(50);

                deferRefresh = this._collectionView.DeferRefresh();
                this._dds.MoveToPage(1);
            });

            this.AssertNoLoadingData(EnsureNoEventsTimeout, "LoadingData should not have been raised until the DeferRefresh was disposed.");

            // Now we'll expect the event
            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                deferRefresh.Dispose();
            });

            this.AssertLoadedData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(3, this._view.Count);
                Assert.AreEqual("Everett", ((City)this._view.CurrentItem).Name);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Changing PageSize and calling MoveToPage multiple times inside a DeferRefresh doesn't trigger a page change until the DeferRefresh is disposed.")]
        public void DeferRefreshWithPageSizeAndMultipleMoveToPages()
        {
            IDisposable deferRefresh = null;

            this.LoadDomainDataSourceControl();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesInStateQuery";
                this._dds.QueryParameters.Add(new Parameter { ParameterName = "state", Value = "WA" });
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
                this._dds.PageSize = 3;
                this._dds.DomainContext = new CityDomainContext();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                Assert.AreEqual(3, this._view.Count);
                Assert.AreEqual("Bellevue", ((City)this._view.CurrentItem).Name);

                // Set the load delay to 50 milliseconds, and assume that if no LoadingData event has
                // occurred after a second, that no event will occur.
                this._dds.LoadDelay = TimeSpan.FromMilliseconds(50);

                deferRefresh = this._collectionView.DeferRefresh();
                this._dds.MoveToPage(1);
                this._dds.PageSize = 1;
                this._dds.MoveToPage(2);
            });

            this.AssertNoLoadingData(EnsureNoEventsTimeout, "LoadingData should not have been raised until the DeferRefresh was disposed.");

            // Now we'll expect the event
            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                this._asyncEventFailureMessage = "Data should be loaded and the page should change once after the DeferRefresh was disposed.";
                deferRefresh.Dispose();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(1, this._view.Count);
                Assert.AreEqual("Duvall", ((City)this._view.CurrentItem).Name);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Changing PageSize and calling MoveToPage multiple times (through the DataView) inside a DeferRefresh doesn't trigger a page change until the DeferRefresh is disposed.")]
        public void DeferRefreshWithPageSizeAndMultipleMoveToPagesThroughECV()
        {
            IDisposable deferRefresh = null;

            this.LoadDomainDataSourceControl();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesInStateQuery";
                this._dds.QueryParameters.Add(new Parameter { ParameterName = "state", Value = "WA" });
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
                this._dds.PageSize = 1; // Ensure there are enough pages to properly run the test
                this._dds.DomainContext = new CityDomainContext();

                this._asyncEventFailureMessage = "Initial AutoLoad";
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                this._asyncEventFailureMessage = "MoveToPage(1); PageSize = 1; MoveToPage(2);";

                Assert.AreEqual(1, this._view.Count);
                Assert.AreEqual("Bellevue", ((City)this._view.CurrentItem).Name);

                // Set the load delay to 50 milliseconds, and assume that if no LoadingData event has
                // occurred after a second, that no event will occur.
                this._dds.LoadDelay = TimeSpan.FromMilliseconds(50);

                deferRefresh = this._collectionView.DeferRefresh();
                this._view.MoveToPage(1);
                this._view.PageSize = 1;
                this._view.MoveToPage(2);
            });

            this.AssertNoLoadingData();
            this.AssertNoPageChanged();

            // Now we'll expect the events
            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                this._asyncEventFailureMessage = "Dispose()";
                deferRefresh.Dispose();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(2, this._view.PageIndex, "PageIndex should be 2 after the Dispose");
                Assert.AreEqual(1, this._view.Count, "Count should be 1 after the dispose");
                Assert.AreEqual("Duvall", ((City)this._view[0]).Name, "this._ecv[0].Name should be Duvall");
                Assert.AreEqual("Duvall", ((City)this._view.CurrentItem).Name, "CurrentItem.Name should be Duvall");
                Assert.AreEqual(0, this._view.CurrentPosition, "CurrentPosition should be 0");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the LoadDelay property functions properly.")]
        public void LoadDelay()
        {
            LoadTimer timer = null;
            this.LoadDomainDataSourceControl();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesInStateQuery";
                this._dds.QueryParameters.Add(new Parameter { ParameterName = "state", Value = "WA" });
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "CountyName", Operator = FilterOperator.IsEqualTo, Value = "King" });
                this._dds.DomainContext = new CityDomainContext();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                Assert.AreEqual(750, this._dds.LoadDelay.Milliseconds);

                timer = this.UseLoadTimer();
                timer.AssertStart(() => this._dds.FilterDescriptors[0].Value = "Lucas", "when setting a filter value.");
                timer.AssertLoadingDataOnTick("after setting a filter value.");
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.LoadDelay = TimeSpan.FromTicks(1);
                timer.AssertStart(() => this._dds.QueryParameters[0].Value = "CA", "when setting a parameter value.");
                timer.AssertLoadingDataOnTick("after setting a parameter value.");
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                timer.AssertAndResetCounts("after the final load.");
            });

            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests whether the AutoLoad property functions properly.")]
        public void AutoLoadData()
        {
            EnqueueCallback(() =>
            {
                Assert.IsFalse(this._dds.AutoLoad);
            });

            this.LoadDomainDataSourceControl();

            EnqueueCallback(() =>
            {
                Assert.IsTrue(this._dds.AutoLoad);
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                int expectedCount = new CityData().Cities.Count;
                Assert.AreEqual(expectedCount, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests whether the AutoLoad property functions properly in conjunction with LoadParameters.")]
        public void AutoLoadDataWithLoadParameters()
        {
            this.LoadDomainDataSourceControl();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();

                this._asyncEventFailureMessage = "Initial AutoLoad";
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._asyncEventFailureMessage = "LoadCitiesInState(WA)";

                this._dds.QueryName = "GetCitiesInStateQuery";
                this._dds.QueryParameters.Add(new Parameter { ParameterName = "state", Value = "WA" });
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._asyncEventFailureMessage = "LoadCities";

                Assert.AreEqual(6, this._view.Count);
                this._dds.QueryParameters.RemoveAt(0);
                this._dds.QueryName = "GetCitiesQuery";
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                Assert.AreEqual(11, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests whether the AutoLoad property functions properly in conjunction with SortDescriptors.")]
        public void AutoLoadDataWithSorting()
        {
            this.LoadDomainDataSourceControl();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Descending));
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual("Toledo", (this._view[0] as City).Name);
                this.ResetLoadState();
                this._dds.SortDescriptors.RemoveAt(0);
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual("Redmond", (this._view[0] as City).Name);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests whether setting AutoLoad to false will stop a running load timer.")]
        public void AutoLoadBeingSetToFalseTurnsOffLoadTimer()
        {
            this.LoadDomainDataSourceControl();

            this.EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
            });

            // Ensure we're past the initial load
            this.AssertLoadingData();

            this.EnqueueCallback(() =>
            {
                this.ResetLoadState();
                // Prompt an autoload...
                this._dds.FilterOperator = FilterDescriptorLogicalOperator.Or;
                // ... and then turn autoloading off and ensure nothing gets loaded
                this._dds.AutoLoad = false;
            });

            this.AssertNoLoadingData();

            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that AutoLoad can be used when the DDS is not included in the visual tree.")]
        public void AutoLoadCanBeUsedOutsideTheVisualTree()
        {
            LoadTimer timer = this.UseLoadTimer();

            this.EnqueueCallback(() =>
            {
                this._dds.AutoLoad = true;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();

                // This asserts the AutoLoad timer has not been touched. It shouldn't be initialized
                // until we explicitly call Load when we're outside of the visual tree.
                timer.AssertAndResetCounts("before loading.");

                this._dds.Load();
            });

            this.AssertLoadedData();

            this.EnqueueCallback(() =>
            {
                this.ResetLoadState();

                timer.AssertStart(() => this._dds.FilterOperator = FilterDescriptorLogicalOperator.Or,
                    "when modifying the FilterOperator.");
                timer.AssertLoadingDataOnTick("after modifying the FilterOperator.");
            });

            this.AssertLoadedData();

            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Description("Tests PropertyPath validation for a FilterDescriptor.")]
        public void InvalidPropertyPathFilterDescriptor()
        {
            this._dds.AutoLoad = true;
            this._dds.QueryName = "GetCitiesQuery";
            this._dds.DomainContext = new CityDomainContext();

            // The first two cases are acceptable and support the binding scenario
            FilterDescriptor fd = new FilterDescriptor(null, FilterOperator.IsEqualTo, string.Empty);
            this._dds.FilterDescriptors.Add(fd);

            fd.PropertyPath = string.Empty;

            this._dds.FilterDescriptors.Clear();

            // The second two cases throw exceptions for property paths that are not valid
            fd.PropertyPath = "invalid";
            AssertExpectedException(
                new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CommonResources.PropertyNotFound,
                        "invalid", "City")),
                () => this._dds.FilterDescriptors.Add(fd));

            AssertExpectedException(
                new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CommonResources.PropertyNotFound,
                        "invalid2", "City")),
                    () => fd.PropertyPath = "invalid2");
        }

        [TestMethod]
        [Description("Tests PropertyPath validation for a GroupDescriptor.")]
        public void InvalidPropertyPathGroupDescriptor()
        {
            this._dds.AutoLoad = true;
            this._dds.QueryName = "GetCitiesQuery";
            this._dds.DomainContext = new CityDomainContext();

            // The first two cases are acceptable and support the binding scenario
            GroupDescriptor gd = new GroupDescriptor(null);
            this._dds.GroupDescriptors.Add(gd);

            gd.PropertyPath = string.Empty;

            this._dds.GroupDescriptors.Clear();

            // The second two cases throw exceptions for property paths that are not valid
            gd.PropertyPath = "invalid";
            AssertExpectedException(
                new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CommonResources.PropertyNotFound,
                        "invalid", "City")),
                () => this._dds.GroupDescriptors.Add(gd));

            AssertExpectedException(
                new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CommonResources.PropertyNotFound,
                        "invalid2", "City")),
                    () => gd.PropertyPath = "invalid2");
        }

        [TestMethod]
        [Description("Tests PropertyPath validation for a SortDescriptor.")]
        public void InvalidPropertyPathSortDescriptor()
        {
            this._dds.AutoLoad = true;
            this._dds.QueryName = "GetCitiesQuery";
            this._dds.DomainContext = new CityDomainContext();

            // The first two cases are acceptable and support the binding scenario
            SortDescriptor sd = new SortDescriptor(null, ListSortDirection.Ascending);
            this._dds.SortDescriptors.Add(sd);

            sd.PropertyPath = string.Empty;

            this._dds.SortDescriptors.Clear();

            // The second two cases throw exceptions for property paths that are not valid
            sd.PropertyPath = "invalid";
            AssertExpectedException(
                new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CommonResources.PropertyNotFound,
                        "invalid", "City")),
                () => this._dds.SortDescriptors.Add(sd));

            AssertExpectedException(
                new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CommonResources.PropertyNotFound,
                        "invalid2", "City")),
                    () => sd.PropertyPath = "invalid2");
        }

        [TestMethod]
        [Description("Tests ParameterName validation for a QueryParameter.")]
        public void InvalidQueryParameterName()
        {
            this._dds.AutoLoad = true;
            this._dds.QueryName = "GetCitiesInStateQuery";
            this._dds.DomainContext = new CityDomainContext();

            // The first two cases are acceptable and support the binding scenario
            Parameter p = new Parameter { ParameterName = null, Value = "WA" };
            this._dds.QueryParameters.Add(p);

            p.ParameterName = string.Empty;

            this._dds.QueryParameters.Clear();

            // The second two cases throw exceptions for parameter names that are not valid
            p.ParameterName = "invalid";
            AssertExpectedException(
                new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DomainDataSourceResources.EntityQueryMethodHasMismatchedArguments,
                        "GetCitiesInStateQuery")),
                () => this._dds.QueryParameters.Add(p));

            AssertExpectedException(
                new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DomainDataSourceResources.EntityQueryMethodHasMismatchedArguments,
                        "GetCitiesInStateQuery")),
                    () => p.ParameterName = "invalid2");
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that using descriptors with null or empty property paths does not trigger an autoload.")]
        public void EmptyDescriptorPropertyPathsPostponeAutoLoad()
        {
            this._dds.AutoLoad = true;
            this._dds.QueryName = "GetCitiesInStateQuery";
            this._dds.DomainContext = new CityDomainContext();

            this.LoadDomainDataSourceControl();

            this.EnqueueCallback(() =>
            {
                this._dds.FilterDescriptors.Add(new FilterDescriptor());
                this._dds.FilterDescriptors[0].PropertyPath = null;
                this._dds.GroupDescriptors.Add(new GroupDescriptor());
                this._dds.GroupDescriptors[0].PropertyPath = null;
                this._dds.QueryParameters.Add(new Parameter());
                this._dds.QueryParameters[0].ParameterName = null;
                this._dds.SortDescriptors.Add(new SortDescriptor());
                this._dds.SortDescriptors[0].PropertyPath = null;
            });

            this.AssertNoLoadingData();

            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests whether the AutoLoad property functions properly in conjunction with FilterDescriptors.")]
        public void AutoLoadDataWithFiltering()
        {
            this.LoadDomainDataSourceControl();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "Name", Operator = FilterOperator.IsEqualTo, Value = "Orange" });
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual("Orange", (this._view[0] as City).Name);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests whether the LoadSize property functions properly.")]
        public void ProgressiveLoadData()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.LoadInterval = defaultLoadInterval;
                this._dds.LoadSize = 5;
                this._dds.Load();
            });

            AssertLoadingData(3);

            EnqueueCallback(() =>
            {
                Assert.AreEqual(11, this._view.Count, "Collection should contain 11 cities.");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that data is loaded correctly with a basic Refresh.")]
        public void RefreshData()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.RefreshInterval = TimeSpan.FromSeconds(5);
                this._dds.Load();
            });

            this.AssertLoadingData(); // checks to see that data loaded

            EnqueueCallback(() =>
            {
                this.ResetLoadState();    // clears the data loaded flag
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(11, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that data is loaded correctly with a Refresh operation and a LoadSize.")]
        public void RefreshDataWithLoadSize()
        {
            ProgressiveLoadTimer pTimer = null;
            RefreshLoadTimer rTimer = null;

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.LoadSize = (new CityData().Cities.Count / 2) + 1; // --> 2 progressive loads

                pTimer = this.UseProgressiveLoadTimer();
                rTimer = this.UseRefreshLoadTimer();

                this._dds.RefreshInterval = TimeSpan.FromTicks(1);
                rTimer.AssertStarted();

                pTimer.ExpectedStartCount = 1;
                rTimer.AssertLoadingDataOnTick("when starting the first refresh load.");
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                rTimer.AssertAndResetCounts("on the RefreshLoadTimer after completing the first refresh load.");
                pTimer.AssertAndResetCounts("on the ProgressiveLoadTimer after completing the first refresh load.");

                pTimer.AssertLoadingDataOnTick("when starting the first progressive load.");
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                rTimer.AssertAndResetCounts("on the RefreshLoadTimer after completing the first progressive load.");
                pTimer.AssertAndResetCounts("on the ProgressiveLoadTimer after completing the first progressive load.");

                pTimer.ExpectedStartCount = 1;
                rTimer.AssertLoadingDataOnTick("when starting the second refresh load.");
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                rTimer.AssertAndResetCounts("on the RefreshLoadTimer after completing the second refresh load.");
                pTimer.AssertAndResetCounts("on the ProgressiveLoadTimer after completing the second refresh load.");

                pTimer.AssertLoadingDataOnTick("when starting the second progressive load.");
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                rTimer.AssertAndResetCounts("on the RefreshLoadTimer after completing the second progressive load.");
                pTimer.AssertAndResetCounts("on the ProgressiveLoadTimer after completing the second progressive load.");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that data is loaded correctly with a Refresh operation and a PageSize.")]
        public void RefreshDataWithPageSize()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.PageSize = 5;
                this._dds.RefreshInterval = TimeSpan.FromMilliseconds(1000);
                this._dds.Load();
                this._asyncEventFailureMessage = "Initial Load";
            });

            this.AssertLoadingData(); // checks to see that data loaded
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();    // clears the data loaded flag
                this.ResetPageChanged();
                this._asyncEventFailureMessage = "Automatic Refresh";
            });

            // Wait for the refresh
            EnqueueDelay(1000);

            EnqueueCallback(() =>
            {
                Assert.AreEqual(1, this._ddsLoadingData, "LoadingData should be 1");
                Assert.AreEqual(5, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When a Refresh occurs, the page should not change")]
        public void RefreshRetainsCurrentPage()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.PageSize = 5;
                this._dds.Load();
                this._asyncEventFailureMessage = "Initial Load";
            });

            this.AssertLoadingData(); // checks to see that data loaded
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();    // clears the data loaded flag
                this.ResetPageChanged();

                this._dds.MoveToPage(1);
                this._asyncEventFailureMessage = "MoveToPage(1)";
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual<int>(1, this._view.PageIndex, "PageIndex after MoveToPage(1)");
                this._viewPageChangedExpected = 0;
                this._collectionView.Refresh();
                this._asyncEventFailureMessage = "Refresh()";
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual<int>(1, this._view.PageIndex, "PageIndex after Refresh()");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When sorts are applied before a load occurs, a deferred page move doesn't occur on a subsequent refresh")]
        public void RefreshRetainsCurrentPageAfterApplyingSortsBeforeLoad()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.PageSize = 5;
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
                this._dds.Load();
                this._asyncEventFailureMessage = "Initial Load";
            });

            this.AssertLoadingData(); // checks to see that data loaded
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();    // clears the data loaded flag
                this.ResetPageChanged();

                this._dds.MoveToPage(1);
                this._asyncEventFailureMessage = "MoveToPage(1)";
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual<int>(1, this._view.PageIndex, "PageIndex after MoveToPage(1)");
                this._viewPageChangedExpected = 0;
                this._collectionView.Refresh();
                this._asyncEventFailureMessage = "Refresh()";
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual<int>(1, this._view.PageIndex, "PageIndex after Refresh()");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When groups are applied before a load occurs, a deferred page move doesn't occur on a subsequent refresh")]
        public void RefreshRetainsCurrentPageAfterApplyingGroupsBeforeLoad()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.PageSize = 5;
                this._dds.GroupDescriptors.Add(new GroupDescriptor("StateName"));
                this._dds.Load();
                this._asyncEventFailureMessage = "Initial Load";
            });

            this.AssertLoadingData(); // checks to see that data loaded
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();    // clears the data loaded flag
                this.ResetPageChanged();

                this._dds.MoveToPage(1);
                this._asyncEventFailureMessage = "MoveToPage(1)";
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual<int>(1, this._view.PageIndex, "PageIndex after MoveToPage(1)");
                this._viewPageChangedExpected = 0;
                this._collectionView.Refresh();
                this._asyncEventFailureMessage = "Refresh()";
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual<int>(1, this._view.PageIndex, "PageIndex after Refresh()");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When filters are applied before a load occurs, a deferred page move doesn't occur on a subsequent refresh")]
        public void RefreshRetainsCurrentPageAfterApplyingFiltersBeforeLoad()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.PageSize = 5;
                this._dds.FilterDescriptors.Add(new FilterDescriptor("StateName", FilterOperator.IsNotEqualTo, "AA"));
                this._dds.Load();
                this._asyncEventFailureMessage = "Initial Load";
            });

            this.AssertLoadingData(); // checks to see that data loaded
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();    // clears the data loaded flag
                this.ResetPageChanged();

                this._dds.MoveToPage(1);
                this._asyncEventFailureMessage = "MoveToPage(1)";
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual<int>(1, this._view.PageIndex, "PageIndex after MoveToPage(1)");
                this._viewPageChangedExpected = 0;
                this._collectionView.Refresh();
                this._asyncEventFailureMessage = "Refresh()";
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual<int>(1, this._view.PageIndex, "PageIndex after Refresh()");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When query parameters are applied before a load occurs, a deferred page move doesn't occur on a subsequent refresh")]
        public void RefreshRetainsCurrentPageAfterApplyingQueryParametersBeforeLoad()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesInState";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.PageSize = 5;
                this._dds.QueryParameters.Add(new Parameter { ParameterName = "state", Value = "WA" });
                this._dds.Load();
                this._asyncEventFailureMessage = "Initial Load";
            });

            this.AssertLoadingData(); // checks to see that data loaded
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();    // clears the data loaded flag
                this.ResetPageChanged();

                this._dds.MoveToPage(1);
                this._asyncEventFailureMessage = "MoveToPage(1)";
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual<int>(1, this._view.PageIndex, "PageIndex after MoveToPage(1)");
                this._viewPageChangedExpected = 0;
                this._collectionView.Refresh();
                this._asyncEventFailureMessage = "Refresh()";
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual<int>(1, this._view.PageIndex, "PageIndex after Refresh()");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that data is not loaded when a Refresh operation is called while the RefreshInterval is set to 0.")]
        public void RefreshDataWithZeroInterval()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.RefreshInterval = TimeSpan.FromMilliseconds(0);
                this._dds.Load();
            });

            this.AssertLoadingData();   // checks to see that data loaded
            EnqueueCallback(() =>
            {
                this.ResetLoadState();      // clears the data loaded flag
            });
            this.AssertNoLoadingData(); // verifies that data not refreshed

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the current item/position is retained with a Refresh.")]
        public void RefreshDataAndCheckCurrency()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.RefreshInterval = TimeSpan.FromMilliseconds(1000);
                this._dds.Load();
            });

            this.AssertLoadingData(); // checks to see that data loaded
            EnqueueCallback(() =>
            {
                this.ResetLoadState();    // clears the data loaded flag
            });

            EnqueueCallback(() =>
            {
                this._view.MoveCurrentToPosition(1);
                Assert.AreEqual(this._view[1], this._view.CurrentItem);
                Assert.AreEqual(1, this._view.CurrentPosition);

                this._dds.Loaded += new RoutedEventHandler(delegate
                {
                    Assert.AreEqual(this._view[1], this._view.CurrentItem);
                    Assert.AreEqual(1, this._view.CurrentPosition);
                });
            });

            this.AssertLoadingData(); // checks that data loaded again with refresh

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(783184)]
        [Description("Setting the RefreshInterval after the initial load should start the refresh timer")]
        public void RefreshIntervalSetAfterInitialLoad()
        {
            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.RefreshInterval = TimeSpan.FromMilliseconds(1000);
            });

            this.AssertLoadingData(); // checks that data loaded again with refresh

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests loading data within a DomainDataSource.LoadedData event handler.")]
        public void LoadDataWithinLoadedData()
        {
            this.LoadDomainDataSourceControl();

            EnqueueCallback(() =>
            {
                this._reload = true;
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
                Assert.IsTrue(this._dds.IsBusy);
                Assert.IsTrue(this._dds.IsLoadingData);
            });

            this.AssertLoadingData(2);

            EnqueueCallback(() =>
            {
                Assert.IsFalse(this._dds.IsBusy);
                Assert.IsFalse(this._dds.IsLoadingData);
                Assert.AreEqual(11, this._dds.Data.Cast<Entity>().Count());
                Assert.AreEqual(11, this._view.Count);
            });

            EnqueueTestComplete();
        }
        [TestMethod]
        [Asynchronous]
        [Description("Tests that loading or submitting within a DomainDataSource.LoadingData event handler throws.")]
        public void LoadingDataReentranceThrows()
        {
            this._dds.LoadingData += (sender, e) =>
            {
                InvalidOperationException ex =
                    new InvalidOperationException(DomainDataSourceResources.InvalidOperationDuringLoadOrSubmit);
                AssertExpectedException<InvalidOperationException>(ex, () => this._dds.Load());
                AssertExpectedException<InvalidOperationException>(ex, () => this._dds.SubmitChanges());
                AssertExpectedException<InvalidOperationException>(ex, () => this._dds.PageSize = 6);
                AssertExpectedException<InvalidOperationException>(ex, () => this._dds.LoadSize = 6);
                AssertExpectedException<InvalidOperationException>(ex, () => this._dds.QueryName = "GetCitiesInStateQuery");
            };

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that loading or submitting within a DomainDataSource.SubmittingChanges event handler throws.")]
        public void SubmittingChangesReentranceThrows()
        {
            this._dds.SubmittingChanges += (sender, e) =>
            {
                InvalidOperationException ex =
                    new InvalidOperationException(DomainDataSourceResources.InvalidOperationDuringLoadOrSubmit);
                AssertExpectedException<InvalidOperationException>(ex, () => this._dds.Load());
                AssertExpectedException<InvalidOperationException>(ex, () => this._dds.SubmitChanges());
                AssertExpectedException<InvalidOperationException>(ex, () => this._dds.PageSize = 6);
                AssertExpectedException<InvalidOperationException>(ex, () => this._dds.LoadSize = 6);
                AssertExpectedException<InvalidOperationException>(ex, () => this._dds.QueryName = "GetCitiesInStateQuery");
            };

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.SubmitChanges();
            });

            this.AssertSubmittingChanges();

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests changing the page size within a DomainDataSource.LoadedData event handler.")]
        public void ChangePageSizeWithinLoadedData()
        {
            this.LoadDomainDataSourceControl();

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.LoadedData += this.DomainDataSourceLoadedDataWithPageSizeIncrement;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
                Assert.IsTrue(this._dds.IsBusy);
                Assert.IsTrue(this._dds.IsLoadingData);
                Assert.AreEqual(0, this._dds.PageSize);
            });

            this.AssertLoadingData(3);

            EnqueueCallback(() =>
            {
                Assert.IsFalse(this._dds.IsBusy,
                    "DDS should no longer be busy.");
                Assert.IsFalse(this._dds.IsLoadingData,
                    "DDS should no longer be loading.");
                Assert.AreEqual(2, this._dds.PageSize);
                Assert.AreEqual(2, this._dds.Data.Cast<Entity>().Count());
                Assert.AreEqual(2, this._view.Count);
                this._dds.LoadedData -= this.DomainDataSourceLoadedDataWithPageSizeIncrement;
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Ensure that the LoadingData event is only raised once when Load() is called manually")]
        public void LoadingDataEventCalledOnlyOnce()
        {
            // We use a local instance of the control so that we
            // can mimic calling Load from within the page's constructor
            // as best as possible.
            bool loadedControl = false;
            DomainDataSource ddsLocal = new DomainDataSource();
            ddsLocal.Loaded += (s, e) => loadedControl = true;
            this.TestPanel.Children.Add(ddsLocal);
            EnqueueConditional(() => loadedControl);

            int loadingCount = 0;
            bool loadedData = false;

            ddsLocal.DomainContext = new CityDomainContext();
            ddsLocal.QueryName = "GetCitiesQuery";
            ddsLocal.LoadingData += (s, e) => loadingCount++;
            ddsLocal.LoadedData += (s, e) => loadedData = true;

            Assert.AreEqual(0, loadingCount, "LoadingData should be zero before calling Load()");
            ddsLocal.Load();

            EnqueueConditional(() => loadedData);
            EnqueueCallback(() =>
            {
                Assert.AreEqual(1, loadingCount, "LoadingData should have only been raised once");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("The correct collection changed events are fired during a single page load")]
        public void CollectionChangedEventsDuringSinglePageLoad()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.PageSize = 5;

                this.TrackCollectionChanged();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                // TODO: Change from 2 Reset events to 1 when bug 709185 is fixed and our workaround can be removed
                //this.AssertCollectionChanged(0, 0, 1, "A single reset event should have occurred with the load");
                this.AssertCollectionChanged(0, 0, 2, "Two reset events shoulde have occurred with the load.");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("The correct collection changed events are fired during a load of multiple pages of data")]
        public void CollectionChangedEventsDuringMultiPageLoad()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.PageSize = 5;
                this._dds.LoadSize = 15;

                this.TrackCollectionChanged();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                // TODO: Change from 2 Reset events to 1 when bug 709185 is fixed and our workaround can be removed
                //this.AssertCollectionChanged(0, 0, 1, "A single reset event should have occurred with the load");
                this.AssertCollectionChanged(0, 0, 2, "Two reset events shoulde have occurred with the load.");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("After the initial load with a single page, the Data enumerator is ready")]
        public void EnumeratorReadyAfterInitialSinglePageLoad()
        {
            CityDomainContext context = new CityDomainContext();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.PageSize = 5;

                this.TrackCollectionChanged();
                this._dds.Load();

                bool isInitialLoad = true;

                this._collectionView.CollectionChanged += (s, e) =>
                {
                    if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset && !isInitialLoad)
                    {
                        Assert.AreEqual(5, this._view.Count, "this._ecv.Count should be 5 when the Reset event occurs");
                    }

                    isInitialLoad = false;
                };
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                int count = 0;
                City[] cities = context.Cities.ToArray();
                foreach (Entity entity in this._view)
                {
                    Assert.AreSame(cities[count++], entity, "Entity mismatch at index " + (count - 1).ToString());
                }

                Assert.AreEqual(5, count, "The enumerator should have had 5 items in it");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify that the expected events occur in the expected order during a load")]
        public void LoadEventSequence()
        {
            List<string> events = null;
            List<string> expectedEvents = new List<string>
            {
                "LoadingData: GetCities",
                "End synchronous events",
                "PropertyChanged: IsPageChanging(True)",
                "PropertyChanged: PageIndex(0)",
                "PropertyChanged: CanAdd(True)",
                "PropertyChanged: ItemCount",
                "LoadedData: 5",
                "PropertyChanged: IsPageChanging(False)",
                "PageChanged: 0",
                "CollectionChanged: Reset",
                "CollectionChanged: Reset", // TODO: Change from 2 Reset events to 1 when bug 709185 is fixed and our workaround can be removed
                "PropertyChanged: Count(5)",
                "PropertyChanged: IsEmpty(False)",
                "CurrentChanging",
                "CurrentChanged: {0}",
                "PropertyChanged: CurrentPosition(0)",
                "PropertyChanged: CurrentItem({0})"
            };

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.PageSize = 5;

                events = this.TrackEventDetails();
                this._dds.Load();
                events.Add("End synchronous events");
            });

            this.AssertLoadingData();
            this.AssertPageChanged();
            EnqueueDelay(1000);

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                City currentCity = (City)this._view[0];
                this.AssertEventDetailsMatch(expectedEvents, events, currentCity);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797331)]
        [Description("When loads are performed, the count properties are available (paging is disabled)")]
        public void CountPropertiesUpdatedWithLoads()
        {
            EnqueueCallback(() =>
            {
                // Catalog supports TotalItemCount without loading all data (CityDomainContext does not)
                this._dds.QueryName = "GetProducts";
                this._dds.DomainContext = new Catalog();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                Assert.AreNotEqual<int>(0, this._view.Count, "Count after the initial load");
                Assert.AreNotEqual<int>(-1, this._view.TotalItemCount, "TotalItemCount after the initial load should be -1 since we are not paging");
                Assert.AreNotEqual<int>(0, this._pagedCollectionView.ItemCount, "ItemCount (IPagedCollectionView) after the initial load");

                // Apply a filter that will guarantee an empty load
                this._dds.FilterDescriptors.Add(new FilterDescriptor("Name", FilterOperator.IsEqualTo, Guid.NewGuid().ToString()));

                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                Assert.AreEqual<int>(0, this._view.Count, "Count after the initial load");
                Assert.AreEqual<int>(-1, this._view.TotalItemCount, "TotalItemCount after the initial load should be -1 since we are not paging");
                Assert.AreEqual<int>(0, this._pagedCollectionView.ItemCount, "ItemCount (IPagedCollectionView) after the initial load");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("When loads are performed, the count properties are available (paging is enabled)")]
        public void CountPropertiesUpdatedWithLoadsWithPaging()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.PageSize = 2;
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                this._dds.MoveToPage(5);
            });

            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(11, this._view.TotalItemCount, "TotalItemCount should be 11 on the last page of the first set of loads");

                this._dds.QueryName = "GetCitiesInStateQuery";
                this._dds.QueryParameters.Add(new Parameter { ParameterName = "state", Value = "WA" });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(-1, this._view.TotalItemCount, "TotalItemCount should be -1 after changing the load");

                this._dds.MoveToPage(2);
            });

            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(-1, this._view.TotalItemCount, "TotalItemCount should be -1 after moving to page 2 after changing the filter");

                this._dds.MoveToPage(3);
            });

            this.AssertLoadingData(2);
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(6, this._view.TotalItemCount, "TotalItemCount should be 6 after moving past the end after changing the filter");
                Assert.AreEqual(2, this._view.PageIndex, "PageIndex should have been restored to 2 after moving past the end");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(785978)]
        [Description("After a load with no results, the next load gets its ItemCount, TotalItemCount, and PageCount properties updated")]
        public void CountPropertiesRefreshedWithLoadAfterEmptyLoad()
        {
            // Catalog supports TotalItemCount without loading all data (CityDomainContext does not)
            Catalog context = new Catalog();
            int totalItemCountExpected = -1;
            Dictionary<string, int> propertiesChanged = new Dictionary<string, int>();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetProducts";
                this._dds.DomainContext = context;
                this._dds.PageSize = 2;

                // Apply a filter that will guarantee an empty load
                this._dds.FilterDescriptors.Add(new FilterDescriptor("Name", FilterOperator.IsEqualTo, Guid.NewGuid().ToString()));
                this._dds.Load();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual<int>(1, this._view.PageCount, "PageCount after the empty load");
                Assert.AreEqual<int>(0, this._view.TotalItemCount, "TotalItemCount after the empty load");

                // Overwrite the filter with one that will guarantee all data is loaded
                this._dds.FilterDescriptors[0] = new FilterDescriptor("Name", FilterOperator.IsNotEqualTo, Guid.NewGuid().ToString());

                // When this data is loaded, set the expectation for TotalItemCount
                this._dds.LoadedData += (s, e) => totalItemCountExpected = e.TotalEntityCount;

                ((INotifyPropertyChanged)this._view).PropertyChanged += (s, e) =>
                {
                    if (!propertiesChanged.ContainsKey(e.PropertyName))
                    {
                        propertiesChanged.Add(e.PropertyName, 1);
                    }
                    else
                    {
                        propertiesChanged[e.PropertyName] = propertiesChanged[e.PropertyName] + 1;
                    }
                };

                this._viewPageChangedExpected = 0;
                this._asyncEventFailureMessage = "Full load";
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                // This test isn't valid unless more than 1 page of data was loaded
                if (this._view.TotalItemCount == context.Products.Count)
                {
                    Assert.Inconclusive("TotalItemCount must be greater than the number of products loaded in order for this to be a valid test.");
                }

                Assert.AreEqual<int>(totalItemCountExpected, this._view.TotalItemCount, "TotalItemCount after full load");
                Assert.IsTrue(propertiesChanged.ContainsKey("TotalItemCount"), "TotalItemCount should have had a property changed notification");
                Assert.AreEqual<int>(1, propertiesChanged["TotalItemCount"], "TotalItemCount property changed notifications");

                Assert.AreEqual<int>(totalItemCountExpected, this._pagedCollectionView.ItemCount, "ItemCount after full load");
                Assert.IsTrue(propertiesChanged.ContainsKey("ItemCount"), "ItemCount should have had a property changed notification");
                Assert.AreEqual<int>(1, propertiesChanged["ItemCount"], "ItemCount property changed notifications");

                int pageCount = PagingHelper.CalculatePageCount(this._view.TotalItemCount, this._dds.PageSize);
                Assert.AreEqual<int>(pageCount, this._view.PageCount, "PageCount after full load");
                Assert.IsTrue(propertiesChanged.ContainsKey("PageCount"), "PageCount should have had a property changed notification");
                Assert.AreEqual<int>(1, propertiesChanged["PageCount"], "PageCount property changed notifications");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797331)]
        [Description("The ItemCount and TotalItemCount properties are set when LoadedData is raised (with paging disabled)")]
        public void CountPropertiesAvailableWithinLoadedData()
        {
            // Catalog supports TotalItemCount without loading all data (CityDomainContext does not)
            Catalog context = new Catalog();

            this._dds.LoadedData += (s, e) =>
            {
                Assert.AreNotEqual<int>(0, this._view.Count, "Count");
                Assert.AreNotEqual<int>(0, this._view.TotalItemCount, "TotalItemCount");
                Assert.AreNotEqual<int>(0, this._pagedCollectionView.ItemCount, "ItemCount (IPagedCollectionView)");
            };

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetProducts";
                this._dds.DomainContext = context;
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797331)]
        [Description("The ItemCount and TotalItemCount properties are set when LoadedData is raised (with paging enabled)")]
        public void CountPropertiesAvailableWithinLoadedDataWithPaging()
        {
            // Catalog supports TotalItemCount without loading all data (CityDomainContext does not)
            Catalog context = new Catalog();

            this._dds.LoadedData += (s, e) =>
            {
                Assert.AreEqual<int>(5, this._view.Count, "Count");
                Assert.AreNotEqual<int>(0, this._view.TotalItemCount, "TotalItemCount");
                Assert.AreNotEqual<int>(0, this._pagedCollectionView.ItemCount, "ItemCount (IPagedCollectionView)");
            };

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetProducts";
                this._dds.DomainContext = context;
                this._dds.PageSize = 5;
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueTestComplete();
        }

        [TestMethod]
        [Description("When the QueryName doesn't end in Query, it's added as a suffix and resolved")]
        public void LoadAddsQuerySuffix()
        {
            string queryName = null;
            Action<EntityQuery, LoadBehavior, object> loadCallback = (query, loadBehavior, userstate) =>
            {
                queryName = query.QueryName;
            };

            this._dds.DomainContext = new MockContext<City>(loadCallback, null);
            this._dds.QueryName = "Basic";

            this._dds.Load();
            Assert.AreEqual("BasicQuery", queryName);
        }

        [TestMethod]
        [Description("When the QueryName references an overlaoded method, and no parameters exist, the correct overload is resolved")]
        public void LoadFindsOverloadedQuery()
        {
            string queryName = null;
            IDictionary<string, object> parameters = null;

            Action<EntityQuery, LoadBehavior, object> loadCallback = (query, loadBehavior, userstate) =>
            {
                queryName = query.QueryName;
                parameters = query.Parameters;
            };

            this._dds.DomainContext = new MockContext<City>(loadCallback, null);
            this._dds.QueryName = "Overload";

            this._dds.Load();
            Assert.AreEqual("OverloadQuery", queryName, "The zero-parameter overload match should resolve to OverloadQuery");
            Assert.IsNull(parameters, "There should not be any parameters for the zero-parameter overload match");
        }

        [TestMethod]
        [Description("When the QueryName references an overlaoded method, and parameters exist, the correct overload is resolved")]
        public void LoadFindsOverloadedQueryWithParameters()
        {
            string queryName = null;
            IDictionary<string, object> parameters = null;

            Action<EntityQuery, LoadBehavior, object> loadCallback = (query, loadBehavior, userstate) =>
            {
                queryName = query.QueryName;
                parameters = query.Parameters;
            };

            this._dds.DomainContext = new MockContext<City>(loadCallback, null);
            this._dds.QueryName = "Overload";

            this._dds.QueryParameters.Add(new Parameter { ParameterName = "parameter1", Value = "WA" });
            this._dds.Load();
            Assert.AreEqual("OverloadQuery", queryName, "The one-parameter overload match should resolve to OverloadQuery");
            Assert.AreEqual(1, parameters.Count(), "There should be one parameter for the one-parameter overload match");
        }

        [TestMethod]
        [Description("When an EntityQuery is specified that doesn't end in Query, and the method doesn't end in Query, it's found")]
        public void LoadFindsEntityQueryWithoutSuffix()
        {
            string queryName = null;
            Action<EntityQuery, LoadBehavior, object> loadCallback = (query, loadBehavior, userstate) =>
            {
                queryName = query.QueryName;
            };

            this._dds.DomainContext = new MockContext<City>(loadCallback, null);
            this._dds.QueryName = "NoQuerySuffix";

            this._dds.Load();
            Assert.AreEqual("NoQuerySuffix", queryName);
        }

        [TestMethod]
        [Description("When the QueryName supplied doesn't end in Query, and there are methods both with and without the suffix, Load prefers the one with the suffix")]
        public void LoadPrefersMethodsEndingInQuery()
        {
            string queryName = null;
            Action<EntityQuery, LoadBehavior, object> loadCallback = (query, loadBehavior, userstate) =>
            {
                queryName = query.QueryName;
            };

            this._dds.DomainContext = new MockContext<City>(loadCallback, null);
            this._dds.QueryName = "WithAndWithoutSuffix";

            this._dds.Load();
            Assert.AreEqual("WithAndWithoutSuffixQuery", queryName);
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(829293)]
        [Description("The DDS must continue to be usable after an exception occurs during a load")]
        public void LoadWithErrorIsRecoverable()
        {
            bool expectError = false;

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesInState";
                this._dds.DomainContext = new TestProvider_Scenarios();

                expectError = true;
                this._dds.QueryParameters.Add(new Parameter() { ParameterName = "state", Value = null });

                this._dds.LoadedData += (sender, e) =>
                {
                    Assert.AreEqual(expectError, e.HasError,
                        "The operation should only return an error when we expect it.");
                    if (e.HasError)
                    {
                        if (!e.Error.Message.Contains("state"))
                        {
                            Assert.Fail("We're expecting an ArgumentNullException for state.");
                        }
                        e.MarkErrorAsHandled();
                    }
                };

                this._ddsLoadErrorExpected = true;
                this._dds.Load();
                this._asyncEventFailureMessage = "GetCitiesInState fails";
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                expectError = false;
                this._dds.QueryParameters[0].Value = "WA"; // state

                this._dds.Load();

                this._asyncEventFailureMessage = "GetCitiesInState succeeds";
            });

            this.AssertLoadingData();

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(856817)]
        [Description("In LoadingData, the IncludeTotalCount property on the query can be overridden to include the count when not paging")]
        public void IncludeTotalCountOverrideWhenNotPaging()
        {
            // Catalog supports total count
            Catalog catalog = new Catalog();

            bool defaultIncludeTotalCount = false;
            int totalEntityCount = 0;

            // When the LoadingData event is raised, we will capture the default
            // value for IncludeTotalCount and then set it to true
            this._dds.LoadingData += (s, e) =>
                {
                    defaultIncludeTotalCount = e.Query.IncludeTotalCount;
                    e.Query.IncludeTotalCount = true;
                };

            // When the LoadedData event is raised, we will capture the
            // TotalEntityCount to ensure it was set
            this._dds.LoadedData += (s, e) =>
                {
                    totalEntityCount = e.TotalEntityCount;
                };

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.DomainContext = catalog;
                this._dds.QueryName = "GetProducts";
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.IsFalse(defaultIncludeTotalCount, "IncludeTotalCount should have been false by default");
                Assert.AreEqual(catalog.Products.Count, totalEntityCount, "The TotalEntityCount should match the number of products loaded");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(856817)]
        [Description("In LoadingData, the IncludeTotalCount property on the query can be overridden to not include the count when paging")]
        public void IncludeTotalCountOverrideWhenPaging()
        {
            // Catalog supports total count
            Catalog catalog = new Catalog();

            bool defaultIncludeTotalCount = false;
            int totalEntityCount = 0;

            // When the LoadingData event is raised, we will capture the default
            // value for IncludeTotalCount and then set it to true
            this._dds.LoadingData += (s, e) =>
            {
                defaultIncludeTotalCount = e.Query.IncludeTotalCount;
                e.Query.IncludeTotalCount = false;
            };

            // When the LoadedData event is raised, we will capture the
            // TotalEntityCount to ensure it was set
            this._dds.LoadedData += (s, e) =>
            {
                totalEntityCount = e.TotalEntityCount;
            };

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.DomainContext = catalog;
                this._dds.QueryName = "GetProducts";
                this._dds.PageSize = 5;
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.IsTrue(defaultIncludeTotalCount, "IncludeTotalCount should have been true by default");
                Assert.AreEqual(-1, totalEntityCount, "The TotalEntityCount should not have been included");
            });

            EnqueueTestComplete();
        }

        #endregion Loading

        #region Editing

        [TestMethod]
        [Asynchronous]
        [Description("Editing an item without a transaction sets HasChanges to true")]
        public void HasChangesAfterEditingItemWithoutTransaction()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                Assert.IsFalse(this._dds.HasChanges, "DDS should not have changes before anything is edited");
                Assert.AreEqual(11, this._view.Count, "There should have been 11 items initially loaded");

                (this._view[0] as City).StateName = "ST";
                Assert.IsTrue(this._dds.HasChanges, "DDS should have changes after editing a city, without using EditItem");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Editing an item with a transaction sets HasChanges to true")]
        public void HasChangesAfterEditingItemWithTransaction()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                Assert.IsFalse(this._dds.HasChanges, "DDS should not have changes before anything is edited");
                Assert.AreEqual(11, this._view.Count, "There should have been 11 items initially loaded");

                this._editableCollectionView.EditItem(this._view[0]);
                (this._view[0] as City).StateName = "ST";
                Assert.IsTrue(this._dds.HasChanges, "DDS should have changes after editing a city, using EditItem");

                this._editableCollectionView.CommitEdit();
                Assert.IsTrue(this._dds.HasChanges, "DDS should have changes after committing the edit");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Adding an item sets HasChanges to true")]
        public void HasChangesAfterAddingItem()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                Assert.IsFalse(this._dds.HasChanges, "DDS should not have changes before anything is edited");
                Assert.AreEqual(11, this._view.Count, "There should have been 11 items initially loaded");

                City city = this._editableCollectionView.AddNew() as City;
                city.Name = "City Name";
                city.StateName = "ST";
                Assert.IsTrue(this._dds.HasChanges, "DDS should have changes after adding an item with AddNew");

                this._editableCollectionView.CommitNew();
                Assert.IsTrue(this._dds.HasChanges, "DDS should have changes after committing the new item");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Adding an item to the associated domain context sets HasChanges to true")]
        public void HasChangesAfterAddingItemToContext()
        {
            CityDomainContext context = new CityDomainContext();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.Load();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                Assert.IsFalse(this._dds.HasChanges, "DDS should not have changes before anything is edited");
                Assert.AreEqual(11, this._view.Count, "There should have been 11 items initially loaded");

                City city = new City { Name = "City Name", StateName = "ST" };
                context.Cities.Add(city);
                Assert.IsTrue(this._dds.HasChanges, "DDS should have changes after adding an item with AddNew");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Removing an item from Data sets HasChanges to true")]
        public void HasChangesAfterRemovingItem()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                Assert.IsFalse(this._dds.HasChanges, "DDS should not have changes before anything is edited");
                Assert.AreEqual(11, this._view.Count, "There should have been 11 items initially loaded");

                this._view.RemoveAt(0);
                Assert.IsTrue(this._dds.HasChanges, "DDS should have changes after removing an item");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Removing an item from the associated domain context sets HasChanges to true")]
        public void HasChangesAfterRemovingItemFromContext()
        {
            CityDomainContext context = new CityDomainContext();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.Load();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                Assert.IsFalse(this._dds.HasChanges, "DDS should not have changes before anything is edited");
                Assert.AreEqual(11, this._view.Count, "There should have been 11 items initially loaded");

                City city = context.Cities.First();
                context.Cities.Remove(city);
                Assert.IsTrue(this._dds.HasChanges, "DDS should have changes after removing an item from context");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("After removing an item from the Data, the Data and DomainContext are in sync")]
        public void DataInSyncWithContextAfterRemoveFromData()
        {
            CityDomainContext context = new CityDomainContext();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.Load();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(11, this._view.Count, "There should have been 11 items initially loaded");

                this._view.RemoveAt(0);
                Assert.AreEqual(10, this._view.Count, "There should only be 10 items in Data after the remove");
                Assert.AreEqual(10, context.Cities.Count, "There should only be 10 items in context.Cities after the remove");

                foreach (City city in this._view)
                {
                    Assert.IsTrue(context.Cities.Contains(city), "Context should have all cities from this._ecv.  Missing City: " + city.Name + ", " + city.StateName);
                }
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("After removing an item from the Context, the Data and DomainContext are in sync")]
        public void DataInSyncWithContextAfterRemoveFromContext()
        {
            CityDomainContext context = new CityDomainContext();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.Load();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(11, this._view.Count, "There should have been 11 items initially loaded");

                context.Cities.Remove(context.Cities.First());
                Assert.AreEqual(10, this._view.Count, "There should only be 10 items in Data after the remove");
                Assert.AreEqual(10, context.Cities.Count, "There should only be 10 items in context.Cities after the remove");

                foreach (City city in this._view)
                {
                    Assert.IsTrue(context.Cities.Contains(city), "Context should have all cities from this._ecv.  Missing City: " + city.Name + ", " + city.StateName);
                }
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Adding a new item to the DomainContext doesn't affect the DataView")]
        public void AddingToDomainContext()
        {
            CityDomainContext context = new CityDomainContext();

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
                this._dds.Load();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                Assert.IsFalse(this._dds.HasChanges, "this._dds.HasChanges should be false after the initial load");
                Assert.IsFalse(context.HasChanges, "context.HasChanges should be false after the initial load");
                Assert.AreEqual(11, this._view.Count, "this._ecv.Count should be 11 after the initial load");
                Assert.AreEqual(11, context.Cities.Count, "context.Cities.Count should be 11 after the initial load");

                City city = new City { Name = "West Chester", StateName = "OH" };
                context.Cities.Add(city);
                Assert.AreEqual(12, context.Cities.Count, "There should be 12 items in context after adding a new city");
                Assert.AreEqual(11, this._view.Count, "this._ecv.Count should remain 11 after adding a new city to the context, because it wasn't added to the ECV");
                Assert.IsTrue(context.HasChanges, "context.HasChanges should be true after adding to context");
                Assert.IsTrue(this._dds.HasChanges, "this._dds.HasChanges should be true after adding to context");
                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.Name), ListSortDirection.Ascending, "The cities should be sorted by name after adding the city");

                context.Cities.Remove(city);
                Assert.IsFalse(this._dds.HasChanges, "this._dds.HasChanges should be false after removing the new item");
                Assert.IsFalse(context.HasChanges, "context.HasChanges should be false after removing the new item");
                Assert.AreEqual(11, this._view.Count, "this._ecv.Count should be 11 after removing the new item");
                Assert.AreEqual(11, context.Cities.Count, "context.Cities.Count should be 11 after removing the new item");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Adding a new item to the DataView adds it to the DomainContext")]
        public void AddingToDataView()
        {
            CityDomainContext context = new CityDomainContext();

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
                this._dds.Load();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                Assert.IsFalse(this._dds.HasChanges, "this._dds.HasChanges should be false after the initial load");
                Assert.IsFalse(context.HasChanges, "context.HasChanges should be false after the initial load");
                Assert.AreEqual(11, this._view.Count, "this._ecv.Count should be 11 after the initial load");
                Assert.AreEqual(11, context.Cities.Count, "context.Cities.Count should be 11 after the initial load");

                City city = this._editableCollectionView.AddNew() as City;
                Assert.AreEqual(12, context.Cities.Count, "There should be 12 items in context after adding a new city");
                Assert.AreEqual(12, this._view.Count, "this._ecv.Count should be 12 after adding a new city");
                Assert.IsTrue(context.HasChanges, "context.HasChanges should be true after the AddNew");
                Assert.IsTrue(this._dds.HasChanges, "this._dds.HasChanges should be true after the AddNew");
                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.Name), ListSortDirection.Ascending, "The cities should be sorted by name after adding the city");
                city.Name = "Added City";
                city.StateName = "ST";

                this._editableCollectionView.CommitNew();
                Assert.AreEqual(12, context.Cities.Count, "There should be 12 items in context after the CommitNew");
                Assert.AreEqual(12, this._view.Count, "this._ecv.Count should be 12 after the CommitNew");
                Assert.IsTrue(context.HasChanges, "context.HasChanges should be true after the CommitNew");
                Assert.IsTrue(this._dds.HasChanges, "this._dds.HasChanges should be true after the CommitNew");
                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.Name), ListSortDirection.Ascending, "The cities should be sorted by name after committing the city");

                this._view.Remove(city);
                Assert.IsFalse(this._dds.HasChanges, "this._dds.HasChanges should be false after removing the new item");
                Assert.IsFalse(context.HasChanges, "context.HasChanges should be false after removing the new item");
                Assert.AreEqual(11, this._view.Count, "this._ecv.Count should be 11 after removing the new item");
                Assert.AreEqual(11, context.Cities.Count, "context.Cities.Count should be 11 after removing the new item");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(798452)]
        [Description("When an item is added to the view, IndexOf the current item matches the current position")]
        public void AddedItemIndexMatchesCurrentPosition()
        {
            EnqueueCallback(() =>
            {
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
            });

            this.LoadCities(0, false);

            EnqueueCallback(() =>
            {
                City newCity = (City)this._editableCollectionView.AddNew();
                Assert.AreEqual<int>(this._view.CurrentPosition, this._view.IndexOf(newCity), "The index of the new city should match the current position");

                IEnumerable<City> cities = this._view.Cast<City>();
                AssertHelper.AssertSequenceSorting(cities.Select(c => c.Name), ListSortDirection.Ascending, "The cities should be sorted by name");

                for (int i = 0; i < this._view.Count; ++i)
                {
                    Assert.AreEqual<int>(i, this._view.IndexOf(cities.ElementAt(i)), "The index of the element at position " + i + " didn't match");
                }
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(798452)]
        [Description("When an item is added to the view, the view's indexer returns items in the correct positions")]
        public void AddedItemIndexerReturnsCurrentItem()
        {
            EnqueueCallback(() =>
            {
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
            });

            this.LoadCities(0, false);

            EnqueueCallback(() =>
            {
                City newCity = (City)this._editableCollectionView.AddNew();
                Assert.AreSame(this._view.CurrentItem, this._view[this._view.CurrentPosition], "The indexer should return the current item for the index of current position");

                IEnumerable<City> cities = this._view.Cast<City>();
                AssertHelper.AssertSequenceSorting(cities.Select(c => c.Name), ListSortDirection.Ascending, "The cities should be sorted by name");

                for (int i = 0; i < this._view.Count; ++i)
                {
                    Assert.AreSame(cities.ElementAt(i), this._view[i], "The item at index " + i + " didn't match");
                }
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [WorkItem(841634)]
        [Description("AddNew will produce a derived entity type rather than the base type from the EntitySet")]
        public void AddingDerivedEntityUsesDerivedType()
        {
            this._dds.AutoLoad = false;
            this._dds.QueryName = "GetCitiesWithInfoQuery";
            this._dds.DomainContext = new CityDomainContext();

            object newEntity = this._editableCollectionView.AddNew();
            Assert.IsInstanceOfType(newEntity, typeof(CityWithInfo));
        }

        [TestMethod]
        [WorkItem(841634)]
        [Description("CanAddNew is false if the entity type is unknown")]
        public void CanAddNewIsFalseIfEntityTypeIsUnknown()
        {
            Assert.IsFalse(this._editableCollectionView.CanAddNew, "CanAddNew should be false if there is no QueryName or DomainContext");

            this._dds.QueryName = "GetCitiesWithInfoQuery";
            Assert.IsFalse(this._editableCollectionView.CanAddNew, "CanAddNew should be false if there is no DomainContext");

            this._dds.DomainContext = new CityDomainContext();
            Assert.IsTrue(this._editableCollectionView.CanAddNew, "CanAddNew should be true after setting both QueryName and DomainContext");
        }

        [TestMethod]
        [WorkItem(841634)]
        [Description("CanAddNew is false if the entity type is abstract")]
        public void CanAddNewIsFalseForAbstractEntityTypes()
        {
            // CityWithEditHistory is an abstract type
            this._dds.QueryName = "GetCitiesWithEditHistoryQuery";
            this._dds.DomainContext = new CityDomainContext();

            Assert.IsFalse(this._editableCollectionView.CanAddNew);
        }

        [TestMethod]
        [WorkItem(841634)]
        [Description("CanAdd is true when the entity type of the list is abstract, even though CanAddNew is false")]
        public void CanAddIsTrueForAbstractEntityTypes()
        {
            // CityWithEditHistory is an abstract type
            this._dds.QueryName = "GetCitiesWithEditHistoryQuery";
            this._dds.DomainContext = new CityDomainContext();

            Assert.IsTrue(this._view.CanAdd);
        }

        [TestMethod]
        [WorkItem(841634)]
        [Description("Can add a derived entity into a list of abstract entities")]
        public void AddingDerivedEntityIntoAbstractList()
        {
            // CityWithEditHistory is an abstract type
            this._dds.QueryName = "GetCitiesWithEditHistoryQuery";
            this._dds.DomainContext = new CityDomainContext();

            CityWithInfo derivedCity = new CityWithInfo();
            this._view.Add(derivedCity);

            Assert.AreEqual<int>(1, this._view.Count);
            Assert.AreSame(derivedCity, this._view[0]);
        }

        [TestMethod]
        [WorkItem(841634)]
        [Description("Can add a base entity into a list of derived entities")]
        public void AddingBaseEntityIntoDerivedList()
        {
            // CityWithEditHistory derives from City
            this._dds.QueryName = "GetCitiesWithEditHistoryQuery";
            this._dds.DomainContext = new CityDomainContext();

            City baseCity = new City();
            this._view.Add(baseCity);

            Assert.AreEqual<int>(1, this._view.Count);
            Assert.AreSame(baseCity, this._view[0]);
        }

        [TestMethod]
        [Asynchronous]
        [Description("Adding an item to the context after removing it from the context will restore it into the DataView")]
        public void RemovingFromDomainContext()
        {
            CityDomainContext context = new CityDomainContext();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.Load();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                Assert.IsFalse(this._dds.HasChanges, "this._dds.HasChanges should be false after the initial load");
                Assert.IsFalse(context.HasChanges, "context.HasChanges should be false after the initial load");
                Assert.AreEqual(11, this._view.Count, "this._ecv.Count should be 11 after the initial load");
                Assert.AreEqual(11, context.Cities.Count, "context.Cities.Count should be 11 after the initial load");

                City city = this._view[0] as City;

                context.Cities.Remove(city);
                Assert.AreEqual(10, context.Cities.Count, "context.Cities.Count should be 10 after removing a city");
                Assert.AreEqual(10, this._view.Count, "this._ecv.Count should be 10 after removing a city");
                Assert.IsTrue(context.HasChanges, "context.HasChanges should be true after Remove");
                Assert.IsTrue(this._dds.HasChanges, "this._dds.HasChanges should be true after Remove");

                context.Cities.Add(city);
                Assert.AreEqual(11, context.Cities.Count, "context.Cities.Count should be 11 after adding the city back");
                Assert.AreEqual(11, this._view.Count, "this._ecv.Count should be 11 after adding the city back");
                Assert.IsFalse(context.HasChanges, "context.HasChanges should be false after adding the city back");
                Assert.IsFalse(this._dds.HasChanges, "this._dds.HasChanges should be false after adding the city back");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Removing an item from the DataView removes it from DomainContext")]
        public void RemovingFromDataView()
        {
            CityDomainContext context = new CityDomainContext();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.Load();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                Assert.IsFalse(this._dds.HasChanges, "this._dds.HasChanges should be false after the initial load");
                Assert.IsFalse(context.HasChanges, "context.HasChanges should be false after the initial load");
                Assert.AreEqual(11, this._view.Count, "this._ecv.Count should be 11 after the initial load");
                Assert.AreEqual(11, context.Cities.Count, "context.Cities.Count should be 11 after the initial load");

                City city = this._view[0] as City;

                this._view.Remove(city);
                Assert.AreEqual(10, context.Cities.Count, "context.Cities.Count should be 10 after removing a city");
                Assert.AreEqual(10, this._view.Count, "this._ecv.Count should be 10 after removing a city");
                Assert.IsTrue(context.HasChanges, "context.HasChanges should be true after Remove");
                Assert.IsTrue(this._dds.HasChanges, "this._dds.HasChanges should be true after Remove");

                context.Cities.Add(city);
                Assert.AreEqual(11, context.Cities.Count, "context.Cities.Count should be 11 after adding the city back");
                Assert.AreEqual(11, this._view.Count, "this._ecv.Count should be 11 after adding the city back");
                Assert.IsFalse(context.HasChanges, "context.HasChanges should be false after adding the city back");
                Assert.IsFalse(this._dds.HasChanges, "this._dds.HasChanges should be false after adding the city back");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Manipulating entities using a shared domain context between two DomainDataSources")]
        public void RemovingAndAddingFromSharedDomainContext()
        {
            DomainDataSourceView data1 = null;
            DomainDataSourceView data2 = null;

            CityDomainContext context = new CityDomainContext();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.PageSize = 2;
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                // Grab a hold of the loaded data and create a new one
                data1 = this._view;
                Assert.AreEqual(2, context.Cities.Count, "context.Cities.Count should be 2 after loading the 1st dds");

                this.Initialize();

                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                data2 = this._view;

                Assert.AreEqual(2, data1.Count, "data1.Count should be 2 after loading the 2nd DDS");
                Assert.AreEqual(11, data2.Count, "data2.Count should be 11 after loading the 2nd DDS");
                Assert.AreEqual(11, context.Cities.Count, "context.Cities.Count should be 11 after loading the 2nd DDS");

                City city = data1[0] as City;
                data1.Remove(city);
                Assert.AreEqual(1, data1.Count, "data1.Count should be 1 after removing an item");
                Assert.AreEqual(10, data2.Count, "data2.Count should be 10 after removing an item from the data1");
                Assert.AreEqual(10, context.Cities.Count, "context.Cities.Count should be 10 after removing an item from data1");

                context.Cities.Add(city);
                Assert.AreEqual(2, data1.Count, "data1.Count should be 2 after adding the item back to the context");
                Assert.AreEqual(11, data2.Count, "data2.Count should be 11 after adding the item back to the context");
                Assert.AreEqual(11, context.Cities.Count, "context.Cities.Count should be 11 after adding the item back to the context");

                city = data2[8] as City;
                data2.Remove(city);
                Assert.AreEqual(2, data1.Count, "data1.Count should be 2 after removing an item from data2 that doesn't exist in data1");
                Assert.AreEqual(10, data2.Count, "data2.Count should be 10 after removing an item1");
                Assert.AreEqual(10, context.Cities.Count, "context.Cities.Count should be 10 after removing an item from data2");

                context.Cities.Add(city);
                Assert.AreEqual(2, data1.Count, "data1.Count should be 2 after adding the item back to the context");
                Assert.AreEqual(11, data2.Count, "data2.Count should be 11 after adding the item back to the context");
                Assert.AreEqual(11, context.Cities.Count, "context.Cities.Count should be 11 after adding the item back to the context");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(829751)]
        [Description("Clearing the EntitySet during an AddNew transaction should throw an InvalidOperationException")]
        public void ClearingTheEntitySetWhileAddingThrows()
        {
            InvalidOperationException expectedException = new InvalidOperationException(string.Format(
                CultureInfo.InvariantCulture,
                EntityCollectionViewResources.OperationNotAllowedDuringAddOrEdit,
                "Removing"));

            this.LoadCities(0, false);

            EnqueueCallback(() =>
            {
                this._editableCollectionView.AddNew();
                AssertExpectedException(expectedException, () => ((CityDomainContext)this._dds.DomainContext).Cities.Clear());
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(829751)]
        [Description("Clearing the EntitySet during an EditItem transaction should throw an InvalidOperationException")]
        public void ClearingTheEntitySetWhileEditingThrows()
        {
            InvalidOperationException expectedException = new InvalidOperationException(string.Format(
                CultureInfo.InvariantCulture,
                EntityCollectionViewResources.OperationNotAllowedDuringAddOrEdit,
                "Removing"));

            this.LoadCities(0, false);

            EnqueueCallback(() =>
            {
                this._editableCollectionView.EditItem(this._view[0]);
                AssertExpectedException(expectedException, () => ((CityDomainContext)this._dds.DomainContext).Cities.Clear());
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(829751)]
        [Description("Removing the CurrentAddItem during an AddNew transaction should throw an InvalidOperationException")]
        public void RemovingTheCurrentAddItemThrows()
        {
            InvalidOperationException expectedException = new InvalidOperationException(string.Format(
                CultureInfo.InvariantCulture,
                EntityCollectionViewResources.OperationNotAllowedDuringAddOrEdit,
                "Removing"));

            this.LoadCities(0, false);

            EnqueueCallback(() =>
            {
                City city = (City)this._editableCollectionView.AddNew();
                Assert.AreEqual(this._editableCollectionView.CurrentAddItem, city,
                    "city should be the CurrentAddItem.");
                AssertExpectedException(expectedException, () => ((CityDomainContext)this._dds.DomainContext).Cities.Remove(city));
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(829751)]
        [Description("Removing the CurrentEditItem during an EditItem transaction should throw an InvalidOperationException")]
        public void RemovingTheCurrentEditItemThrows()
        {
            InvalidOperationException expectedException = new InvalidOperationException(string.Format(
                CultureInfo.InvariantCulture,
                EntityCollectionViewResources.OperationNotAllowedDuringAddOrEdit,
                "Removing"));

            this.LoadCities(0, false);

            EnqueueCallback(() =>
            {
                City city = (City)this._view[0];
                this._editableCollectionView.EditItem(city);
                Assert.AreEqual(this._editableCollectionView.CurrentEditItem, city,
                    "city should be the CurrentEditItem.");
                AssertExpectedException(expectedException, () => ((CityDomainContext)this._dds.DomainContext).Cities.Remove(city));
            });

            EnqueueTestComplete();
        }

        #endregion Editing

        #region Submitting

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the SubmitChanges method functions properly.")]
        public void SubmitChanges()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            // The submit won't actually work with this domain context
            this._dds.SubmittedChanges += (s, e) => e.MarkErrorAsHandled();

            EnqueueCallback(() =>
            {
                Assert.IsFalse(this._dds.HasChanges, "HasChanges after first load");
                Assert.AreEqual(11, this._view.Count, "Step 1");
                City city = this._editableCollectionView.AddNew() as City;
                city.Name = "Renton";
                city.StateName = "WA";
                Assert.IsTrue(this._dds.HasChanges, "HasChanges after editing the city");
                Assert.AreEqual(12, this._view.Count, "Step 2");
                this._editableCollectionView.CommitNew();
                this._dds.SubmitChanges();
                Assert.IsTrue(this._dds.IsBusy, "IsBusy while submitting changes");
                Assert.IsTrue(this._dds.IsSubmittingChanges, "IsSubmittingChanges");
            });

            this.AssertSubmittingChanges();

            EnqueueCallback(() =>
            {
                Assert.IsFalse(this._dds.HasChanges, "HasChanges after submitted changes");
                Assert.IsFalse(this._dds.IsBusy, "IsBusy after submitted changes");
                Assert.IsFalse(this._dds.IsSubmittingChanges, "IsSubmittingChanges after submitted changes");
                Assert.AreEqual(12, this._view.Count, "Step 3");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(740476)]
        [Description("Submitting changes with a removed entity will remove the entity from the page tracking")]
        public void SubmitChangesRemovesEntitiesFromPageTracking()
        {
            City removedCity = null;

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                Assert.AreEqual(11, this._view.Count, "Count should be 11 after the initial load");

                removedCity = (City)this._view[0];
                this._view.Remove(removedCity);

                Assert.AreEqual(10, this._view.Count, "There should only be 10 entities after removing the entity");

                City newFirstCity = (City)this._view[0];
                Assert.AreEqual(-1, removedCity.Name.CompareTo(newFirstCity.Name), "The new first city should be the next city, alphabetically ordered");
                Assert.IsFalse(this._view.Contains(removedCity), "The ECV should no longer contain the removed city");
                Assert.IsFalse(this._view.Cast<City>().Any(c => c.Name == removedCity.Name), "The ECV should no longer contain a city with the name of the removed city");

                this._asyncEventFailureMessage = "Submitting Changes";
                this._dds.SubmitChanges();
            });

            this.AssertSubmittingChanges();

            EnqueueCallback(() =>
            {
                this.ResetSubmitState();

                this._dds.Load();
            });


            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                // CitiesData is permanent data, so removing an entity doesn't really remove it, so
                // a reload will serve the removed entity back up, but it should be unmodified
                Assert.AreEqual(11, this._view.Count, "There should be 11 entities after submitting changes");
                Assert.IsFalse(this._view.Contains(removedCity), "The ECV should no longer contain the removed city after submitting changes, although it will contain a new entity with the same data");
                Assert.AreEqual(EntityState.Detached, removedCity.EntityState, "The EntityState of our removed entity should become Detached after it was deleted from the store");

                // With bug 740476, the removed city showed up in the list twice, because it was added back to the end of the list,
                // because the city we captured as the removedCity got changed to be a New entity
                Assert.AreEqual(1, this._view.Cast<City>().Count(c => c.Name == removedCity.Name), "The ECV should contain a single entity matching the removed city's name after submitting changes");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that submitting an empty changeset will result in a submitted event.")]
        public void SubmitEmptyChanges()
        {
            EnqueueCallback(() =>
            {
                this._dds.DomainContext = new CityDomainContext();
                this._dds.SubmitChanges();
                Assert.IsTrue(this._dds.IsBusy, "IsBusy while submitting changes");
                Assert.IsTrue(this._dds.IsSubmittingChanges, "IsSubmittingChanges");
            });

            this.AssertSubmittingChanges();

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(873535)]
        [Description("When there is a pending add transaction, SubmitChanges will commit the add implicitly before the SubmittingChanges event is raised.")]
        public void SubmitChangesWithPendingAdd()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                Assert.IsFalse(this._dds.HasChanges);
                City city = (City)this._editableCollectionView.AddNew();
                Assert.IsNotNull(city);
                city.StateName = "KY";
                city.CountyName = "Bourbon";
                city.Name = "Paris";

                this._dds.SubmittingChanges += (s, e) =>
                {
                    Assert.IsFalse(this._editableCollectionView.IsAddingNew, "IsAddingItem should be false when SubmittingChanges is raised, because the transaction should have been committed");
                    Assert.AreEqual<string>("Paris", city.Name, "The City Name should remain as 'Paris' because the changes should be committed");
                };

                this._dds.SubmitChanges();
            });

            this.AssertSubmittingChanges();

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(873535)]
        [Description("When there is a pending edit transaction on the DataView, SubmitChanges will commit the edit implicitly before the SubmittingChanges event is raised.")]
        public void SubmitChangesWithPendingEditOnDataView()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                Assert.IsFalse(this._dds.HasChanges);
                City city = this._view.GetItemAt(0) as City;
                Assert.IsNotNull(city);
                this._editableCollectionView.EditItem(city);
                int newZoneId = city.ZoneID + 1;
                city.ZoneID = newZoneId;

                this._dds.SubmittingChanges += (s, e) =>
                {
                    Assert.IsFalse(this._editableCollectionView.IsEditingItem, "IsEditingItem should be false when SubmittingChanges is raised, because the transaction should have been committed");
                    Assert.AreEqual<int>(newZoneId, city.ZoneID, "The ZoneID should retain the new value because the changes should be committed");
                };

                this._dds.SubmitChanges();
            });

            this.AssertSubmittingChanges();

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(873535)]
        [Description("When there is a pending edit transaction on the CurrentItem, SubmitChanges will commit the edit implicitly before the SubmittingChanges event is raised.")]
        public void SubmitChangesWithPendingEditOnCurrentItem()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                Assert.IsFalse(this._dds.HasChanges);
                City city = this._view.CurrentItem as City;
                Assert.IsNotNull(city);
                ((IEditableObject)city).BeginEdit();
                int newZoneId = city.ZoneID + 1;
                city.ZoneID = newZoneId;

                this._dds.SubmittingChanges += (s, e) =>
                {
                    Assert.AreEqual<int>(newZoneId, city.ZoneID, "The ZoneID should retain the new value because the changes should be committed");
                };

                this._dds.SubmitChanges();
            });

            this.AssertSubmittingChanges();

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the CancelSubmit method functions properly.")]
        public void CancelSubmit()
        {
            EnqueueCallback(() =>
            {
                this._ddsSubmittingChangesExpected = 2;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            // The submit that doesn't get cancelled won't actually work with this domain context
            this._dds.SubmittedChanges += (s, e) =>
            {
                if (!e.Cancelled)
                {
                    e.MarkErrorAsHandled();
                }
            };

            EnqueueCallback(() =>
            {
                Assert.IsFalse(this._dds.HasChanges, "HasChanges after first load");
                Assert.AreEqual(11, this._view.Count, "Step 1");
                City city = this._editableCollectionView.AddNew() as City;
                city.Name = "Renton";
                city.StateName = "WA";
                Assert.IsTrue(this._dds.HasChanges, "HasChanges after editing the city");
                Assert.AreEqual(12, this._view.Count, "Step 2");
                this._editableCollectionView.CommitNew();
                this._dds.SubmitChanges();
                Assert.IsTrue(this._dds.IsBusy, "IsBusy while submitting changes");
                Assert.IsTrue(this._dds.IsSubmittingChanges, "IsSubmittingChanges");
                this._dds.CancelSubmit();
                Assert.IsTrue(this._dds.HasChanges, "HasChanges after canceling the submit");
                Assert.IsFalse(this._dds.IsBusy, "IsBusy while submitting changes");
                Assert.IsFalse(this._dds.IsSubmittingChanges, "IsSubmittingChanges");
                this._dds.SubmitChanges();
                Assert.IsTrue(this._dds.IsBusy, "IsBusy while submitting changes");
                Assert.IsTrue(this._dds.IsSubmittingChanges, "IsSubmittingChanges");
            });

            this.AssertSubmittingChanges(2);

            EnqueueCallback(() =>
            {
                Assert.IsFalse(this._dds.HasChanges, "HasChanges after submitted changes");
                Assert.IsFalse(this._dds.IsBusy, "IsBusy after submitted changes");
                Assert.IsFalse(this._dds.IsSubmittingChanges, "IsSubmittingChanges after submitted changes");
                Assert.AreEqual(12, this._view.Count, "Step 3");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that a submit can be canceled in a SubmittingChanges event handler.")]
        public void CancelSubmittingChanges()
        {
            EnqueueCallback(() =>
            {
                this._ddsSubmittingChangesExpected = 2;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.IsFalse(this._dds.HasChanges, "HasChanges after first load");
                Assert.AreEqual(11, this._view.Count, "Step 1");
                City city = this._editableCollectionView.AddNew() as City;
                city.Name = "Renton";
                city.StateName = "WA";
                Assert.IsTrue(this._dds.HasChanges, "HasChanges after editing the city");
                Assert.AreEqual(12, this._view.Count, "Step 2");
                this._editableCollectionView.CommitNew();
                this._dds.SubmittingChanges += (sender, e) => e.Cancel = true;
                this._dds.SubmitChanges();
                Assert.IsTrue(this._dds.HasChanges, "HasChanges after canceling the submit");
                Assert.IsFalse(this._dds.IsBusy, "IsBusy while submitting changes");
                Assert.IsFalse(this._dds.IsSubmittingChanges, "IsSubmittingChanges");
            });

            EnqueueTestComplete();
        }

        #endregion Submitting

        #region Rejecting and Clearing

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the RejectChanges method functions properly.")]
        public void RejectChanges()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                Assert.AreEqual(11, this._view.Count, "Expected 11 items after the initial load.");

                City city = this._editableCollectionView.AddNew() as City;
                city.Name = "To Be Removed";
                city.StateName = "ST";
                this._editableCollectionView.CommitNew();
                Assert.AreEqual(12, this._view.Count, "Expected 12 items after committing the new item.");

                this._dds.RejectChanges();
                Assert.AreEqual(11, this._view.Count, "Expected 11 items after rejecting changes but before reloading.");

                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(11, this._view.Count, "Expected 11 items after rejecting changes and reloading.");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Rejecting changes reverts grouping back to its original state.")]
        public void RejectingRevertsGrouping()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.GroupDescriptors.Add(new GroupDescriptor("StateName"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                Assert.IsNotNull(this._collectionView.Groups);
                Assert.AreEqual(4, this._collectionView.Groups.Count);

                City bob = this._view[0] as City;
                this._editableCollectionView.EditItem(bob);
                bob.StateName = "BO";
                this._editableCollectionView.CommitEdit();

                Assert.AreEqual(5, this._collectionView.Groups.Count);

                this._dds.RejectChanges();
                Assert.AreEqual(4, this._collectionView.Groups.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(710859)]
        [Description("Rejecting changes with removed entity restores the entity in the correct sort position")]
        public void RejectChangesRestoresSortOrderOfRemovedEntity()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.Name), ListSortDirection.Ascending, "Initial load");

                // Remove an item from the middle of the list
                this._view.RemoveAt(2);
                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.Name), ListSortDirection.Ascending, "After Remove");

                this._dds.RejectChanges();
                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.Name), ListSortDirection.Ascending, "After RejectChanges");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Rejecting changes during either an Add or Edit transaction will cancel that transaction.")]
        public void RejectChangesCancelsTransaction()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                City city = this._editableCollectionView.AddNew() as City;
                Assert.IsTrue(this._editableCollectionView.IsAddingNew, "Should be adding a new item.");
                this._dds.RejectChanges();
                Assert.IsFalse(this._editableCollectionView.IsAddingNew, "Should no longer be adding a new item.");

                this._editableCollectionView.EditItem(this._view[0]);
                Assert.IsTrue(this._editableCollectionView.IsEditingItem, "Should be editing an item.");
                this._dds.RejectChanges();
                Assert.IsFalse(this._editableCollectionView.IsEditingItem, "Should no longer be editing an item.");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(740476)]
        [Description("After adding an entity, rejecting changes, and reloading the data, the once added entity is not added back")]
        public void RejectChangesWithAddedEntityReloadDoesNotAddEntityBack()
        {
            City addedCity = null;

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                Assert.AreEqual(11, this._view.Count, "Count should be 11 after the initial load");

                addedCity = this._editableCollectionView.AddNew() as City;
                addedCity.Name = "Added City";
                addedCity.StateName = "ST";
                this._editableCollectionView.CommitNew();

                Assert.AreEqual(12, this._view.Count, "The count of items should be 12 after adding the new city");
                Assert.AreEqual(11, this._view.IndexOf(addedCity), "The city should have the index of 11 after being added");

                this._dds.RejectChanges();

                Assert.AreEqual(11, this._view.Count, "Count should be 11 after rejecting changes");

                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                Assert.AreEqual(11, this._view.Count, "Count should be 11 after reloading the data after rejecting changes");
                Assert.IsFalse(this._view.Contains(addedCity), "The ECV should not contain the once added city");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(781815)]
        [Description("DDS should have a clear method that clears the underlying EntitySet and also discards page tracking")]
        public void ClearThroughDDS()
        {
            CityDomainContext context = new CityDomainContext();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.PageSize = 5;
                this._dds.Load();
            });

            this.AssertLoadingData();

            City loadedCity = null;

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                Assert.AreEqual<int>(this._dds.PageSize, this._view.Count, "this._view.Count after the load");
                Assert.AreEqual<int>(this._dds.PageSize, context.Cities.Count, "context.Cities.Count after the load");

                // Grab reference to a city that was loaded.  We'll use it later to ensure page tracking was discarded.
                loadedCity = (City)this._view[0];

                this._dds.Clear();
                Assert.AreEqual<int>(0, this._view.Count, "this._view.Count after Clear()");
                Assert.AreEqual<int>(0, context.Cities.Count, "context.Cities.Count after Clear()");

                // Add the loaded city back into the domain context, ensuring that page tracking doesn't restore it to the view
                context.Cities.Add(loadedCity);
                Assert.AreEqual<int>(0, this._view.Count, "this._view.Count after adding the loaded city back to the context");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(781815)]
        [Description("Clearing the EntitySet that backs the DDS should clear its view but retain page tracking")]
        public void ClearThroughEntitySet()
        {
            CityDomainContext context = new CityDomainContext();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.PageSize = 5;
                this._dds.Load();
            });

            this.AssertLoadingData();

            City loadedCity = null;

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                Assert.AreEqual<int>(this._dds.PageSize, this._view.Count, "this._view.Count after the load");
                Assert.AreEqual<int>(this._dds.PageSize, context.Cities.Count, "context.Cities.Count after the load");

                // Grab reference to a city that was loaded.  We'll use it later to ensure page tracking is kept intact.
                loadedCity = (City)this._view[0];

                context.Cities.Clear();
                Assert.AreEqual<int>(0, this._view.Count, "this._view.Count after Clear()");
                Assert.AreEqual<int>(0, context.Cities.Count, "context.Cities.Count after Clear()");

                // Add the loaded city back into the domain context, ensuring that page tracking adds it back to the view
                context.Cities.Add(loadedCity);
                Assert.AreEqual<int>(1, this._view.Count, "this._view.Count after adding the loaded city back to the context");
                Assert.IsTrue(this._view.Contains(loadedCity), "The view should contain the loaded city");
            });

            EnqueueTestComplete();
        }

        #endregion Rejecting and Clearing

        #region Combinations

        [TestMethod]
        [Asynchronous]
        [Description("Tests the SortDescriptors property and the FilterDescriptors property together.")]
        public void SortingAndFiltering()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.IsEqualTo, Value = "OH" });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
                this.ResetLoadState();
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "CountyName", Operator = FilterOperator.IsEqualTo, Value = "Lucas" });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors.RemoveAt(0);
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors.RemoveAt(0);
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(11, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests Sorting, Filtering, and Grouping together.")]
        public void SortingFilteringAndGrouping()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.IsEqualTo, Value = "WA" });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(6, this._view.Count);
                this.ResetLoadState();
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Descending));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual("Tacoma", (this._view[0] as City).Name);
                Assert.AreEqual("Redmond", (this._view[1] as City).Name);
                Assert.AreEqual("Everett", (this._view[2] as City).Name);
                Assert.AreEqual("Duvall", (this._view[3] as City).Name);
                Assert.AreEqual("Carnation", (this._view[4] as City).Name);
                Assert.AreEqual("Bellevue", (this._view[5] as City).Name);

                this.ResetLoadState();
                this._dds.SortDescriptors.Add(new SortDescriptor("CountyName", ListSortDirection.Descending));
                this._dds.GroupDescriptors.Add(new GroupDescriptor("CountyName"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                // Group - "Pierce County"
                Assert.AreEqual("Tacoma", (this._view[0] as City).Name);

                // Group - "King County"
                Assert.AreEqual("Redmond", (this._view[1] as City).Name);
                Assert.AreEqual("Everett", (this._view[2] as City).Name);
                Assert.AreEqual("Duvall", (this._view[3] as City).Name);
                Assert.AreEqual("Carnation", (this._view[4] as City).Name);
                Assert.AreEqual("Bellevue", (this._view[5] as City).Name);

                Assert.AreEqual(2, this._collectionView.Groups.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests Sorting and Grouping on the same property.")]
        public void SortingAndGroupingOnSameProperty()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(11, this._view.Count);
                this.ResetLoadState();
                this._dds.SortDescriptors.Add(new SortDescriptor("CountyName", ListSortDirection.Descending));
                this._dds.GroupDescriptors.Add(new GroupDescriptor("CountyName"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                // normally when we group it should sort in Ascending order, but since we have a sort
                // on the same property in Descending order, we should honor that request
                Assert.AreEqual("Santa Barbara", (this._view[0] as City).CountyName);
                Assert.AreEqual("Pierce", (this._view[1] as City).CountyName);
                Assert.AreEqual("Orange", (this._view[2] as City).CountyName);
                Assert.AreEqual("Lucas", (this._view[3] as City).CountyName);
                Assert.AreEqual("Lucas", (this._view[4] as City).CountyName);
                Assert.AreEqual("King", (this._view[5] as City).CountyName);
                Assert.AreEqual("King", (this._view[6] as City).CountyName);
                Assert.AreEqual("King", (this._view[7] as City).CountyName);
                Assert.AreEqual("King", (this._view[8] as City).CountyName);
                Assert.AreEqual("King", (this._view[9] as City).CountyName);
                Assert.AreEqual("Jackson", (this._view[10] as City).CountyName);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests the LoadSize property and the SortDescriptors property together.")]
        public void ProgressiveLoadWithSorting()
        {
            this._dds.LoadedData += new EventHandler<LoadedDataEventArgs>((sender, e) =>
            {
                AssertHelper.AssertSequenceSorting(e.AllEntities.Cast<City>().Select(c => c.Name), ListSortDirection.Descending, string.Empty);
            });

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.LoadInterval = defaultLoadInterval;
                this._dds.LoadSize = 5;
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Descending));
                this._dds.Load();
            });

            AssertLoadingData(3);

            EnqueueCallback(() =>
            {
                Assert.AreEqual(11, this._view.Count, "Sorted collection should contain 11 cities.");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests the LoadSize property and the FilterDescriptors property together.")]
        public void ProgressiveLoadWithFiltering()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.LoadInterval = defaultLoadInterval;
                this._dds.LoadSize = 5;
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.IsEqualTo, Value = "WA" });
                this._dds.Load();
            });

            AssertLoadingData(2);

            EnqueueCallback(() =>
            {
                Assert.AreEqual(6, this._view.Count, "Filtered collection should contain 6 cities.");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests the LoadSize property, the SortDescriptors property, and the FilterDescriptors property together.")]
        public void ProgressiveLoadWithSortingAndFiltering()
        {
            this._dds.LoadedData += new EventHandler<LoadedDataEventArgs>((sender, e) =>
            {
                AssertHelper.AssertSequenceSorting(e.AllEntities.Cast<City>().Select(c => c.Name), ListSortDirection.Descending, string.Empty);
            });

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.LoadInterval = defaultLoadInterval;
                this._dds.LoadSize = 5;
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Descending));
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.IsEqualTo, Value = "WA" });
                this._dds.Load();
            });

            AssertLoadingData(2);

            EnqueueCallback(() =>
            {
                Assert.AreEqual(6, this._view.Count, "Filtered collection should contain 6 cities.");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests the LoadSize property, the PageSize property, the SortDescriptors property, and the FilterDescriptors property together.")]
        public void PagingSortingAndFiltering()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.LoadSize = 7;
                this._dds.PageSize = 3;
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Descending));
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.IsEqualTo, Value = "WA" });
                this._dds.Load();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(0, this._view.PageIndex, "PageIndex should be 0 on the initial load");
                Assert.AreEqual(3, this._view.Count, "this._ecv.Count should be 3 on the initial load");
                Assert.AreEqual("Tacoma", (this._view[0] as City).Name, "Initial load");
                Assert.AreEqual("Redmond", (this._view[1] as City).Name, "Initial load");
                Assert.AreEqual("Everett", (this._view[2] as City).Name, "Initial load");

                // Move to 2nd page, which is already loaded
                this._ddsLoadingDataExpected = 0;
                this._view.MoveToNextPage();
            });

            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(1, this._view.PageIndex, "PageIndex should become 1");
                Assert.AreEqual(3, this._view.Count, "this._ecv.Count should be 3 on the 2nd page");
                Assert.AreEqual("Duvall", (this._view[0] as City).Name, "Move to page 2");
                Assert.AreEqual("Carnation", (this._view[1] as City).Name, "Move to page 2");
                Assert.AreEqual("Bellevue", (this._view[2] as City).Name, "Move to page 2");

                // Load 3rd page
                this._view.MoveToNextPage();
            });

            this.AssertLoadingData();
            // There's no more data, so the page will not change
            this.AssertNoPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(1, this._view.PageIndex, "PageIndex should remain 1");
                Assert.AreEqual(3, this._view.Count, "this._ecv.Count should still be 3");
                Assert.AreEqual("Duvall", (this._view[0] as City).Name, "Attempt tp move to page 3");
                Assert.AreEqual("Carnation", (this._view[1] as City).Name, "Attempt tp move to page 3");
                Assert.AreEqual("Bellevue", (this._view[2] as City).Name, "Attempt tp move to page 3");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Adding an item to the current page will increase the number of items on the page")]
        public void AddingToCurrentPageIncreasesCount()
        {
            CityDomainContext context = new CityDomainContext();

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.PageSize = 3;
                this._dds.Load();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            // Our submissions won't succeed with this domain context
            this._dds.SubmittedChanges += (s, e) =>
            {
                if (e.HasError)
                {
                    e.MarkErrorAsHandled();
                }
            };

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(3, this._view.Count, "3 items should have been loaded onto the page");
                Assert.AreEqual(3, context.Cities.Count, "context.Cities.Count should be 3 after the initial load");

                City newCity = this._editableCollectionView.AddNew() as City;
                newCity.Name = "West Chester";
                newCity.StateName = "OH";
                this._editableCollectionView.CommitNew();

                Assert.AreEqual(4, this._view.Count, "this._ecv.Count should be 4 after adding the new item");
                Assert.AreEqual(4, context.Cities.Count, "context.Cities.Count should be 4 after adding the new item");

                this._dds.SubmitChanges();
            });

            this.AssertSubmittingChanges();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();
                this.ResetSubmitState();

                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();
                this.ResetSubmitState();

                Assert.AreEqual(3, this._view.Count, "this._ecv.Count should return to 3 after submitting");
                Assert.AreEqual(4, context.Cities.Count, "context.Cities.Count should remain 4 after submitting");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Removing and restoring an item from another page doesn't affect the current page")]
        public void RemovingAndRestoringItemFromAnotherPage()
        {
            CityDomainContext context = new CityDomainContext();
            City city = null;

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.PageSize = 5;
                this._dds.Load();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                city = this._view[0] as City;

                this._view.MoveToNextPage();
            });

            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(5, this._view.Count, "this._ecv.Count should be 5 after loading the 2nd page");
                Assert.AreEqual(10, context.Cities.Count, "context.Cities.Count should be 10 after loading the 2nd page");

                context.Cities.Remove(city);
                Assert.AreEqual(5, this._view.Count, "this._ecv.Count should be 5 after removing the city, since it belongs to another page");
                Assert.AreEqual(9, context.Cities.Count, "context.Cities.Count should be 9 after removing the city");

                context.Cities.Add(city);
                Assert.AreEqual(5, this._view.Count, "this._ecv.Count should be 5 after restoring the removed city");
                Assert.AreEqual(10, context.Cities.Count, "context.Cities.Count should be 10 after restoring the removed city");

            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verifies that the correct events are fired during paging events when local paging is utilized")]
        public void PagingCollectionEventsWithLocalPaging()
        {
            CityDomainContext context = new CityDomainContext();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.PageSize = 3;
                this._dds.LoadSize = 11;

                this._asyncEventFailureMessage = "Initial Load";
                this.TrackCollectionChanged();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(0, this._view.PageIndex, "PageIndex should be 0 after initial load");
                // TODO: Change from 2 Reset events to 1 when bug 709185 is fixed and our workaround can be removed
                this.AssertCollectionChanged(0, 0, 2, "Initial load");

                this._ddsLoadedDataExpected = 0;

                this._asyncEventFailureMessage = "MoveToNextPage()";
                this._view.MoveToNextPage();
            });

            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(1, this._view.PageIndex, "PageIndex should be 1 on the 2nd page");
                // TODO: Change from 2 Reset events to 1 when bug 709185 is fixed and our workaround can be removed
                this.AssertCollectionChanged(0, 0, 2, "Move to 2nd page");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verifies that the correct events are fired during paging events when the server is hit on each page move")]
        public void PagingCollectionEventsWithServerPaging()
        {
            CityDomainContext context = new CityDomainContext();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.PageSize = 3;

                this.TrackCollectionChanged();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(0, this._view.PageIndex, "PageIndex should be 0 after initial load");
                // TODO: Change from 2 Reset events to 1 when bug 709185 is fixed and our workaround can be removed
                this.AssertCollectionChanged(0, 0, 2, "Initial load");

                this._view.MoveToNextPage();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(1, this._view.PageIndex, "PageIndex should be 1 on the 2nd page");
                // TODO: Change from 2 Reset events to 1 when bug 709185 is fixed and our workaround can be removed
                this.AssertCollectionChanged(0, 0, 2, "Move to 2nd page");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Currency is set to the first item on the new page during local paging")]
        public void CurrencySetAfterLocalPaging()
        {
            CityDomainContext context = new CityDomainContext();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = context;
                this._dds.PageSize = 3;
                this._dds.LoadSize = 11;

                this._asyncEventFailureMessage = "Initial Load";
                this._dds.Load();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreSame(context.Cities.First(), this._view.CurrentItem, "CurrentItem should match the context.Cities[0] after the initial load");
                Assert.AreEqual(0, this._view.CurrentPosition, "CurrentPosition should be 0 after the initial load");
                this._ddsLoadedDataExpected = 0;

                this._asyncEventFailureMessage = "MoveToNextPage()";
                this._view.MoveToNextPage();
            });

            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                City[] cities = context.Cities.ToArray();
                Assert.AreSame(cities[3], this._view.CurrentItem, "CurrentItem should match the context.Cities[3] after MoveToNextPage()");
                Assert.AreEqual(0, this._view.CurrentPosition, "CurrentPosition should be 0 after MoveToNextPage()");
            });

            EnqueueTestComplete();
        }

        #endregion Combinations
    }

    #region Helper Classes

    internal class ExceptionsODS : DomainContext
    {
        public ExceptionsODS(Uri serviceUri)
            : base(new WebDomainClient<object>(serviceUri))
        {
        }

        public void MissingAttribute(IQueryable<City> query, LoadBehavior loadBehavior, object userState)
        {
        }

        protected override EntityContainer CreateEntityContainer()
        {
            return new Cities.CityDomainContext.CityDomainContextEntityContainer();
        }

        public EntitySet<City> Cities
        {
            get
            {
                return null;
            }
        }

        public EntitySet<City> Cities2
        {
            get
            {
                return null;
            }
        }
    }

    internal class ExceptionsODS2 : DomainContext
    {
        public ExceptionsODS2(Uri serviceUri)
            : base(new WebDomainClient<object>(serviceUri))
        {
        }

        protected override EntityContainer CreateEntityContainer()
        {
            return new Cities.CityDomainContext.CityDomainContextEntityContainer();
        }
    }

    /// <summary>
    /// IValueConverter used by ControlParameter for testing of the Converter and ConverterParameter properties.
    /// </summary>
    internal class StateConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((targetType == null) ||
                ((targetType != typeof(string)) && (targetType != typeof(object))))
            {
                return "Unexpected targetType in StateConverter";
            }

            if (!(value is int))
            {
                return "Unexpected value in StateConverter";
            }

            string strParameter = parameter as string;
            if (!(parameter == null || strParameter == "FullStateName"))
            {
                return "Unexpected parameter in StateConverter";
            }

            int stateLength = (int)value;
            if (parameter == null)
            {
                switch (stateLength)
                {
                    case 1: // O
                        return "OR";
                    case 3: // Cal
                        return "CA";
                    case 4: // Ohio
                        return "OH";
                    case 5: // Washi
                        return "WA";
                    default:
                        return "NoState";
                }
            }
            else if (strParameter == "FullStateName")
            {
                switch (stateLength)
                {
                    case 6: // Oregon
                        return "OR";
                    case 10: // Washington
                        return "WA";
                    default:
                        return "NoState";
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    #endregion Helper Classes
}
