using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using TestDomainServices;

namespace OpenRiaServices.DomainServices.Client.Test
{
    [TestClass]
    public class ServerSideAsyncTests : UnitTestBase
    {
        [TestMethod]
        [Asynchronous]
        [Description("Invoke server side method returning Task<string>")]
        public void Invoke_TaskAsyncReference()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);
            const string message = "client";
            const string expected = "Hello client";

            var invokeOp = ctx.GreetTaskAsync(message);

            this.EnqueueConditional(() => invokeOp.IsComplete);
            this.EnqueueCallback(() =>
            {
                Assert.IsNull(invokeOp.Error);
                Assert.AreEqual(expected, invokeOp.Value);
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Invoke server side method returning Task<int>")]
        public void Invoke_TaskAsyncValue()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);
            const int number = 42;

            var invokeOp = ctx.AddOneTaskAsync(number);

            this.EnqueueConditional(() => invokeOp.IsComplete);
            this.EnqueueCallback(() =>
            {
                Assert.IsNull(invokeOp.Error);
                Assert.AreEqual(number+1, invokeOp.Value);
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Invoke server side method returning Task<int?>")]
        public void Invoke_TaskAsyncNullable()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);
            const int number = 42;

            // Check non-null return value
            var invokeOp = ctx.AddNullableOneTaskAsync(number);
            this.EnqueueConditional(() => invokeOp.IsComplete);
            this.EnqueueCallback(() =>
            {
                Assert.IsNull(invokeOp.Error);
                Assert.AreEqual(number + 1, invokeOp.Value);
            });

            // Check null return value
            var invokeNullOp = ctx.AddNullableOneTaskAsync(null);
            this.EnqueueConditional(() => invokeNullOp.IsComplete);
            this.EnqueueCallback(() =>
            {
                Assert.IsNull(invokeNullOp.Error);
                Assert.AreEqual(null, invokeNullOp.Value);
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Invoke server side method returning void and ensure it waits")]
        public void Invoke_TaskAsyncVoid()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);
            TimeSpan delay = TimeSpan.FromMilliseconds(200);
            TimeSpan minExpectedDelay = TimeSpan.FromMilliseconds(100);
            
            // Check that we properly wait for "void-Tasks"
            DateTime start = DateTime.Now;
            var invokeOp = ctx.SleepAndSetLastDelay(delay);
            this.EnqueueConditional(() => invokeOp.IsComplete);
            this.EnqueueCallback(() =>
            {
                var actualDelay = (DateTime.Now - start);

                Assert.IsNull(invokeOp.Error);

                // Server store the last delay the last thing it does so we shold only get
                // it if we actually waited for the Task to complete
                var getDelayOp = ctx.GetLastDelay();
                this.EnqueueConditional(() => getDelayOp.IsCanceled);
                this.EnqueueCallback(() =>
                {
                    Assert.IsNull(invokeOp.Error);
                    Assert.AreEqual(delay, invokeOp.Value, "Server should have had time to set the actual delay");
                    Assert.IsTrue(actualDelay >= minExpectedDelay, "Delay was less than expected");
                });
                this.EnqueueTestComplete();
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Server side query returning Task<IEnumerable<T>>")]
        public void Query()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);
            var normalQuery = ctx.GetRangeQuery();
            var asyncQuery = ctx.GetQueryableRangeTaskAsyncQuery();

            var normalLoad = ctx.Load(normalQuery);
            var asyncLoad = ctx.LoadAsync(asyncQuery);
            this.EnqueueConditional(() => normalLoad.IsComplete);
            this.EnqueueConditional(() => asyncLoad.IsCompleted);
            this.EnqueueCallback(() =>
            {
                Assert.IsNull(normalLoad.Error, "Normal load failed");
                Assert.IsNull(asyncLoad.Exception, "Async load failed");
                CollectionAssert.AreEquivalent(normalLoad.Entities.ToArray(), asyncLoad.Result);

                Assert.IsTrue(normalLoad.Entities.Any(), "No entities loaded");
                Assert.AreEqual(normalLoad.TotalEntityCount, asyncLoad.Result.TotalEntityCount, "TotalEntityCount different");
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Server side query returning Task<T>")]
        public void Query_Single()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);
            const int expectedId = 4;
            var query = ctx.GetRangeByIdTaskAsyncQuery(expectedId);

            var load = ctx.LoadAsync(query);
            this.EnqueueConditional(() => load.IsCompleted);
            this.EnqueueCallback(() =>
            {
                Assert.AreEqual(null, load.Exception, "Load failed");

                Assert.AreEqual(1, load.Result.Count, "Only 1 entity should have been loaded");
                Assert.AreEqual(expectedId, load.Result.First().Id);
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Test that filtering is applied to server side query returning Task<IEnumerable<T>>")]
        public void Query_WithFilter()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);
            const int expectedId = 3;
            Expression<Func<RangeItem, bool>> filter = (RangeItem item) => item.Id == expectedId;
            var normalQuery = ctx.GetRangeQuery().Where(filter);
            var asyncQuery = ctx.GetQueryableRangeTaskAsyncQuery().Where(filter);

            normalQuery.IncludeTotalCount = true;
            asyncQuery.IncludeTotalCount = true;

            var normalLoadTask = ctx.LoadAsync(normalQuery);
            var asyncLoadTask = ctx.LoadAsync(asyncQuery);
            this.EnqueueConditional(() => normalLoadTask.IsCompleted);
            this.EnqueueConditional(() => asyncLoadTask.IsCompleted);
            this.EnqueueCallback(() =>
            {
                Assert.IsNull(normalLoadTask.Exception, "Normal load failed");
                Assert.IsNull(asyncLoadTask.Exception, "Async load failed");
                var normalLoad = normalLoadTask.Result;
                var asyncLoad = asyncLoadTask.Result;

                CollectionAssert.AreEquivalent(normalLoad, asyncLoad);

                Assert.AreEqual(1, normalLoad.Count, "Only 1 entity should have been loaded");
                Assert.AreEqual(expectedId, asyncLoad.Entities.First().Id);

                // Check Total entity count
                Assert.AreEqual(normalLoad.TotalEntityCount, asyncLoad.TotalEntityCount, "TotalEntityCount different");
                Assert.AreEqual(1, normalLoad.TotalEntityCount, "Wrong TotalEntityCount");
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Test that exceptions thrown directly from Task async methods are propagated")]
        public void Query_WithException_First()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);
            var exceptionFirstQuery = ctx.GetQueryableRangeWithExceptionFirstQuery();
            const int expectedErrorCode = 23;
            const string expectedMessage = "GetQueryableRangeWithExceptionFirst";

            var load = ctx.Load(exceptionFirstQuery);
            this.EnqueueConditional(() => load.IsComplete);
            this.EnqueueCallback(() =>
            {
                Assert.IsNotNull(load.Error, "Exception is null");
                var dex = ((DomainException)load.Error);

                Assert.AreEqual(expectedErrorCode, dex.ErrorCode, "Wrong error code");
                Assert.AreEqual(expectedMessage, dex.Message, "Wrong error message");
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Test that exceptions returned in Task from Task async methods are propagated")]
        public void Query_WithException_InTask()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);
            var exceptionFirstQuery = ctx.GetQueryableRangeWithExceptionTaskQuery();
            const int expectedErrorCode = 24;
            string expectedMessage = "GetQueryableRangeWithExceptionTask";
            
            var load = ctx.Load(exceptionFirstQuery, (res) =>
            {
                Assert.IsNotNull(res.Error, "Exception is null");
                res.MarkErrorAsHandled();
            }, null);
            this.EnqueueConditional(() => load.IsComplete);
            this.EnqueueCallback(() =>
            {
                Assert.IsNotNull(load.Error, "Exception is null");
                var dex = ((DomainException)load.Error);

                Assert.AreEqual(expectedMessage, dex.Message, "Wrong error message"); 
                Assert.AreEqual(expectedErrorCode, dex.ErrorCode, "Wrong error code");
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Test that exceptions thrown directly from Task async methods are propagated")]
        public void Query_Single_WithException_First()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);
            var exceptionFirstQuery = ctx.GetRangeByIdWithExceptionFirstQuery(2);
            const int expectedErrorCode = 23;
            const string expectedMessage = "GetRangeByIdWithExceptionFirst";

            var load = ctx.LoadAsync(exceptionFirstQuery);
            this.EnqueueConditional(() => load.IsCompleted);
            this.EnqueueCallback(() =>
            {
                Assert.IsNotNull(load.Exception, "Exception is null");
                var dex = ((DomainException)load.Exception.InnerException);

                Assert.AreEqual(expectedErrorCode, dex.ErrorCode, "Wrong error code");
                Assert.AreEqual(expectedMessage, dex.Message, "Wrong error message");
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Test that exceptions returned in Task from Task async methods are propagated")]
        public void Query_Single_WithException_InTask()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);
            var exceptionFirstQuery = ctx.GetRangeByIdWithExceptionTaskQuery(2);
            const int expectedErrorCode = 24;
            const string expectedMessage = "GetRangeByIdWithExceptionTask";

            var load = ctx.LoadAsync(exceptionFirstQuery);
            this.EnqueueConditional(() => load.IsCompleted);
            this.EnqueueCallback(() =>
            {
                Assert.IsNotNull(load.Exception, "Exception is null");
                var dex = ((DomainException)load.Exception.InnerException);

                Assert.AreEqual(expectedErrorCode, dex.ErrorCode, "Wrong error code");
                Assert.AreEqual(expectedMessage, dex.Message, "Wrong error message");
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Test that exceptions thrown directly from Task async methods are propagated")]
        public void Invoke_WithException_First()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);
            const int expectedErrorCode = 23;
            const string expectedMessage = "InvokeWithExceptionFirst";

            var invoke = ctx.InvokeWithExceptionFirstAsync();
            this.EnqueueConditional(() => invoke.IsCompleted);
            this.EnqueueCallback(() =>
            {
                Assert.IsNotNull(invoke.Exception, "Exception is null");
                var dex = ((DomainException)invoke.Exception.InnerException);

                Assert.AreEqual(expectedErrorCode, dex.ErrorCode, "Wrong error code");
                Assert.AreEqual(expectedMessage, dex.Message, "Wrong error message");
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Test that exceptions returned in Task from Task async methods are propagated")]
        public void Invoke_WithException_InTask()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);
            const int expectedErrorCode = 24;
            const string expectedMessage = "InvokeWithExceptionTask";

            var invoke = ctx.InvokeWithExceptionTaskAsync(delay:3);
            this.EnqueueConditional(() => invoke.IsCompleted);
            this.EnqueueCallback(() =>
            {
                Assert.IsNotNull(invoke.Exception, "Exception is null");
                var dex = ((DomainException)invoke.Exception.InnerException);

                Assert.AreEqual(expectedErrorCode, dex.ErrorCode, "Wrong error code");
                Assert.AreEqual(expectedMessage, dex.Message, "Wrong error message");
            });
            this.EnqueueTestComplete();
        }
    }
}
