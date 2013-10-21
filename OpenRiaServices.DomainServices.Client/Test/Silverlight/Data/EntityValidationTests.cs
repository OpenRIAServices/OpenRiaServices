extern alias SSmDsClient;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.DomainServices.Client.Test
{
    using Resource = SSmDsClient::OpenRiaServices.DomainServices.Client.Resource;

    [TestClass]
    public class EntityValidationTests : UnitTestBase
    {
        #region Property tests

        [TestMethod]
        [Description("Setting [Editable(false)] property throws InvalidOperationException")]
        public void Entity_Validation_Property_Fail_ReadOnly_Throws()
        {
            MockEntity entity = new MockEntity();
            ExceptionHelper.ExpectInvalidOperationException(delegate()
            {
                entity.ReadOnlyProperty = "x";
            }, "The 'ReadOnlyProperty' property is read only.");
        }

        [TestMethod]
        [Description("Setting [Editable(true)] property succeeds")]
        public void Entity_Validation_Property_ReadOnly_False()
        {
            MockEntity entity = new MockEntity();
            entity.ReadOnlyFalseProperty = "x";
            Assert.AreEqual("x", entity.ReadOnlyFalseProperty);
        }

        [TestMethod]
        [Description("Setting property without [Editable()] succeeds")]
        public void Entity_Validation_Property_ReadOnly_Missing()
        {
            MockEntity entity = new MockEntity();
            entity.ReadWriteProperty = "x";
            Assert.AreEqual("x", entity.ReadWriteProperty);
        }

        [TestMethod]
        [Description("Setting non-public property throws ArgumentException")]
        public void Entity_Validation_Property_Fail_NonPublic_Throws()
        {
            MockEntity entity = new MockEntity();
            ExceptionHelper.ExpectArgumentException(delegate()
            {
                entity.NonPublicProperty = "x";
            }, "Type 'MockEntity' does not contain a public property named 'NonPublicProperty'.\r\nParameter name: propertyName");
        }

        [TestMethod]
        [Description("Setting unknown property throws ArgumentException")]
        public void Entity_Validation_Property_Fail_Bogus_Throws()
        {
            MockEntity entity = new MockEntity();
            ExceptionHelper.ExpectArgumentException(delegate()
            {
                entity.BogusProperty = "x";
            }, "Type 'MockEntity' does not contain a public property named 'XXX'.\r\nParameter name: propertyName");
        }

        [TestMethod]
        [Description("Setting an property with a public getter but internal setter succeeds")]
        public void Entity_Validation_Property_Internal_Setter()
        {
            MockEntity entity = new MockEntity();
            entity.PublicGetterInternalSetter = "x";
            Assert.AreEqual("x", entity.PublicGetterInternalSetter);
        }

        [TestMethod]
        [Description("Setting [Required] property to null results in a validation error")]
        public void Entity_Validation_Property_Fail_Null_Required()
        {
            MockEntity entity = new MockEntity();

#if SILVERLIGHT
            INotifyDataErrorInfo notifier = (INotifyDataErrorInfo)entity;

            List<string> actualErrors = new List<string>();

            notifier.ErrorsChanged += (s, e) =>
                {
                    actualErrors.Add(e.PropertyName);
                };

            entity.ReadWriteProperty = null;

            // With INotifyDataErrorInfo validation, the property will get set, even though it's invalid
            Assert.AreEqual<string>(null, entity.ReadWriteProperty, "The value should have been set");
            Assert.IsTrue(actualErrors.SequenceEqual(new string[] { "ReadWriteProperty" }), "The list of errors received");

            // Verify that we have the expected validation error for the property
            IEnumerable<ValidationResult> results = entity.ValidationErrors.Where(e => e.MemberNames.Contains("ReadWriteProperty"));
            Assert.AreEqual<int>(1, results.Count(), "Expected a single error for the property");
            Assert.AreEqual<string>("The ReadWriteProperty field is required.", results.Single().ErrorMessage, "ErrorMessage from the result");
#else
            ExceptionHelper.ExpectValidationException(delegate()
            {
                entity.ReadWriteProperty = null;
            }, "The ReadWriteProperty field is required.", typeof(RequiredAttribute), null);
#endif
        }

        [TestMethod]
        [Description("Setting [StringLength(10)] property to too-long a string results in a validation error")]
        public void Entity_Validation_Property_Fail_StringLength_Exceeded_Throws()
        {
            MockEntity entity = new MockEntity();
#if SILVERLIGHT
            INotifyDataErrorInfo notifier = (INotifyDataErrorInfo)entity;

            List<string> actualErrors = new List<string>();

            notifier.ErrorsChanged += (s, e) =>
            {
                actualErrors.Add(e.PropertyName);
            };

            string newValue = "LongerThan10Characters";
            entity.ReadWriteProperty = newValue;

            // With INotifyDataErrorInfo validation, the property will get set, even though it's invalid
            Assert.AreEqual<string>(newValue, entity.ReadWriteProperty, "The value should have been set");
            Assert.IsTrue(actualErrors.SequenceEqual(new string[] { "ReadWriteProperty" }), "The list of errors received");

            // Verify that we have the expected validation error for the property
            IEnumerable<ValidationResult> results = entity.ValidationErrors.Where(e => e.MemberNames.Contains("ReadWriteProperty"));
            Assert.AreEqual<int>(1, results.Count(), "Expected a single error for the property");
            Assert.AreEqual<string>("The field ReadWriteProperty must be a string with a maximum length of 10.", results.Single().ErrorMessage, "ErrorMessage from the result");
#else
            ExceptionHelper.ExpectValidationException(delegate()
            {
                entity.ReadWriteProperty = "LongerThan10Characters";
            }, "The field ReadWriteProperty must be a string with a maximum length of 10.", typeof(StringLengthAttribute), "LongerThan10Characters");
#endif
        }
        #endregion Property tests

        #region Object tests

        [TestMethod]
        [Description("Missing [Required] property throws ValidationException")]
        public void Entity_Validation_Object_Fail_Null_Required_Throws()
        {
            MockEntity entity = new MockEntity();
            ValidationContext context = new ValidationContext(entity, null, null);
            ExceptionHelper.ExpectValidationException(delegate()
            {
                System.ComponentModel.DataAnnotations.Validator.ValidateObject(entity, context);
            }, "The ReadWriteProperty field is required.", typeof(RequiredAttribute), null);
        }
        #endregion Object tests

        #region ValidationResult Assumption Tests

        [TestMethod]
        [Description("Verifies assumption that instances of ValidationResult with null error messages and no members are considered different")]
        public void ValidationResult_NullErrorMessageNoMembers()
        {
            ValidationResult vr1 = new ValidationResult(null);
            ValidationResult vr2 = new ValidationResult(null);

            Assert.AreNotEqual(vr1.GetHashCode(), vr2.GetHashCode(), "GetHashCode");
            Assert.IsFalse(vr1.Equals(vr2), "vr1.Equals(vr2)");
        }

        [TestMethod]
        [Description("Verifies assumption that instances of ValidationResult that have the same error and no members are considered different")]
        public void ValidationResult_SameErrorMessageNoMembers()
        {
            ValidationResult vr1 = new ValidationResult("Error");
            ValidationResult vr2 = new ValidationResult("Error");

            Assert.AreNotEqual(vr1.GetHashCode(), vr2.GetHashCode(), "GetHashCode");
            Assert.IsFalse(vr1.Equals(vr2), "vr1.Equals(vr2)");
        }

        [TestMethod]
        [Description("Verifies assumption that instances of ValidationResult with null error message and the same members are considered different")]
        public void ValidationResult_NullErrorMessageSameMembers()
        {
            ValidationResult vr1 = new ValidationResult(null, new string[] { "Member" });
            ValidationResult vr2 = new ValidationResult(null, new string[] { "Member" });

            Assert.AreNotEqual(vr1.GetHashCode(), vr2.GetHashCode(), "GetHashCode");
            Assert.IsFalse(vr1.Equals(vr2), "vr1.Equals(vr2)");
        }

        [TestMethod]
        [Description("Verifies assumption that instances of ValidationResult that have error message and the same members are considered different")]
        public void ValidationResult_SameErrorMessageSameMembers()
        {
            ValidationResult vr1 = new ValidationResult("Error", new string[] { "Member" });
            ValidationResult vr2 = new ValidationResult("Error", new string[] { "Member" });

            Assert.AreNotEqual(vr1.GetHashCode(), vr2.GetHashCode(), "GetHashCode");
            Assert.IsFalse(vr1.Equals(vr2), "vr1.Equals(vr2)");
        }

        [TestMethod]
        [Description("Verifies assumption that instances of ValidationResult that have different error messages and the same members are considered different")]
        public void ValidationResult_DifferentErrorMessageSameMembers()
        {
            ValidationResult vr1 = new ValidationResult("Error 1", new string[] { "Member" });
            ValidationResult vr2 = new ValidationResult("Error 2", new string[] { "Member" });

            Assert.AreNotEqual(vr1.GetHashCode(), vr2.GetHashCode(), "GetHashCode");
            Assert.IsFalse(vr1.Equals(vr2), "vr1.Equals(vr2)");
        }

        [TestMethod]
        [Description("Verifies assumption that instances of ValidationResult that the same error message and the different members are considered different")]
        public void ValidationResult_SameErrorMessageDifferentMembers()
        {
            ValidationResult vr1 = new ValidationResult("Error", new string[] { "Member 1" });
            ValidationResult vr2 = new ValidationResult("Error", new string[] { "Member 2" });

            Assert.AreNotEqual(vr1.GetHashCode(), vr2.GetHashCode(), "GetHashCode");
            Assert.IsFalse(vr1.Equals(vr2), "vr1.Equals(vr2)");
        }

        #endregion ValidationResult Assumption Tests

        #region ValidationContext tests

        [TestMethod]
        [WorkItem(871338)]
        [Description("When a ValidationContext is provided to the DomainContext, it should be used for Property Validation")]
        public void ValidationContextUsedForPropertyValidation()
        {
            Dictionary<object, object> items = new Dictionary<object,object>();
            items.Add("TestMethod", "ValidationContextUsedForPropertyValidation");
            ValidationContext providedValidationContext = new ValidationContext(this, null, items);

            Cities.CityDomainContext domainContext = new Cities.CityDomainContext(TestURIs.Cities);
            domainContext.ValidationContext = providedValidationContext;

            bool callbackCalled = false;

            Cities.City newCity = new Cities.City();
            domainContext.Cities.Add(newCity);

            newCity.ValidatePropertyCallback = validationValidationContext =>
                {
                    Assert.AreNotSame(providedValidationContext, validationValidationContext, "The ValidationContext provided to ValidationProperty should not be the same actual instance of the ValidationContext we provided");
                    Assert.IsTrue(validationValidationContext.Items.ContainsKey("TestMethod"), "The ValidationContext provided should have the items we provided");
                    Assert.AreEqual(providedValidationContext.Items["TestMethod"], validationValidationContext.Items["TestMethod"], "The ValidationContext provided should have the items we provided");

                    callbackCalled = true;
                };

            // Set a property, triggering property validation
            newCity.Name = "Foo";
            Assert.IsTrue(callbackCalled, "Make sure our callback was called to perform the test");
        }

        [TestMethod]
        [WorkItem(871338)]
        [Description("When a ValidationContext is provided to the DomainContext, it should be used for Entity Validation")]
        public void ValidationContextUsedForEntityValidation()
        {
            Dictionary<object, object> items = new Dictionary<object, object>();
            items.Add("TestMethod", "ValidationContextUsedForPropertyValidation");
            ValidationContext providedValidationContext = new ValidationContext(this, null, items);

            Cities.CityDomainContext domainContext = new Cities.CityDomainContext(TestURIs.Cities);
            domainContext.ValidationContext = providedValidationContext;

            bool callbackCalled = false;

            Cities.City newCity = new Cities.City();
            domainContext.Cities.Add(newCity);

            newCity.ValidateCityCallback = validationValidationContext =>
            {
                Assert.AreNotSame(providedValidationContext, validationValidationContext, "The ValidationContext provided to ValidationProperty should not be the same actual instance of the ValidationContext we provided");
                Assert.IsTrue(validationValidationContext.Items.ContainsKey("TestMethod"), "The ValidationContext provided should have the items we provided");
                Assert.AreEqual(providedValidationContext.Items["TestMethod"], validationValidationContext.Items["TestMethod"], "The ValidationContext provided should have the items we provided");

                callbackCalled = true;
            };

            // Entity-level validation is performed by calling EndEdit with valid properties
            IEditableObject editableCity = (IEditableObject)newCity;
            editableCity.BeginEdit();
            newCity.Name = "Cincinnati";
            newCity.StateName = "OH";
            newCity.CountyName = "Hamilton";
            editableCity.EndEdit();

            Assert.IsTrue(callbackCalled, "Make sure our callback was called to perform the test");
        }

        [TestMethod]
        [WorkItem(871338)]
        [Description("When the ValidationContext is changed on the DomainContext, it is pushed through to the Entity")]
        public void ValidationContextUpdatedForEntityWhenChangedForDomainContext()
        {
            Cities.CityDomainContext domainContext = new Cities.CityDomainContext(TestURIs.Cities);

            Cities.City newCity = new Cities.City();
            domainContext.Cities.Add(newCity);

            // Set up the ValidationContext after adding the entity into the domain context
            // to ensure that the updated validation context is plumbed through
            Dictionary<object, object> items = new Dictionary<object, object>();
            items.Add("TestMethod", "ValidationContextUsedForPropertyValidation");
            ValidationContext providedValidationContext = new ValidationContext(this, null, items);
            domainContext.ValidationContext = providedValidationContext;

            bool callbackCalled = false;

            Action<ValidationContext> assertValidationContext = validationValidationContext =>
            {
                Assert.AreNotSame(providedValidationContext, validationValidationContext, "The ValidationContext provided to ValidationProperty should not be the same actual instance of the ValidationContext we provided");
                Assert.IsTrue(validationValidationContext.Items.ContainsKey("TestMethod"), "The ValidationContext provided should have the items we provided");
                Assert.AreEqual(providedValidationContext.Items["TestMethod"], validationValidationContext.Items["TestMethod"], "The ValidationContext provided should have the items we provided");

                callbackCalled = true;
            };

            newCity.ValidatePropertyCallback = assertValidationContext;
            newCity.ValidateCityCallback = assertValidationContext;

            // Entity-level validation is performed by calling EndEdit with valid properties
            IEditableObject editableCity = (IEditableObject)newCity;
            editableCity.BeginEdit();
            newCity.Name = "Cincinnati";
            newCity.StateName = "OH";
            newCity.CountyName = "Hamilton";
            editableCity.EndEdit();

            Assert.IsTrue(callbackCalled, "Make sure our callback was called to perform the test");
        }

        #endregion

#if SILVERLIGHT
        #region INotifyDataErrorInfo Tests

        [TestMethod]
        [Description("When an entity gains property-level errors or loses all of them, the HasValidationErrors property changed event should occur")]
        public void ValidationErrors_PropertyChangedEventsPropertyErrors()
        {
            MockEntity entity = new MockEntity();
            INotifyDataErrorInfo notifier = (INotifyDataErrorInfo)entity;

            // Expect property change notifications for: HasValidationErrors, ValidationErrors
            string[] expectedChanges = new string[] { "HasValidationErrors", "ValidationErrors" };

            List<string> actualChanges = new List<string>();
            entity.PropertyChanged += (s, e) => actualChanges.Add(e.PropertyName);

            Assert.IsFalse(entity.HasValidationErrors, "HasValidationErrors before adding");
            Assert.IsFalse(notifier.HasErrors, "HasErrors before adding");

            entity.ValidationErrors.Add(new ValidationResult("Error Message", new string[] { "Foo" }));
            Assert.IsTrue(entity.HasValidationErrors, "HasValidationErrors after adding");
            Assert.IsTrue(notifier.HasErrors, "HasErrors after adding");
            Assert.IsTrue(actualChanges.SequenceEqual(expectedChanges), "Property changes after adding");
            actualChanges.Clear();

            entity.ValidationErrors.Clear();
            Assert.IsFalse(entity.HasValidationErrors, "HasValidationErrors after clearing");
            Assert.IsFalse(notifier.HasErrors, "HasErrors after clearing");
            Assert.IsTrue(actualChanges.SequenceEqual(expectedChanges), "Property changes after clearing");
        }

        [TestMethod]
        [Description("When an entity gains entity-level errors or loses them, the HasValidationErrors property changed event should occur")]
        public void ValidationErrors_PropertyChangedEventsEntityErrors()
        {
            MockEntity entity = new MockEntity();
            INotifyDataErrorInfo notifier = (INotifyDataErrorInfo)entity;

            // Expect property change notifications for: HasValidationErrors, ValidationErrors
            string[] expectedChanges = new string[] { "HasValidationErrors", "ValidationErrors" };

            List<string> actualChanges = new List<string>();
            entity.PropertyChanged += (s, e) => actualChanges.Add(e.PropertyName);

            Assert.IsFalse(entity.HasValidationErrors, "HasValidationErrors before adding");
            Assert.IsFalse(notifier.HasErrors, "HasErrors before adding");

            entity.ValidationErrors.Add(new ValidationResult("Error Message", null));
            Assert.IsTrue(entity.HasValidationErrors, "HasValidationErrors after adding");
            Assert.IsTrue(notifier.HasErrors, "HasErrors after adding");
            Assert.IsTrue(actualChanges.SequenceEqual(expectedChanges), "Property changes after adding");
            actualChanges.Clear();

            entity.ValidationErrors.Clear();
            Assert.IsFalse(entity.HasValidationErrors, "HasValidationErrors after clearing");
            Assert.IsFalse(notifier.HasErrors, "HasErrors after clearing");
            Assert.IsTrue(actualChanges.SequenceEqual(expectedChanges), "Property changes after clearing");
        }

        [TestMethod]
        [Description("When an entity gains or loses errors, ErrorsChanged events should occur for those properties")]
        public void ValidationErrors_ErrorsChangedEvents()
        {
            MockEntity entity = new MockEntity();
            INotifyDataErrorInfo notifier = (INotifyDataErrorInfo)entity;

            List<string> actualChanges = new List<string>();
            notifier.ErrorsChanged += (s, e) => actualChanges.Add(e.PropertyName);

            // Adding an error for Foo should raise a single event only for Foo
            string[] oneMemberName = new string[] { "Foo" };
            ValidationResult singlePropertyResult = new ValidationResult("Error Message", oneMemberName);
            entity.ValidationErrors.Add(singlePropertyResult);
            Assert.IsTrue(actualChanges.SequenceEqual(oneMemberName), "Events when adding single property error");
            actualChanges.Clear();

            // Adding an error for Foo and Bar will raise events for both properties even though it's a single error
            // Additionally, having a member name of null (entity-level error) will cause an event with a null property name
            string[] twoMemberNamesPlusNull = new string[] { "Foo", "Bar", null };
            ValidationResult multiPropertyResult = new ValidationResult("Error Message", twoMemberNamesPlusNull);
            entity.ValidationErrors.Add(multiPropertyResult);
            Assert.IsTrue(actualChanges.OrderBy(s => s).SequenceEqual(twoMemberNamesPlusNull.OrderBy(s => s)), "Events when adding multi-property error");
            actualChanges.Clear();

            // Removing the single-property result will raise an event for that property
            entity.ValidationErrors.Remove(singlePropertyResult);
            Assert.IsTrue(actualChanges.SequenceEqual(oneMemberName), "HasChanges events when removing single property error");
            actualChanges.Clear();

            // Removing the multi-property result will raise events for both properties
            entity.ValidationErrors.Remove(multiPropertyResult);
            Assert.IsTrue(actualChanges.OrderBy(s => s).SequenceEqual(twoMemberNamesPlusNull.OrderBy(s => s)), "HasChanges events when removing multi-property error");
        }

        [TestMethod]
        [Description("ValidateProperty causes all errors for that property to be replaced with the new errors")]
        public void ValidationErrors_PropertyErrorsAreReplacedFromValidateProperty()
        {
            MockEntity entity = new MockEntity();
            INotifyDataErrorInfo notifier = (INotifyDataErrorInfo)entity;

            string[] affectedMemberNames = new string[] { "ReadWriteProperty" };
            string[] unaffectedMemberNames = new string[] { "Foo", "Bar" };

            // Add a fake error that we'll ensure gets cleared out as well as other errors that will remain
            entity.ValidationErrors.Add(new ValidationResult("Fake error for affected members", affectedMemberNames));
            entity.ValidationErrors.Add(new ValidationResult("Fake error for unaffected members", unaffectedMemberNames));

            List<string> actualChanges = new List<string>();
            IEnumerable<ValidationResult> errors;
            notifier.ErrorsChanged += (s, e) => actualChanges.Add(e.PropertyName);

            // Set the property to an invalid value
            entity.ReadWriteProperty = null;
            errors = notifier.GetErrors(affectedMemberNames.Single()).Cast<ValidationResult>();

            Assert.IsTrue(actualChanges.SequenceEqual(affectedMemberNames), "Events when setting to an invalid value");
            Assert.AreEqual<int>(1, errors.Count(), "Expect a single error after setting to an invalid value");

            errors = notifier.GetErrors("Foo").Cast<ValidationResult>();
            Assert.AreEqual<int>(1, errors.Count(), "Expect a single Foo error after setting to an invalid value");

            errors = notifier.GetErrors("Bar").Cast<ValidationResult>();
            Assert.AreEqual<int>(1, errors.Count(), "Expect a single Bar error after setting to an invalid value");
            actualChanges.Clear();

            // Set the property to a valid value
            entity.ReadWriteProperty = "Valid";
            errors = notifier.GetErrors(affectedMemberNames.Single()).Cast<ValidationResult>();

            Assert.IsTrue(actualChanges.SequenceEqual(affectedMemberNames), "Events when setting to a valid value");
            Assert.AreEqual<int>(0, errors.Count(), "Expect no errors after setting to a valid value");
            actualChanges.Clear();
        }

        [TestMethod]
        [WorkItem(854187)]
        [Description("Verifies that when an invalid entity's changes are rejected, the errors are cleared when RejectChanges is called")]
        public void Entity_RejectChanges_Clears_ValidationErrors()
        {
            // This test requires an entity that is attached
            Cities.CityDomainContext domainContext = new Cities.CityDomainContext();
            Cities.City entity = new Cities.City();
            domainContext.Cities.Add(entity);

            INotifyDataErrorInfo notifier = (INotifyDataErrorInfo)entity;
            List<string> actualErrors = new List<string>();
            notifier.ErrorsChanged += (s, e) => actualErrors.Add(e.PropertyName);

            entity.StateName = "Not a State Name"; // Marks the entity as changed and adds a validation result for StateName
            entity.ValidationErrors.Add(new ValidationResult("Invalid Property Error", new string[] { "Foo" }));
            entity.ValidationErrors.Add(new ValidationResult("Entity Error", null));

            string[] membersNotifed = new string[] { "StateName", "Foo", null };
            Assert.IsTrue(actualErrors.OrderBy(s => s).SequenceEqual(membersNotifed.OrderBy(s => s)), "The list of errors when adding errors");
            actualErrors.Clear();

            ((IRevertibleChangeTracking)entity).RejectChanges();
            Assert.IsTrue(actualErrors.OrderBy(s => s).SequenceEqual(membersNotifed.OrderBy(s => s)), "The list of errors when rejecting changes");
        }

        [TestMethod]
        [Description("Entities can override ValidateProperty behavior to throw ValidationExceptions")]
        public void ValidatePropertyOverrideCanThrowValidationException()
        {
            Cities.City city = new Cities.City() { Name = "Cincinnati" };
            city.ThrowValidationExceptions = true;

            string invalidName = "This 1 is an invalid city name";
            ExceptionHelper.ExpectValidationException(() => city.Name = invalidName, "The field CityName must match the regular expression '^[A-Z]+[a-z A-Z]*$'.", typeof(RegularExpressionAttribute), invalidName);
            Assert.AreEqual<string>("Cincinnati", city.Name, "The city name should be unchanged when invalid");
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(858230)]
        [Description("When EndEdit is called after reverting changes on the entity, the ValidationErrors collection is cleared of property errors")]
        public void Entity_RevertChanges_EndEdit_Clears_Property_ValidationErrors()
        {
            Cities.CityDomainContext dp = new Cities.CityDomainContext(TestURIs.Cities);    // Abs URI so runs on desktop too
            LoadOperation lo = dp.Load(dp.GetCitiesQuery(), false);

            EnqueueConditional(() => lo.IsComplete);

            EnqueueCallback(() =>
            {
                Cities.City city = dp.Cities.First();
                IEditableObject editableCity = (IEditableObject)city;
                string originalName = city.Name;

                // Edit the entity to give it property-level validation errors
                editableCity.BeginEdit();
                city.Name = null;
                editableCity.EndEdit();
                Assert.AreEqual<int>(1, city.ValidationErrors.Count, "We should have a validation error after the first EndEdit");
                Assert.IsTrue(city.HasValidationErrors, "HasValidationErrors after the first EndEdit");

                // Revert the changes so that it doesn't have property-level validation errors anymore
                editableCity.BeginEdit();
                city.Name = originalName;
                editableCity.EndEdit();
                Assert.AreEqual<int>(0, city.ValidationErrors.Count, "ValidationErrors should be cleared after the second EndEdit");
                Assert.IsFalse(city.HasValidationErrors, "HasValidationErrors after the second EndEdit");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(858230)]
        [Description("When EndEdit is called after reverting changes on the entity, the ValidationErrors collection is cleared of entity errors")]
        public void Entity_RevertChanges_EndEdit_Clears_Entity_ValidationErrors()
        {
            Cities.CityDomainContext dp = new Cities.CityDomainContext(TestURIs.Cities);    // Abs URI so runs on desktop too
            LoadOperation lo = dp.Load(dp.GetCitiesQuery(), false);

            EnqueueConditional(() => lo.IsComplete);

            EnqueueCallback(() =>
            {
                Cities.City city = dp.Cities.First();
                IEditableObject editableCity = (IEditableObject)city;

                // Make the entity have entity-level validation errors
                editableCity.BeginEdit();
                city.MakeEntityValidationFail = true;
                editableCity.EndEdit();
                Assert.AreEqual<int>(1, city.ValidateCityCallCount, "Our entity-level validation should have been called");
                Assert.AreEqual<int>(1, city.ValidationErrors.Count, "We should have a validation error after the first EndEdit");
                Assert.IsTrue(city.HasValidationErrors, "HasValidationErrors after the first EndEdit");

                // Make the entity have no entity-level errors
                editableCity.BeginEdit();
                city.MakeEntityValidationFail = false;
                city.ValidateCityCallCount = 0; // reset the call count
                editableCity.EndEdit();
                Assert.AreEqual<int>(1, city.ValidateCityCallCount, "Our entity-level validation should have been called");
                Assert.AreEqual<int>(0, city.ValidationErrors.Count, "ValidationErrors should be cleared after the second EndEdit");
                Assert.IsFalse(city.HasValidationErrors, "HasValidationErrors after the second EndEdit");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [WorkItem(858230)]
        [Description("INotifyDataErrorInfo.GetErrors() prevents deferred enumeration")]
        public void GetErrors_Prevents_Deferred_Enumeration()
        {
            Cities.City city = new Cities.City();
            INotifyDataErrorInfo notifier = (INotifyDataErrorInfo)city;
            System.Collections.IEnumerable noErrors = notifier.GetErrors("Name");

            city.ValidationErrors.Add(new ValidationResult("Error", new string[] { "Name" }));
            System.Collections.IEnumerable withError = notifier.GetErrors("Name");

            Assert.AreEqual<int>(0, noErrors.Cast<ValidationResult>().Count(), "Count from first enumerable");
            Assert.AreEqual<int>(1, withError.Cast<ValidationResult>().Count(), "Count from second enumerable");
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(858230)]
        [Description("When CancelEdit is called, any previous validation errors are restored for entities that were previously unmodified")]
        public void CancelEdit_Restores_ValidationErrors_ForUnmodifiedEntity()
        {
            Cities.CityDomainContext dp = new Cities.CityDomainContext(TestURIs.Cities);    // Abs URI so runs on desktop too
            LoadOperation lo = dp.Load(dp.GetCitiesQuery(), false);

            EnqueueConditional(() => lo.IsComplete);

            EnqueueCallback(() =>
            {
                // Start with an existing, valid entity
                Cities.City city = dp.Cities.First();

                IEditableObject editableCity = (IEditableObject)city;

                string customEntityError = "Entity-level error that was added manually";
                city.ValidationErrors.Add(new ValidationResult(customEntityError));

                // We want to add a two-member error manually. When we re-validate ZoneID, the error for CountyName will also go away.
                string customPropertyError = "Property-level error that was added manually";
                city.ValidationErrors.Add(new ValidationResult(customPropertyError, new string[] { "ZoneID", "CountyName" }));

                Assert.AreEqual<int>(2, city.ValidationErrors.Count, "Error count before editing");

                // Edit the city, making changes that will replace the property errors for ZoneID and CountyName (because it was tied to the ZoneID member too)
                // with new errors, leaving the entity-level error in place.
                editableCity.BeginEdit();
                city.ZoneID = -1; // Out of range
                city.ZoneName = Cities.CityPropertyValidator.InvalidZoneName;
                Assert.AreEqual<int>(3, city.ValidationErrors.Count, "Error count before CancelEdit");

                INotifyDataErrorInfo notifier = (INotifyDataErrorInfo)city;
                List<Tuple<string, IEnumerable<ValidationResult>>> errorNotifications = new List<Tuple<string, IEnumerable<ValidationResult>>>();
                notifier.ErrorsChanged += (s, e) =>
                {
                    errorNotifications.Add(new Tuple<string, IEnumerable<ValidationResult>>(e.PropertyName, notifier.GetErrors(e.PropertyName).Cast<ValidationResult>()));
                };

                /// When we cancel the edit, the following changes will occur to the validation errors, in no particular order
                /// - RejectChanges is called, which clears all validation errors, raising ErrorsChanged for ZoneName, ZoneID, and null (entity-level)
                /// - Resurrection of the manually-added Name/CountyName error
                /// - Resurrection of the manually-added entity-level error
                /// 
                /// This results in a total of 6 ErrorsChanged events, with 2 events each for ZoneID and null (entity-level), and 1 each
                /// for CountyName and ZoneName.
                /// 
                /// Note that this is different from when we call CancelEdit on an Added or Modified entity, because
                /// state is restored in one step rather than in two.  This difference is acceptable.

                editableCity.CancelEdit();
                // Verify our validation errors count reverted back to the 2 errors we had before editing
                Assert.AreEqual<int>(2, city.ValidationErrors.Count, "Error count after CancelEdit");

                // Verify the entity-level error that we manually added still shows up
                Assert.AreEqual<string>(customEntityError, city.ValidationErrors.Single(e => e.MemberNames.Count() == 0 || (e.MemberNames.Count() == 1 && string.IsNullOrEmpty(e.MemberNames.Single()))).ErrorMessage, "ErrorMessage after CancelEdit");

                // Verify the property-level error that we manually added still shows up
                Assert.AreEqual<string>(customPropertyError, city.ValidationErrors.Single(e => e.MemberNames.Contains("ZoneID")).ErrorMessage, "ErrorMessage for ZoneID after CancelEdit");
                Assert.AreEqual<string>(customPropertyError, city.ValidationErrors.Single(e => e.MemberNames.Contains("CountyName")).ErrorMessage, "ErrorMessage for CountyName after CancelEdit");

                // Verify that we got the 6 expected notifications from INotifyDataErrorInfo
                Assert.AreEqual<int>(6, errorNotifications.Count, "Error notification count");

                // Two notifications for Name and null (clearing, and adding), and one for StateName (cleared) and CountyName (added)
                Assert.AreEqual<int>(2, errorNotifications.Count(e => e.Item1 == "ZoneID"), "Count of ZoneID notifications");
                Assert.AreEqual<int>(1, errorNotifications.Count(e => e.Item1 == "ZoneName"), "Count of ZoneName notifications");
                Assert.AreEqual<int>(1, errorNotifications.Count(e => e.Item1 == "CountyName"), "Count of CountyName notifications");
                Assert.AreEqual<int>(2, errorNotifications.Count(e => string.IsNullOrEmpty(e.Item1)), "Count of entity-level notifications");

                // When the first Name and null notifications occurred, there were no errors for either
                // When the StateName notification occurred, there was no error for that property
                // When the second Name and null notification occurred, an error had been added for each
                // When the CountyName notification occurred, there was an error added for that property
                Assert.AreEqual<int>(0, errorNotifications.First(e => e.Item1 == "ZoneID").Item2.Count(), "Error count for ZoneID at time of first notification");
                Assert.AreEqual<int>(0, errorNotifications.First(e => string.IsNullOrEmpty(e.Item1)).Item2.Count(), "Error count for entity errors at time of first notification");
                Assert.AreEqual<int>(0, errorNotifications.Single(e => e.Item1 == "ZoneName").Item2.Count(), "Error count for ZoneName at time of notification");
                Assert.AreEqual<int>(1, errorNotifications.Where(e => e.Item1 == "ZoneID").Skip(1).First().Item2.Count(), "Error count for ZoneID at time of second notification");
                Assert.AreEqual<int>(1, errorNotifications.Where(e => string.IsNullOrEmpty(e.Item1)).Skip(1).First().Item2.Count(), "Error count for entity errors at time of second notification");
                Assert.AreEqual<int>(1, errorNotifications.Single(e => e.Item1 == "CountyName").Item2.Count(), "Error count for CountyName at time of notification");

                // Verify the manually-added errors were in place at the time of the notifications
                Assert.AreEqual<string>(customPropertyError, errorNotifications.Where(e => e.Item1 == "ZoneID").Skip(1).First().Item2.Single().ErrorMessage, "ErrorMessage of the ZoneID error when its notification was raised");
                Assert.AreEqual<string>(customPropertyError, errorNotifications.Single(e => e.Item1 == "CountyName").Item2.Single().ErrorMessage, "ErrorMessage of the StateName error when its notification was raised");
                Assert.AreEqual<string>(customEntityError, errorNotifications.Where(e => string.IsNullOrEmpty(e.Item1)).Skip(1).First().Item2.Single().ErrorMessage, "ErrorMessage of the entity-level error when its notification was raised");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [WorkItem(858230)]
        [Description("When CancelEdit is called, any previous validation errors are restored for entities in an Added state")]
        public void CancelEdit_Restores_ValidationErrors_ForAddedEntity()
        {
            // Start with a valid entity
            Cities.City city = new Cities.City() { Name = "Cincinnati", StateName = "OH", CountyName = "Hamilton" };

            IEditableObject editableCity = (IEditableObject)city;

            string customEntityError = "Entity-level error that was added manually";
            city.ValidationErrors.Add(new ValidationResult(customEntityError));

            string customPropertyError = "Property-level error that was added manually";
            city.ValidationErrors.Add(new ValidationResult(customPropertyError, new string[] { "ZoneID", "CountyName" }));

            Assert.AreEqual<int>(2, city.ValidationErrors.Count, "Error count before editing");

            // Edit the city, making changes that will replace the property errors for Name and CountyName (because it was tied to the Name member too)
            // with new errors, leaving the entity-level error in place.
            editableCity.BeginEdit();
            city.ZoneID = -1; // Out of range
            city.ZoneName = Cities.CityPropertyValidator.InvalidZoneName;
            Assert.AreEqual<int>(3, city.ValidationErrors.Count, "Error count before CancelEdit");

            INotifyDataErrorInfo notifier = (INotifyDataErrorInfo)city;
            List<Tuple<string, IEnumerable<ValidationResult>>> errorNotifications = new List<Tuple<string, IEnumerable<ValidationResult>>>();
            notifier.ErrorsChanged += (s, e) =>
                {
                    errorNotifications.Add(new Tuple<string, IEnumerable<ValidationResult>>(e.PropertyName, notifier.GetErrors(e.PropertyName).Cast<ValidationResult>()));
                };

            /// When we cancel the edit, the following changes will occur to the validation errors, in no particular order
            /// - StateName is no longer invalid; notification, with no errors for that property
            /// - Name loses its required error but regains the manually-added custom error
            /// - CountyName regains the manually-added error
            /// - The custom entity-level error message that was added will be restored

            editableCity.CancelEdit();
            // Verify our validation errors count reverted back to the 2 errors we had before editing
            Assert.AreEqual<int>(2, city.ValidationErrors.Count, "Error count after CancelEdit");

            // Verify the entity-level error that we manually added still shows up
            Assert.AreEqual<string>(customEntityError, city.ValidationErrors.Single(e => e.MemberNames.Count() == 0 || (e.MemberNames.Count() == 1 && string.IsNullOrEmpty(e.MemberNames.Single()))).ErrorMessage, "ErrorMessage after CancelEdit");

            // Verify the property-level error that we manually added still shows up
            Assert.AreEqual<string>(customPropertyError, city.ValidationErrors.Single(e => e.MemberNames.Contains("ZoneID")).ErrorMessage, "ErrorMessage for ZoneID after CancelEdit");
            Assert.AreEqual<string>(customPropertyError, city.ValidationErrors.Single(e => e.MemberNames.Contains("CountyName")).ErrorMessage, "ErrorMessage for CountyName after CancelEdit");

            // Verify that we got the 4 expected notifications from INotifyDataErrorInfo
            Assert.AreEqual<int>(4, errorNotifications.Count, "Error notification count");

            // One of which should have been for Name, one for StateName, one for CountyName, and one for the entity-level error
            Assert.AreEqual<int>(1, errorNotifications.Count(e => e.Item1 == "ZoneID"), "Count of Name notifications");
            Assert.AreEqual<int>(1, errorNotifications.Count(e => e.Item1 == "ZoneName"), "Count of ZoneName notifications");
            Assert.AreEqual<int>(1, errorNotifications.Count(e => e.Item1 == "CountyName"), "Count of CountyName notifications");
            Assert.AreEqual<int>(1, errorNotifications.Count(e => string.IsNullOrEmpty(e.Item1)), "Count of entity-level notifications");

            // Verify that when the notification occurred for Name, CountyName, and the entity, we had a single error for each, and there was no error for StateName
            Assert.AreEqual<int>(1, errorNotifications.Single(e => e.Item1 == "ZoneID").Item2.Count(), "Error count for ZoneID at time of notification");
            Assert.AreEqual<int>(1, errorNotifications.Single(e => e.Item1 == "CountyName").Item2.Count(), "Error count for CountyName at time of notification");
            Assert.AreEqual<int>(1, errorNotifications.Single(e => string.IsNullOrEmpty(e.Item1)).Item2.Count(), "Error count for entity errors at time of notification");
            Assert.AreEqual<int>(0, errorNotifications.Single(e => e.Item1 == "ZoneName").Item2.Count(), "Error count for ZoneName at time of notification");

            // Verify the manually-added errors were in place at the time of the notifications
            Assert.AreEqual<string>(customPropertyError, errorNotifications.Single(e => e.Item1 == "ZoneID").Item2.Single().ErrorMessage, "ErrorMessage of the ZoneID error when its notification was raised");
            Assert.AreEqual<string>(customPropertyError, errorNotifications.Single(e => e.Item1 == "CountyName").Item2.Single().ErrorMessage, "ErrorMessage of the CountyName error when its notification was raised");
            Assert.AreEqual<string>(customEntityError, errorNotifications.Single(e => string.IsNullOrEmpty(e.Item1)).Item2.Single().ErrorMessage, "ErrorMessage of the entity-level error when its notification was raised");
        }

        [TestMethod]
        [WorkItem(187138)]
        [Description("Ensure that the ErrorsChanged event properly subscribes and unsubscribes handlers")]
        public void ErrorsChanged_Event_Subscription()
        {
            // Start with a valid entity
            Cities.City city = new Cities.City() { Name = "Cincinnati", StateName = "OH", CountyName = "Hamilton" };

            INotifyDataErrorInfo notifier = city as INotifyDataErrorInfo;

            int errorsChangedCount = 0;
            EventHandler<DataErrorsChangedEventArgs> handler = (s, e) => ++errorsChangedCount;
            notifier.ErrorsChanged += handler;

            city.ValidationErrors.Add(new ValidationResult("Foo"));
            Assert.AreEqual<int>(1, errorsChangedCount, "Error count after subscribing");

            notifier.ErrorsChanged -= handler;
            city.ValidationErrors.Add(new ValidationResult("Bar"));
            Assert.AreEqual<int>(1, errorsChangedCount, "Error count after unsubscribing");
        }

        #endregion
#endif

        [TestMethod]
        [Description("ValidationUtilities disambiguates based on parameter types")]
        public void ValidationUtilities_FindsRightMethod()
        {
            MockEntity_Validation entity = new MockEntity_Validation();

            object[] parameters = new object[] { 5 };
            MethodInfo method = ValidationUtilities.GetMethod(entity, "Overload", parameters);
            Assert.AreEqual(typeof(int), method.GetParameters()[0].ParameterType);

            method = ValidationUtilities.GetMethod(entity, "Method", parameters);
            Assert.AreEqual("Method", method.Name);

            // calling overloaded method
            parameters = new object[] { entity };
            ExceptionHelper.ExpectException<AmbiguousMatchException>(delegate
            {
                ValidationUtilities.GetMethod(entity, "Overload2", parameters);
            }, string.Format(Resource.ValidationUtilities_AmbiguousMatch, "Overload2"));

            // calling non existent method
            ExceptionHelper.ExpectException<MissingMethodException>(delegate
            {
                ValidationUtilities.GetMethod(entity, "NonExistentMethod", parameters);
            }, string.Format(Resource.ValidationUtilities_MethodNotFound, "OpenRiaServices.DomainServices.Client.Test.MockEntity_Validation", "NonExistentMethod", parameters.Length, "'OpenRiaServices.DomainServices.Client.Test.MockEntity_Validation'"));

            // calling with empty parameter collection
            parameters = new object[] { };
            ExceptionHelper.ExpectException<MissingMethodException>(delegate
            {
                ValidationUtilities.GetMethod(entity, "Method", parameters);
            }, string.Format(Resource.ValidationUtilities_MethodNotFound_ZeroParams, "OpenRiaServices.DomainServices.Client.Test.MockEntity_Validation", "Method"));

            // calling with null parameter collection
            parameters = null;
            ExceptionHelper.ExpectException<MissingMethodException>(delegate
            {
                ValidationUtilities.GetMethod(entity, "Method", parameters);
            }, string.Format(Resource.ValidationUtilities_MethodNotFound_ZeroParams, "OpenRiaServices.DomainServices.Client.Test.MockEntity_Validation", "Method"));

            // calling with different data types and null in parameter collection, method takes less parameters
            parameters = new object[] { 1, true, "hello", null, entity, new object() };
            ExceptionHelper.ExpectException<MissingMethodException>(delegate
            {
                ValidationUtilities.GetMethod(entity, "Method", parameters);
            }, string.Format(Resource.ValidationUtilities_MethodNotFound, "OpenRiaServices.DomainServices.Client.Test.MockEntity_Validation", "Method", 6, "'System.Int32', 'System.Boolean', 'System.String', null, 'OpenRiaServices.DomainServices.Client.Test.MockEntity_Validation', 'System.Object'"));

            // calling with different data types and null in parameter collection, method takes same number of parameters
            ExceptionHelper.ExpectException<MissingMethodException>(delegate
            {
                ValidationUtilities.GetMethod(entity, "MethodWith6Params", parameters);
            }, string.Format(Resource.ValidationUtilities_MethodNotFound, "OpenRiaServices.DomainServices.Client.Test.MockEntity_Validation", "MethodWith6Params", 6, "'System.Int32', 'System.Boolean', 'System.String', null, 'OpenRiaServices.DomainServices.Client.Test.MockEntity_Validation', 'System.Object'"));

        }

        [TestMethod]
        [Description("Calling ValidateProperty with a null validationContext throws an ArgumentNullException")]
        public void ValidateProperty_Null_ValidationContext_Throws()
        {
            MockEntity entity = new MockEntity();
            ExceptionHelper.ExpectArgumentNullException(() => entity.CallValidateProperty(null, "value"), "validationContext");
        }
    }

    public class MockEntity : Entity
    {
        private string _publicGetterInternalSetter;
        private string _readonlyProp;
        private string _readonlyFalseProp;
        private string _readwriteProp;

        [Editable(false)]
        public string ReadOnlyProperty
        {
            get
            {
                return this._readonlyProp;
            }
            set
            {
                this.ValidateProperty("ReadOnlyProperty", value);
                this._readonlyProp = value;
            }
        }

        [Editable(true)]
        public string ReadOnlyFalseProperty
        {
            get
            {
                return this._readonlyFalseProp;
            }
            set
            {
                this.ValidateProperty("ReadOnlyFalseProperty", value);
                this._readonlyFalseProp = value;
            }
        }

        [Required]
        [StringLength(10)]
        public string ReadWriteProperty
        {
            get
            {
                return this._readwriteProp;
            }
            set
            {
                this.ValidateProperty("ReadWriteProperty", value);
                this._readwriteProp = value;
            }
        }

        public string BogusProperty
        {
            get
            {
                return string.Empty;
            }
            set
            {
                this.ValidateProperty("XXX", value);    // deliberately wrong prop name
            }
        }

        internal string NonPublicProperty
        {
            get
            {
                return string.Empty;
            }
            set
            {
                this.ValidateProperty("NonPublicProperty", value);
            }
        }

        public string PublicGetterInternalSetter
        {
            get
            {
                return this._publicGetterInternalSetter;
            }
            internal set
            {
                this.ValidateProperty("PublicGetterInternalSetter", value);
                this._publicGetterInternalSetter = value;
            }
        }

        public override object GetIdentity()
        {
            return null;
        }

        public void CallValidateProperty(ValidationContext validationContext, object value)
        {
            this.ValidateProperty(validationContext, value);
        }
    }

    public class MockEntity_Validation : MockEntity
    {
        public void Overload(int x)
        {
        }

        public void Overload(bool x)
        {
        }

        public void Overload2(MockEntity e)
        {
        }

        public void Overload2(MockEntity_Validation e)
        {
        }

        public void Method(int x)
        {
        }

        public void MethodWith6Params(int x, int y, string s1, string s2, bool b, object o)
        {
        }
    }
}

namespace Cities
{
    [CustomValidation(typeof(City), "ValidateCity")]
    public partial class City
    {
        /// <summary>
        /// Initializes a new instance of a <see cref="City"/> using the specified
        /// state name, which can be invalid.
        /// </summary>
        /// <param name="stateName">
        /// The state name to use for initialization. This can be
        /// an invalid value, as validation won't be performed.
        /// </param>
        public City(string stateName)
        {
            this._stateName = stateName;
        }

        /// <summary>
        /// Gets or sets whether or not the entity-level validation for ValidateCity should fail.
        /// </summary>
        internal bool ThrowValidationExceptions { get; set; }

        /// <summary>
        /// Gets or sets a callback to be used whenever the ValidateProperty method is invoked.
        /// </summary>
        internal Action<ValidationContext> ValidatePropertyCallback { get; set; }

        /// <summary>
        /// Gets or sets a callback to be used whenever the ValidateCity validation method is invoked.
        /// </summary>
        internal Action<ValidationContext> ValidateCityCallback { get; set; }

        /// <summary>
        /// Gets or sets the count of calls to the ValidateCity method, which is an
        /// entity-level validation methods.
        /// </summary>
        internal int ValidateCityCallCount { get; set; }

        protected override void ValidateProperty(ValidationContext context, object value)
        {
            if (this.ValidatePropertyCallback != null)
            {
                this.ValidatePropertyCallback(context);
            }

            if (this.ThrowValidationExceptions)
            {
                System.ComponentModel.DataAnnotations.Validator.ValidateProperty(value, context);
            }
            else
            {
                base.ValidateProperty(context, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the entity-level custom validation should fail.
        /// </summary>
        public bool MakeEntityValidationFail { get; set; }

        public static ValidationResult ValidateCity(City entity, ValidationContext validationContext)
        {
            if (entity.ValidateCityCallback != null)
            {
                entity.ValidateCityCallback(validationContext);
            }

            // Increment our call counter
            ++entity.ValidateCityCallCount;

            // And if we're supposed to fail, return the failure result
            if (entity.MakeEntityValidationFail)
            {
                return new ValidationResult("MakeEntityValidationFail is true");
            }

            // Otherwise return success
            return ValidationResult.Success;
        }
    }
}
