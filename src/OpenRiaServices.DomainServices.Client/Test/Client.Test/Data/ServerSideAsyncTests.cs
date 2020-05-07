using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
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

            var invokeOp = ctx.Greet(message);

            this.EnqueueCompletion(() => invokeOp);
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

            var invokeOp = ctx.AddOne(number);

            this.EnqueueCompletion(() => invokeOp);
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
            var invokeOp = ctx.AddNullableOne(number);
            this.EnqueueCompletion(() => invokeOp);
            this.EnqueueCallback(() =>
            {
                Assert.IsNull(invokeOp.Error);
                Assert.AreEqual(number + 1, invokeOp.Value);
            });

            // Check null return value
            var invokeNullOp = ctx.AddNullableOne(null);
            this.EnqueueCompletion(() => invokeNullOp);
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
            this.EnqueueCompletion(() => invokeOp);
            this.EnqueueCallback(() =>
            {
                var actualDelay = (DateTime.Now - start);

                Assert.IsNull(invokeOp.Error);
                Assert.IsTrue(actualDelay >= minExpectedDelay, "Delay was less than expected");

                // Server store the last delay the last thing it does so we shold only get
                // it if we actually waited for the Task to complete
                var getDelayOp = ctx.GetLastDelay();
                this.EnqueueCompletion(() => getDelayOp);
                this.EnqueueCallback(() =>
                {
                    Assert.IsNull(getDelayOp.Error);
                    Assert.AreEqual(delay, getDelayOp.Value, "Server should have had time to set the actual delay");
                });
                this.EnqueueTestComplete();
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Server side query returning Task<IEnumerable<T>>")]
        public void Query_TaskAsync()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);
            var normalQuery = ctx.GetRangeQuery();
            var asyncQuery = ctx.GetQueryableRangeQuery();

            var normalLoad = ctx.Load(normalQuery);
            var asyncLoad = ctx.LoadAsync(asyncQuery);
            this.EnqueueCompletion(() => normalLoad);
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
        public void Query_TaskAsync_Single()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);
            const int expectedId = 4;
            var query = ctx.GetRangeByIdQuery(expectedId);

            var load = ctx.LoadAsync(query);
            this.EnqueueConditional(() => load.IsCompleted);
            this.EnqueueCallback(() =>
            {
                Assert.IsNull(load.Exception, "Load failed");

                Assert.AreEqual(1, load.Result.Count, "Only 1 entity should have been loaded");
                Assert.AreEqual(expectedId, load.Result.First().Id);
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Test that filtering is applied to server side query returning Task<IEnumerable<T>>")]
        public void Query_TaskAsync_WithFilter()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);
            const int expectedId = 3;
            Expression<Func<RangeItem, bool>> filter = (RangeItem item) => item.Id == expectedId;
            var normalQuery = ctx.GetRangeQuery().Where(filter);
            var asyncQuery = ctx.GetQueryableRangeQuery().Where(filter);

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
        public void Query_TaskAsync_WithException_First()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);
            var exceptionFirstQuery = ctx.GetQueryableRangeWithExceptionFirstQuery();
            const int expectedErrorCode = 23;
            const string expectedMessage = "GetQueryableRangeWithExceptionFirst";

            var load = ctx.Load(exceptionFirstQuery, throwOnError:false);
            this.EnqueueCompletion(() => load);
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
        public void Query_TaskAsync_WithException_InTask()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);
            var exceptionFirstQuery = ctx.GetQueryableRangeWithExceptionTaskQuery();
            const int expectedErrorCode = 24;
            string expectedMessage = "GetQueryableRangeWithExceptionTask";
            
            var load = ctx.Load(exceptionFirstQuery, throwOnError: false);
            this.EnqueueCompletion(() => load);
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
        public void Query_TaskAsync_Single_WithException_First()
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
        public void Query_TaskAsync_Single_WithException_InTask()
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
        public void Invoke_TaskAsyncWithException_First()
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
        public void Invoke_TaskAsyncWithException_InTask()
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

        [TestMethod]
        [Description("Tests that an async Task Insert operation is executed and waited for")]
        public async Task Insert_TaskAsync()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);

            var rangeItem = new RangeItem() { Text = "insert" };
            ctx.RangeItems.Add(rangeItem);
            await ctx.SubmitChangesAsync();

            Assert.AreEqual(42, rangeItem.Id);
        }

        [TestMethod]
        [Description("Test that exceptions thrown in Task from Task async Insert methods are propagated")]
        public async Task Insert_TaskAsyncWithException_InTask()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);

            var rangeItem = new RangeItem2() { Text = "insert failed" };
            ctx.RangeItem2s.Add(rangeItem);

            await AssertDomainExceptionIsThrown(ctx.SubmitChangesAsync, 25);
        }

        [TestMethod]
        [Description("Tests that an async Task Update operation is executed and waited for")]
        public async Task Update_TaskAsync()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);

            var rangeItem = new RangeItem() { Text = "test" };
            ctx.RangeItems.Attach(rangeItem);

            rangeItem.Text = "updated";

            await ctx.SubmitChangesAsync();
        }

        [TestMethod]
        [Description("Test that exceptions thrown in Task from Task async Update methods are propagated")]
        public async Task Update_TaskAsyncWithException_InTask()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);

            var rangeItem = new RangeItem2() { Text = "test" };
            ctx.RangeItem2s.Attach(rangeItem);

            rangeItem.Text = "updated";

            await AssertDomainExceptionIsThrown(ctx.SubmitChangesAsync, 26);
        }

        [TestMethod]
        [Description("Tests that an async Task EntityAction operation is executed")]
        public async Task EntityAction_TaskAsync()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);

            var rangeItem = new RangeItem() { Text = "test" };
            ctx.RangeItems.Attach(rangeItem);

            rangeItem.CustomUpdateRange();

            await ctx.SubmitChangesAsync();
        }

        [TestMethod]
        [Description("Test that exceptions thrown in Task from Task async EntityAction methods are propagated")]
        public async Task EntityAction_TaskAsyncWithException_InTask()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);

            var rangeItem = new RangeItem2() { Text = "test" };
            ctx.RangeItem2s.Attach(rangeItem);

            rangeItem.CustomUpdateRangeAsyncThrowsException();

            await AssertDomainExceptionIsThrown(ctx.SubmitChangesAsync, 28);
        }

        [TestMethod]
        [Description("Tests that an async Task Delete operation is executed and waited for")]
        public async Task Delete_TaskAsync()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);

            var rangeItem = new RangeItem() { Id = 42, Text = "test" };
            ctx.RangeItems.Attach(rangeItem);

            ctx.RangeItems.Remove(rangeItem);

            await ctx.SubmitChangesAsync();
        }

        [TestMethod]
        [Description("Test that exceptions thrown in Task from Task async Delete methods are propagated")]
        public async Task Delete_TaskAsyncWithException_InTask()
        {
            var ctx = new ServerSideAsyncDomainContext(TestURIs.ServerSideAsync);

            var rangeItem = new RangeItem2() { Id = 42, Text = "test" };
            ctx.RangeItem2s.Attach(rangeItem);

            ctx.RangeItem2s.Remove(rangeItem);

            await AssertDomainExceptionIsThrown(ctx.SubmitChangesAsync, 27);
        }

        /// <summary>
        /// Asserts that executing <paramref name="action"/> throws a <see cref="DomainException"/> 
        /// with the specified <paramref name="expectedErrorCode"/>.
        /// </summary>
        /// <param name="action">Action to execute</param>
        /// <param name="expectedErrorCode">The expected errorCode</param>
        /// <returns></returns>
        private static async Task AssertDomainExceptionIsThrown(Func<Task> action, int expectedErrorCode)
        {
            try
            {
                await action();

                Assert.Fail("No exception thrown");
            }
            catch (DomainException de)
            {
                Assert.AreEqual(expectedErrorCode, de.ErrorCode, "Wrong error code returned or expected operation was not executed");
            }
            catch
            {
                Assert.Fail("Wrong exception thrown");
            }
        }
    }
}
