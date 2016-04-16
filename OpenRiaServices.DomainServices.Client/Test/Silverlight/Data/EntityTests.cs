extern alias SSmDsClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Cities;
using DataTests.AdventureWorks.LTS;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;

namespace OpenRiaServices.DomainServices.Client.Test
{
    using Resource = SSmDsClient::OpenRiaServices.DomainServices.Client.Resource;
    using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

    [TestClass]
    public class EntityTests : UnitTestBase
    {
        /// <summary>
        /// Verifies that Entity.ValidateProperty respects EditableAttribute.AllowInitialValue
        /// for Detached/New entities.
        /// </summary>
        [TestMethod]
        [WorkItem(877780)]
        public void EditableFalse_AllowInitialValue()
        {
            TestDomainServices.TestProvider_Scenarios ctxt = new TestDomainServices.TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            // Detached Entity : Editable(false, AllowInitialValue=true) - verify the property can be set
            TestDomainServices.Entity_TestEditableAttribute e = new TestDomainServices.Entity_TestEditableAttribute();
            Assert.AreEqual(EntityState.Detached, e.EntityState);
            e.EditableFalse_AllowInitialValueTrue = "Foo";
            Assert.AreEqual("Foo", e.EditableFalse_AllowInitialValueTrue);

            // New Entity : Editable(false, AllowInitialValue=true) - verify the property can be set
            e = new TestDomainServices.Entity_TestEditableAttribute();
            e.InitializeNew();
            Assert.AreEqual(EntityState.New, e.EntityState);
            e.EditableFalse_AllowInitialValueTrue = "Foo";
            Assert.AreEqual("Foo", e.EditableFalse_AllowInitialValueTrue);

            // Unmodified Entity : Editable(false, AllowInitialValue=true) - verify the property is read only
            ctxt.Entity_TestEditableAttributes.Attach(e);
            Assert.AreEqual(EntityState.Unmodified, e.EntityState);
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                e.EditableFalse_AllowInitialValueTrue = "Boom";
            }, string.Format(Resource.Property_Is_ReadOnly, "EditableFalse_AllowInitialValueTrue"));

