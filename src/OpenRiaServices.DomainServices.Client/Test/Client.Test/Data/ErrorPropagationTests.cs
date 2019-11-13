extern alias SSmDsClient;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Cities;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using TestDomainServices;

#if! SILVERLIGHT
using System.Web;
#endif

namespace OpenRiaServices.DomainServices.Client.Test
{
    using Resource = SSmDsClient::OpenRiaServices.DomainServices.Client.Resource;

    [TestClass]
    public class ErrorPropagationTests : UnitTestBase
    {
        #region End to end tests - errors during server method invocations
        [TestMethod]
        [Asynchronous]
        [Description("Verify e2e query method error propagation")]
        public void DomainContext_Load_SelectMethodThrows()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            LoadOperation lo = provider.Load(provider.GetMixedTypesThrowQuery(), false);

            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                Assert.IsNotNull(lo.Error);

                // verify we propagate the top level exception message as well
                // as the inner exception message (to one level deep)
                Assert.IsTrue(lo.Error.Message.Contains("Not implemented yet."));
                Assert.IsTrue(lo.Error.Message.Contains("InnerException1"));
                Assert.IsFalse(lo.Error.Message.Contains("InnerException2"));
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(871231)]
        public void DomainContext_Submit_DomainMethodsThrow_EntityValidationError()
        {
            List<string>[] propChanged = new List<string>[] { new List<string>(), new List<string>(), new List<string>() };
            int refZip = 0;

            CityDomainContext citiesProvider = new CityDomainContext(TestURIs.Cities);
            SubmitOperation so = null;
            LoadOperation lo = citiesProvider.Load(citiesProvider.GetZipsQuery(), false);

            // wait for Load to complete, then invoke some domain methods
            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                // invoke methods that cause exception
                Zip[] zips = citiesProvider.Zips.ToArray();
                citiesProvider.ThrowException(zips[0], "EntityValidationException");
                citiesProvider.ThrowException(zips[1], "EntityValidationException");

                // invoke method that does not cause exception
                zips[2].ReassignZipCode(1, true);
                refZip = zips[2].Code;

                zips[0].PropertyChanged += delegate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                {
                    propChanged[0].Add(e.PropertyName);
                };
                zips[1].PropertyChanged += delegate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                {
                    propChanged[1].Add(e.PropertyName);
                };
                zips[2].PropertyChanged += delegate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                {
                    propChanged[2].Add(e.PropertyName);
                };

                so = citiesProvider.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                Assert.IsNotNull(so.Error);
                DomainOperationException ex = so.Error as DomainOperationException;

                // this is a case where method invocations causes a ValidationException.
                Assert.AreEqual(OperationErrorStatus.ValidationFailed, ex.Status);
                Assert.AreEqual(Resource.DomainContext_SubmitOperationFailed_Validation, ex.Message);

                // verify ValidationError collection is correct
                Zip[] zips = citiesProvider.Zips.ToArray();
                IEnumerable<ValidationResult> errors = zips[0].ValidationErrors;
                LogErrorListContents("citiesProvider.Zips[0].ValidationErrors", errors);
                Assert.AreEqual(1, errors.Count());
                ValidationResult vr = errors.First();
                Assert.AreEqual("Invalid Zip properties!", vr.ErrorMessage);
                Assert.AreEqual(2, vr.MemberNames.Count());
                Assert.IsTrue(vr.MemberNames.Contains("CityName"));
                Assert.IsTrue(vr.MemberNames.Contains("CountyName"));

                errors = zips[1].ValidationErrors;
                LogErrorListContents("citiesProvider.Zips[0].ValidationErrors", errors);
                Assert.AreEqual(1, errors.Count());
                vr = errors.First();
                Assert.AreEqual("Invalid Zip properties!", vr.ErrorMessage);
                Assert.AreEqual(2, vr.MemberNames.Count());
                Assert.IsTrue(vr.MemberNames.Contains("CityName"));
                Assert.IsTrue(vr.MemberNames.Contains("CountyName"));

                // verify the Entity.ValidationErrors collection is populated as expected
                Assert.IsTrue(propChanged[0].Contains("HasValidationErrors"));
                Assert.IsTrue(propChanged[1].Contains("HasValidationErrors"));

                // verify entities are not auto-synced back to the client because there were errors
                Assert.IsFalse(propChanged[2].Contains("Code"));
                Assert.IsFalse(propChanged[2].Contains("FourDigit"));
                Assert.IsFalse(propChanged[2].Contains("HasValidationErrors"));
                Assert.AreEqual(0, zips[2].ValidationErrors.Count());
                Assert.AreEqual(refZip, zips[2].Code);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify e2e domain method error propagation and changes not autosynced when any of the server invocations failed")]
        public void DomainContext_Submit_DomainMethodsThrow_ValidationError()
        {
            List<string>[] propChanged = new List<string>[] { new List<string>(), new List<string>(), new List<string>() };
            int refZip = 0;

            CityDomainContext citiesProvider = new CityDomainContext(TestURIs.Cities);
            SubmitOperation so = null;
            LoadOperation lo = citiesProvider.Load(citiesProvider.GetZipsQuery(), false);

            // wait for Load to complete, then invoke some domain methods
            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                // invoke methods that cause exception
                Zip[] zips = citiesProvider.Zips.ToArray();
                citiesProvider.ThrowException(zips[0], "ValidationException");
                citiesProvider.ThrowException(zips[1], "ValidationException");

                // invoke method that does not cause exception
                zips[2].ReassignZipCode(1, true);
                refZip = zips[2].Code;

                zips[0].PropertyChanged += delegate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                {
                    propChanged[0].Add(e.PropertyName);
                };
                zips[1].PropertyChanged += delegate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                {
                    propChanged[1].Add(e.PropertyName);
                };
                zips[2].PropertyChanged += delegate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                {
                    propChanged[2].Add(e.PropertyName);
                };

                so = citiesProvider.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                Assert.IsNotNull(so.Error);
                DomainOperationException ex = so.Error as DomainOperationException;

                // this is a case where method invocations causes a ValidationException.
                Assert.AreEqual(OperationErrorStatus.ValidationFailed, ex.Status);
                Assert.AreEqual(Resource.DomainContext_SubmitOperationFailed_Validation, ex.Message);

                // verify ValidationError collection is correct
                Zip[] zips = citiesProvider.Zips.ToArray();
                IEnumerable<ValidationResult> errors = zips[0].ValidationErrors;
                LogErrorListContents("citiesProvider.Zips[0].ValidationErrors", errors);
                Assert.AreEqual(1, errors.Count());
                ValidationResult vr = errors.First();
                Assert.AreEqual("testing", vr.ErrorMessage);
                Assert.AreEqual(0, vr.MemberNames.Count());

                errors = zips[1].ValidationErrors;
                LogErrorListContents("citiesProvider.Zips[0].ValidationErrors", errors);
                Assert.AreEqual(1, errors.Count());
                vr = errors.First();
                Assert.AreEqual("testing", vr.ErrorMessage);
                Assert.AreEqual(0, vr.MemberNames.Count());

                // verify the Entity.ValidationErrors collection is populated as expected
                Assert.IsTrue(propChanged[0].Contains("HasValidationErrors"));
                Assert.IsTrue(propChanged[1].Contains("HasValidationErrors"));

                // verify entities are not auto-synced back to the client because there were errors
                Assert.IsFalse(propChanged[2].Contains("Code"));
                Assert.IsFalse(propChanged[2].Contains("FourDigit"));
                Assert.IsFalse(propChanged[2].Contains("HasValidationErrors"));
                Assert.AreEqual(0, zips[2].ValidationErrors.Count());
                Assert.AreEqual(refZip, zips[2].Code);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify Entity.ValidationErrors are cleared on RejectChanges")]
        public void DomainContext_Submit_ErrorsClearOnReject()
        {
            int refZip = 0;
            CityDomainContext citiesProvider = new CityDomainContext(TestURIs.Cities);

            SubmitOperation so = null;
            LoadOperation lo = citiesProvider.Load(citiesProvider.GetZipsQuery(), false);

            // wait for Load to complete, then invoke domain method that throws on server. Submit.
            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                // invoke methods that cause exception
                Zip[] zips = citiesProvider.Zips.ToArray();
                zips[0].ThrowException("ValidationException");
                so = citiesProvider.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });

            // wait for submitted event being fired and verify Entity.ValidationErrors is not empty
            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                DomainOperationException ex = so.Error as DomainOperationException;
                Assert.IsNotNull(ex);

                Zip zip = citiesProvider.Zips.First();
                IEnumerable<ValidationResult> errors = zip.ValidationErrors;
                LogErrorListContents("zips[0].ValidationErrors", errors);
                Assert.AreEqual(1, errors.Count());

                // Verify that failed submission does not clear out the last invocation
                Assert.IsFalse(zip.CanThrowException);
                Assert.IsTrue(zip.EntityActions.Any(a => a.Name == "ThrowException"));

                // Add a custom validation error to ensure it gets cleared
                zip.ValidationErrors.Add(new ValidationResult("Temporary Error"));

                // Call RejectChanges and verify ValidationErrors collection is cleared
                citiesProvider.RejectChanges();
                Assert.IsFalse(zip.ValidationErrors.Any());

                // Invoke domain method that does not throw on same entity
                zip.ReassignZipCode(1, true);
                refZip = zip.Code;
                so = citiesProvider.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });

            // wait for submitted event being fired and verify Entity.ValidationErrors remains empty
            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                Zip zip = citiesProvider.Zips.First();
                Assert.IsNull(so.Error);
                Assert.IsFalse(zip.ValidationErrors.Any());
                Assert.AreEqual(refZip + 1, zip.Code);
            });

            EnqueueTestComplete();
        }
        #endregion

        #region End to end tests - errors during server submit validation
        [TestMethod]
        [Asynchronous]
        [Description("Verify ValidationError that happens on server")]
        public void DomainContext_Submit_ValidationErrorOnServer()
        {
            CityDomainContext citiesProvider = new CityDomainContext(TestURIs.Cities);
            Zip newZip = null;

            SubmitOperation so = null;
            LoadOperation lo = citiesProvider.Load(citiesProvider.GetZipsQuery(), false);

            // wait for Load to complete, then invoke domain method that throws on server. Submit.
            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                // Add an entity that will cause a Validation exception on the server (99999 is used as a way to signal failure for our validator)
                newZip = new Zip()
                {
                    Code = 99999,
                    FourDigit = 8625,
                    CityName = "Redmond",
                    CountyName = "King",
                    StateName = "WA"
                };
                citiesProvider.Zips.Add(newZip);
                newZip.ThrowException("InvalidOperationException");

                so = citiesProvider.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });

            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                DomainOperationException ex = so.Error as DomainOperationException;
                Assert.IsNotNull(ex);
                Assert.AreEqual(OperationErrorStatus.ValidationFailed, ex.Status);
                Assert.AreEqual(Resource.DomainContext_SubmitOperationFailed_Validation, ex.Message);

                IEnumerable<ValidationResult> errors = newZip.ValidationErrors;
                LogErrorListContents("newZip.ValidationErrors", errors);
                Assert.AreEqual(1, errors.Count());
                UnitTestHelper.AssertListContains<ValidationResult>(errors, (e => e.ErrorMessage == "Server fails validation"));
            });

            EnqueueTestComplete();
        }
        #endregion

        #region End to end tests - errors during client submit validation

        // test method level validation for CUD/Custom methods - this validation is server side only
        [TestMethod]
        [Asynchronous]
        [Description("Verify that method level validation is executed for CUD operations.")]
        public void DomainContext_Submit_CUDOperationMethodValidation()
        {
            CityDomainContext ctxt = new CityDomainContext(TestURIs.Cities);

            LoadOperation lo = ctxt.Load(ctxt.GetCitiesQuery(), false);
            SubmitOperation so = null;

            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                // update
                City[] cities = ctxt.Cities.Where(p => p.GetType() == typeof(City)).ToArray();
                City city = cities[0];
                city.ZoneID = 693;

                // custom method
                city.AssignCityZone("Z1");

                // delete
                City deletedCity = new City() { Name = "Issaquah", CountyName = "King", StateName = "WA", ZoneID = 693 };
                ctxt.Cities.Attach(deletedCity);
                ctxt.Cities.Remove(deletedCity);

                // insert
                City newCity = new City() { Name = "Sylvannia", CountyName = "Lucas", StateName = "OH" };
                newCity.ZoneID = 693;
                ctxt.Cities.Add(newCity);

                so = ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                DomainOperationException ex = so.Error as DomainOperationException;
                Assert.IsNotNull(ex);
                Assert.AreEqual(OperationErrorStatus.ValidationFailed, ex.Status);
                Assert.AreEqual(Resource.DomainContext_SubmitOperationFailed_Validation, ex.Message);

                EntityChangeSet cs = so.ChangeSet;

                City city = (City)cs.AddedEntities.Single();
                Assert.AreEqual("CityMethodValidator.ValidateMethod Failed (InsertCity)!", city.ValidationErrors.Single().ErrorMessage);

                city = (City)cs.ModifiedEntities.Single();
                ValidationResult[] validationResults = city.ValidationErrors.ToArray();
                Assert.AreEqual("CityMethodValidator.ValidateMethod Failed (UpdateCity)!", validationResults[0].ErrorMessage);
                Assert.AreEqual("CityMethodValidator.ValidateMethod Failed (AssignCityZone)!", validationResults[1].ErrorMessage);

                city = (City)cs.RemovedEntities.Single();
                Assert.AreEqual("CityMethodValidator.ValidateMethod Failed (DeleteCity)!", city.ValidationErrors.Single().ErrorMessage);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify ValidationError that happens on client during submit processing")]
        public void DomainContext_Submit_ValidationErrorDuringClientSubmit()
        {
            CityDomainContext citiesProvider = new CityDomainContext(TestURIs.Cities);
            Zip newZip = new Zip() { Code = 98765, FourDigit = 1234 };
            Zip validZip = new Zip() { Code = 90000, FourDigit = 1000, CityName = "MyCity", StateName = "MY" };
            City deletedCity = null;

            SubmitOperation so = null;
            LoadOperation loadCitiesOperation = citiesProvider.Load(citiesProvider.GetCitiesQuery(), false);
            LoadOperation loadZipsOperation = citiesProvider.Load(citiesProvider.GetZipsQuery(), false);

            // wait for Load to complete, then invoke domain method that throws on server. Submit.
            EnqueueConditional(() => loadCitiesOperation.IsComplete && loadZipsOperation.IsComplete);
            EnqueueCallback(delegate
            {
                // update entity in a way that caused entity validation to fail on client
                Zip[] zips = citiesProvider.Zips.ToArray();
                zips[0].CityName = zips[0].StateName;

                // internally set domain method invocation to cause method param validation to fail on client
                zips[0].CustomMethodInvocation = new EntityAction("ReassignZipCode", new object[] { -10000, true });

                // insert entity that caused object/property validation to fail on client
                citiesProvider.Zips.Add(newZip);

                // Add a temporary error to that invalid object to ensure errors are reset during submit
                newZip.ValidationErrors.Add(new ValidationResult("Temporary Error", new string[] { "StateName" }));

                // insert entity that is valid
                citiesProvider.Zips.Add(validZip);

                // Add a temporary error to that valid object to ensure errors are reset during submit
                validZip.ValidationErrors.Add(new ValidationResult("Temporary Error", new string[] { "StateName" }));

                // remove city
                City[] cities = citiesProvider.Cities.ToArray();
                deletedCity = cities[1];
                citiesProvider.Cities.Remove(deletedCity);

                so = citiesProvider.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });

            // wait for submitted event being fired and verify Entity.ValidationErrors is not empty
            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                DomainOperationException ex = so.Error as DomainOperationException;
                Assert.IsNotNull(ex);
                Assert.AreEqual(OperationErrorStatus.ValidationFailed, ex.Status);
                Assert.AreEqual(Resource.DomainContext_SubmitOperationFailed_Validation, ex.Message);

                // verify errors are generated on the client side
                Zip[] zips = citiesProvider.Zips.ToArray();
                IEnumerable<ValidationResult> errors = zips[0].ValidationErrors;
                LogErrorListContents("citiesProvider.Zips[0].ValidationErrors", errors);
                Assert.AreEqual(2, errors.Count());
                UnitTestHelper.AssertListContains<ValidationResult>(errors, (e => e.ErrorMessage == "The field offset must be between -9999 and 9999."));
                UnitTestHelper.AssertListContains<ValidationResult>(errors, (e => e.ErrorMessage == "Zip codes cannot have matching city and state names" && e.MemberNames.Contains("StateName") && e.MemberNames.Contains("CityName")));

                LogErrorListContents("newZip.ValidationErrors", newZip.ValidationErrors);
                errors = newZip.ValidationErrors;

                // Expect only 2 errors for the properties.  The entity level error is not checked if property level checks fail
                Assert.AreEqual(2, errors.Count());
                UnitTestHelper.AssertListContains<ValidationResult>(errors, (e => e.ErrorMessage == "The CityName field is required."));
                UnitTestHelper.AssertListContains<ValidationResult>(errors, (e => e.ErrorMessage == "The StateName field is required."));

                Assert.AreEqual(0, deletedCity.ValidationErrors.Count(), "The deleted city shouldn't have any validation errors");
                Assert.AreEqual(0, validZip.ValidationErrors.Count(), "The valid city shouldn't have any validation errors");
            });

            EnqueueTestComplete();
        }
        #endregion

        #region End to end tests - client validation fails
        [TestMethod]
        [Asynchronous]
        public void UpdateThrows_ChangeSetStillReturned()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            SubmitOperation so = null;
            LoadOperation lo = provider.Load(provider.GetAsQuery(), false);

            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                Assert.IsNull(lo.Error);
                Assert.IsTrue(lo.Entities.Count() > 0);
                A entity = provider.As.First();
                entity.BID1++;
                so = provider.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                Assert.IsNotNull(so.Error);
                Assert.AreEqual(Resource.DomainContext_SubmitOperationFailed_Validation, so.Error.Message);
                Assert.IsNotNull(so.ChangeSet);
                Assert.AreEqual(1, so.ChangeSet.ModifiedEntities.Count);
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void InsertThrows_AssociationCollectionPropertyIsNull()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);
            SubmitOperation so = null;

            Cart c = new Cart();
            c.CartId = 1;
            CartItem ci1 = new CartItem()
            {
                CartItemId = 1,
                CartId = c.CartId,
                Cart = c,
                Data = "Cart item #1 data"
            };
            CartItem ci2 = new CartItem()
            {
                CartItemId = 2,
                CartId = c.CartId,
                Cart = c,
                Data = "Cart item #2 data"
            };
            provider.Carts.Add(c);
            provider.CartItems.Add(ci1);
            provider.CartItems.Add(ci2);

            so = provider.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                Assert.IsNotNull(so.Error);
                Assert.AreEqual(string.Format(Resource.DomainContext_SubmitOperationFailed, "One or more associated objects were passed for collection property 'Items' on type 'Cart', but the target collection is null."), so.Error.Message);
                Assert.IsNotNull(so.ChangeSet);
                Assert.AreEqual(3, so.ChangeSet.AddedEntities.Count);
            });
            EnqueueTestComplete();
        }
        #endregion

        #region Error pipeline tests
        [TestMethod]
        [Asynchronous]
        [Description("Verify server submit throwing unauthorized exception returns as error 401 and converted to Unauthorized status")]
        public void ErrorPipeline_UnauthorizedEx()
        {
            CityDomainContext citiesProvider = new CityDomainContext(TestURIs.Cities);

            SubmitOperation so = null;
            LoadOperation lo = citiesProvider.Load(citiesProvider.GetZipsQuery(), false);

            // wait for Load to complete, then invoke domain method that throws on server. Submit.
            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                Zip[] zips = citiesProvider.Zips.ToArray();
                citiesProvider.Zips.Remove(zips[1]);
                so = citiesProvider.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });

            // wait for submitted event being fired and verify submittedEventArgs.Error is not null
            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                DomainOperationException error = so.Error as DomainOperationException;
                Assert.IsNotNull(error);
                Assert.AreEqual(string.Format(Resource.DomainContext_SubmitOperationFailed, "Access to operation 'DeleteZip' was denied."), error.Message);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify domain operation entry throwing DomainServiceException is reconstructed on client as submittedEventArgs.Error with Custom status")]
        public void ErrorPipeline_DomainServiceEx()
        {
            CityDomainContext citiesProvider = new CityDomainContext(TestURIs.Cities);

            LoadOperation lo = citiesProvider.Load(citiesProvider.GetZipsQuery(), false);
            SubmitOperation so = null;

            // wait for Load to complete, then invoke domain method that throws on server. Submit.
            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                Zip zip = citiesProvider.Zips.First();
                zip.ThrowException("DomainServiceExceptionWithErrorCode");
                so = citiesProvider.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                DomainException ex = so.Error as DomainException;
                Assert.IsNotNull(ex);
                Assert.AreEqual("testing with error code", ex.Message);
                Assert.AreEqual(10, ex.ErrorCode);

            });

            EnqueueTestComplete();
        }
        #endregion

        // TODO: error propagation: should reloading entities clear the Errors collection?
        // TODO: error propagation: CUD that throws -> invoke that throws

        private static void LogErrorListContents(string listName, IEnumerable<ValidationResult> errors)
        {
            Console.WriteLine(string.Format("Contents of error list {0}:", listName));
            if (errors == null)
            {
                Console.WriteLine("<null>");
                return;
            }
            Console.WriteLine(string.Format("Count: {0}", errors.Count()));
            foreach (ValidationResult error in errors)
            {
                Console.WriteLine(string.Format(
                    "Item: ErrorMessage={0}",
                    error.ErrorMessage
                    ));
                Console.Write(" Member Names=");

                foreach (string memberName in error.MemberNames)
                {
                    Console.Write(string.Format("{0} ", memberName));
                }

            }
            Console.WriteLine();
        }
    }
}
