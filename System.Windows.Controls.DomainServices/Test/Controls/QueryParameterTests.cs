using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using Cities;
using DataTests.AdventureWorks.LTS;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices;
using TestDomainServices.LTS;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace System.Windows.Controls.DomainServices.Test
{
    /// <summary>
    /// Tests the QueryParameter aspects of the <see cref="DomainDataSource"/> feature.
    /// </summary>
    [TestClass]
    public class QueryParameterTests : DomainDataSourceTestBase
    {
        #region Query Parameter Tests

        [TestMethod]
        [Asynchronous]
        [Description("Tests that data is loaded correctly from the mock database with query parameters.")]
        public void LoadDataWithQueryParameters()
        {
            this.LoadDomainDataSourceControl();

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
                Assert.AreEqual(11, this._view.Count);
                this.ResetLoadState();
                this._dds.QueryName = "GetCitiesInStateQuery";
                this._dds.QueryParameters.Add(new Parameter { ParameterName = "state", Value = "WA" });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(6, this._view.Count);
                this.ResetLoadState();
                this._dds.QueryParameters[0].Value = "OR";
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(1, this._view.Count);
                this.ResetLoadState();
                this._dds.QueryParameters.RemoveAt(0);
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(11, this._view.Count);
                this.ResetLoadState();
                this._dds.AutoLoad = true;
                this._dds.QueryName = "GetCitiesInStateQuery";
                Assert.AreEqual(0, this._dds.QueryParameters.Count);
            });

            this.AssertNoLoadingData();

            EnqueueCallback(() =>
            {
                this._dds.QueryParameters.Add(new Parameter { ParameterName = "state", Value = "WA" });
                Assert.AreEqual(1, this._dds.QueryParameters.Count);
                Assert.AreEqual("GetCitiesInStateQuery", this._dds.QueryName);
            });

            this.AssertLoadingData();

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that data is loaded correctly from the mock database with query parameters set up with ControlParameters.")]
        public void LoadDataWithQueryParametersAndControlParameters()
        {
            this.LoadDomainDataSourceControl();
            this.LoadTextBoxControl();

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
                this._dds.QueryName = "GetCitiesInStateQuery";
                this.TextBoxValueProvider.SetValue("WA");
                this._dds.QueryParameters.Add(this.CreateValueBoundParameter("state", this._textBox, "Text"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(6, this._view.Count);
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("CA");
                Assert.AreEqual("CA", this._dds.QueryParameters[0].Value.ToString());
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
                this.ResetLoadState();
                this._dds.QueryParameters[0].Value = "OH";
                Assert.AreEqual("OH", this._dds.QueryParameters[0].Value.ToString());
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that data is loaded correctly from the mock database with a query ControlParameter set up with Converter/ConverterParameter properties.")]
        public void LoadDataWithQueryControlParameterAndConverter()
        {
            this.LoadDomainDataSourceControl();
            this.LoadTextBoxControl();

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
                this._dds.QueryName = "GetCitiesInStateQuery";

                this.TextBoxValueProvider.SetValue("Washi"); // Washi (5 characters) --> WA

                Parameter parameter = new Parameter() { ParameterName = "state" };
                this.CreateBinding(parameter, Parameter.ValueProperty, this._textBox, "Text.Length", new StateConverter(), null);
                this._dds.QueryParameters.Add(parameter);

                Assert.AreEqual("WA", this._dds.QueryParameters[0].Value);

                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(6, this._view.Count);
                this.ResetLoadState();

                this.TextBoxValueProvider.SetValue("Cal"); // Cal (3 characters) --> CA
                Assert.AreEqual("CA", this._dds.QueryParameters[0].Value);

                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
                this.ResetLoadState();

                this.TextBoxValueProvider.SetValue("Ohio"); // Ohio (4 characters) --> OH
                Assert.AreEqual("OH", this._dds.QueryParameters[0].Value);

                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
                this.ResetLoadState();

                this._dds.QueryParameters.Clear();
                Parameter parameter = new Parameter() { ParameterName = "state" };
                this.CreateBinding(parameter, Parameter.ValueProperty, this._textBox, "Text.Length", new StateConverter(), "FullStateName");
                this._dds.QueryParameters.Add(parameter);

                this.TextBoxValueProvider.SetValue("Washington"); // 10 characters --> WA
                Assert.AreEqual("WA", this._dds.QueryParameters[0].Value);

                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(6, this._view.Count);
                this.ResetLoadState();

                this.TextBoxValueProvider.SetValue("Oregon"); // 6 characters --> OR
                Assert.AreEqual("OR", this._dds.QueryParameters[0].Value);

                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(1, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(792200)]
        [WorkItem(809074)]
        [Description("Loading data using enum query parameter using an enum value")]
        public void LoadWithEnumParameterEnumValue()
        {
            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetStatesInShippingZone";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.QueryParameters.Add(new Parameter { ParameterName = "shippingZone", Value = ShippingZone.Eastern });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.IsTrue(this._view.Cast<State>().All(s => s.ShippingZone == ShippingZone.Eastern), "There shouldn't be any states outside of the Eastern shipping zone");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(792200)]
        [WorkItem(809074)]
        [Description("Loading data using enum query parameter using a numeric value")]
        public void LoadWithEnumParameterNumericValue()
        {
            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetStatesInShippingZone";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.QueryParameters.Add(new Parameter { ParameterName = "shippingZone", Value = (int)ShippingZone.Eastern });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.IsTrue(this._view.Cast<State>().All(s => s.ShippingZone == ShippingZone.Eastern), "There shouldn't be any states outside of the Eastern shipping zone");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(792200)]
        [WorkItem(809074)]
        [Description("Loading data using numeric query parameter using an enum value")]
        public void LoadWithNumericParameterEnumValue()
        {
            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetProductsByCategory";
                this._dds.DomainContext = new Catalog();
                this._dds.QueryParameters.Add(new Parameter { ParameterName = "subCategoryID", Value = ShippingZone.Eastern });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.IsTrue(this._view.Cast<Product>().All(p => p.ProductSubcategoryID == (int)ShippingZone.Eastern), "There should only be products with a ProductSubcategoryID of the integer value of ShippingZone.Eastern (2)");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(792200)]
        [WorkItem(809074)]
        [Description("Loading data using enum query parameter using an enum value from a different enum is expected to fail")]
        public void LoadWithEnumParameterDifferentEnumValueFails()
        {
            this.AssertExpectedLoadError(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetStatesInShippingZone";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.QueryParameters.Add(new Parameter { ParameterName = "shippingZone", Value = TimeZone.Eastern });
                this._dds.Load();
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests ControlParameter with Value that cannot be converted into target type.")]
        public void LoadDataWithInvalidControlParameterValueType()
        {
            this.LoadDomainDataSourceControl();
            this.LoadTextBoxControl();

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.FilterDescriptors.Add(this.CreateValueBoundFilterDescriptor("ZoneID", FilterOperator.IsEqualTo, this._textBox, "Text"));
            });

            this.AssertNoLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("0");
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("Not_A_Number");
            });

            this.AssertNoLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("0");
            });

            this.AssertLoadingData();

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(803699)] // Raise LoadedData event with the error instead of throwing exception from Load()
        [Description("Tests that validation exceptions that occur during a query are presented in a LoadedData event with the error using AutoLoad")]
        public void LoadDataWithValidationExceptionUsingAutoLoad()
        {
            this.LoadDomainDataSourceControl();

            // Add invalid parameters: [Range(0, 10)] int a, [StringLength(2)] string b
            Parameter a = new Parameter { ParameterName = "a", Value = "11" };
            Parameter b = new Parameter { ParameterName = "b", Value = "ABC" };

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = true;
                this._dds.DomainContext = new TestProvider_Scenarios();
                this._dds.QueryName = "QueryWithParamValidationQuery";

                this._dds.QueryParameters.Add(a);
            });

            this.AssertExpectedLoadError(() => this._dds.QueryParameters.Add(b));

            EnqueueCallback(() =>
            {
                Assert.IsInstanceOfType(this._ddsLoadedDataEventArgs.Error, typeof(ValidationException), "The exception within the LoadedData event with the error should be a ValidationException");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(803699)] // Raise LoadedData event with the error instead of throwing exception from Load()
        [Description("Tests that validation exceptions that occur during a query are presented in a LoadedData event with the error after calling Load")]
        public void LoadDataWithValidationExceptionUsingLoad()
        {
            this.LoadDomainDataSourceControl();

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.DomainContext = new TestProvider_Scenarios();
                this._dds.QueryName = "QueryWithParamValidationQuery";

                // Add invalid parameters: [Range(0, 10)] int a, [StringLength(2)] string b
                Parameter a = new Parameter { ParameterName = "a", Value = "11" };
                Parameter b = new Parameter { ParameterName = "b", Value = "ABC" };

                this._dds.QueryParameters.Add(a);
                this._dds.QueryParameters.Add(b);
            });

            this.AssertExpectedLoadError(() => this._dds.Load());

            EnqueueCallback(() =>
            {
                Assert.IsInstanceOfType(this._ddsLoadedDataEventArgs.Error, typeof(ValidationException), "The error within the LoadedData event should be a ValidationException");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(857038)]
        [Description("Tests that removing a query parameter from a valid query expression while using autoload will not cause a load.")]
        public void RemovingParameterFromValidQueryDoesNotLoad()
        {
            this.LoadDomainDataSourceControl();

            this.LoadCitiesInState("WA", 0, true);

            this.EnqueueCallback(() =>
            {
                this._dds.QueryParameters.RemoveAt(0);
            });

            this.AssertNoLoadingData();

            this.EnqueueTestComplete();
        }

        [TestMethod]
        [WorkItem(170441)]
        [Description("Tests that the supported operations are determined for a fully specified EntityQuery (by DomainContext, QueryName, and QueryParameter names).")]
        public void SupportedOperationsAreDeterminedForAFullySpecifiedEntityQuery()
        {
            Assert.IsFalse(this._dds.DataView.CanAdd,
                "CanAdd should be false before anything has been specified.");

            this._dds.DomainContext = new CityDomainContext();

            Assert.IsFalse(this._dds.DataView.CanAdd,
                "CanAdd should be false when only the DomainContext has been specified.");

            this._dds.QueryName = "GetCitiesInState";

            Assert.IsFalse(this._dds.DataView.CanAdd,
                "CanAdd should be false when only the DomainContext and QueryName have been specified.");

            this._dds.QueryParameters.Add(new Parameter { ParameterName = "state" });

            Assert.IsTrue(this._dds.DataView.CanAdd,
                "CanAdd should be true once the Query has been fully specified.");
        }

        #endregion Query Parameter Tests

        #region Paging Enabled

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When calling load after editing a query parameter from other than the first page, the first page is loaded and the data is filtered")]
        public void EditingQueryParameterAndThenLoadingLoadsFirstPage()
        {
            // PageSize of 1 to ensure there is more than 1 page for both WA and CA
            this.LoadCitiesInState("WA", 1, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.QueryParameters[0].Value = "CA";
                this._dds.Load();
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("CA", "After adding the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When calling refresh after editing a query parameter from other than the first page, the first page is loaded and the data is filtered")]
        public void EditingQueryParameterAndThenRefreshingLoadsFirstPage()
        {
            // PageSize of 1 to ensure there is more than 1 page for both WA and CA
            this.LoadCitiesInState("WA", 1, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.QueryParameters[0].Value = "CA";
                this._collectionView.Refresh();
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("CA", "After adding the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When editing a query parameter from other than the first page with AutoLoad set to true, the first page is loaded and the data is filtered")]
        public void EditingQueryParameterWithAutoLoadLoadsFirstPage()
        {
            this.LoadDomainDataSourceControl();
            // PageSize of 1 to ensure there is more than 1 page for both WA and CA
            this.LoadCitiesInState("WA", 1, true);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.QueryParameters[0].Value = "CA";
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("CA", "After adding the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When editing a query parameter from other than the first page within a defer load, the first page is loaded and the data is filtered")]
        public void EditingQueryParameterWithinDeferLoadLoadsFirstPage()
        {
            // PageSize of 1 to ensure there is more than 1 page for both WA and CA
            this.LoadCitiesInState("WA", 1, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._dds.DeferLoad())
                {
                    this._dds.QueryParameters[0].Value = "CA";
                }
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("CA", "After adding the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When editing a query parameter from other than the first page within a defer refresh, the first page is loaded and the data is filtered")]
        public void EditingQueryParameterWithinDeferRefreshLoadsFirstPage()
        {
            // PageSize of 1 to ensure there is more than 1 page for both WA and CA
            this.LoadCitiesInState("WA", 1, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._collectionView.DeferRefresh())
                {
                    this._dds.QueryParameters[0].Value = "CA";
                }
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("CA", "After adding the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When replacing load after editing a query parameter from other than the first page, the first page is loaded and the data is filtered")]
        public void ReplacingQueryParameterAndThenLoadingLoadsFirstPage()
        {
            // PageSize of 1 to ensure there is more than 1 page for both WA and CA
            this.LoadCitiesInState("WA", 1, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.QueryParameters.RemoveAt(0);
                this._dds.QueryParameters.Add(new Parameter { ParameterName = "state", Value = "CA" });
                this._dds.Load();
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("CA", "After adding the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When replacing refresh after editing a query parameter from other than the first page, the first page is loaded and the data is filtered")]
        public void ReplacingQueryParameterAndThenRefreshingLoadsFirstPage()
        {
            // PageSize of 1 to ensure there is more than 1 page for both WA and CA
            this.LoadCitiesInState("WA", 1, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.QueryParameters.RemoveAt(0);
                this._dds.QueryParameters.Add(new Parameter { ParameterName = "state", Value = "CA" });
                this._collectionView.Refresh();
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("CA", "After adding the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When repacing a query parameter from other than the first page with AutoLoad set to true, the first page is loaded and the data is filtered")]
        public void ReplacingQueryParameterWithAutoLoadLoadsFirstPage()
        {
            this.LoadDomainDataSourceControl();
            // PageSize of 1 to ensure there is more than 1 page for both WA and CA
            this.LoadCitiesInState("WA", 1, true);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.QueryParameters.RemoveAt(0);
                this._dds.QueryParameters.Add(new Parameter { ParameterName = "state", Value = "CA" });
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("CA", "After adding the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When replacing a query parameter from other than the first page within a defer load, the first page is loaded and the data is filtered")]
        public void ReplacingQueryParameterWithinDeferLoadLoadsFirstPage()
        {
            // PageSize of 1 to ensure there is more than 1 page for both WA and CA
            this.LoadCitiesInState("WA", 1, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._dds.DeferLoad())
                {
                    this._dds.QueryParameters.RemoveAt(0);
                    this._dds.QueryParameters.Add(new Parameter { ParameterName = "state", Value = "CA" });
                }
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("CA", "After adding the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When replacing a query parameter from other than the first page within a defer refresh, the first page is loaded and the data is filtered")]
        public void ReplacingQueryParameterWithinDeferRefreshLoadsFirstPage()
        {
            // PageSize of 1 to ensure there is more than 1 page for both WA and CA
            this.LoadCitiesInState("WA", 1, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._collectionView.DeferRefresh())
                {
                    this._dds.QueryParameters.RemoveAt(0);
                    this._dds.QueryParameters.Add(new Parameter { ParameterName = "state", Value = "CA" });
                }
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("CA", "After adding the filter");

            EnqueueTestComplete();
        }

        #endregion

        #region Progressive Loading Enabled

        [TestMethod]
        [Asynchronous]
        [WorkItem(812133)]
        [Description("Can edit a query parameter while progressive loading is enabled")]
        public void EditingQueryParameterWithProgressiveLoading()
        {
            IEnumerable<City> cities = new CityData().Cities;
            int citiesInWA = cities.Count(c => c.StateName == "WA");
            int citiesinOH = cities.Count(c => c.StateName == "OH");

            EnqueueCallback(() =>
            {
                // By using a loadsize of 1, we know the number of loads to be performed is equal to the
                // expected city count + 1 (the load that will determine that there are no more records).
                this._dds.LoadSize = 1;
                this._dds.QueryName = "GetCitiesInState";
                this._dds.QueryParameters.Add(new Parameter { ParameterName = "state", Value = "WA" });
                this._dds.DomainContext = new CityDomainContext();
                this._asyncEventFailureMessage = "First Progressive Load";
                this._dds.Load();
            });

            this.AssertLoadingData(citiesInWA + 1, true);

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                Assert.AreEqual<int>(citiesInWA, this._view.Count, "The count should match the number of WA cities after the first progressive load");
                Assert.IsTrue(this._view.Cast<City>().All(c => c.StateName == "WA"), "All cities should be in WA after the first progressive load");

                this._dds.QueryParameters[0].Value = "OH";

                // Calling Refresh will test that the load of FirstItems is deferred properly
                this._collectionView.Refresh();
            });

            this.AssertLoadingData(citiesinOH + 1, true);

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                Assert.AreEqual<int>(citiesinOH, this._view.Count, "The count should match the number of OH cities after the second progressive load");
                Assert.IsTrue(this._view.Cast<City>().All(c => c.StateName == "OH"), "All cities should be in OH after the second progressive load");
            });

            EnqueueTestComplete();
        }

        #endregion Progressive Loading Enabled

        #region Helper Methods

        /// <summary>
        /// Enqueue the necessary calls to load the cities in the specified state,
        /// and with the specified page size and auto load properties.
        /// </summary>
        /// <param name="state">The state to use as the query parameter value.</param>
        /// <param name="pageSize">The <see cref="DomainDataSource.PageSize"/> to use.</param>
        /// <param name="autoLoad">What to set <see cref="DomainDataSource.AutoLoad"/> to.</param>
        private void LoadCitiesInState(string state, int pageSize, bool autoLoad)
        {
            Parameter stateParameter = new Parameter { ParameterName = "state", Value = state };
            this.LoadCitiesInState(stateParameter, pageSize, autoLoad, true /* expectSuccessfulLoad */);
        }

        /// <summary>
        /// Enqueue the necessary calls to attempt to load cities using the <paramref name="stateParameter"/>
        /// specified.  If <paramref name="expectSuccessfulLoad"/> is <c>true</c>, then this will enqueue
        /// the calls to expect the successful load, otherwise, it will expect a load error.
        /// </summary>
        /// <param name="stateParameter">The <see cref="Parameter"/> to use for GetCitiesInState.</param>
        /// <param name="pageSize">The page size to use on the DDS.  When greater than 0, a PageChanged event is expected.</param>
        /// <param name="autoLoad">Whether or not to auto load the DDS.</param>
        /// <param name="expectSuccessfulLoad">Whether to expect a successful load or a load error.</param>
        private void LoadCitiesInState(Parameter stateParameter, int pageSize, bool autoLoad, bool expectSuccessfulLoad)
        {
            // Enqueue the load
            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = autoLoad;
                this._dds.QueryName = "GetCitiesInState";
                this._dds.QueryParameters.Add(stateParameter);
                this._dds.DomainContext = new CityDomainContext();
                this._dds.PageSize = pageSize;
            });

            if (expectSuccessfulLoad)
            {
                EnqueueCallback(() =>
                {
                    this._asyncEventFailureMessage = "LoadCitiesInState: Waiting for LoadingData event";

                    if (!autoLoad)
                    {
                        this._dds.Load();
                    }
                });

                this.AssertLoadingData(false);

                EnqueueCallback(() => this._asyncEventFailureMessage = "LoadCitiesInState: Waiting for LoadedData event");
                this.AssertLoadedData();

                if (pageSize > 0)
                {
                    EnqueueCallback(() => this._asyncEventFailureMessage = "LoadCitiesInState: Waiting for PageChanged event");
                    this.AssertPageChanged();
                }
            }
            else
            {
                this.AssertExpectedLoadError(() =>
                {
                    this._asyncEventFailureMessage = "LoadCitiesInState: Waiting for LoadingData/LoadedData events";
                    this._dds.Load();
                });
            }

            EnqueueCallback(() =>
            {
                this._asyncEventFailureMessage = null;
                this.ResetLoadState();
                this.ResetPageChanged();
            });
        }

        /// <summary>
        /// Assert that the first page is loaded as the result of a previously enqueued callback,
        /// and that the view only contains cities with the specified state name.
        /// </summary>
        /// <param name="stateName">The state name expected for all cities in the view.</param>
        /// <param name="message">The message to include in the assert.</param>
        private void AssertFirstPageLoadedAndFilteredByStateName(string stateName, string message)
        {
            this.AssertFirstPageLoaded(message);

            EnqueueCallback(() =>
            {
                Assert.AreEqual<int>(0, this._view.Cast<City>().Count(c => c.StateName != stateName), string.Format("Not all cities had the state name '{0}'. {1}", stateName, message));
            });
        }

        #endregion
    }
}