            // Detached Entity : Editable(false, AllowInitialValue=false) - verify the property is read only
            e = new TestDomainServices.Entity_TestEditableAttribute();
            Assert.AreEqual(EntityState.Detached, e.EntityState);
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                e.EditableFalse_AllowInitialValueFalse = "Boom";
            }, string.Format(Resource.Property_Is_ReadOnly, "EditableFalse_AllowInitialValueFalse"));

            // New Entity : Editable(false, AllowInitialValue=false) - verify the property is read only
            e = new TestDomainServices.Entity_TestEditableAttribute();
            e.InitializeNew();
            Assert.AreEqual(EntityState.New, e.EntityState);
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                e.EditableFalse_AllowInitialValueFalse = "Boom";
            }, string.Format(Resource.Property_Is_ReadOnly, "EditableFalse_AllowInitialValueFalse"));
        }

        /// <summary>
        /// Verify that the codegen pattern for GetIdentity
        /// handles default values properly.
        /// </summary>
        [TestMethod]
        public void GetIdentityDefaultKey_Codegen()
        {
            // city has all string (nullable) key members
            City city = new City();

            Assert.IsNull(city.GetIdentity());

            city.StateName = "A";
            Assert.IsNull(city.GetIdentity());

            city.CountyName = "B";
            Assert.IsNull(city.GetIdentity());

            city.Name = "C";
            Assert.IsNotNull(city.GetIdentity());

            // this entity has some nullable and some non-nullable
            // key members
            TestDomainServices.MultipartKeyTestEntity1 entity = new TestDomainServices.MultipartKeyTestEntity1();
            Assert.IsNull(entity.GetIdentity());
            entity.B = "A";
            Assert.IsNull(entity.GetIdentity());
            entity.C = 5;
            Assert.IsNotNull(entity.GetIdentity());  // all nullable members have been set now
            entity.A = 1;
            entity.D = 'x';
            Assert.IsNotNull(entity.GetIdentity());

            // this entity has one nullable and one non-nullable
            // key members
            TestDomainServices.MultipartKeyTestEntity2 entity2 = new TestDomainServices.MultipartKeyTestEntity2();
            Assert.IsNull(entity2.GetIdentity());
            entity2.A = 5;
            Assert.IsNull(entity2.GetIdentity());  // all nullable members have been set now
            entity.B = "B";
            Assert.IsNotNull(entity.GetIdentity());

            // this entity has one nullable and some non-nullable
            // key members
            TestDomainServices.MultipartKeyTestEntity3 entity3 = new TestDomainServices.MultipartKeyTestEntity3();
            Assert.IsNull(entity3.GetIdentity());
            entity3.A = 1;
            Assert.IsNull(entity3.GetIdentity());
            entity3.B = 'B';
            Assert.IsNull(entity3.GetIdentity());
            entity3.C = 2;
            Assert.IsNotNull(entity3.GetIdentity()); // all nullable members have been set now
        }

        /// <summary>
        /// Verify that the base implementation of GetIdentity
        /// handles default values properly.
        /// </summary>
        [TestMethod]
        public void GetIdentityDefaultKey_BaseEntity()
        {
            EntityMultipartKey_DefaultGetIdentity entity = new EntityMultipartKey_DefaultGetIdentity();

            Assert.IsNull(entity.GetIdentity());

            entity.K1 = 1;
            Assert.IsNull(entity.GetIdentity());

            entity.K2 = "A";
            Assert.IsNull(entity.GetIdentity());

            entity.K4 = 2;
            Assert.IsNotNull(entity.GetIdentity()); // all nullable members have been set now

            entity.K3 = 22.5M;
            Assert.IsNotNull(entity.GetIdentity());
        }

        [TestMethod]
        public void TestReadOnlyValidation()
        {
            ReadOnlyMembers entity = new ReadOnlyMembers();

            // verify we can set these properties
            entity.EditableTrue += "a";

            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                entity.EditableFalse += "a";
            }, string.Format(Resource.Property_Is_ReadOnly, "EditableFalse"));
        }

        [TestMethod]
        public void Bug698107_EntityReferenceTransitions()
        {
            CityData cities = new CityData();
            City city = cities.Cities.First();
            County newCounty = cities.Counties.First(p => p != city.County);

            List<string> propChangeNotifications = new List<string>();
            ((INotifyPropertyChanged)city).PropertyChanged += (s, e) =>
            {
                propChangeNotifications.Add(e.PropertyName);

                // we've moved to a design where as the fk members
                // are being synced with the ref change, the object can
                // go through temporarily invalid states
                if (e.PropertyName == "County")
                {
                    if (city.County != null)
                    {
                        Assert.AreEqual(city.CountyName, city.County.Name);
                    }
                }
            };

            city.County = newCounty;

            Assert.AreSame(newCounty, city.County);
            Assert.AreEqual(2, propChangeNotifications.Count(p => p == "County"));
        }

        /// <summary>
        /// Issue was we were setting internal flags after raising the
        /// PropertyChangedNotification when they needed to be set before
        /// the handler was called.
        /// </summary>
        [TestMethod]
        public void Bug659146_EntityRefHasAssignedValue()
        {
            City city = new City();
            County county = new County
            {
                Name = "Lucas"
            };
            city.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "County")
                {
                    // we expect to get the value we set the
                    // reference to here in the handler
                    City tmpCity = (City)s;
                    County tmpCounty = tmpCity.County;
                    if (tmpCounty != null)
                    {
                        Assert.AreSame(county, tmpCounty);
                    }
                }
            };
            city.County = county;
            Assert.AreSame(county, city.County);
        }

        [TestMethod]
        public void EntityRefValidation()
        {
            City invalidCity = new City();
            ValidationContext ctxt = new ValidationContext(invalidCity, null, null) { MemberName = "City" };
            Assert.AreNotEqual(Cities.CityPropertyValidator.IsValidCity(invalidCity, ctxt), ValidationResult.Success);

            Zip zip = new Zip()
            {
                Code = 98053,
                FourDigit = 8625,
                CityName = "Redmond",
                CountyName = "King",
                StateName = "WA"
            };

            INotifyDataErrorInfo notifier = (INotifyDataErrorInfo)zip;

            List<string> actualErrors = new List<string>();

            notifier.ErrorsChanged += (s, e) =>
                {
                    actualErrors.Add(e.PropertyName);
                };

            zip.City = invalidCity;

            // With INotifyDataErrorInfo validation, the property will get set, even though it's invalid
            Assert.AreEqual<City>(invalidCity, zip.City, "The value should have been set");
            Assert.IsTrue(actualErrors.SequenceEqual(new string[] { "City", "CityName", "StateName" }), "Expected errors from the City, CityName, and StateName properties on the Zip entity.");

            // Verify that we have the expected validation error for the property
            IEnumerable<ValidationResult> results = zip.ValidationErrors.Where(e => e.MemberNames.Contains("City"));
            Assert.AreEqual<int>(1, results.Count(), "Expected a single error for the property");

            // set to a valid City - expect success
            City validCity = new City
            {
                Name = "Toledo",
                CountyName = "Lucas",
                StateName = "OH"
            };
            zip.City = validCity;
        }

        [TestMethod]
        public void Entity_ReadOnlyPropertyValidation()
        {
            ConfigurableEntityContainer ec = new ConfigurableEntityContainer();
            ec.CreateSet<City>(EntitySetOperations.Add);
            ec.CreateSet<County>(EntitySetOperations.All);
            EntitySet<City> set = ec.GetEntitySet<City>();

            City city = new City
            {
                Name = "Toledo",
                CountyName = "Lucas",
                StateName = "OH"
            };

            InvalidOperationException expectedException = null;
            try
            {
                city.CalculatedCounty = "x";
            }
            catch (InvalidOperationException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);
            Assert.AreEqual(string.Format(Resource.Property_Is_ReadOnly, "CalculatedCounty"), expectedException.Message);
        }

        /// <summary>
        /// Verify Entity.IsReadOnly behaviors appropriately
        /// </summary>
        [TestMethod]
        public void Entity_IsReadOnly()
        {
            ConfigurableEntityContainer ec = new ConfigurableEntityContainer();
            ec.CreateSet<City>(EntitySetOperations.Add);
            ec.CreateSet<County>(EntitySetOperations.All);
            EntitySet<City> set = ec.GetEntitySet<City>();

            // a new unattached city isn't read only
            City city = new City { Name = "Toledo", CountyName = "Lucas", StateName = "OH" };
            Assert.IsFalse(city.IsReadOnly);

            // when new and attached, the entity is still not read only
            set.Add(city);
            Assert.IsFalse(city.IsReadOnly);

            // if the entity is being submitted, the entity should be readonly
            city.IsSubmitting = true;
            Assert.IsTrue(city.IsReadOnly);
            city.IsSubmitting = false;
            Assert.IsFalse(city.IsReadOnly);

            // if a custom method is being invoked, the entity should be readonly
            city.AutoAssignCityZone();
            Assert.IsTrue(city.IsReadOnly);
            city.UndoAction(city.EntityActions.First());
            Assert.IsFalse(city.IsReadOnly);

            // if a custom method is being invoked but validation errors are present, the entity should not be readonly
            city.AutoAssignCityZone();
            city.ValidationResultCollection.ReplaceErrors(new ValidationResult[] { new ValidationResult(string.Empty) });
            Assert.IsFalse(city.IsReadOnly);
            city.UndoAction(city.EntityActions.First());
            city.ValidationErrors.Clear();
            Assert.IsFalse(city.IsReadOnly);

            // if a custom method is being invoked but errors are present, the entity should not be readonly
            city.AutoAssignCityZone();
            city.ValidationResultCollection.ReplaceErrors(new ValidationResult[] { new ValidationResult(string.Empty) });
            Assert.IsFalse(city.IsReadOnly);
            city.UndoAction(city.EntityActions.First());
            city.ValidationErrors.Clear();
            Assert.IsFalse(city.IsReadOnly);

            // if a custom method is being invoked but conflicts are present, the entity should not be readonly
            city.AutoAssignCityZone();
            city.EntityConflict = new EntityConflict(city, null, null, true);
            Assert.IsFalse(city.IsReadOnly);
            city.UndoAction(city.EntityActions.First());
            city.EntityConflict = null;
            Assert.IsFalse(city.IsReadOnly);

            // after attaching as unmodified, the entity is readonly
            // since the set was configured as readonly
            set.Clear();
            set.Attach(city);
            Assert.IsTrue(city.IsReadOnly);
        }

        [TestMethod]
        [WorkItem(856845)]
        [TestDescription("EndEdit should perform required property validation")]
        public void Entity_EndEditValidatesRequiredProperties()
        {
            // Start with an entity that doesn't have its required properties
            // satisfied (Name is required)
            Cities.City city = new Cities.City();
            IEditableObject editableCity = (IEditableObject)city;

            RequiredAttribute template = new RequiredAttribute();
            string expectedMemberName = "CityName";
            string expectedError = template.FormatErrorMessage(expectedMemberName);

            // Begin the edit transaction
            editableCity.BeginEdit();

            string expectedMember = "Name";

            // End the edit transaction, which performs property and entity-level validation
            editableCity.EndEdit();
            Assert.AreEqual<int>(1, city.ValidationErrors.Count, "After EndEdit");
            Assert.AreEqual<int>(1, city.ValidationErrors.Single().MemberNames.Count(), "MemberNames count after EndEdit");
            Assert.AreEqual<string>(expectedMember, city.ValidationErrors.Single().MemberNames.Single(), "Member name after EndEdit");
            Assert.AreEqual<string>(expectedError, city.ValidationErrors.Single().ErrorMessage, "ErrorMessage after EndEdit");
        }

        [TestMethod]
        [WorkItem(856845)]
        [TestDescription("EndEdit should perform property value validation")]
        public void Entity_EndEditValidatesPropertyValues()
        {
            // Start with an entity that has required properties satisfied, but has
            // an invalid property value (for StateName)
            Cities.City city = new Cities.City("This is an invalid state name") { Name = "Redmond" };
            IEditableObject editableCity = (IEditableObject)city;

            StringLengthAttribute template = new StringLengthAttribute(2);
            string expectedMember = "StateName";
            string expectedError = template.FormatErrorMessage(expectedMember);

            // Begin the edit transaction
            editableCity.BeginEdit();

            // End the edit transaction, which performs property and entity-level validation
            editableCity.EndEdit();
            Assert.AreEqual<int>(1, city.ValidationErrors.Count, "After EndEdit");
            Assert.AreEqual<int>(1, city.ValidationErrors.Single().MemberNames.Count(), "MemberNames count after EndEdit");
            Assert.AreEqual<string>(expectedMember, city.ValidationErrors.Single().MemberNames.Single(), "Member name after EndEdit");
            Assert.AreEqual<string>(expectedError, city.ValidationErrors.Single().ErrorMessage, "ErrorMessage after EndEdit");
        }

        [TestMethod]
        [WorkItem(852215)]
        [TestDescription("for an invalid entity during EndEdit we commit the edit, populate the errors, and raise events")]
        public void Entity_EndEditValidatesEntity()
        {
            TestEntity invalidEntity = new TestEntity();
            ((IEditableObject)invalidEntity).BeginEdit();
            invalidEntity.ID1 = "1";
            invalidEntity.ID2 = "1";

            string expectedError = "TestEntity is not valid.";
            INotifyDataErrorInfo notifier = (INotifyDataErrorInfo)invalidEntity;

            // Track what errors existed for the property name at the time of each event
            var actualErrors = new List<Tuple<string, IEnumerable<ValidationResult>>>();
            notifier.ErrorsChanged += (s, e) =>
                {
                    actualErrors.Add(Tuple.Create(e.PropertyName, notifier.GetErrors(e.PropertyName).Cast<ValidationResult>()));
                };

            ((IEditableObject)invalidEntity).EndEdit();

            Assert.AreEqual<int>(1, actualErrors.Count, "There should have been a single ErrorsChanged event");

            Tuple<string, IEnumerable<ValidationResult>> error = actualErrors[0];
            Assert.AreEqual<string>(null, error.Item1, "The error should have been an entity-level error (null PropertyName)");

            Assert.AreEqual<int>(1, error.Item2.Count(), "There should have been a single error at the time of the event");
            Assert.AreEqual<string>(expectedError, error.Item2.First().ErrorMessage, "The error message of the single error didn't match the expectation");

            // Clear out the errors for the next stage of the test
            actualErrors.Clear();

            TestEntity validEntity = new TestEntity();
            ((IEditableObject)validEntity).BeginEdit();
            validEntity.ID1 = "1";
            validEntity.ID2 = "2";
            ((IEditableObject)validEntity).EndEdit();
            Assert.AreEqual("1", validEntity.ID1);
            Assert.AreEqual("2", validEntity.ID2);

            Assert.AreEqual<int>(0, actualErrors.Count, "There should not have been any errors during the valid EndEdit()");
        }

        [TestMethod]
        public void Entity_GetOriginal()
        {
            TestCityContainer ec = new TestCityContainer();
            var el = ec.GetEntitySet<City>();
            City city = new City
            {
                Name = "Perrysburg",
                CountyName = "Wood",
                StateName = "OH",
                ZoneName = "Foo"
            };
            el.Attach(city);

            // There should not be any original state right now, so this should return null.
            var original = (City)city.GetOriginal();
            Assert.IsNull(original);

            city.ZoneName = "Bar"; // Make a change, causing entity to create original state.
            original = (City)city.GetOriginal();
            Assert.AreEqual("Foo", original.ZoneName);
        }

        [TestMethod]
        public void Entity_ToString()
        {
            PurchaseOrder order = new PurchaseOrder
            {
                PurchaseOrderID = 1234
            };
            string s = order.ToString();
            Assert.AreEqual("PurchaseOrder : 1234", s);

            PurchaseOrderDetail detail = new PurchaseOrderDetail
            {
                PurchaseOrderID = 1234,
                PurchaseOrderDetailID = 5678
            };
            s = detail.ToString();
            Assert.AreEqual("PurchaseOrderDetail : {5678,1234}", s);

            TestEntity test = new TestEntity();
            s = test.ToString();
            Assert.AreEqual("TestEntity : {null,null}", s);
        }

        /// <summary>
        /// Test the EntityKey APIs directly
        /// </summary>
        [TestMethod]
        public void EntityKey_Creation()
        {
            // Test one of the generic Create overloads (that doesn't box
            // the key values)
            object key = EntityKey.Create(5, 2.34M, "test", "hello world");
            string formattedKey = key.ToString();

            object[] keyValues = new object[] { 5, 2.34M, "test", "hello world" };
            string expectedKey = "{" + string.Join(",", keyValues) + "}";
            Assert.AreEqual(expectedKey, formattedKey);

            // pass the same set into the params version and verify the keys are equal
            object key2 = EntityKey.Create(keyValues);
            Assert.AreSame(key.GetType(), key2.GetType());
            Assert.AreEqual(key, key2);
            Assert.AreEqual(key.GetHashCode(), key2.GetHashCode());

            // Test boxing version for N key values
            key = EntityKey.Create(5, "hello", 3, 4, 5, "test", 2132, '?');
            formattedKey = key.ToString();
            Assert.AreEqual("{5,hello,3,4,5,test,2132,?}", formattedKey);

            // pass the same set of values into the params version and verify the keys are equal
            key2 = EntityKey.Create(new object[] { 5, "hello", 3, 4, 5, "test", 2132, '?' });
            Assert.AreEqual(key, key2);
            Assert.AreEqual(key.GetHashCode(), key2.GetHashCode());
        }

        [TestMethod]
        public void EntityKey_NullValues()
        {
            string expectedMsg = new ArgumentNullException("value", Resource.EntityKey_CannotBeNull).Message;
            ArgumentNullException expectedException = null;
            try
            {
                EntityKey.Create(new object[] { 5, null, "test" });
            }
            catch (ArgumentNullException e)
            {
                expectedException = e;
            }
            Assert.AreEqual(expectedMsg, expectedException.Message);
            expectedException = null;

            try
            {
                EntityKey.Create<int, string>(5, null);
            }
            catch (ArgumentNullException e)
            {
                expectedException = e;
            }
            Assert.AreEqual(expectedMsg, expectedException.Message);
            expectedException = null;
        }

        /// <summary>
        /// Verify that for a multipart key, the key created returns
        /// the same hash code that would result from ORing the values
        /// together
        /// </summary>
        [TestMethod]
        public void EntityKey_TestHashCodeValues()
        {
            Guid g = Guid.NewGuid();
            object[] keyValues = new object[] { 123, 34.5M, "hello", new DateTime(234234), g, false, '?' };
            object key = EntityKey.Create(keyValues);
            int hashCode = key.GetHashCode();
            int expectedHashCode = 0;
            foreach (object keyValue in keyValues)
            {
                expectedHashCode ^= keyValue.GetHashCode();
            }
            Assert.AreEqual(expectedHashCode, hashCode);

            // test the non-boxed version and verify we get the same hashcode
            key = EntityKey.Create(123, 34.5M, "hello", new DateTime(234234), g, false, '?');
            hashCode = key.GetHashCode();
            Assert.AreEqual(expectedHashCode, hashCode);

            // compute directly without boxing and verify equal
            int directlyComputed = ((int)123).GetHashCode() ^ ((decimal)34.5M).GetHashCode() ^ "hello".GetHashCode() ^
                new DateTime(234234).GetHashCode() ^ g.GetHashCode() ^ false.GetHashCode() ^ '?'.GetHashCode();
            Assert.AreEqual(directlyComputed, key.GetHashCode());
        }

        /// <summary>
        /// Test codegenerated GetIdentity methods which calls EntityKey.Create
        /// </summary>
        [TestMethod]
        public void Entity_GetIdentityGenerated()
        {
            // Test an entity having 3 key members
            City city = new City
            {
                Name = "Perrysburg",
                CountyName = "Wood",
                StateName = "OH"
            };

            object key = city.GetIdentity();
            Assert.AreNotEqual(city.GetHashCode(), key.GetHashCode());

            // create a duplicate city and verify it hashes to the same value
            // and that the values are equal
            City city2 = new City
            {
                Name = "Perrysburg",
                CountyName = "Wood",
                StateName = "OH"
            };
            object key2 = city2.GetIdentity();
            Assert.AreEqual(key, key2);
            Assert.AreEqual(key.GetHashCode(), key2.GetHashCode());
            Assert.IsTrue(key.Equals(key2));

            // verify that for an entity with a single key member
            // the value itself is returned
            Product product = new Product
            {
                ProductID = 345
            };
            key = product.GetIdentity();
            Assert.AreEqual(product.ProductID, key);
        }

        [TestMethod]
        public void Entity_GetIdentityDefault()
        {
            EntityNoKeyMembers e0 = new EntityNoKeyMembers();
            ExceptionHelper.ExpectArgumentException(() => { e0.GetIdentity(); }, Resource.EntityKey_EmptyKeyMembers, "keyValues");

            Entity1KeyMember e1 = new Entity1KeyMember()
            {
                K1 = 5
            };

            Entity1KeyMember ee1 = new Entity1KeyMember()
            {
                K1 = 5
            };

            EnsureEntityKeysEqual(e1, ee1);
            ee1.K1 = 7;
            EnsureEntityKeysNotEqual(e1, ee1);

            Entity2KeyMembers e2 = new Entity2KeyMembers(e1, "A");
            Entity2KeyMembers ee2 = new Entity2KeyMembers(e1, "A");
            EnsureEntityKeysEqual(e2, ee2);
            ee2.K2 = "B";
            EnsureEntityKeysNotEqual(e2, ee2);

            Entity3KeyMembers e3 = new Entity3KeyMembers(e2, 3.1m);
            Entity3KeyMembers ee3 = new Entity3KeyMembers(e2, 3.1m);
            EnsureEntityKeysEqual(e3, ee3);
            ee3.K3 = 4.2m;
            EnsureEntityKeysNotEqual(e3, ee3);

            DateTime someDate = new DateTime(2009, 1, 1);
            Entity4KeyMembers e4 = new Entity4KeyMembers(e3, someDate);
            Entity4KeyMembers ee4 = new Entity4KeyMembers(e3, someDate);
            EnsureEntityKeysEqual(e4, ee4);
            ee4.K4 = new DateTime(2008, 1, 1);
            EnsureEntityKeysNotEqual(e4, ee4);

            Entity5KeyMembers e5 = new Entity5KeyMembers(e4, TimeSpan.FromHours(2));
            Entity5KeyMembers ee5 = new Entity5KeyMembers(e4, TimeSpan.FromHours(2));
            EnsureEntityKeysEqual(e5, ee5);
            ee5.K5 = TimeSpan.FromHours(3);
            EnsureEntityKeysNotEqual(e5, ee5);

            Guid someGuid = Guid.NewGuid();
            Entity4KeyMembers e6 = new Entity6KeyMembers(e5, someGuid);
            Entity6KeyMembers ee6 = new Entity6KeyMembers(e5, someGuid);
            EnsureEntityKeysEqual(e6, ee6);
            ee6.K6 = Guid.NewGuid();
            EnsureEntityKeysNotEqual(e6, ee6);
        }

        private void EnsureEntityKeysEqual(Entity e1, Entity e2)
        {
            object k1 = e1.GetIdentity();
            object k2 = e2.GetIdentity();
            Assert.AreEqual(k1, k2);
            Assert.AreEqual(k1.GetHashCode(), k2.GetHashCode());
        }

        private void EnsureEntityKeysNotEqual(Entity e1, Entity e2)
        {
            object k1 = e1.GetIdentity();
            object k2 = e2.GetIdentity();
            Assert.AreNotEqual(k1, k2);
        }

        /// <summary>
        /// Test AcceptChanges.
        /// </summary>
        [TestMethod]
        public void Entity_AcceptChanges()
        {
            TestCityContainer ec = new TestCityContainer();
            var el = ec.GetEntitySet<City>();

            string name = "Perrysburg";
            string countyName = "Wood";
            string stateName = "OH";
            object[] key = new object[] { countyName, name, stateName };

            City city = new City
            {
                Name = name,
                CountyName = countyName,
                StateName = stateName
            };

            el.Attach(city);
            Assert.AreEqual(1, el.Count);

            var cityInCache = el.GetEntityByKey(key);
            Assert.AreEqual(city, cityInCache);

            // Simulate a delete.
            el.Remove(city);
            Assert.AreEqual(0, el.Count);
            Assert.AreEqual(EntityState.Deleted, city.EntityState);

            // Verify that it's still in the cache.
            cityInCache = el.GetEntityByKey(key);
            Assert.AreEqual(city, cityInCache);

            // Clear out part of the PK. (This could happen if someone removes an entity via an EntityCollection. See bug 619454.)
            city.CountyName = "";

            ((IChangeTracking)city).AcceptChanges();
            Assert.AreEqual(EntityState.Detached, city.EntityState);

            // Verify it's removed from the cache now.
            cityInCache = el.GetEntityByKey(key);
            Assert.IsNull(cityInCache);
        }

        /// <summary>
        /// Test change notifications
        /// </summary>
        [TestMethod]
        public void Entity_PropertyChanged()
        {
            TestCityContainer ec = new TestCityContainer();
            var el = ec.GetEntitySet<City>();
            City city = new City
            {
                Name = "Perrysburg",
                CountyName = "Wood",
                StateName = "OH"
            };
            el.Attach(city);

            int hasChangesChangeCount = 0;
            int entityStateChangeCount = 0;
            int validationErrorsChangeCount = 0;
            int totalPropertyChangeNotificationCount = 0;
            city.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
            {
                totalPropertyChangeNotificationCount++;

                if (e.PropertyName == "HasChanges")
                {
                    hasChangesChangeCount++;
                }
                else if (e.PropertyName == "EntityState")
                {
                    entityStateChangeCount++;
                }
                else if (e.PropertyName == "ValidationErrors")
                {
                    validationErrorsChangeCount++;
                }
            };
            Assert.IsFalse(city.HasChanges);
            Assert.IsFalse(((IChangeTracking)city).IsChanged);
            city.StateName = "WA";
            Assert.IsTrue(city.HasChanges);
            Assert.IsTrue(((IChangeTracking)city).IsChanged);
            ((IChangeTracking)city).AcceptChanges();
            Assert.IsFalse(city.HasChanges);
            Assert.IsFalse(((IChangeTracking)city).IsChanged);
            city.AssignCityZone("Foo");
            Assert.IsTrue(city.HasChanges);
            Assert.IsTrue(((IChangeTracking)city).IsChanged);
            ((IChangeTracking)city).AcceptChanges();
            el.Remove(city);
            Assert.AreEqual(4, hasChangesChangeCount);
            Assert.AreEqual(5, entityStateChangeCount);

            Assert.AreEqual(0, validationErrorsChangeCount);
            city.ValidationResultCollection.ReplaceErrors(new ValidationResult[] { new ValidationResult(string.Empty) });
            Assert.AreEqual(1, validationErrorsChangeCount);
            city.ValidationErrors.Clear();
            Assert.AreEqual(2, validationErrorsChangeCount);

            Assert.AreEqual(23, totalPropertyChangeNotificationCount);
        }

        [TestMethod]
        public void Entity_BindableProperties()
        {
            Type entityType = typeof(Entity);

            // Examine all public properties
            foreach (var property in entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                // Verify property has a display attribute
                var displayAttributes = property.GetCustomAttributes(typeof(DisplayAttribute), true).Cast<DisplayAttribute>();
                Assert.AreEqual(1, displayAttributes.Count(), string.Format("Expected 1 DisplayAttribute on property '{0}'.", property.Name));

                // Verify display attribute has AutoGenerateField==false
                var displayAttribute = displayAttributes.Single();
                Assert.IsFalse(displayAttribute.AutoGenerateField, string.Format("Expected [DisplayAttribute(AutoGenerateField=false)] on property '{0}'.", property.Name));
            }
        }

        [TestMethod]
        public void Entity_RaiseDataMemberChanged()
        {
            MockEntity_RaisePropertyChangedEvents entity = new MockEntity_RaisePropertyChangedEvents();
            entity.StartTracking();

            ConfigurableEntityContainer container = new ConfigurableEntityContainer();
            container.CreateSet<MockEntity_RaisePropertyChangedEvents>(EntitySetOperations.All);
            container.GetEntitySet<MockEntity_RaisePropertyChangedEvents>().Add(entity);

            List<string> propertyChanges = new List<string>();
            entity.PropertyChanged += (s, a) => propertyChanges.Add(a.PropertyName);

            // Real property, should have no errors
            entity.Mock_RaiseDataMemberChanging("Property1");
            entity.Mock_RaiseDataMemberChanged("Property1");
            entity.Mock_RaiseDataMemberChanged("Property1"); // Called twice to ensure we go through the Entity::PropertyHasChanged code path

            Assert.AreEqual(2, propertyChanges.Count(p => p == "Property1"));
        }

        [TestMethod]
        public void Entity_RaiseDataMemberChanged_NonExistentProperty()
        {
            MockEntity_RaisePropertyChangedEvents entity = new MockEntity_RaisePropertyChangedEvents();
            entity.StartTracking();

            ConfigurableEntityContainer container = new ConfigurableEntityContainer();
            container.CreateSet<MockEntity_RaisePropertyChangedEvents>(EntitySetOperations.All);
            container.GetEntitySet<MockEntity_RaisePropertyChangedEvents>().Add(entity);

            List<string> propertyChanges = new List<string>();
            entity.PropertyChanged += (s, a) => propertyChanges.Add(a.PropertyName);

            // Non-existent property - we don't do any validation internally
            // for this. The methods are protected, so we don't attempt to protect
            // the user from lying to themselves.
            entity.Mock_RaiseDataMemberChanging("Property_DOES_NOT_EXIST");
            entity.Mock_RaiseDataMemberChanged("Property_DOES_NOT_EXIST");
            entity.Mock_RaiseDataMemberChanged("Property_DOES_NOT_EXIST");

            Assert.AreEqual(2, propertyChanges.Count(p => p == "Property_DOES_NOT_EXIST"));
        }

        [TestMethod]
        public void Entity_RaiseDataMemberChanged_NonChangeTrackedProperty()
        {
            MockEntity_RaisePropertyChangedEvents entity = new MockEntity_RaisePropertyChangedEvents();
            entity.StartTracking();

            ConfigurableEntityContainer container = new ConfigurableEntityContainer();
            container.CreateSet<MockEntity_RaisePropertyChangedEvents>(EntitySetOperations.All);
            container.GetEntitySet<MockEntity_RaisePropertyChangedEvents>().Add(entity);

            List<string> propertyChanges = new List<string>();
            entity.PropertyChanged += (s, a) => propertyChanges.Add(a.PropertyName);

            // Non-existent property - we don't do any validation internally
            // for this. The methods are protected, so we don't attempt to protect
            // the user from lying to themselves.
            entity.Mock_RaiseDataMemberChanging("CalculatedProperty1");
            entity.Mock_RaiseDataMemberChanged("CalculatedProperty1");
            entity.Mock_RaiseDataMemberChanged("CalculatedProperty1");

            Assert.AreEqual(2, propertyChanges.Count(p => p == "CalculatedProperty1"));
        }

        [TestMethod]
        [Asynchronous]
        [TestDescription("Verifies that entity child and parent relationships are restored after RejectChanges is called.")]
        [WorkItem(720495)]
        public void Entity_RejectChanges_ParentAssociationRestored()
        {
            List<Employee> employeeList = new List<Employee>();
            ConfigurableEntityContainer container = new ConfigurableEntityContainer();
            container.CreateSet<Employee>(EntitySetOperations.All);
            ConfigurableDomainContext catalog = new ConfigurableDomainContext(new WebDomainClient<TestDomainServices.LTS.Catalog.ICatalogContract>(TestURIs.LTS_Catalog), container);

            var load = catalog.Load(catalog.GetEntityQuery<Employee>("GetEmployees"), throwOnError:false);
            this.EnqueueConditional(() => load.IsComplete);
            this.EnqueueCallback(() =>
            {
                Assert.AreEqual(null, load.Error);

                Employee parent, child;
                parent = container.GetEntitySet<Employee>().OrderByDescending(e => e.Reports.Count).First();

                while (parent != null)
                {
                    // Track parent, get a report from it
                    employeeList.Add(parent);
                    child = parent.Reports.OrderByDescending(e => e.Reports.Count).FirstOrDefault();

                    // Track child
                    if (child == null)
                    {
                        break;
                    }

                    // Remove child and continue
                    parent.Reports.Remove(child);
                    parent = child;
                }

                // By rejecting changes, our parent<=>child relationships should be restored.
                catalog.RejectChanges();

                // Unwind, walking up management chain
                foreach (Employee employee in employeeList.Reverse<Employee>())
                {
                    Assert.AreSame(parent, employee, "Expected parent relationship to be restored.");
                    parent = employee.Manager;
                    Assert.IsTrue(parent.Reports.Contains(employee), "Expected child relationship to be restored.");
                }
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        public void Entity_SkipIndexers()
        {
            MockEntity_Indexer entity = new MockEntity_Indexer { Data = "Foo", Key = 10 };
            IDictionary<string, object> stateInfo = entity.ExtractState();
            Assert.IsTrue(stateInfo.Count == 2, "Expected only 2 properties to be returned");
            Assert.IsTrue(stateInfo["Data"] != null && stateInfo["Key"] != null, "The state returned should only contain the Data and Key properties");
        }        

        private class TestCityContainer : EntityContainer
        {
            public TestCityContainer()
            {
                CreateEntitySet<City>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
                CreateEntitySet<County>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
            }
        }
    }

    public class ConfigurableDomainContext : DomainContext
    {
        private EntityContainer _entityContainer;

        public ConfigurableDomainContext(DomainClient client, EntityContainer entityContainer)
            : base(client)
        {
            this._entityContainer = entityContainer;
        }

        public EntityQuery<TEntity> GetEntityQuery<TEntity>(string queryName) where TEntity : Entity
        {
            return this.CreateQuery<TEntity>(queryName, null, false, false);
        }

        protected override EntityContainer CreateEntityContainer()
        {
            return this._entityContainer;
        }
    }

    public class ConfigurableEntityContainer : EntityContainer
    {
        public void CreateSet<TEntity>(EntitySetOperations operations) where TEntity : Entity, new()
        {
            base.CreateEntitySet<TEntity>(operations);
        }
    }

    public class ReadOnlyMembers : Entity
    {
        public string _value;

        [Key]
        [Editable(false)]
        public string EditableFalse
        {
            get
            {
                return this._value;
            }
            set
            {
                if ((this._value != value))
                {
                    this.ValidateProperty("EditableFalse", value);
                    this.RaiseDataMemberChanging("EditableFalse");
                    this._value = value;
                    this.RaiseDataMemberChanged("EditableFalse");
                }
            }
        }

        [Editable(true)]
        public string EditableTrue
        {
            get
            {
                return this._value;
            }
            set
            {
                if ((this._value != value))
                {
                    this.ValidateProperty("EditableTrue", value);
                    this.RaiseDataMemberChanging("EditableTrue");
                    this._value = value;
                    this.RaiseDataMemberChanged("EditableTrue");
                }
            }
        }
    }

    /// <summary>
    /// Test entity with multiple nullable key members and entity-level validation
    /// </summary>
    [CustomValidation(typeof(TestEntity), "Validate")]
    public class TestEntity : Entity
    {
        private string _id1;
        private string _id2;

        [Key]
        public string ID1
        {
            get
            {
                return this._id1;
            }
            set
            {
                if ((this._id1 != value))
                {
                    this.ValidateProperty("ID1", value);
                    this.RaiseDataMemberChanging("ID1");
                    this._id1 = value;
                    this.RaiseDataMemberChanged("ID1");
                }
            }
        }

        [Key]
        public string ID2
        {
            get
            {
                return this._id2;
            }
            set
            {
                if ((this._id2 != value))
                {
                    this.ValidateProperty("ID2", value);
                    this.RaiseDataMemberChanging("ID2");
                    this._id2 = value;
                    this.RaiseDataMemberChanged("ID2");
                }
            }
        }

        public override object GetIdentity()
        {
            return EntityKey.Create(ID1, ID2);
        }

        public static ValidationResult Validate(TestEntity entity)
        {
            if (entity.ID1 != entity.ID2)
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult("TestEntity is not valid.");
            }
        }
    }

    public class EntityNoKeyMembers : Entity
    {
    }

    public class EntityMultipartKey_DefaultGetIdentity : Entity
    {
        [Key]
        public int K1 { get; set; }
        [Key]
        public string K2 { get; set; }
        [Key]
        public decimal K3 { get; set; }
        [Key]
        public int? K4 { get; set; }
    }

    public class Entity1KeyMember : EntityNoKeyMembers
    {
        public Entity1KeyMember()
            : base()
        {
        }

        public Entity1KeyMember(Entity1KeyMember e1)
        {
            this.K1 = e1.K1;
        }

        [Key]
        public int K1 { get; set; }
    }

    public class Entity2KeyMembers : Entity1KeyMember
    {
        public Entity2KeyMembers(Entity1KeyMember e1, string k2)
            : base(e1)
        {
            this.K2 = string.Copy(k2);
        }

        [Key]
        public string K2 { get; set; }
    }

    public class Entity3KeyMembers : Entity2KeyMembers
    {
        public Entity3KeyMembers(Entity2KeyMembers e2, decimal k3)
            : base(e2, e2.K2)
        {
            this.K3 = k3;
        }

        [Key]
        public decimal K3 { get; set; }
    }

    public class Entity4KeyMembers : Entity3KeyMembers
    {
        public Entity4KeyMembers(Entity3KeyMembers e3, DateTime k4)
            : base(e3, e3.K3)
        {
            this.K4 = k4;
        }

        [Key]
        public DateTime K4 { get; set; }
    }

    public class Entity5KeyMembers : Entity4KeyMembers
    {
        public Entity5KeyMembers(Entity4KeyMembers e4, TimeSpan k5)
            : base(e4, e4.K4)
        {
            this.K5 = k5;
        }

        [Key]
        public TimeSpan K5 { get; set; }
    }

    public class Entity6KeyMembers : Entity5KeyMembers
    {
        public Entity6KeyMembers(Entity5KeyMembers e5, Guid k6)
            : base(e5, e5.K5)
        {
            this.K6 = k6;
        }

        [Key]
        public Guid K6 { get; set; }
    }

    public class MockEntity_RaisePropertyChangedEvents : Entity
    {
        [Key]
        public int Property1 { get; set; }
        public int CalculatedProperty1 { get { return this.Property1 + 1; } }

        public void Mock_RaiseDataMemberChanged(string propertyName) { base.RaiseDataMemberChanged(propertyName); }
        public void Mock_RaiseDataMemberChanging(string propertyName) { base.RaiseDataMemberChanging(propertyName); }
        public void Mock_RaisePropertyChanged(string propertyName) { base.RaisePropertyChanged(propertyName); }
    }

    public class MockEntity_Indexer : Entity
    {
        public string Data { get; set; }
        [Key]
        public int Key { get; set; }
        public int this[int indexer]
        {
            get
            {
                return 0;
            }
            set
            {
            }
        }
    }
}
