extern alias SSmDsClient;
using System;
using System.Collections.Generic;
using System.Linq;
using Cities;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataTests.Northwind.LTS;
using System.ComponentModel.DataAnnotations;
using OpenRiaServices.Silverlight.Testing;
using System.Collections;
using System.Threading.Tasks;
using System.Threading;

namespace OpenRiaServices.Client.Test
{
    using Resource = SSmDsClient::OpenRiaServices.Client.Resource;
    using Resources = SSmDsClient::OpenRiaServices.Client.Resources;

    #region Test Classes
    public class TestOperation : OperationBase
    {
        private readonly Action<TestOperation> _completeAction;

        public TestOperation(Action<TestOperation> completeAction, object userState)
            : base(userState, new CancellationTokenSource())
        {
            this._completeAction = completeAction;
        }

        public new void SetError(Exception error)
        {
            base.SetError(error);
        }

        public new void Complete(object result)
        {
            base.Complete(result);
        }

        public new void Cancel()
        {
            base.Cancel();

            base.SetCancelled();
        }

        /// <summary>
        /// Invoke the completion callback.
        /// </summary>
        protected override void InvokeCompleteAction()
        {
            this._completeAction?.Invoke(this);
        }
    }


    class ExceptionRecordingSynchronizationContext : SynchronizationContext
    {
        public Exception LastException { get; set; }
        public bool HasException { get => LastException != null; }

        public override void Send(SendOrPostCallback d, object state)
        {
            try
            {
                d(state);
            }
            catch (Exception ex)
            {
                LastException = ex;
            }
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            this.Send(d, state);
        }
    }
    #endregion


    /// <summary>
    /// Targeted tests for OperationBase and derived classes
    /// </summary>
    [TestClass]
    public class OperationTests : UnitTestBase
    {
        [TestMethod]
        public void Operation_DirectException()
        {
            TestDataContext ctxt = new TestDataContext(new Uri(TestURIs.RootURI, "TestDomainServices-TestCatalog1.svc"));

            var query = ctxt.CreateQuery<Product>("ThrowGeneralException", null, false, true);

            Exception ex = new DomainOperationException("Operation Failed!", OperationErrorStatus.ServerError, 42, "StackTrace");

            LoadOperation lo = new LoadOperation<Product>(query, LoadBehavior.KeepCurrent, MarkExceptionAsHandled, null, Task.FromException<LoadResult<Product>>(ex), null);

            Assert.AreSame(ex, lo.Error);
        }

