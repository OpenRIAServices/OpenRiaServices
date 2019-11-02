extern alias SSmDsClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using OpenRiaServices.DomainServices.Client;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using OpenRiaServices.DomainServices.Client.Test.Utilities;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.DomainServices.Client.Test
{
    using Resources = SSmDsClient::OpenRiaServices.DomainServices.Client.Resources;

    [TestClass]
    public class DomainContextWithMockDomainClientTests : UnitTestBase
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
        public void TestMockClient_CancellationSupport()
        {
            Cities.CityDomainContext dp = new Cities.CityDomainContext(new CitiesMockDomainClient());

            var query = dp.GetCitiesQuery();
            LoadOperation lo = dp.Load(query, false);
            Assert.IsFalse(lo.CanCancel, "Cancellation should not be supported.");
            Assert.IsFalse(dp.DomainClient.SupportsCancellation, "Cancellation should not be supported.");

            ExceptionHelper.ExpectException<NotSupportedException>(delegate
            {
                lo.Cancel();
            }, string.Format(CultureInfo.CurrentCulture, Resources.AsyncOperation_CancelNotSupported));
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
        /// Test that ValidationErrors for invoke are properly returned.
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void TestMockClient_Invoke_ValidationErrors()
        {
            var mockDomainClient = new CitiesMockDomainClient();
            ValidationResult[] validationErrors = new ValidationResult[] { new ValidationResult("Foo", new string[] { "Bar" }) };
            mockDomainClient.InvokeCompletedResult = Task.FromResult(new InvokeCompletedResult(null, validationErrors));
            Cities.CityDomainContext dp = new Cities.CityDomainContext(mockDomainClient);
            string myState = "Test User State";

            InvokeOperation invoke = dp.Echo("TestInvoke", TestHelperMethods.DefaultOperationAction, myState);

            EnqueueConditional(delegate
            {
                return invoke.IsComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.IsNotNull(invoke.Error);
                Assert.AreSame(myState, invoke.UserState);

                CollectionAssert.AreEqual(validationErrors, (ICollection)invoke.ValidationErrors);

                // verify the exception properties
                var ex = (DomainOperationException)invoke.Error;
                Assert.AreEqual(OperationErrorStatus.ValidationFailed, ex.Status);
                CollectionAssert.AreEqual(validationErrors, (ICollection)ex.ValidationErrors);
                Assert.AreEqual(string.Format(Resource.DomainContext_InvokeOperationFailed_Validation, "Echo"), ex.Message);
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Test that ValidationErrors for invoke are properly returned.
        /// </summary>
        [TestMethod]
        public async Task TestMockClient_InvokeAsync_ValidationErrors()
        {
            var mockDomainClient = new CitiesMockDomainClient();
            ValidationResult[] validationErrors = new ValidationResult[] { new ValidationResult("Foo", new string[] { "Bar" }) };
            mockDomainClient.InvokeCompletedResult = Task.FromResult(new InvokeCompletedResult(null, validationErrors));
            Cities.CityDomainContext dp = new Cities.CityDomainContext(mockDomainClient);

            var ex = await ExceptionHelper.ExpectExceptionAsync<DomainOperationException>(
                () => dp.EchoAsync("TestInvoke"),
                string.Format(Resource.DomainContext_InvokeOperationFailed_Validation, "Echo"));

            // verify the exception properties
            Assert.AreEqual(OperationErrorStatus.ValidationFailed, ex.Status);
            CollectionAssert.AreEqual(validationErrors, (ICollection)ex.ValidationErrors);
        }

        /// <summary>
        /// Test that ValidationErrors for invoke are properly returned.
        /// </summary>
        [TestMethod]
        public async Task TestMockClient_InvokeAsync_DomainOperationException()
        {
            var mockDomainClient = new CitiesMockDomainClient();
            DomainOperationException exception = new DomainOperationException("Operation Failed!", OperationErrorStatus.ServerError, 42, "StackTrace");
            mockDomainClient.InvokeCompletedResult = Task.FromException<InvokeCompletedResult>(exception);
            Cities.CityDomainContext dp = new Cities.CityDomainContext(mockDomainClient);

            var ex = await ExceptionHelper.ExpectExceptionAsync<DomainOperationException>(
                () => dp.EchoAsync("TestInvoke"),
                string.Format(Resource.DomainContext_InvokeOperationFailed, "Echo", exception.Message));

            // verify the exception properties
            Assert.AreEqual(null, ex.InnerException);
            Assert.AreEqual(ex.StackTrace, exception.StackTrace);
            Assert.AreEqual(ex.Status, exception.Status);
            Assert.AreEqual(ex.ErrorCode, exception.ErrorCode);
        }

        /// <summary>
        /// Test that ValidationErrors for invoke are properly returned.
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void TestMockClient_Query_ValidationErrors()
        {
            var mockDomainClient = new CitiesMockDomainClient();
            ValidationResult[] validationErrors = new ValidationResult[] { new ValidationResult("Foo", new string[] { "Bar" }) };
            mockDomainClient.QueryCompletedResult = Task.FromResult(new QueryCompletedResult(Enumerable.Empty<Entity>(), Enumerable.Empty<Entity>(), 0, validationErrors));
            Cities.CityDomainContext dp = new Cities.CityDomainContext(mockDomainClient);
            string myState = "Test User State";

            LoadOperation<Cities.City> loadOperation = dp.Load(dp.GetCitiesQuery(), LoadBehavior.RefreshCurrent, l => l.MarkErrorAsHandled(), myState); ;

            EnqueueConditional(delegate
            {
                return loadOperation.IsComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.IsNotNull(loadOperation.Error);
                Assert.AreSame(myState, loadOperation.UserState);

                // verify the exception properties

                Assert.AreEqual(OperationErrorStatus.ValidationFailed, loadOperation.Error);
                CollectionAssert.AreEqual(validationErrors, (ICollection)loadOperation.ValidationErrors);
                Assert.AreEqual(string.Format(Resource.DomainContext_LoadOperationFailed_Validation, "GetCities"), loadOperation.Error.Message);
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Test that ValidationErrors for invoke are properly returned.
        /// </summary>
        [TestMethod]
        public async Task TestMockClient_QueryAsync_ValidationErrors()
        {
            var mockDomainClient = new CitiesMockDomainClient();
            ValidationResult[] validationErrors = new ValidationResult[] { new ValidationResult("Foo", new string[] { "Bar" }) };
            mockDomainClient.QueryCompletedResult = Task.FromResult(new QueryCompletedResult(Enumerable.Empty<Entity>(), Enumerable.Empty<Entity>(), 0, validationErrors));
            Cities.CityDomainContext dp = new Cities.CityDomainContext(mockDomainClient);


            var ex = await ExceptionHelper.ExpectExceptionAsync<DomainOperationException>(
                () => dp.LoadAsync(dp.GetCitiesQuery()),
                string.Format(Resource.DomainContext_LoadOperationFailed_Validation, "GetCities"));

            // verify the exception properties
            Assert.AreEqual(OperationErrorStatus.ValidationFailed, ex.Status);
            CollectionAssert.AreEqual(validationErrors, (ICollection)ex.ValidationErrors);
        }

        /// <summary>
        /// Sample DomainClient implementation that operates on a set of in memory data for testing purposes.
        /// </summary>
        public class CitiesMockDomainClient : DomainClient
        {
            private readonly Cities.CityData citiesData = new Cities.CityData();

            /// <summary>
            /// What to return on next Invoke
            /// </summary>
            public Task<InvokeCompletedResult> InvokeCompletedResult { get; set; }
            public Task<QueryCompletedResult> QueryCompletedResult { get; set; }

            protected override Task<QueryCompletedResult> QueryAsyncCore(EntityQuery query, CancellationToken cancellationToken)
            {
                if (QueryCompletedResult != null)
                    return QueryCompletedResult;

                // load test data and get query result
                IEnumerable<Entity> entities = GetQueryResult(query.QueryName, query.Parameters);
                if (query.Query != null)
                {
                    entities = RebaseQuery(entities.AsQueryable(), query.Query).Cast<Entity>().ToList();
                }

                int entityCount = entities.Count();
                QueryCompletedResult results = new QueryCompletedResult(entities, Array.Empty<Entity>(), entityCount, Array.Empty<ValidationResult>());
                return TaskHelper.FromResult(results);
            }

            protected override Task<SubmitCompletedResult> SubmitAsyncCore(EntityChangeSet changeSet, CancellationToken cancellationToken)
            {
                IEnumerable<ChangeSetEntry> submitOperations = changeSet.GetChangeSetEntries();

                // perform mock submit operations
                SubmitCompletedResult submitResults = new SubmitCompletedResult(changeSet, submitOperations);
                return TaskHelper.FromResult(submitResults);
            }

            protected override Task<InvokeCompletedResult> InvokeAsyncCore(InvokeArgs invokeArgs, CancellationToken cancellationToken)
            {
                if (InvokeCompletedResult != null)
                    return InvokeCompletedResult;

                object returnValue = null;
                // do the invoke and get the return value
                if (invokeArgs.OperationName == "Echo")
                {
                    returnValue = "Echo: " + (string)invokeArgs.Parameters.Values.First();
                }

                return TaskHelper.FromResult(new InvokeCompletedResult(returnValue));
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
        }
    }
}
