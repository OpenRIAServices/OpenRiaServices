extern alias SSmDsClient;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Cities;
using DataTests.AdventureWorks.LTS;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices;
using TestDomainServices.LTS;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.DomainServices.Client.Test
{
    using TestDomainServices.Saleテ;
    using Resource = SSmDsClient::OpenRiaServices.DomainServices.Client.Resource;
    using Resources = SSmDsClient::OpenRiaServices.DomainServices.Client.Resources;

    /// <summary>
    /// Non provider specific query tests.
    /// </summary>
    [TestClass]
    public class QueryTests : DomainContextTestBase<Catalog>
    {
        /// <summary>
        /// This is the hard-coded constant for the number of purchase orders
        /// in the read-only database used for these tests.
        /// </summary>
        private const int PurchaseOrderCountInDatabase = 16;

        public QueryTests()
            : base(TestURIs.LTS_Catalog, ProviderType.LTS)
        {
        }

        /// <summary>
        /// There was a bug in server query parsing, which was choking on query parts that were substring matches of other
        /// parts of the query. The repro test below demonstrates this, where "take=1" is a substring of "take=10".
        /// </summary>
        [TestMethod]
        [WorkItem(195495)]
        public async Task VerifyQueryOperatorOrdering()
        {
            TestDomainServices.TestProvider_Scenarios ctxt = new TestDomainServices.TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            var query = ctxt.GetRoundtripQueryEntitiesQuery().OrderBy(p => p.PropB).Take(10).Where(p => !p.PropC.Contains("Pluto")).Take(1);
            LoadOperation lo = ctxt.Load(query, false);

            await lo;
            Assert.IsFalse(lo.HasError);
            RoundtripQueryEntity entity = (RoundtripQueryEntity)lo.Entities.Single();
            Assert.IsTrue(entity.Query.Contains(".OrderBy(Param_0 => Param_0.PropB).Take(10).Where(Param_1 => Not(Param_1.PropC.Contains(\"Pluto\"))).Take(1)"));

            // do a similar test, this time reversing the order of the takes
            query = ctxt.GetRoundtripQueryEntitiesQuery().OrderBy(p => p.PropB).Take(1).Where(p => !p.PropC.Contains("Pluto")).Take(10);
            lo = ctxt.Load(query, LoadBehavior.RefreshCurrent, false);

            await lo;
            Assert.IsFalse(lo.HasError);
            entity = (RoundtripQueryEntity)lo.Entities.Single();
            Assert.IsTrue(entity.Query.Contains(".OrderBy(Param_0 => Param_0.PropB).Take(1).Where(Param_1 => Not(Param_1.PropC.Contains(\"Pluto\"))).Take(10)"));

            query = ctxt.GetRoundtripQueryEntitiesQuery().Take(100).Take(10).Take(1);
            lo = ctxt.Load(query, LoadBehavior.RefreshCurrent, false);

            await lo;
            Assert.IsFalse(lo.HasError);
            entity = (RoundtripQueryEntity)lo.Entities.Single();
            Assert.IsTrue(entity.Query.Contains(".Take(100).Take(10).Take(1)"));
        }

        [TestMethod]
        public async Task Bug830468_MultipleWhereClauses()
        {
            Northwind nw = new Northwind(TestURIs.LTS_Northwind);

            var query = nw.GetOrdersQuery().Where(p => p.Freight < 1000).Where(p => p.ShipAddress.ToLower() != "").Take(1);
            LoadOperation lo = nw.Load(query, false);

            await lo;
            Assert.IsFalse(lo.HasError);
            Assert.AreEqual(1, lo.Entities.Count);
        }

        [TestMethod]
        public async Task SelectQueryOperator()
        {
            Cities.CityDomainContext cities = new CityDomainContext(TestURIs.Cities);

            // verify that attempting to define a selector function results
            // in an exception
            var query = from c in cities.GetCitiesQuery()
                        select new City
                        {
                            StateName = c.StateName
                        };
            ExceptionHelper.ExpectException<NotSupportedException>(delegate
            {
                cities.Load(query, false);
            }, Resources.QuerySerialization_ProjectionsNotSupported);

            // try another non-empty selection
            query = from c in cities.GetCitiesQuery()
                    select new City();
            ExceptionHelper.ExpectException<NotSupportedException>(delegate
            {
                cities.Load(query, false);
            }, Resources.QuerySerialization_ProjectionsNotSupported);

            // try selecting a different type
            var query2 = from c in cities.GetCitiesQuery()
                         select null;
            ExceptionHelper.ExpectException<NotSupportedException>(delegate
            {
                cities.Load(query2, false);
            }, Resources.QuerySerialization_ProjectionsNotSupported);

            // verify that we can select from a non-composable query w/o exception
            query = new EntityQuery<City>(cities.DomainClient, "Foo", null, false, false);
            query = from c in query select c;

            // verify that a default selector w/o a where clause compiles and runs
            query = from c in cities.GetCitiesQuery()
                    select c;
            LoadOperation lo = cities.Load(query, false);

            await lo;
            Assert.IsFalse(lo.HasError);
            Assert.IsTrue(lo.Entities.Count > 0);
        }

        /// <summary>
        /// Verify that synchronous method level validation occurs immediately for
        /// query operations.
        [TestMethod]
        public void ClientValidation_Query()
        {
            TestDomainServices.TestProvider_Scenarios ctxt = new TestDomainServices.TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            ExceptionHelper.ExpectValidationException(delegate
            {
                ctxt.QueryWithParamValidationQuery(-3, "ABC");
            }, "The field a must be between 0 and 10.", typeof(RangeAttribute), -3);
        }

        /// <summary>
        /// Verify that server side validation errors are propagated back to the
        /// client for query operations.
        /// </summary>
        [TestMethod]
        public async Task ServerValidation_Query()
        {
            TestDomainServices.TestProvider_Scenarios ctxt = new TestDomainServices.TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            // Validate using an action so we can assert state for each of the 3 different
            // completion patterns; callback, event, and polling
            Action<Exception> validateException = (ex) =>
            {
                Assert.IsNotNull(ex);
                DomainOperationException exception = (DomainOperationException)ex;
                Assert.AreEqual(typeof(DomainOperationException), exception.GetType());
                Assert.AreEqual(OperationErrorStatus.ValidationFailed, exception.Status);
                Assert.AreEqual(string.Format(Resource.DomainContext_LoadOperationFailed_Validation, "QueryWithParamValidation"), exception.Message);
                Assert.AreEqual(1, exception.ValidationErrors.Count(),
                    "There should be 1 validation error.");
                ValidationResult error = exception.ValidationErrors.Single();
                Assert.AreEqual("Server validation exception thrown!", error.ErrorMessage);
            };
            Action<LoadOperation<A>> validate = (lo) =>
            {
                Assert.IsNotNull(lo.Error);
                validateException(lo.Error);
                Assert.AreEqual(1, lo.ValidationErrors.Count(),
                    "There should be 1 validation error.");
                ValidationResult error = lo.ValidationErrors.Single();
                Assert.AreEqual("Server validation exception thrown!", error.ErrorMessage);
                lo.MarkErrorAsHandled();
            };

            var query = ctxt.QueryWithParamValidationQuery(5, "ex");
            LoadOperation<A> op = ctxt.Load(query, validate, null);
            op.Completed += (sender, e) =>
            {
                validate((LoadOperation<A>)sender);
            };

            await op;
            validate(op);

            var loadException = await ExceptionHelper.ExpectExceptionAsync<DomainOperationException>(()
                => ctxt.LoadAsync(query));
            validateException(loadException);
        }

        [TestMethod]
        public async Task TestNullReturningQueryOperations()
        {
            TestDomainServices.TestProvider_Scenarios ctxt = new TestDomainServices.TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            // test null returning singleton
            LoadOperation lo = ctxt.Load(ctxt.GetAReturnNullQuery(), false);

            await lo;
            Assert.IsNull(lo.Error);
            Assert.AreEqual(0, lo.Entities.Count);

            // test null returning collection query
            lo = ctxt.Load(ctxt.GetAsReturnNullQuery(), false);

            await lo;
            Assert.IsNull(lo.Error);
            Assert.AreEqual(0, lo.Entities.Count);
        }

#if !VBTests

        [TestMethod]
        public async Task LoadOperation_SingletonQueryMethod()
        {
            Northwind nw = new Northwind(TestURIs.LTS_Northwind);

            // first test a query that returns an instance
            LoadOperation lo = nw.Load(nw.GetProductByIdQuery(5), false);

            await lo;
            Assert.IsFalse(lo.HasError);

            DataTests.Northwind.LTS.Product prod = nw.Products.Single();
            Assert.AreEqual(5, prod.ProductID);

            // next test a query that returns nothing
            lo = nw.Load(nw.GetProductByIdQuery(-1), false);

            await lo;
            Assert.IsFalse(lo.HasError);
            Assert.AreEqual(1, nw.Products.Count);
        }

        [TestMethod]
        public async Task LoadOperation_SubscribeCompletedAfterComplete()
        {
            Cities.CityDomainContext cities = new CityDomainContext(TestURIs.Cities);
            bool callbackCalled = false;

            var query = cities.GetCitiesQuery().Where(p => p.StateName == "Ohio");
            LoadOperation<City> lo = cities.Load(query, false);

            await lo;
            Assert.IsFalse(lo.HasError);
            Assert.IsTrue(lo.IsComplete);

            // subscribe to completed event AFTER completion. Verify
            // that the callback is called immediately
            lo.Completed += (s, e) =>
            {
                callbackCalled = true;
            };

            Assert.IsTrue(callbackCalled);
        }

        [TestMethod]
        public async Task LoadOperation_ObservableCollections()
        {
            Cities.CityDomainContext cities = new CityDomainContext(TestURIs.Cities);

            // first execute a query returning no results and verify we don't
            // get collection change notifications
            var query = cities.GetCitiesQuery().Where(p => p.Name == "DNE");
            LoadOperation<City> lo = cities.Load(query, false);

            Assert.AreEqual(0, lo.Entities.Count);
            Assert.AreEqual(0, lo.AllEntities.Count);

            List<NotifyCollectionChangedEventArgs> entitiesCollectionChangedArgs = new List<NotifyCollectionChangedEventArgs>();
            NotifyCollectionChangedEventHandler entitiesCollectionChangedHandler = (s, e) =>
            {
                entitiesCollectionChangedArgs.Add(e);
            };

            List<NotifyCollectionChangedEventArgs> allEntitiesCollectionChangedArgs = new List<NotifyCollectionChangedEventArgs>();
            NotifyCollectionChangedEventHandler allEntitiesCollectionChangedHandler = (s, e) =>
            {
                allEntitiesCollectionChangedArgs.Add(e);
            };

            bool totalCountChanged = false;
            PropertyChangedEventHandler propertyChangedEventHandler = (s, e) =>
            {
                if (e.PropertyName == "TotalEntityCount")
                {
                    totalCountChanged = true;
                }
            };

            ((INotifyCollectionChanged)lo.Entities).CollectionChanged += entitiesCollectionChangedHandler;
            ((INotifyCollectionChanged)lo.AllEntities).CollectionChanged += allEntitiesCollectionChangedHandler;
            ((INotifyPropertyChanged)lo).PropertyChanged += propertyChangedEventHandler;

            await lo;
            Assert.AreEqual(0, entitiesCollectionChangedArgs.Count);
            Assert.AreEqual(0, allEntitiesCollectionChangedArgs.Count);
            Assert.IsFalse(totalCountChanged);

            // now execute a query returning 3 entities
            query = cities.GetCitiesQuery().Take(3);
            lo = cities.Load(query, false);
            ((INotifyCollectionChanged)lo.Entities).CollectionChanged += entitiesCollectionChangedHandler;
            ((INotifyCollectionChanged)lo.AllEntities).CollectionChanged += allEntitiesCollectionChangedHandler;
            ((INotifyPropertyChanged)lo).PropertyChanged += propertyChangedEventHandler;

            await lo;
            // Entities : expect a Reset event and no Adds
            Assert.AreEqual(1, entitiesCollectionChangedArgs.Count);
            Assert.AreEqual(3, lo.Entities.Count);

            // AllEntities : expect a Reset event
            Assert.AreEqual(1, allEntitiesCollectionChangedArgs.Count);
            Assert.AreEqual(3, lo.AllEntities.Count);

            Assert.IsTrue(totalCountChanged);
        }

        [TestMethod]
        public async Task LoadOperation_MulipleCompletionBehavior()
        {
            Cities.CityDomainContext cities = new CityDomainContext(TestURIs.Cities);

            var query = cities.GetCitiesQuery().Take(3);
            LoadOperation<City> lo = cities.Load(query, false);

            lo.Cancel();
            await lo;
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                lo.Cancel();
            }, Resources.AsyncOperation_AlreadyCompleted);

            lo = cities.Load(query, false);

            await lo;
            Assert.IsTrue(lo.IsComplete);

            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                lo.Complete(new LoadResult<City>(query, default(LoadBehavior), Array.Empty<City>(), Array.Empty<City>(), 0));
            }, Resources.AsyncOperation_AlreadyCompleted);

            lo = cities.Load(query, false);

            await lo;
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                lo.SetError(new Exception("FAIL!"));
            }, Resources.AsyncOperation_AlreadyCompleted);
        }

        /// <summary>
        /// Verify that for a successful load operation, the following happens:
        ///   - the Completed event is raised
        ///   - the user specfied callback is called. Callback is invoked BEFORE events
        ///   - INPC events are fired
        /// </summary>
        [TestMethod]
        public async Task LoadOperationLifecycle_Success()
        {
            var mockDomainClient = new CitiesMockDomainClient();
            var tcs = new TaskCompletionSource<QueryCompletedResult>();
            mockDomainClient.QueryCompletedResult = tcs.Task;
            Cities.CityDomainContext cities = new CityDomainContext(mockDomainClient);
            var returnedCities = new Entity[3]
            {
                new City() {Name="Duvall", CountyName="King", StateName="WA"},
                new City() {Name="Carnation", CountyName="King", StateName="WA"},
                new City() {Name="Everett", CountyName="King", StateName="WA"},
            };

            // subscribe a user callback
            object userState = new object();
            bool completedCalled = false;
            bool callbackCalled = false;
            Action<LoadOperation<City>> callback = (o) =>
            {
                if (o.HasError)
                {
                    o.MarkErrorAsHandled();
                }

                Assert.IsTrue(o.IsComplete);
                Assert.IsFalse(o.CanCancel);
                Assert.IsFalse(completedCalled);
                Assert.AreSame(userState, o.UserState);
                Assert.IsTrue(o.Entities.Count > 0);
                Assert.IsTrue(o.AllEntities.Count > 0);
                callbackCalled = true;
            };

            var query = cities.GetCitiesQuery().Take(3);
            LoadOperation<City> lo = cities.Load(query, callback, userState);

            Assert.AreSame(query, lo.EntityQuery);

            // verify state before completion
            Assert.IsTrue(lo.CanCancel);
            Assert.AreSame(query, lo.EntityQuery);
            Assert.AreEqual(LoadBehavior.KeepCurrent, lo.LoadBehavior);
            Assert.IsNull(lo.Error);
            Assert.IsFalse(lo.HasError);
            Assert.IsFalse(lo.IsCanceled);
            Assert.IsFalse(lo.IsComplete);
            Assert.IsTrue(lo.CanCancel);

            // subscribe to PC notifications
            List<string> propChangeNotifications = new List<string>();
            ((INotifyPropertyChanged)lo).PropertyChanged += (s, e) =>
            {
                Assert.IsFalse(lo.CanCancel);
                Assert.AreSame(lo, s);
                propChangeNotifications.Add(e.PropertyName);
            };

            // subscribe to completed event
            lo.Completed += (s, e) =>
            {
                LoadOperation<City> o = (LoadOperation<City>)s;

                // verify callback is called BEFORE the event is raised
                Assert.IsTrue(callbackCalled);

                Assert.IsTrue(o.IsComplete);
                Assert.IsFalse(o.CanCancel);

                Assert.IsTrue(o.Entities.Count > 0);
                Assert.IsTrue(o.AllEntities.Count > 0);

                completedCalled = true;
            };


            Assert.IsFalse(Volatile.Read(ref completedCalled));
            Assert.IsFalse(Volatile.Read(ref callbackCalled));
            Assert.AreEqual(0, propChangeNotifications.Count);

            // Now have load operation complete
            tcs.SetResult(new QueryCompletedResult(returnedCities, Enumerable.Empty<Entity>(), 10, Enumerable.Empty<ValidationResult>()));
            await lo;
            Assert.IsTrue(Volatile.Read(ref completedCalled));
            Assert.IsTrue(Volatile.Read(ref callbackCalled));

            // verify state after completion
            Assert.IsNull(lo.Error);
            Assert.IsFalse(lo.HasError);
            Assert.IsFalse(lo.IsCanceled);
            Assert.IsTrue(lo.IsComplete);
            Assert.IsFalse(lo.CanCancel);

            // verify property change notifications and ordering
            Assert.AreEqual(3, propChangeNotifications.Count);
            Assert.AreEqual("IsComplete", propChangeNotifications[0]);
            Assert.AreEqual("CanCancel", propChangeNotifications[1]);
            Assert.AreEqual("TotalEntityCount", propChangeNotifications[2]);
        }

        /// <summary>
        /// Verify that for a cancelled load operation, the following happens:
        ///   - the Completed event is raised
        ///   - the user specfied callback is called. Callback is invoked BEFORE events
        ///   - INPC events are fired
        /// </summary>
        [TestMethod]
        public async Task LoadOperationLifecycle_Cancel()
        {
            var mockDomainClient = new CitiesMockDomainClient();
            var tcs = new TaskCompletionSource<QueryCompletedResult>();
            mockDomainClient.QueryCompletedResult = tcs.Task;
            Cities.CityDomainContext cities = new CityDomainContext(mockDomainClient);

            // subscribe a user callback
            object userState = new object();
            bool completedCalled = false;
            bool callbackCalled = false;
            Action<LoadOperation<City>> callback = (o) =>
            {
                if (o.HasError)
                {
                    o.MarkErrorAsHandled();
                }

                Assert.IsTrue(o.IsComplete);
                Assert.IsTrue(o.IsCanceled);
                Assert.IsFalse(o.CanCancel);
                Assert.IsFalse(completedCalled);
                Assert.AreSame(userState, o.UserState);
                callbackCalled = true;
            };

            var query = cities.GetCitiesQuery().Take(3);
            LoadOperation<City> lo = cities.Load(query, callback, userState);

            // verify state before completion
            Assert.IsTrue(lo.CanCancel);
            Assert.AreSame(query, lo.EntityQuery);
            Assert.IsNull(lo.Error);
            Assert.IsFalse(lo.HasError);
            Assert.IsFalse(lo.IsCanceled);
            Assert.IsFalse(lo.IsComplete);
            Assert.IsTrue(lo.CanCancel);

            // subscribe to PC notifications
            List<string> propChangeNotifications = new List<string>();
            ((INotifyPropertyChanged)lo).PropertyChanged += (s, e) =>
            {
                Assert.IsFalse(lo.CanCancel);
                Assert.AreSame(lo, s);
                propChangeNotifications.Add(e.PropertyName);
            };

            // subscribe to completed event
            lo.Completed += (s, e) =>
            {
                LoadOperation<City> o = (LoadOperation<City>)s;

                // verify callback is called BEFORE the event is raised
                Assert.IsTrue(callbackCalled);

                Assert.IsTrue(o.IsComplete);
                Assert.IsFalse(o.CanCancel);
                Assert.IsTrue(o.IsCanceled);

                completedCalled = true;
            };

            // request cancellation
            Assert.IsFalse(lo.IsComplete);
            lo.Cancel();

            Assert.IsFalse(lo.CanCancel);
            Assert.IsTrue(lo.IsCancellationRequested, "Cancellation should be requested");

            Assert.IsFalse(Volatile.Read(ref completedCalled));
            Assert.IsFalse(Volatile.Read(ref callbackCalled));
            Assert.AreEqual(0, propChangeNotifications.Count);

            // continue with actual cancellation
            tcs.TrySetCanceled(lo.CancellationToken);
            await lo;
            Assert.IsTrue(Volatile.Read(ref completedCalled));
            Assert.IsTrue(Volatile.Read(ref callbackCalled));

            // verify state after completion
            Assert.IsNull(lo.Error);
            Assert.IsFalse(lo.HasError);
            Assert.IsTrue(lo.IsCanceled);
            Assert.IsTrue(lo.IsComplete);
            Assert.IsFalse(lo.CanCancel);

            // verify property change notifications and ordering
            Assert.AreEqual(3, propChangeNotifications.Count);
            Assert.AreEqual("IsCanceled", propChangeNotifications[0]);
            Assert.AreEqual("CanCancel", propChangeNotifications[1]);
            Assert.AreEqual("IsComplete", propChangeNotifications[2]);
        }

        /// <summary>
        /// Verify that for a failed load operation, the following happens:
        ///   - the Completed event is raised
        ///   - the user specfied callback is called. Callback is invoked BEFORE events
        ///   - INPC events are fired
        /// </summary>
        [TestMethod]
        public async Task LoadOperationLifecycle_Error()
        {
            var mockDomainClient = new CitiesMockDomainClient();
            var tcs = new TaskCompletionSource<QueryCompletedResult>();
            mockDomainClient.QueryCompletedResult = tcs.Task;
            TestDataContext ctxt = new TestDataContext(mockDomainClient);

            object userState = new object();
            bool completedCalled = false;
            bool callbackCalled = false;
            List<string> propChangeNotifications = new List<string>();
            LoadOperation<Product> lo = null;

            Action<LoadOperation<Product>> callback = (o) =>
            {
                if (o.HasError)
                {
                    o.MarkErrorAsHandled();
                }

                Assert.IsTrue(o.IsComplete);
                Assert.IsTrue(o.HasError);
                Assert.IsFalse(o.CanCancel);
                Assert.IsFalse(completedCalled);
                Assert.AreSame(userState, o.UserState);
                Assert.IsTrue(o.Entities.Count == 0);
                Assert.IsTrue(o.AllEntities.Count == 0);
                callbackCalled = true;
            };

            var query = ctxt.CreateQuery<Product>("ThrowGeneralException", null, false, true);
            lo = ctxt.Load(query, callback, userState);

            // verify state before completion
            Assert.IsTrue(lo.CanCancel);
            Assert.AreSame(query, lo.EntityQuery);
            Assert.IsNull(lo.Error);
            Assert.IsFalse(lo.HasError);
            Assert.IsFalse(lo.IsCanceled);
            Assert.IsFalse(lo.IsComplete);
            Assert.IsTrue(lo.CanCancel);

            // subscribe to PC notifications      
            ((INotifyPropertyChanged)lo).PropertyChanged += (s, e) =>
            {
                Assert.IsFalse(lo.CanCancel);
                Assert.AreSame(lo, s);
                propChangeNotifications.Add(e.PropertyName);
            };

            // subscribe to completed event
            lo.Completed += (s, e) =>
            {
                LoadOperation<Product> o = (LoadOperation<Product>)s;

                // verify callback is called BEFORE the event is raised
                Assert.IsTrue(callbackCalled);

                Assert.IsTrue(o.IsComplete);
                Assert.IsTrue(o.HasError);
                Assert.IsFalse(o.CanCancel);
                Assert.IsFalse(o.IsCanceled);

                completedCalled = true;
            };

            Assert.IsFalse(Volatile.Read(ref completedCalled));
            Assert.IsFalse(Volatile.Read(ref callbackCalled));
            Assert.AreEqual(0, propChangeNotifications.Count);

            tcs.SetException(new DomainOperationException("error"));
            await lo;
            Assert.IsTrue(Volatile.Read(ref completedCalled));
            Assert.IsTrue(Volatile.Read(ref callbackCalled));


            // verify state after completion
            Assert.IsTrue(lo.HasError);
            Assert.IsFalse(lo.IsCanceled);
            Assert.IsTrue(lo.IsComplete);
            Assert.IsFalse(lo.CanCancel);

            // verify property change notifications and ordering
            Assert.AreEqual(5, propChangeNotifications.Count);
            Assert.AreEqual("IsErrorHandled", propChangeNotifications[0]);
            Assert.AreEqual("Error", propChangeNotifications[1]);
            Assert.AreEqual("HasError", propChangeNotifications[2]);
            Assert.AreEqual("IsComplete", propChangeNotifications[3]);
            Assert.AreEqual("CanCancel", propChangeNotifications[4]);
        }

        [TestMethod]
        public void Bug669603_QuerySerializationExceptionHandling()
        {
            Catalog catalog = CreateDomainContext();

            int[] prodIds = new int[] { 1, 2, 3, 4, 5 };
            EntityQuery<Product> query = catalog.GetProductsQuery().Where(p => prodIds.Contains(p.ProductID));

            NotSupportedException expectedException = null;
            try
            {
                catalog.Load(query, false);
            }
            catch (NotSupportedException e)
            {
                expectedException = e;
            }
            string expectedMsg = string.Format(CultureInfo.CurrentCulture, Resources.QuerySerialization_UnsupportedQueryOperator, "Contains");
            Assert.AreEqual(expectedMsg, expectedException.Message);
        }

        /// <summary>
        /// Verify that a DomainOperationEntry returning polymorphic results is correctly
        /// serialized to the client
        /// </summary>
        [TestMethod]
        public async Task TestInheritance()
        {
            TestDomainServices.TestProvider_Inheritance1 ctxt = new TestDomainServices.TestProvider_Inheritance1(new Uri(TestURIs.RootURI, "TestDomainServices-TestProvider_Inheritance1.svc"));
            LoadOperation<TestDomainServices.InheritanceC> result = ctxt.Load(ctxt.GetCsQuery(), false);

            await result;
            Assert.IsNull(result.Error);

            TestDomainServices.InheritanceC c = ctxt.InheritanceCs.First();
            Assert.AreEqual("AVal", c.InheritanceAProp);
            Assert.AreEqual("CVal", c.InheritanceCProp);
        }
