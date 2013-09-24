using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
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
    public class GroupingTests : DomainDataSourceTestBase
    {
        #region Single-Level Grouping

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the GroupDescriptors property functions properly.")]
        public void Grouping()
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

                Assert.AreEqual(11, this._view.Count);
                Assert.AreEqual("WA", (this._view[0] as City).StateName);
                Assert.AreEqual("WA", (this._view[1] as City).StateName);
                Assert.AreEqual("WA", (this._view[2] as City).StateName);
                Assert.AreEqual("WA", (this._view[3] as City).StateName);
                Assert.AreEqual("WA", (this._view[4] as City).StateName);
                Assert.AreEqual("WA", (this._view[5] as City).StateName);
                Assert.AreEqual("OR", (this._view[6] as City).StateName);
                Assert.AreEqual("CA", (this._view[7] as City).StateName);
                Assert.AreEqual("CA", (this._view[8] as City).StateName);
                Assert.AreEqual("OH", (this._view[9] as City).StateName);
                Assert.AreEqual("OH", (this._view[10] as City).StateName);

                Assert.IsNull(this._collectionView.Groups);

                this._dds.GroupDescriptors.Add(new GroupDescriptor("StateName"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                Assert.IsNotNull(this._collectionView.Groups);
                Assert.AreEqual(4, this._collectionView.Groups.Count);

                Assert.AreEqual("CA", (this._view[0] as City).StateName);
                Assert.AreEqual("CA", (this._view[1] as City).StateName);
                Assert.AreEqual("OH", (this._view[2] as City).StateName);
                Assert.AreEqual("OH", (this._view[3] as City).StateName);
                Assert.AreEqual("OR", (this._view[4] as City).StateName);
                Assert.AreEqual("WA", (this._view[5] as City).StateName);
                Assert.AreEqual("WA", (this._view[6] as City).StateName);
                Assert.AreEqual("WA", (this._view[7] as City).StateName);
                Assert.AreEqual("WA", (this._view[8] as City).StateName);
                Assert.AreEqual("WA", (this._view[9] as City).StateName);
                Assert.AreEqual("WA", (this._view[10] as City).StateName);

                this._dds.GroupDescriptors.Add(new GroupDescriptor("CountyName"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                Assert.AreEqual("Orange", (this._view[0] as City).CountyName);
                Assert.AreEqual("Santa Barbara", (this._view[1] as City).CountyName);
                Assert.AreEqual("Lucas", (this._view[2] as City).CountyName);
                Assert.AreEqual("Lucas", (this._view[3] as City).CountyName);
                Assert.AreEqual("Jackson", (this._view[4] as City).CountyName);
                Assert.AreEqual("King", (this._view[5] as City).CountyName);
                Assert.AreEqual("King", (this._view[6] as City).CountyName);
                Assert.AreEqual("King", (this._view[7] as City).CountyName);
                Assert.AreEqual("King", (this._view[8] as City).CountyName);
                Assert.AreEqual("King", (this._view[9] as City).CountyName);
                Assert.AreEqual("Pierce", (this._view[10] as City).CountyName);

                this._dds.GroupDescriptors.RemoveAt(1); // removing "CountyName"
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                Assert.AreEqual("CA", (this._view[0] as City).StateName);
                Assert.AreEqual("CA", (this._view[1] as City).StateName);
                Assert.AreEqual("OH", (this._view[2] as City).StateName);
                Assert.AreEqual("OH", (this._view[3] as City).StateName);
                Assert.AreEqual("OR", (this._view[4] as City).StateName);
                Assert.AreEqual("WA", (this._view[5] as City).StateName);
                Assert.AreEqual("WA", (this._view[6] as City).StateName);
                Assert.AreEqual("WA", (this._view[7] as City).StateName);
                Assert.AreEqual("WA", (this._view[8] as City).StateName);
                Assert.AreEqual("WA", (this._view[9] as City).StateName);
                Assert.AreEqual("WA", (this._view[10] as City).StateName);

                this._dds.GroupDescriptors[0] = new GroupDescriptor("CountyName"); // replacing
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                Assert.AreEqual("Jackson", (this._view[0] as City).CountyName);
                Assert.AreEqual("King", (this._view[1] as City).CountyName);
                Assert.AreEqual("King", (this._view[2] as City).CountyName);
                Assert.AreEqual("King", (this._view[3] as City).CountyName);
                Assert.AreEqual("King", (this._view[4] as City).CountyName);
                Assert.AreEqual("King", (this._view[5] as City).CountyName);
                Assert.AreEqual("Lucas", (this._view[6] as City).CountyName);
                Assert.AreEqual("Lucas", (this._view[7] as City).CountyName);
                Assert.AreEqual("Orange", (this._view[8] as City).CountyName);
                Assert.AreEqual("Pierce", (this._view[9] as City).CountyName);
                Assert.AreEqual("Santa Barbara", (this._view[10] as City).CountyName);

                this._dds.GroupDescriptors.Clear(); // removing all
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                Assert.AreEqual("WA", (this._view[0] as City).StateName);
                Assert.AreEqual("WA", (this._view[1] as City).StateName);
                Assert.AreEqual("WA", (this._view[2] as City).StateName);
                Assert.AreEqual("WA", (this._view[3] as City).StateName);
                Assert.AreEqual("WA", (this._view[4] as City).StateName);
                Assert.AreEqual("WA", (this._view[5] as City).StateName);
                Assert.AreEqual("OR", (this._view[6] as City).StateName);
                Assert.AreEqual("CA", (this._view[7] as City).StateName);
                Assert.AreEqual("CA", (this._view[8] as City).StateName);
                Assert.AreEqual("OH", (this._view[9] as City).StateName);
                Assert.AreEqual("OH", (this._view[10] as City).StateName);

                Assert.IsNull(this._collectionView.Groups);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the GroupDescriptors and GroupDescriptions are in sync.")]
        public void GroupDesciptorInSyncTest()
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

                Assert.AreEqual(0, this._collectionView.GroupDescriptions.Count);
                Assert.AreEqual(0, this._dds.GroupDescriptors.Count);

                this._dds.GroupDescriptors.Add(new GroupDescriptor("StateName"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                Assert.AreEqual(this._dds.GroupDescriptors.Count, this._collectionView.GroupDescriptions.Count);
                Assert.AreEqual(this._dds.GroupDescriptors[0].PropertyPath, (this._collectionView.GroupDescriptions[0] as PropertyGroupDescription).PropertyName);

                this._collectionView.GroupDescriptions.Add(new PropertyGroupDescription("CountyName"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(this._dds.GroupDescriptors.Count, this._collectionView.GroupDescriptions.Count);
                Assert.AreEqual(this._dds.GroupDescriptors[0].PropertyPath, (this._collectionView.GroupDescriptions[0] as PropertyGroupDescription).PropertyName);
                Assert.AreEqual(this._dds.GroupDescriptors[1].PropertyPath, (this._collectionView.GroupDescriptions[1] as PropertyGroupDescription).PropertyName);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Editing a GroupDescription's PropertyName results in a refresh of the data.")]
        public void EditingGroupDescriptionPropertyName()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._collectionView.GroupDescriptions.Add(new PropertyGroupDescription("StateName"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                // Assert against the GroupDescriptors to ensure that changes to GroupDescriptions flow through
                Assert.AreEqual(1, this._dds.GroupDescriptors.Count);
                Assert.AreEqual("StateName", this._dds.GroupDescriptors[0].PropertyPath);

                (this._collectionView.GroupDescriptions[0] as PropertyGroupDescription).PropertyName = "CountyName";
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                // Assert against the GroupDescriptors to ensure that changes to GroupDescriptions flow through
                Assert.AreEqual(1, this._dds.GroupDescriptors.Count);
                Assert.AreEqual("CountyName", this._dds.GroupDescriptors[0].PropertyPath);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Replacing a GroupDescription results in a refresh of the data")]
        public void ReplacingGroupDescription()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._collectionView.GroupDescriptions.Add(new PropertyGroupDescription("StateName"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                // Assert against the GroupDescriptors to ensure that changes to GroupDescriptions flow through
                Assert.AreEqual(1, this._dds.GroupDescriptors.Count);
                Assert.AreEqual("StateName", this._dds.GroupDescriptors[0].PropertyPath);

                this._collectionView.GroupDescriptions[0] = new PropertyGroupDescription("CountyName");
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                // Assert against the GroupDescriptors to ensure that changes to GroupDescriptions flow through
                Assert.AreEqual(1, this._dds.GroupDescriptors.Count);
                Assert.AreEqual("CountyName", this._dds.GroupDescriptors[0].PropertyPath);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Editing a GroupDescriptor's PropertyPath.Value results in a refresh of the data.")]
        public void EditingGroupDescriptorPropertyPathValue()
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

                // Assert against the GroupDescriptions to ensure that changes to GroupDescriptors flow through
                Assert.AreEqual(1, this._collectionView.GroupDescriptions.Count);
                Assert.AreEqual("StateName", (this._collectionView.GroupDescriptions[0] as PropertyGroupDescription).PropertyName);

                this._dds.GroupDescriptors[0].PropertyPath = "CountyName";
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                // Assert against the GroupDescriptions to ensure that changes to GroupDescriptors flow through
                Assert.AreEqual(1, this._collectionView.GroupDescriptions.Count);
                Assert.AreEqual("CountyName", (this._collectionView.GroupDescriptions[0] as PropertyGroupDescription).PropertyName);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Replacing a GroupDescriptor results in a refresh of the data.")]
        public void ReplacingGroupDescriptor()
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

                // Assert against the GroupDescriptions to ensure that changes to GroupDescriptors flow through
                Assert.AreEqual(1, this._collectionView.GroupDescriptions.Count);
                Assert.AreEqual("StateName", (this._collectionView.GroupDescriptions[0] as PropertyGroupDescription).PropertyName);

                this._dds.GroupDescriptors[0] = new GroupDescriptor("CountyName");
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                // Assert against the GroupDescriptions to ensure that changes to GroupDescriptors flow through
                Assert.AreEqual(1, this._collectionView.GroupDescriptions.Count);
                Assert.AreEqual("CountyName", (this._collectionView.GroupDescriptions[0] as PropertyGroupDescription).PropertyName);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(718473)]
        [Description("When grouping by a field that is sorted in descending order, the group is applied in descending order")]
        public void GroupingByFieldWithDescendingSortRespectsDescendingOrder()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.GroupDescriptors.Add(new GroupDescriptor("StateName"));
                this._dds.PageSize = 4;
                this._dds.LoadSize = 8;
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.IsTrue(this._collectionView.Groups.Count > 1, "We need more than 1 group on the first page for this test");
                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.StateName), ListSortDirection.Ascending, "Initial load");

                // Sort by StateName descending and then Name descending
                this._dds.SortDescriptors.Add(new SortDescriptor("StateName", ListSortDirection.Descending));
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Descending));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.StateName + ":" + c.Name), ListSortDirection.Descending, "Sorting on the first page");
                this._view.MoveToNextPage();
            });

            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.StateName + ":" + c.Name), ListSortDirection.Descending, "Sorting on the second page");
            });

            EnqueueTestComplete();
        }

        [WorkItem(718857)]
        [TestMethod]
        [Asynchronous]
        [Description("Test that an element on the second page can be deleted and the changes rejected successfully.")]
        public void SecondPageDeleteAndRejectWithGrouping()
        {
            NotifyCollectionChangedEventHandler ccHandler = (sender, e) =>
            {
                Assert.IsTrue((e.Action != NotifyCollectionChangedAction.Add) || (e.NewItems[0] != null),
                    "A null item should never be added to the ECV.");
            };

            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.GroupDescriptors.Add(new GroupDescriptor("StateName"));
                this._dds.PageSize = 3;
                this._dds.LoadSize = 9;
                this._dds.Load();
                this._collectionView.CollectionChanged += ccHandler;
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                this._view.MoveToPage(1);
            });

            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetPageChanged();

                this._view.RemoveAt(1);
                this._dds.RejectChanges();
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(770746)]
        [Description("When adding a new item with grouping set up, the new item's new group should be sorted appropriately")]
        public void GroupingAddToNewGroup()
        {
            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.GroupDescriptors.Add(new GroupDescriptor("StateName"));
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.StateName), ListSortDirection.Ascending, "The cities should be sorted by state name after the initial load");

                City toAdd = new City
                {
                    Name = "Added City",
                    StateName = "ST",
                    CountyName = "County"
                };

                int originalCount = this._view.Count;
                this._view.Add(toAdd);
                Assert.AreEqual<int>(originalCount + 1, this._view.Count, "The count should increment by one after adding the city");
                Assert.IsTrue(this._view.Contains(toAdd), "The added city should be in the view");
                Assert.AreEqual<int>(this._view.IndexOf(toAdd), this._view.CurrentPosition, "CurrentPosition should match the index of the item added");
                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.StateName), ListSortDirection.Ascending, "The cities should be sorted by state name after adding the city");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(770746)]
        [Description("When adding a new item through AddNew with grouping set up, the new item's new group should be sorted appropriately")]
        public void GroupingAddNewToNewGroup()
        {
            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.GroupDescriptors.Add(new GroupDescriptor("StateName"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.StateName), ListSortDirection.Ascending, "The cities should be sorted by state name after the initial load");

                int originalCount = this._view.Count;

                City toAdd = (City)this._editableCollectionView.AddNew();
                Assert.AreEqual<int>(originalCount + 1, this._view.Count, "The count should increment by one after adding the city");
                Assert.IsTrue(this._view.Contains(toAdd), "The added city should be in the view");
                Assert.AreEqual<int>(this._view.IndexOf(toAdd), this._view.CurrentPosition, "CurrentPosition should match the index of the item added");
                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.StateName), ListSortDirection.Ascending, "The cities should be sorted by state name after adding the city");
                toAdd.Name = "Added City";
                toAdd.StateName = "ST";
                toAdd.CountyName = "County";

                this._editableCollectionView.CommitNew();
                Assert.AreEqual<int>(originalCount + 1, this._view.Count, "The count should increment by one after committing the city");
                Assert.IsTrue(this._view.Contains(toAdd), "The committed city should be in the view");
                Assert.AreEqual<int>(this._view.IndexOf(toAdd), this._view.CurrentPosition, "CurrentPosition should match the index of the item committed");
                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.StateName), ListSortDirection.Ascending, "The cities should be sorted by state name after committing the city");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(766862)]
        [Description("When adding a new item with grouping set up, the new item should be sorted appropriately within the existing group")]
        public void GroupingAddToExistingGroup()
        {
            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.GroupDescriptors.Add(new GroupDescriptor("StateName"));
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.StateName), ListSortDirection.Ascending, "The cities should be sorted by state name after the initial load");

                string existingStateName = this._view.Cast<City>().Last().StateName;

                City toAdd = new City
                {
                    Name = "Added City",
                    StateName = existingStateName,
                    CountyName = "County"
                };

                int originalCount = this._view.Count;
                this._view.Add(toAdd);
                Assert.AreEqual<int>(originalCount + 1, this._view.Count, "The count should increment by one after adding the city");
                Assert.IsTrue(this._view.Contains(toAdd), "The added city should be in the view");
                Assert.AreEqual<int>(this._view.IndexOf(toAdd), this._view.CurrentPosition, "CurrentPosition should match the index of the item added");
                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.StateName), ListSortDirection.Ascending, "The cities should be sorted by state name after adding the city");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(766862)]
        [Description("When adding a new item through AddNew with grouping set up, the new item should be sorted appropriately within the existing group")]
        public void GroupingAddNewToExistingGroup()
        {
            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.GroupDescriptors.Add(new GroupDescriptor("StateName"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.StateName), ListSortDirection.Ascending, "The cities should be sorted by state name after the initial load");

                int originalCount = this._view.Count;
                string existingStateName = this._view.Cast<City>().Last().StateName;

                City toAdd = (City)this._editableCollectionView.AddNew();
                Assert.AreEqual<int>(originalCount + 1, this._view.Count, "The count should increment by one after adding the city");
                Assert.IsTrue(this._view.Contains(toAdd), "The added city should be in the view");
                Assert.AreEqual<int>(this._view.IndexOf(toAdd), this._view.CurrentPosition, "CurrentPosition should match the index of the item added");
                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.StateName), ListSortDirection.Ascending, "The cities should be sorted by state name after adding the city");
                toAdd.Name = "Added City";
                toAdd.StateName = existingStateName;
                toAdd.CountyName = "County";

                this._editableCollectionView.CommitNew();
                Assert.AreEqual<int>(originalCount + 1, this._view.Count, "The count should increment by one after committing the city");
                Assert.IsTrue(this._view.Contains(toAdd), "The committed city should be in the view");
                Assert.AreEqual<int>(this._view.IndexOf(toAdd), this._view.CurrentPosition, "CurrentPosition should match the index of the item committed");
                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.StateName), ListSortDirection.Ascending, "The cities should be sorted by state name after committing the city");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(782521)]
        [Description("When Grouping, adding and then removing an item should remove the item from the view")]
        public void GroupingAddAndRemoveItem()
        {
            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.GroupDescriptors.Add(new GroupDescriptor("StateName"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                City toAddAndRemove = new City
                {
                    Name = "Add and Remove",
                    StateName = "ST",
                    CountyName = "County"
                };

                int originalCount = this._view.Count;
                this._view.Add(toAddAndRemove);
                Assert.AreEqual<int>(originalCount + 1, this._view.Count, "The count should increment by one after adding the city");
                Assert.IsTrue(this._view.Contains(toAddAndRemove), "The added city should be in the view");

                List<NotifyCollectionChangedEventArgs> actions = new List<NotifyCollectionChangedEventArgs>();
                this._collectionView.CollectionChanged += (s, e) => actions.Add(e);

                this._view.Remove(toAddAndRemove);
                Assert.AreEqual<int>(originalCount, this._view.Count, "The count should decrement by one after removing the city");
                Assert.IsFalse(this._view.Contains(toAddAndRemove), "The removed city should no longer be in the view");

                Assert.AreEqual<int>(1, actions.Count, "There should have been one CollectionChanged event removing the city");
                Assert.AreEqual<NotifyCollectionChangedAction>(NotifyCollectionChangedAction.Remove, actions.Single().Action, "The single CollectionChanged event action should have been a Remove");
                Assert.AreEqual<int>(1, actions.Single().OldItems.Count, "There should have been one element in OldItems");
                Assert.AreEqual<City>(toAddAndRemove, actions.Single().OldItems.Cast<City>().Single(), "The OldItems item should have been our city that was removed");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(798406)]
        [Description("When Grouping, transactionally adding an item and then cancelling the add should remove it from view")]
        public void GroupingAddNewAndCancel()
        {
            this.LoadGroupedCities("StateName", 0, false);

            EnqueueCallback(() =>
            {
                int originalCount = this._view.Count;

                this._editableCollectionView.AddNew();
                Assert.AreEqual(originalCount + 1, this._view.Count, "The count should increment by one after adding the city");

                this._editableCollectionView.CancelNew();
                Assert.AreEqual<int>(originalCount, this._view.Count, "The count should decrement by one after cancelling the AddNew");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(845238)]
        [Description("Tests that a change to the GroupDescriptions is not applied locally and causes a reload.")]
        public void GroupIsNotAppliedUntilAutoloadCompletes()
        {
            CityData cities = new CityData();

            this.LoadDomainDataSourceControl();

            this.LoadCities(0, true);

            this.EnqueueCallback(() =>
            {
                Assert.IsTrue(cities.Cities.Select(c => c.Name).SequenceEqual(this._view.Cast<City>().Select(c => c.Name)),
                    "The view order should be equal to the Cities order.");

                this._collectionView.GroupDescriptions.Add(new PropertyGroupDescription("StateName"));

                Assert.IsTrue(cities.Cities.Select(c => c.Name).SequenceEqual(this._view.Cast<City>().Select(c => c.Name)),
                    "The view order should still be equal to the Cities order.");
            });

            this.AssertLoadedData();

            this.EnqueueCallback(() =>
            {
                this.ResetLoadState();

                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.StateName), ListSortDirection.Ascending,
                    "The view order should now be sorted in the ascending direction.");
            });

            EnqueueTestComplete();
        }

        #endregion Single-Level Grouping

        #region Nested Grouping

        [TestMethod]
        [Asynchronous]
        [WorkItem(796434)]
        [Description("Adding a new item that will have nested new groups")]
        public void GroupingAddToNewGroupWithinNewGroup()
        {
            this.LoadCitiesGroupedByStateThenCounty();

            EnqueueCallback(() =>
            {
                City toAdd = new City
                {
                    Name = "Added City",
                    StateName = "ST",
                    CountyName = "County"
                };

                int originalCount = this._view.Count;
                this._view.Add(toAdd);
                Assert.AreEqual<int>(originalCount + 1, this._view.Count, "The count should increment by one after adding the city");
                Assert.IsTrue(this._view.Contains(toAdd), "The added city should be in the view");
                Assert.AreEqual<int>(this._view.IndexOf(toAdd), this._view.CurrentPosition, "CurrentPosition should match the index of the item added");
                this.AssertStateCountyCitySorting("After Add");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(796434)]
        [Description("Adding a new item with AddNew that will have nested new groups")]
        public void GroupingAddNewToNewGroupWithinNewGroup()
        {
            this.LoadCitiesGroupedByStateThenCounty();

            EnqueueCallback(() =>
            {
                int originalCount = this._view.Count;

                City toAdd = (City)this._editableCollectionView.AddNew();
                toAdd.Name = "Added City";
                toAdd.StateName = "ST";
                toAdd.CountyName = "County";

                this._editableCollectionView.CommitNew();
                Assert.AreEqual<int>(originalCount + 1, this._view.Count, "The count should increment by one after adding the city");
                Assert.IsTrue(this._view.Contains(toAdd), "The added city should be in the view");
                Assert.AreEqual<int>(this._view.IndexOf(toAdd), this._view.CurrentPosition, "CurrentPosition should match the index of the item added");
                this.AssertStateCountyCitySorting("After AddNew");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(796434)]
        [Description("Adding a new item that will be in a new group within an existing group")]
        public void GroupingAddToNewGroupWithinExistingGroup()
        {
            this.LoadCitiesGroupedByStateThenCounty();

            EnqueueCallback(() =>
            {
                // Find an existing state that has more than 1 county so that we can insert into the middle
                IEnumerable<City> cities = this._view.Cast<City>();

                string existingStateName = (from city in cities
                                            let state = city.StateName
                                            let counties = cities.Where(c => c.StateName == state).Distinct().Count()
                                            where counties > 1
                                            orderby counties descending
                                            select state).FirstOrDefault();

                if (existingStateName == null)
                {
                    Assert.Inconclusive("This test relies on a state with more than 1 county");
                }

                // Force two of the existing elements to have county names such that we can insert in between them
                // This will test to ensure the new group isn't just put at the beginning or end, but rather
                // sorted correctly within the first-level group.
                IEnumerable<City> citiesInState = cities.Where(c => c.StateName == existingStateName);

                City first = citiesInState.ElementAt(0);
                this._editableCollectionView.EditItem(first);
                first.CountyName = "AAAAA";
                this._editableCollectionView.CommitEdit();

                City second = citiesInState.ElementAt(1);
                this._editableCollectionView.EditItem(second);
                second.CountyName = "ZZZZZ";
                this._editableCollectionView.CommitEdit();

                this.AssertStateCountyCitySorting("After splitting elements 0 and 1 apart");

                City toAdd = new City
                {
                    Name = "Added City",
                    StateName = existingStateName,
                    CountyName = "MMMMM"
                };

                int originalCount = this._view.Count;
                this._view.Add(toAdd);
                Assert.AreEqual<int>(originalCount + 1, this._view.Count, "The count should increment by one after adding the city");
                Assert.IsTrue(this._view.Contains(toAdd), "The added city should be in the view");
                Assert.AreEqual<int>(this._view.IndexOf(toAdd), this._view.CurrentPosition, "CurrentPosition should match the index of the item added");
                this.AssertStateCountyCitySorting("After Add");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(796434)]
        [Description("Adding a new item with AddNew that will be in a new group within an existing group")]
        public void GroupingAddNewToNewGroupWithinExistingGroup()
        {
            this.LoadCitiesGroupedByStateThenCounty();

            EnqueueCallback(() =>
            {
                // Find an existing state that has more than 1 county so that we can insert into the middle
                IEnumerable<City> cities = this._view.Cast<City>();

                string existingStateName = (from city in cities
                                            let state = city.StateName
                                            let counties = cities.Where(c => c.StateName == state).Distinct().Count()
                                            where counties > 1
                                            orderby counties descending
                                            select state).FirstOrDefault();

                if (existingStateName == null)
                {
                    Assert.Inconclusive("This test relies on a state with more than 1 county");
                }

                // Force two of the existing elements to have county names such that we can insert in between them
                // This will test to ensure the new group isn't just put at the beginning or end, but rather
                // sorted correctly within the first-level group.
                IEnumerable<City> citiesInState = cities.Where(c => c.StateName == existingStateName);

                City first = citiesInState.ElementAt(0);
                this._editableCollectionView.EditItem(first);
                first.CountyName = "AAAAA";
                this._editableCollectionView.CommitEdit();

                City second = citiesInState.ElementAt(1);
                this._editableCollectionView.EditItem(second);
                second.CountyName = "ZZZZZ";
                this._editableCollectionView.CommitEdit();

                this.AssertStateCountyCitySorting("After splitting elements 0 and 1 apart");

                int originalCount = this._view.Count;

                City toAdd = (City)this._editableCollectionView.AddNew();
                toAdd.Name = "Added City";
                toAdd.StateName = existingStateName;
                toAdd.CountyName = "MMMMM";
                this._editableCollectionView.CommitNew();

                Assert.AreEqual<int>(originalCount + 1, this._view.Count, "The count should increment by one after adding the city");
                Assert.IsTrue(this._view.Contains(toAdd), "The added city should be in the view");
                Assert.AreEqual<int>(this._view.IndexOf(toAdd), this._view.CurrentPosition, "CurrentPosition should match the index of the item added");
                this.AssertStateCountyCitySorting("After AddNew");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(796434)]
        [Description("Adding a new item that will be in an existing group within an existing group")]
        public void GroupingAddToExistingGroupWithinExistingGroup()
        {
            this.LoadCitiesGroupedByStateThenCounty();

            EnqueueCallback(() =>
            {
                // Find an existing state and county that has more than 1 city
                IEnumerable<City> cities = this._view.Cast<City>();

                City reference = (from city in cities
                                  let state = city.StateName
                                  let county = city.CountyName
                                  let countyCities = cities.Where(c => c.StateName == state && c.CountyName == county).Distinct().Count()
                                  where countyCities > 1
                                  orderby countyCities descending
                                  select city).FirstOrDefault();

                if (reference == null)
                {
                    Assert.Inconclusive("This test relies on a county with more than 1 city");
                }

                // Force two of the existing elements to have city names such that we can insert in between them
                // This will test to ensure the new item isn't just put at the beginning or end, but rather
                // sorted correctly within the second-level group.
                IEnumerable<City> citiesInCounty = cities.Where(c => c.StateName == reference.StateName && c.CountyName == reference.CountyName);

                City first = citiesInCounty.ElementAt(0);
                this._editableCollectionView.EditItem(first);
                // Here we're using ApplyState to allow us to set a PK member w/o validation failure
                // since PK members cannot be changed. This test should really be based on an association
                // not involving PK, but the test is still valid this way.
                first.ApplyState(new Dictionary<string, object> { { "Name", "AAAAA" } });
                this._editableCollectionView.CommitEdit();

                City second = citiesInCounty.ElementAt(1);
                this._editableCollectionView.EditItem(second);
                second.ApplyState(new Dictionary<string, object> { { "Name", "ZZZZZ" } });
                this._editableCollectionView.CommitEdit();

                this.AssertStateCountyCitySorting("After splitting elements 0 and 1 apart");

                City toAdd = new City
                {
                    Name = "MMMMM",
                    StateName = reference.StateName,
                    CountyName = reference.CountyName
                };

                int originalCount = this._view.Count;
                this._view.Add(toAdd);
                Assert.AreEqual<int>(originalCount + 1, this._view.Count, "The count should increment by one after adding the city");
                Assert.IsTrue(this._view.Contains(toAdd), "The added city should be in the view");
                Assert.AreEqual<int>(this._view.IndexOf(toAdd), this._view.CurrentPosition, "CurrentPosition should match the index of the item added");
                this.AssertStateCountyCitySorting("After Add");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(796434)]
        [Description("Adding a new item with AddNew that will be in an existing group within an existing group")]
        public void GroupingAddNewToExistingGroupWithinExistingGroup()
        {
            this.LoadCitiesGroupedByStateThenCounty();

            EnqueueCallback(() =>
            {
                // Find an existing state and county that has more than 1 city
                IEnumerable<City> cities = this._view.Cast<City>();

                City reference = (from city in cities
                                  let state = city.StateName
                                  let county = city.CountyName
                                  let countyCities = cities.Where(c => c.StateName == state && c.CountyName == county).Distinct().Count()
                                  where countyCities > 1
                                  orderby countyCities descending
                                  select city).FirstOrDefault();

                if (reference == null)
                {
                    Assert.Inconclusive("This test relies on a county with more than 1 city");
                }

                // Force two of the existing elements to have city names such that we can insert in between them
                // This will test to ensure the new item isn't just put at the beginning or end, but rather
                // sorted correctly within the second-level group.
                IEnumerable<City> citiesInCounty = cities.Where(c => c.StateName == reference.StateName && c.CountyName == reference.CountyName);

                City first = citiesInCounty.ElementAt(0);
                this._editableCollectionView.EditItem(first);
                // Here we're using ApplyState to allow us to set a PK member w/o validation failure
                // since PK members cannot be changed. This test should really be based on an association
                // not involving PK, but the test is still valid this way.
                first.ApplyState(new Dictionary<string, object> { { "Name", "AAAAA" } });
                this._editableCollectionView.CommitEdit();

                City second = citiesInCounty.ElementAt(1);
                this._editableCollectionView.EditItem(second);
                second.ApplyState(new Dictionary<string, object> { { "Name", "ZZZZZ" } });
                this._editableCollectionView.CommitEdit();

                this.AssertStateCountyCitySorting("After splitting elements 0 and 1 apart");

                int originalCount = this._view.Count;

                City toAdd = (City)this._editableCollectionView.AddNew();
                toAdd.Name = "MMMMM";
                toAdd.StateName = reference.StateName;
                toAdd.CountyName = reference.CountyName;
                this._editableCollectionView.CommitNew();

                Assert.AreEqual<int>(originalCount + 1, this._view.Count, "The count should increment by one after adding the city");
                Assert.IsTrue(this._view.Contains(toAdd), "The added city should be in the view");
                Assert.AreEqual<int>(this._view.IndexOf(toAdd), this._view.CurrentPosition, "CurrentPosition should match the index of the item added");
                this.AssertStateCountyCitySorting("After Add");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(796680)]
        [Description("When Grouping, editing the group name field on the last item in a nested group should remove the group and create a new one")]
        public void GroupingEditItemToRemoveGroupAndCreateNewNestedGroup()
        {
            this.LoadCitiesGroupedByStateThenCounty();

            EnqueueCallback(() =>
            {
                City toAddAndEdit = new City
                {
                    Name = "Add and Edit",
                    StateName = "AA",
                    CountyName = "County"
                };

                this._view.Add(toAddAndEdit);
                Assert.IsTrue(this._view.Contains(toAddAndEdit), "After being added");

                toAddAndEdit.StateName = "BB";
                Assert.IsTrue(this._view.Contains(toAddAndEdit), "After being edited directly");
                this.AssertStateCountyCitySorting("After being edited directly");

                this._editableCollectionView.EditItem(toAddAndEdit);
                toAddAndEdit.StateName = "CC";
                this._editableCollectionView.CommitEdit();
                Assert.IsTrue(this._view.Contains(toAddAndEdit), "After being edited with EditItem/CommitEdit");
                this.AssertStateCountyCitySorting("After being edited with EditItem/CommitEdit");
            });

            EnqueueTestComplete();
        }

        #endregion Nested Grouping

        #region Currency Management

        [TestMethod]
        [Asynchronous]
        [Description("Ensure that MoveCurrentToFirst uses correct indexes when sorted Ascending")]
        public void GroupMoveCurrentToFirst()
        {
            this.LoadGroupedCities("Name", 5, false);

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
        public void GroupMoveCurrentToLast()
        {
            this.LoadGroupedCities("Name", 5, false);

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
        public void GroupMoveCurrentToPrevious()
        {
            this.LoadGroupedCities("Name", 5, false);

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
        public void GroupMoveCurrentToNext()
        {
            this.LoadGroupedCities("Name", 5, false);

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
        public void GroupMoveCurrentToPosition()
        {
            this.LoadGroupedCities("Name", 5, false);

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
        public void GroupMoveCurrentTo()
        {
            this.LoadGroupedCities("Name", 5, false);

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
            this.LoadGroupedCities("Name", 10, false);

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
            this.LoadGroupedCities("Name", 10, false);

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
            this.LoadGroupedCities("Name", 1, false);

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
            this.LoadGroupedCities("Name", 10, false);

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
            this.LoadGroupedCities("Name", 10, false);

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
            this.LoadGroupedCities("Name", 2, false);

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
            this.LoadGroupedCities("Name", 10, false);

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
        [Description("Group-applied sorting is respected with paging enabled")]
        public void PagingAndGrouping()
        {
            this.LoadGroupedCities("StateName", 3, false);

            EnqueueCallback(() =>
            {
                Assert.AreEqual("CA", (this._view[0] as City).StateName);
                Assert.AreEqual("CA", (this._view[1] as City).StateName);
                Assert.AreEqual("OH", (this._view[2] as City).StateName);
            });

            this.MoveToNextPage("First MoveToNextPage()");

            EnqueueCallback(() =>
            {
                Assert.AreEqual("OH", (this._view[0] as City).StateName);
                Assert.AreEqual("OR", (this._view[1] as City).StateName);
                Assert.AreEqual("WA", (this._view[2] as City).StateName);
            });

            this.MoveToNextPage("Second MoveToNextPage()");

            EnqueueCallback(() =>
            {
                Assert.AreEqual("WA", (this._view[0] as City).StateName);
                Assert.AreEqual("WA", (this._view[1] as City).StateName);
                Assert.AreEqual("WA", (this._view[2] as City).StateName);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When calling load after adding a group decsriptor from other than the first page, the first page is loaded and the data is sorted")]
        public void AddingGroupDescriptorAndThenLoadingLoadsFirstPage()
        {
            this.LoadCities(3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.GroupDescriptors.Add(new GroupDescriptor("Name"));
                this._dds.Load();
            });

            this.AssertFirstPageLoadedSortedByName("After adding the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When calling refresh after adding a group decsriptor from other than the first page, the first page is loaded and the data is sorted")]
        public void AddingGroupDescriptorAndThenRefreshingLoadsFirstPage()
        {
            this.LoadCities(3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.GroupDescriptors.Add(new GroupDescriptor("Name"));
                this._collectionView.Refresh();
            });

            this.AssertFirstPageLoadedSortedByName("After adding the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When adding a group descriptor from other than the first page with AutoLoad set to true, the first page is loaded and the data is sorted")]
        public void AddingGroupDescriptorWithAutoLoadLoadsFirstPage()
        {
            this.LoadDomainDataSourceControl();
            this.LoadCities(3, true);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.GroupDescriptors.Add(new GroupDescriptor("Name"));
            });

            this.AssertFirstPageLoadedSortedByName("After adding the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When adding a group descriptor from other than the first page within a defer load, the first page is loaded and the data is sorted")]
        public void AddingGroupDescriptorWithinDeferLoadLoadsFirstPage()
        {
            this.LoadCities(3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._dds.DeferLoad())
                {
                    this._dds.GroupDescriptors.Add(new GroupDescriptor("Name"));
                }
            });

            this.AssertFirstPageLoadedSortedByName("After adding the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When adding a group descriptor from other than the first page within a defer refresh, the first page is loaded and the data is sorted")]
        public void AddingGroupDescriptorWithinDeferRefreshLoadsFirstPage()
        {
            this.LoadCities(3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._collectionView.DeferRefresh())
                {
                    this._dds.GroupDescriptors.Add(new GroupDescriptor("Name"));
                }
            });

            this.AssertFirstPageLoadedSortedByName("After adding the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When calling load after editing a group descriptor from other than the first page, the first page is loaded and the data is sorted")]
        public void EditingGroupDescriptorAndThenLoadingLoadsFirstPage()
        {
            this.LoadGroupedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.GroupDescriptors[0].PropertyPath = "Name";
                this._dds.Load();
            });

            this.AssertFirstPageLoadedSortedByName("After editing the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When calling refresh after editing a group descriptor from other than the first page, the first page is loaded and the data is sorted")]
        public void EditingGroupDescriptorAndThenRefreshingLoadsFirstPage()
        {
            this.LoadGroupedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.GroupDescriptors[0].PropertyPath = "Name";
                this._collectionView.Refresh();
            });
            this.AssertFirstPageLoadedSortedByName("After editing the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When editing a group descriptor from other than the first page with AutoLoad set to true, the first page is loaded and the data is sorted")]
        public void EditingGroupDescriptorWithAutoLoadLoadsFirstPage()
        {
            this.LoadDomainDataSourceControl();
            this.LoadGroupedCities("StateName", 3, true);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.GroupDescriptors[0].PropertyPath = "Name";
            });

            this.AssertFirstPageLoadedSortedByName("After editing the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When editing a group descriptor from other than the first page within a defer load, the first page is loaded and the data is sorted")]
        public void EditingGroupDescriptorWithinDeferLoadLoadsFirstPage()
        {
            this.LoadGroupedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._dds.DeferLoad())
                {
                    this._dds.GroupDescriptors[0].PropertyPath = "Name";
                }
            });

            this.AssertFirstPageLoadedSortedByName("After editing the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When editing a group descriptor from other than the first page within a defer refresh, the first page is loaded and the data is sorted")]
        public void EditingGroupDescriptorWithinDeferRefreshLoadsFirstPage()
        {
            this.LoadGroupedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._collectionView.DeferRefresh())
                {
                    this._dds.GroupDescriptors[0].PropertyPath = "Name";
                }
            });

            this.AssertFirstPageLoadedSortedByName("After editing the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When calling load after removing a group descriptor from other than the first page, the first page is loaded and the data is sorted")]
        public void RemovingGroupDescriptorAndThenLoadingLoadsFirstPage()
        {
            this.LoadGroupedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.GroupDescriptors.RemoveAt(0);
                this._dds.Load();
            });

            this.AssertFirstPageLoaded("After removing the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When calling refresh after removing a group descriptor from other than the first page, the first page is loaded and the data is sorted")]
        public void RemovingGroupDescriptorAndThenRefreshingLoadsFirstPage()
        {
            this.LoadGroupedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.GroupDescriptors.RemoveAt(0);
                this._collectionView.Refresh();
            });

            this.AssertFirstPageLoaded("After removing the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When removing a group descriptor from other than the first page with AutoLoad set to true, the first page is loaded and the data is sorted")]
        public void RemovingGroupDescriptorWithAutoLoadLoadsFirstPage()
        {
            this.LoadDomainDataSourceControl();
            this.LoadGroupedCities("StateName", 3, true);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.GroupDescriptors.RemoveAt(0);
            });

            this.AssertFirstPageLoaded("After removing the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When removing a group descriptor from other than the first page within a defer load, the first page is loaded and the data is sorted")]
        public void RemovingGroupDescriptorWithinDeferLoadLoadsFirstPage()
        {
            this.LoadGroupedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._dds.DeferLoad())
                {
                    this._dds.GroupDescriptors.RemoveAt(0);
                }
            });

            this.AssertFirstPageLoaded("After removing the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When removing a group descriptor from other than the first page within a defer refresh, the first page is loaded and the data is sorted")]
        public void RemovingGroupDescriptorWithinDeferRefreshLoadsFirstPage()
        {
            this.LoadGroupedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._collectionView.DeferRefresh())
                {
                    this._dds.GroupDescriptors.RemoveAt(0);
                }
            });

            this.AssertFirstPageLoaded("After removing the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When calling load after adding a group description from other than the first page, the first page is loaded and the data is sorted")]
        public void AddingGroupDescriptionAndThenLoadingLoadsFirstPage()
        {
            this.LoadCities(3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._collectionView.GroupDescriptions.Add(new PropertyGroupDescription("Name"));
                this._dds.Load();
            });

            this.AssertFirstPageLoadedSortedByName("After adding the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When calling refresh after adding a group description from other than the first page, the first page is loaded and the data is sorted")]
        public void AddingGroupDescriptionAndThenRefreshingLoadsFirstPage()
        {
            this.LoadCities(3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._collectionView.GroupDescriptions.Add(new PropertyGroupDescription("Name"));
                this._collectionView.Refresh();
            });

            this.AssertFirstPageLoadedSortedByName("After adding the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When adding a group description from other than the first page with AutoLoad set to true, the first page is loaded and the data is sorted")]
        public void AddingGroupDescriptionWithAutoLoadLoadsFirstPage()
        {
            this.LoadDomainDataSourceControl();
            this.LoadCities(3, true);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._collectionView.GroupDescriptions.Add(new PropertyGroupDescription("Name"));
            });

            this.AssertFirstPageLoadedSortedByName("After adding the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When adding a group description from other than the first page within a defer load, the first page is loaded and the data is sorted")]
        public void AddingGroupDescriptionWithinDeferLoadLoadsFirstPage()
        {
            this.LoadCities(3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._dds.DeferLoad())
                {
                    this._collectionView.GroupDescriptions.Add(new PropertyGroupDescription("Name"));
                }
            });

            this.AssertFirstPageLoadedSortedByName("After adding the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When adding a group description from other than the first page within a defer refresh, the first page is loaded and the data is sorted")]
        public void AddingGroupDescriptionWithinDeferRefreshLoadsFirstPage()
        {
            this.LoadCities(3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._collectionView.DeferRefresh())
                {
                    this._collectionView.GroupDescriptions.Add(new PropertyGroupDescription("Name"));
                }
            });

            this.AssertFirstPageLoadedSortedByName("After adding the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When calling load after editing a group description from other than the first page, the first page is loaded and the data is sorted")]
        public void EditingGroupDescriptionAndThenLoadingLoadsFirstPage()
        {
            this.LoadGroupedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                ((PropertyGroupDescription)this._collectionView.GroupDescriptions[0]).PropertyName = "Name";
                this._dds.Load();
            });

            this.AssertFirstPageLoadedSortedByName("After editing the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When calling refresh after editing a group description from other than the first page, the first page is loaded and the data is sorted")]
        public void EditingGroupDescriptionAndThenRefreshingLoadsFirstPage()
        {
            this.LoadGroupedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                ((PropertyGroupDescription)this._collectionView.GroupDescriptions[0]).PropertyName = "Name";
                this._collectionView.Refresh();
            });

            this.AssertFirstPageLoadedSortedByName("After editing the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When editing a group description from other than the first page with AutoLoad set to true, the first page is loaded and the data is sorted")]
        public void EditingGroupDescriptionWithAutoLoadLoadsFirstPage()
        {
            this.LoadDomainDataSourceControl();
            this.LoadGroupedCities("StateName", 3, true);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                ((PropertyGroupDescription)this._collectionView.GroupDescriptions[0]).PropertyName = "Name";
            });

            this.AssertFirstPageLoadedSortedByName("After editing the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When editing a group description from other than the first page within a defer load, the first page is loaded and the data is sorted")]
        public void EditingGroupDescriptionWithinDeferLoadLoadsFirstPage()
        {
            this.LoadGroupedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._dds.DeferLoad())
                {
                    ((PropertyGroupDescription)this._collectionView.GroupDescriptions[0]).PropertyName = "Name";
                }
            });

            this.AssertFirstPageLoadedSortedByName("After editing the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When editing a group description from other than the first page within a defer refresh, the first page is loaded and the data is sorted")]
        public void EditingGroupDescriptionWithinDeferRefreshLoadsFirstPage()
        {
            this.LoadGroupedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._collectionView.DeferRefresh())
                {
                    ((PropertyGroupDescription)this._collectionView.GroupDescriptions[0]).PropertyName = "Name";
                }
            });

            this.AssertFirstPageLoadedSortedByName("After editing the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When calling load after removing a group description from other than the first page, the first page is loaded")]
        public void RemovingGroupDescriptionAndThenLoadingLoadsFirstPage()
        {
            this.LoadGroupedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._collectionView.GroupDescriptions.RemoveAt(0);
                this._dds.Load();
            });

            this.AssertFirstPageLoaded("After removing the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When calling refresh after removing a group description from other than the first page, the first page is loaded")]
        public void RemovingGroupDescriptionAndThenRefreshingLoadsFirstPage()
        {
            this.LoadGroupedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._collectionView.GroupDescriptions.RemoveAt(0);
                this._collectionView.Refresh();
            });

            this.AssertFirstPageLoaded("After removing the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When removing a group description from other than the first page with AutoLoad set to true, the first page is loaded")]
        public void RemovingGroupDescriptionWithAutoLoadLoadsFirstPage()
        {
            this.LoadDomainDataSourceControl();
            this.LoadGroupedCities("StateName", 3, true);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._collectionView.GroupDescriptions.RemoveAt(0);
            });

            this.AssertFirstPageLoaded("After removing the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When removing a group description from other than the first page within a defer load, the first page is loaded")]
        public void RemovingGroupDescriptionWithinDeferLoadLoadsFirstPage()
        {
            this.LoadGroupedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._dds.DeferLoad())
                {
                    this._collectionView.GroupDescriptions.RemoveAt(0);
                }
            });

            this.AssertFirstPageLoaded("After removing the group");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(748183)]
        [Description("When removing a group description from other than the first page within a defer refresh, the first page is loaded")]
        public void RemovingGroupDescriptionWithinDeferRefreshLoadsFirstPage()
        {
            this.LoadGroupedCities("StateName", 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._collectionView.DeferRefresh())
                {
                    this._collectionView.GroupDescriptions.RemoveAt(0);
                }
            });

            this.AssertFirstPageLoaded("After removing the group");

            EnqueueTestComplete();
        }

        #endregion Paging Enabled

        #region Progressive Loading Enabled

        [TestMethod]
        [Asynchronous]
        [WorkItem(812133)]
        [Description("Can group while progressive loading is enabled")]
        public void GroupingWithProgressiveLoading()
        {
            int totalCityCount = new CityData().Cities.Count;

            EnqueueCallback(() =>
            {
                // By using a loadsize of 1, we know the number of loads to be performed is equal to the
                // total city count + 1 (the load that will determine that there are no more records).
                this._dds.LoadSize = 1;
                this._dds.QueryName = "GetCities";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.GroupDescriptors.Add(new GroupDescriptor("Name"));
                this._asyncEventFailureMessage = "First Progressive Load";
                this._dds.Load();
            });

            this.AssertLoadingData(totalCityCount + 1, true);

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                Assert.AreEqual<int>(totalCityCount, this._view.Count, "The count should match the total city count after allowing the first progressive load to finish");
                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.Name), ListSortDirection.Ascending, "Cities should be sorted by Name after the first progressive load");

                this._dds.GroupDescriptors[0].PropertyPath = "StateName";

                // Calling Refresh will test that the load of FirstItems is deferred properly
                this._collectionView.Refresh();
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
        /// Load the cities with grouping by <see cref="City.StateName"/>,
        /// <see cref="City.CountyName"/>, and then sorting by <see cref="City.Name"/>.
        /// </summary>
        /// <remarks>
        /// Will assert that the grouping and sorting after the initial load was correct.
        /// </remarks>
        private void LoadCitiesGroupedByStateThenCounty()
        {
            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.GroupDescriptors.Add(new GroupDescriptor("StateName"));
                this._dds.GroupDescriptors.Add(new GroupDescriptor("CountyName"));
                this._dds.SortDescriptors.Add(new SortDescriptor("Name", ListSortDirection.Ascending));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.AssertStateCountyCitySorting("After the Initial Load");
            });
        }

        /// <summary>
        /// Assert that the cities in the view are sorted by state name, county name, and city name (for nested grouping tests).
        /// </summary>
        /// <param name="message">The message to append to the assert statements.</param>
        private void AssertStateCountyCitySorting(string message)
        {
            IEnumerable<City> cities = this._view.Cast<City>();

            AssertHelper.AssertSequenceSorting(cities.Select(c => c.StateName), ListSortDirection.Ascending, "State sorting. " + message);

            foreach (string state in cities.Select(c => c.StateName))
            {
                IEnumerable<string> counties = cities.Where(c => c.StateName == state).Select(c => c.CountyName);
                AssertHelper.AssertSequenceSorting(counties, ListSortDirection.Ascending, "County sorting for " + state + ". " + message);

                foreach (string county in counties)
                {
                    AssertHelper.AssertSequenceSorting(cities.Where(c => c.StateName == state && c.CountyName == county).Select(c => c.Name), ListSortDirection.Ascending, "City sorting for " + county + ", " + state + ". " + message);
                }
            }

            IEnumerable<CollectionViewGroup> firstLevelGroups = this._collectionView.Groups.Cast<CollectionViewGroup>();
            AssertHelper.AssertSequenceSorting(firstLevelGroups.Select(g => g.Name.ToString()), ListSortDirection.Ascending, "First level group sorting." + message);

            foreach (CollectionViewGroup firstLevelGroup in firstLevelGroups)
            {
                IEnumerable<CollectionViewGroup> secondLevelGroups = firstLevelGroup.Items.Cast<CollectionViewGroup>();
                AssertHelper.AssertSequenceSorting(secondLevelGroups.Select(g => g.Name.ToString()), ListSortDirection.Ascending, "Second level group sorting within " + firstLevelGroup.Name.ToString() + ". " + message);

                foreach (CollectionViewGroup secondLevelGroup in secondLevelGroups)
                {
                    IEnumerable<City> groupCities = secondLevelGroup.Items.Cast<City>();
                    AssertHelper.AssertSequenceSorting(groupCities.Select(c => c.Name), ListSortDirection.Ascending, "City storting within " + secondLevelGroup.Name.ToString() + ". " + message);
                }
            }
        }

        /// <summary>
        /// Enqueue the necessary calls to load the cities, grouped by the specified property,
        /// and with the specified page size and auto load properties.
        /// </summary>
        /// <param name="sortProperty">The property to group the cities by.</param>
        /// <param name="pageSize">The <see cref="DomainDataSource.PageSize"/> to use.</param>
        /// <param name="autoLoad">What to set <see cref="DomainDataSource.AutoLoad"/> to.</param>
        private void LoadGroupedCities(string groupProperty, int pageSize, bool autoLoad)
        {
            EnqueueCallback(() =>
            {
                this._collectionView.GroupDescriptions.Add(new PropertyGroupDescription(groupProperty));
            });

            this.LoadCities(pageSize, autoLoad);
        }

        #endregion Helper Methods
    }
}
