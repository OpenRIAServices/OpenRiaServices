using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.Core.Objects;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Security.Principal;
using OpenRiaServices.DomainServices.Client.Test;
using OpenRiaServices.DomainServices.EntityFramework;
using OpenRiaServices.DomainServices.Hosting;
using System.Text;
using System.Threading;
using System.Web;
using Cities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.DomainServices.Server.Test
{
    using System.Security;
    using System.Threading.Tasks;
    using IgnoreAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.IgnoreAttribute;

    /// <summary>
    /// These tests hit the server objects directly withoug making a web request or going through
    /// the client stack.
    /// </summary>
    [TestClass]
    public class DirectServiceTests
    {
        [TestMethod]
        [WorkItem(877241)]
        public void TestDomainService_UpdateMemberToDefaultValue()
        {
            TestDomainServices.EF.Catalog service = ServerTestHelper.CreateInitializedDomainService<TestDomainServices.EF.Catalog>(DomainOperationType.Submit);

            DomainServiceDescription serviceDescription = DomainServiceDescription.GetDescription(service.GetType());

            // in the below, non RTO is simulated by leaving ReorderPoint as its default value in the
            // original instance
            AdventureWorksModel.Product currProduct = new AdventureWorksModel.Product { ProductID = 1, ReorderPoint = 0, Weight = 0 };
            AdventureWorksModel.Product origProduct = new AdventureWorksModel.Product { ProductID = 1, Weight = 50.0M };

            // verify expected test state - this test relies on the below attribute values
            PropertyDescriptor pd = TypeDescriptor.GetProperties(typeof(AdventureWorksModel.Product))["ReorderPoint"];
            Assert.IsNull(pd.Attributes[typeof(RoundtripOriginalAttribute)]);
            pd = TypeDescriptor.GetProperties(typeof(AdventureWorksModel.Product))["Weight"];
            Assert.IsNotNull(pd.Attributes[typeof(RoundtripOriginalAttribute)]);
            pd = TypeDescriptor.GetProperties(typeof(AdventureWorksModel.Product))["SafetyStockLevel"];
            Assert.IsNotNull(pd.Attributes[typeof(ExcludeAttribute)]);

            ObjectContextExtensions.AttachAsModified(service.ObjectContext.Products, currProduct, origProduct);

            // verify the expected property modifications
            ObjectStateEntry stateEntry = service.ObjectContext.ObjectStateManager.GetObjectStateEntry(currProduct);
            string[] modifiedProperties = stateEntry.GetModifiedProperties().ToArray();
            Assert.IsTrue(modifiedProperties.Contains("ReorderPoint"));   // no RTO so this should be modified
            Assert.IsTrue(modifiedProperties.Contains("Weight"));         // RTO so this is picked up by normal value comparison
            Assert.IsFalse(modifiedProperties.Contains("SafetyStockLevel"));  // excluded member, so shouldn't be marked modified
            Assert.IsFalse(modifiedProperties.Contains("ProductID"));  // key members shouldn't be marked modified
        }

        [TestMethod]
        public void TestDirectChangeset_Cities()
        {
            for (int i = 0; i < 100; i++)
            {
                CityDomainService ds = new CityDomainService();
                DomainServiceContext dsc = new DomainServiceContext(new MockDataService(new MockUser("mathew")), DomainOperationType.Submit);
                ds.Initialize(dsc);

                List<ChangeSetEntry> entries = new List<ChangeSetEntry>();
                for (int j = 0; j < 500; j++)
                {
                    City newCity = new City() { Name = "Toledo", CountyName = "Lucas", StateName = "OH" };
                    entries.Add(new ChangeSetEntry(j, newCity, null, DomainOperation.Insert));
                }

                ChangeSetProcessor.Process(ds, entries);

                Assert.IsFalse(entries.Any(p => p.HasError));
            }
        }

        [TestMethod]
        public void TestDirectChangeset_Simple()
        {
            for (int i = 0; i < 100; i++)
            {
                TestDomainServices.TestProvider_Scenarios ds = new TestDomainServices.TestProvider_Scenarios();
                DomainServiceContext dsc = new DomainServiceContext(new MockDataService(new MockUser("mathew")), DomainOperationType.Submit);
                ds.Initialize(dsc);

                List<ChangeSetEntry> entries = new List<ChangeSetEntry>();
                for (int j = 0; j < 500; j++)
                {
                    TestDomainServices.POCONoValidation e = new TestDomainServices.POCONoValidation()
                        {
                            ID = i,
                            A = "A" + i,
                            B = "B" + i,
                            C = "C" + i,
                            D = "D" + i,
                            E = "E" + i
                        };
                    entries.Add(new ChangeSetEntry(j, e, null, DomainOperation.Insert));
                }

                ChangeSetProcessor.Process(ds, entries);

                Assert.IsFalse(entries.Any(p => p.HasError));
            }
        }

        /// <summary>
        /// This test ensures that our DomainService test assembly uses the default security 
        /// mode (i.e. no SecurityTransparent/APTCA attributes).
        /// </summary>
        [TestMethod]
        public void VerifyDomainServiceTestAssemblyUsesDefaultSecurityMode()
        {
            var a = typeof(TestDomainServices.EF.Northwind).Assembly;
            Assert.IsFalse(a.GetCustomAttributes(typeof(SecurityTransparentAttribute), true).Any(), "SecurityTransparentAttribute not expected.");
            Assert.IsFalse(a.GetCustomAttributes(typeof(AllowPartiallyTrustedCallersAttribute), true).Any(), "AllowPartiallyTrustedCallersAttribute not expected.");
        }

        /// <summary>
        /// Direct test of the query pipeline
        /// </summary>
        [TestMethod]
        [TestCategory("DatabaseTest")]
        public void DomainService_DirectQuery()
        {
            DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(TestDomainServices.EF.Catalog));

            TestDomainServices.EF.Catalog service = new TestDomainServices.EF.Catalog();
            DomainServiceContext dsc = new DomainServiceContext(new MockDataService(new MockUser("mathew") { IsAuthenticated = true }), DomainOperationType.Query);
            service.Initialize(dsc);

            DomainOperationEntry queryOperation = description.GetQueryMethod("GetPurchaseOrders");

            ServiceQuery serviceQuery = new ServiceQuery();
            serviceQuery.QueryParts = new ServiceQueryPart[]
            {
                new ServiceQueryPart("where", "(it.Freight!=0)"),
                new ServiceQueryPart("take", "1")
            };

            QueryResult<AdventureWorksModel.PurchaseOrder> result = QueryProcessor.Process<AdventureWorksModel.PurchaseOrder>(service, queryOperation, new object[0], serviceQuery)
                .GetAwaiter().GetResult();

            Assert.AreEqual(1, result.RootResults.Count());
        }

        /// <summary>
        /// This test ensures that an initialized domain service can only be used for operations of the
        /// type that it was intitialized for.
        /// </summary>
        [TestMethod]
        public void DomainService_InvalidOperationType()
        {
            TestDomainServices.EF.Northwind nw = new TestDomainServices.EF.Northwind();
            DomainServiceContext dsc = new DomainServiceContext(new MockDataService(new MockUser("mathew") { IsAuthenticated = true }), DomainOperationType.Submit);
            nw.Initialize(dsc);

            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TestDomainServices.EF.Northwind));
            DomainOperationEntry entry = dsd.DomainOperationEntries.First(p => p.Operation == DomainOperation.Query);
            QueryDescription qd = new QueryDescription(entry);
            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                nw.QueryAsync(qd).GetAwaiter().GetResult();
            }, string.Format(Resource.DomainService_InvalidOperationType, DomainOperationType.Submit, DomainOperationType.Query));

            InvokeDescription id = new InvokeDescription(entry, null);
            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                nw.InvokeAsync(id).GetAwaiter().GetResult();
            }, string.Format(Resource.DomainService_InvalidOperationType, DomainOperationType.Submit, DomainOperationType.Invoke));

            nw = new TestDomainServices.EF.Northwind();
            dsc = new DomainServiceContext(new MockDataService(new MockUser("mathew") { IsAuthenticated = true }), DomainOperationType.Query);
            nw.Initialize(dsc);

            ChangeSet cs = new ChangeSet(new ChangeSetEntry[] {
                new ChangeSetEntry() { 
                    Entity = new ServiceContext_CurrentOperation_Entity() { Key = 1 },
                    Operation = DomainOperation.Insert
                }
            });
            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                nw.Submit(cs);
            }, string.Format(Resource.DomainService_InvalidOperationType, DomainOperationType.Query, DomainOperationType.Submit));
        }

        /// <summary>
        /// Verify that when EF recursively attaches an unmodified graph, new
        /// entities can be state transitioned to new.
        /// </summary>
        [TestMethod]
        [WorkItem(811427)]
        public void EFDomainService_AddUnmodifiedScenario()
        {
            // create an updated order with an attached new detail
            NorthwindModel.Order order = new NorthwindModel.Order { OrderID = 1, ShipCity = "London" };
            NorthwindModel.Order origOrder = new NorthwindModel.Order { OrderID = 1, ShipCity = "Paris" };
            NorthwindModel.Order_Detail detail = new NorthwindModel.Order_Detail { OrderID = 1, ProductID = 1 };
            NorthwindModel.Order_Detail detail2 = new NorthwindModel.Order_Detail { OrderID = 1, ProductID = 2 };
            order.Order_Details.Add(detail);
            order.Order_Details.Add(detail2);

            // create and initialize the service
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TestDomainServices.EF.Northwind));
            TestDomainServices.EF.Northwind nw = new TestDomainServices.EF.Northwind();
            DomainServiceContext dsc = new DomainServiceContext(new MockDataService(new MockUser("mathew") { IsAuthenticated = true }), DomainOperationType.Submit);
            nw.Initialize(dsc);

            // call attach directly - this causes the detail to be attached as unmodified
            order.EntityKey = nw.ObjectContext.CreateEntityKey("Orders", order);
            origOrder.EntityKey = nw.ObjectContext.CreateEntityKey("Orders", order);
            ObjectContextExtensions.AttachAsModified(nw.ObjectContext.Orders, order, origOrder);
            
            // now insert the detail, and verify that even though the detail is already
            // attached it can be transitioned to new
            nw.InsertOrderDetail(detail);
            nw.InsertOrderDetail(detail2);

            Assert.AreEqual(System.Data.Entity.EntityState.Modified, order.EntityState);
            Assert.AreEqual(System.Data.Entity.EntityState.Added, detail.EntityState);
            Assert.AreEqual(System.Data.Entity.EntityState.Added, detail2.EntityState);
        }

        /// <summary>
        /// Verify DomainServiceContext.Operation represents the currently executing operation.
        /// </summary>
        [TestMethod]
        public async Task ServiceContext_CurrentOperation()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(ServiceContext_CurrentOperation_DomainService));
            ServiceContext_CurrentOperation_DomainService ds;

            // Execute a query.
            ds = new ServiceContext_CurrentOperation_DomainService(DomainOperationType.Query);
            DomainOperationEntry queryOp = dsd.GetQueryMethod("GetEntities");
            Assert.IsNotNull(queryOp);
            QueryDescription desc = new QueryDescription(queryOp);
            var queryResult = ds.QueryAsync(desc).GetAwaiter().GetResult();
            Assert.AreEqual(queryOp, ServiceContext_CurrentOperation_DomainService.LastOperation);
            Assert.IsNull(ds.Context.Operation);

            // Invoke an operation.
            ds = new ServiceContext_CurrentOperation_DomainService(DomainOperationType.Invoke);
            DomainOperationEntry invokeOp = dsd.GetInvokeOperation("Echo");
            Assert.IsNotNull(invokeOp);
            await ds.InvokeAsync(new InvokeDescription(invokeOp, null));
            Assert.AreEqual(invokeOp, ServiceContext_CurrentOperation_DomainService.LastOperation);
            Assert.IsNull(ds.Context.Operation);

            // Invoke an insert operation.
            ds = new ServiceContext_CurrentOperation_DomainService(DomainOperationType.Submit);
            DomainOperationEntry insertOp = dsd.GetSubmitMethod(typeof(ServiceContext_CurrentOperation_Entity), DomainOperation.Insert);
            Assert.IsNotNull(insertOp);
            ds.Submit(new ChangeSet(new ChangeSetEntry[] {
                new ChangeSetEntry() { 
                    Entity = new ServiceContext_CurrentOperation_Entity() { Key = 1 },
                    Operation = DomainOperation.Insert
                }
            }));
            Assert.AreEqual(insertOp, ServiceContext_CurrentOperation_DomainService.LastOperation);
            Assert.IsNull(ds.Context.Operation);
        }

        /// <summary>
        /// Verify that method level validation occurs for query operations.
        [TestMethod]
        public void ServerValidation_Query()
        {
            TestDomainServices.TestProvider_Scenarios service = ServerTestHelper.CreateInitializedDomainService<TestDomainServices.TestProvider_Scenarios>(DomainOperationType.Query);

            DomainServiceDescription serviceDescription = DomainServiceDescription.GetDescription(service.GetType());
            DomainOperationEntry method = serviceDescription.DomainOperationEntries.Single(p => p.Name == "QueryWithParamValidation");
            QueryDescription qd = new QueryDescription(method, new object[] { -1, "ABC" });
            
            var queryResult = service.QueryAsync(qd).GetAwaiter().GetResult();

            Assert.IsNotNull(queryResult.ValidationErrors);
            Assert.AreEqual(2, queryResult.ValidationErrors.Count());

            ValidationResult error = queryResult.ValidationErrors.ElementAt(0);
            Assert.AreEqual("The field a must be between 0 and 10.", error.ErrorMessage);
            Assert.AreEqual("a", error.MemberNames.Single());

            error = queryResult.ValidationErrors.ElementAt(1);
            Assert.AreEqual("The field b must be a string with a maximum length of 2.", error.ErrorMessage);
            Assert.AreEqual("b", error.MemberNames.Single());
        }

        /// <summary>
        /// Verify that method level validation occurs for invoke operations.
        [TestMethod]
        public async Task ServerValidation_InvokeOperation()
        {
            TestDomainServices.TestProvider_Scenarios service = ServerTestHelper.CreateInitializedDomainService<TestDomainServices.TestProvider_Scenarios>(DomainOperationType.Invoke);

            DomainServiceDescription serviceDescription = DomainServiceDescription.GetDescription(service.GetType());
            DomainOperationEntry method = serviceDescription.DomainOperationEntries.Single(p => p.Name == "InvokeOperationWithParamValidation");
            InvokeDescription invokeDescription = new InvokeDescription(method, new object[] { -3, "ABC", new TestDomainServices.CityWithCacheData() });
            var invokeResult = await service.InvokeAsync(invokeDescription);

            Assert.IsNotNull(invokeResult.ValidationErrors);
            Assert.AreEqual(2, invokeResult.ValidationErrors.Count());

            ValidationResult error = invokeResult.ValidationErrors.ElementAt(0);
            Assert.AreEqual("The field a must be between 0 and 10.", error.ErrorMessage);
            Assert.AreEqual("a", error.MemberNames.Single());

            error = invokeResult.ValidationErrors.ElementAt(1);
            Assert.AreEqual("The field b must be a string with a maximum length of 2.", error.ErrorMessage);
            Assert.AreEqual("b", error.MemberNames.Single());
        }

        [TestMethod]
        [TestCategory("DatabaseTest")]
        public void TestDomainService_QueryDirect()
        {
            TestDomainServices.LTS.Catalog provider = ServerTestHelper.CreateInitializedDomainService<TestDomainServices.LTS.Catalog>(DomainOperationType.Query);

            DomainServiceDescription serviceDescription = DomainServiceDescription.GetDescription(provider.GetType());
            DomainOperationEntry method = serviceDescription.DomainOperationEntries.Where(p => p.Operation == DomainOperation.Query).First(p => p.Name == "GetProductsByCategory");
            QueryDescription qd = new QueryDescription(method, new object[] { 1 });
   
            // TODO: Make async
            var queryResult = provider.QueryAsync(qd).GetAwaiter().GetResult();
            int count = queryResult.Result.Cast<DataTests.AdventureWorks.LTS.Product>().Count();
            Assert.AreEqual(32, count);

            // verify that we can use the same provider to execute another query
            qd = new QueryDescription(method, new object[] { 2 });
            queryResult = provider.QueryAsync(qd).GetAwaiter().GetResult();
            count = queryResult.Result.Cast<DataTests.AdventureWorks.LTS.Product>().Count();
            Assert.AreEqual(43, count);
        }

        [TestMethod]
        public void TestDomainService_QueryDirect_Throws()
        {
            MockDomainService_SelectThrows provider = ServerTestHelper.CreateInitializedDomainService<MockDomainService_SelectThrows>(DomainOperationType.Query);

            DomainServiceDescription serviceDescription = DomainServiceDescription.GetDescription(provider.GetType());
            DomainOperationEntry method = serviceDescription.DomainOperationEntries.First(p => p.Name == "GetEntities" && p.Operation == DomainOperation.Query);
            QueryDescription qd = new QueryDescription(method, new object[0]);
            ExceptionHelper.ExpectException<Exception>(delegate
            {
                try
                {
                    provider.QueryAsync(qd).GetAwaiter().GetResult();
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }
            }, "Test");
        }

        // TODO: Remove the [Ignore] on the following two tests once we've updated our test runner such that it 
        //       starts a webserver before running these tests. Or consider moving these tests, or consider 
        //       writing true direct tests that don't require a webserver.

        /// <summary>
        /// Verify that when a member is marked Exclude, it doesn't show up in the serialized response
        /// </summary>
        [TestMethod]
        [Ignore]
        public void TestDomainOperationEntry_VerifyDataMemberExclusion()
        {
            string soap = RequestDirect(typeof(TestDomainServices.LTS.Catalog), "GetProducts", null);

            // verify that the server entity type has the SafetyStockLevel property and that
            // it is marked [Exclude]
            DomainServiceDescription.GetDescription(typeof(TestDomainServices.LTS.Catalog));
            PropertyDescriptor pd = TypeDescriptor.GetProperties(typeof(DataTests.AdventureWorks.LTS.Product)).Cast<PropertyDescriptor>().Single(p => p.Name == "SafetyStockLevel");
            Assert.IsTrue(pd.Attributes.OfType<ExcludeAttribute>().Count() == 1);

            // verify that the serialized response doesn't contain excluded data
            Assert.IsTrue(soap.Contains("ProductID"));  // make sure we got at least one product
            Assert.IsFalse(soap.Contains("SafetyStockLevel"));
        }

        [TestMethod]
        [Ignore]
        public void TestDataService_LTS_Query_MultipleThreads()
        {
            const int numberOfThreads = 10;
            Semaphore s = new Semaphore(0, numberOfThreads);
            Exception lastError = null;

            string soap = RequestDirect(typeof(TestDomainServices.LTS.Catalog), "GetProducts", null);
            Assert.IsTrue(soap.Contains("ProductID"));  // make sure we got at least one product

            for (int i = 0; i < numberOfThreads; i++)
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    try
                    {
                        string soap2 = RequestDirect(typeof(TestDomainServices.LTS.Catalog), "GetProducts", null);
                        Assert.IsTrue(soap2.Contains("ProductID"));  // make sure we got at least one product
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;
                    }
                    finally
                    {
                        s.Release();
                    }
                });
            }

            for (int i = 0; i < numberOfThreads; i++)
            {
                s.WaitOne(TimeSpan.FromSeconds(5));
            }

            if (lastError != null)
            {
                Assert.Fail(lastError.ToString());
            }
        }

        // TODO: get this test working again and remove [Ignore]
        // This test fails intermittently on the json.Contains line below.
        // It also appears it will "pass" if everything times out.
        // Please do not re-enable this test without understanding why it failed
        // and fixing that.  Note that experiments have shown this test takes
        // approximately 16 seconds, and some threads take around 5 seconds to
        // respond -- dangerously close to the timeout value.  Failure is more
        // frequent early in the morning or whenever the unit test load on
        // the DB server is light -- possibly an issue spinning up databases.
        // Failure seen on both of Ron's dev machines.
        [TestMethod]
        [Ignore]
        public void TestDataService_EF_Query_MultipleThreads()
        {
            const int numberOfThreads = 10;
            Semaphore s = new Semaphore(0, numberOfThreads);
            Exception lastError = null;

            for (int i = 0; i < numberOfThreads; i++)
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    try
                    {
                        string soap = RequestDirect(typeof(TestDomainServices.EF.Catalog), "GetProducts", null);
                        Assert.IsTrue(soap.Contains("ProductID"));  // make sure we got at least one product
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;
                    }
                    finally
                    {
                        s.Release();
                    }
                });
            }

            for (int i = 0; i < numberOfThreads; i++)
            {
                s.WaitOne(TimeSpan.FromSeconds(5));
            }

            if (lastError != null)
            {
                Assert.Fail(lastError.ToString());
            }
        }

        [TestMethod]
        public void TestQuery_QueryOperatorOrderPreservation()
        {
            // test no encoding
            StringBuilder sb = new StringBuilder();
            sb.Append("$skip=1");
            sb.Append("&$where=").Append("Color==\"Yel$l&ow\"");
            sb.Append("&$orderby=ListPrice");
            sb.Append("&$where=").Append("Weight>10");
            sb.Append("&$orderby=Style");
            sb.Append("&$skip=2");
            sb.Append("&$take=3");
            sb.Append("&$where=").Append("Color!=\"Purple\"");
            string queryString = sb.ToString();
            List<ServiceQueryPart> queryParts = (List<ServiceQueryPart>)DomainServiceWebHttpBehavior.GetServiceQuery(queryString, queryString).QueryParts;
            Assert.AreEqual("skip", queryParts[0].QueryOperator);
            Assert.AreEqual("where", queryParts[1].QueryOperator);
            Assert.AreEqual("orderby", queryParts[2].QueryOperator);
            Assert.AreEqual("where", queryParts[3].QueryOperator);
            Assert.AreEqual("orderby", queryParts[4].QueryOperator);
            Assert.AreEqual("skip", queryParts[5].QueryOperator);
            Assert.AreEqual("take", queryParts[6].QueryOperator);
            Assert.AreEqual("where", queryParts[7].QueryOperator);

            // test a single where clause with a comma
            sb = new StringBuilder();
            sb.Append("$where=1,2");
            queryString = sb.ToString();
            queryParts = (List<ServiceQueryPart>)DomainServiceWebHttpBehavior.GetServiceQuery(queryString, queryString).QueryParts;
            Assert.AreEqual("where", queryParts[0].QueryOperator);
            Assert.AreEqual("1,2", queryParts[0].Expression);
        }

        /// <summary>
        /// Scenario test that can be used for perf tuning. The test loads 500 orders with OrderDetails
        /// and Products included.
        /// Test is commented out - uncomment it to measure perf.
        /// </summary>
        [TestMethod]
        [Ignore]
        public void Perf_MeasureScenario1()
        {
            // The test can be run against LTS simply by changing the service
            // type to TestDomainServices.LTS.Northwind
            Type serviceType = typeof(TestDomainServices.EF.Northwind);

            DateTime before, after;
            TimeSpan diff;
            List<double> times = new List<double>();
            Dictionary<string, object> queryParameters = new Dictionary<string, object> { {"$take", 500} };

            for (int i = 0; i < 10; i++)
            {
                before = DateTime.Now;
                string soap = RequestDirect(serviceType, "GetOrders", queryParameters);
                after = DateTime.Now;

                Assert.IsTrue(soap.Contains("<ResultCount>500</ResultCount>"));

                diff = after - before;
                times.Add(diff.TotalSeconds);
            }

            double avgTime = times.Average();
            Console.WriteLine("Average time for Perf_MeasureScenario1 : {0} seconds", avgTime);
        }

        private QueryResult DeserializeResult(HttpResponse response, IEnumerable<Type> knownTypes)
        {
            string responseText = response.Output.ToString();

            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(QueryResult), knownTypes);
            MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(responseText));
            QueryResult serviceResult = (QueryResult)ser.ReadObject(ms);

            return serviceResult;
        }

        private string RequestDirect(Type providerType, string dataMethodName, IDictionary<string, object> queryParameters)
        {
            return RequestDirect(providerType, dataMethodName, queryParameters, null);
        }

        private string RequestDirect(Type providerType, string dataMethodName, IDictionary<string, object> queryParameters, IDictionary<string, object> parameters)
        {
            parameters = parameters ?? new Dictionary<string, object>();

            string queryString = string.Empty;

            if (queryParameters != null)
            {
                foreach (var entry in queryParameters)
                {
                    if (queryString != string.Empty)
                    {
                        queryString += "&";
                    }
                    queryString += string.Format("{0}={1}", entry.Key, entry.Value);
                }
            }

            if (parameters.Count > 0)
            {
                foreach (var entry in parameters)
                {
                    if (queryString != string.Empty)
                    {
                        queryString += "&";
                    }
                    queryString += string.Format("{0}={1}", entry.Key, entry.Value);
                }
            }

            string providerName = providerType.FullName.Replace('.', '-');
            
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(string.Format("{0}{1}.svc/{2}?{3}", "http://localhost:60002/", providerName, dataMethodName, queryString));
            using (WebResponse response = request.GetResponse())
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }

    public class ServiceContext_CurrentOperation_DomainService : DomainService, IServiceProvider
    {
        public static DomainOperationEntry LastOperation = null;

        public ServiceContext_CurrentOperation_DomainService(DomainOperationType type)
        {
            this.Initialize(new DomainServiceContext(this, type));
        }

        public DomainServiceContext Context
        {
            get
            {
                return this.ServiceContext;
            }
        }

        public IQueryable<ServiceContext_CurrentOperation_Entity> GetEntities()
        {
            LastOperation = this.ServiceContext.Operation;
            return null;
        }

        public void InsertEntity(ServiceContext_CurrentOperation_Entity entity)
        {
            LastOperation = this.ServiceContext.Operation;
        }

        public string Echo()
        {
            LastOperation = this.ServiceContext.Operation;
            return "echo";
        }

        #region IServiceProvider Members
        object IServiceProvider.GetService(Type serviceType)
        {
            if (serviceType == typeof(IPrincipal))
            {
                return Thread.CurrentPrincipal;
            }

            return null;
        }
        #endregion
    }

    public class ServiceContext_CurrentOperation_Entity
    {
        [Key]
        public int Key
        {
            get;
            set;
        }
    }
}
