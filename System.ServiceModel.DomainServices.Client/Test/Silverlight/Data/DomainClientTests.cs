extern alias SSmDsClient;

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.ServiceModel.DomainServices.Client;
using DataTests.AdventureWorks.LTS;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.ServiceModel.DomainServices.Client.Test
{
    using DomainClientAsyncResult = SSmDsClient::System.ServiceModel.DomainServices.Client.DomainClientAsyncResult;
    using Resource = SSmDsClient::System.ServiceModel.DomainServices.Client.Resource;
    using Resources = SSmDsClient::System.ServiceModel.DomainServices.Client.Resources;

    [TestClass]
    public class DomainClientTests : UnitTestBase
    {
        /// <summary>
        /// Test that query processing works using a mock DomainClient.
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void TestMockClient_Query()
        {
            Cities.CityDomainContext dp = new Cities.CityDomainContext(new CitiesMockDomainClient());
            string myState = "Test User State";

            var query = dp.GetCitiesQuery().Where(p => p.StateName == "WA").OrderBy(p => p.CountyName).Take(4);
            LoadOperation lo = dp.Load(query, null, myState);
            lo.Completed += (o, e) =>
            {
                LoadOperation loadOp = (LoadOperation)o;
                if (loadOp.HasError)
                {
                    loadOp.MarkErrorAsHandled();
                }
            };

            EnqueueConditional(delegate
            {
                return lo.IsComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.IsNull(lo.Error);
                Assert.AreEqual(4, dp.Cities.Count);
                Assert.IsTrue(dp.Cities.All(p => p.StateName == "WA"));
                Assert.AreEqual(myState, lo.UserState);
            });

            EnqueueTestComplete();
        }
        [TestMethod]
        [Asynchronous]
        public void TestMockClient_CancellationSupport()
        {
            Cities.CityDomainContext dp = new Cities.CityDomainContext(new CitiesMockDomainClient());

            bool domainClientQueryCompleted = false;
            dp.DomainClient.BeginQuery(
                new EntityQuery<Cities.City>(dp.DomainClient, "GetCities", null, true, false),
                delegate(IAsyncResult getCitiesAsyncResult)
                {
                    ExceptionHelper.ExpectException<NotSupportedException>(delegate
                    {
                        dp.DomainClient.CancelQuery(getCitiesAsyncResult);
                    }, string.Format(CultureInfo.CurrentCulture, Resource.DomainClient_CancellationNotSupported, typeof(CitiesMockDomainClient).FullName));

                    domainClientQueryCompleted = true;
                }, null);

            EnqueueConditional(() => domainClientQueryCompleted);
            EnqueueCallback(delegate
            {
                var query = dp.GetCitiesQuery();
                LoadOperation lo = dp.Load(query, false);
                Assert.IsFalse(lo.CanCancel, "Cancellation should not be supported.");
                Assert.IsFalse(dp.DomainClient.SupportsCancellation, "Cancellation should not be supported.");

                ExceptionHelper.ExpectException<NotSupportedException>(delegate
                {
                    lo.Cancel();
                }, string.Format(CultureInfo.CurrentCulture, Resources.AsyncOperation_CancelNotSupported));
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestMockClient_Submit()
        {
            Cities.CityDomainContext dp = new Cities.CityDomainContext(new CitiesMockDomainClient());
            string myState = "Test User State";

            Cities.Zip newZip = new Cities.Zip
            {
                Code = 93551,
                FourDigit = 1234,
                CityName = "Issaquah",
                StateName = "Issaquah"
            };
            dp.Zips.Add(newZip);
            SubmitOperation so = dp.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);

            EnqueueConditional(delegate
            {
                return so.IsComplete;
            });
            EnqueueCallback(delegate
            {
                // verify that validation logic is run
                Assert.IsNotNull(so.Error);
                Assert.AreSame(newZip, so.EntitiesInError.Single());

                // fix by setting the Name
                newZip.StateName = "WA";
                so = dp.SubmitChanges(null, myState);
            });
            EnqueueConditional(delegate
            {
                return so.IsComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.IsNull(so.Error);
                Assert.AreEqual(myState, so.UserState);
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Test that query processing works using a mock DomainClient.
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void TestMockClient_Invoke()
        {
            Cities.CityDomainContext dp = new Cities.CityDomainContext(new CitiesMockDomainClient());
            string myState = "Test User State";

            InvokeOperation invoke = dp.Echo("TestInvoke", TestHelperMethods.DefaultOperationAction, myState);

            EnqueueConditional(delegate
            {
                return invoke.IsComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.IsNull(invoke.Error);
                Assert.AreSame(myState, invoke.UserState);
                Assert.AreEqual("Echo: TestInvoke", invoke.Value);
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Test that we propagate null values correctly when we invoke DomainClient directly.
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void TestMockClientReturnsNull()
        {
            NullReturningMockDomainClient dc = new NullReturningMockDomainClient();

            AsyncCallback ignored = delegate { };

            InvokeArgs invokeArgs = new InvokeArgs("M", typeof(void), null, true /*hasSideEffects*/);
            DomainClientAsyncResult result = (DomainClientAsyncResult)dc.BeginInvoke(invokeArgs, ignored, null);

            EnqueueConditional(() => result != null);

            EnqueueCallback(delegate
            {
                Assert.IsNull(result.InnerAsyncResult);

                ExceptionHelper.ExpectArgumentNullException(
                () => dc.EndInvoke(result.InnerAsyncResult),
                "asyncResult");

                result = null;
                result = (DomainClientAsyncResult)dc.BeginQuery(new EntityQuery<Entity>(dc, "GetIgnored", null, true, false), ignored, null);
            });

            EnqueueConditional(() => result != null);

            EnqueueCallback(delegate
            {
                Assert.IsNull(result.InnerAsyncResult);

                ExceptionHelper.ExpectArgumentNullException(
                    () => dc.EndQuery(result.InnerAsyncResult),
                    "asyncResult");

                List<Entity> list = new List<Entity>();
                list.Add(new Product());
                ReadOnlyCollection<Entity> simpleCollection = new ReadOnlyCollection<Entity>(list);
                ReadOnlyCollection<Entity> emptyCollection = new ReadOnlyCollection<Entity>(new List<Entity>());
                EntityChangeSet emptyChangeSet = new EntityChangeSet(simpleCollection, emptyCollection, emptyCollection);
                result = null;
                result = (DomainClientAsyncResult)dc.BeginSubmit(emptyChangeSet, ignored, null);
            });

            EnqueueConditional(() => result != null);

            EnqueueCallback(delegate
            {
                Assert.IsNull(result.InnerAsyncResult);

                ExceptionHelper.ExpectArgumentNullException(
                    () => dc.EndSubmit(result.InnerAsyncResult),
                    "asyncResult");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestQuery()
        {
            QueryCompletedResult result = null;
            object userState = this;

            WebDomainClient<TestDomainServices.LTS.Catalog.ICatalogContract> dc = new WebDomainClient<TestDomainServices.LTS.Catalog.ICatalogContract>(TestURIs.LTS_Catalog)
            {
                EntityTypes = new Type[] { typeof(Product) }
            };

            var query = from p in new Product[0].AsQueryable()
                        where p.Weight < 10.5M
                        orderby p.Weight
                        select p;

            var entityQuery = new EntityQuery<Product>(new EntityQuery<Product>(dc, "GetProducts", null, true, false), query);
            entityQuery.IncludeTotalCount = true;

            dc.BeginQuery(
                entityQuery,
                delegate(IAsyncResult asyncResult)
                {
                    result = dc.EndQuery(asyncResult);
                },
                userState
            );

            EnqueueConditional(delegate
            {
                return result != null;
            });
            EnqueueCallback(delegate
            {
                Assert.AreEqual(79, result.Entities.Concat(result.IncludedEntities).Count());
                Assert.AreEqual(result.Entities.Count(), result.TotalCount);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestQueryEvents()
        {
            QueryCompletedResult result = null;
            object userState = this;

            WebDomainClient<TestDomainServices.LTS.Catalog.ICatalogContract> dc = new WebDomainClient<TestDomainServices.LTS.Catalog.ICatalogContract>(TestURIs.LTS_Catalog)
            {
                EntityTypes = new Type[] { typeof(Product) }
            };
            dc.BeginQuery(
                new EntityQuery<Product>(dc, "GetProducts", null, true, false),
                delegate(IAsyncResult asyncResult)
                {
                    result = dc.EndQuery(asyncResult);
                },
                userState
            );

            EnqueueConditional(delegate
            {
                return result != null;
            });
            EnqueueCallback(delegate
            {
                Assert.AreEqual(504, result.Entities.Concat(result.IncludedEntities).Count());
                Assert.AreEqual(result.Entities.Count(), result.TotalCount);
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// It is possible to specify a query expression who's type doesn't match the
        /// type returned by the server. In cases where the query is valid (as below where
        /// it's only a Take - no members are queried) this mismatch cannot be caught, since
        /// BeginQuery isn't strongly typed. This test exists to capture this scenario/issue.
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void TestMethodQueryMismatch()
        {
            WebDomainClient<TestDomainServices.LTS.Catalog.ICatalogContract> dc = new WebDomainClient<TestDomainServices.LTS.Catalog.ICatalogContract>(TestURIs.EF_Catalog)
            {
                EntityTypes = new Type[] { typeof(Product), typeof(PurchaseOrder), typeof(PurchaseOrderDetail) }
            };
            QueryCompletedResult queryResults = null;

            var query = new EntityQuery<PurchaseOrder>(new EntityQuery<Product>(dc, "GetProducts", null, true, false), new PurchaseOrder[0].AsQueryable().Take(2));
            query.IncludeTotalCount = true;

            dc.BeginQuery(
                query,
                delegate(IAsyncResult asyncResult)
                {
                    queryResults = dc.EndQuery(asyncResult);
                },
                null
            );

            EnqueueConditional(() => queryResults != null);

            EnqueueCallback(delegate
            {
                Assert.AreEqual(2, queryResults.Entities.Concat(queryResults.IncludedEntities).Count());
                Assert.AreEqual(504, queryResults.TotalCount);
            });

            EnqueueTestComplete();
        }
    }

    /// <summary>
    /// Sample DomainClient implementation that operates on a set of in memory data for testing purposes.
    /// </summary>
    public class CitiesMockDomainClient : DomainClient
    {
        private Cities.CityData citiesData = new Cities.CityData();

        protected override IAsyncResult BeginQueryCore(EntityQuery query, AsyncCallback callback, object userState)
        {
            // load test data and get query result
            IEnumerable<Entity> entities = GetQueryResult(query.QueryName, query.Parameters);
            if (query.Query != null)
            {
                entities = RebaseQuery(entities.AsQueryable(), query.Query).Cast<Entity>();
            }

            MockAsyncResult ar = new MockAsyncResult(entities, userState, null);
            callback.Invoke(ar);

            return ar;
        }

        protected override QueryCompletedResult EndQueryCore(IAsyncResult asyncResult)
        {
            MockAsyncResult ar = (MockAsyncResult)asyncResult;
            int entityCount = ar.Entities.Count();
            QueryCompletedResult results = new QueryCompletedResult(ar.Entities, new Entity[0], entityCount, new ValidationResult[0]);
            return results;
        }

        protected override IAsyncResult BeginSubmitCore(EntityChangeSet changeSet, AsyncCallback callback, object userState)
        {
            IEnumerable<ChangeSetEntry> submitOperations = changeSet.GetChangeSetEntries();
            MockAsyncResult ar = new MockAsyncResult(null, userState, new object[] { changeSet, submitOperations, userState });

            // perform mock submit operations

            callback.Invoke(ar);

            return ar;
        }

        protected override SubmitCompletedResult EndSubmitCore(IAsyncResult asyncResult)
        {
            MockAsyncResult ar = (MockAsyncResult)asyncResult;
            object[] stateParts = (object[])ar.InnerState;
            EntityChangeSet changeSet = (EntityChangeSet)stateParts[0];
            IEnumerable<ChangeSetEntry> submitOperations = (IEnumerable<ChangeSetEntry>)stateParts[1];

            SubmitCompletedResult submitResults = new SubmitCompletedResult(changeSet, submitOperations);

            return submitResults;
        }

        protected override IAsyncResult BeginInvokeCore(InvokeArgs invokeArgs, AsyncCallback callback, object userState)
        {
            MockAsyncResult ar = new MockAsyncResult(null, userState, new object[] { invokeArgs.OperationName, invokeArgs.ReturnType, invokeArgs.Parameters, userState });

            // do the invoke and get the return value
            if (invokeArgs.OperationName == "Echo")
            {
                ar.ReturnValue = "Echo: " + (string)invokeArgs.Parameters.Values.First();
            }

            callback.Invoke(ar);

            return ar;
        }

        protected override InvokeCompletedResult EndInvokeCore(IAsyncResult asyncResult)
        {
            MockAsyncResult ar = (MockAsyncResult)asyncResult;

            InvokeCompletedResult invokeResults = new InvokeCompletedResult(ar.ReturnValue);

            return invokeResults;
        }

        /// <summary>
        /// Rebases the specified query with the specified queryable root
        /// </summary>
        /// <param name="root">The new root</param>
        /// <param name="query">The query to insert the root into</param>
        /// <returns>The rebased query</returns>
        private static IQueryable RebaseQuery(IQueryable root, IQueryable query)
        {
            if (root.ElementType != query.ElementType)
            {
                // types not equal, so we need to inject a cast
                System.Linq.Expressions.Expression castExpr = System.Linq.Expressions.Expression.Call(
                    typeof(Queryable), "Cast",
                    new Type[] { query.ElementType },
                    root.Expression);
                root = root.Provider.CreateQuery(castExpr);
            }

            return RebaseInternal(root, query.Expression);
        }

        private static IQueryable RebaseInternal(IQueryable root, System.Linq.Expressions.Expression queryExpression)
        {
            MethodCallExpression mce = queryExpression as MethodCallExpression;
            if (mce != null && (mce.Arguments[0].NodeType == ExpressionType.Constant) &&
               (((ConstantExpression)mce.Arguments[0]).Value != null) &&
                (((ConstantExpression)mce.Arguments[0]).Value is IQueryable))
            {
                // this MethodCall is directly on the query root - replace
                // the root
                mce = ResourceQueryOperatorCall(System.Linq.Expressions.Expression.Constant(root), mce);
                return root.Provider.CreateQuery(mce);
            }

            // make the recursive call to find and replace the root
            root = RebaseInternal(root, mce.Arguments[0]);
            mce = ResourceQueryOperatorCall(root.Expression, mce);

            return root.Provider.CreateQuery(mce);
        }

        /// <summary>
        /// Given a MethodCallExpression, copy the expression, replacing the source with the source provided
        /// </summary>
        /// <param name="source"></param>
        /// <param name="mce"></param>
        /// <returns></returns>
        private static System.Linq.Expressions.MethodCallExpression ResourceQueryOperatorCall(System.Linq.Expressions.Expression source, MethodCallExpression mce)
        {
            List<System.Linq.Expressions.Expression> exprs = new List<System.Linq.Expressions.Expression>();
            exprs.Add(source);
            exprs.AddRange(mce.Arguments.Skip(1));
            return System.Linq.Expressions.Expression.Call(mce.Method, exprs.ToArray());
        }

        private IEnumerable<Entity> GetQueryResult(string operation, IDictionary<string, object> parameters)
        {
            string dataMember = operation.Replace("Get", "");
            PropertyInfo pi = typeof(Cities.CityData).GetProperty(dataMember);
            return ((IEnumerable)pi.GetValue(citiesData, null)).Cast<Entity>();
        }

        private class MockAsyncResult : IAsyncResult
        {
            public MockAsyncResult(IEnumerable results, object userState, object innerState)
            {
                if (results != null)
                {
                    Entities = results.Cast<Entity>();
                }
                AsyncState = userState;
                InnerState = innerState;
            }

            public object InnerState
            {
                get;
                set;
            }

            public IEnumerable<Entity> Entities
            {
                get;
                set;
            }

            public object ReturnValue
            {
                get;
                set;
            }

            #region IAsyncResult Members

            public object AsyncState
            {
                get;
                set;
            }

            public System.Threading.WaitHandle AsyncWaitHandle
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool CompletedSynchronously
            {
                get
                {
                    return true;
                }
            }

            public bool IsCompleted
            {
                get
                {
                    return true;
                }
            }

            #endregion
        }
    }

    /// <summary>
    /// Sample DomainClient implementation that returns null from all methods for testing null propagation.
    /// </summary>
    public class NullReturningMockDomainClient : DomainClient
    {
        public NullReturningMockDomainClient()
        {
        }

        public void M()
        {
        }

        protected override IAsyncResult BeginQueryCore(EntityQuery query, AsyncCallback callback, object userState)
        {
            return null;
        }

        protected override QueryCompletedResult EndQueryCore(IAsyncResult asyncResult)
        {
            return null;
        }

        protected override IAsyncResult BeginSubmitCore(EntityChangeSet changeSet, AsyncCallback callback, object userState)
        {
            return null;
        }

        protected override SubmitCompletedResult EndSubmitCore(IAsyncResult asyncResult)
        {
            return null;
        }

        protected override IAsyncResult BeginInvokeCore(InvokeArgs invokeArgs, AsyncCallback callback, object userState)
        {
            return null;
        }

        protected override InvokeCompletedResult EndInvokeCore(IAsyncResult asyncResult)
        {
            return null;
        }
    }
}