        [TestMethod]
        public async Task Operation_MarkAsHandled()
        {
            TestDataContext ctxt = new TestDataContext(new Uri(TestURIs.RootURI, "TestDomainServices-TestCatalog1.svc"));

            var query = ctxt.CreateQuery<Product>("ThrowGeneralException", null, false, true);

            var loadTask = new TaskCompletionSource<LoadResult<Product>>();
            LoadOperation lo = new LoadOperation<Product>(query, LoadBehavior.KeepCurrent, null, null, loadTask.Task, null);

            EventHandler action = (o, e) =>
            {
                LoadOperation loadOperation = (LoadOperation)o;
                if (loadOperation.HasError)
                {
                    loadOperation.MarkErrorAsHandled();
                }
            };
            lo.Completed += action;

            loadTask.SetException(new DomainOperationException("Operation Failed!", OperationErrorStatus.ServerError, 42, "StackTrace"));
            await lo;

            // verify that calling MarkAsHandled again is a noop
            lo.MarkErrorAsHandled();
            lo.MarkErrorAsHandled();

            // verify that calling MarkAsHandled on an operation not in error
            // results in an exception
            lo = new LoadOperation<Product>(query, LoadBehavior.KeepCurrent, null, null, false);
            Assert.IsFalse(lo.HasError);
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                lo.MarkErrorAsHandled();
            }, Resource.Operation_HasErrorMustBeTrue);
        }

        /// <summary>
        /// Verify that Load operations that don't specify a callback to handle
        /// errors and don't specify throwOnError = false result in an exception.
        /// </summary>
        [TestMethod]
        public async Task UnhandledLoadOperationError()
        {
            TestDataContext ctxt = new TestDataContext(new Uri(TestURIs.RootURI, "TestDomainServices-TestCatalog1.svc"));
            var syncCtx = new ExceptionRecordingSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(syncCtx);

            var query = ctxt.CreateQuery<Product>("ThrowGeneralException", null, false, true);
            var tcs = new TaskCompletionSource<LoadResult<Product>>();
            LoadOperation<Product> lo = new LoadOperation<Product>(query, LoadBehavior.KeepCurrent, null, null, tcs.Task, null);

            Exception ex = new DomainOperationException("Operation Failed!", OperationErrorStatus.ServerError, 42, "StackTrace");
            tcs.SetException(ex);
            await lo;

            // verify the exception properties
            Assert.AreSame(ex, syncCtx.LastException, "Exception should have been thrown on SynchronizationContext");
            Assert.AreEqual(false, lo.IsErrorHandled);

            // now test again with validation errors
            ValidationResult[] validationErrors = new ValidationResult[] { new ValidationResult("Foo", new string[] { "Bar" }) };

            tcs = new TaskCompletionSource<LoadResult<Product>>();
            lo = new LoadOperation<Product>(query, LoadBehavior.KeepCurrent, null, null, tcs.Task, null);
            ex = new DomainOperationException("expected", validationErrors);

            tcs.SetException(ex);
            await lo;

            // verify the exception properties
            Assert.AreSame(ex, syncCtx.LastException, "Exception should have been thrown on SynchronizationContext");
            Assert.AreEqual(false, lo.IsErrorHandled);
            CollectionAssert.AreEqual(validationErrors, (ICollection)lo.ValidationErrors);
        }

        /// <summary>
        /// Verify that Load operations that don't specify a callback to handle
        /// errors and don't specify throwOnError = false result in an exception.
        /// </summary>
        [TestMethod]
        public async Task UnhandledInvokeOperationError()
        {
            CityDomainContext cities = new CityDomainContext(TestURIs.Cities);

            var syncCtx = new ExceptionRecordingSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(syncCtx);

            TaskCompletionSource<InvokeResult<string>> tcs = new TaskCompletionSource<InvokeResult<string>>();
            InvokeOperation<string> invoke = new InvokeOperation<string>("Echo", null, null, null, tcs.Task, null);

            DomainOperationException ex = new DomainOperationException("Operation Failed!", OperationErrorStatus.ServerError, 42, "StackTrace");

            tcs.SetException(ex);
            await invoke;

            // verify the exception properties
            Assert.AreSame(ex, syncCtx.LastException);
            Assert.AreEqual(false, invoke.IsErrorHandled);

            // now test again with validation errors
            ValidationResult[] validationErrors = new ValidationResult[] { new ValidationResult("Foo", new string[] { "Bar" }) };
            tcs = new TaskCompletionSource<InvokeResult<string>>();
            invoke = new InvokeOperation<string>("Echo", null, null, null, tcs.Task, null);
            var validationException = new DomainOperationException("validation", validationErrors);

            tcs.SetException(validationException);
            await invoke;

            // verify the exception properties
            Assert.AreSame(validationException, syncCtx.LastException);
            CollectionAssert.AreEqual(validationErrors, (ICollection)invoke.ValidationErrors);
        }

        /// <summary>
        /// Verify that Load operations that don't specify a callback to handle
        /// errors and don't specify throwOnError = false result in an exception.
        /// </summary>
        [TestMethod]
        public async Task UnhandledSubmitOperationError()
        {
            CityDomainContext cities = new CityDomainContext(TestURIs.Cities);
            var syncCtx = new ExceptionRecordingSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(syncCtx);

            CityData data = new CityData();
            cities.Cities.LoadEntities(data.Cities.ToArray());

            City city = cities.Cities.First();
            city.ZoneID = 1;
            Assert.IsTrue(cities.EntityContainer.HasChanges);

            TaskCompletionSource<SubmitResult> submitResult = new TaskCompletionSource<SubmitResult>();
            SubmitOperation submit = new SubmitOperation(cities.EntityContainer.GetChanges(), null, null, submitResult.Task, null);
            DomainOperationException ex = new DomainOperationException("Submit Failed!", OperationErrorStatus.ServerError, 42, "StackTrace");

            submitResult.SetException(ex);
            await submit;

            // verify the exception properties
            Assert.AreSame(ex, syncCtx.LastException);
            Assert.AreEqual(false, submit.IsErrorHandled);
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verifies that cached LoadOperation Entity results are valid when accessed from the complete callback.")]
        public void Bug706034_AccessCachedEntityResultsInCallback()
        {
            Cities.CityDomainContext cities = new CityDomainContext(TestURIs.Cities);

            bool callbackCalled = false;
            Exception callbackException = null;
            Action<LoadOperation<City>> callback = (op) =>
            {
                if (op.HasError)
                {
                    op.MarkErrorAsHandled();
                }

                try
                {
                    Assert.AreEqual(11, op.AllEntities.Count());
                    Assert.AreEqual(11, op.Entities.Count());
                }
                catch (Exception e)
                {
                    callbackException = e;
                }
                finally
                {
                    callbackCalled = true;
                }
            };

            var q = cities.GetCitiesQuery();
            LoadOperation<City> lo = cities.Load(q, callback, null);

            // KEY to bug : access Entity collections to force them to cache
            IEnumerable<City> entities = lo.Entities;
            IEnumerable<Entity> allEntities = lo.AllEntities;

            EnqueueConditional(() => lo.IsComplete && callbackCalled);
            EnqueueCallback(delegate
            {
                Assert.IsNull(callbackException);
                Assert.IsNull(lo.Error);

                Assert.AreEqual(11, lo.AllEntities.Count());
                Assert.AreEqual(11, lo.Entities.Count());
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Description("Verifies that exceptions are thrown and callstacks are preserved.")]
        public async Task ExceptionsFromCallbacks()
        {
            Cities.CityDomainContext cities = new CityDomainContext(TestURIs.Cities);
            const string Message = "Fnord!";

            Action<LoadOperation<City>> loCallback = (op) =>
            {
                if (op.HasError)
                {
                    op.MarkErrorAsHandled();
                }

                throw new InvalidOperationException(Message);
            };

            Action<SubmitOperation> soCallback = (op) =>
            {
                if (op.HasError)
                {
                    op.MarkErrorAsHandled();
                }

                throw new InvalidOperationException(Message);
            };

            Action<InvokeOperation> ioCallback = (op) =>
            {
                if (op.HasError)
                {
                    op.MarkErrorAsHandled();
                }

                throw new InvalidOperationException(Message);
            };

            var query = cities.GetCitiesQuery();
            var loadBehaviour = LoadBehavior.MergeIntoCurrent;

            // verify completion callbacks that throw
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                try
                {
                    LoadResult<City> result = new(query, loadBehaviour, Array.Empty<City>(), Array.Empty<Entity>(), 0);
                    var load = new LoadOperation<City>(query, loadBehaviour, loCallback, null, Task.FromResult(result), null);
                }
                catch (Exception ex)
                {
                    Assert.IsTrue(ex.StackTrace.StartsWith("   at OpenRiaServices.Client.Test.OperationTests"), "Stacktrace not preserved.");

                    throw;
                }
            }, Message);

            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                try
                {
                    var submit = new SubmitOperation(cities.EntityContainer.GetChanges(), soCallback, null, Task.FromResult(new SubmitResult(null)), null);
                }
                catch (Exception ex)
                {
                    Assert.IsTrue(ex.StackTrace.StartsWith("   at OpenRiaServices.Client.Test.OperationTests"), "Stacktrace not preserved.");

                    throw;
                }
            }, Message);

            // Verify exceptions from callbacks are properly sent to dispatcher (when task has not completed synchronously)
            var syncCtx = new ExceptionRecordingSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(syncCtx);

            var loadTaskSource = new TaskCompletionSource<LoadResult<City>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var loadOperation = new LoadOperation<City>(query, loadBehaviour, loCallback, null, loadTaskSource.Task, null);
            await AssertExceptionIsCorrectlyRaisedOnSyncContext(loadOperation, loadTaskSource, new LoadResult<City>(query, loadBehaviour, Array.Empty<City>(), Array.Empty<Entity>(), 0));

            var invokeTaskSource = new TaskCompletionSource<InvokeResult<object>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var invokeOperation = new InvokeOperation<object>("Fnord", null, ioCallback, null, invokeTaskSource.Task, null);
            await AssertExceptionIsCorrectlyRaisedOnSyncContext(invokeOperation, invokeTaskSource, new InvokeResult<object>(null));

            var submitTaskSource = new TaskCompletionSource<SubmitResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var submitOperation = new SubmitOperation(cities.EntityContainer.GetChanges(), soCallback, null, submitTaskSource.Task, null);
            await AssertExceptionIsCorrectlyRaisedOnSyncContext(submitOperation, submitTaskSource, new SubmitResult(null));

            // verify cancellation callbacks for all fx operation types
            var noCompleteLoad = new TaskCompletionSource<LoadResult<City>>();
            var cts = new CancellationTokenSource();
            var lo = new LoadOperation<City>(cities.GetCitiesQuery(), LoadBehavior.MergeIntoCurrent, null, null, noCompleteLoad.Task, cts);
            lo.CancellationToken.Register(() => throw new InvalidOperationException(Message));
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                lo.Cancel();
            }, Message);

            cts = new CancellationTokenSource();
            var noCompleteSubmit = new TaskCompletionSource<SubmitResult>();
            SubmitOperation so = new SubmitOperation(cities.EntityContainer.GetChanges(), soCallback, null, noCompleteSubmit.Task, cts);
            so.CancellationToken.Register(() => throw new InvalidOperationException(Message));
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                so.Cancel();
            }, Message);

            cts = new CancellationTokenSource();
            var noCompleteInvoke = new TaskCompletionSource<InvokeResult<object>>();
            InvokeOperation io = new InvokeOperation<object>("Fnord", null, null, null, noCompleteInvoke.Task, cts);
            io.CancellationToken.Register(() => throw new InvalidOperationException(Message));
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                io.Cancel();
            }, Message);

            async Task AssertExceptionIsCorrectlyRaisedOnSyncContext<T>(OperationBase operation, TaskCompletionSource<T> tcs, T result)
            {
                // Clear exception so it 
                syncCtx.LastException = null;

                tcs.SetResult(result: result);
                await operation;

                Assert.IsInstanceOfType(syncCtx.LastException, typeof(InvalidOperationException));
                Assert.AreEqual(Message, syncCtx.LastException.Message);
                Assert.IsTrue(syncCtx.LastException.StackTrace.StartsWith("   at OpenRiaServices.Client.Test.OperationTests"), "Stacktrace not preserved.");
                Assert.IsNull(operation.Error, "Submit should not have error because of callback");
            }
        }

        /// <summary>
        /// Attempt to call cancel from the completion callback. Expect
        /// an exception since the operation is already complete.
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void Bug706066_CancelInCallback()
        {
            Cities.CityDomainContext cities = new CityDomainContext(TestURIs.Cities);

            bool callbackCalled = false;
            InvalidOperationException expectedException = null;
            Action<LoadOperation<City>> callback = (op) =>
            {
                if (op.HasError)
                {
                    op.MarkErrorAsHandled();
                }

                // verify that CanCancel is false even though we'll
                // ignore this and try below
                Assert.IsFalse(op.CanCancel);

                try
                {
                    op.Cancel();
                }
                catch (InvalidOperationException io)
                {
                    expectedException = io;
                }
                callbackCalled = true;
            };

            var q = cities.GetCitiesQuery().Take(1);
            LoadOperation lo = cities.Load(q, callback, null);

            EnqueueConditional(() => lo.IsComplete && callbackCalled);
            EnqueueCallback(delegate
            {
                Assert.IsFalse(lo.IsCanceled);
                Assert.AreEqual(Resources.AsyncOperation_AlreadyCompleted, expectedException.Message);
            });
            EnqueueTestComplete();
        }



        private void MarkExceptionAsHandled<TEntity>(LoadOperation<TEntity> loadOperation)
            where TEntity : Entity
        {
            if (loadOperation.HasError)
            {
                loadOperation.MarkErrorAsHandled();
            }
        }
    }
}
