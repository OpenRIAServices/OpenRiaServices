using System.ComponentModel;
using System.Linq;
using Cities;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace System.Windows.Controls.DomainServices.Test
{
    /// <summary>
    /// Tests the Grouping aspects of the <see cref="DomainDataSource"/> feature.
    /// </summary>
    [TestClass]
    public class SortingTests : DomainDataSourceTestBase
    {
        #region Sorting

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the SortDescriptors property and the SortCollection property of DomainDataSource.Data function properly.")]
        public void Sorting()
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
                this._dds.SortDescriptors.Add(new SortDescriptor("CountyName", ListSortDirection.Ascending));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual("Ashland", (this._view[0] as City).Name);
                Assert.AreEqual("Redmond", (this._view[1] as City).Name);
                Assert.AreEqual("Bellevue", (this._view[2] as City).Name);
                this.ResetLoadState();
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual("Ashland", (this._view[0] as City).Name);
                Assert.AreEqual("Bellevue", (this._view[1] as City).Name);
                Assert.AreEqual("Carnation", (this._view[2] as City).Name);
                this.ResetLoadState();
                this._dds.SortDescriptors[0].Direction = ListSortDirection.Descending;
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual("Santa Barbara", (this._view[0] as City).Name);
                Assert.AreEqual("Tacoma", (this._view[1] as City).Name);
                Assert.AreEqual("Orange", (this._view[2] as City).Name);
                this.ResetLoadState();
                this._collectionView.SortDescriptions.RemoveAt(0);
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual("Ashland", (this._view[0] as City).Name);
                Assert.AreEqual("Bellevue", (this._view[1] as City).Name);
                Assert.AreEqual("Carnation", (this._view[2] as City).Name);
                this.ResetLoadState();
                this._dds.SortDescriptors[0].Direction = ListSortDirection.Descending;
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual("Toledo", (this._view[0] as City).Name);
                Assert.AreEqual("Tacoma", (this._view[1] as City).Name);
                Assert.AreEqual("Santa Barbara", (this._view[2] as City).Name);
                this.ResetLoadState();
                this._collectionView.SortDescriptions.RemoveAt(0);
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual("Redmond", (this._view[0] as City).Name);
                Assert.AreEqual("Bellevue", (this._view[1] as City).Name);
                Assert.AreEqual("Duvall", (this._view[2] as City).Name);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests a SortDescriptor with a dotted property path.")]
        public void SortingWithDottedPropertyPath()
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
                this._dds.SortDescriptors.Add(new SortDescriptor("County.State.Name", ListSortDirection.Ascending));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual("Santa Barbara", (this._view[0] as City).Name);
                Assert.AreEqual("Orange", (this._view[1] as City).Name);
                Assert.AreEqual("Oregon", (this._view[2] as City).Name);
                Assert.AreEqual("Toledo", (this._view[3] as City).Name);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests a SortDescriptor with an invalid dotted property path.")]
        public void SortingWithInvalidDottedPropertyPath()
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
                this._dds.SortDescriptors.Add(new SortDescriptor("County.Invalid.Name", ListSortDirection.Ascending));
            });

            string expectedMessage = string.Format(
                DomainDataSourceResources.CannotEvaluateDescriptor, "SortDescriptor", "County.Invalid.Name");
            this.AssertExpectedLoadError(typeof(InvalidOperationException), expectedMessage, () => this._dds.Load());

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(733246)]
        [Description("Replacing a SortDescription should re-sort the data correctly")]
        public void SortingReplaceSortDescription()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._collectionView.SortDescriptions.Add(new System.ComponentModel.SortDescription("Name", System.ComponentModel.ListSortDirection.Ascending));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.Name), ListSortDirection.Ascending, "Initial load");

                this._collectionView.SortDescriptions[0] = new System.ComponentModel.SortDescription("Name", System.ComponentModel.ListSortDirection.Descending);
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.Name), ListSortDirection.Descending, "After sorting descending");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(733246)]
        [Description("Replacing a SortDescriptor should re-sort the data correctly")]
        public void SortingReplaceSortDescriptor()
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

                this._dds.SortDescriptors[0] = new SortDescriptor("Name", ListSortDirection.Descending);
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.Name), ListSortDirection.Descending, "After sorting descending");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the SortDescriptors property functions properly with ControlParameters.")]
        public void SortingWithControlParameters()
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
                this.TextBoxValueProvider.SetValue("CountyName");
                this._dds.SortDescriptors.Add(this.CreatePathBoundSortDescriptor(ListSortDirection.Ascending, this._textBox, "Text"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual("Ashland", (this._view[0] as City).Name);
                Assert.AreEqual("Redmond", (this._view[1] as City).Name);
                Assert.AreEqual("Bellevue", (this._view[2] as City).Name);
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("Name");
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual("Ashland", (this._view[0] as City).Name);
                Assert.AreEqual("Bellevue", (this._view[1] as City).Name);
                Assert.AreEqual("Carnation", (this._view[2] as City).Name);
                this.ResetLoadState();
                this._dds.SortDescriptors[0].Direction = ListSortDirection.Descending;
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual("Toledo", (this._view[0] as City).Name);
                Assert.AreEqual("Tacoma", (this._view[1] as City).Name);
                Assert.AreEqual("Santa Barbara", (this._view[2] as City).Name);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests a SortDescriptor with a ControlParameter that uses a dotted property path.")]
        public void SortingWithControlParameterAndDottedPropertyPath()
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
                this.TextBoxValueProvider.SetValue("County.State.Name");
                this._dds.SortDescriptors.Add(this.CreatePathBoundSortDescriptor(ListSortDirection.Ascending, this._textBox, "Text"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual("Santa Barbara", (this._view[0] as City).Name);
                Assert.AreEqual("Orange", (this._view[1] as City).Name);
                Assert.AreEqual("Oregon", (this._view[2] as City).Name);
                Assert.AreEqual("Toledo", (this._view[3] as City).Name);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests a SortDescriptor with a ControlParameter that uses a dotted control property path.")]
        public void SortingWithControlParameterAndDottedControlPropertyPath()
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
            this.LoadComboBoxControl();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._comboBox.Items.Add(new DataTypeTestClass { StringP = "CountyName" });
                this._comboBox.Items.Add(new DataTypeTestClass { StringP = "Invalid" });
                this._comboBox.SelectedIndex = 0;
                this._dds.SortDescriptors.Add(this.CreatePathBoundSortDescriptor(ListSortDirection.Ascending, this._comboBox, "SelectedItem.StringP"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual("Ashland", (this._view[0] as City).Name);
                Assert.AreEqual("Redmond", (this._view[1] as City).Name);
                Assert.AreEqual("Bellevue", (this._view[2] as City).Name);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(803699)] // Raise LoadedData event with the error instead of throwing exception from Load()
        [Description("Tests SortDescriptor with invalid PropertyPath.Value.")]
        public void SortDescriptorWithInvalidPropertyPathValue()
        {
            string expectedAutoLoadMessage = string.Format(
                System.Windows.Common.CommonResources.PropertyNotFound, "BadValue", typeof(City).Name);
            string expectedLoadMessage = string.Format(
                DomainDataSourceResources.CannotEvaluateDescriptor, "SortDescriptor", "BadValue");

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();

                this._dds.AutoLoad = true;
                AssertExpectedException(new ArgumentException(expectedAutoLoadMessage), () =>
                {
                    this._dds.SortDescriptors.Add(new SortDescriptor("BadValue", ListSortDirection.Ascending));
                });
                this._dds.AutoLoad = false;
            });

            this.AssertExpectedLoadError(typeof(InvalidOperationException), expectedLoadMessage, () => this._dds.Load());

            EnqueueTestComplete();
        }

        [TestMethod]
        [Description("Tests that the SortDescriptors property is in sync with the SortDescriptions property of DomainDataSource.Data")]
        public void SortAndSortCollectionInSync()
        {
            this._dds.QueryName = "GetCitiesQuery";
            this._dds.DomainContext = new CityDomainContext();

            this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
            Assert.AreEqual(1, this._collectionView.SortDescriptions.Count);
            Assert.AreEqual("Name", this._collectionView.SortDescriptions[0].PropertyName);
            Assert.AreEqual(System.ComponentModel.ListSortDirection.Ascending, this._collectionView.SortDescriptions[0].Direction);

            this._collectionView.SortDescriptions.Add(new System.ComponentModel.SortDescription("CountyName", System.ComponentModel.ListSortDirection.Descending));
            Assert.AreEqual(2, this._dds.SortDescriptors.Count);
            Assert.AreEqual("CountyName", this._dds.SortDescriptors[1].PropertyPath);
            Assert.AreEqual(ListSortDirection.Descending, this._dds.SortDescriptors[1].Direction);

            this._collectionView.SortDescriptions.RemoveAt(0);
            Assert.AreEqual(1, this._dds.SortDescriptors.Count);
            Assert.AreEqual("CountyName", this._dds.SortDescriptors[0].PropertyPath);
            Assert.AreEqual(ListSortDirection.Descending, this._dds.SortDescriptors[0].Direction);

            this._dds.SortDescriptors.RemoveAt(0);
            Assert.AreEqual(0, this._collectionView.SortDescriptions.Count);
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(845238)]
        [Description("Tests that a change to the SortDescriptions is not applied locally and causes a reload.")]
        public void SortIsNotAppliedUntilAutoloadCompletes()
        {
            CityData cities = new CityData();

            this.LoadDomainDataSourceControl();

            this.LoadCities(0, true);

            this.EnqueueCallback(() =>
            {
                Assert.IsTrue(cities.Cities.Select(c => c.Name).SequenceEqual(this._view.Cast<City>().Select(c => c.Name)),
                    "The view order should be equal to the Cities order.");

                this._collectionView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

                Assert.IsTrue(cities.Cities.Select(c => c.Name).SequenceEqual(this._view.Cast<City>().Select(c => c.Name)),
                    "The view order should still be equal to the Cities order.");
            });

            this.AssertLoadedData();

            this.EnqueueCallback(() =>
            {
                this.ResetLoadState();

                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.Name), ListSortDirection.Ascending,
                    "The view order should now be sorted in the ascending direction.");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(170444)]
        [Description("Tests that editing the sorted property on an entity will move it to the first and last positions in the view.")]
        public void EditingMovesItemToFirstAndLastPositions()
        {
            CityData cities = new CityData();

            this.LoadDomainDataSourceControl();
            this.LoadSortedCities("Name", 0, true);

            this.EnqueueCallback(() =>
            {
                this.ResetLoadState();

                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.Name), ListSortDirection.Ascending,
                    "The view order should now be sorted in the ascending direction.");

                City city = (City)this._view[3];
                this._editableCollectionView.EditItem(city);
                city.Name = "AAA" + this._view.Cast<City>().First().Name;
                this._editableCollectionView.CommitEdit();

                Assert.AreEqual(this._view.Cast<City>().First(), city,
                    "The city should have moved to the first position in the view.");

                this._editableCollectionView.EditItem(city);
                city.Name = this._view.Cast<City>().Last().Name + "ZZZ";
                this._editableCollectionView.CommitEdit();

                Assert.AreEqual(this._view.Cast<City>().Last(), city,
                    "The city should have moved to the last position in the view.");

                // Test the second to last item
                city = (City)this._view[this._view.Count - 2];
                this._editableCollectionView.EditItem(city);
                city.Name = this._view.Cast<City>().Last().Name + "ZZZ";
                this._editableCollectionView.CommitEdit();

                Assert.AreEqual(this._view.Cast<City>().Last(), city,
                    "The second-to-last city should have moved to the last position in the view.");
            });

            EnqueueTestComplete();
        }

        #endregion Sorting

        #region Currency Management

        [TestMethod]
        [Asynchronous]
        [Description("Ensure that MoveCurrentToFirst uses correct indexes when sorted Ascending")]
        public void SortAscendingMoveCurrentToFirst()
        {
            this.LoadSortedCities("Name", ListSortDirection.Ascending, 5, false);

            EnqueueCallback(() =>
            {
                this._view.MoveCurrentToFirst();

                City firstCity = this._view.Cast<City>().First();
                City currentCity = (City)this._view.CurrentItem;

                Assert.AreEqual<int>(0, this._view.CurrentPosition, "CurrentPosition");
                Assert.AreSame(firstCity, currentCity, "CurrentItem should match the first item in the view");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Ensure that MoveCurrentToLast uses correct indexes when sorted Ascending")]
        public void SortAscendingMoveCurrentToLast()
        {
            this.LoadSortedCities("Name", ListSortDirection.Ascending, 5, false);

            EnqueueCallback(() =>
            {
                this._view.MoveCurrentToLast();

                City lastCity = this._view.Cast<City>().Last();
                City currentCity = (City)this._view.CurrentItem;

                Assert.AreEqual<int>(4, this._view.CurrentPosition, "CurrentPosition");
                Assert.AreSame(lastCity, currentCity, "CurrentItem should match the last item in the view");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Ensure that MoveCurrentToPrevious uses correct indexes when sorted Ascending")]
        public void SortAscendingMoveCurrentToPrevious()
        {
            this.LoadSortedCities("Name", ListSortDirection.Ascending, 5, false);

            EnqueueCallback(() =>
            {
                this._view.MoveCurrentToLast();
                this._view.MoveCurrentToPrevious();

                City previousCity = this._view.Cast<City>().Take(4).Last();
                City currentCity = (City)this._view.CurrentItem;

                Assert.AreEqual<int>(3, this._view.CurrentPosition, "CurrentPosition");
                Assert.AreSame(previousCity, currentCity, "CurrentItem should match the second to last item in the view");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Ensure that MoveCurrentToNext uses correct indexes when sorted Ascending")]
        public void SortAscendingMoveCurrentToNext()
        {
            this.LoadSortedCities("Name", ListSortDirection.Ascending, 5, false);

            EnqueueCallback(() =>
            {
                this._view.MoveCurrentToNext();

                City nextCity = this._view.Cast<City>().Skip(1).First();
                City currentCity = (City)this._view.CurrentItem;

                Assert.AreEqual<int>(1, this._view.CurrentPosition, "CurrentPosition");
                Assert.AreSame(nextCity, currentCity, "CurrentItem should match the second item in the view");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Ensure that MoveCurrentToPosition uses correct indexes when sorted Ascending")]
        public void SortAscendingMoveCurrentToPosition()
        {
            this.LoadSortedCities("Name", ListSortDirection.Ascending, 5, false);

            EnqueueCallback(() =>
            {
                this._view.MoveCurrentToPosition(2);

                City middleCity = this._view.Cast<City>().Skip(2).First();
                City currentCity = (City)this._view.CurrentItem;

                Assert.AreEqual<int>(2, this._view.CurrentPosition, "CurrentPosition");
                Assert.AreSame(middleCity, currentCity, "CurrentItem should match the third item in the view");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Ensure that MoveCurrentTo uses correct indexes when sorted Ascending")]
        public void SortAscendingMoveCurrentTo()
        {
            this.LoadSortedCities("Name", ListSortDirection.Ascending, 5, false);

            EnqueueCallback(() =>
            {
                City middleCity = this._view.Cast<City>().Skip(2).First();
                this._view.MoveCurrentTo(middleCity);

                City currentCity = (City)this._view.CurrentItem;

                Assert.AreEqual<int>(2, this._view.CurrentPosition, "CurrentPosition");
                Assert.AreSame(middleCity, currentCity, "CurrentItem should match the third item in the view");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Ensure that MoveCurrentToFirst uses correct indexes when sorted Descending")]
        public void SortDescendingMoveCurrentToFirst()
        {
            this.LoadSortedCities("Name", ListSortDirection.Descending, 5, false);

            EnqueueCallback(() =>
            {
                this._view.MoveCurrentToFirst();

                City firstCity = this._view.Cast<City>().First();
                City currentCity = (City)this._view.CurrentItem;

                Assert.AreEqual<int>(0, this._view.CurrentPosition, "CurrentPosition");
                Assert.AreSame(firstCity, currentCity, "CurrentItem should match the first item in the view");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Ensure that MoveCurrentToLast uses correct indexes when sorted Descending")]
        public void SortDescendingMoveCurrentToLast()
        {
            this.LoadSortedCities("Name", ListSortDirection.Descending, 5, false);

            EnqueueCallback(() =>
            {
                this._view.MoveCurrentToLast();

                City lastCity = this._view.Cast<City>().Last();
                City currentCity = (City)this._view.CurrentItem;

                Assert.AreEqual<int>(4, this._view.CurrentPosition, "CurrentPosition");
                Assert.AreSame(lastCity, currentCity, "CurrentItem should match the last item in the view");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Ensure that MoveCurrentToPrevious uses correct indexes when sorted Descending")]
        public void SortDescendingMoveCurrentToPrevious()
        {
            this.LoadSortedCities("Name", ListSortDirection.Descending, 5, false);

            EnqueueCallback(() =>
            {
                this._view.MoveCurrentToLast();
                this._view.MoveCurrentToPrevious();

                City previousCity = this._view.Cast<City>().Take(4).Last();
                City currentCity = (City)this._view.CurrentItem;

                Assert.AreEqual<int>(3, this._view.CurrentPosition, "CurrentPosition");
                Assert.AreSame(previousCity, currentCity, "CurrentItem should match the second to last item in the view");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Ensure that MoveCurrentToNext uses correct indexes when sorted Descending")]
        public void SortDescendingMoveCurrentToNext()
        {
            this.LoadSortedCities("Name", ListSortDirection.Descending, 5, false);

            EnqueueCallback(() =>
            {
                this._view.MoveCurrentToNext();

                City nextCity = this._view.Cast<City>().Skip(1).First();
                City currentCity = (City)this._view.CurrentItem;

                Assert.AreEqual<int>(1, this._view.CurrentPosition, "CurrentPosition");
                Assert.AreSame(nextCity, currentCity, "CurrentItem should match the second item in the view");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Ensure that MoveCurrentToPosition uses correct indexes when sorted Descending")]
        public void SortDescendingMoveCurrentToPosition()
        {
            this.LoadSortedCities("Name", ListSortDirection.Descending, 5, false);

            EnqueueCallback(() =>
            {
                this._view.MoveCurrentToPosition(2);

                City middleCity = this._view.Cast<City>().Skip(2).First();
                City currentCity = (City)this._view.CurrentItem;

                Assert.AreEqual<int>(2, this._view.CurrentPosition, "CurrentPosition");
                Assert.AreSame(middleCity, currentCity, "CurrentItem should match the third item in the view");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Ensure that MoveCurrentTo uses correct indexes when sorted Descending")]
        public void SortDescendingMoveCurrentTo()
        {
            this.LoadSortedCities("Name", ListSortDirection.Descending, 5, false);

            EnqueueCallback(() =>
            {
                City middleCity = this._view.Cast<City>().Skip(2).First();
                this._view.MoveCurrentTo(middleCity);

                City currentCity = (City)this._view.CurrentItem;

                Assert.AreEqual<int>(2, this._view.CurrentPosition, "CurrentPosition");
                Assert.AreSame(middleCity, currentCity, "CurrentItem should match the third item in the view");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(766862), WorkItem(862184), WorkItem(864171)]
        [Description("Ensure that AddNew/CommitNew maintains the added item as the current item/position")]
        public void AddedItemIsCurrentItem()
        {
            this.LoadSortedCities("Name", ListSortDirection.Ascending, 10, false);

            EnqueueCallback(() =>
            {
                City addedItem = this._editableCollectionView.AddNew() as City;
                Assert.AreSame(addedItem, this._view.CurrentItem, "CurrentItem after AddNew()");
                Assert.AreEqual(this._view.IndexOf(addedItem), this._view.CurrentPosition, "IndexOf/CurrentPosition after AddNew()");

                addedItem.StateName = "AA";
                addedItem.Name = "AAA";
                addedItem.CountyName = "AAAA";

                this._editableCollectionView.CommitNew();
                Assert.AreSame(addedItem, this._view.CurrentItem, "CurrentItem after CommitNew()");
                Assert.AreEqual(this._view.IndexOf(addedItem), this._view.CurrentPosition, "IndexOf/CurrentPosition after CommitNew()");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(766862), WorkItem(862184), WorkItem(864171)]
        [Description("Ensure that EditItem/CommitEdit maintains the edited item as the current item/position")]
        public void EditedItemIsCurrentItem()
        {
            this.LoadSortedCities("Name", ListSortDirection.Ascending, 10, false);

            EnqueueCallback(() =>
            {
                City editedItem = this._view.GetItemAt(4) as City;
                this._view.MoveCurrentTo(editedItem);
                this._editableCollectionView.EditItem(editedItem);
                Assert.AreSame(editedItem, this._view.CurrentItem, "CurrentItem after EditItem()");
                Assert.AreEqual(this._view.IndexOf(editedItem), this._view.CurrentPosition, "IndexOf/CurrentPosition after EditItem()");

                editedItem.ZoneID += 1;

                this._editableCollectionView.CommitEdit();
                Assert.AreSame(editedItem, this._view.CurrentItem, "CurrentItem after CommitEdit()");
                Assert.AreEqual(this._view.IndexOf(editedItem), this._view.CurrentPosition, "IndexOf/CurrentPosition after CommitEdit()");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(766862), WorkItem(862184), WorkItem(864171)]
        [Description("Removing the only item in the collection results in a null CurrentItem")]
        public void RemoveOnlyItemClearsCurrency()
        {
            this.LoadSortedCities("Name", ListSortDirection.Ascending, 1, false);

            EnqueueCallback(() =>
            {
                City city = this._view.GetItemAt(0) as City;
                Assert.AreSame(city, this._view.CurrentItem, "CurrentItem before Remove");
                Assert.AreEqual<int>(0, this._view.CurrentPosition, "CurrentPosition before Remove");

                this._view.Remove(city);
                Assert.IsNull(this._view.CurrentItem, "CurrentItem after Remove");
                Assert.AreEqual<int>(-1, this._view.CurrentPosition, "CurrentPosition after Remove");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(766862), WorkItem(862184), WorkItem(864171)]
        [Description("Removing an item before the CurrentItem adjusts CurrentPosition")]
        public void RemoveItemBeforeCurrentAdjustsPosition()
        {
            this.LoadSortedCities("Name", ListSortDirection.Ascending, 10, false);

            EnqueueCallback(() =>
            {
                City first = this._view.GetItemAt(0) as City;
                City second = this._view.GetItemAt(1) as City;
                this._view.MoveCurrentTo(second);
                Assert.AreSame(second, this._view.CurrentItem, "CurrentItem before Remove");
                Assert.AreEqual<int>(1, this._view.CurrentPosition, "CurrentPosition before Remove");

                this._view.Remove(first);
                Assert.AreSame(second, this._view.CurrentItem, "CurrentItem after Remove");
                Assert.AreEqual<int>(0, this._view.CurrentPosition, "CurrentPosition after Remove");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(766862), WorkItem(862184), WorkItem(864171)]
        [Description("Removing the CurrentItem will retain CurrentPosition if there are items after the current item")]
        public void RemoveCurrentItemRetainsPosition()
        {
            this.LoadSortedCities("Name", ListSortDirection.Ascending, 10, false);

            EnqueueCallback(() =>
            {
                City first = this._view.GetItemAt(0) as City;
                City second = this._view.GetItemAt(1) as City;
                Assert.AreSame(first, this._view.CurrentItem, "CurrentItem before Remove");
                Assert.AreEqual<int>(0, this._view.CurrentPosition, "CurrentPosition before Remove");

                this._view.Remove(first);
                Assert.AreSame(second, this._view.CurrentItem, "CurrentItem after Remove");
                Assert.AreEqual<int>(0, this._view.CurrentPosition, "CurrentPosition after Remove");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(766862), WorkItem(862184), WorkItem(864171)]
        [Description("Removing the CurrentItem when it's the last item will set currency to the new last item")]
        public void RemoveCurrentItemAsLastItem()
        {
            this.LoadSortedCities("Name", ListSortDirection.Ascending, 2, false);

            EnqueueCallback(() =>
            {
                City first = this._view.GetItemAt(0) as City;
                City second = this._view.GetItemAt(1) as City;
                this._view.MoveCurrentTo(second);
                Assert.AreSame(second, this._view.CurrentItem, "CurrentItem before Remove");
                Assert.AreEqual<int>(1, this._view.CurrentPosition, "CurrentPosition before Remove");

                this._view.Remove(second);
                Assert.AreSame(first, this._view.CurrentItem, "CurrentItem after Remove");
                Assert.AreEqual<int>(0, this._view.CurrentPosition, "CurrentPosition after Remove");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(766862), WorkItem(862184), WorkItem(864171)]
        [Description("Removing an item after the CurrentItem does not affect currency")]
        public void RemoveItemAfterCurrentRetainsCurrency()
        {
            this.LoadSortedCities("Name", ListSortDirection.Ascending, 10, false);

            EnqueueCallback(() =>
            {
                City first = this._view.GetItemAt(0) as City;
                City second = this._view.GetItemAt(1) as City;
                Assert.AreSame(first, this._view.CurrentItem, "CurrentItem before Remove");
                Assert.AreEqual<int>(0, this._view.CurrentPosition, "CurrentPosition before Remove");

                this._view.Remove(second);
                Assert.AreSame(first, this._view.CurrentItem, "CurrentItem after Remove");
                Assert.AreEqual<int>(0, this._view.CurrentPosition, "CurrentPosition after Remove");
            });

            EnqueueTestComplete();
        }

        #endregion Currency Management

        #region Paging Enabled

        [TestMethod]
        [Asynchronous]
        [Description("Sorting is respected with paging enabled")]
        public void PagingAndSorting()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.LoadSize = 6;
                this._dds.PageSize = 3;
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Descending));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(3, this._view.Count, "Count should be 3 on the first page");
                Assert.AreEqual("Toledo", (this._view[0] as City).Name);
                Assert.AreEqual("Tacoma", (this._view[1] as City).Name);
                Assert.AreEqual("Santa Barbara", (this._view[2] as City).Name);

                // Move to 2nd page, which is already loaded
                this._ddsLoadingDataExpected = 0;
                this._view.MoveToNextPage();
            });

            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(3, this._view.Count, "Count should be 3 on the second page");
                Assert.AreEqual("Redmond", (this._view[0] as City).Name);
                Assert.AreEqual("Oregon", (this._view[1] as City).Name);
                Assert.AreEqual("Orange", (this._view[2] as City).Name);

                // Load 3rd page
                this._view.MoveToNextPage();
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(3, this._view.Count, "Count should be 3 on the 3rd page");
                Assert.AreEqual("Everett", (this._view[0] as City).Name);
                Assert.AreEqual("Duvall", (this._view[1] as City).Name);
                Assert.AreEqual("Carnation", (this._view[2] as City).Name);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When calling load after adding a sort decsriptor from other than the first page, the first page is loaded and the data is sorted")]
        public void AddingSortDescriptorAndThenLoadingLoadsFirstPage()
        {
            this.LoadCities(3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
                this._dds.Load();
            });

            this.AssertFirstPageLoadedSortedByName("After adding the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When calling refresh after adding a sort decsriptor from other than the first page, the first page is loaded and the data is sorted")]
        public void AddingSortDescriptorAndThenRefreshingLoadsFirstPage()
        {
            this.LoadCities(3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
                this._collectionView.Refresh();
            });

            this.AssertFirstPageLoadedSortedByName("After adding the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When adding a sort descriptor from other than the first page with AutoLoad set to true, the first page is loaded and the data is sorted")]
        public void AddingSortDescriptorWithAutoLoadLoadsFirstPage()
        {
            this.LoadDomainDataSourceControl();
            this.LoadCities(3, true);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
            });

            this.AssertFirstPageLoadedSortedByName("After adding the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When adding a sort descriptor from other than the first page within a defer load, the first page is loaded and the data is sorted")]
        public void AddingSortDescriptorWithinDeferLoadLoadsFirstPage()
        {
            this.LoadCities(3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._dds.DeferLoad())
                {
                    this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
                }
            });

            this.AssertFirstPageLoadedSortedByName("After adding the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When adding a sort descriptor from other than the first page within a defer refresh, the first page is loaded and the data is sorted")]
        public void AddingSortDescriptorWithinDeferRefreshLoadsFirstPage()
        {
            this.LoadCities(3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._collectionView.DeferRefresh())
                {
                    this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
                }
            });

            this.AssertFirstPageLoadedSortedByName("After adding the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When calling load after editing a sort descriptor from other than the first page, the first page is loaded and the data is sorted")]
        public void EditingSortDescriptorAndThenLoadingLoadsFirstPage()
        {
            this.LoadSortedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.SortDescriptors[0].PropertyPath = "Name";
                this._dds.Load();
            });

            this.AssertFirstPageLoadedSortedByName("After editing the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When calling refresh after editing a sort descriptor from other than the first page, the first page is loaded and the data is sorted")]
        public void EditingSortDescriptorAndThenRefreshingLoadsFirstPage()
        {
            this.LoadSortedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.SortDescriptors[0].PropertyPath = "Name";
                this._collectionView.Refresh();
            });

            this.AssertFirstPageLoadedSortedByName("After editing the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When editing a sort descriptor from other than the first page with AutoLoad set to true, the first page is loaded and the data is sorted")]
        public void EditingSortDescriptorWithAutoLoadLoadsFirstPage()
        {
            this.LoadDomainDataSourceControl();
            this.LoadSortedCities("StateName", 3, true);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.SortDescriptors[0].PropertyPath = "Name";
            });

            this.AssertFirstPageLoadedSortedByName("After editing the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When editing a sort descriptor from other than the first page within a defer load, the first page is loaded and the data is sorted")]
        public void EditingSortDescriptorWithinDeferLoadLoadsFirstPage()
        {
            this.LoadSortedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._dds.DeferLoad())
                {
                    this._dds.SortDescriptors[0].PropertyPath = "Name";
                }
            });

            this.AssertFirstPageLoadedSortedByName("After editing the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When editing a sort descriptor from other than the first page within a defer refresh, the first page is loaded and the data is sorted")]
        public void EditingSortDescriptorWithinDeferRefreshLoadsFirstPage()
        {
            this.LoadSortedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._collectionView.DeferRefresh())
                {
                    this._dds.SortDescriptors[0].PropertyPath = "Name";
                }
            });

            this.AssertFirstPageLoadedSortedByName("After editing the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When calling load after removing a sort descriptor from other than the first page, the first page is loaded and the data is sorted")]
        public void RemovingSortDescriptorAndThenLoadingLoadsFirstPage()
        {
            this.LoadSortedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.SortDescriptors.RemoveAt(0);
                this._dds.Load();
            });

            this.AssertFirstPageLoaded("After removing the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When calling refresh after removing a sort descriptor from other than the first page, the first page is loaded and the data is sorted")]
        public void RemovingSortDescriptorAndThenRefreshingLoadsFirstPage()
        {
            this.LoadSortedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.SortDescriptors.RemoveAt(0);
                this._collectionView.Refresh();
            });

            this.AssertFirstPageLoaded("After removing the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When removing a sort descriptor from other than the first page with AutoLoad set to true, the first page is loaded and the data is sorted")]
        public void RemovingSortDescriptorWithAutoLoadLoadsFirstPage()
        {
            this.LoadDomainDataSourceControl();
            this.LoadSortedCities("StateName", 3, true);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.SortDescriptors.RemoveAt(0);
            });

            this.AssertFirstPageLoaded("After removing the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When removing a sort descriptor from other than the first page within a defer load, the first page is loaded and the data is sorted")]
        public void RemovingSortDescriptorWithinDeferLoadLoadsFirstPage()
        {
            this.LoadSortedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._dds.DeferLoad())
                {
                    this._dds.SortDescriptors.RemoveAt(0);
                }
            });

            this.AssertFirstPageLoaded("After removing the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When removing a sort descriptor from other than the first page within a defer refresh, the first page is loaded and the data is sorted")]
        public void RemovingSortDescriptorWithinDeferRefreshLoadsFirstPage()
        {
            this.LoadSortedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._collectionView.DeferRefresh())
                {
                    this._dds.SortDescriptors.RemoveAt(0);
                }
            });

            this.AssertFirstPageLoaded("After removing the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When calling load after adding a sort description from other than the first page, the first page is loaded and the data is sorted")]
        public void AddingSortDescriptionAndThenLoadingLoadsFirstPage()
        {
            this.LoadCities(3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._collectionView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                this._dds.Load();
            });

            this.AssertFirstPageLoadedSortedByName("After adding the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When calling refresh after adding a sort description from other than the first page, the first page is loaded and the data is sorted")]
        public void AddingSortDescriptionAndThenRefreshingLoadsFirstPage()
        {
            this.LoadCities(3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._collectionView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                this._collectionView.Refresh();
            });

            this.AssertFirstPageLoadedSortedByName("After adding the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When adding a sort description from other than the first page with AutoLoad set to true, the first page is loaded and the data is sorted")]
        public void AddingSortDescriptionWithAutoLoadLoadsFirstPage()
        {
            this.LoadDomainDataSourceControl();
            this.LoadCities(3, true);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._collectionView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            });

            this.AssertFirstPageLoadedSortedByName("After adding the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When adding a sort description from other than the first page within a defer load, the first page is loaded and the data is sorted")]
        public void AddingSortDescriptionWithinDeferLoadLoadsFirstPage()
        {
            this.LoadCities(3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._dds.DeferLoad())
                {
                    this._collectionView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                }
            });

            this.AssertFirstPageLoadedSortedByName("After adding the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When adding a sort description from other than the first page within a defer refresh, the first page is loaded and the data is sorted")]
        public void AddingSortDescriptionWithinDeferRefreshLoadsFirstPage()
        {
            this.LoadCities(3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._collectionView.DeferRefresh())
                {
                    this._collectionView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                }
            });

            this.AssertFirstPageLoadedSortedByName("After adding the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When calling load after editing a sort description from other than the first page, the first page is loaded and the data is sorted")]
        public void EditingSortDescriptionAndThenLoadingLoadsFirstPage()
        {
            this.LoadSortedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                // SortDescriptions are immutable, so we must replace the entire sort description
                this._collectionView.SortDescriptions[0] = new SortDescription("Name", ListSortDirection.Ascending);
                this._dds.Load();
            });

            this.AssertFirstPageLoadedSortedByName("After editing the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When calling refresh after editing a sort description from other than the first page, the first page is loaded and the data is sorted")]
        public void EditingSortDescriptionAndThenRefreshingLoadsFirstPage()
        {
            this.LoadSortedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                // SortDescriptions are immutable, so we must replace the entire sort description
                this._collectionView.SortDescriptions[0] = new SortDescription("Name", ListSortDirection.Ascending);
                this._collectionView.Refresh();
            });

            this.AssertFirstPageLoadedSortedByName("After editing the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When editing a sort description from other than the first page with AutoLoad set to true, the first page is loaded and the data is sorted")]
        public void EditingSortDescriptionWithAutoLoadLoadsFirstPage()
        {
            this.LoadDomainDataSourceControl();
            this.LoadSortedCities("StateName", 3, true);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                // SortDescriptions are immutable, so we must replace the entire sort description
                this._collectionView.SortDescriptions[0] = new SortDescription("Name", ListSortDirection.Ascending);
            });

            this.AssertFirstPageLoadedSortedByName("After editing the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When editing a sort description from other than the first page within a defer load, the first page is loaded and the data is sorted")]
        public void EditingSortDescriptionWithinDeferLoadLoadsFirstPage()
        {
            this.LoadSortedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._dds.DeferLoad())
                {
                    // SortDescriptions are immutable, so we must replace the entire sort description
                    this._collectionView.SortDescriptions[0] = new SortDescription("Name", ListSortDirection.Ascending);
                }
            });

            this.AssertFirstPageLoadedSortedByName("After editing the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When editing a sort description from other than the first page within a defer refresh, the first page is loaded and the data is sorted")]
        public void EditingSortDescriptionWithinDeferRefreshLoadsFirstPage()
        {
            this.LoadSortedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._collectionView.DeferRefresh())
                {
                    // SortDescriptions are immutable, so we must replace the entire sort description
                    this._collectionView.SortDescriptions[0] = new SortDescription("Name", ListSortDirection.Ascending);
                }
            });

            this.AssertFirstPageLoadedSortedByName("After editing the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When calling load after removing a sort description from other than the first page, the first page is loaded and the data is sorted")]
        public void RemovingSortDescriptionAndThenLoadingLoadsFirstPage()
        {
            this.LoadSortedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._collectionView.SortDescriptions.RemoveAt(0);
                this._dds.Load();
            });

            this.AssertFirstPageLoaded("After removing the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When calling refresh after removing a sort description from other than the first page, the first page is loaded and the data is sorted")]
        public void RemovingSortDescriptionAndThenRefreshingLoadsFirstPage()
        {
            this.LoadSortedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._collectionView.SortDescriptions.RemoveAt(0);
                this._collectionView.Refresh();
            });

            this.AssertFirstPageLoaded("After removing the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When removing a sort description from other than the first page with AutoLoad set to true, the first page is loaded and the data is sorted")]
        public void RemovingSortDescriptionWithAutoLoadLoadsFirstPage()
        {
            this.LoadDomainDataSourceControl();
            this.LoadSortedCities("StateName", 3, true);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._collectionView.SortDescriptions.RemoveAt(0);
            });

            this.AssertFirstPageLoaded("After removing the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When removing a sort description from other than the first page within a defer load, the first page is loaded and the data is sorted")]
        public void RemovingSortDescriptionWithinDeferLoadLoadsFirstPage()
        {
            this.LoadSortedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._dds.DeferLoad())
                {
                    this._collectionView.SortDescriptions.RemoveAt(0);
                }
            });

            this.AssertFirstPageLoaded("After removing the sort");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When removing a sort description from other than the first page within a defer refresh, the first page is loaded and the data is sorted")]
        public void RemovingSortDescriptionWithinDeferRefreshLoadsFirstPage()
        {
            this.LoadSortedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._collectionView.DeferRefresh())
                {
                    this._collectionView.SortDescriptions.RemoveAt(0);
                }
            });

            this.AssertFirstPageLoaded("After removing the sort");

            EnqueueTestComplete();
        }

        #endregion Paging Enabled

        #region Progressive Loading Enabled

        [TestMethod]
        [Asynchronous]
        [WorkItem(812133)]
        [Description("Can sort while progressive loading is enabled")]
        public void SortingWithProgressiveLoading()
        {
            int totalCityCount = new CityData().Cities.Count;

            EnqueueCallback(() =>
            {
                // By using a loadsize of 1, we know the number of loads to be performed is equal to the
                // total city count + 1 (the load that will determine that there are no more records).
                this._dds.LoadSize = 1;
                this._dds.QueryName = "GetCities";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
                this._asyncEventFailureMessage = "First Progressive Load";
                this._dds.Load();
            });

            this.AssertLoadingData(totalCityCount + 1, true);

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                Assert.AreEqual<int>(totalCityCount, this._view.Count, "The count should match the total city count after allowing the first progressive load to finish");
                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.Name), ListSortDirection.Ascending, "Cities should be sorted by Name after the first progressive load");

                // Sort through the collection view like DataGrid does
                using (this._collectionView.DeferRefresh())
                {
                    this._collectionView.SortDescriptions.Clear();
                    this._collectionView.SortDescriptions.Add(new SortDescription("StateName", ListSortDirection.Ascending));
                    this._asyncEventFailureMessage = "Second Progressive Load";
                }
            });

            this.AssertLoadingData(totalCityCount + 1, true);

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                Assert.AreEqual<int>(totalCityCount, this._view.Count, "The count should match the total city count after allowing the second progressive load to finish");
                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.StateName), ListSortDirection.Ascending, "Cities should be sorted by StateName after the second progressive load");
            });

            EnqueueTestComplete();
        }

        #endregion Progressive Loading Enabled

        #region Helper Methods

        /// <summary>
        /// Enqueue the necessary calls to load the cities, sorted by the specified property,
        /// and with the specified page size and auto load properties.
        /// </summary>
        /// <param name="sortProperty">The property to sort the cities by.</param>
        /// <param name="pageSize">The <see cref="DomainDataSource.PageSize"/> to use.</param>
        /// <param name="autoLoad">What to set <see cref="DomainDataSource.AutoLoad"/> to.</param>
        private void LoadSortedCities(string sortProperty, int pageSize, bool autoLoad)
        {
            this.LoadSortedCities(sortProperty, ListSortDirection.Ascending, pageSize, autoLoad);
        }

        /// <summary>
        /// Enqueue the necessary calls to load the cities, sorted by the specified property,
        /// and with the specified page size and auto load properties.
        /// </summary>
        /// <param name="sortProperty">The property to sort the cities by.</param>
        /// <param name="direction">The sort direction.</param>
        /// <param name="pageSize">The <see cref="DomainDataSource.PageSize"/> to use.</param>
        /// <param name="autoLoad">What to set <see cref="DomainDataSource.AutoLoad"/> to.</param>
        private void LoadSortedCities(string sortProperty, ListSortDirection direction, int pageSize, bool autoLoad)
        {
            EnqueueCallback(() =>
            {
                this._collectionView.SortDescriptions.Add(new SortDescription(sortProperty, direction));
            });

            this.LoadCities(pageSize, autoLoad);
        }

        #endregion Helper Methods
    }
}