#endif

        [TestMethod]
        public void Bug591588_OneToOneAssociationUpdate()
        {
            ScenariosEntityContainer ec = new ScenariosEntityContainer();

            C c1 = new C
            {
                ID = 1,
                DID_Ref1 = 1
            };
            C c2 = new C
            {
                ID = 2,
                DID_Ref1 = 2
            };
            D d1 = new D
            {
                ID = 1
            };
            D d2 = new D
            {
                ID = 2
            };
            ec.LoadEntities(new Entity[] { c1, c2, d1, d2 });

            // when this bug was first logged, the issue was an infinite loop.
            // that no longer occurs, however the change being attempted isn't
            // actually a valid data model - a 1:1 association is expected to
            // resolve to one and only one entity. In that case, EntityRef will
            // return null.

            // This is the repro line that was failing
            c1.D_Ref1 = c2.D_Ref1;
            Assert.AreSame(c1.D_Ref1, c2.D_Ref1);
            Assert.AreEqual(c1.DID_Ref1, c2.DID_Ref1);
        }

        [TestMethod]
        public async Task TestInvokingQueryWithSideEffects()
        {
            TestDomainServices.TestProvider_Scenarios provider = new TestDomainServices.TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            System.Linq.Expressions.Expression<Func<TestSideEffects, bool>> predicate = (res) => res.Name.StartsWith("Test");
            EntityQuery<TestSideEffects> entityQuery = provider.CreateAndGetSideEffectsObjectsQuery("TestName").Where(predicate);
            entityQuery.IncludeTotalCount = true;
            LoadOperation<TestSideEffects> result = provider.Load(entityQuery, false);

            await result;
            Assert.IsNull(result.Error);
            Assert.AreEqual(2, result.TotalEntityCount);
            Assert.AreEqual(2, result.Entities.Count);

            var entity = result.Entities.First();
            Assert.AreEqual("TestName", entity.Name);
            Assert.AreEqual("POST", entity.Verb);
            Assert.IsFalse(entity.URL.AbsoluteUri.Contains(@"$where"));
        }

        [TestMethod]
        public async Task TestReadOnlyMemberSerialization()
        {
            TestDomainServices.TestProvider_Scenarios provider = new TestDomainServices.TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            LoadOperation result = provider.Load(provider.GetAsQuery(), false);

            await result;
            Assert.IsNull(result.Error);

            TestDomainServices.A a = provider.As.First();

            Assert.AreEqual("ReadOnlyData", a.ReadOnlyData_NoReadOnlyAttribute);
            Assert.AreEqual("ReadOnlyData", a.ReadOnlyData_NoSetter);
            Assert.AreEqual("ReadOnlyData", a.ReadOnlyData_WithSetter);
        }

        /// <summary>
        /// Issue was the way URL encoding was handling '+' - this test executes a query
        /// with '+' operators so that they go all the way through the service layer to ensure
        /// we're handling them properly.
        /// </summary>
        [TestMethod]
        public async Task Bug479461_StringConcatQuery()
        {
            Catalog catalog = CreateDomainContext();

            var query = catalog.GetProductsQuery().Where(p => (p.ProductID + 5) > 0).OrderBy(p => (p.Name + "ZZ"));
            LoadOperation result = catalog.Load(query, false);

            await result;
            AssertSuccess();
        }

        [TestMethod]
        public async Task TestCycleRoot()
        {
            TestProvider_Scenarios ctxt = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);
            LoadOperation result = ctxt.Load(ctxt.GetTestCyclesRootQuery(), false);

            await result;
            Assert.IsNull(result.Error);

            // Verify resulting graph structure
            Assert.AreEqual(1, result.Entities.Count);
            Assert.AreEqual(2, result.Entities.Cast<TestCycles>().Single().IncludedTs.Count);
            Assert.AreEqual(63, result.AllEntities.Count);
        }

        [TestMethod]
        public async Task TestCycleTier1()
        {
            TestProvider_Scenarios ctxt = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);
            LoadOperation result = ctxt.Load(ctxt.GetTestCyclesTier1Query(), false);

            await result;
            Assert.IsNull(result.Error);

            // Verify resulting graph structure
            Assert.AreEqual(16, result.Entities.Count);
            Assert.AreEqual(16, result.Entities.Cast<TestCycles>().First().IncludedTs.Count);
            Assert.AreEqual(273, result.AllEntities.Count);
        }

        [TestMethod]
        public async Task Bug479449_Requery_RefreshCurrent()
        {
            Northwind nw = new Northwind(TestURIs.LTS_Northwind);

            var query = nw.GetOrderDetailsQuery().Take(5);
            Action<LoadOperation<DataTests.Northwind.LTS.Order_Detail>> action = delegate (LoadOperation<DataTests.Northwind.LTS.Order_Detail> o)
            {
                if (o.HasError)
                {
                    o.MarkErrorAsHandled();
                }
            };
            LoadOperation result = nw.Load(query, LoadBehavior.RefreshCurrent, action, "data");
            await result;
            Assert.IsNull(result.Error);
            Assert.AreEqual(LoadBehavior.RefreshCurrent, result.LoadBehavior);

            foreach (DataTests.Northwind.LTS.Order_Detail detail in nw.Order_Details)
            {
                Assert.AreEqual(detail.ProductID, detail.Product.ProductID);
            }

            result = nw.Load(query, LoadBehavior.RefreshCurrent, action, "data");
            await result;
            foreach (DataTests.Northwind.LTS.Order_Detail detail in nw.Order_Details)
            {
                Assert.AreEqual(detail.ProductID, detail.Product.ProductID);
            }
        }

        [TestMethod]
        public async Task TestProjectionIncludes()
        {
            TestDomainServices.IncludeScenariosTestProvider provider = new TestDomainServices.IncludeScenariosTestProvider(new Uri(TestURIs.RootURI, "TestDomainServices-IncludeScenariosTestProvider.svc"));
            LoadOperation result = provider.Load(provider.GetAsQuery(), false);

            await result;
            Assert.IsNull(result.Error);
            List<TestDomainServices.IncludesA> results = provider.IncludesAs.ToList();

            // verify all projections
            TestDomainServices.IncludesA a = results[0];
            Assert.AreEqual("BP1", a.BP1);
            Assert.AreEqual("CP1", a.CP1);
            Assert.AreEqual("DP2", a.DP2);

            // verify projections in hierarchy with null links
            a = results[1];
            Assert.AreEqual("BP1", a.BP1);
            Assert.AreEqual(null, a.CP1);
            Assert.AreEqual(null, a.DP2);

            // verify that projection properties are read-only
            EditableAttribute editableAttribute = (EditableAttribute)a.GetType().GetProperty("BP1").GetCustomAttributes(typeof(EditableAttribute), false).Single();
            Assert.IsFalse(editableAttribute.AllowEdit);
        }

        [TestMethod]
        public async Task TestQueryProjectionIncludes()
        {
            TestDomainServices.IncludeScenariosTestProvider provider = new TestDomainServices.IncludeScenariosTestProvider(new Uri(TestURIs.RootURI, "TestDomainServices-IncludeScenariosTestProvider.svc"));
            LoadOperation result1 = provider.Load(provider.GetAsQuery().Where(p => p.ID == 1 && p.DP2 == "DP2"), false);
            LoadOperation result2 = provider.Load(provider.GetAsQuery().Where(p => p.ID == 1 && p.DP2.ToLower() == "dp2"), false);

            await result1;
            await result2;
            Assert.IsNull(result1.Error);
            Assert.IsNull(result2.Error);
            Assert.AreEqual(1, provider.IncludesAs.Count);
        }

        /// <summary>
        /// Execute two queries and verify results are accumulated and EntitySet change notifications
        /// are fired as expected
        /// </summary>
        [TestMethod]
        public async Task TestEntitySet_Accumulation()
        {
            Catalog catalog = CreateDomainContext();
            string classFilter = "M ";
            string styleFilter = "W ";
            int listChangeNotifications = 0;

            ((INotifyCollectionChanged)catalog.Products).CollectionChanged += delegate (object sender, NotifyCollectionChangedEventArgs e)
            {
                listChangeNotifications += 1;
            };

            var query = catalog.GetProductsQuery().Where(p => p.Class == classFilter && p.Style == styleFilter);
            LoadOperation result = catalog.Load(query, false);

            await result;
            Assert.AreEqual(22, catalog.Products.Where(p => p.Class == classFilter && p.Style == styleFilter).Count());
            Assert.AreEqual(22, listChangeNotifications);

            listChangeNotifications = 0;
            styleFilter = "U ";
            result = catalog.Load(query, false);

            await result;
            Assert.AreEqual(22, catalog.Products.Where(p => p.Class == classFilter && p.Style == styleFilter).Count());
            Assert.AreEqual(22, listChangeNotifications);
        }

        [TestMethod]
        public async Task TestAssociations_FKSynchronization()
        {
            Catalog catalog = CreateDomainContext();

            LoadOperation lo = catalog.Load(catalog.GetPurchaseOrdersQuery().Take(2), false);

            await lo;
            PurchaseOrder[] orders = catalog.PurchaseOrders.ToArray();

            PurchaseOrder po = orders[0];
            PurchaseOrderDetail detail = po.PurchaseOrderDetails.First();
            Assert.AreEqual(po.PurchaseOrderID, detail.PurchaseOrderID);

            // now set the detail's order ref to a new entity and verify
            // FKs are synched properly
            PurchaseOrder newOrder = orders[1];
            detail.PurchaseOrder = newOrder;
            Assert.AreEqual(newOrder.PurchaseOrderID, detail.PurchaseOrderID);
            Assert.AreSame(newOrder, detail.PurchaseOrder);

            // now set the detail's order ref to null
            detail.PurchaseOrder = null;
            Assert.AreEqual(0, detail.PurchaseOrderID);
            Assert.AreEqual(null, detail.PurchaseOrder);
        }

        [TestMethod]
        public async Task TestLoadLoadBehaviors()
        {
            Product cachedProd = null;
            Catalog catalog = CreateDomainContext();

            // use internal access to load an entity manually
            Product prod = new Product
            {
                ProductID = 317,
                Color = "Red"
            };
            catalog.Products.LoadEntity(prod);

            // now issue a query for the same entity using
            // LoadBehavior.KeepCurrent
            var query = catalog.GetProductsQuery().Where(p => p.ProductID >= 317 && p.ProductID <= 321);
            LoadOperation<Product> lo = catalog.Load(query, LoadBehavior.KeepCurrent, false);

            await lo;
            // Assert that the event args contains the cached value
            List<Product> loadedProducts = lo.Entities.ToList();
            Product loadedProduct = loadedProducts.Single(p => p.ProductID == prod.ProductID);
            Assert.AreSame(prod, loadedProduct);

            Assert.AreEqual(5, catalog.Products.Count);
            cachedProd = catalog.Products.Single(p => p.ProductID == prod.ProductID);

            // Assert that the instances are equal and that
            // our cached values haven't been overwritten
            Assert.AreSame(prod, cachedProd);
            Assert.AreEqual("Red", cachedProd.Color);

            // now issue a query for the same entity using
            // LoadBehavior.RefreshCurrent
            lo = catalog.Load(query, LoadBehavior.RefreshCurrent, false);

            await lo;
            // Assert that the event args contains the cached value
            loadedProducts = lo.Entities.ToList();
            loadedProduct = loadedProducts.Single(p => p.ProductID == prod.ProductID);
            Assert.AreSame(prod, loadedProduct);

            Assert.AreEqual(5, catalog.Products.Count);
            cachedProd = catalog.Products.Single(p => p.ProductID == prod.ProductID);

            // Assert that the instances are equal and that
            // our cached values HAVE been overwritten
            Assert.AreSame(prod, cachedProd);
            Assert.AreEqual("Black", cachedProd.Color);
        }

        [TestMethod]
        public async Task TestMethodWithParameters()
        {
            Catalog catalog = CreateDomainContext();

            var query = from p in catalog.GetProductsByCategoryQuery(1)
                        where (p.Color == "Black" || p.Color == "Silver") &&
                              p.FinishedGoodsFlag == true
                        select p;

            LoadOperation lo = catalog.Load(query, false);

            await lo;
            Assert.AreEqual(32, catalog.Products.Count);
        }

        [TestMethod]
        public async Task TestMethodWithParameters_PrimitiveTypes()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            var query = provider.GetMixedTypes_PrimitiveQuery("MixedType_Max", true, 123, 123, 123, 123, 123, 123, 123, 123, (char)123, 123.123, (float)123.123);
            LoadOperation lo = provider.Load(query, false);

            await lo;
            Assert.IsNull(lo.Error);

            Assert.AreEqual(3, lo.Entities.Count, "Entities count should be 3");
            MixedType changedObj = provider.MixedTypes.Single(t => t.ID == "MixedType_Max");
            Assert.AreEqual(true, changedObj.BooleanProp);
            Assert.AreEqual<Byte>(123, changedObj.ByteProp);
            Assert.AreEqual<Int16>(123, changedObj.Int16Prop);
            Assert.AreEqual<UInt16>(123, changedObj.UInt16Prop);
            Assert.AreEqual<Int32>(123, changedObj.Int32Prop);
            Assert.AreEqual<UInt32>(123, changedObj.UInt32Prop);
            Assert.AreEqual<Int64>(123, changedObj.Int64Prop);
            Assert.AreEqual<UInt64>(123, changedObj.UInt64Prop);
            Assert.AreEqual<Char>((char)123, changedObj.CharProp);
            Assert.AreEqual<Double>(123.123, changedObj.DoubleProp);
            Assert.AreEqual<Single>((Single)123.123, changedObj.SingleProp);
        }

        [TestMethod]
        public async Task TestMethodWithParameters_PrimitiveTypes_MaxAndNaN()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            var query = provider.GetMixedTypes_PrimitiveQuery("MixedType_Max", true, byte.MaxValue, sbyte.MaxValue, short.MaxValue, ushort.MaxValue, int.MaxValue, uint.MaxValue, long.MaxValue, ulong.MaxValue, char.MaxValue, double.NaN, float.NaN);
            LoadOperation lo = provider.Load(query, false);

            await lo;
            Assert.IsNull(lo.Error);

            Assert.AreEqual(3, lo.Entities.Count, "Entities count should be 3");
            MixedType changedObj = provider.MixedTypes.Single(t => t.ID == "MixedType_Max");
            Assert.AreEqual(true, changedObj.BooleanProp);
            Assert.AreEqual<Byte>(byte.MaxValue, changedObj.ByteProp);
            Assert.AreEqual<Int16>(short.MaxValue, changedObj.Int16Prop);
            Assert.AreEqual<UInt16>(ushort.MaxValue, changedObj.UInt16Prop);
            Assert.AreEqual<Int32>(int.MaxValue, changedObj.Int32Prop);
            Assert.AreEqual<UInt32>(uint.MaxValue, changedObj.UInt32Prop);
            Assert.AreEqual<Int64>(long.MaxValue, changedObj.Int64Prop);
            Assert.AreEqual<UInt64>(ulong.MaxValue, changedObj.UInt64Prop);
            Assert.AreEqual<Char>(char.MaxValue, changedObj.CharProp);
            Assert.AreEqual<Double>(double.NaN, changedObj.DoubleProp);
            Assert.AreEqual<Single>(float.NaN, changedObj.SingleProp);
        }

        [TestMethod]
        [FullTrustTest] // ISerializable types cannot be deserialized in medium trust.
        public async Task TestMethodWithParameters_PredefinedTypes()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);
            string[] strings = { "hello", "world" };
            int[] ints = { 5, 6 };
            DateTime dt = new DateTime(2009, 9, 10);
            TimeSpan ts = new TimeSpan(123);
            DateTimeOffset dto = new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(1, 0, 0));
            Uri uri = new Uri("http://localhost");
            Guid guid = new Guid();
            XElement xElem = XElement.Parse("<myNode xmlns=\"foo\">node text</myNode>");
            var dictDTO = CreateDictionary(dto);
            var dictDT = CreateDictionary(dt);
            var dictGuid = CreateDictionary(guid);
            var dictString = CreateDictionary("some string");
            var dictEnum = CreateDictionary(TestEnum.Value2);
            var dictXE = CreateDictionary(xElem);

            var query = provider.GetMixedTypes_PredefinedQuery("MixedType_Max", "hello", 123, dt, ts, dto, strings, uri, guid, new byte[] { 0, 111, 222 }, xElem, new byte[] { 123, 234 }, TestEnum.Value1 | TestEnum.Value2, ints, dictDT, dictGuid, dictString, dictEnum, dictXE, dictDTO);
            LoadOperation lo = provider.Load(query, false);

            await lo;
            Assert.IsNull(lo.Error);

            Assert.AreEqual(3, lo.Entities.Count);
            MixedType changedObj = provider.MixedTypes.Single(t => t.ID == "MixedType_Max");
            Assert.AreEqual<string>("hello", changedObj.StringProp);
            Assert.AreEqual<Decimal>(123, changedObj.DecimalProp);
            Assert.AreEqual<DateTime>(dt, changedObj.DateTimeProp);
            Assert.AreEqual<TimeSpan>(ts, changedObj.TimeSpanProp);
            Assert.AreEqual<DateTimeOffset>(dto, changedObj.DateTimeOffsetProp);
            Assert.AreEqual<Uri>(uri, changedObj.UriProp);
            Assert.AreEqual<Guid>(guid, changedObj.GuidProp);
            Assert.AreEqual(3, changedObj.BinaryProp.Length);
            Assert.AreEqual(222, changedObj.BinaryProp[2]);
            Assert.AreEqual(2, changedObj.ByteArrayProp.Length);
            Assert.AreEqual(123, changedObj.ByteArrayProp[0]);
            Assert.AreEqual<TestEnum>(TestEnum.Value1 | TestEnum.Value2, changedObj.EnumProp);
            Assert.AreEqual("<myNode xmlns=\"foo\">node text</myNode>", changedObj.XElementProp.ToString());
            Assert.AreEqual("node text", changedObj.XElementProp.Value);

            Assert.IsTrue(CompareDictionaries(dictDT, changedObj.DictionaryDateTimeProp));
            Assert.IsTrue(CompareDictionaries(dictDTO, changedObj.DictionaryDateTimeOffsetProp));
            Assert.IsTrue(CompareDictionaries(dictGuid, changedObj.DictionaryGuidProp));
            Assert.IsTrue(CompareDictionaries(dictString, changedObj.DictionaryStringProp));
            Assert.IsTrue(CompareDictionaries(dictEnum, changedObj.DictionaryTestEnumProp));
            Assert.IsTrue(CompareDictionaries(dictXE, changedObj.DictionaryXElementProp));

            Assert.IsNotNull(changedObj.StringsProp);
            Assert.IsNotNull(changedObj.IntsProp);

            var returnedStrings = changedObj.StringsProp.ToArray();
            Assert.AreEqual(strings.Length, returnedStrings.Length);
            for (int i = 0; i < returnedStrings.Length; i++)
            {
                Assert.AreEqual(strings[i], returnedStrings[i]);
            }

            var returnedInts = changedObj.IntsProp;
            Assert.AreEqual(ints.Length, returnedInts.Length);
            for (int i = 0; i < returnedInts.Length; i++)
            {
                Assert.AreEqual(ints[i], returnedInts[i]);
            }
        }

        [TestMethod]
        public async Task TestMethod_ComplexQueryProperties()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            var query = provider.GetMixedTypesQuery()
                .Where(x => x.UriProp == new Uri("http://localhost") && x.TimeSpanProp == new TimeSpan(123)
                       && x.IntsProp.Length == 2 && x.NullableInt32Prop.ToString() == "123");
            LoadOperation lo = provider.Load(query, false);

            await lo;
            Assert.IsNull(lo.Error);
            Assert.AreEqual(1, lo.Entities.Count);
            Assert.AreEqual(new Uri("http://localhost"), provider.MixedTypes.First().UriProp);
            Assert.AreEqual(new TimeSpan(123), provider.MixedTypes.First().TimeSpanProp);
        }

        [TestMethod]
        [Asynchronous]
        [FullTrustTest] // ISerializable types cannot be deserialized in medium trust.
        public async Task TestMethodWithParameters_PredefinedTypes_WithNull()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);
            DateTime dt = new DateTime(2009, 9, 10);
            TimeSpan ts = new TimeSpan(123);
            DateTimeOffset dto = new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(2, 0, 0));
            Uri uri = new Uri("http://localhost");
            Guid guid = Guid.NewGuid();
            var dictDT = CreateDictionary(dt);
            var dictDTO = CreateDictionary(dto);
            var dictGuid = CreateDictionary(guid);
            var dictString = CreateDictionary("some string");
            var dictEnum = CreateDictionary(TestEnum.Value2);
            var dictXE = CreateDictionary(XElement.Parse("<a>{<b>.</b>}</a>"));

            var query = provider.GetMixedTypes_PredefinedQuery("MixedType_Max", null, 123, dt, ts, dto, null, uri, guid, null, null, null, TestEnum.Value1 | TestEnum.Value2, null, dictDT, dictGuid, dictString, dictEnum, dictXE, dictDTO);
            LoadOperation lo = provider.Load(query, false);

            await lo;
            Assert.IsNull(lo.Error);
            Assert.AreEqual(3, lo.Entities.Count);
            MixedType changedObj = provider.MixedTypes.Single(t => t.ID == "MixedType_Max");
            Assert.IsNull(changedObj.StringProp);
            Assert.IsNull(changedObj.BinaryProp);
            Assert.IsNull(changedObj.ByteArrayProp);
            Assert.IsNull(changedObj.XElementProp);
            Assert.IsNull(changedObj.StringsProp);
            Assert.IsNull(changedObj.IntsProp);
        }

        [TestMethod]
        public async Task TestMethodWithParameters_NullableTypes()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);
            TimeSpan?[] nullableTimeSpans =
            {
                TimeSpan.FromSeconds(5),
                null,
                TimeSpan.FromSeconds(6)
            };
            DateTime dt = new DateTime(2009, 2, 2);
            TimeSpan ts = new TimeSpan(123);
            Guid guid = new Guid();
            Dictionary<DateTime, DateTime?> nullDictDT = CreateDictionary<DateTime, DateTime?>(dt, dt);

            var query = provider.GetMixedTypes_NullableQuery("MixedType_Max", true, 123, 123, 123, 123, 123, 123, 123, 123, (char)123, 123.123, (float)123.123,
                123, dt, ts, guid, TestEnum.Value1 | TestEnum.Value2, nullableTimeSpans, nullDictDT);
            LoadOperation lo = provider.Load(query, false);

            await lo;
            Assert.IsNull(lo.Error);
            Assert.AreEqual(3, lo.Entities.Count);
            MixedType changedObj = provider.MixedTypes.Single(t => t.ID == "MixedType_Max");
            Assert.AreEqual<Boolean?>(true, changedObj.NullableBooleanProp);
            Assert.AreEqual<Byte?>(123, changedObj.NullableByteProp);
            Assert.AreEqual<Int16?>(123, changedObj.NullableInt16Prop);
            Assert.AreEqual<UInt16?>(123, changedObj.NullableUInt16Prop);
            Assert.AreEqual<Int32?>(123, changedObj.NullableInt32Prop);
            Assert.AreEqual<UInt32?>(123, changedObj.NullableUInt32Prop);
            Assert.AreEqual<Int64?>(123, changedObj.NullableInt64Prop);
            Assert.AreEqual<UInt64?>(123, changedObj.NullableUInt64Prop);
            Assert.AreEqual<Char?>((char)123, changedObj.NullableCharProp);
            Assert.AreEqual<Double?>(123.123, changedObj.NullableDoubleProp);
            Assert.AreEqual<Single?>((Single)123.123, changedObj.NullableSingleProp);
            Assert.AreEqual<Decimal?>(123, changedObj.NullableDecimalProp);
            Assert.AreEqual<DateTime?>(dt, ((DateTime)changedObj.NullableDateTimeProp));
            Assert.AreEqual<TimeSpan?>(ts, ((TimeSpan)changedObj.NullableTimeSpanProp));
            Assert.AreEqual<Guid?>(guid, changedObj.NullableGuidProp);
            Assert.AreEqual<TestEnum?>(TestEnum.Value1 | TestEnum.Value2, changedObj.NullableEnumProp);
            Assert.IsTrue(CompareDictionaries(nullDictDT, changedObj.NullableDictionaryDateTimeProp));

            var returnedTimeSpans = changedObj.NullableTimeSpanListProp.ToArray();
            Assert.AreEqual(3, returnedTimeSpans.Length);
            Assert.AreEqual(nullableTimeSpans[0], returnedTimeSpans[0]);
            Assert.IsNull(returnedTimeSpans[1]);
            Assert.AreEqual(nullableTimeSpans[2], returnedTimeSpans[2]);
        }

        [TestMethod]
        public async Task TestMethodWithParameters_NullableTypes_WithNull()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);
            DateTime dt = new DateTime(2009, 2, 2);

            var query = provider.GetMixedTypes_NullableQuery("MixedType_Max", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
            LoadOperation lo = provider.Load(query, false);

            await lo;
            Assert.IsNull(lo.Error);
            Assert.AreEqual(3, lo.Entities.Count);
            MixedType changedObj = provider.MixedTypes.Single(t => t.ID == "MixedType_Max");
            Assert.IsNull(changedObj.NullableBooleanProp);
            Assert.IsNull(changedObj.NullableByteProp);
            Assert.IsNull(changedObj.NullableInt16Prop);
            Assert.IsNull(changedObj.NullableUInt16Prop);
            Assert.IsNull(changedObj.NullableInt32Prop);
            Assert.IsNull(changedObj.NullableUInt32Prop);
            Assert.IsNull(changedObj.NullableInt64Prop);
            Assert.IsNull(changedObj.NullableUInt64Prop);
            Assert.IsNull(changedObj.NullableCharProp);
            Assert.IsNull(changedObj.NullableDoubleProp);
            Assert.IsNull(changedObj.NullableSingleProp);
            Assert.IsNull(changedObj.NullableDecimalProp);
            Assert.IsNull(changedObj.NullableDateTimeProp);
            Assert.IsNull(changedObj.NullableGuidProp);
            Assert.IsNull(changedObj.NullableEnumProp);
            Assert.IsNull(changedObj.NullableTimeSpanListProp);
            Assert.IsNull(changedObj.NullableDictionaryDateTimeProp);
        }

        [TestMethod]
        public async Task TestAssociations()
        {
            Catalog catalog = CreateDomainContext();

            var query = from p in catalog.GetPurchaseOrdersQuery()
                        where p.Freight > 50 && p.OrderDate >= new DateTime(1974, 8, 13)
                        select p;
            LoadOperation lo = catalog.Load(query.Take(5), false);

            await lo;
            List<PurchaseOrder> orders = catalog.PurchaseOrders.ToList();
            Assert.IsTrue(orders.Count > 0);
            PurchaseOrder order = orders.First();

            // enumerate the PurchaseOrderDetails collection
            Assert.IsTrue(order.PurchaseOrderDetails.Count() > 0);
            PurchaseOrderDetail detail = order.PurchaseOrderDetails.First();

            // verify the back reference
            Assert.AreSame(order, detail.PurchaseOrder);

            Product prod = detail.Product;
            Assert.AreEqual(detail.ProductID, prod.ProductID);
        }

        [TestMethod]
        public async Task TestDomainContextCancelLoad()
        {
            Catalog catalog = CreateDomainContext();

            var query = catalog.GetProductsQuery();
            LoadOperation lo = catalog.Load(query, false);

            lo.Cancel();
            Assert.IsTrue(lo.IsCancellationRequested, "Cancellation should be requested");

            await lo;
            Assert.IsTrue(lo.IsCanceled);
            Assert.IsNull(lo.Error);

            // call load again to verify that cancelled state is managed
            // properly for a subsequent successfull query
            lo = catalog.Load(query, false);

            await lo;
            Assert.IsFalse(lo.IsCanceled);
            Assert.IsNull(lo.Error);
            Assert.AreEqual(504, lo.Entities.Count);
        }

        [TestMethod]
        public async Task TestDomainContext_IsLoadingPropertyChangeNotifications()
        {
            var propertyChangeCalled = new SemaphoreSlim(0);
            PropertyChangedEventArgs loadingEventArgs = null;
            PropertyChangedEventArgs loadedEventArgs = null;
            object savedSender = null;
            int isLoadingEventCount = 0;
            bool loadCompleteDuringLoading = false;
            bool? isLoadingDuringPropertyChange = null;

            Catalog catalog = CreateDomainContext();

            Assert.IsFalse(catalog.IsLoading);
            Assert.IsFalse(this.IsLoadComplete);

            catalog.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                savedSender = sender;
                isLoadingDuringPropertyChange = catalog.IsLoading;
                if (catalog.IsLoading)
                {
                    loadingEventArgs = e;
                    loadCompleteDuringLoading = this.IsLoadComplete;
                }
                else
                {
                    loadedEventArgs = e;
                }
                isLoadingEventCount++;
                if (isLoadingEventCount == 1)
                    propertyChangeCalled.Release();
            };

            LoadOperation lo = catalog.Load(catalog.GetProductsQuery(), false);

            bool waitSuccess = await propertyChangeCalled.WaitAsync(TimeSpan.FromSeconds(30));
            propertyChangeCalled.Dispose();

            Assert.IsTrue(waitSuccess, "Waited to long for callback to be executed, semaphore note signalled");

            Assert.IsNotNull(loadingEventArgs);
            Assert.IsFalse(loadCompleteDuringLoading);
            Assert.AreEqual("IsLoading", loadingEventArgs.PropertyName);
            Assert.AreSame(catalog, savedSender);  // verify sender
            Assert.AreEqual(true, isLoadingDuringPropertyChange, "IsLoading should have been true");

            await lo;
            Assert.AreEqual("IsLoading", loadedEventArgs.PropertyName);
            Assert.AreEqual(2, isLoadingEventCount);
            Assert.AreEqual(false, isLoadingDuringPropertyChange, "IsLoading should have been true");
        }

        [TestMethod]
        public async Task TestMultipartQuery()
        {
            Catalog catalog = CreateDomainContext();

            bool callbackCalled = false;
            object userState = new object();
            Action<LoadOperation<Product>> callback =
                (o) =>
                {
                    callbackCalled = true;
                    Assert.AreSame(userState, o.UserState);

                    if (o.HasError)
                    {
                        o.MarkErrorAsHandled();
                    }
                };

            EntityQuery<Product> query =
                from p in catalog.GetProductsQuery()
                where p.ListPrice > 10
                orderby p.ListPrice, p.Style descending
                select p;
            query = query.Skip(2).Take(3);

            LoadOperation<Product> lo = catalog.Load(query, callback, userState);

            await lo;
            Assert.IsTrue(callbackCalled);
            Assert.IsNull(lo.Error);
            Assert.IsTrue(lo.Entities.Count > 0);
        }

        [TestMethod]
        public async Task TestAscDescSorting()
        {
            Catalog catalog = CreateDomainContext();

            var query = (from p in catalog.GetProductsQuery()
                         orderby p.ProductID descending, p.ListPrice ascending
                         select p).Skip(0).Take(10);

            LoadOperation lo = catalog.Load(query, false);

            await lo;
            Assert.AreEqual(10, catalog.Products.Count);
            Product[] products = catalog.Products.ToArray();
            Assert.IsTrue(products[0].ProductID > products[1].ProductID);
            Assert.IsTrue(products[0].ListPrice <= products[1].ListPrice);
            Assert.IsTrue(products[1].ListPrice <= products[2].ListPrice);
        }

        [TestMethod]
        public async Task TestDuplicateQueryOperations()
        {
            Catalog catalog = CreateDomainContext();

            EntityQuery<Product> query = null;
            query = from p in catalog.GetProductsQuery()
                    where p.Color == "Yellow"
                    orderby p.ListPrice
                    where p.Weight > 10
                    orderby p.Style
                    select p;
            query = query.Skip(2).Take(3);
            query = query.Where(p => p.Color != "Purple");

            LoadOperation lo = catalog.Load(query, false);

            await lo;
            Assert.IsTrue(catalog.Products.Count > 0);
        }

        [TestMethod]
        public async Task TestLoadOperationResults()
        {
            int prodCount = 0;
            object userState = this;

            Catalog catalog = CreateDomainContext();

            Action<LoadOperation<Product>> action = delegate (LoadOperation<Product> o)
            {
                if (o.HasError)
                {
                    o.MarkErrorAsHandled();
                }
            };
            LoadOperation lo = catalog.Load(catalog.GetProductsQuery().Where(p => p.ListPrice > 2000), LoadBehavior.KeepCurrent, action, userState);

            await lo;
            Assert.AreSame(userState, lo.UserState);

            // the LoadedEntities on the event args should equal all the loaded products
            prodCount = catalog.Products.Count;
            Assert.IsTrue(prodCount > 0);
            Assert.IsTrue(lo.Entities.Cast<Product>().OrderBy(p => p.ProductID).SequenceEqual(catalog.Products.OrderBy(p => p.ProductID)));

            // verify error is null
            Assert.IsNull(lo.Error);

            // load another set of products
            lo = catalog.Load(catalog.GetProductsQuery().Where(p => p.ListPrice < 1500), false);

            await lo;
            // the LoadedEntities on the event args should equal all the loaded products
            Assert.IsTrue(catalog.Products.Count > prodCount);  // make sure we read more products
            Assert.IsTrue(lo.Entities.Cast<Product>().OrderBy(p => p.ProductID).SequenceEqual(catalog.Products.Where(p => p.ListPrice < 1500).OrderBy(p => p.ProductID)));
        }

        [TestMethod]
        public async Task TestLoadOperationState()
        {
            Catalog catalog = CreateDomainContext();
            int prodCount = 0;
            object userState = this;

            var query = catalog.GetProductsQuery().Where(p => p.ListPrice > 2000);
            Action<LoadOperation<Product>> action = delegate (LoadOperation<Product> o)
            {
                if (o.HasError)
                {
                    o.MarkErrorAsHandled();
                }
            };
            LoadOperation<Product> lo = catalog.Load(query, action, userState);

            await lo;
            Assert.AreSame(userState, lo.UserState);
            prodCount = catalog.Products.Count;
            Assert.IsTrue(prodCount > 0);
            Assert.IsTrue(lo.Entities.OrderBy(p => p.ProductID).SequenceEqual(catalog.Products.OrderBy(p => p.ProductID)));

            // verify error is null
            Assert.IsNull(lo.Error);

            // load another set of products
            query = catalog.GetProductsQuery().Where(p => p.ListPrice < 1500);
            lo = catalog.Load(query, false);

            await lo;
            // the LoadedEntities on the event args should equal all the loaded products
            Assert.IsTrue(catalog.Products.Count > prodCount);  // make sure we read more products
            Assert.IsTrue(lo.Entities.OrderBy(p => p.ProductID).SequenceEqual(catalog.Products.Where(p => p.ListPrice < 1500).OrderBy(p => p.ProductID)));
        }

        [TestMethod]
        public async Task TestLoad_DataContext_ResultCounts()
        {
            Catalog catalog = CreateDomainContext();

            LoadOperation lo = catalog.Load(catalog.GetPurchaseOrdersQuery()
                .OrderBy(c => c.PurchaseOrderID).Take(5), false);

            await lo;
            Assert.IsNull(lo.Error, (lo.Error != null) ? lo.Error.ToString() : null);
            Assert.AreEqual<int>(5, lo.Entities.Count);
            Assert.AreEqual<int>(17, lo.AllEntities.Count);
        }

        [TestMethod]
        public async Task TestLoad_ResultCounts()
        {
            int purchaseOrdersToTake = 5;

            // This value is an assumption based on our read-only database
            // making this particular call with this particular request to
            // only take 5 purchase orders.  The query will include some
            // related entities.  While having this hard-coded value is
            // somewhat unfortunate, this test is specifically testing
            // these counts.
            int relatedEntitiesIncluded = 12;

            Catalog catalog = CreateDomainContext();

            var query = catalog.GetPurchaseOrdersQuery()
                .OrderBy(c => c.PurchaseOrderID)
                .Take(purchaseOrdersToTake);
            query.IncludeTotalCount = true;

            LoadOperation lo = catalog.Load(query, LoadBehavior.RefreshCurrent, false);

            await lo;
            Assert.IsFalse(lo.IsCanceled);
            Assert.AreEqual<int>(PurchaseOrderCountInDatabase, lo.TotalEntityCount);
            Assert.AreEqual<int>(purchaseOrdersToTake, lo.Entities.Count);
            Assert.AreEqual<int>(purchaseOrdersToTake + relatedEntitiesIncluded, lo.AllEntities.Count);
        }

        /// <summary>
        /// Verify that an empty result set is handled properly
        /// </summary>
        [TestMethod]
        public async Task TestLoad_NoResults()
        {
            Catalog catalog = CreateDomainContext();

            var query = catalog.GetProductsQuery().Where(p => p.ProductID < 0);
            LoadOperation lo = catalog.Load(query, false);

            await lo;
            List<Product> products = catalog.Products.ToList();
            Assert.IsTrue(products.Count == 0);
            Assert.IsTrue(lo.Entities.Count == 0);
        }

        private class TestUserState
        {
            public int A;
            public string B;
        }

        /// <summary>
        /// Verify user state is passed through async Load calls and is available after
        /// completion
        /// </summary>
        [TestMethod]
        public async Task TestLoad_UserState()
        {
            TestUserState myUserState = new TestUserState
            {
                A = 1,
                B = "Test"
            };
            Catalog catalog = CreateDomainContext();
            Action<LoadOperation<Product>> action = delegate (LoadOperation<Product> o)
            {
                if (o.HasError)
                {
                    o.MarkErrorAsHandled();
                }
            };
            LoadOperation lo = catalog.Load(catalog.GetProductsQuery().Where(p => p.ProductID == 1), action, myUserState);

            await lo;
            Assert.IsNull(lo.Error, (lo.Error != null) ? lo.Error.ToString() : null);
            Assert.IsTrue(catalog.Products.Count == 1);
            Assert.AreSame(myUserState, lo.UserState);
        }

        [TestMethod]
        public async Task TestLoad_MultipleCalls()
        {
            const int numberOfActiveLoadCalls = 10;
            Catalog catalog = new Catalog(this.ServiceUri);

            List<LoadOperation> loadOps = new List<LoadOperation>();
            for (int i = 0; i < numberOfActiveLoadCalls; i++)
            {
                loadOps.Add(catalog.Load(catalog.GetProductsQuery().Skip(i * 5).Take(5), false));
            }

            foreach (LoadOperation op in loadOps)
                await op;
            for (int i = 0; i < numberOfActiveLoadCalls; i++)
            {
                if (loadOps[i].Error != null)
                {
                    Assert.Fail(loadOps[i].Error.ToString());
                }
                Assert.AreEqual(5, loadOps[i].Entities.Count);
            }
            Assert.AreEqual(numberOfActiveLoadCalls * 5, catalog.Products.Count);
        }

        [TestMethod]
        public async Task TestLoad_MultipleCalls_Cancel()
        {
            const int numberOfActiveLoadCalls = 10;
            Catalog tempCatalog = new Catalog(this.ServiceUri);
            Catalog catalog = new Catalog(this.ServiceUri);
            object syncObject = new object();

            List<int> productIDs = new List<int>();
            LoadOperation[] loadOperations = new LoadOperation[numberOfActiveLoadCalls];

            // Load 10 products to get 10 product IDs.
            LoadOperation lo = tempCatalog.Load(tempCatalog.GetProductsQuery().Take(numberOfActiveLoadCalls), false);

            SemaphoreSlim[] waitHandles = new SemaphoreSlim[numberOfActiveLoadCalls];
            for (int i = 0; i < numberOfActiveLoadCalls; ++i)
                waitHandles[i] = new SemaphoreSlim(0);

            await lo;
            Assert.IsNull(lo.Error);
            Assert.AreEqual(numberOfActiveLoadCalls, tempCatalog.Products.Count);

            var products = tempCatalog.Products.ToList();
            for (int i = 0; i < numberOfActiveLoadCalls; ++i)
            {
                // Do this in separate threads to prevent this becoming a synchronous process.
                // [Ron] My single-proc machine generally has finished all the loads by the time
                // the cancel is issued.  Using ThreadPool breaks this behavior.
                ThreadPool.QueueUserWorkItem((object state) =>
                {
                    int index = (int)state;
                    Product thisProduct = products[index];
                    LoadOperation thisLoad;
                    try
                    {
                        lock (syncObject)
                        {
                            thisLoad = catalog.Load(catalog.GetProductsQuery().Where(p => p.ProductID == thisProduct.ProductID), throwOnError: false);
                            // When have asked for Product[5], issue a cancel.
                            if (index == 5)
                            {
                                thisLoad.Cancel();
                            }
                            loadOperations[index] = thisLoad;
                        }

                        thisLoad.Completed += (_, __) => waitHandles[index].Release();
                    }
                    catch
                    {
                        waitHandles[index].Release();
                    }
                }, i);
            }

            // Wait for loads to complete with a timeout
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            for (int i = 0; i < numberOfActiveLoadCalls; i++)
            {
                await waitHandles[i].WaitAsync(cts.Token);
                waitHandles[i].Dispose();

                Assert.IsTrue(loadOperations[i].IsComplete);

                if (!loadOperations[i].IsCanceled)
                {
                    Assert.IsNull(loadOperations[i].Error);
                    Assert.AreEqual(1, loadOperations[i].Entities.Count);
                    var product = (Product)loadOperations[i].Entities.First();
                    Assert.AreNotEqual(products[5].ProductID, product.ProductID);
                }
            }
            Assert.AreEqual((numberOfActiveLoadCalls - 1), catalog.Products.Count);
            Assert.AreEqual(1, loadOperations.Where(a => a.IsCanceled).Count());

            cts.Dispose();
        }

        [TestMethod]
        public async Task TestLoad_Cancel()
        {
            Catalog catalog = CreateDomainContext();

            var query = catalog.GetProductsQuery().Where(p => p.ProductID == 1);
            LoadOperation lo = catalog.Load(query, LoadBehavior.RefreshCurrent, false);
            Assert.IsTrue(lo.CanCancel, "Cancellation should be supported.");

            // after starting the load, issue a cancel
            lo.Cancel();

            await lo;
            Assert.AreEqual(0, catalog.Products.Count);
            Assert.IsTrue(lo.IsCanceled, "Operation should've been cancelled.");

            var cts = new CancellationTokenSource();
            var loadTask = catalog.LoadAsync(query, cts.Token);
            cts.Cancel();

            await ExceptionHelper.ExpectExceptionAsync<OperationCanceledException>(() => loadTask);
            Assert.IsTrue(loadTask.IsCanceled, "Task should be cancelled");
        }

        [TestMethod]
        public async Task TestBeginLoad_CancelFast()
        {
            Catalog catalog = CreateDomainContext();

            LoadOperation lo1 = catalog.Load(catalog.GetProductsQuery().Where(p => p.ProductID == 1), LoadBehavior.RefreshCurrent, false);
            LoadOperation lo2 = catalog.Load(catalog.GetProductsQuery().Where(p => p.ProductID == 2), LoadBehavior.RefreshCurrent, false);

            Assert.IsTrue(lo1.CanCancel, "Cancellation should be supported.");
            Assert.IsTrue(lo2.CanCancel, "Cancellation should be supported.");

            // cancel the first load call only
            lo1.Cancel();

            await lo1;
            await lo2;
            // Make sure we only loaded the product from our second load call.
            Assert.IsNull(lo1.Error);
            Assert.IsNull(lo2.Error);
            Assert.IsTrue(lo1.IsCanceled, "Operation should've been cancelled.");
            Assert.IsFalse(lo2.IsCanceled, "Operation should not have been cancelled.");
            Assert.AreEqual(1, catalog.Products.Count);
            Assert.AreEqual(2, catalog.Products.Single().ProductID);
        }

        [TestMethod]
        public async Task TestLoad_MultipleParameters()
        {
            TestDataContext ctxt = new TestDataContext(new Uri(TestURIs.RootURI, "TestDomainServices-TestCatalog1.svc"));

            var args =
                new Dictionary<string, object> {
                {"subCategoryID", 21},
                {"minListPrice", 50},
                {"color", "Yellow"}
                };

            EntityQuery<Product> query = ctxt.CreateQuery<Product>("GetProductsMultipleParams", args);
            LoadOperation lo = ctxt.Load(query, false);

            await lo;
            Assert.IsNull(lo.Error, lo.Error?.ToString());
            Assert.IsTrue(ctxt.Products.Count == 4);
        }

        /// <summary>
        /// Verify that a server entity type that doesn't use a contract namespace redirect
        /// can be deserialized correctly on the client if the server and client namespaces
        /// match and the default contract is used.
        /// </summary>
        [TestMethod]
        public async Task TestLoad_DefaultContractNamespace()
        {
            WebDomainClient<CityDomainContext.ICityDomainServiceContract> client = new WebDomainClient<CityDomainContext.ICityDomainServiceContract>(TestURIs.Cities)
            {
                EntityTypes = new Type[] { typeof(City) }
            };

            var results = await client.QueryAsync(new EntityQuery<City>(client, "GetCities", null, true, true), CancellationToken.None);
            Assert.IsTrue(results.TotalCount > 0);
        }

        /// <summary>
        /// Verify that an attempt to call a DomainOperationEntry with a [Permission(AuthenticationRequired=true)] attribute
        /// fails because we are not authenticated.
        /// </summary>
        [TestMethod]
        public async Task TestLoad_AuthenticationRequired()
        {
            WebDomainClient<CityDomainContext.ICityDomainServiceContract> client = new WebDomainClient<CityDomainContext.ICityDomainServiceContract>(TestURIs.Cities)
            {
                EntityTypes = new Type[] { typeof(Zip) }
            };

            var queryTask = client.QueryAsync(
                new EntityQuery<Zip>(client, "GetZipsIfAuthenticated", null, true, false),
                CancellationToken.None);

            DomainOperationException error = await ExceptionHelper.ExpectExceptionAsync<DomainOperationException>(() => queryTask);
            Assert.IsNotNull(error, "[Permission(AuthenticationRequired=true)] attribute should have raised a DomainOperationException");
            Assert.AreEqual(OperationErrorStatus.Unauthorized, error.Status);
            Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, "Access to operation '{0}' was denied.", "GetZipsIfAuthenticated"), error.Message);
        }

        /// <summary>
        /// Verify that an attempt to call a DomainOperationEntry with a [Permission(Role="manager")] attribute
        /// fails because we are not in the manager role.
        /// </summary>
        [TestMethod]
        public async Task TestLoad_RoleRequired()
        {
            WebDomainClient<CityDomainContext.ICityDomainServiceContract> client = new WebDomainClient<CityDomainContext.ICityDomainServiceContract>(TestURIs.Cities)
            {
                EntityTypes = new Type[] { typeof(Zip) }
            };
            var queryTask = client.QueryAsync(new EntityQuery<Zip>(client, "GetZipsIfInRole", null, true, false), CancellationToken.None);

            DomainOperationException error = await ExceptionHelper.ExpectExceptionAsync<DomainOperationException>(() => queryTask);
            Assert.IsNotNull(error, "[Permission(Role=\"manager\")] attribute should have raised a DomainOperationException");
            Assert.AreEqual(OperationErrorStatus.Unauthorized, error.Status);
            Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, "Access to operation '{0}' was denied.", "GetZipsIfInRole"), error.Message);
        }

        /// <summary>
        /// Verify that an attempt to use a query with a custom authorization attribute requiring a specific user
        /// </summary>
        [TestMethod]
        [Description("Accessing a query with a custom authorization attribute asserting a specific user is denied")]
        public async Task TestLoad_UserRequired()
        {
            WebDomainClient<CityDomainContext.ICityDomainServiceContract> client = new WebDomainClient<CityDomainContext.ICityDomainServiceContract>(TestURIs.Cities)
            {
                EntityTypes = new Type[] { typeof(Zip) }
            };

            var queryTask = client.QueryAsync(new EntityQuery<Zip>(client, "GetZipsIfUser", null, true, false), CancellationToken.None);
            DomainOperationException error = await ExceptionHelper.ExpectExceptionAsync<DomainOperationException>(() => queryTask);

            Assert.IsNotNull(error, "[RequiresUser(\"mathew\")] attribute should have raised a DomainOperationException");
            Assert.AreEqual(OperationErrorStatus.Unauthorized, error.Status);
            Assert.AreEqual(error.Message, "Only one user is authorized for this query, and it isn't you.");
        }

        [TestMethod]
        public async Task TestLoad_EnumerableQueryComposition()
        {
            TestDataContext ctxt = new TestDataContext(new Uri(TestURIs.RootURI, "TestDomainServices-TestCatalog1.svc"));

            EntityQuery<Product> query =
                from p in ctxt.CreateQuery<Product>("GetProducts_Enumerable_Composable", null, false, true)
                where p.Color == "Yellow"
                orderby p.ListPrice
                select p;
            LoadOperation lo = ctxt.Load(query, false);

            await lo;
            Assert.IsTrue(ctxt.Products.Count == 36);
            Assert.IsFalse(ctxt.Products.Any(p => p.Color != "Yellow"));
        }

        [TestMethod]
        public async Task TestLoad_DomainOperationEntryReturnsNull()
        {
            TestDataContext ctxt = new TestDataContext(new Uri(TestURIs.RootURI, "TestDomainServices-TestCatalog1.svc"));

            EntityQuery<Product> query = ctxt.CreateQuery<Product>("GetProducts_ReturnNull", null, false, true);
            LoadOperation lo = ctxt.Load(query, false);

            await lo;
            Assert.IsNull(lo.Error);
            Assert.IsTrue(ctxt.Products.Count == 0);
        }

        /// <summary>
        /// Missing parameters in a query string are treated as nulls by the server
        /// </summary>
        [TestMethod]
        public async Task TestDomainOperationEntry_MissingParameters()
        {
            TestDataContext ctxt = new TestDataContext(new Uri(TestURIs.RootURI, "TestDomainServices-TestCatalog1.svc"));

            Dictionary<string, object> paramValues = new Dictionary<string, object>();
            paramValues["subCategoryID"] = 21;
            paramValues["minListPrice"] = 50;

            var query = ctxt.CreateQuery<Product>("GetProductsMultipleParams", paramValues, false, true);
            LoadOperation lo = ctxt.Load(query, false);

            await lo;
            Assert.IsNull(lo.Error);
        }

        [TestMethod]
        public void TestDomainOperationEntry_IncorrectParameterType()
        {
            TestDataContext ctxt = new TestDataContext(new Uri(TestURIs.RootURI, "TestDomainServices-TestCatalog1.svc"));

            Dictionary<string, object> paramValues = new Dictionary<string, object>();
            paramValues["subCategoryID"] = "Foobar";
            paramValues["minListPrice"] = 50;
            paramValues["color"] = "Yellow";

            ExceptionHelper.ExpectArgumentException(delegate
            {
                var query = ctxt.CreateQuery<Product>("GetProductsMultipleParams", paramValues, false, true);
                ctxt.Load(query, false);
            }, "Object of type 'System.String' cannot be converted to type 'System.Int32'.");
        }

        /// <summary>
        /// Verify that exception messages for general exceptions are propagated back to the client
        /// </summary>
        [TestMethod]
        public async Task TestServerExceptions_GeneralException()
        {
            TestDataContext ctxt = new TestDataContext(new Uri(TestURIs.RootURI, "TestDomainServices-TestCatalog1.svc"));
            var query = ctxt.CreateQuery<Product>("ThrowGeneralException", null, false, true);
            await ValidateQueryException(ctxt, query, ValidateGeneralException);

            void ValidateGeneralException(Exception exception)
            {
                var ex = (DomainOperationException)exception;
                Assert.IsNotNull(ex);
                // domain operation entry invocation exceptions other than DomainService/DomainOperationEntryExceptions get turned into generic ServerError status
                Assert.AreEqual(OperationErrorStatus.ServerError, ex.Status);
                Assert.AreEqual(string.Format(Resource.DomainContext_LoadOperationFailed, "ThrowGeneralException", "Athewmay Arelschay"), ex.Message);
                Assert.IsTrue(ex.StackTrace.Contains("ThrowGeneralException"));
            }
        }

        /// <summary>
        /// Verify that a DomainServiceException thrown by the provider results in a DomainOperationException
        /// on the client
        /// </summary>
        [TestMethod]
        public async Task TestServerExceptions_DataOperationException()
        {
            TestDataContext ctxt = new TestDataContext(new Uri(TestURIs.RootURI, "TestDomainServices-TestCatalog1.svc"));
            var query = ctxt.CreateQuery<Product>("ThrowDataOperationException", null, false, true);
            await ValidateQueryException(ctxt, query, ex =>
            {
                DomainException dpe = (DomainException)ex;
                Assert.IsNotNull(dpe);
                Assert.AreEqual(777, dpe.ErrorCode);
                Assert.AreEqual("Athewmay Arelschay", dpe.Message);
            });
        }

        [TestMethod]
        public async Task TestServerExceptions_QueryOnNonExistentMethod()
        {
            TestDataContext ctxt = new TestDataContext(new Uri(TestURIs.RootURI, "TestDomainServices-TestCatalog1.svc"));
            var query = ctxt.CreateQuery<Product>("NonExistentMethod", null, false, true);
            await ValidateQueryException(ctxt, query, ex =>
            {
                // REVIEW: Assert the error message.
                Assert.IsNotNull(ex.InnerException as CommunicationException, "Expected CommunicationException");
                Assert.IsNotNull(ex.InnerException.InnerException as WebException, "Expected WebException");
            });
        }

        [TestMethod]
        public async Task TestDataContracts()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            LoadOperation lo = provider.Load(provider.GetEntitiesWithDataContractsQuery(), false);

            await lo;
            Assert.IsNull(lo.Error);
            Assert.AreEqual(2, provider.EntityWithDataContracts.Count);

            EntityWithDataContract[] entities = provider.EntityWithDataContracts.ToArray();
            Assert.AreEqual(1, entities[0].Id);
            Assert.AreEqual("First", entities[0].Data);
            Assert.AreEqual(2, entities[1].Id);
            Assert.AreEqual("Second", entities[1].Data);
        }

        [TestMethod]
        public async Task TestSpecialTypeNames()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            LoadOperation lo = provider.Load(provider.GetEntitiesWithSpecialTypeNameQuery(), false);

            await lo;
            Assert.IsNull(lo.Error);
            Assert.AreEqual(2, provider.EntityWithSpecialTypeNames.Count);

            EntityWithSpecialTypeName[] entities = provider.EntityWithSpecialTypeNames.ToArray();
            Assert.AreEqual(1, entities[0].Id);
            Assert.AreEqual("First", entities[0].Data);
            Assert.AreEqual(2, entities[1].Id);
            Assert.AreEqual("Second", entities[1].Data);
        }

        [TestMethod]
        public async Task TestProjectionWithBinaryPropertyType()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            LoadOperation lo = provider.Load(provider.GetDsQuery(), false);

            await lo;
            Assert.IsNull(lo.Error);
            Assert.AreEqual(1, provider.Ds.Count);

            D d = provider.Ds.First();
            Assert.AreEqual(1, d.ID);
            Assert.IsNotNull(d.BinaryData);
            Assert.AreEqual(2, d.BinaryData.Length);
            Assert.AreEqual(20, d.BinaryData[0]);
            Assert.AreEqual(30, d.BinaryData[1]);
        }

        [TestMethod]
        public async Task TestStringIndexerExpression()
        {
            TestProvider_Scenarios ctxt = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            var query = ctxt.GetCitiesQuery().Where(c => c.Name[0] == 'a');
            LoadOperation lo = ctxt.Load(query, false);

            await lo;
            Assert.IsNull(lo.Error);
            Assert.IsFalse(lo.HasError);
        }

        [TestMethod]
        public async Task TestStringReplaceDoesNotAllowPropertyForReplacementArgument()
        {
            TestProvider_Scenarios ctxt = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            var query = ctxt.GetCitiesQuery().Where(c => c.Name.Replace("a", c.Name) == "a");
            LoadOperation lo = ctxt.Load(query, false);

            await lo;
            Assert.IsNotNull(lo.Error);
            Assert.IsTrue(lo.HasError);
        }

        [TestMethod]
        public async Task TestStringReplaceDoesNotAllowMethodForReplacementArgument()
        {
            TestProvider_Scenarios ctxt = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            // Note: the method used for this test (String.Concat) needs to be an allowed method for the test
            // to execute the code we're aiming for.  Otherwise, the method call would get rejected before
            // we even validate the Replace arguments.
            var query = ctxt.GetCitiesQuery().Where(c => c.Name.Replace("a", String.Concat(c.Name, "a")) == "a");
            LoadOperation lo = ctxt.Load(query, false);

            await lo;
            Assert.IsNotNull(lo.Error);
            Assert.IsTrue(lo.HasError);
        }

        [TestMethod]
        public async Task TestStringReplaceDoesNotAllowStringLongerThan100CharactersForReplacementArgument()
        {
            TestProvider_Scenarios ctxt = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            var tooLong = new String('a', 101);
            var query = ctxt.GetCitiesQuery().Where(c => c.Name.Replace("a", tooLong) == "a");

            LoadOperation lo = ctxt.Load(query, false);

            await lo;
            Assert.IsNotNull(lo.Error);
            Assert.IsTrue(lo.HasError);
        }

        [TestMethod]
        public async Task TestStringReplaceAllowsStringUpTo100CharactersForReplacementArgument()
        {
            TestProvider_Scenarios ctxt = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            var justRight = new string('a', 100);
            var query = ctxt.GetCitiesQuery().Where(c => c.Name.Replace("a", justRight) == "a");

            LoadOperation lo = ctxt.Load(query, false);

            await lo;
            Assert.IsNull(lo.Error);
            Assert.IsFalse(lo.HasError);
        }

        private static Dictionary<TType, TType> CreateDictionary<TType>(TType seed)
        {
            return CreateDictionary(seed, seed);
        }

        private static Dictionary<TKey, TValue> CreateDictionary<TKey, TValue>(TKey seedKey, TValue seedValue)
        {
            var d = new Dictionary<TKey, TValue>();
            d.Add(seedKey, seedValue);
            return d;
        }

        private static bool CompareDictionaries<TKey, TValue>(IDictionary<TKey, TValue> a, IDictionary<TKey, TValue> b)
        {
            return (a.Select(ak => new KeyValuePair<string, string>(ak.Key.ToString(), ak.Value.ToString()))
                        .Intersect(b.Select(bk => new KeyValuePair<string, string>(bk.Key.ToString(), bk.Value.ToString())))
                            .Count() == a.Count);
        }

        private async Task ValidateQueryException(TestDataContext ctxt, EntityQuery<Product> query, Action<Exception> validateException)
        {
            LoadOperation lo = ctxt.Load(query, false);
            await lo;
            Assert.IsNotNull(lo.Error, "Load should have resulted in exception");
            validateException(lo.Error);

            var ex = await ExceptionHelper.ExpectExceptionAsync<Exception>(() => ctxt.LoadAsync(query), allowDerivedExceptions: true);
            validateException(ex);
        }
    }

    internal class ScenariosEntityContainer : EntityContainer
    {
        public ScenariosEntityContainer()
        {
            this.CreateEntitySet<A>(EntitySetOperations.All);
            this.CreateEntitySet<B>(EntitySetOperations.All);
            this.CreateEntitySet<C>(EntitySetOperations.All);
            this.CreateEntitySet<D>(EntitySetOperations.All);
        }
    }
}
