extern alias SSmDsClient;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using Cities;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataTests.Northwind.LTS;
using System.ComponentModel.DataAnnotations;
using OpenRiaServices.Silverlight.Testing;

namespace OpenRiaServices.DomainServices.Client.Test
{
    using Resource = SSmDsClient::OpenRiaServices.DomainServices.Client.Resource;
    using Resources = SSmDsClient::OpenRiaServices.DomainServices.Client.Resources;

    #region Test Classes
    public class TestOperation : OperationBase
    {
        private Action<TestOperation> _completeAction;

        public TestOperation(Action<TestOperation> completeAction, object userState)
            : base(userState)
        {
            this._completeAction = completeAction;
        }

        protected override bool SupportsCancellation
        {
            get
            {
                return true;
            }
        }

        protected override void CancelCore()
        {
            base.CancelCore();
        }

        public new void Complete(Exception error)
        {
            base.Complete(error);
        }

        public new void Complete(object result)
        {
            base.Complete(result);
        }

        public new void Cancel()
        {
            base.Cancel();
        }

        /// <summary>
        /// Invoke the completion callback.
        /// </summary>
        protected override void InvokeCompleteAction()
        {
            if (this._completeAction != null)
            {
                this._completeAction(this);
            }
        }
    }
    #endregion

