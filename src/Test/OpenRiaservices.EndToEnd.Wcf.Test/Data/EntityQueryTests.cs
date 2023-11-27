extern alias SSmDsClient;

using System.Collections.Generic;
using System.Linq;
using OpenRiaServices.Client;
using Cities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace OpenRiaServices.Client.Test
{
    using Resource = SSmDsClient::OpenRiaServices.Client.Resource;

    [TestClass]
    public class EntityQueryTests
    {
        private readonly DomainClient _testClient = new Cities.CityDomainContext(TestURIs.Cities).DomainClient;

        /// <summary>
        /// Verify that an EntityQuery can only be passed to the Load method of the context
        /// used to create the query.
        /// </summary>
        [TestMethod]
        public void ExogenousDomainClient()
        {
            Cities.CityDomainContext ctxt1 = new CityDomainContext(TestURIs.Cities);
            Cities.CityDomainContext ctxt2 = new CityDomainContext(TestURIs.Cities);

            var q1 = ctxt1.GetCitiesInStateQuery("OH");

            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                ctxt2.Load(q1, false);
            }, string.Format(Resource.DomainContext_InvalidEntityQueryDomainClient, q1.QueryName));
        }

        [TestMethod]
        public void TestQueryOperators_QueryComprehension()
        {
            // test where
            EntityQuery<City> citiesQuery = new EntityQuery<City>(_testClient, "GetCities", null, false, true);
            citiesQuery = from c in citiesQuery
                          where c.CountyName == "Lucas"
                          select c;
            List<ServiceQueryPart> parts = QuerySerializer.Serialize(citiesQuery.Query);
            Assert.AreEqual(1, parts.Count);
            Assert.AreEqual("where", parts[0].QueryOperator);
            Assert.AreEqual("(it.CountyName==\"Lucas\")", parts[0].Expression);
            Assert.AreSame(typeof(City), citiesQuery.EntityType);

            // test orderby, thenby
            citiesQuery = new EntityQuery<City>(_testClient, "GetCities", null, false, true);
            citiesQuery = from c in citiesQuery
                          orderby c.Name, c.StateName
                          select c;
            parts = QuerySerializer.Serialize(citiesQuery.Query);
            Assert.AreEqual(1, parts.Count);
            Assert.AreEqual("orderby", parts[0].QueryOperator);
            Assert.AreEqual("it.Name, it.StateName", parts[0].Expression);

            // test orderby desc, thenby desc
            citiesQuery = new EntityQuery<City>(_testClient, "GetCities", null, false, true);
            citiesQuery = from c in citiesQuery
                          orderby c.Name descending, c.StateName descending
                          select c;
            parts = QuerySerializer.Serialize(citiesQuery.Query);
            Assert.AreEqual(1, parts.Count);
            Assert.AreEqual("orderby", parts[0].QueryOperator);
            Assert.AreEqual("it.Name desc, it.StateName desc", parts[0].Expression);

            // test skip and take
            citiesQuery = new EntityQuery<City>(_testClient, "GetCities", null, false, true);
            citiesQuery = citiesQuery.Skip(20).Take(10);
            parts = QuerySerializer.Serialize(citiesQuery.Query);
            Assert.AreEqual(2, parts.Count);
            Assert.AreEqual("skip", parts[0].QueryOperator);
            Assert.AreEqual("20", parts[0].Expression);
            Assert.AreEqual("take", parts[1].QueryOperator);
            Assert.AreEqual("10", parts[1].Expression);

            // test all together
            citiesQuery =
                (from c in new EntityQuery<City>(_testClient, "GetCities", null, false, true)
                 where c.CountyName == "Lucas"
                 orderby c.Name descending, c.StateName descending
                 select c
                 ).Skip(20).Take(10);
            parts = QuerySerializer.Serialize(citiesQuery.Query);
            Assert.AreEqual(4, parts.Count);
        }

        [TestMethod]
        public void TestQueryOperators_QueryMethods()
        {
            // test where
            EntityQuery<City> citiesQuery = new EntityQuery<City>(_testClient, "GetCities", null, false, true);
            citiesQuery = citiesQuery.Where(c => c.CountyName == "Lucas");
            List<ServiceQueryPart> parts = QuerySerializer.Serialize(citiesQuery.Query);
            Assert.AreEqual(1, parts.Count);
            Assert.AreEqual("where", parts[0].QueryOperator);
            Assert.AreEqual("(it.CountyName==\"Lucas\")", parts[0].Expression);

            // test orderby, thenby
            citiesQuery = new EntityQuery<City>(_testClient, "GetCities", null, false, true);
            citiesQuery = citiesQuery.OrderBy(c => c.Name).ThenBy(c => c.StateName);
            parts = QuerySerializer.Serialize(citiesQuery.Query);
            Assert.AreEqual(1, parts.Count);
            Assert.AreEqual("orderby", parts[0].QueryOperator);
            Assert.AreEqual("it.Name, it.StateName", parts[0].Expression);

            // test orderby desc, thenby desc
            citiesQuery = new EntityQuery<City>(_testClient, "GetCities", null, false, true);
            citiesQuery = citiesQuery.OrderByDescending(c => c.Name).ThenByDescending(c => c.StateName);
            parts = QuerySerializer.Serialize(citiesQuery.Query);
            Assert.AreEqual(1, parts.Count);
            Assert.AreEqual("orderby", parts[0].QueryOperator);
            Assert.AreEqual("it.Name desc, it.StateName desc", parts[0].Expression);

            // test skip and take
            citiesQuery = new EntityQuery<City>(_testClient, "GetCities", null, false, true);
            citiesQuery = citiesQuery.Skip(20).Take(10);
            parts = QuerySerializer.Serialize(citiesQuery.Query);
            Assert.AreEqual(2, parts.Count);
            Assert.AreEqual("skip", parts[0].QueryOperator);
            Assert.AreEqual("20", parts[0].Expression);
            Assert.AreEqual("take", parts[1].QueryOperator);
            Assert.AreEqual("10", parts[1].Expression);

            // test all together
            citiesQuery = new EntityQuery<City>(_testClient, "GetCities", null, false, true)
                 .Where(c => c.CountyName == "Lucas")
                 .OrderBy(c => c.Name).ThenBy(c => c.StateName)
                 .Skip(20)
                 .Take(10);
            parts = QuerySerializer.Serialize(citiesQuery.Query);
            Assert.AreEqual(4, parts.Count);
        }

        [TestMethod]
        public void NonComposableQuery()
        {
            EntityQuery<City> citiesQuery = new EntityQuery<City>(_testClient, "GetCities", null, false, false);
            IQueryable<City> queryable = Array.Empty<City>().AsQueryable();

            string expectedMessage = string.Format(Resource.EntityQuery_NotComposable, "City", "GetCities");

            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                citiesQuery.Where(p => p.StateName == "Toledo");
            }, expectedMessage);

            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                citiesQuery.Skip(1);
            }, expectedMessage);

            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                citiesQuery.Take(1);
            }, expectedMessage);

            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                citiesQuery.OrderBy(p => p.CountyName);
            }, expectedMessage);

            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                citiesQuery.OrderByDescending(p => p.CountyName);
            }, expectedMessage);

            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                citiesQuery.ThenBy(p => p.CountyName);
            }, expectedMessage);

            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                citiesQuery.ThenByDescending(p => p.CountyName);
            }, expectedMessage);
        }

        [TestMethod]
        public void ParameterChecking()
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                {"a", 1},
                {"b", 2}
            };
            EntityQuery<City> baseQuery = new EntityQuery<City>(_testClient, "GetCities", null, false, true);

            ExceptionHelper.ExpectArgumentNullException(delegate
            {
                EntityQuery<City> citiesQuery = new EntityQuery<City>(_testClient, null, parameters, false, true);
            }, "queryName");

            ExceptionHelper.ExpectArgumentNullException(delegate
            {
                EntityQuery<City> citiesQuery = new EntityQuery<City>(_testClient, string.Empty, parameters, false, true);
            }, "queryName");

            ExceptionHelper.ExpectArgumentNullException(delegate
            {
                EntityQuery<City> citiesQuery = new EntityQuery<City>(null, "GetCities", parameters, false, true);
            }, "domainClient");

            ExceptionHelper.ExpectArgumentNullException(delegate
            {
                EntityQuery<City> citiesQuery = new EntityQuery<City>(null, Array.Empty<City>().AsQueryable().Where(p => p.StateName == "Toledo"));
            }, "baseQuery");

            ExceptionHelper.ExpectArgumentNullException(delegate
            {
                EntityQuery<City> citiesQuery = new EntityQuery<City>(baseQuery, null);
            }, "query");

            ExceptionHelper.ExpectArgumentNullException(delegate
            {
                EntityQuery<City> citiesQuery = new EntityQuery<City>(baseQuery, null);
            }, "query"); 
        }

        [TestMethod]
        public void TestPropertyValues()
        {
            // Test Query property
            EntityQuery<City> citiesQuery = new EntityQuery<City>(_testClient, "GetCities", null, false, true);
            Assert.IsNull(citiesQuery.Query);
            citiesQuery = citiesQuery.Where(p => p.StateName == "Ohio");
            Assert.IsNotNull(citiesQuery.Query);

            // Test IsComposable property
            citiesQuery = new EntityQuery<City>(_testClient, "GetCities", null, false, true);
            Assert.AreEqual(true, citiesQuery.IsComposable);
            citiesQuery = new EntityQuery<City>(_testClient, "GetCities", null, false, false);
            Assert.AreEqual(false, citiesQuery.IsComposable);

            // Test Parameters property
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                {"a", 1},
                {"b", 2}
            };
            citiesQuery = new EntityQuery<City>(_testClient, "GetCities", parameters, false, true);
            Assert.AreSame(parameters, citiesQuery.Parameters);
            citiesQuery = new EntityQuery<City>(_testClient, "GetCities", null, false, true);
            Assert.IsNull(citiesQuery.Parameters);

            // Test QueryName property
            citiesQuery = new EntityQuery<City>(_testClient, "GetCities", parameters, false, true);
            Assert.AreEqual("GetCities", citiesQuery.QueryName);
        }
    }
}
