extern alias SSmDsClient;
extern alias SSmDsWeb;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataTests.AdventureWorks.LTS;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;

namespace OpenRiaServices.Client.Web.Test
{
    /// <summary>
    /// These tests was moved from the file "DomainClientTests".
    /// They was not merged into WebDomainClientTests since that class has 
    /// Setup logic and almost all tests are written in a separate way
    /// </summary>
    [TestClass]
    public class DomainClientTestsSimple : UnitTestBase
    {
        /// <summary>
        /// It is possible to specify a query expression who's type doesn't match the
        /// type returned by the server. In cases where the query is valid (as below where
        /// it's only a Take - no members are queried) this mismatch cannot be caught, since
        /// BeginQuery isn't strongly typed. This test exists to capture this scenario/issue.
        /// </summary>
        [TestMethod]
        public async Task TestMethodQueryMismatch()
        {
            var dc = DomainContext.DomainClientFactory.CreateDomainClient(typeof(TestDomainServices.LTS.Catalog.ICatalogContract), TestURIs.EF_Catalog, false);
            dc.EntityTypes = new Type[] { typeof(Product), typeof(PurchaseOrder), typeof(PurchaseOrderDetail) };

            var query = new EntityQuery<PurchaseOrder>(new EntityQuery<Product>(dc, "GetProducts", null, true, false), Array.Empty<PurchaseOrder>().AsQueryable().Take(2));
            query.IncludeTotalCount = true;

            var results = await dc.QueryAsync(query, CancellationToken.None);

            Assert.AreEqual(2, results.Entities.Concat(results.IncludedEntities).Count());
            Assert.AreEqual(504, results.TotalCount);
        }


        [TestMethod]
        public async Task TestQuery()
        {
            var dc = DomainContext.DomainClientFactory.CreateDomainClient(typeof(TestDomainServices.LTS.Catalog.ICatalogContract), TestURIs.EF_Catalog, false);
            dc.EntityTypes = new Type[] { typeof(Product) };

            var query = from p in Array.Empty<Product>().AsQueryable()
                        where p.Weight < 10.5M
                        orderby p.Weight
                        select p;

            var entityQuery = new EntityQuery<Product>(new EntityQuery<Product>(dc, "GetProducts", null, true, false), query);
            entityQuery.IncludeTotalCount = true;

            var result = await dc.QueryAsync(entityQuery, CancellationToken.None);

            Assert.AreEqual(79, result.Entities.Concat(result.IncludedEntities).Count());
            Assert.AreEqual(result.Entities.Count(), result.TotalCount);
        }

        [TestMethod]
        public async Task TestQueryEvents()
        {
            var dc = DomainContext.DomainClientFactory.CreateDomainClient(typeof(TestDomainServices.LTS.Catalog.ICatalogContract), TestURIs.EF_Catalog, false);
            dc.EntityTypes = new Type[] { typeof(Product) };

            var result = await dc.QueryAsync(new EntityQuery<Product>(dc, "GetProducts", null, true, false), CancellationToken.None);
            Assert.AreEqual(504, result.Entities.Concat(result.IncludedEntities).Count());
            Assert.AreEqual(result.Entities.Count(), result.TotalCount);
        }
    }
}
