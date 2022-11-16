﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Hosting.Wcf;
using OpenRiaServices.Server.UnitTesting;

namespace OpenRiaServices.Server.Test
{
    using Description = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

    /// <summary>
    /// Unit tests for authorization against a DomainService with real data
    /// </summary>
    [TestClass]
    public class AuthorizationTests
    {
        public AuthorizationTests()
        {
        }

        [TestMethod]
        [Description("Validates DomainService authorization is denied and allowed using a mock user accessing queries.")]
        public async Task Authorization_MockUser()
        {
            CityDomainService cities = new CityDomainService();
            DomainServiceDescription serviceDescription = DomainServiceDescription.GetDescription(typeof(CityDomainService));
            DomainOperationEntry getZipsIfAuthenticated = serviceDescription.GetQueryMethod("GetZipsIfAuthenticated");
            DomainOperationEntry getZipsIfInRole = serviceDescription.GetQueryMethod("GetZipsIfInRole");

            // Validate a null principal is denied
            MockUser user = null;
            MockDataService dataService = new MockDataService();
            cities.Initialize(new WcfDomainServiceContext(dataService, user, DomainOperationType.Query));
            Exception expectedException = null;
            ServiceQueryResult<Zip> result;

            try
            {
                result = await cities.QueryAsync<Zip>(new QueryDescription(getZipsIfAuthenticated), CancellationToken.None);
            }
            catch (UnauthorizedAccessException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);

            Assert.AreEqual("Access to operation 'GetZipsIfAuthenticated' was denied.", expectedException.Message, "Expected standard deny message for null principal");

            // Validate a non-authenticated user is denied
            user = new MockUser("mathew");
            cities = new CityDomainService();
            cities.Initialize(new WcfDomainServiceContext(dataService, user, DomainOperationType.Query));

            expectedException = null;
            try
            {
                user.IsAuthenticated = false;
                result = await cities.QueryAsync<Zip>(new QueryDescription(getZipsIfAuthenticated), CancellationToken.None);
            }
            catch (UnauthorizedAccessException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);

            // we're authenticated, so this should succeed
            expectedException = null;
            user.IsAuthenticated = true;
            result = await cities.QueryAsync<Zip>(new QueryDescription(getZipsIfAuthenticated), CancellationToken.None);
            Assert.IsNotNull(result.Result);

            // authenticated, but not in role, so we should fail
            cities = new CityDomainService();
            expectedException = null;
            user = new MockUser("mathew", new string[] { "clerk" });
            user.IsAuthenticated = true;
            cities.Initialize(new WcfDomainServiceContext(dataService, user, DomainOperationType.Query));
            try
            {
                result = await cities.QueryAsync<Zip>(new QueryDescription(getZipsIfInRole), CancellationToken.None);
            }
            catch (UnauthorizedAccessException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);

            // authenticated and in role, so we should succeed
            cities = new CityDomainService();
            user = new MockUser("mathew", new string[] { "manager" });
            user.IsAuthenticated = true;
            cities.Initialize(new WcfDomainServiceContext(dataService, user, DomainOperationType.Query));
            result = await cities.QueryAsync<Zip>(new QueryDescription(getZipsIfInRole), CancellationToken.None);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        [Description("Accessing a query with a custom authorization attribute is denied and allowed appropriately")]
        public async Task Authorization_Custom_Authorization_On_Query()
        {
            CityDomainService cities = new CityDomainService();
            DomainServiceDescription serviceDescription = DomainServiceDescription.GetDescription(typeof(CityDomainService));
            DomainOperationEntry getZipsIfUser = serviceDescription.GetQueryMethod("GetZipsIfUser");

            // The attribute permits only a user named mathew to access the query
            MockUser user = new MockUser("NotZipGuy");
            MockDataService dataService = new MockDataService();
            cities.Initialize(new WcfDomainServiceContext(dataService, user, DomainOperationType.Query));

            // not authenticated should be denied cleanly because there is no user name
            Exception expectedException = null;
            System.Collections.IEnumerable result;
            try
            {
                user.IsAuthenticated = false;
                result = (await cities.QueryAsync<Zip>(new QueryDescription(getZipsIfUser), CancellationToken.None)).Result;
            }
            catch (UnauthorizedAccessException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);
            Assert.AreEqual("Only one user is authorized for this query, and it isn't you.", expectedException.Message, "Expected this custom authorization deny message for non-authenticated user.");
      
            // Authenticated, but still not the right user name -- should be denied
            cities = new CityDomainService();
            expectedException = null;
            user = new MockUser("NotZipGuy", new string[] { "clerk" });
            user.IsAuthenticated = true;
            cities.Initialize(new WcfDomainServiceContext(dataService, user, DomainOperationType.Query));
            try
            {
                result = (await cities.QueryAsync<Zip>(new QueryDescription(getZipsIfUser), CancellationToken.None))
                    .Result;
            }
            catch (UnauthorizedAccessException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);
            Assert.AreEqual("Only one user is authorized for this query, and it isn't you.", expectedException.Message, "Expected this custom authorization deny message for authenticated user with wrong name.");

            // authenticated and in with the right name -- should be allowed
            cities = new CityDomainService();
            user = new MockUser("ZipGuy");
            user.IsAuthenticated = true;
            cities.Initialize(new WcfDomainServiceContext(dataService, user, DomainOperationType.Query));
            var queryResult = await cities.QueryAsync<Zip>(new QueryDescription(getZipsIfUser), CancellationToken.None);
            Assert.IsNotNull(queryResult.Result);
            Assert.IsNull(queryResult.ValidationErrors);
            Assert.IsTrue(queryResult.Result.OfType<Zip>().Any(), "Expected non-zero number of zip codes returned");
        }

        [TestMethod]
        [Description("Attempting a CUD operation marked with a custom authorization attribute is denied and allowed appropriately")]
        public async Task Authorization_Custom_Authorization_On_CUD()
        {
            // Specifically, the City data is marked so that no one can delete a Zip code
            // from WA unless their user name is WAGuy
            MockUser notWaGuy = new MockUser("notWAGuy");
            notWaGuy.IsAuthenticated = true;

            DomainServiceDescription serviceDescription = DomainServiceDescription.GetDescription(typeof(CityDomainService));
            Zip zip = null;

            // First execute a query to get some zips
            DomainOperationEntry getZipsQuery = serviceDescription.GetQueryMethod("GetZips");
            DomainServiceContext ctxt;

            using (CityDomainService cities = new CityDomainService())
            {
                // Now prepare for a query to find a Zip in WA
                ctxt = new WcfDomainServiceContext(new MockDataService(), notWaGuy, DomainOperationType.Query);
                cities.Initialize(ctxt);
                IEnumerable result = (await cities.QueryAsync<Zip>(new QueryDescription(getZipsQuery), CancellationToken.None)).Result;

                zip = result.OfType<Zip>().FirstOrDefault(z => z.StateName == "WA");
                Assert.IsNotNull(zip, "Could not find a zip code in WA");
            }

            // Prepare a submit to delete this zip from a user who is not authorized
            using (CityDomainService cities = new CityDomainService())
            {
                // Now prepare for a query to find a Zip in WA
                ctxt = new WcfDomainServiceContext(new MockDataService(), notWaGuy, DomainOperationType.Submit);
                cities.Initialize(ctxt);

                // Prepare an attempt to delete this with a user whose name is not WAGuy
                // This should fail due to a custom auth attribute
                List<ChangeSetEntry> entries = new List<ChangeSetEntry>();

                ChangeSetEntry entry = new ChangeSetEntry(1, zip, zip, DomainOperation.Delete);
                entries.Add(entry);
                UnauthorizedAccessException exception = null;
                try
                {
                    await ChangeSetProcessor.ProcessAsync(cities, entries);
                }
                catch (UnauthorizedAccessException ex)
                {
                    exception = ex;
                }
                Assert.IsNotNull(exception, "Expected failure attempting to delete a zip from WA with inappropriate user name");
                Assert.AreEqual("Only one user can delete zip codes from that state, and it isn't you.", exception.Message);
            }

            // Now do that again but with a user who is WAGuy -- it should succeed
            using (CityDomainService cities = new CityDomainService())
            {
                MockUser waGuy = new MockUser("WAGuy");
                waGuy.IsAuthenticated = true;

                // Now try a submit where the user *is* Mathew to validate we succeed
                ctxt = new WcfDomainServiceContext(new MockDataService(), waGuy, DomainOperationType.Submit);
                cities.Initialize(ctxt);
                List<ChangeSetEntry> entries = new List<ChangeSetEntry>();

                ChangeSetEntry entry = new ChangeSetEntry(1, zip, zip, DomainOperation.Delete);
                entries.Add(entry);
                Exception exception = null;
                try
                {
                    await ChangeSetProcessor.ProcessAsync(cities, entries);
                }
                catch (UnauthorizedAccessException ex)
                {
                    exception = ex;
                }
                Assert.IsNull(exception, "Expected success attempting to delete a zip from WA with inappropriate user name");
            }
        }

        [TestMethod]
        [Description("Attempting a custom method operation marked with a custom authorization attribute is denied and allowed appropriately")]
        public async Task Authorization_Custom_Authorization_On_Custom_Update()
        {
            // Specifically, the City data is marked so that no one can delete a Zip code
            // from WA unless their user name is WAGuy
            MockUser notWaGuy = new MockUser("notWAGuy");
            notWaGuy.IsAuthenticated = true;

            DomainServiceDescription serviceDescription = DomainServiceDescription.GetDescription(typeof(CityDomainService));
            City city = null;

            // Execute a query to get a City from WA
            using (CityDomainService cities = new CityDomainService())
            {
                DomainOperationEntry getCitiesQuery = serviceDescription.GetQueryMethod("GetCities");

                // Now prepare for a query to find a Zip in WA
                DomainServiceContext ctxt = new WcfDomainServiceContext(new MockDataService(), notWaGuy, DomainOperationType.Query);
                cities.Initialize(ctxt);

                IEnumerable result = (await cities.QueryAsync<City>(new QueryDescription(getCitiesQuery), CancellationToken.None)).Result;

                city = result.OfType<City>().FirstOrDefault(z => z.StateName == "WA");
                Assert.IsNotNull(city, "Could not find a city in WA");
            }


            using (CityDomainService cities = new CityDomainService())
            {
                // Now prepare for a submit to invoke AssignCityZoneIfAuthorized as a named update method
                DomainServiceContext ctxt = new WcfDomainServiceContext(new MockDataService(), notWaGuy, DomainOperationType.Submit);
                cities.Initialize(ctxt);

                // Prepare an attempt to delete this with a user whose name is not WAGuy
                // This should fail due to a custom auth attribute
                List<ChangeSetEntry> entries = new List<ChangeSetEntry>();

                ChangeSetEntry entry = new ChangeSetEntry();
                entry.DomainOperationEntry = serviceDescription.GetCustomMethod(typeof(City), "AssignCityZoneIfAuthorized");
                entry.EntityActions = new EntityActionCollection { { "AssignCityZoneIfAuthorized", new object[] { "SomeZone" } } };
                entry.Operation = DomainOperation.Update;
                entry.Entity = city;
                entries.Add(entry);

                UnauthorizedAccessException exception = null;
                try
                {
                    await ChangeSetProcessor.ProcessAsync(cities, entries);
                }
                catch (UnauthorizedAccessException ex)
                {
                    exception = ex;
                }
                Assert.IsNotNull(exception, "Expected failure attempting to perform custom method on WA with inappropriate user name");
                Assert.AreEqual("Only one user is authorized to execute operation 'AssignCityZoneIfAuthorized', and it isn't you.", exception.Message);
            }

            // Now do that again but with a user who is WAGuy -- it should succeed
            using (CityDomainService cities = new CityDomainService())
            {
                MockUser waGuy = new MockUser("WAGuy");
                waGuy.IsAuthenticated = true;

                // Now prepare for a submit to invoke AssignCityZoneIfAuthorized as a named update method
                DomainServiceContext ctxt = new WcfDomainServiceContext(new MockDataService(), waGuy, DomainOperationType.Submit);
                cities.Initialize(ctxt);

                // Prepare an attempt to delete this with a user whose name is not WAGuy
                // This should fail due to a custom auth attribute

                // Prepare a submit to call the AssignCityZoneIfAuthorized with an unauthorized user
                List<ChangeSetEntry> entries = new List<ChangeSetEntry>();

                ChangeSetEntry entry = new ChangeSetEntry();
                entry.DomainOperationEntry = serviceDescription.GetCustomMethod(typeof(City), "AssignCityZoneIfAuthorized");
                entry.EntityActions = new EntityActionCollection { { "AssignCityZoneIfAuthorized", new object[] { "SomeZone" } } };
                entry.Operation = DomainOperation.Update;
                entry.Entity = city;
                entries.Add(entry);

                Exception exception = null;
                try
                {
                    await ChangeSetProcessor.ProcessAsync(cities, entries);
                }
                catch (UnauthorizedAccessException ex)
                {
                    exception = ex;
                }
                Assert.IsNull(exception, "Expected success attempting to delete a zip from WA with inappropriate user name");
            }
        }

        [TestMethod]
        [Description("Attempting an Invoke operation marked with a custom authorization attribute is denied and allowed appropriately")]
        public async Task Authorization_Custom_Authorization_On_Invoke()
        {
            // Specifically, the City data is marked so that no one can delete a Zip code
            // from WA unless their user name is WAGuy
            MockUser notWaGuy = new MockUser("notWAGuy");
            notWaGuy.IsAuthenticated = true;

            DomainServiceDescription serviceDescription = DomainServiceDescription.GetDescription(typeof(CityDomainService));
            DomainOperationEntry invokeOperation = serviceDescription.GetInvokeOperation("GetStateIfUser");
            Assert.IsNotNull(invokeOperation, "Could not locate GetStateIfUser Invoke operation");
            DomainOperationEntry getCitiesQuery = serviceDescription.GetQueryMethod("GetCities");

            City city = null;

            // Execute a query to get a City from WA
            using (CityDomainService cities = new CityDomainService())
            {
                // Now prepare for a query to find a Zip in WA
                DomainServiceContext ctxt = new WcfDomainServiceContext(new MockDataService(), notWaGuy, DomainOperationType.Query);
                cities.Initialize(ctxt);

                IEnumerable result = (await cities.QueryAsync<City>(new QueryDescription(getCitiesQuery), CancellationToken.None)).Result;

                city = result.OfType<City>().FirstOrDefault(z => z.StateName == "WA");
                Assert.IsNotNull(city, "Could not find a city in WA");
            }

            // Perform an invoke against a method that has a custom auth attribute requiring WaGuy
            // where the user is something else -- should be denied
            using (CityDomainService cities = new CityDomainService())
            {
                // Prepare an invoke to call a method that has a custom auth attribute
                DomainServiceContext ctxt = new WcfDomainServiceContext(new MockDataService(), notWaGuy, DomainOperationType.Invoke);
                cities.Initialize(ctxt);

                // verify that even top level exceptions go through
                // the OnError handler
                UnauthorizedAccessException expectedException = null;
                try
                {
                    // cause a domain service not initialized exception
                    await cities.InvokeAsync(new InvokeDescription(invokeOperation, new object[] { city }), CancellationToken.None);
                }
                catch (UnauthorizedAccessException e)
                {
                    expectedException = e;
                }

                Assert.IsNotNull(expectedException, "Expected Invoke to be denied");
                Assert.AreEqual("Access to operation 'GetStateIfUser' was denied.", expectedException.Message);
            }

            // Perform an invoke against a method that has a custom auth attribute requiring WaGuy
            // where the user is correct -- should be allowed
            using (CityDomainService cities = new CityDomainService())
            {
                MockUser waGuy = new MockUser("WAGuy");
                waGuy.IsAuthenticated = true;

                // Prepare an invoke to call a method that has a custom auth attribute
                DomainServiceContext ctxt = new WcfDomainServiceContext(new MockDataService(), waGuy, DomainOperationType.Invoke);
                cities.Initialize(ctxt);

                // verify that even top level exceptions go through
                // the OnError handler
                UnauthorizedAccessException expectedException = null;
                try
                {
                    // cause a domain service not initialized exception
                    await cities.InvokeAsync(new InvokeDescription(invokeOperation, new object[] { city }), CancellationToken.None);
                }
                catch (UnauthorizedAccessException e)
                {
                    expectedException = e;
                }

                Assert.IsNull(expectedException, "Expected Invoke to be allowed");
            }
        }
    }
}
