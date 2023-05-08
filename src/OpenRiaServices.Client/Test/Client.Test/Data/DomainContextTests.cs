using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cities;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using TestDomainServices.LTS;

namespace OpenRiaServices.Client.Test
{
    [TestClass]
    public class DomainContext_E2E_Tests : UnitTestBase
    {
        #region Test Setup

        public Exception Error
        {
            get;
            set;
        }

        public LoadOperation LoadOperation
        {
            get;
            set;
        }


        public SubmitOperation SubmitOperation
        {
            get;
            set;
        }

        public DomainContext DomainContext
        {
            get
            {
                return this.CityDomainContext as DomainContext;
            }
        }

        public CityDomainContext CityDomainContext
        {
            get;
            set;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            this.Error = null;
            this.LoadOperation = null;
            this.SubmitOperation = null;
            this.CityDomainContext = new CityDomainContext(TestURIs.Cities);
        }

        #endregion // Test Setup

        #region Test Methods

        /// <summary>
        /// Previously in (SL only) CanCancel would return true and Cancel would be invoked,
        /// then the completion callback posted to the UI thread by DomainContext would attempt
        /// to complete the operation again.
        /// </summary>
        [TestMethod]
        [WorkItem(755212)]
        [Ignore] // Need to fix Race condition before enabling again
        public async Task CancelSubmit_EmptyChangeset()
        {
            Northwind nw = new Northwind(TestURIs.LTS_Northwind);
            SubmitOperation so = nw.SubmitChanges();

            // WARNING: Race condition
            // A SyncronizationContext needs to be provided 
            // So that the the operation cannot be completed between the CanCancel
            // and the IsCompleted check in Cancel
            so.Cancel();

            await so;
            Assert.IsNull(so.Error);
        }

        [TestMethod]
        public async Task SubmitAsync_Cancel_EmptyChangeset()
        {
            Northwind nw = new Northwind(TestURIs.LTS_Northwind);
            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();

            // This should not throw any error, OperationCancelledException might be acceptable
            // in the  future
            await nw.SubmitChangesAsync(cts.Token);
        }

