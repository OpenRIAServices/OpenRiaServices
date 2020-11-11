extern alias SSmDsClient;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Cities;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using TestDomainServices;
using TestDomainServices.NamedUpdates;

namespace OpenRiaServices.Client.Test
{
    using Resource = SSmDsClient::OpenRiaServices.Client.Resource;

    [TestClass]
    public class InheritanceTests : UnitTestBase
    {
        // entity actions that don't exist on CityDomainContext
        private readonly EntityAction _approve = new EntityAction("Approve");
        private readonly EntityAction _reject = new EntityAction("Reject", "order not correctly filled out");

        // entity actions that actually exist on CityDomainContext
        private readonly EntityAction _assignCityZone = new EntityAction("AssignCityZone", "Zone1");
        private readonly EntityAction _autoAssignCityZone = new EntityAction("AutoAssignCityZone");
        private readonly EntityAction _reassignZip = new EntityAction("ReassignZip", 100, true);

        // entity actions that exist only for CityWityInfo entities
        private readonly EntityAction _setCityInfo = new EntityAction("SetCityInfo", "someInfo");

        private List<City> _cities;
        private List<CityWithInfo> _citiesWithInfo;
        private IList<City> _rootCities;

        // TODOs:
        // AcceptChanges and Invoke (plus property change notifications)

        [TestInitialize]
        public void TestInitialize()
        {
            CityData cityData = new CityData();
            _cities = cityData.Cities;
            _citiesWithInfo = cityData.CitiesWithInfo;

            // Root cities eliminates any derived types
            _rootCities = _cities.Where(c => c.GetType() == typeof(City)).ToList();
        }

        [TestMethod]
        [Description("Verify that we support specialized associations on derived entity types")]
        public void Inherit_Run_Entity_SpecializedAssociation()
        {
            TestEntityContainer container = new TestEntityContainer();
            var cities = container.LoadEntities(_citiesWithInfo).OfType<CityWithInfo>();

            var redmond = cities.Single(c => c.Name == "Redmond");
            Assert.AreEqual(3, redmond.ZipCodes.Count);
            Assert.AreEqual(1, redmond.ZipCodesWithInfo.Count);
            Assert.AreEqual("Microsoft", redmond.ZipCodesWithInfo.Single().Info);
        }

        #region Custom methods
        [TestMethod]
        [Description("Verify internal implementation of Invoke and CanInvoke on loaded derived entities")]
        public void Inherit_Run_Entity_InvokeOnLoaded()
        {
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_citiesWithInfo);
            Assert.AreEqual(EntityState.Unmodified, _citiesWithInfo[0].EntityState);
            Assert.AreEqual(0, _citiesWithInfo[0].EntityActions.Count());
            Assert.IsTrue(_citiesWithInfo[0].CanInvokeAction(_setCityInfo.Name));

            // verify invoke on a new entity succeeds and subsequent CanInvoke returns false
            _citiesWithInfo[0].InvokeAction(_setCityInfo.Name, _setCityInfo.Parameters.ToArray<object>());
            Assert.AreEqual(1, _citiesWithInfo[0].EntityActions.Count());
            Assert.AreEqual(EntityState.Modified, _citiesWithInfo[0].EntityState);
            Assert.AreEqual<string>(_setCityInfo.Name, _citiesWithInfo[0].EntityActions.Single().Name);
            Assert.AreEqual<int>(_setCityInfo.Parameters.Count(), _citiesWithInfo[0].EntityActions.Single().Parameters.Count());
            Assert.IsFalse(_citiesWithInfo[0].CanInvokeAction(_setCityInfo.Name));
        }

        [TestMethod]
        [Description("Verify that after invoking a domain method, the derived entity is read-only.")]
        public void Inherit_Run_Entity_ReadOnly()
        {
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_citiesWithInfo);

            Assert.IsFalse(_citiesWithInfo[0].IsReadOnly);
            _citiesWithInfo[0].SetCityInfo("new stuff");
            Assert.IsTrue(_citiesWithInfo[0].IsReadOnly);

            ((System.ComponentModel.IRevertibleChangeTracking)_cities[0]).RejectChanges();

