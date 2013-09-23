extern alias SSmDsClient;

using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading;
using OpenRiaServices.DomainServices.Client;
using OpenRiaServices.DomainServices.Client.Test.Services;
using System.Xml.Linq;
using Cities;
using DataTests.AdventureWorks.LTS;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices;
using TestDomainServices.LTS;

namespace OpenRiaServices.DomainServices.Client.Test
{
    [TestClass]
    public class PerfTests : DomainContextTestBase<Catalog>
    {
        public PerfTests()
            : base(TestURIs.EF_Catalog, ProviderType.EF)
        {
        }

        #region Query tests.
        [TestMethod]
        [Asynchronous]
        [PerfTest]
        public void Query_NoResults()
        {
            Catalog catalog = CreateDomainContext();

            var query = catalog.GetProductsQuery().Where(p => p.ProductID < 0);
            LoadOperation lo = catalog.Load(query, false);

            EnqueueConditional(delegate
            {
                return lo.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);
                Assert.IsTrue(lo.Entities.Count() == 0);
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [PerfTest]
        public void Query_POCO_NoResults()
        {
            CityDomainContext dc = new CityDomainContext(TestURIs.Cities);

            var query = dc.GetCitiesQuery().Where(c => c.Name == "-");
            LoadOperation lo = dc.Load(query, false);

            EnqueueConditional(delegate
            {
                return lo.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);
                Assert.IsTrue(lo.Entities.Count() == 0);
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [PerfTest]
        public void Query_Take50Products()
        {
            Catalog catalog = CreateDomainContext();

            var query = catalog.GetProductsQuery().OrderBy(p => p.Name).Take(50);
            LoadOperation lo = catalog.Load(query, false);

            EnqueueConditional(delegate
            {
                return lo.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);
                Assert.IsTrue(lo.Entities.Count() == 50);
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [PerfTest]
        public void Query_POCO_AllCities()
        {
            CityDomainContext dc = new CityDomainContext(TestURIs.Cities);

            var query = dc.GetCitiesQuery();
            LoadOperation lo = dc.Load(query, false);

            EnqueueConditional(delegate
            {
                return lo.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);
                Assert.IsTrue(lo.Entities.Count() > 0);
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [PerfTest]
        public void Query_Take50ProductsWithCaching()
        {
            Catalog catalog = CreateDomainContext();

            var query = catalog.GetProductsWithCachingQuery().OrderBy(p => p.Name).Take(50);
            LoadOperation lo = catalog.Load(query, false);

            EnqueueConditional(delegate
            {
                return lo.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);
                Assert.IsTrue(lo.Entities.Count() == 50);
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [PerfTest]
        public void Query_Take500Products()
        {
            Catalog catalog = CreateDomainContext();

            var query = catalog.GetProductsQuery().OrderBy(p => p.Name).Take(500);
            LoadOperation lo = catalog.Load(query, false);

            EnqueueConditional(delegate
            {
                return lo.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);
                Assert.IsTrue(lo.Entities.Count() == 500);
            });
            EnqueueTestComplete();
        }
        #endregion

        #region Invoke tests.
        [TestMethod]
        [Asynchronous]
        [PerfTest]
        public void Invoke_RoundtripString()
        {
            TestProvider_Scenarios dc = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            string str = "Hello, World!";
            InvokeOperation<string> io = dc.ReturnsString_Online(str);

            EnqueueConditional(delegate
            {
                return io.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(io);
                Assert.AreEqual(str, io.Value);
            });
            EnqueueTestComplete();
        }
        #endregion

        #region Submit tests.
        // Uncomment this test out to do lightweight perf measurement for the submit pipeline
        //[TestMethod]
        public void Submit_MeasureChangeset()
        {
            CityDomainContext dc = null;

            int numIterations = 500;
            DateTime start = DateTime.Now;
            for (int i = 0; i < numIterations; i++)
            {
                dc = new CityDomainContext(TestURIs.Cities);
                Cities.CityData data = new CityData();
                dc.EntityContainer.LoadEntities(data.Cities);

                foreach (City city in dc.Cities)
                {
                    city.ZoneID += 1;
                    city.AssignCityZone("z");
                }

                for (int j = 1; j <= 5; j++)
                {
                    dc.Cities.Add(new City()
                    {
                        Name = "Redmond" + new string('x', j),
                        CountyName = "King",
                        StateName = "WA"
                    });
                }

                // simulate the major changeset operations here
                EntityChangeSet cs = dc.EntityContainer.GetChanges();
                cs.Validate(dc.ValidationContext);
                ChangeSetBuilder.Build(cs);
            }
            DateTime stop = DateTime.Now;
            TimeSpan ts = stop - start;
            double avgMs = ts.TotalMilliseconds / numIterations;
        }

        [TestMethod]
        [Asynchronous]
        [PerfTest]
        public void Submit_POCO_Insert5Cities()
        {
            CityDomainContext dc = new CityDomainContext(TestURIs.Cities);

            for (int i = 1; i <= 5; i++)
            {
                dc.Cities.Add(new City()
                {
                    Name = "Redmond" + new string('x', i),
                    CountyName = "King",
                    StateName = "WA"
                });
            }
            SubmitOperation so = dc.SubmitChanges();

            EnqueueConditional(delegate
            {
                return so.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(so);
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [PerfTest]
        public void Submit_POCO_Insert5Simple()
        {
            TestProvider_Scenarios dc = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            for (int i = 0; i < 5; i++)
            {
                dc.POCONoValidations.Add(new POCONoValidation()
                {
                    ID = i,
                    A = "A" + i,
                    B = "B" + i,
                    C = "C" + i,
                    D = "D" + i,
                    E = "E" + i
                });
            }
            SubmitOperation so = dc.SubmitChanges();

            EnqueueConditional(delegate
            {
                return so.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(so);
            });
            EnqueueTestComplete();
        }
        #endregion
    }
}