        [TestMethod]
        [Asynchronous]
        public void Load_DefaultBehavior()
        {
            this.EnqueueCallback(() =>
            {
                this.BeginLoadCityData();
            });
            this.EnqueueCompletion(() => LoadOperation);
            this.EnqueueCallback(() =>
            {
                Assert.IsNull(LoadOperation.Error);
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public async Task Load_CancellationBehavior()
        {
            this.BeginLoadCityDataAndCancel();

            await LoadOperation;

            this.AssertLoadCancelled();
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void SubmitChanges_DefaultBehavior()
        {
            this.EnqueueCallback(() =>
            {
                this.BeginSubmitCityData();
            });
            this.EnqueueCompletion(() => SubmitOperation);
            this.EnqueueCallback(() =>
            {
                Assert.IsNull(SubmitOperation.Error);
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void DomainContextCallsVirtualMethods()
        {
            this.EnqueueCallback(() =>
            {
                var query = this.CityDomainContext.GetCitiesQuery();
                this.LoadOperation = this.CityDomainContext.Load(query);
                Assert.IsTrue(this.CityDomainContext.LoadCalled);
                Assert.IsTrue(this.CityDomainContext.LoadAsyncCalled);
            });

            this.EnqueueCallback(() =>
            {
                this.CityDomainContext.SubmitChanges();
                Assert.IsTrue(this.CityDomainContext.SubmitChangesCalled);
                Assert.IsTrue(this.CityDomainContext.SubmitChangesAsyncCalled);
            });

            this.EnqueueCallback(() =>
            {
                this.CityDomainContext.Echo("hi", null, null);
                Assert.IsTrue(this.CityDomainContext.InvokeOperationGenericCalled);
                Assert.IsTrue(this.CityDomainContext.InvokeOperationAsyncGenericCalled);
            });

            this.EnqueueCallback(() =>
            {
                this.CityDomainContext.ResetData(null, null);
                Assert.IsTrue(this.CityDomainContext.InvokeOperationGenericCalled);
                Assert.IsTrue(this.CityDomainContext.InvokeOperationAsyncGenericCalled);
            });

            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void DomainContextCallsVirtualMethods_Async()
        {
            this.EnqueueCallback(() =>
            {
                var query = this.CityDomainContext.GetCitiesQuery();
                this.CityDomainContext.LoadAsync(query);
                Assert.IsTrue(this.CityDomainContext.LoadAsyncCalled);
                Assert.IsFalse(this.CityDomainContext.LoadCalled);
            });

            this.EnqueueCallback(() =>
            {
                this.CityDomainContext.SubmitChangesAsync();
                Assert.IsTrue(this.CityDomainContext.SubmitChangesAsyncCalled);
                Assert.IsFalse(this.CityDomainContext.SubmitChangesCalled);
            });

            this.EnqueueCallback(() =>
            {
                this.CityDomainContext.EchoAsync("hi");
                Assert.IsFalse(this.CityDomainContext.InvokeOperationGenericCalled);
                Assert.IsTrue(this.CityDomainContext.InvokeOperationAsyncGenericCalled);
            });

            this.EnqueueCallback(() =>
            {
                this.CityDomainContext.ResetDataAsync();
                Assert.IsFalse(this.CityDomainContext.InvokeOperationGenericCalled);
                Assert.IsTrue(this.CityDomainContext.InvokeOperationAsyncGenericCalled);
            });


            this.EnqueueTestComplete();
        }

        [TestMethod]
        [WorkItem(854187)]
        [Asynchronous]
        [Description("SubmitChanges should clear errors and perform validation for the changed entities, raising the INotifyDataErrorInfo events")]
        public void SubmitChanges_ValidatesEntities()
        {
            CityDomainContext domainContext = new CityDomainContext();
            City newCity = new City();
            domainContext.Cities.Add(newCity);

            System.ComponentModel.INotifyDataErrorInfo notifier = (System.ComponentModel.INotifyDataErrorInfo)newCity;
            List<string> actualErrors = new List<string>();
            notifier.ErrorsChanged += (s, e) => actualErrors.Add(e.PropertyName);

            this.EnqueueCallback(() =>
            {
                // Required fields will be reported as in error
                // The submission will fail because of the validation error.  Mark it as handled.
                this.SubmitOperation = domainContext.SubmitChanges(s => s.MarkErrorAsHandled(), null);

                Assert.IsTrue(newCity.ValidationErrors.Any(r => r.MemberNames.Contains("Name")), "Name is required");
                Assert.IsTrue(actualErrors.OrderBy(s => s).SequenceEqual(new string[] { "Name" }), "We should have received errors for Name");
                actualErrors.Clear();
            });

            this.EnqueueCompletion(() => SubmitOperation);

            this.EnqueueCallback(() =>
            {
                newCity.Name = "West Chester";
                newCity.StateName = "OH";
                newCity.CountyName = "Butler"; // No validation - but needed for a complete key

                Assert.IsTrue(actualErrors.OrderBy(s => s).SequenceEqual(new string[] { "Name" }), "We should have received a notification for Name when setting it to a valid value");
                actualErrors.Clear();

                newCity.ZoneID = -50; // Invalid
                Assert.IsTrue(actualErrors.SequenceEqual(new string[] { "ZoneID" }), "We should have received an error for ZoneID when setting it to an invalid value");
                actualErrors.Clear();

                // Property errors will be reported
                // The submission will fail because of the validation error.  Mark it as handled.
                this.SubmitOperation = domainContext.SubmitChanges(s => s.MarkErrorAsHandled(), null);

                Assert.IsTrue(newCity.ValidationErrors.Any(r => r.MemberNames.Contains("ZoneID")), "ZoneID is out of range");
                Assert.IsTrue(actualErrors.SequenceEqual(new string[] { "ZoneID" }), "We should have received an error for ZoneID only - errors were cleared and replaced");
                actualErrors.Clear();
            });

            this.EnqueueCompletion(() => SubmitOperation);

            this.EnqueueCallback(() =>
            {
                newCity.StateName = "ASDF"; // Too long
                Assert.IsTrue(actualErrors.SequenceEqual(new string[] { "StateName" }), "We should have received an error for StateName when setting it to an invalid value");
                actualErrors.Clear();

                // Property errors will be reported
                this.SubmitOperation = domainContext.SubmitChanges(s => s.MarkErrorAsHandled(), null);

                Assert.IsTrue(newCity.ValidationErrors.Any(r => r.MemberNames.Contains("ZoneID")), "ZoneID is invalid");
                Assert.IsTrue(newCity.ValidationErrors.Any(r => r.MemberNames.Contains("StateName")), "StateName is too long");
                Assert.IsTrue(actualErrors.OrderBy(s => s).SequenceEqual(new string[] { "StateName", "ZoneID" }), "We should have received errors for StateName and ZoneID");
                actualErrors.Clear();
            });

            this.EnqueueCompletion(() => SubmitOperation);

            this.EnqueueCallback(() =>
            {
                newCity.ZoneID = 0;
                newCity.StateName = "OH";
                Assert.IsTrue(actualErrors.OrderBy(s => s).SequenceEqual(new string[] { "StateName", "ZoneID" }), "We should have received notifications for StateName and ZoneID when setting them to valid values");
                actualErrors.Clear();

                // Force entity-level validation failure
                newCity.MakeEntityValidationFail = true;

                // The entity-level error will be reported
                this.SubmitOperation = domainContext.SubmitChanges(s => s.MarkErrorAsHandled(), null);

                Assert.IsTrue(newCity.ValidationErrors.Any(r => r.MemberNames == null || !r.MemberNames.Any(m => !string.IsNullOrEmpty(m))), "We should only have entity-level errors (no members or null or empty members only)");
                Assert.IsTrue(actualErrors.SequenceEqual(new string[] { null }), "We should have received an error for the entity-level error");
                actualErrors.Clear();
            });

            this.EnqueueCompletion(() => SubmitOperation);

            this.EnqueueCallback(() =>
            {
                // Allow all validation to pass
                newCity.MakeEntityValidationFail = false;

                // The submission will succeed - there are no further validation errors.
                // But this will still result in notifications because the validation errors are cleared
                // To help verify this, we will add a validation result to the collection to ensure it's cleared
                newCity.ValidationErrors.Add(new ValidationResult("Added Error", new string[] { "MakeBelieveProperty" }));
                actualErrors.Clear();

                this.SubmitOperation = domainContext.SubmitChanges();

                Assert.IsTrue(!newCity.ValidationErrors.Any(), "There should not be any errors after valid submission");
                Assert.IsTrue(actualErrors.OrderBy(s => s).SequenceEqual(new string[] { null, "MakeBelieveProperty" }.OrderBy(s => s)), "We should have received notifications that the entity-level and property-level erros were cleared");
            });

            this.EnqueueCompletion(() => SubmitOperation);

            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Description("SubmitChanges should validate IValidatableObject entities")]
        public async Task SubmitChanges_ValidatesIValidatableEntities()
        {
            CityDomainContext domainContext = new CityDomainContext();
            City newCity = new City() { Name = "Test", CountyName = "Test", StateName = "TE", MakeIValidatableObjectFail = true };
            domainContext.Cities.Add(newCity);

            // Invalid IValidatableObject's will be reported as in error
            var ex = await ExceptionHelper.ExpectExceptionAsync<SubmitOperationException>(domainContext.SubmitChangesAsync);

            Assert.AreEqual(OperationErrorStatus.ValidationFailed, ex.Status);
            Assert.IsTrue(newCity.HasValidationErrors);
            Assert.AreEqual(1, newCity.ValidationErrors.Count, "Expected one (and only one) validation error");

            ValidationResult validationResult = newCity.ValidationErrors.Single();
            Assert.AreEqual("IValidatableObject", validationResult.ErrorMessage);
            string memberName = validationResult.MemberNames.Single();
            Assert.AreEqual("MakeIValidatableObjectFail", memberName);

            // Verify that IValidatableObject validation error clearing is treated the same way as other errors.
            newCity.MakeIValidatableObjectFail = false;
            await domainContext.SubmitChangesAsync();

            Assert.IsFalse(newCity.HasValidationErrors, "Entity should not have any validation errors");
            Assert.IsFalse(newCity.ValidationErrors.Any());
        }

        private void BeginLoadCityData(Action<LoadOperation<City>> callback, object userState)
        {
            Assert.IsTrue(this.LoadOperation == null);

            var query = this.CityDomainContext.GetCitiesQuery();
            this.LoadOperation = this.CityDomainContext.Load(query, LoadBehavior.RefreshCurrent, callback, userState);
        }

        private void BeginLoadCityData()
        {
            this.BeginLoadCityData(null, null);
        }

        private void BeginLoadCityDataAndCancel()
        {
            this.BeginLoadCityData(null, null);
            this.LoadOperation.Cancel();
        }

        private void BeginSubmitCityData(Action<SubmitOperation> callback)
        {
            EntitySet entitySet = this.CityDomainContext.EntityContainer.GetEntitySet<City>();
            entitySet.Add(new City() { Name = "NewCity", StateName = "NN", CountyName = "NewCounty" });

            this.SubmitOperation = this.CityDomainContext.SubmitChanges(callback, this.CityDomainContext);
        }

        private void BeginSubmitCityData()
        {
            this.BeginSubmitCityData(null);
        }

        private void AssertLoadCancelled()
        {
            this.AssertOperationCompleted(this.LoadOperation, true);
        }

        private void AssertOperationCompleted(OperationBase operation, bool cancelled)
        {
            Assert.IsTrue(operation.IsComplete);
            Assert.AreEqual(cancelled, operation.IsCanceled);
        }

        #endregion // Test Methods
    }
}