    /// <summary>
    /// Targeted tests for OperationBase and derived classes
    /// </summary>
    [TestClass]
    public class OperationTests : UnitTestBase
    {
        public void Operation_MarkAsHandled()
        {
            TestDataContext ctxt = new TestDataContext(new Uri(TestURIs.RootURI, "TestDomainServices-TestCatalog1.svc"));

            var query = ctxt.CreateQuery<Product>("ThrowGeneralException", null, false, true);
            LoadOperation lo = new LoadOperation<Product>(query, LoadBehavior.KeepCurrent, null, null, null);

            EventHandler action = (o, e) =>
            {
                LoadOperation loadOperation = (LoadOperation)o;
                if (loadOperation.HasError)
                {
                    loadOperation.MarkErrorAsHandled();
                }
            };
            lo.Completed += action;

            DomainOperationException ex = new DomainOperationException("Operation Failed!", OperationErrorStatus.ServerError, 42, "StackTrace");
            lo.Complete(ex);

            // verify that calling MarkAsHandled again is a noop
            lo.MarkErrorAsHandled();
            lo.MarkErrorAsHandled();

            // verify that calling MarkAsHandled on an operation not in error
            // results in an exception
            lo = new LoadOperation<Product>(query, LoadBehavior.KeepCurrent, null, null, null);
            Assert.IsFalse(lo.HasError);
            Assert.IsTrue(lo.IsErrorHandled);
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
        public void UnhandledLoadOperationError()
        {
            TestDataContext ctxt = new TestDataContext(new Uri(TestURIs.RootURI, "TestDomainServices-TestCatalog1.svc"));

            var query = ctxt.CreateQuery<Product>("ThrowGeneralException", null, false, true);
            LoadOperation lo = new LoadOperation<Product>(query, LoadBehavior.KeepCurrent, null, null, null);

            DomainOperationException expectedException = null;
            DomainOperationException ex = new DomainOperationException("Operation Failed!", OperationErrorStatus.ServerError, 42, "StackTrace");
            try
            {
                lo.Complete(ex);
            }
            catch (DomainOperationException e)
            {
                expectedException = e;
            }

            // verify the exception properties
            Assert.IsNotNull(expectedException);
            Assert.AreEqual(string.Format(Resource.DomainContext_LoadOperationFailed, "ThrowGeneralException", ex.Message), expectedException.Message);
            Assert.AreEqual(ex.StackTrace, expectedException.StackTrace);
            Assert.AreEqual(ex.Status, expectedException.Status);
            Assert.AreEqual(ex.ErrorCode, expectedException.ErrorCode);

            Assert.AreEqual(false, lo.IsErrorHandled);

            // now test again with validation errors
            expectedException = null;
            ValidationResult[] validationErrors = new ValidationResult[] { new ValidationResult("Foo", new string[] { "Bar" }) };
            lo = new LoadOperation<Product>(query, LoadBehavior.KeepCurrent, null, null, null);

            try
            {
                lo.Complete(validationErrors);
            }
            catch (DomainOperationException e)
            {
                expectedException = e;
            }

            // verify the exception properties
            Assert.IsNotNull(expectedException);
            Assert.AreEqual(string.Format(Resource.DomainContext_LoadOperationFailed_Validation, "ThrowGeneralException"), expectedException.Message);
        }

        /// <summary>
        /// Verify that Load operations that don't specify a callback to handle
        /// errors and don't specify throwOnError = false result in an exception.
        /// </summary>
        [TestMethod]
        public void UnhandledInvokeOperationError()
        {
            CityDomainContext cities = new CityDomainContext(TestURIs.Cities);

            InvokeOperation invoke = new InvokeOperation("Echo", null, null, null, null);

            DomainOperationException expectedException = null;
            DomainOperationException ex = new DomainOperationException("Operation Failed!", OperationErrorStatus.ServerError, 42, "StackTrace");
            try
            {
                invoke.Complete(ex);
            }
            catch (DomainOperationException e)
            {
                expectedException = e;
            }

            // verify the exception properties
            Assert.IsNotNull(expectedException);
            Assert.AreEqual(string.Format(Resource.DomainContext_InvokeOperationFailed, "Echo", ex.Message), expectedException.Message);
            Assert.AreEqual(ex.StackTrace, expectedException.StackTrace);
            Assert.AreEqual(ex.Status, expectedException.Status);
            Assert.AreEqual(ex.ErrorCode, expectedException.ErrorCode);

            Assert.AreEqual(false, invoke.IsErrorHandled);

            // now test again with validation errors
            expectedException = null;
            ValidationResult[] validationErrors = new ValidationResult[] { new ValidationResult("Foo", new string[] { "Bar" }) };
            invoke = new InvokeOperation("Echo", null, null, null, null);

            try
            {
                invoke.Complete(validationErrors);
            }
            catch (DomainOperationException e)
            {
                expectedException = e;
            }

            // verify the exception properties
            Assert.IsNotNull(expectedException);
            Assert.AreEqual(string.Format(Resource.DomainContext_InvokeOperationFailed_Validation, "Echo"), expectedException.Message);
        }

        /// <summary>
        /// Verify that Load operations that don't specify a callback to handle
        /// errors and don't specify throwOnError = false result in an exception.
        /// </summary>
        [TestMethod]
        public void UnhandledSubmitOperationError()
        {
            CityDomainContext cities = new CityDomainContext(TestURIs.Cities);
            CityData data = new CityData();
            cities.Cities.LoadEntities(data.Cities.ToArray());

            City city = cities.Cities.First();
            city.ZoneID = 1;
            Assert.IsTrue(cities.EntityContainer.HasChanges);

            SubmitOperation submit = new SubmitOperation(cities.EntityContainer.GetChanges(), null, null, null);

            DomainOperationException expectedException = null;
            DomainOperationException ex = new DomainOperationException("Submit Failed!", OperationErrorStatus.ServerError, 42, "StackTrace");
            try
            {
                submit.Complete(ex);
            }
            catch (DomainOperationException e)
            {
                expectedException = e;
            }

            // verify the exception properties
            Assert.IsNotNull(expectedException);
            Assert.AreEqual(string.Format(Resource.DomainContext_SubmitOperationFailed, ex.Message), expectedException.Message);
            Assert.AreEqual(ex.StackTrace, expectedException.StackTrace);
            Assert.AreEqual(ex.Status, expectedException.Status);
            Assert.AreEqual(ex.ErrorCode, expectedException.ErrorCode);

            Assert.AreEqual(false, submit.IsErrorHandled);

            // now test again with conflicts
            expectedException = null;
            IEnumerable<ChangeSetEntry> entries = ChangeSetBuilder.Build(cities.EntityContainer.GetChanges());
            ChangeSetEntry entry = entries.First();
            entry.ValidationErrors = new ValidationResultInfo[] { new ValidationResultInfo("Foo", new string[] { "Bar" }) };

            submit = new SubmitOperation(cities.EntityContainer.GetChanges(), null, null, null);

            try
            {
                submit.Complete(OperationErrorStatus.Conflicts);
            }
            catch (DomainOperationException e)
            {
                expectedException = e;
            }

            // verify the exception properties
            Assert.IsNotNull(expectedException);
            Assert.AreEqual(string.Format(Resource.DomainContext_SubmitOperationFailed_Conflicts), expectedException.Message);

            // now test again with validation errors
            expectedException = null;
            entries = ChangeSetBuilder.Build(cities.EntityContainer.GetChanges());
            entry = entries.First();
            entry.ConflictMembers = new string[] { "ZoneID" };

            submit = new SubmitOperation(cities.EntityContainer.GetChanges(), null, null, null);

            try
            {
                submit.Complete(OperationErrorStatus.ValidationFailed);
            }
            catch (DomainOperationException e)
            {
                expectedException = e;
            }

            // verify the exception properties
            Assert.IsNotNull(expectedException);
            Assert.AreEqual(string.Format(Resource.DomainContext_SubmitOperationFailed_Validation, ex.Message), expectedException.Message);
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
        public void Exceptions()
        {
            Cities.CityDomainContext cities = new CityDomainContext(TestURIs.Cities);

            Action<LoadOperation<City>> loCallback = (op) =>
            {
                if (op.HasError)
                {
                    op.MarkErrorAsHandled();
                }

                throw new InvalidOperationException("Fnord!");
            };

            Action<SubmitOperation> soCallback = (op) =>
            {
                if (op.HasError)
                {
                    op.MarkErrorAsHandled();
                }

                throw new InvalidOperationException("Fnord!");
            };

            Action<InvokeOperation> ioCallback = (op) =>
            {
                if (op.HasError)
                {
                    op.MarkErrorAsHandled();
                }

                throw new InvalidOperationException("Fnord!");
            };

            LoadOperation lo = new LoadOperation<City>(cities.GetCitiesQuery(), LoadBehavior.MergeIntoCurrent, loCallback, null, loCallback);

            // verify completion callbacks that throw
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                try
                {
                    lo.Complete(DomainClientResult.CreateQueryResult(new Entity[0], new Entity[0], 0, new ValidationResult[0]));
                }
                catch (Exception ex)
                {
                    Assert.IsTrue(ex.StackTrace.Contains("at OpenRiaServices.DomainServices.Client.Test.OperationTests"), "Stacktrace not preserved.");

                    throw;
                }
            }, "Fnord!");

            // verify cancellation callbacks for all fx operation types
            lo = new LoadOperation<City>(cities.GetCitiesQuery(), LoadBehavior.MergeIntoCurrent, null, null, loCallback);
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                lo.Cancel();
            }, "Fnord!");

            SubmitOperation so = new SubmitOperation(cities.EntityContainer.GetChanges(), soCallback, null, soCallback);
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                so.Cancel();
            }, "Fnord!");

            InvokeOperation io = new InvokeOperation("Fnord", null, null, null, ioCallback);
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                io.Cancel();
            }, "Fnord!");
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
    }
}
