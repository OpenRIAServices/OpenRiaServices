extern alias SSmDsClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cities;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.DomainServices.Client;
using OpenRiaServices.Silverlight.Testing;

namespace OpenRiaServices.DomainServices.Client.Test
{
    using Resources = SSmDsClient::OpenRiaServices.DomainServices.Client.Resources;

    [TestClass]
    public class DomainContextWithMockDomainClientTests : UnitTestBase
    {
        [TestMethod]
        public void CancellationSupport()
        {
            var domainClient = new CitiesMockDomainClient();
            domainClient.SetSupportsCancellation(false);
            Cities.CityDomainContext ctx = new Cities.CityDomainContext(domainClient);

            var query = ctx.GetCitiesQuery();
            LoadOperation lo = ctx.Load(query, false);
            Assert.IsFalse(lo.CanCancel, "Cancellation should not be supported.");
            Assert.IsFalse(ctx.DomainClient.SupportsCancellation, "Cancellation should not be supported.");

            ExceptionHelper.ExpectException<NotSupportedException>(delegate
            {
                lo.Cancel();
            }, string.Format(CultureInfo.CurrentCulture, Resources.AsyncOperation_CancelNotSupported));
        }

        /// <summary>
        /// Test that query processing works using a mock DomainClient.
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void Invoke()
        {
            Cities.CityDomainContext ctx = new Cities.CityDomainContext(new CitiesMockDomainClient());
            string myState = "Test User State";

            InvokeOperation invoke = ctx.Echo("TestInvoke", TestHelperMethods.DefaultOperationAction, myState);

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
        public void Invoke_ValidationErrors()
        {
            var mockDomainClient = new CitiesMockDomainClient();
            ValidationResult[] validationErrors = new ValidationResult[] { new ValidationResult("Foo", new string[] { "Bar" }) };
            mockDomainClient.InvokeCompletedResult = Task.FromResult(new InvokeCompletedResult(null, validationErrors));
            Cities.CityDomainContext ctx = new Cities.CityDomainContext(mockDomainClient);
            string myState = "Test User State";

            InvokeOperation invoke = ctx.Echo("TestInvoke", TestHelperMethods.DefaultOperationAction, myState);

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
        /// Test case where DomainClient do cancel on cancellation request
        /// </summary>
        [TestMethod]
        public async Task InvokeAsync_Cancel_DomainClientCancel()
        {
            var mockDomainClient = new CitiesMockDomainClient();
            Cities.CityDomainContext ctx = new Cities.CityDomainContext(mockDomainClient);

            // If cancellation results in request beeing cancelled the result should be cancelled
            var tcs = new TaskCompletionSource<InvokeCompletedResult>();
            using var cts = new CancellationTokenSource();
            mockDomainClient.InvokeCompletedResult = tcs.Task;
            var invokeTask = ctx.EchoAsync("TestInvoke", cts.Token);
            cts.Cancel();
            tcs.TrySetCanceled(cts.Token);

            await ExceptionHelper.ExpectExceptionAsync<TaskCanceledException>(() => invokeTask);
            Assert.IsTrue(invokeTask.IsCanceled);
        }

        /// <summary>
        /// Test case where DomainClient completes despite cancellation request
        /// </summary>
        [TestMethod]
        public async Task InvokeAsync_Cancel_DomainClientCompletes()
        {
            var mockDomainClient = new CitiesMockDomainClient();
            Cities.CityDomainContext ctx = new Cities.CityDomainContext(mockDomainClient);

            // If the web requires returns a value even if cancelled
            // It should still return a result
            var tcs = new TaskCompletionSource<InvokeCompletedResult>();
            using var cts = new CancellationTokenSource();
            mockDomainClient.InvokeCompletedResult = tcs.Task;
            var invokeTask = ctx.EchoAsync("TestInvoke", cts.Token);
            cts.Cancel();
            tcs.SetResult(new InvokeCompletedResult("Res"));

            var res = await invokeTask;
            Assert.AreEqual("Res", res.Value);
        }

        /// <summary>
        /// Test that ValidationErrors for invoke are properly returned.
        /// </summary>
        [TestMethod]
        public async Task InvokeAsync_DomainOperationException()
        {
            var mockDomainClient = new CitiesMockDomainClient();
            DomainOperationException exception = new DomainOperationException("Operation Failed!", OperationErrorStatus.ServerError, 42, "StackTrace");
            mockDomainClient.InvokeCompletedResult = Task.FromException<InvokeCompletedResult>(exception);
            Cities.CityDomainContext ctx = new Cities.CityDomainContext(mockDomainClient);

            var ex = await ExceptionHelper.ExpectExceptionAsync<DomainOperationException>(
                () => ctx.EchoAsync("TestInvoke"),
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
        public async Task InvokeAsync_ValidationErrors()
        {
            var mockDomainClient = new CitiesMockDomainClient();
            ValidationResult[] validationErrors = new ValidationResult[] { new ValidationResult("Foo", new string[] { "Bar" }) };
            mockDomainClient.InvokeCompletedResult = Task.FromResult(new InvokeCompletedResult(null, validationErrors));
            Cities.CityDomainContext ctx = new Cities.CityDomainContext(mockDomainClient);

            var ex = await ExceptionHelper.ExpectExceptionAsync<DomainOperationException>(
                () => ctx.EchoAsync("TestInvoke"),
                string.Format(Resource.DomainContext_InvokeOperationFailed_Validation, "Echo"));

            // verify the exception properties
            Assert.AreEqual(OperationErrorStatus.ValidationFailed, ex.Status);
            CollectionAssert.AreEqual(validationErrors, (ICollection)ex.ValidationErrors);
        }

        /// <summary>
        /// Test that query processing works using a mock DomainClient.
        /// </summary>
        [TestMethod]
        public async Task Load()
        {
            Cities.CityDomainContext ctx = new Cities.CityDomainContext(new CitiesMockDomainClient());
            string myState = "Test User State";

            var query = ctx.GetCitiesQuery().Where(p => p.StateName == "WA").OrderBy(p => p.CountyName).Take(4);
            LoadOperation lo = ctx.Load(query, TestHelperMethods.DefaultOperationAction, myState);

            await lo;

            Assert.IsNull(lo.Error);
            Assert.AreEqual(4, ctx.Cities.Count);
            Assert.IsTrue(ctx.Cities.All(p => p.StateName == "WA"));
            Assert.AreEqual(myState, lo.UserState);
        }

        /// <summary>
        /// Test case where DomainClient do cancel on cancellation request
        /// </summary>
        [TestMethod]
        public async Task Load_Cancel_DomainClientCancel()
        {
            var mockDomainClient = new CitiesMockDomainClient();
            Cities.CityDomainContext ctx = new Cities.CityDomainContext(mockDomainClient);

            // If cancellation results in request beeing cancelled the result should be cancelled
            var tcs = new TaskCompletionSource<QueryCompletedResult>();
            mockDomainClient.QueryCompletedResult = tcs.Task;
            var loadOp = ctx.Load(ctx.GetCitiesQuery());
            loadOp.Cancel();

            Assert.IsTrue(loadOp.IsCancellationRequested);
            Assert.IsTrue(ctx.IsLoading);
            Assert.IsFalse(loadOp.IsCanceled);
            Assert.IsFalse(loadOp.IsComplete);

            tcs.TrySetCanceled(loadOp.CancellationToken);
            await loadOp;

            Assert.IsFalse(ctx.IsLoading);
            Assert.IsTrue(loadOp.IsCanceled);
            Assert.IsTrue(loadOp.IsComplete);
            Assert.IsFalse(loadOp.HasError, "Cancelled operation should not have any error");
        }

        /// <summary>
        /// Test that ValidationErrors for invoke are properly returned.
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void Load_ValidationErrors()
        {
            var mockDomainClient = new CitiesMockDomainClient();
            ValidationResult[] validationErrors = new ValidationResult[] { new ValidationResult("Foo", new string[] { "Bar" }) };
            mockDomainClient.QueryCompletedResult = Task.FromResult(new QueryCompletedResult(Enumerable.Empty<Entity>(), Enumerable.Empty<Entity>(), 0, validationErrors));
            Cities.CityDomainContext ctx = new Cities.CityDomainContext(mockDomainClient);
            string myState = "Test User State";

            LoadOperation<Cities.City> loadOperation = ctx.Load(ctx.GetCitiesQuery(), LoadBehavior.RefreshCurrent, l => l.MarkErrorAsHandled(), myState); ;

            EnqueueConditional(delegate
            {
                return loadOperation.IsComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.AreSame(myState, loadOperation.UserState);

                CollectionAssert.AreEqual(validationErrors, (ICollection)loadOperation.ValidationErrors);

                // verify the exception properties
                var ex = loadOperation.Error as DomainOperationException;
                Assert.IsNotNull(ex, "expected exception of type DomainOperationException");
                Assert.AreEqual(OperationErrorStatus.ValidationFailed, ex.Status);
                Assert.AreEqual(string.Format(Resource.DomainContext_LoadOperationFailed_Validation, "GetCities"), ex.Message);
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Test case where DomainClient do cancel on cancellation request
        /// </summary>
        [TestMethod]
        public async Task LoadAsync_Cancel_DomainClientCancel()
        {
            var mockDomainClient = new CitiesMockDomainClient();
            Cities.CityDomainContext ctx = new Cities.CityDomainContext(mockDomainClient);

            // If cancellation results in request beeing cancelled the result should be cancelled
            var tcs = new TaskCompletionSource<QueryCompletedResult>();
            using var cts = new CancellationTokenSource();
            mockDomainClient.QueryCompletedResult = tcs.Task;
            var loadTask = ctx.LoadAsync(ctx.GetCitiesQuery(), cts.Token);

            Assert.IsTrue(ctx.IsLoading);
            cts.Cancel();
            tcs.TrySetCanceled(cts.Token);

            await ExceptionHelper.ExpectExceptionAsync<OperationCanceledException>(() => loadTask, allowDerivedExceptions: true);
            Assert.IsTrue(loadTask.IsCanceled);
            Assert.IsFalse(ctx.IsLoading);
        }

        /// <summary>
        /// Test case where DomainClient do cancel on cancellation request
        /// </summary>
        [TestMethod]
        public async Task LoadAsync_Cancel_DomainClientCompletes()
        {
            var mockDomainClient = new CitiesMockDomainClient();
            Cities.CityDomainContext ctx = new Cities.CityDomainContext(mockDomainClient);
            var city = new City() { Name = "NewCity", StateName = "NN", CountyName = "NewCounty" };

            // If cancellation results in request beeing cancelled the result should be cancelled
            var tcs = new TaskCompletionSource<QueryCompletedResult>();
            using var cts = new CancellationTokenSource();
            mockDomainClient.QueryCompletedResult = tcs.Task;
            var loadTask = ctx.LoadAsync(ctx.GetCitiesQuery(), cts.Token);
            cts.Cancel();
            tcs.SetResult(new QueryCompletedResult(new[] { city }, Array.Empty<Entity>(), -1, Array.Empty<ValidationResult>()));

            var result = await loadTask;
            Assert.AreEqual(1, result.Entities.Count);
            Assert.AreSame(city, result.Entities.First());
        }

        /// <summary>
        /// Test that ValidationErrors for invoke are properly returned.
        /// </summary>
        [TestMethod]
        public async Task LoadAsync_ValidationErrors()
        {
            var mockDomainClient = new CitiesMockDomainClient();
            ValidationResult[] validationErrors = new ValidationResult[] { new ValidationResult("Foo", new string[] { "Bar" }) };
            mockDomainClient.QueryCompletedResult = Task.FromResult(new QueryCompletedResult(Enumerable.Empty<Entity>(), Enumerable.Empty<Entity>(), 0, validationErrors));
            Cities.CityDomainContext ctx = new Cities.CityDomainContext(mockDomainClient);

            var ex = await ExceptionHelper.ExpectExceptionAsync<DomainOperationException>(
                () => ctx.LoadAsync(ctx.GetCitiesQuery()),
                string.Format(Resource.DomainContext_LoadOperationFailed_Validation, "GetCities"));

            // verify the exception properties
            Assert.AreEqual(OperationErrorStatus.ValidationFailed, ex.Status);
            CollectionAssert.AreEqual(validationErrors, (ICollection)ex.ValidationErrors);
        }

        [TestMethod]
        [Asynchronous]
        public void Submit()
        {
            Cities.CityDomainContext ctx = new Cities.CityDomainContext(new CitiesMockDomainClient());
            string myState = "Test User State";

            Cities.Zip newZip = new Cities.Zip
            {
                Code = 93551,
                FourDigit = 1234,
                CityName = "Issaquah",
                StateName = "Issaquah"
            };
            ctx.Zips.Add(newZip);
            SubmitOperation so = ctx.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);

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
                so = ctx.SubmitChanges(null, myState);
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

        [TestMethod]

        public async Task Submit_Cancel_DomainClientCancel()
        {
            var mockDomainClient = new CitiesMockDomainClient();
            Cities.CityDomainContext ctx = new Cities.CityDomainContext(mockDomainClient);

            // If cancellation results in request beeing cancelled the result should be cancelled
            var tcs = new TaskCompletionSource<SubmitCompletedResult>();
            mockDomainClient.SubmitCompletedResult = tcs.Task;

            ctx.Cities.Add(new City() { Name = "NewCity", StateName = "NN", CountyName = "NewCounty" });

            var submitOp = ctx.SubmitChanges();
            submitOp.Cancel();
            Assert.IsTrue(submitOp.IsCancellationRequested);
            Assert.IsFalse(submitOp.IsCanceled);
            Assert.IsTrue(ctx.IsSubmitting);
            Assert.IsTrue(ctx.Cities.First().IsSubmitting, "entity should be in submitting state");

            tcs.TrySetCanceled(submitOp.CancellationToken);
            await submitOp;

            // Return cancellation from domain client
            Assert.IsTrue(submitOp.IsCanceled);
            Assert.IsFalse(ctx.IsSubmitting);
            Assert.IsFalse(ctx.Cities.First().IsSubmitting, "entity should not be in submitting state");
        }


        [TestMethod]
        public async Task SubmitAsync_Cancel_DomainClientCancel()
        {
            var mockDomainClient = new CitiesMockDomainClient();
            Cities.CityDomainContext ctx = new Cities.CityDomainContext(mockDomainClient);

            // If cancellation results in request beeing cancelled the result should be cancelled
            var tcs = new TaskCompletionSource<SubmitCompletedResult>();
            using var cts = new CancellationTokenSource();
            mockDomainClient.SubmitCompletedResult = tcs.Task;

            ctx.Cities.Add(new City() { Name = "NewCity", StateName = "NN", CountyName = "NewCounty" });

            var submitTask = ctx.SubmitChangesAsync(cts.Token);
            cts.Cancel();
            Assert.IsTrue(ctx.IsSubmitting);
            Assert.IsTrue(ctx.Cities.First().IsSubmitting, "entity should be in submitting state");

            // Return cancellation from domain client
            tcs.TrySetCanceled(cts.Token);

            await ExceptionHelper.ExpectExceptionAsync<TaskCanceledException>(() => submitTask);
            Assert.IsTrue(submitTask.IsCanceled);
            Assert.IsFalse(ctx.IsSubmitting);
            Assert.IsFalse(ctx.Cities.First().IsSubmitting, "entity should not be in submitting state");
        }

        [TestMethod]
        public async Task SubmitAsync_Cancel_DomainClientCompletes()
        {
            var mockDomainClient = new CitiesMockDomainClient();
            Cities.CityDomainContext ctx = new Cities.CityDomainContext(mockDomainClient);

            // If cancellation results in request beeing cancelled the result should be cancelled
            using var cts = new CancellationTokenSource();
            mockDomainClient.SubmitCompletedCallback = async (changeSet, submitOperations) =>
            {
                // Wait for cancellation, and then return successfully without cancellation
                try
                {
                    await Task.Delay(-1, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Do nothing this is the expected outcome
                }
                // perform mock submit operations
                SubmitCompletedResult submitResults = new SubmitCompletedResult(changeSet, submitOperations);
                return submitResults;
            };

            ctx.Cities.Add(new City() { Name = "NewCity", StateName = "NN", CountyName = "NewCounty" });

            var submitTask = ctx.SubmitChangesAsync(cts.Token);
            Assert.IsTrue(ctx.IsSubmitting);
            Assert.IsTrue(ctx.Cities.First().IsSubmitting, "entity should be in submitting state");
            cts.Cancel();

            var result = await submitTask;
            Assert.IsFalse(ctx.IsSubmitting);
            Assert.IsFalse(ctx.Cities.First().IsSubmitting, "entity should not be in submitting state");
        }

        [TestMethod]
        public async Task SubmitAsync_Exceptions()
        {
            var mockDomainClient = new CitiesMockDomainClient();
            Cities.CityDomainContext ctx = new Cities.CityDomainContext(mockDomainClient);
            DomainOperationException ex = new DomainOperationException("Submit Failed!", OperationErrorStatus.ServerError, 42, "StackTrace");

            // If cancellation results in request beeing cancelled the result should be cancelled
            mockDomainClient.SubmitCompletedResult = Task.FromException<SubmitCompletedResult>(ex);

            ctx.Cities.Add(new City() { Name = "NewCity", StateName = "NN", CountyName = "NewCounty" });

            Assert.IsTrue(ctx.EntityContainer.HasChanges);
            var submitEx = await ExceptionHelper.ExpectExceptionAsync<SubmitOperationException>(() => ctx.SubmitChangesAsync());

            // verify the exception properties
            Assert.AreEqual(string.Format(Resource.DomainContext_SubmitOperationFailed, ex.Message), submitEx.Message);
            Assert.AreEqual(ex.StackTrace, submitEx.StackTrace);
            Assert.AreEqual(ex.Status, submitEx.Status);
            Assert.AreEqual(ex.ErrorCode, submitEx.ErrorCode);
            Assert.IsTrue(ctx.EntityContainer.HasChanges);

            // now test with validation exception
            var changeSet = ctx.EntityContainer.GetChanges();
            IEnumerable<ChangeSetEntry> entries = ChangeSetBuilder.Build(changeSet);
            ChangeSetEntry entry = entries.First();
            entry.ValidationErrors = new ValidationResultInfo[] { new ValidationResultInfo("Foo", new string[] { "Bar" }) };

            mockDomainClient.SubmitCompletedResult = Task.FromResult(new SubmitCompletedResult(changeSet, entries));
            submitEx = await ExceptionHelper.ExpectExceptionAsync<SubmitOperationException>(() => ctx.SubmitChangesAsync());

            // verify the exception properties
            Assert.AreEqual(Resource.DomainContext_SubmitOperationFailed_Validation, submitEx.Message);
            Assert.AreEqual(OperationErrorStatus.ValidationFailed, submitEx.Status);
            Assert.AreEqual(1, submitEx.EntitiesInError.Count);
            Assert.AreEqual(entry.ClientEntity, submitEx.EntitiesInError.First());

            // now test again with conflicts
            entries = ChangeSetBuilder.Build(changeSet);
            entry = entries.First();
            entry.ConflictMembers = new string[] { nameof(City.CountyName) };
            entry.StoreEntity = new City() { CountyName = "OtherCounty" };

            mockDomainClient.SubmitCompletedResult = Task.FromResult(new SubmitCompletedResult(changeSet, entries));
            submitEx = await ExceptionHelper.ExpectExceptionAsync<SubmitOperationException>(() => ctx.SubmitChangesAsync());

            // verify the exception properties
            Assert.AreEqual(Resource.DomainContext_SubmitOperationFailed_Conflicts, submitEx.Message);
            Assert.AreEqual(OperationErrorStatus.Conflicts, submitEx.Status);
            Assert.AreEqual(1, submitEx.EntitiesInError.Count);
            Assert.AreEqual(entry.ClientEntity, submitEx.EntitiesInError.First());
        }
    }
}
