using System;
using System.Collections.Generic;
using System.Linq;
using Cities;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.DomainServices.Client.Test
{
    [TestClass]
    public class CitiesDomainServiceTests : UnitTestBase
    {
        protected void After(Func<bool> condition)
        {
            EnqueueConditional(delegate() { return condition(); });
        }
        protected void Then(Action a)
        {
            EnqueueCallback(delegate() {a();});
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verifies that a custom host is used to host CityDomainService")]
        public void Cities_VerifyCustomHost()
        {
            CityDomainContext dp = new CityDomainContext(TestURIs.Cities);    // Abs URI so runs on desktop too

            InvokeOperation<bool> invokeOp = dp.UsesCustomHost(TestHelperMethods.DefaultOperationAction, null);

            EnqueueConditional(() => invokeOp.IsComplete);

            EnqueueCallback(() =>
            {
                if (invokeOp.Error != null)
                    Assert.Fail("InvokeOperation.Error: " + invokeOp.Error.Message);
                Assert.IsTrue(invokeOp.Value, "CityDomainService isn't using a custom host.");
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Verify that Enum Entity properties are handled properly by testing
        /// both query and update scenarios
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void Cities_LoadStates_TestEnums()
        {
            CityDomainContext dp = new CityDomainContext(TestURIs.Cities);

            SubmitOperation so = null;
            LoadOperation lo = dp.Load(dp.GetStatesQuery().Where(s => s.TimeZone == Cities.TimeZone.Pacific), false);

            EnqueueConditional(() => lo.IsComplete);

            EnqueueCallback(() =>
            {
                if (lo.Error != null)
                    Assert.Fail("LoadOperation.Error: " + lo.Error.Message);

                // verify the TimeZones were serialized to the client properly
                State state = dp.States.Single(p => p.Name == "WA");
                Assert.AreEqual(Cities.TimeZone.Pacific, state.TimeZone);

                Assert.IsFalse(dp.States.Any(p => p.Name == "OH"));

                // Now test update
                state.TimeZone = state.TimeZone = Cities.TimeZone.Central;
                Assert.AreEqual(EntityState.Modified, state.EntityState);

                EntityChangeSet cs = dp.EntityContainer.GetChanges();
                Assert.IsTrue(cs.ModifiedEntities.Contains(state));

                so = dp.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(() =>
            {
                TestHelperMethods.AssertOperationSuccess(so);
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Verify that Enum Entity properties are handled properly by testing
        /// both query and update scenarios
        /// </summary>
        [TestMethod]
        [Description("Loads states using a query method that takes a generated enum type")]
        [Asynchronous]
        public void Cities_LoadStates_TestEnums_Generated()
        {
            CityDomainContext dp = new CityDomainContext(TestURIs.Cities);

            SubmitOperation so = null;
            LoadOperation lo = dp.Load(dp.GetStatesInShippingZoneQuery(ShippingZone.Eastern), false);

            EnqueueConditional(() => lo.IsComplete);

            EnqueueCallback(() =>
            {
                if (lo.Error != null)
                    Assert.Fail("LoadOperation.Error: " + lo.Error.Message);

                // verify the TimeZones were serialized to the client properly
                State state = dp.States.Single(p => p.Name == "OH");
                Assert.AreEqual(Cities.ShippingZone.Eastern, state.ShippingZone);

                // Now test update
                state.ShippingZone = Cities.ShippingZone.Central;
                Assert.AreEqual(EntityState.Modified, state.EntityState);

                EntityChangeSet cs = dp.EntityContainer.GetChanges();
                Assert.IsTrue(cs.ModifiedEntities.Contains(state));

                so = dp.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(() =>
            {
                Assert.IsNull(so.Error);
            });

            EnqueueTestComplete();
        }


        [TestMethod]
        [Asynchronous]
        [Description("Simple load of all Cities from CityDomainContext")]
        public void Cities_TestLoad()
        {
            CityDomainContext dp = new CityDomainContext(TestURIs.Cities);    // Abs URI so runs on desktop too

            LoadOperation lo = dp.Load(dp.GetCitiesQuery(), false);

            EnqueueConditional(() => lo.IsComplete);

            EnqueueCallback(() => 
                {
                    if (lo.Error != null)
                        Assert.Fail("LoadOperation.Error: " + lo.Error.Message);
                    IEnumerable<City> expected = new CityData().Cities;
                    AssertSame(expected, dp.Cities);
                });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Simple load of all Cities from CityDomainContext")]
        public void Cities_TestLoad_Demo()
        {
            CityDomainContext dp = new CityDomainContext(TestURIs.Cities);    // Abs URI so runs on desktop too

            LoadOperation lo = dp.Load(dp.GetCitiesQuery(), false);

            After(() => lo.IsComplete);

            Then(() => {
                if (lo.Error != null)
                    Assert.Fail("LoadOperation.Error: " + lo.Error.Message);
            });

            Then(() => AssertSame(new CityData().Cities, dp.Cities));

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Load Cities but pass the state name as a parameter to the server")]
        public void Cities_Cities_In_State_Parameterized_Query()
        {
            CityDomainContext dp = new CityDomainContext(TestURIs.Cities);    // Abs URI so runs on desktop too

            LoadOperation lo = dp.Load(dp.GetCitiesInStateQuery("WA"), false);

            EnqueueConditional(() => lo.IsComplete);

            EnqueueCallback(() =>
            {
                if (lo.Error != null)
                    Assert.Fail("LoadOperation.Error: " + lo.Error.Message);
                IEnumerable<City> expected = new CityData().Cities.Where(c => c.StateName.Equals("WA"));
                AssertSame(expected, dp.Cities);

                // Validate a [Editable(false)] property deserialized properly
                foreach (City c in dp.Cities)
                    Assert.AreEqual(c.CountyName, c.CalculatedCounty);
           });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Ensure that if an empty string is passed for a string parameter, it flows
        /// all the way to the server DomainOperationEntry as an empty string
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void TestEmptyStringParameter()
        {
            CityDomainContext dp = new CityDomainContext(TestURIs.Cities);    

            LoadOperation lo = dp.Load(dp.GetCitiesInStateQuery(string.Empty), false);

            EnqueueConditional(() => lo.IsComplete);

            EnqueueCallback(() =>
            {
                if (lo.Error != null)
                    Assert.Fail("LoadOperation.Error: " + lo.Error.Message);
                Assert.AreEqual(0, dp.Cities.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Loads Cities using a query expression composed locally that runs on the server")]
        public void Cities_Cities_In_County_Serialized_Query()
        {
            CityDomainContext dp = new CityDomainContext(TestURIs.Cities);    // Abs URI so runs on desktop too

            // Pass the query to the server to select only cities in King county
            var cityQuery = dp.GetCitiesQuery().Where(c => c.CountyName == "King");
            LoadOperation lo = dp.Load(cityQuery, false);

            EnqueueConditional(() => lo.IsComplete);

            EnqueueCallback(() =>
            {
                if (lo.Error != null)
                    Assert.Fail("LoadOperation.Error: " + lo.Error.Message);
                IEnumerable<City> expected = new CityData().Cities.Where(c => c.CountyName == "King");
                AssertSame(expected, dp.Cities);
            });

            EnqueueTestComplete();
        }


        private void AssertSame(IEnumerable<City> expected, IEnumerable<City> actual)
        {
            Assert.AreEqual(expected.Count(), actual.Count(), "Local CityData has different number of results than query result");
            foreach (City c1 in expected)
            {
                City bestActual = null;
                foreach (City c2 in actual)
                {
                    if (c2.StateName.Equals(c1.StateName) && 
                        c2.Name.Equals(c1.Name) &&
                        c2.CountyName.Equals(c1.CountyName))
                    {
                        bestActual = c2;
                        break;
                    }
                }
                Assert.IsNotNull(bestActual, "Could not find city " + c1.Name + " in actual results");
            }
        }

        private static void EnsureContainsAll(IEnumerable<string> actual, IEnumerable<string> expected)
        {
            foreach (string s in expected)
                Assert.IsTrue(actual.Contains(s), "Expected to find " + s);
            Assert.AreEqual(expected.Count(), actual.Count());
        }
    }
}