            Assert.IsFalse(_citiesWithInfo[1].IsReadOnly);
        }

        [TestMethod]
        [Description("Calling Entity.Invoke with entity action available only on derived entity type throws")]
        public void Inherit_Run_Entity_Invoke_Illegal_EntityAction()
        {
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_rootCities);
            var invocableCity = _rootCities[0];
            Assert.IsNotNull(invocableCity);

            // verify calling Invoke throws InvalidOperationException and the invocation list remains the same
            ExceptionHelper.ExpectException<MissingMethodException>(delegate
            {
                invocableCity.InvokeAction(_setCityInfo.Name, _setCityInfo.Parameters.ToArray());
            }, string.Format(Resource.ValidationUtilities_MethodNotFound, "Cities.City", _setCityInfo.Name, _setCityInfo.Parameters.Count(), "'System.String'"));

            Assert.AreEqual(EntityState.Unmodified, _rootCities[0].EntityState);
        }

        [TestMethod]
        [Asynchronous]
        [Description("Call a custom method declared on the base entity through a derived entity")]
        public void Inherit_Run_Call_Base_Custom_Method_On_Derived_Entity()
        {
            EntityChangeSet changeset;
            List<string> propChanged = new List<string>();
            CityDomainContext citiesProvider = new CityDomainContext(TestURIs.Cities);

            LoadOperation lo = citiesProvider.Load(citiesProvider.GetCitiesQuery());
            SubmitOperation so = null;

            CityWithInfo cityWithInfo = null;

            // wait for Load to complete, then invoke some domain methods
            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                changeset = citiesProvider.EntityContainer.GetChanges();
                Assert.IsTrue(changeset.IsEmpty);

                cityWithInfo = citiesProvider.Cities.OfType<CityWithInfo>().FirstOrDefault();
                Assert.IsNotNull(cityWithInfo, "Expected to find a CityWithInfo type in entity list");

                cityWithInfo.PropertyChanged += delegate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                {
                    // We filter to only those properties we see in the City hierarchy
                    BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
                    bool isCityProperty = (typeof(CityWithInfo).GetProperty(e.PropertyName, flags) != null ||
                                           typeof(CityWithEditHistory).GetProperty(e.PropertyName, flags) != null ||
                                           typeof(City).GetProperty(e.PropertyName, flags) != null);
                    if (isCityProperty)
                    {
                        propChanged.Add(e.PropertyName);
                    }
                };

                Assert.IsTrue(cityWithInfo.CanAssignCityZone);
                cityWithInfo.AssignCityZone("Zone15");
            });

            // wait for prop changed for domain method guards
            EnqueueConditional(() => propChanged.Count > 0);
            EnqueueCallback(delegate
            {
                Assert.IsTrue(propChanged.Contains("CanAssignCityZone"));
                propChanged.Clear();

                changeset = citiesProvider.EntityContainer.GetChanges();
                Assert.AreEqual(1, changeset.ModifiedEntities.Count);
                so = citiesProvider.SubmitChanges();

                Assert.AreEqual(1, so.ChangeSet.ModifiedEntities.Count);
                Assert.AreEqual(0, so.ChangeSet.AddedEntities.Count);
                Assert.AreEqual(0, so.ChangeSet.RemovedEntities.Count);
            });
            // wait for submit to complete, then verify invokedEntities in changeset
            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                if (so.Error != null)
                {
                    Assert.Fail(so.Error.Message);
                }
                Assert.AreEqual(1, so.ChangeSet.ModifiedEntities.Count);
                // verify we got the property change notification for the city entity as a result of autosync
                Assert.AreEqual(14, propChanged.Count, "Received different property notifications than expected:\r\n" + string.Join(",", propChanged.ToArray()));
                Assert.AreEqual(1, propChanged.Count(prop => prop == "EditHistory"));
                Assert.AreEqual(1, propChanged.Count(prop => prop == "ZoneName"));
                Assert.AreEqual(1, propChanged.Count(prop => prop == "ZoneID"));
                Assert.AreEqual(2, propChanged.Count(prop => prop == "CanAssignCityZone"));
                Assert.AreEqual(1, propChanged.Count(prop => prop == "IsAssignCityZoneInvoked"));
                Assert.AreEqual(2, propChanged.Count(prop => prop == "CanAutoAssignCityZone"));
                Assert.AreEqual(2, propChanged.Count(prop => prop == "CanAssignCityZoneIfAuthorized"));
                Assert.AreEqual(2, propChanged.Count(prop => prop == "CanSetCityInfo"));
                Assert.AreEqual(2, propChanged.Count(prop => prop == "CanTouchHistory"));

                // verify entities are auto-synced back to the client as a result of the domain method execution on server
                Assert.AreEqual(15, citiesProvider.Cities.Single<City>(c => (c.ZoneName == "Zone15")).ZoneID);

                // verify unchanged entities
                Assert.AreEqual(0, citiesProvider.Cities.First(c => (c.ZoneName == null)).ZoneID);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Call a derived custom method on the derived entity that declares it")]
        public void Inherit_Run_Call_Derived_Custom_Method()
        {
            EntityChangeSet changeset;
            List<string> propChanged = new List<string>();
            CityDomainContext citiesProvider = new CityDomainContext(TestURIs.Cities);
            CityWithInfo cityWithInfo = null;
            DateTime priorLastUpdated = DateTime.Now;

            LoadOperation lo = citiesProvider.Load(citiesProvider.GetCitiesWithInfoQuery());
            SubmitOperation so = null;

            // wait for Load to complete, then invoke some domain methods
            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                cityWithInfo = citiesProvider.Cities.FirstOrDefault() as CityWithInfo;
                Assert.IsNotNull(cityWithInfo, "Cities[0] should have been CityWithInfo but was " + citiesProvider.Cities.First().GetType().Name); 
                changeset = citiesProvider.EntityContainer.GetChanges();
                Assert.IsTrue(changeset.IsEmpty);
                cityWithInfo.PropertyChanged += delegate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                {
                    // We filter to only those properties we see in the City hierarchy
                    BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
                    bool isCityProperty = (typeof(CityWithInfo).GetProperty(e.PropertyName, flags) != null ||
                                           typeof(CityWithEditHistory).GetProperty(e.PropertyName, flags) != null ||
                                           typeof(City).GetProperty(e.PropertyName, flags) != null);
                    if (isCityProperty)
                    {
                        propChanged.Add(e.PropertyName);
                    }
                };

                priorLastUpdated = cityWithInfo.LastUpdated;
                Assert.IsTrue(cityWithInfo.CanSetCityInfo);
                cityWithInfo.SetCityInfo("new city info");
            });

            // wait for prop changed for domain method guards
            EnqueueConditional(() => propChanged.Count > 0);

            // Inject small delay so DateTime.Now executed on server is later than value in cithWithInfo
            EnqueueDelay(50);

            // Test validation that we will, in fact, get a different time stamp
            EnqueueCallback(delegate
            {
                Assert.AreNotEqual(priorLastUpdated, DateTime.Now, "Expected difference in times after small delay");
            });

            EnqueueCallback(delegate
            {
                Assert.IsTrue(propChanged.Contains("CanSetCityInfo"));
                propChanged.Clear();

                changeset = citiesProvider.EntityContainer.GetChanges();
                Assert.AreEqual(1, changeset.ModifiedEntities.Count);

                so = citiesProvider.SubmitChanges();

                Assert.AreEqual(1, so.ChangeSet.ModifiedEntities.Count);
                Assert.AreEqual(0, so.ChangeSet.AddedEntities.Count);
                Assert.AreEqual(0, so.ChangeSet.RemovedEntities.Count);
             });
            // wait for submit to complete, then verify invokedEntities in changeset
            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                Assert.IsNull(so.Error);
                Assert.AreEqual(1, so.ChangeSet.ModifiedEntities.Count);

                // verify we got the property change notification for the city entity as a result of autosync
                Assert.AreEqual(14, propChanged.Count, "Received different property notifications than expected:\r\n" + string.Join(",", propChanged.ToArray()));
                Assert.AreEqual(1, propChanged.Count(prop => prop =="EditHistory"));
                Assert.AreEqual(1, propChanged.Count(prop => prop =="Info"));
                Assert.AreEqual(1, propChanged.Count(prop => prop =="LastUpdated"));
                Assert.AreEqual(2, propChanged.Count(prop => prop =="CanAssignCityZone"));
                Assert.AreEqual(2, propChanged.Count(prop => prop =="CanAssignCityZoneIfAuthorized"));
                Assert.AreEqual(2, propChanged.Count(prop => prop =="CanAutoAssignCityZone"));
                Assert.AreEqual(2, propChanged.Count(prop => prop =="CanTouchHistory"));
                Assert.AreEqual(1, propChanged.Count(prop => prop =="IsSetCityInfoInvoked"));
                Assert.AreEqual(2, propChanged.Count(prop => prop =="CanSetCityInfo"));

                // verify entities are auto-synced back to the client as a result of the domain method execution on server
                CityWithInfo newCityWithInfo = citiesProvider.Cities.OfType<CityWithInfo>().SingleOrDefault<CityWithInfo>(c => (c.Info.Equals( "new city info")));
                Assert.IsNotNull(newCityWithInfo, "Did not find modified CityWithInfo after the submit");
                Assert.AreNotEqual(newCityWithInfo.LastUpdated, priorLastUpdated, "Expected lastUpdated to be modified by submit");
                Assert.IsTrue(newCityWithInfo.EditHistory.Contains("info=new city info"), "EditHistory was" + newCityWithInfo.EditHistory);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Call a custom method declared on the abstract base of a derived entity")]
        public void Inherit_Run_Call_Derived_Custom_Method_On_Abstract_Base_()
        {
            // Inheritance is City <-- CityWithEditHistory <-- CityWithInfo
            // This test invokes a custom method declared on CityWithEditHistory via a CityWithInfo instance
            EntityChangeSet changeset;
            List<string> propChanged = new List<string>();
            CityDomainContext citiesProvider = new CityDomainContext(TestURIs.Cities);
            CityWithInfo cityWithInfo = null;
            DateTime priorLastUpdated = DateTime.Now;

            LoadOperation lo = citiesProvider.Load(citiesProvider.GetCitiesWithInfoQuery());
            SubmitOperation so = null;

            // wait for Load to complete, then invoke some domain methods
            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                cityWithInfo = citiesProvider.Cities.FirstOrDefault() as CityWithInfo;
                Assert.IsNotNull(cityWithInfo, "Cities[0] should have been CityWithInfo but was " + citiesProvider.Cities.First().GetType().Name);
                changeset = citiesProvider.EntityContainer.GetChanges();
                Assert.IsTrue(changeset.IsEmpty);
                cityWithInfo.PropertyChanged += delegate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                {
                    // We filter to only those properties we see in the City hierarchy
                    BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
                    bool isCityProperty = (typeof(CityWithInfo).GetProperty(e.PropertyName, flags) != null ||
                                           typeof(CityWithEditHistory).GetProperty(e.PropertyName, flags) != null ||
                                           typeof(City).GetProperty(e.PropertyName, flags) != null);
                    if (isCityProperty)
                    {
                        propChanged.Add(e.PropertyName);
                    }
                };

                priorLastUpdated = cityWithInfo.LastUpdated;
                Assert.IsTrue(cityWithInfo.CanTouchHistory);
                cityWithInfo.TouchHistory("xxx");
            });

            // wait for prop changed for domain method guards
            EnqueueConditional(() => propChanged.Count > 0);

            // Inject small delay so DateTime.Now executed on server is later than value in cithWithInfo
            EnqueueDelay(50);

            // Test validation that we will, in fact, get a different time stamp
            EnqueueCallback(delegate
            {
                Assert.AreNotEqual(priorLastUpdated, DateTime.Now, "Expected difference in times after small delay");
            });

            EnqueueCallback(delegate
            {
                Assert.IsTrue(propChanged.Contains("CanTouchHistory"));
                propChanged.Clear();

                changeset = citiesProvider.EntityContainer.GetChanges();
                Assert.AreEqual(1, changeset.ModifiedEntities.Count);

                so = citiesProvider.SubmitChanges();

                Assert.AreEqual(1, so.ChangeSet.ModifiedEntities.Count);
                Assert.AreEqual(0, so.ChangeSet.AddedEntities.Count);
                Assert.AreEqual(0, so.ChangeSet.RemovedEntities.Count);
            });
            // wait for submit to complete, then verify invokedEntities in changeset
            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                Assert.IsNull(so.Error);
                Assert.AreEqual(1, so.ChangeSet.ModifiedEntities.Count);

                // verify we got the property change notification for the city entity as a result of autosync
                Assert.AreEqual(13, propChanged.Count, "Received different property notifications than expected:\r\n" + string.Join(",", propChanged.ToArray()));
                Assert.AreEqual(1, propChanged.Count(prop => prop =="EditHistory"));
                Assert.AreEqual(1, propChanged.Count(prop => prop =="LastUpdated"));
                Assert.AreEqual(2, propChanged.Count(prop => prop =="CanAssignCityZone"));
                Assert.AreEqual(2, propChanged.Count(prop => prop =="CanAssignCityZoneIfAuthorized"));
                Assert.AreEqual(2, propChanged.Count(prop => prop =="CanAutoAssignCityZone"));
                Assert.AreEqual(2, propChanged.Count(prop => prop =="CanTouchHistory"));
                Assert.AreEqual(1, propChanged.Count(prop => prop =="IsTouchHistoryInvoked"));
                Assert.AreEqual(2, propChanged.Count(prop => prop =="CanSetCityInfo"));

                // verify entities are auto-synced back to the client as a result of the domain method execution on server
                CityWithInfo newCityWithInfo = citiesProvider.Cities.OfType<CityWithInfo>().SingleOrDefault<CityWithInfo>(c => (c.EditHistory.Contains("touch")));
                Assert.IsNotNull(newCityWithInfo, "Did not find modified CityWithInfo after the submit");
                Assert.AreNotEqual(newCityWithInfo.LastUpdated, priorLastUpdated, "Expected lastUpdated to be modified by submit");
                Assert.IsTrue(newCityWithInfo.EditHistory.Contains("touch=xxx"), "EditHistory was" + newCityWithInfo.EditHistory);
            });

            EnqueueTestComplete();
        }

        #endregion //Custom methods

        #region CUD
        [TestMethod]
        [Asynchronous]
        [Description("Insert a new derived entity and verify its CUD method was invoked")]
        public void Inherit_Run_CUD_Insert_Derived()
        {
            // Inheritance is City <-- CityWithEditHistory <-- CityWithInfo
            CityDomainContext citiesContext= new CityDomainContext(TestURIs.Cities);
            DateTime priorLastUpdated = DateTime.Now;

            // Load all cities, not just derived ones
            LoadOperation lo = citiesContext.Load(citiesContext.GetCitiesQuery());
            SubmitOperation so = null;

            // wait for Load to complete
            this.EnqueueCompletion(() => lo);

            EnqueueCallback(delegate
            {

                CityWithInfo newCity = new CityWithInfo() { Name = "CocoaVille", StateName = "WA", CountyName = "King", Info="stuff" };
                citiesContext.Cities.Add(newCity);

                so = citiesContext.SubmitChanges();
            });
            // wait for submit to complete
            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                if (so.Error != null)
                {
                    Assert.Fail("Unexpected error on submit: " + so.Error.Message);
                }

                // verify entities are auto-synced back to the client as a result of the domain method execution on server
                CityWithInfo newCity = citiesContext.Cities.OfType<CityWithInfo>().SingleOrDefault<CityWithInfo>(c => (c.Name == "CocoaVille"));
                Assert.IsNotNull(newCity, "Did not find modified City after the submit");
                Assert.IsTrue(newCity.EditHistory.Contains("insert"), "EditHistory was" + newCity.EditHistory);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Update a derived entity and verify its CUD method was invoked")]
        public void Inherit_Run_CUD_Update_Derived()
        {
            // Inheritance is City <-- CityWithEditHistory <-- CityWithInfo
            CityDomainContext citiesContext = new CityDomainContext(TestURIs.Cities);
            DateTime priorLastUpdated = DateTime.Now;

            // Load all cities, not just derived ones
            LoadOperation lo = citiesContext.Load(citiesContext.GetCitiesQuery());
            SubmitOperation so = null;
            CityWithInfo cityWithInfo = null;
            string originalName = null;
            string originalStateName = null;
            string originalCountyName = null;

            // wait for Load to complete
            this.EnqueueCompletion(() => lo);

            EnqueueCallback(delegate
            {

                cityWithInfo = citiesContext.Cities.OfType<CityWithInfo>().FirstOrDefault();
                Assert.IsNotNull(cityWithInfo, "expected to find at least one CityWithInfo entity");
                Assert.IsFalse(cityWithInfo.EditHistory.Contains("update"), "Did not expect edit history to be set yet.");

                originalName = cityWithInfo.Name;
                originalStateName = cityWithInfo.StateName;
                originalCountyName = cityWithInfo.CountyName;

                cityWithInfo.Info = "inserted new info";

                so = citiesContext.SubmitChanges();
            });
            // wait for submit to complete
            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                if (so.Error != null)
                {
                    Assert.Fail("Unexpected error on submit: " + so.Error.Message);
                }

                // verify entities are auto-synced back to the client as a result of the domain method execution on server
                CityWithInfo updatedCity = citiesContext.Cities.OfType<CityWithInfo>().SingleOrDefault<CityWithInfo>
                                                (c => (c.Name == originalName && 
                                                       c.StateName == originalStateName && 
                                                       c.CountyName == originalCountyName));
                Assert.IsNotNull(updatedCity, "Did not find modified City after the submit");
                Assert.IsTrue(updatedCity.EditHistory.Contains("update"), "EditHistory was" + updatedCity.EditHistory);
                Assert.AreEqual("inserted new info", updatedCity.Info, "Updated Info did not get applied");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Delete a derived entity and verify its CUD method was invoked")]
        public void Inherit_Run_CUD_Delete_Derived()
        {
            // Inheritance is City <-- CityWithEditHistory <-- CityWithInfo
            CityDomainContext citiesContext = new CityDomainContext(TestURIs.Cities);
            DateTime priorLastUpdated = DateTime.Now;
            LoadOperation lo = null;
            SubmitOperation so = null;
            CityWithInfo cityWithInfo = null;
            string originalName = null;
            string originalStateName = null;
            string originalCountyName = null;

            // Invoke service operation to clear out all static data
            // (we rely on static data for deleted cities so that it
            //  survives across queries)
            InvokeOperation invoke = citiesContext.ResetData(null, null);

            this.EnqueueCompletion(() => invoke);
            EnqueueCallback(delegate
            {
                if (invoke.Error != null)
                {
                    Assert.Fail("Failed on invoke of ResetData: " + invoke.Error.Message);
                }
            });

            EnqueueCallback(delegate
            {
                // Load all cities, not just derived ones
                lo = citiesContext.Load(citiesContext.GetCitiesQuery());
            });


            // wait for Load to complete
            this.EnqueueCompletion(() => lo);

            EnqueueCallback(delegate
            {

                cityWithInfo = citiesContext.Cities.OfType<CityWithInfo>().FirstOrDefault();
                Assert.IsNotNull(cityWithInfo, "expected to find at least one CityWithInfo entity");
                Assert.IsFalse(cityWithInfo.EditHistory.Contains("update"), "Did not expect edit history to be set yet.");

                originalName = cityWithInfo.Name;
                originalStateName = cityWithInfo.StateName;
                originalCountyName = cityWithInfo.CountyName;

                // Delete it.  Note that the delete CUD method in the CityDomainService
                // moves the deleted city over into DeletedCities so it can still be found
                citiesContext.Cities.Remove(cityWithInfo);

                so = citiesContext.SubmitChanges();
            });
            // wait for submit to complete
            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                if (so.Error != null)
                {
                    Assert.Fail("Unexpected error on submit: " + so.Error.Message);
                }

                // verify entities are auto-synced back to the client as a result of the domain method execution on server
                CityWithInfo deletedCity = citiesContext.Cities.OfType<CityWithInfo>().SingleOrDefault<CityWithInfo>
                                                (c => (c.Name == originalName &&
                                                       c.StateName == originalStateName &&
                                                       c.CountyName == originalCountyName));
                Assert.IsNull(deletedCity, "Did not expect to find deleted City after the submit");
                
                // Load the deleted cities (it was tombstoned)
                citiesContext.Cities.Clear();
                lo = citiesContext.Load(citiesContext.GetDeletedCitiesQuery());
            });

            // Wait for deleted city query to complete
            this.EnqueueCompletion(() => lo);

            EnqueueCallback(delegate
            {
                if (lo.Error != null)
                {
                    Assert.Fail("Unexpected error on load of deleted queries: " + lo.Error.Message);
                }

                // verify entities are auto-synced back to the client as a result of the domain method execution on server
                CityWithInfo deletedCity = citiesContext.Cities.OfType<CityWithInfo>().SingleOrDefault<CityWithInfo>
                                                (c => (c.Name == originalName &&
                                                       c.StateName == originalStateName &&
                                                       c.CountyName == originalCountyName));
                Assert.IsNotNull(deletedCity, "Expected to find deleted City in the tombstone list");
                Assert.IsTrue(deletedCity.EditHistory.Contains("delete"), "Expected edit history to show delete but it only shows: " + deletedCity.EditHistory);
            });

            EnqueueTestComplete();
        }



        #endregion // CUD

        #region Helpers
        public class TestEntityContainer : EntityContainer
        {
            public TestEntityContainer()
            {
                CreateEntitySet<City>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
                CreateEntitySet<Zip>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
                CreateEntitySet<State>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
                CreateEntitySet<County>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
            }
        }
        #endregion
    }
}
