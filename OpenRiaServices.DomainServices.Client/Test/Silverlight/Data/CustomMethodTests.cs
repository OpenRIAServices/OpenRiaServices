extern alias SSmDsClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Cities;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using TestDomainServices;
using TestDomainServices.NamedUpdates;

namespace OpenRiaServices.DomainServices.Client.Test
{
    using Resource = SSmDsClient::OpenRiaServices.DomainServices.Client.Resource;

    [TestClass]
    public class CustomMethodTests : UnitTestBase
    {
        #region Common entity actions used for testing
        // entity actions that don't exist on CityDomainContext
        private EntityAction _approve = new EntityAction("Approve");
        private EntityAction _reject = new EntityAction("Reject", "order not correctly filled out");

        // entity actions that actually exist on CityDomainContext
        private EntityAction _assignCityZone = new EntityAction("AssignCityZone", "Zone1");
        private EntityAction _autoAssignCityZone = new EntityAction("AutoAssignCityZone");
        private EntityAction _reassignZip = new EntityAction("ReassignZip", 100, true);
        #endregion

        private List<City> _cities;
        private List<CityWithInfo> _citiesWithInfo;
        private IList<City> _rootCities;

        [TestInitialize]
        public void TestInitialize()
        {
            CityData cityData = new CityData();
            _cities = cityData.Cities;
            _citiesWithInfo = cityData.CitiesWithInfo;

            // Root cities eliminates any derived types
            _rootCities = _cities.Where(c => c.GetType() == typeof(City)).ToList();
        }

        #region API tests
        [TestMethod]
        [Description("Verify internal implementation of Invoke and CanInvoke on loaded entites")]
        public void Entity_InvokeOnLoaded()
        {
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_cities);
            Assert.AreEqual(EntityState.Unmodified, _cities[0].EntityState);
            Assert.AreEqual(0, _cities[0].EntityActions.Count());
            Assert.IsTrue(_cities[0].CanInvokeAction(_assignCityZone.Name));

            // verify invoke on a new entity succeeds and subsequent CanInvoke returns false
            _cities[0].InvokeAction(_assignCityZone.Name, _assignCityZone.Parameters.ToArray<object>());
            Assert.AreEqual(1, _cities[0].EntityActions.Count());
            Assert.AreEqual(EntityState.Modified, _cities[0].EntityState);
            Assert.AreEqual<string>(_assignCityZone.Name, _cities[0].EntityActions.Single().Name);
            Assert.AreEqual<int>(_assignCityZone.Parameters.Count(), _cities[0].EntityActions.Single().Parameters.Count());
            Assert.IsFalse(_cities[0].CanInvokeAction(_assignCityZone.Name));
        }

        [TestMethod]
        [Description("Verify that after invoking a domain method, the entity is read-only.")]
        public void Entity_ReadOnly()
        {
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_cities);

            Assert.IsFalse(_cities[0].IsReadOnly);
            _cities[0].AssignCityZone(_assignCityZone.Name);
            Assert.IsTrue(_cities[0].IsReadOnly);

            Assert.IsFalse(_cities[1].IsReadOnly);
            _cities[1].CountyName += "X";
            _cities[1].AssignCityZone(_assignCityZone.Name);
            Assert.IsTrue(_cities[1].IsReadOnly);

            ((System.ComponentModel.IRevertibleChangeTracking)_cities[1]).RejectChanges();

            Assert.IsFalse(_cities[1].IsReadOnly);
        }

        [TestMethod]
        [Description("Verify calling Entity domain method APIs")]
        public void Entity_EntityAction_InvokeOnLoaded()
        {
            // verify initial state: empty invocations and one can invoke any methods
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_cities);
            var invocableCity = _cities[1];
            Assert.IsNotNull(invocableCity);
            Assert.IsTrue(invocableCity.CanInvokeAction(_assignCityZone.Name));

            Assert.AreEqual(0, invocableCity.EntityActions.Count());

            // verify invoke on a new entity succeeds and subsequent CanInvoke for a different entity action returns false
            invocableCity.InvokeAction(_assignCityZone.Name, _assignCityZone.Parameters.ToArray());
            Assert.IsFalse(invocableCity.CanInvokeAction(_assignCityZone.Name)); // different entity action name
            Assert.AreSame(_assignCityZone.Name, _cities[1].EntityActions.Single().Name);

            // verify you get back the same entity action on GetInvocations
            Assert.AreEqual<string>(_assignCityZone.Name, invocableCity.EntityActions.Single().Name);
        }

        [TestMethod]
        [Description("Verify entity action API")]
        public void EntityAction()
        {
            EntityAction action = new EntityAction("MyAction1");
            Assert.AreEqual("MyAction1", action.Name);
            Assert.IsNotNull(action.Parameters);
            Assert.AreEqual(0, action.Parameters.Count());

            action = new EntityAction("MyAction2", "Param1", new string[] { "Param2_1", "Param2_2" });
            Assert.AreEqual("MyAction2", action.Name);
            Assert.AreEqual(2, action.Parameters.Count());
            Assert.AreEqual("Param1", action.Parameters.ElementAt(0) as string);
            string[] param2 = action.Parameters.ElementAt(1) as string[];
            Assert.AreEqual(2, param2.Length);
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify HasErrors")]
        public void HasErrors()
        {
            List<ValidationResult> listWithOneError = new List<ValidationResult>();
            listWithOneError.Add(new ValidationResult("error1", new string[] { "member" }));

            List<ValidationResult> listWithTwoErrors = new List<ValidationResult>();
            listWithTwoErrors.Add(new ValidationResult("error2", new string[] { "member" }));
            listWithTwoErrors.Add(new ValidationResult("error3", new string[] { "member" }));

            // subscribe to PropertyChange event
            List<string> propChanged = new List<string>();
            Assert.IsFalse(_cities[0].HasValidationErrors);
            Assert.IsNotNull(_cities[0].ValidationErrors);
            _cities[0].PropertyChanged += delegate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (e.PropertyName != "ValidationErrors")
                {
                    propChanged.Add(e.PropertyName);
                }
            };

            // set Errors to list with one error, verify HasErrors is updated and change notification is raised
            _cities[0].ValidationResultCollection.ReplaceErrors(listWithOneError);
            EnqueueConditional(() => propChanged.Contains("HasValidationErrors"));
            EnqueueCallback(delegate
            {
                Assert.AreEqual(1, _cities[0].ValidationErrors.Count());
                propChanged.Clear();

                // set Errors to another list with 2 errors, verify change notification is not raised on HasValidationErrors because the flag does not change
                _cities[0].ValidationResultCollection.ReplaceErrors(listWithTwoErrors);
                Assert.AreEqual(2, _cities[0].ValidationErrors.Count());
            });

            EnqueueDelay(2000);
            EnqueueCallback(delegate
            {
                Assert.AreEqual(0, propChanged.Count);
                propChanged.Clear();

                // clear the reference collection assigned to Errors, verify no changes occur to the ValidationErrors property
                listWithTwoErrors.Clear();
                Assert.AreEqual(2, _cities[0].ValidationErrors.Count());
            });

            EnqueueDelay(2000);
            EnqueueCallback(delegate
            {
                Assert.AreEqual(0, propChanged.Count);
                propChanged.Clear();

                // set ValidationErrors to null after setting it to a valid collection, verify HasValidationErrors is updated and change notification is raised
                _cities[0].ValidationResultCollection.ReplaceErrors(listWithOneError);
                _cities[0].ValidationResultCollection.ReplaceErrors(new ValidationResult[0]);
            });

            EnqueueConditional(() => propChanged.Contains("HasValidationErrors"));
            EnqueueCallback(delegate
            {
                Assert.AreEqual(0, _cities[0].ValidationErrors.Count());
            });

            EnqueueTestComplete();
        }
        #endregion

        #region Combining domain method invocation with CRUD/Attach/Detach operations
        [TestMethod]
        [Description("Verify invoke on unattached entity throws")]
        public void Entity_InvokeOnUnattached()
        {
            City city = new City { Name = "Redmond", CountyName = "King", StateName = "WA" };
            Assert.IsFalse(city.CanInvokeAction(_reject.Name));
            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                city.InvokeAction(_reject.Name, _reject.Parameters.ToArray());
            }, Resource.Entity_InvokeOnDetachedEntity);
            Assert.AreEqual(EntityState.Detached, city.EntityState);
        }

        [TestMethod]
        [Description("Verify that invoking on an unattached entity throws")]
        public void Entity_EntityAction_InvokeOnUnattached()
        {
            City city = new City { Name = "Redmond", CountyName = "King", StateName = "WA" };
            var invocableCity = city;
            Assert.IsNotNull(invocableCity);
            Assert.IsFalse(invocableCity.CanInvokeAction(_reject.Name));
            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                invocableCity.InvokeAction(_reject.Name, _reject.Parameters);
            }, Resource.Entity_InvokeOnDetachedEntity);
            Assert.AreEqual(EntityState.Detached, city.EntityState);
        }

        [TestMethod]
        [Description("Verify that invoking a domain method after a Detach throws")]
        public void Entity_InvokeOnDetached()
        {
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_cities);
            container.GetEntitySet<City>().Detach(_cities[1]);
            Assert.IsFalse(_cities[1].CanInvokeAction(_approve.Name));
            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                _cities[1].InvokeAction(_approve.Name, _approve.Parameters.ToArray());
            }, Resource.Entity_InvokeOnDetachedEntity);
            Assert.AreEqual(EntityState.Detached, _cities[1].EntityState);
        }

        [TestMethod]
        [Description("Verify that one can invoke a domain method after an Add")]
        public void Entity_InvokeOnAdded()
        {
            TestEntityContainer container = new TestEntityContainer();
            City city = new City { Name = "Redmond", CountyName = "King", StateName = "WA" };
            container.GetEntitySet<City>().Add(city);
            Assert.AreEqual(EntityState.New, city.EntityState);
            Assert.AreEqual(0, city.EntityActions.Count());
            Assert.IsTrue(city.CanInvokeAction(_assignCityZone.Name));

            city.InvokeAction(_assignCityZone.Name, _assignCityZone.Parameters.ToArray());
            Assert.AreEqual(1, city.EntityActions.Count());
            Assert.AreEqual(EntityState.New, city.EntityState);
            Assert.AreEqual<string>(_assignCityZone.Name, city.EntityActions.Single().Name);
            Assert.AreEqual<int>(_assignCityZone.Parameters.Count(), city.EntityActions.Single().Parameters.Count());
            Assert.IsFalse(city.CanInvokeAction(_assignCityZone.Name));
        }

        [TestMethod]
        [Description("Verify that one cannot invoke a domain method after a Delete")]
        public void Entity_InvokeOnDeleted()
        {
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_cities);
            container.GetEntitySet<City>().Remove(_cities[2]);
            Assert.AreEqual(EntityState.Deleted, _cities[2].EntityState);
            Assert.IsFalse(_cities[2].CanInvokeAction(_assignCityZone.Name));
            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                _cities[2].InvokeAction(_assignCityZone.Name, _assignCityZone.Parameters.ToArray());
            }, Resource.Entity_InvokeOnDeletedEntity);
            Assert.AreEqual(0, _cities[2].EntityActions.Count());
            Assert.AreEqual(EntityState.Deleted, _cities[2].EntityState);
        }

        [TestMethod]
        [Description("Verify that deleting an entity with a pending invocation clears all invocations.")]
        public void Entity_DeleteAfterInvoke()
        {
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_cities);

            // first invoke the action
            City city = _cities.First();
            city.InvokeAction(_assignCityZone.Name, _assignCityZone.Parameters.ToArray());
            Assert.IsFalse(city.CanInvokeAction(_assignCityZone.Name));
            Assert.AreEqual(1, city.EntityActions.Count());

            // delete the entity - expect the actions to be cleared
            container.GetEntitySet<City>().Remove(city);
            Assert.AreEqual(0, city.EntityActions.Count());
            Assert.AreEqual(null, city.EntityActions.Single());
            Assert.IsFalse(city.CanInvokeAction(_assignCityZone.Name));
        }

        [TestMethod]
        [Description("Verify that one can invoke a domain method after updates")]
        public void Entity_InvokeOnUpdated()
        {
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_cities);
            _cities[1].ZoneID += 1;
            Assert.AreEqual(EntityState.Modified, _cities[1].EntityState);
            Assert.IsTrue(_cities[1].CanInvokeAction(_assignCityZone.Name));
            Assert.IsTrue(_cities[1].EntityState == EntityState.Modified);
            _cities[1].InvokeAction(_assignCityZone.Name, _assignCityZone.Parameters.ToArray());
            Assert.AreEqual(1, _cities[1].EntityActions.Count());
            Assert.AreEqual(_assignCityZone.Name, _cities[1].EntityActions.Single().Name);
            Assert.AreEqual(EntityState.Modified, _cities[1].EntityState);
        }
        #endregion

        #region Other negative test scenarios
        [TestMethod]
        [Description("Verify calling Invoke twice throws and the pending invocation remains the same as the first")]
        public void Entity_InvokeTwiceOnNew()
        {
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_cities);
            _cities[0].InvokeAction(_assignCityZone.Name, _assignCityZone.Parameters.ToArray());
            Assert.AreEqual<string>(_assignCityZone.Name, _cities[0].EntityActions.Single().Name);
            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                _cities[0].InvokeAction(_assignCityZone.Name, _assignCityZone.Parameters.ToArray());
            }, Resource.Entity_MultipleCustomMethodInvocations);
            Assert.AreEqual<string>(_assignCityZone.Name, _cities[0].EntityActions.Single().Name);
            Assert.AreEqual(EntityState.Modified, _cities[0].EntityState);
        }

        [TestMethod]
        [Description("Verify calling Entity.Invoke with null entity action throws")]
        public void Entity_InvokeNull_Throws()
        {
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_cities);
            var invocableCity = _cities[0];
            Assert.IsNotNull(invocableCity);

            ExceptionHelper.ExpectArgumentException(delegate
            {
                invocableCity.InvokeAction(null);
            }, String.Format(Resource.Parameter_NullOrEmpty, "actionName"));

            Assert.AreEqual(EntityState.Unmodified, _cities[0].EntityState);
        }

        [TestMethod]
        [Description("Verify calling Entity.Invoke with unknown entity action throws")]
        public void Entity_Invoke_Unknown_EntityAction()
        {
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_rootCities);    // root ensures we don't get a derived type which changes message text
            var invocableCity = _rootCities[0];
            Assert.IsNotNull(invocableCity);

            // verify calling Invoke throws InvalidOperationException and the invocation list remains the same
            ExceptionHelper.ExpectException<MissingMethodException>(delegate
            {
                invocableCity.InvokeAction(_reject.Name, _reject.Parameters.ToArray());
            }, string.Format(Resource.ValidationUtilities_MethodNotFound, "Cities.City", _reject.Name, _reject.Parameters.Count(), "'System.String'"));

            Assert.AreEqual(EntityState.Unmodified, _rootCities[0].EntityState);
        }

        [TestMethod]
        [WorkItem(856845)]
        [Description("Verify calling Entity.Invoke with missing required properties throws ValidationException")]
        public void Entity_Invoke_Validates_Required_Properties()
        {
            TestEntityContainer container = new TestEntityContainer();
            var invocableCity = new City();
            container.GetEntitySet<City>().Add(invocableCity);

            RequiredAttribute expectedAttribute = new RequiredAttribute();
            string expectedMember = "CityName";
            string expectedError = expectedAttribute.FormatErrorMessage(expectedMember);

            ExceptionHelper.ExpectValidationException(delegate
            {
                invocableCity.AssignCityZone("West");
            }, expectedError, expectedAttribute.GetType(), null);
        }

        [TestMethod]
        [WorkItem(856845)]
        [Description("Verify calling Entity.Invoke with invalid property values throws ValidationException")]
        public void Entity_Invoke_Validates_Property_Validation()
        {
            TestEntityContainer container = new TestEntityContainer();
            var invocableCity = new City("This is an invalid state name");
            container.GetEntitySet<City>().Add(invocableCity);

            invocableCity.Name = "Redmond";

            StringLengthAttribute expectedAttribute = new StringLengthAttribute(2);
            string expectedMember = "StateName";
            string expectedError = expectedAttribute.FormatErrorMessage(expectedMember);

            ExceptionHelper.ExpectValidationException(delegate
            {
                invocableCity.AssignCityZone("West");
            }, expectedError, expectedAttribute.GetType(), invocableCity.StateName);
        }

        [TestMethod]
        [WorkItem(856845)]
        [Description("Verify calling Entity.Invoke with invalid entity validation throws ValidationException")]
        public void Entity_Invoke_Validates_Entity_Validation()
        {
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_rootCities);    // root ensures we don't get a derived type which changes message text
            var invocableCity = _rootCities[0];
            Assert.IsNotNull(invocableCity);

            invocableCity.ZoneID += 1;
            invocableCity.StateName = "WA";
            invocableCity.MakeEntityValidationFail = true;

            ExceptionHelper.ExpectValidationException(delegate
            {
                invocableCity.AssignCityZone("West");
            }, "MakeEntityValidationFail is true", typeof(CustomValidationAttribute), invocableCity);
        }

        [TestMethod]
        [Description("Verify exceptions when sending changeset containing invalid invocation directly to DomainClient")]
        public void DomainClient_SubmitWithInvalidInvocation()
        {
            TestEntityContainer container = new TestEntityContainer();
            WebDomainClient<CityDomainContext.ICityDomainServiceContract> client = new WebDomainClient<CityDomainContext.ICityDomainServiceContract>(TestURIs.Cities)
            {
                EntityTypes = new Type[] { typeof(City) }
            };
            List<Entity> emptyList = new List<Entity>();
            List<Entity> modifiedEntities = new List<Entity>();

            // verify exception is thrown when trying to invoke invalid action
            ExceptionHelper.ExpectArgumentException(delegate
            {
                _cities[0].InvokeAction("");
            }, Resource.DomainClient_InvocationNameCannotBeNullOrEmpty);
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify results does not include entity with null invocations when submitting directly to DomainClient")]
        public void DomainClient_SubmitWithNullInvocation()
        {
            TestEntityContainer container = new TestEntityContainer();

            //TODO: find a better way to not hardcode the list of known types
            WebDomainClient<CityDomainContext.ICityDomainServiceContract> client = new WebDomainClient<CityDomainContext.ICityDomainServiceContract>(TestURIs.Cities)
            {
                EntityTypes = new Type[] { typeof(City), typeof(ChangeSetEntry), typeof(EntityOperationType) }
            };
            List<Entity> emptyList = new List<Entity>();
            List<Entity> modifiedEntities = new List<Entity>();

            // invoke domain methods on a few entities
            container.LoadEntities(_cities);
            _cities[1].InvokeAction(_assignCityZone.Name, _assignCityZone.Parameters);

            // submit changeset with hand-crafted entities: valid invocation and null invocation
            modifiedEntities.Add(_cities[0]);
            modifiedEntities.Add(_cities[1]);
            modifiedEntities.Add(_cities[2]);
            Assert.AreEqual(EntityState.Modified, _cities[1].EntityState);
            EntityChangeSet changeset = new EntityChangeSet(emptyList.AsReadOnly(), modifiedEntities.AsReadOnly(), emptyList.AsReadOnly());
            SubmitCompletedResult submitResults = null;
            client.BeginSubmit(
                changeset,
                delegate(IAsyncResult asyncResult)
                {
                    submitResults = client.EndSubmit(asyncResult);
                },
                null
            );

            // wait for submit to complete
            EnqueueConditional(() => submitResults != null);
            EnqueueCallback(delegate
            {
                Assert.AreEqual(1, submitResults.Results.Count());
                Assert.AreEqual(1, submitResults.Results.Where(e => e.Operation == EntityOperationType.Update).Count());

                // REVIEW: Do we really need the operation data back from the server?
                // ChangeSetEntry returned = submitResults.Results.Single(e => e.OperationName == _assignCityZone.Name);
                // Assert.IsNotNull(returned);
                // Assert.AreEqual(1, returned.OperationData.Count());
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify failure from server when submitting an invocation for domain method that does not exist on server")]
        public void DomainClient_SubmitWithNonexistentDomainMethod()
        {
            TestEntityContainer container = new TestEntityContainer();
            WebDomainClient<CityDomainContext.ICityDomainServiceContract> client = new WebDomainClient<CityDomainContext.ICityDomainServiceContract>(TestURIs.Cities)
            {
                EntityTypes = new Type[] { typeof(City) }
            };
            List<Entity> emptyList = new List<Entity>();
            List<Entity> modifiedEntities = new List<Entity>();

            // invoke domain methods on a few entities
            container.LoadEntities(_cities);

            _cities[0].CustomMethodInvocation = _reject;

            // submit changeset with hand-crafted entities (without calling Invoke)
            modifiedEntities.Add(_cities[0]);
            EntityChangeSet changeset = new EntityChangeSet(emptyList.AsReadOnly(), modifiedEntities.AsReadOnly(), emptyList.AsReadOnly());
            SubmitCompletedResult submitResults = null;
            DomainOperationException expectedException = null;

            EnqueueCallback(delegate
            {
                client.BeginSubmit(
                    changeset,
                    delegate(IAsyncResult asyncResult)
                    {
                        try
                        {
                            submitResults = client.EndSubmit(asyncResult);
                        }
                        catch (DomainOperationException e)
                        {
                            expectedException = e;
                        }
                    },
                    null
                );
            });
            EnqueueConditional(() => expectedException != null);
            EnqueueCallback(delegate
            {
                Assert.IsNull(submitResults);
                Assert.AreEqual("This DomainService does not support operation 'Reject' for entity 'CityWithInfo'.", expectedException.Message);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Description("Verify Entity Invoke throws when input name is empty or null")]
        public void Entity_InvalidNameToInvoke()
        {
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_cities);

            // call Invoke with name=empty string
            ExceptionHelper.ExpectArgumentException(delegate
            {
                _cities[0].InvokeAction("");
            }, string.Format(CultureInfo.CurrentCulture, Resource.Parameter_NullOrEmpty, "actionName"));

            // call invoke with name=null
            ExceptionHelper.ExpectArgumentException(delegate
            {
                _cities[0].InvokeAction(null);
            }, string.Format(CultureInfo.CurrentCulture, Resource.Parameter_NullOrEmpty, "actionName"));

            // The following is commented since the product currently ignores the name attribute as
            // only one domain method is allowed for an entity.
            //// call CanInvoke with name=emtpy string
            //ExceptionHelper.ExpectArgumentException(delegate
            //{
            //    _cities[0].CanInvoke("");
            //}, string.Format(CultureInfo.CurrentCulture, Resource.Parameter_NullOrEmpty, "actionName"));

            //// call CanInvoke with name=null
            //ExceptionHelper.ExpectArgumentException(delegate
            //{
            //    _cities[0].CanInvoke(null);
            //}, string.Format(CultureInfo.CurrentCulture, Resource.Parameter_NullOrEmpty, "actionName"));

            Assert.AreEqual(EntityState.Unmodified, _cities[0].EntityState);
        }
        #endregion

        #region Domain method changeset testing
        [TestMethod]
        [Description("Verify calling Entity.RejectChanges() clears domain method invocation")]
        public void Entity_RejectChanges()
        {
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_cities);
            _cities[0].InvokeAction(_assignCityZone.Name, _assignCityZone.Parameters.ToArray<object>());
            Assert.AreEqual(1, _cities[0].EntityActions.Count());

            ((System.ComponentModel.IRevertibleChangeTracking)_cities[0]).RejectChanges();
            Assert.AreEqual(0, _cities[0].EntityActions.Count());
            Assert.AreEqual(EntityState.Unmodified, _cities[0].EntityState);

            var invocableCity = _cities[0];
            Assert.AreEqual(0, invocableCity.EntityActions.Count());
        }

        [TestMethod]
        [Description("Verify EntityContainer.GetChanges include domain method invocations")]
        public void EntityContainer_GetChanges()
        {
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_cities);
            EntityChangeSet changeset = container.GetChanges();
            Assert.IsNotNull(changeset);
            Assert.IsTrue(changeset.IsEmpty);

            // invoke domain method on 2 cities and verify invocation is in changeset
            // TODO: need 2 separate legal domain methods
            _cities[0].InvokeAction(_assignCityZone.Name, _assignCityZone.Parameters.ToArray<object>());
            _cities[2].InvokeAction(_assignCityZone.Name, _assignCityZone.Parameters.ToArray<object>());
            changeset = container.GetChanges();
            Assert.IsTrue(changeset.AddedEntities.Count == 0);
            Assert.IsTrue(changeset.ModifiedEntities.Count == 2);
            Assert.IsTrue(changeset.RemovedEntities.Count == 0);

            Assert.AreEqual<string>(_assignCityZone.Name, changeset.ModifiedEntities[0].EntityActions.Single().Name);
            Assert.AreEqual<string>(_assignCityZone.Name, changeset.ModifiedEntities[1].EntityActions.Single().Name);

            // revert one of the domain method invocations. verify invocation in changeset is updated
            ((System.ComponentModel.IRevertibleChangeTracking)_cities[0]).RejectChanges();
            changeset = container.GetChanges();
            City returned = changeset.ModifiedEntities.Single(p => p == _cities[2]) as City;
            Assert.IsNotNull(returned);
            Assert.AreEqual<string>(_assignCityZone.Name, returned.EntityActions.Single().Name);
            Assert.AreEqual<int>(_assignCityZone.Parameters.Count(), returned.EntityActions.Single().Parameters.Count());
        }

        [TestMethod]
        [Description("Verify changeset does not contain invocation if the entity is deleted")]
        public void EntityContainer_GetChanges_RemoveAfterInvoke()
        {
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_cities);
            EntityChangeSet changeset = container.GetChanges();
            Assert.IsTrue(changeset.IsEmpty);

            // invoke then delete the entity. Verify the ModifiedEntities are updated
            _cities[0].InvokeAction(_assignCityZone.Name, _assignCityZone.Parameters.ToArray<object>());
            Assert.AreEqual(1, _cities[0].EntityActions.Count());
            Assert.AreEqual(1, container.GetChanges().ModifiedEntities.Count());

            container.GetEntitySet<City>().Remove(_cities[0]);
            Assert.AreEqual(0, container.GetChanges().ModifiedEntities.Count());
        }

        [TestMethod]
        [Description("Verify changeset does not contain invocation if the entity is detached")]
        public void EntityContainer_GetChanges_DetachAfterInvoke()
        {
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_cities);
            EntityChangeSet changeset = container.GetChanges();
            Assert.IsTrue(changeset.IsEmpty);

            // invoke then delete the entity. Verify the ModifiedEntities are updated
            _cities[0].InvokeAction(_assignCityZone.Name, _assignCityZone.Parameters.ToArray<object>());
            Assert.AreEqual(1, _cities[0].EntityActions.Count());
            Assert.AreEqual(1, container.GetChanges().ModifiedEntities.Count());

            container.GetEntitySet<City>().Detach(_cities[0]);
            Assert.AreEqual(0, container.GetChanges().ModifiedEntities.Count());
        }
        #endregion

        #region End2End tests

        [TestMethod]
        [Asynchronous]
        [Description("Verify that change notifications for CanInvoke/IsInvoked properties are raised properly.")]
        public void CustomMethodFlag_ChangeNotifications()
        {
            List<string> propChanged = new List<string>();
            CityDomainContext ctxt = new CityDomainContext(TestURIs.Cities);
            City city = null;

            LoadOperation lo = ctxt.Load(ctxt.GetCitiesQuery(), false);
            SubmitOperation so = null;

            EnqueueConditional(() => lo.IsComplete);
            EnqueueCallback(delegate
            {
                city = ctxt.Cities.First();

                city.PropertyChanged += (s, e) =>
                {
                    propChanged.Add(e.PropertyName);
                };

                Assert.IsTrue(city.CanAssignCityZone);
                Assert.IsFalse(city.IsAssignCityZoneInvoked);

                city.AssignCityZone("Twilight");

                Assert.IsFalse(city.CanAssignCityZone);
                Assert.IsTrue(city.IsAssignCityZoneInvoked);

                Assert.IsTrue(propChanged.Contains("CanAssignCityZone"));
                Assert.IsTrue(propChanged.Contains("IsAssignCityZoneInvoked"));
                propChanged.Clear();

                so = ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.IsFalse(so.HasError);

                Assert.IsTrue(city.CanAssignCityZone);
                Assert.IsFalse(city.IsAssignCityZoneInvoked);

                Assert.IsTrue(propChanged.Contains("CanAssignCityZone"));
                Assert.IsTrue(propChanged.Contains("IsAssignCityZoneInvoked"));
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify e2e domain method execution during a DomainContext Submit")]
        public void DomainContext_Submit_DomainMethodOnly()
        {
            EntityChangeSet changeset;
            List<string> propChanged = new List<string>();
            CityDomainContext citiesProvider = new CityDomainContext(TestURIs.Cities);

            LoadOperation lo = citiesProvider.Load(citiesProvider.GetCitiesQuery(), false);
            SubmitOperation so = null;

            City firstRootCity = null;
            City lastRootCity = null;

            // wait for Load to complete, then invoke some domain methods
            EnqueueConditional(() => lo.IsComplete);
            EnqueueCallback(delegate
            {
                changeset = citiesProvider.EntityContainer.GetChanges();
                Assert.IsTrue(changeset.IsEmpty);

                // Find a root city.  This test specifically does not want to accidently use a derived type
                firstRootCity = citiesProvider.Cities.Where(c => c.GetType() == typeof(City)).FirstOrDefault();
                Assert.IsNotNull(firstRootCity, "Expected to find a root City type in entity list");

                lastRootCity = citiesProvider.Cities.Where(c => c.GetType() == typeof(City)).LastOrDefault();
                Assert.IsNotNull(lastRootCity, "Expected to find a root City type in entity list");

                Assert.AreNotEqual(firstRootCity, lastRootCity, "Expected first and last city to be different");

                firstRootCity.PropertyChanged += delegate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                {
                    bool isEntityBaseProperty = typeof(City).GetProperty(e.PropertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) == null;
                    if (!isEntityBaseProperty)
                    {
                        propChanged.Add(e.PropertyName);
                    }
                };

                Assert.IsTrue(firstRootCity.CanAssignCityZone);
                firstRootCity.AssignCityZone("Zone15");
            });

            // wait for prop changed for domain method guards
            EnqueueConditional(() => propChanged.Count > 0);
            EnqueueCallback(delegate
            {
                Assert.IsTrue(propChanged.Contains("CanAssignCityZone"));
                Assert.IsTrue(propChanged.Contains("CanAutoAssignCityZone"));
                propChanged.Clear();

                Assert.IsTrue(lastRootCity.CanAutoAssignCityZone);
                lastRootCity.AutoAssignCityZone();

                changeset = citiesProvider.EntityContainer.GetChanges();
                Assert.AreEqual(2, changeset.ModifiedEntities.Count);

                so = citiesProvider.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);

                Assert.AreEqual(2, so.ChangeSet.ModifiedEntities.Count);
                Assert.AreEqual(0, so.ChangeSet.AddedEntities.Count);
                Assert.AreEqual(0, so.ChangeSet.RemovedEntities.Count);
            });
            // wait for submit to complete, then verify invoked entities in changeset
            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.IsNull(so.Error);
                Assert.AreEqual(2, so.ChangeSet.ModifiedEntities.Count);

                // verify we got the property change notification for the city entity as a result of autosync
                Assert.AreEqual(6, propChanged.Count);
                Assert.IsTrue(propChanged.Contains("ZoneName"));
                Assert.IsTrue(propChanged.Contains("ZoneID"));
                Assert.IsTrue(propChanged.Contains("CanAssignCityZone"));
                Assert.IsTrue(propChanged.Contains("IsAssignCityZoneInvoked"));
                Assert.IsTrue(propChanged.Contains("CanAutoAssignCityZone"));
                Assert.IsTrue(propChanged.Contains("CanAssignCityZoneIfAuthorized"));

                // verify entities are auto-synced back to the client as a result of the domain method execution on server
                Assert.AreEqual(15, citiesProvider.Cities.Single<City>(c => (c.ZoneName == "Zone15")).ZoneID);
                Assert.AreEqual(1, citiesProvider.Cities.Single<City>(c => (c.ZoneName == "Auto_Zone1")).ZoneID);

                // verify unchanged entities
                Assert.AreEqual(0, citiesProvider.Cities.First(c => (c.ZoneName == null)).ZoneID);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify e2e domain method and CRUD execution during a DomainContext Submit")]
        public void DomainContext_Submit_DomainMethodAndCRUD()
        {
            List<string> propChanged_addedCity = new List<string>();
            City newCity = new City { Name = "Sammamish", CountyName = "King", StateName = "WA" };
            int refZipCode = 0;
            int refCityCount = 0;

            CityDomainContext citiesProvider = new CityDomainContext(TestURIs.Cities);

            LoadOperation lo = citiesProvider.Load(citiesProvider.GetCitiesQuery(), false);
            SubmitOperation so = null;

            // wait for LoadCities to complete, then LoadZips
            EnqueueConditional(() => lo.IsComplete);
            EnqueueCallback(delegate
            {
                refCityCount = citiesProvider.Cities.Count;
                lo = citiesProvider.Load(citiesProvider.GetZipsQuery(), false);
            });

            // wait for Load to complete, then invoke some domain methods
            EnqueueConditional(() => lo.IsComplete);
            EnqueueCallback(delegate
            {
                // this test the following combinations of CRUD and Domain method in changeset:
                // - Invoke -> update
                // - Add -> invoke
                // - Remove
                // - Invoke only
                City[] cities = citiesProvider.Cities.ToArray();
                cities[0].ZoneName = "Zone44";
                cities[0].AssignCityZone("Zone1");
                cities[1].AssignCityZone("Zone2");
                citiesProvider.Cities.Add(newCity);
                newCity.AssignCityZone("Zone3");
                citiesProvider.Cities.Remove(cities[4]);
                citiesProvider.Cities.Remove(cities[5]);

                // keep a reference zip code before invoking the method: this will increment Code by offset=1 on the server
                Zip zip = citiesProvider.Zips.First();
                refZipCode = zip.Code;
                zip.ReassignZipCode(1, true);

                newCity.PropertyChanged += delegate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                {
                    if (e.PropertyName != "IsReadOnly" && e.PropertyName != "HasChanges"
                        && e.PropertyName != "EntityState" && e.PropertyName != "ValidationErrors")
                    {
                        propChanged_addedCity.Add(e.PropertyName);
                    }
                };

                so = citiesProvider.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);

                Assert.AreEqual(3, so.ChangeSet.ModifiedEntities.Count);
                Assert.AreEqual(1, so.ChangeSet.AddedEntities.Count);
                Assert.AreEqual(2, so.ChangeSet.RemovedEntities.Count);
            });
            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.IsNull(so.Error, string.Format("SubmitOperation.Error should be null.\r\nMessage: {0}\r\nStack Trace:\r\n{1}", so.Error != null ? so.Error.Message : string.Empty, so.Error != null ? so.Error.StackTrace : string.Empty));

                // verify we got property change notifications for the new city entity (guard property should be reverted once SubmitChanges is called)
                Assert.AreEqual(7, propChanged_addedCity.Count);
                Assert.IsTrue(propChanged_addedCity.Contains("ZoneName"));
                Assert.IsTrue(propChanged_addedCity.Contains("ZoneID"));
                Assert.IsTrue(propChanged_addedCity.Contains("CanAssignCityZone"));
                Assert.IsTrue(propChanged_addedCity.Contains("CanAssignCityZoneIfAuthorized"));
                Assert.IsTrue(propChanged_addedCity.Contains("IsAssignCityZoneInvoked"));
                Assert.IsTrue(propChanged_addedCity.Contains("CanAutoAssignCityZone"));

                // verify entities are auto-synced back to the client as a result of the domain method execution on server
                Assert.AreEqual(1, citiesProvider.Cities.Single<City>(c => (c.ZoneName == "Zone1" && c.CountyName == "King")).ZoneID);
                Assert.AreEqual(2, citiesProvider.Cities.Single<City>(c => (c.ZoneName == "Zone2")).ZoneID);
                Assert.AreEqual(3, citiesProvider.Cities.Single<City>(c => (c.ZoneName == "Zone3" && c.Name == newCity.Name)).ZoneID);
                Assert.IsTrue(citiesProvider.Zips.Any<Zip>(z => z.Code == refZipCode - 1));

                // verify unchanged entities
                Assert.IsFalse(citiesProvider.Cities.Any(c => (c.ZoneName == null && c.ZoneID != 0)));

                // verify that after a successful submit, DM invocations on the entities have been accepted
                Assert.IsFalse(so.ChangeSet.ModifiedEntities.Any(p => p.EntityActions.Any()));
                EntityChangeSet changeSet = citiesProvider.EntityContainer.GetChanges();
                Assert.IsTrue(changeSet.IsEmpty);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Description("Verify error message when a domain method is invoked with an incorrect number of parameters.")]
        public void DomainMethod_InvalidParameterCount()
        {
            // Create a test City Entity
            CityDomainContext provider = new CityDomainContext(TestURIs.Cities);
            City city = new City();
            city.Name = "TestCity";
            city.StateName = "AA";
            provider.Cities.Add(city); // attach to set entity set reference

            // Invoke a domain method with incorrect number of parameters.
            ExceptionHelper.ExpectException<MissingMethodException>(
                () => city.InvokeAction("AssignCityZone", 1, 2, 3),
                string.Format(Resource.ValidationUtilities_MethodNotFound, "Cities.City", "AssignCityZone", 3, "'System.Int32', 'System.Int32', 'System.Int32'"));
        }

        [TestMethod]
        [Description("Verify error message when a domain method is invoked with correct number of parameters but incorrect types (Bug 615860).")]
        public void DomainMethod_InvalidParameterTypes_Bug615860()
        {
            // Create a test City Entity
            CityDomainContext provider = new CityDomainContext(TestURIs.Cities);
            City city = new City();
            city.Name = "TestCity";
            city.StateName = "AA";
            provider.Cities.Add(city); // attach to set entity set reference

            // Invoke a domain method with correct number of parameters but incorrect types
            ExceptionHelper.ExpectException<MissingMethodException>(
                () => city.InvokeAction("AssignCityZone", 1 /*correct parameter type is string*/),
                string.Format(Resource.ValidationUtilities_MethodNotFound, "Cities.City", "AssignCityZone", 1, "'System.Int32'"));
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify the read-only state of entities through the course of a custom operation submission that causes a ValidationException.")]
        public void CustomMethod_ValidationExceptionRecovery()
        {
            bool completed = false;
            Zip zip = null;
            SubmitOperation submitOp = null;
            LoadOperation<Zip> loadOp = null;
            EventHandler completedDelegate = (sender, args) => completed = true;
            
            CityDomainContext context = new CityDomainContext(TestURIs.Cities);
            loadOp = context.Load(context.GetZipsQuery(), false);
            loadOp.Completed += completedDelegate;

            this.EnqueueConditional(() => completed);
            this.EnqueueCallback(() =>
            {
                Assert.IsFalse(loadOp.HasError, "Failed to load Zips");

                // Grab a Zip and invoke a custom operation that will throw a ValidationException.
                zip = context.Zips.First();
                zip.ThrowException(typeof(ValidationException).Name);

                // Submit
                completed = false;
                submitOp = context.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                submitOp.Completed += completedDelegate;

                // Verify entity state
                Assert.IsTrue(zip.IsReadOnly, "Expected Zip to be in a read-only state.");
                Assert.IsTrue(zip.IsSubmitting, "Expected Zip to be in a submitting state.");
                Assert.AreEqual(1, zip.EntityActions.Count(), "Expected Zip EntityActions property to contain a single invocation");
            });
            this.EnqueueConditional(() => completed);
            this.EnqueueCallback(() =>
            {
                // Verify results
                Assert.IsTrue(submitOp.HasError, "Expected errors.");
                Assert.AreEqual(1, submitOp.EntitiesInError.Count(), "Expected 1 Zip entity in error.");
                Assert.AreEqual(submitOp.EntitiesInError.Single(), zip, "Expected 1 Zip entity in error.");
                Assert.AreEqual(1, zip.ValidationErrors.Count, "Expected 1 validation error on Zip entity.");

                // Verify entity state
                Assert.IsFalse(zip.IsReadOnly, "Zip should not be in a read-only state.");
                Assert.IsFalse(zip.IsSubmitting, "Zip should not be in a submitting state.");
                Assert.AreEqual(1, zip.EntityActions.Count(), "Expected Zip EntityActions property to contain a single invocation");

                // Explicitly set a property
                zip.StateName += "x";
            });
            this.EnqueueTestComplete();
        }

        #endregion

        #region IEditableObject and IEditableCollection tests
        [TestMethod]
        [Description("Verify domain method invocations with IEditableObject.CancelEdit")]
        public void Entity_CancelEdit()
        {
            // verify that DomainMethod invocations participate in IEditableObject sessions properly
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_cities);
            System.ComponentModel.IEditableObject editableCity = _cities[0] as System.ComponentModel.IEditableObject;
            Assert.IsNotNull(editableCity);

            editableCity.BeginEdit();
            int prevZoneID = _cities[0].ZoneID;
            _cities[0].ZoneID = 777;
            _cities[0].InvokeAction(_assignCityZone.Name, _assignCityZone.Parameters.ToArray<object>());
            Assert.AreEqual(1, _cities[0].EntityActions.Count());

            // cancel editing. Domain method invocation and property updates are reverted
            editableCity.CancelEdit();
            Assert.AreEqual(prevZoneID, _cities[0].ZoneID);
            Assert.AreEqual(0, _cities[0].EntityActions.Count());
            Assert.AreEqual(EntityState.Unmodified, _cities[0].EntityState);
            Assert.IsTrue(container.GetChanges().IsEmpty);

            // now start an edit session with a modified entity
            // and verify same results
            _cities[0].ZoneID = 777;
            Assert.AreEqual(EntityState.Modified, _cities[0].EntityState);
            editableCity.BeginEdit();
            _cities[0].ZoneID = 888;
            _cities[0].InvokeAction(_assignCityZone.Name, _assignCityZone.Parameters.ToArray<object>());
            editableCity.CancelEdit();
            Assert.AreEqual(777, _cities[0].ZoneID);
            Assert.AreEqual(0, _cities[0].EntityActions.Count());
            Assert.AreEqual(EntityState.Modified, _cities[0].EntityState);

            // now test an edit session where only a DM was invoked (no property edits)
            ((System.ComponentModel.IRevertibleChangeTracking)_cities[0]).RejectChanges();
            Assert.AreEqual(EntityState.Unmodified, _cities[0].EntityState);
            editableCity.BeginEdit();
            _cities[0].InvokeAction(_assignCityZone.Name, _assignCityZone.Parameters.ToArray<object>());
            Assert.AreEqual(EntityState.Modified, _cities[0].EntityState);
            editableCity.CancelEdit();
            Assert.AreEqual(EntityState.Unmodified, _cities[0].EntityState);
            Assert.IsTrue(container.GetChanges().IsEmpty);
        }

        [TestMethod]
        [Description("Verify domain method invocation with IEditableObject.EndEdit and CancelEdit")]
        public void Entity_CancelAndEndEdit()
        {
            // BeginEdit -> invoke domain method -> EndEdit. Verify state reverts after End but changeset is not empty.
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_cities);
            System.ComponentModel.IEditableObject editableCity = _cities[0] as System.ComponentModel.IEditableObject;

            editableCity.BeginEdit();
            _cities[0].InvokeAction(_assignCityZone.Name, _assignCityZone.Parameters.ToArray<object>());
            Assert.AreEqual(EntityState.Modified, _cities[0].EntityState);
            Assert.AreEqual(1, _cities[0].EntityActions.Count());

            editableCity.EndEdit();
            Assert.AreEqual(EntityState.Modified, _cities[0].EntityState);
            Assert.AreEqual(1, _cities[0].EntityActions.Count());

            // begin another session and CancelEdit immediately. Verify that previous invocation is not affected
            editableCity.BeginEdit();
            Assert.IsFalse(_cities[0].CanInvokeAction(_assignCityZone.Name));
            editableCity.CancelEdit();
            Assert.AreEqual(EntityState.Modified, _cities[0].EntityState);
            Assert.AreEqual(1, _cities[0].EntityActions.Count());
        }
        #endregion

        #region Domain method guard property changed notifications tests

        [TestMethod]
        [Description("Verify property change notifications are raised when entity is attached")]
        public void PropertyNotification_RaisedOnAttach()
        {
            CityDomainContext cities = new CityDomainContext(TestURIs.Cities);
            City city = _rootCities[0]; // ensure we have a true base type City

            List<string> propChanged = new List<string>();
            city.PropertyChanged += delegate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (e.PropertyName.StartsWith("Can"))
                {
                    propChanged.Add(e.PropertyName);
                }
            };

            Assert.AreEqual(0, propChanged.Count);

            cities.Cities.Attach(city);

            Assert.AreEqual(3, propChanged.Count, "Saw these property changes: " + string.Join(",", propChanged.ToArray()));
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify property change notification is raised on LoadEntities (false->true)")]
        public void PropertyNotification_RaisedOnLoadEntities()
        {
            // subscribe to property changed events on entity
            List<string> propChanged = new List<string>();
            _cities[0].PropertyChanged += delegate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                propChanged.Add(e.PropertyName);
            };

            Assert.IsFalse(_cities[0].CanInvokeAction("AssignCityZone"));
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_cities);

            // LoadEntities should raise property changed notification for guard properties
            EnqueueConditional(() => propChanged.Any<string>(p => p.StartsWith("Can")));
            EnqueueCallback(delegate
            {
                Assert.IsTrue(_cities[0].CanInvokeAction("AssignCityZone"));
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify property change notification is raised on Invoke (true->false)")]
        public void PropertyNotification_RaisedOnInvoke()
        {
            List<string> propChanged = new List<string>();
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_cities);
            Assert.IsTrue(_cities[0].CanInvokeAction("AssignCityZone"));

            // subscribe to property changed events on entity and invoke
            _cities[0].PropertyChanged += delegate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                propChanged.Add(e.PropertyName);
            };
            _cities[0].AssignCityZone("Zone1");

            // Invoking domain method should raise property changed notification for guard properties
            EnqueueConditional(() => propChanged.Any<string>(p => p.StartsWith("Can")));
            EnqueueCallback(delegate
            {
                Assert.IsFalse(_cities[0].CanInvokeAction("AssignCityZone"));
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify property change notification is raised on Delete (true->false)")]
        public void PropertyNotification_RaisedOnDelete()
        {
            List<string> propChanged = new List<string>();
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_cities);
            Assert.IsTrue(_cities[0].CanInvokeAction("AssignCityZone"));

            // subscribe to property changed events on entity and Remove
            _cities[0].PropertyChanged += delegate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                propChanged.Add(e.PropertyName);
            };
            container.GetEntitySet<City>().Remove(_cities[0]);

            // Deleting entity should raise property changed notification for guard properties
            EnqueueConditional(() => propChanged.Any<string>(p => p.StartsWith("Can")));
            EnqueueCallback(delegate
            {
                Assert.IsFalse(_cities[0].CanInvokeAction("AssignCityZone"));
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify property change notification is raised on RejectChanges (false->true)")]
        public void PropertyNotification_RaisedOnRejectChanges()
        {
            List<string> propChanged = new List<string>();
            TestEntityContainer container = new TestEntityContainer();
            container.LoadEntities(_cities);

            // invoke domain method and remove the entity
            _cities[0].AssignCityZone("Zone1");
            container.GetEntitySet<City>().Remove(_cities[0]);
            Assert.IsFalse(_cities[0].CanInvokeAction("AssignCityZone"));

            // subscribe to property changed events on entity and call RejectChanges
            _cities[0].PropertyChanged += delegate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                propChanged.Add(e.PropertyName);
            };
            ((System.ComponentModel.IRevertibleChangeTracking)container).RejectChanges();

            // RejectChanges should raise property changed notification for guard properties
            EnqueueConditional(() => propChanged.Any<string>(p => p.StartsWith("Can")));
            EnqueueCallback(delegate
            {
                Assert.IsTrue(_cities[0].CanInvokeAction("AssignCityZone"));

                // verify RejectChanges does not cause multiple property change notifications
                Assert.IsNotNull(propChanged.Single<string>(p => p == "CanAssignCityZone"));
            });

            EnqueueTestComplete();
        }
        #endregion

        #region Exhaustive supported types tests

        [TestMethod]
        [Asynchronous]
        [Description("Verify e2e domain method execution using primitive supported types")]
        [FullTrustTest] // ISerializable types cannot be deserialized in medium trust.
        public void DomainMethod_PrimitiveTypes()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);
            MixedType changedObj = null;
            MixedType valuesObj = null;

            SubmitOperation so = null;
            LoadOperation lo = provider.Load(provider.GetMixedTypesQuery(), false);

            // wait for Load to complete, then invoke some domain methods
            EnqueueConditional(() => lo.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.AreEqual(3, lo.Entities.Count(), "Entities count should be 3");
                changedObj = provider.MixedTypes.Single(t => t.ID == "MixedType_Max");
                valuesObj = provider.MixedTypes.Single(t => t.ID == "MixedType_Other");

                // invoke domain method on changedObj with values from valuesObj
                changedObj.TestPrimitive(valuesObj.BooleanProp, valuesObj.ByteProp, valuesObj.SByteProp, valuesObj.Int16Prop,
                    valuesObj.UInt16Prop, valuesObj.Int32Prop, valuesObj.UInt32Prop, valuesObj.Int64Prop, valuesObj.UInt64Prop,
                    valuesObj.CharProp, valuesObj.DoubleProp, valuesObj.SingleProp);

                // submit
                so = provider.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });

            // wait for submitted event being fired and verify invoked entities in changeset
            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.IsNull(so.Error);

                // verify values of changedObj are now the same as valuesObj
                Assert.AreNotEqual(valuesObj.ID, changedObj.ID);
                Assert.AreEqual(valuesObj.BooleanProp, changedObj.BooleanProp);
                Assert.AreEqual(valuesObj.ByteProp, changedObj.ByteProp);
                Assert.AreEqual(valuesObj.SByteProp, changedObj.SByteProp);
                Assert.AreEqual(valuesObj.Int16Prop, changedObj.Int16Prop);
                Assert.AreEqual(valuesObj.UInt16Prop, changedObj.UInt16Prop);
                Assert.AreEqual(valuesObj.Int32Prop, changedObj.Int32Prop);
                Assert.AreEqual(valuesObj.UInt32Prop, changedObj.UInt32Prop);
                Assert.AreEqual(valuesObj.Int64Prop, changedObj.Int64Prop);
                Assert.AreEqual(valuesObj.UInt64Prop, changedObj.UInt64Prop);
                Assert.AreEqual(valuesObj.CharProp, changedObj.CharProp);
                Assert.AreEqual(valuesObj.DoubleProp, changedObj.DoubleProp);
                Assert.AreEqual(valuesObj.SingleProp, changedObj.SingleProp);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify e2e domain method execution using predefined supported types")]
        [FullTrustTest] // ISerializable types cannot be deserialized in medium trust.
        public void DomainMethod_PredefinedTypes()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);
            MixedType changedObj = null;
            MixedType valuesObj = null;

            SubmitOperation so = null;
            LoadOperation lo = provider.Load(provider.GetMixedTypesQuery(), false);

            // wait for Load to complete, then invoke some domain methods
            EnqueueConditional(() => lo.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.AreEqual(3, lo.Entities.Count(), "Entities count should be 3");
                changedObj = provider.MixedTypes.Single(t => t.ID == "MixedType_Max");
                valuesObj = provider.MixedTypes.Single(t => t.ID == "MixedType_Other");

                // invoke domain method on changedObj with values from valuesObj
                changedObj.TestPredefined(valuesObj.StringProp, valuesObj.DecimalProp, valuesObj.DateTimeProp,
                    valuesObj.TimeSpanProp, valuesObj.StringsProp, valuesObj.UriProp, valuesObj.GuidProp, valuesObj.BinaryProp,
                    /*valuesObj.XElementProp,*/ valuesObj.ByteArrayProp, valuesObj.EnumProp, valuesObj.DictionaryStringProp, valuesObj.DateTimeOffsetProp);

                // submit
                so = provider.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });

            // wait for submitted event being fired and verify invoked entities in changeset
            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.IsNull(so.Error);

                // verify values of changedObj are now the same as valuesObj
                Assert.AreNotEqual(valuesObj.ID, changedObj.ID);
                Assert.AreEqual(valuesObj.StringProp, changedObj.StringProp);
                Assert.AreEqual(valuesObj.DateTimeProp, changedObj.DateTimeProp);
                Assert.AreEqual(valuesObj.TimeSpanProp, changedObj.TimeSpanProp);
                Assert.AreEqual(valuesObj.DecimalProp, changedObj.DecimalProp);
                Assert.AreEqual(valuesObj.UriProp, changedObj.UriProp);
                Assert.AreEqual(valuesObj.GuidProp, changedObj.GuidProp);
                Assert.AreEqual(valuesObj.ByteArrayProp.Length, changedObj.ByteArrayProp.Length);
                Assert.AreEqual(123, changedObj.BinaryProp[2]);
                Assert.AreEqual(valuesObj.BinaryProp.Length, changedObj.BinaryProp.Length);
                //Assert.AreEqual(valuesObj.XElementProp.Value, changedObj.XElementProp.Value);
                //Assert.AreEqual("<someElement>element text</someElement>", changedObj.XElementProp.ToString());
                Assert.AreEqual(valuesObj.EnumProp, changedObj.EnumProp);
                Assert.AreEqual(valuesObj.DictionaryStringProp.Count, changedObj.DictionaryStringProp.Count);
                Assert.IsTrue(valuesObj.DictionaryStringProp.Keys.SequenceEqual(changedObj.DictionaryStringProp.Keys));
                Assert.AreEqual(valuesObj.DateTimeOffsetProp, changedObj.DateTimeOffsetProp);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify e2e domain method execution using nullable primitive supported types")]
        [FullTrustTest] // ISerializable types cannot be deserialized in medium trust.
        public void DomainMethod_NullablePrimitiveTypes()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);
            MixedType changedObj = null;
            MixedType valuesObj = null;

            SubmitOperation so = null;
            LoadOperation lo = provider.Load(provider.GetMixedTypesQuery(), false);

            // wait for Load to complete, then invoke some domain methods
            EnqueueConditional(() => lo.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.AreEqual(3, lo.Entities.Count(), "Entities count should be 3");
                changedObj = provider.MixedTypes.Single(t => t.ID == "MixedType_Max");
                valuesObj = provider.MixedTypes.Single(t => t.ID == "MixedType_Other");

                // invoke domain method on changedObj with values from valuesObj
                changedObj.TestNullablePrimitive(valuesObj.NullableBooleanProp, valuesObj.NullableByteProp, valuesObj.NullableSByteProp,
                    valuesObj.NullableInt16Prop, valuesObj.NullableUInt16Prop, valuesObj.NullableInt32Prop, valuesObj.NullableUInt32Prop,
                    valuesObj.NullableInt64Prop, valuesObj.NullableUInt64Prop, valuesObj.NullableCharProp, valuesObj.NullableDoubleProp,
                    valuesObj.NullableSingleProp);

                // submit
                so = provider.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });

            // wait for submitted event being fired and verify invoked entities in changeset
            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.IsNull(so.Error);

                // verify values of changedObj are now the same as valuesObj
                Assert.AreNotEqual(valuesObj.ID, changedObj.ID);
                Assert.AreEqual(valuesObj.NullableBooleanProp, changedObj.NullableBooleanProp);
                Assert.AreEqual(valuesObj.NullableByteProp, changedObj.NullableByteProp);
                Assert.AreEqual(valuesObj.NullableSByteProp, changedObj.NullableSByteProp);
                Assert.AreEqual(valuesObj.NullableInt16Prop, changedObj.NullableInt16Prop);
                Assert.AreEqual(valuesObj.NullableUInt16Prop, changedObj.NullableUInt16Prop);
                Assert.AreEqual(valuesObj.NullableInt32Prop, changedObj.NullableInt32Prop);
                Assert.AreEqual(valuesObj.NullableUInt32Prop, changedObj.NullableUInt32Prop);
                Assert.AreEqual(valuesObj.NullableInt64Prop, changedObj.NullableInt64Prop);
                Assert.AreEqual(valuesObj.NullableUInt64Prop, changedObj.NullableUInt64Prop);
                Assert.AreEqual(valuesObj.NullableCharProp, changedObj.NullableCharProp);
                Assert.AreEqual(valuesObj.NullableDoubleProp, changedObj.NullableDoubleProp);
                Assert.AreEqual(valuesObj.NullableSingleProp, changedObj.NullableSingleProp);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify e2e domain method execution using nullable predefined supported types")]
        [FullTrustTest] // ISerializable types cannot be deserialized in medium trust.
        public void DomainMethod_NullablePredefinedTypes()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);
            MixedType changedObj = null;
            MixedType valuesObj = null;

            SubmitOperation so = null;
            LoadOperation lo = provider.Load(provider.GetMixedTypesQuery(), false);

            // wait for Load to complete, then invoke some domain methods
            EnqueueConditional(() => lo.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.AreEqual(3, lo.Entities.Count(), "Entities count should be 3");
                changedObj = provider.MixedTypes.Single(t => t.ID == "MixedType_Max");
                valuesObj = provider.MixedTypes.Single(t => t.ID == "MixedType_Other");

                // invoke domain method on changedObj with values from valuesObj
                changedObj.TestNullablePredefined(valuesObj.NullableDecimalProp, valuesObj.NullableDateTimeProp,
                    valuesObj.NullableTimeSpanProp, valuesObj.NullableGuidProp, valuesObj.NullableEnumProp, valuesObj.NullableDateTimeOffsetProp);

                // submit
                so = provider.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });

            // wait for submitted event being fired and verify invoked entities in changeset
            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.IsNull(so.Error);

                // verify values of changedObj are now the same as valuesObj
                Assert.AreNotEqual(valuesObj.ID, changedObj.ID);
                Assert.AreEqual(valuesObj.NullableDateTimeProp, changedObj.NullableDateTimeProp);
                Assert.AreEqual(valuesObj.NullableTimeSpanProp, changedObj.NullableTimeSpanProp);
                Assert.AreEqual(valuesObj.NullableDecimalProp, changedObj.NullableDecimalProp);
                Assert.AreEqual(valuesObj.NullableGuidProp, changedObj.NullableGuidProp);
                Assert.AreEqual(valuesObj.NullableEnumProp, changedObj.NullableEnumProp);
                Assert.AreEqual(valuesObj.NullableDateTimeOffsetProp, changedObj.NullableDateTimeOffsetProp);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify e2e domain method execution using null values for nullable params")]
        [FullTrustTest] // ISerializable types cannot be deserialized in medium trust.
        public void DomainMethod_NullableTypes_NullParams()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);
            MixedType obj1 = null;
            MixedType obj2 = null;

            SubmitOperation so = null;
            LoadOperation lo = provider.Load(provider.GetMixedTypesQuery(), false);

            // wait for Load to complete, then invoke some domain methods
            EnqueueConditional(() => lo.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.AreEqual(3, lo.Entities.Count(), "Entities count should be 3");
                obj1 = provider.MixedTypes.Single(t => t.ID == "MixedType_Min");
                obj2 = provider.MixedTypes.Single(t => t.ID == "MixedType_Other");

                // invoke domain methods with null values
                obj1.TestNullablePrimitive(null, null, null, null, null, null, null, null, null, null, null, null);
                obj2.TestNullablePredefined(null, null, null, null, null, null);

                // submit
                so = provider.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });

            // wait for submitted event being fired and verify invoked entities in changeset
            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.IsNull(so.Error);

                // verify values of obj1 and obj2
                Assert.IsNull(obj1.NullableBooleanProp);
                Assert.IsNull(obj1.NullableByteProp);
                Assert.IsNull(obj1.NullableSByteProp);
                Assert.IsNull(obj1.NullableInt16Prop);
                Assert.IsNull(obj1.NullableUInt16Prop);
                Assert.IsNull(obj1.NullableInt32Prop);
                Assert.IsNull(obj1.NullableUInt32Prop);
                Assert.IsNull(obj1.NullableInt64Prop);
                Assert.IsNull(obj1.NullableUInt64Prop);
                Assert.IsNull(obj1.NullableCharProp);
                Assert.IsNull(obj1.NullableDoubleProp);
                Assert.IsNull(obj1.NullableSingleProp);
                Assert.IsNotNull(obj1.NullableDateTimeProp);

                Assert.IsNull(obj2.NullableDateTimeProp);
                Assert.IsNull(obj2.NullableTimeSpanProp);
                Assert.IsNull(obj2.NullableDecimalProp);
                Assert.IsNull(obj2.NullableGuidProp);
                Assert.IsNull(obj2.NullableEnumProp);
                Assert.IsNotNull(obj2.NullableBooleanProp);
                Assert.IsNull(obj2.NullableDateTimeOffsetProp);
            });

            EnqueueTestComplete();
        }
        #endregion

        #region Named Update Methods

        [TestMethod]
        [WorkItem(591109)]
        [Description("Verifies entity type supported operations when only a Custom method is defined in the DomainService.")]
        public void NamedUpdate_CustomOnly_SupportedOperations()
        {
            Uri uri = new Uri(TestURIs.RootURI, "TestDomainServices-NamedUpdates-NamedUpdate_CustomOnly.svc");
            NamedUpdate_CustomOnly ctx = new NamedUpdate_CustomOnly(uri);

            Assert.IsFalse(ctx.MockEntity1s.CanAdd);
            Assert.IsTrue(ctx.MockEntity1s.CanEdit);
            Assert.IsFalse(ctx.MockEntity1s.CanRemove);
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(591109)]
        [Description("Verifies that entity updates are applied successfully when a named custom update method is invoked.")]
        public void NamedUpdate_CustomOnly_SuccessfulUpdate()
        {
            LoadOperation<MockEntity1> loadOp = null;
            SubmitOperation submitOp = null;
            Uri uri = new Uri(TestURIs.RootURI, "TestDomainServices-NamedUpdates-NamedUpdate_CustomOnly.svc");
            NamedUpdate_CustomOnly ctx = new NamedUpdate_CustomOnly(uri);

            // Here, we have a custom method defined on the server.  Let's 
            // perform some update operations on an entity, invoke a custom 
            // method and submit changes.
            loadOp = ctx.Load(ctx.GetEntitiesQuery(), false);
            this.EnqueueConditional(() => loadOp.IsComplete);
            this.EnqueueCallback(() =>
            {
                // Verify we loaded OK
                this.AssertCompletedWithoutErrors(loadOp);
                Assert.AreEqual(1, loadOp.Entities.Count(), "Expected to load 1 entity.");

                // Retrieve an editable entity
                MockEntity1 entity = ctx.MockEntity1s.First();
                Assert.IsFalse(entity.IsReadOnly);

                // Verify the initial expected state
                Assert.AreEqual("OriginalValue1", entity.Property1);
                Assert.AreEqual("OriginalValue2", entity.Property2);
                Assert.AreEqual("OriginalValue3", entity.Property3);

                // Update entity state using property setters and a custom method
                entity.Property2 = "NewValue2";
                entity.Property3 = "NewValue3";
                entity.NamedUpdateMethod("NewValue1");

                // Submit changes
                Assert.IsTrue(entity.IsReadOnly);
                submitOp = ctx.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            this.EnqueueConditional(() => submitOp.IsComplete);
            this.EnqueueCallback(() =>
            {
                // Verify we submitted OK
                this.AssertCompletedWithoutErrors(submitOp);

                // Verify our update persisted
                MockEntity1 entity = ctx.MockEntity1s.First();
                Assert.IsFalse(entity.IsReadOnly);
                Assert.AreEqual("NewValue1", entity.Property1);
                Assert.AreEqual("NewValue2", entity.Property2);
                Assert.AreEqual("OriginalValue3", entity.Property3); // Our update is a masked update that always reverts the Property3 value.
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(591109)]
        [Description("Verifies that entity updates fail when a named custom update method is not invoked.")]
        public void NamedUpdate_CustomOnly_FailWhenCustomMethodNotCalled()
        {
            LoadOperation<MockEntity1> loadOp = null;
            SubmitOperation submitOp = null;
            Uri uri = new Uri(TestURIs.RootURI, "TestDomainServices-NamedUpdates-NamedUpdate_CustomOnly.svc");
            NamedUpdate_CustomOnly ctx = new NamedUpdate_CustomOnly(uri);

            // Here, we have a custom method defined on the server.  Let's 
            // perform some update operations on an entity and submit changes.
            // This should result in a failure since no custom method was 
            // invoked.
            loadOp = ctx.Load(ctx.GetEntitiesQuery(), false);
            this.EnqueueConditional(() => loadOp.IsComplete);
            this.EnqueueCallback(() =>
            {
                // Verify we loaded OK
                this.AssertCompletedWithoutErrors(loadOp);
                Assert.AreEqual(1, loadOp.Entities.Count(), "Expected to load 1 entity.");

                // Retrieve an editable entity
                MockEntity1 entity = ctx.MockEntity1s.First();
                Assert.IsFalse(entity.IsReadOnly);

                // Verify the initial expected state
                Assert.AreEqual("OriginalValue1", entity.Property1);
                Assert.AreEqual("OriginalValue2", entity.Property2);
                Assert.AreEqual("OriginalValue3", entity.Property3);

                // Update entity state using property setters
                entity.Property1 = "NewValue1";
                entity.Property2 = "NewValue2";
                entity.Property3 = "NewValue3";

                // Submit changes
                submitOp = ctx.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            this.EnqueueConditional(() => submitOp.IsComplete);
            this.EnqueueCallback(() =>
            {
                // Verify error message
                string expectedMessage = string.Format(Resource.DomainContext_SubmitOperationFailed, string.Format("This DomainService does not support operation '{0}' for entity '{1}'.", "Update", typeof(MockEntity1).Name));
                this.AssertCompletedWithError(submitOp, expectedMessage);
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(591109)]
        [Description("Verifies that entity updates are applied successfully when an Update and Custom method exist.")]
        public void NamedUpdate_CustomAndUpdate_SuccessfulUpdate()
        {
            LoadOperation<MockEntity2> loadOp = null;
            SubmitOperation submitOp = null;
            Uri uri = new Uri(TestURIs.RootURI, "TestDomainServices-NamedUpdates-NamedUpdate_CustomAndUpdate.svc");
            NamedUpdate_CustomAndUpdate ctx = new NamedUpdate_CustomAndUpdate(uri);

            // Verify expected operation support
            Assert.IsFalse(ctx.MockEntity2s.CanAdd);
            Assert.IsTrue(ctx.MockEntity2s.CanEdit);
            Assert.IsFalse(ctx.MockEntity2s.CanRemove);

            // Here, we have a custom AND update method defined on the server.  
            // Let's perform some update operations on an entity, invoke a custom 
            // method and submit changes.
            loadOp = ctx.Load(ctx.GetEntitiesQuery(), false);
            this.EnqueueConditional(() => loadOp.IsComplete);
            this.EnqueueCallback(() =>
            {
                // Verify we loaded OK
                this.AssertCompletedWithoutErrors(loadOp);
                Assert.AreEqual(1, loadOp.Entities.Count(), "Expected to load 1 entity.");

                // Retrieve an editable entity
                MockEntity2 entity = ctx.MockEntity2s.First();
                Assert.IsFalse(entity.IsReadOnly);

                // Verify the initial expected state
                Assert.AreEqual("OriginalValue1", entity.Property1);
                Assert.AreEqual("OriginalValue2", entity.Property2);
                Assert.AreEqual("OriginalValue3", entity.Property3);

                // Update entity state using property setters and a custom method
                entity.Property2 = "NewValue2";
                entity.Property3 = "NewValue3";
                entity.NamedUpdateMethod("NewValue1");

                // Submit changes
                Assert.IsTrue(entity.IsReadOnly);
                submitOp = ctx.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            this.EnqueueConditional(() => submitOp.IsComplete);
            this.EnqueueCallback(() =>
            {
                // Verify we submitted OK
                this.AssertCompletedWithoutErrors(submitOp);

                // Verify our update persisted
                MockEntity2 entity = ctx.MockEntity2s.First();
                Assert.IsFalse(entity.IsReadOnly);

                // This is a special case where we know our update method performs
                // a masked update and we can expect Property2 => 'UpdatedProperty2'.
                // (See NamedUpdate_CustomAndUpdate domain service.)
                Assert.AreEqual("NewValue1", entity.Property1);
                Assert.AreEqual("UpdatedValue2", entity.Property2);
                Assert.AreEqual("OriginalValue3", entity.Property3);  // Our update is a masked-update that always reverts the Property3 value.
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verifies that custom update entity parameter validation does not run on the client and does run on the server.")]
        public void NamedUpdate_CustomValidation_Entity_Parameter()
        {
            string clientInvalidProperty = "Invalid on client";
            MockComplexObject1[] complexArray = new MockComplexObject1[] { new MockComplexObject1() };
            MockComplexObject1 recursiveComplexObject = new MockComplexObject1 { Property1 = new MockComplexObject1() };
            string[] memberNames = new string[] { "PlaceholderName" };
            ValidationResult expectedClientValidationResult = new ValidationResult(clientInvalidProperty, memberNames);

            Uri uri = new Uri(TestURIs.RootURI, "TestDomainServices-NamedUpdates-NamedUpdate_CustomValidation.svc");
            NamedUpdate_CustomValidation ctx = new NamedUpdate_CustomValidation(uri);
            MockEntity3 entity = null;
            LoadOperation<MockEntity3> loadOp = null;
            SubmitOperation submitOp = null;

            this.EnqueueCallback(() =>
            {
                loadOp = ctx.Load(ctx.GetEntities3Query(), TestHelperMethods.DefaultOperationAction, null);
            });

            this.EnqueueConditional(() => loadOp.IsComplete);

            this.EnqueueCallback(() =>
            {
                TestHelperMethods.AssertOperationSuccess(loadOp);
                entity = loadOp.Entities.First();

                // make entity param invalid
                DynamicTestValidator.Reset();
                DynamicTestValidator.ForcedValidationResults.Add(typeof(MockEntity3), expectedClientValidationResult);
                DynamicTestValidator.Monitor(true);

                // entity param validation should not be run during the call
                entity.NamedUpdateWithParamValidation(complexArray, recursiveComplexObject);

                // entity param validation should not be run during the submit on the client side
                submitOp = ctx.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });

            this.EnqueueConditional(() => submitOp.IsComplete);

            this.EnqueueCallback(() =>
            {
                // the server fails all params
                Assert.IsTrue(submitOp.HasError, "Error on client submit of an incorrect entity.");

                // check that param validation was not run on the client
                // note, entity has no param name so we cannot check for it directly
                Assert.IsFalse(DynamicTestValidator.ValidationCalls.Where(vc =>
                    vc.DisplayName != "array"
                    && vc.DisplayName != "complexObject"
                    && vc.DisplayName != "ValidatedProperty").Any(), "Entity underwent param validation.");

                // check that the param validtion was run on the server
                IEnumerable<ValidationResult> expectedServerValidationResult = CustomMethodTests.GetExpectedErrors("NamedUpdateWithParamValidation");
                UnitTestHelper.AssertValidationResultsAreEqual(expectedServerValidationResult, entity.ValidationResultCollection);
            });

            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verifies that custom update parameter validation runs successfully during the call and client submit.")]
        public void NamedUpdate_CustomValidation_Parameter()
        {
            MockComplexObject1[] complexArray = new MockComplexObject1[] { new MockComplexObject1() };
            MockComplexObject1 complexObject = new MockComplexObject1 { Property1 = new MockComplexObject1() };

            Uri uri = new Uri(TestURIs.RootURI, "TestDomainServices-NamedUpdates-NamedUpdate_CustomValidation.svc");
            NamedUpdate_CustomValidation ctx = null;
            MockEntity3 entity = null;
            LoadOperation<MockEntity3> loadOp = null;
            SubmitOperation submitOp = null;

            // The core body of the test. This delegate can be called multiple times.
            Action<object, ValidationResult, ValidationResult> paramVariation = (invalidParam, registeredResult, expectedResult) =>
            {
                this.EnqueueCallback(() =>
                {
                    ctx = new NamedUpdate_CustomValidation(uri);
                    loadOp = ctx.Load(ctx.GetEntities3Query(), TestHelperMethods.DefaultOperationAction, null);
                });

                this.EnqueueConditional(() => loadOp.IsComplete);

                this.EnqueueCallback(() =>
                {
                    TestHelperMethods.AssertOperationSuccess(loadOp);
                    entity = loadOp.Entities.First();

                    // make param invalid
                    DynamicTestValidator.Reset();
                    DynamicTestValidator.ForcedValidationResults.Add(invalidParam.GetType(), registeredResult);

                    // param validation should throw on call
                    ValidationException vex = ExceptionHelper.ExpectValidationException(() =>
                    {
                        entity.NamedUpdateWithParamValidation(complexArray, complexObject);
                    }, expectedResult.ErrorMessage, typeof(CustomValidationAttribute), invalidParam);
                    // The exception varies from the expected because the framework does not alter the result.
                    // This means the expected MemberNames is equivalent to the registered MemberNames
                    UnitTestHelper.AssertValidationResultsAreEqual(new ValidationResult[] { new ValidationResult(expectedResult.ErrorMessage, registeredResult.MemberNames) }, new ValidationResult[] { vex.ValidationResult });

                    // ensure param validation fails on client submit
                    DynamicTestValidator.Reset();
                    entity.NamedUpdateWithParamValidation(complexArray, complexObject);

                    // make param invalid once again
                    DynamicTestValidator.ForcedValidationResults.Add(invalidParam.GetType(), registeredResult);

                    // actually submit
                    submitOp = ctx.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                });

                this.EnqueueConditional(() => submitOp.IsComplete);

                this.EnqueueCallback(() =>
                {
                    Assert.IsTrue(submitOp.HasError, "No error on client submit of an incorrect param.");

                    // compare to expected
                    UnitTestHelper.AssertValidationResultsAreEqual(new ValidationResult[] { expectedResult }, entity.ValidationResultCollection);

                    // ensure param validation fails on server submit
                    DynamicTestValidator.Reset();
                    submitOp = ctx.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                });

                this.EnqueueConditional(() => submitOp.IsComplete);

                this.EnqueueCallback(() =>
                {
                    Assert.IsTrue(submitOp.HasError, "No error on server submit of an incorrect param.");

                    // compare to expected, the server errors no matter what we send it, so its
                    // expected validation failures can be static.
                    IEnumerable<ValidationResult> expectedServerValidationResults = CustomMethodTests.GetExpectedErrors("NamedUpdateWithParamValidation");
                    UnitTestHelper.AssertValidationResultsAreEqual(expectedServerValidationResults, entity.ValidationResultCollection);
                });
            };

            string format1 = "{0} invalid on the client";

            // Run variations
            paramVariation(complexArray,
                CreateRegisteredValidationResult(format1, complexArray.GetType()),
                CreateExpectedValidationResult(format1, complexArray.GetType(), true, "NamedUpdateWithParamValidation.array().PlaceholderName"));

            paramVariation(complexObject,
                CreateRegisteredValidationResult(format1, complexObject.GetType()),
                CreateExpectedValidationResult(format1, complexObject.GetType(), true, "NamedUpdateWithParamValidation.complexObject.PlaceholderName"));

            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verifies that custom update type validation runs successfully during the call and submit.")]
        public void NamedUpdate_CustomValidation_Type()
        {
            MockComplexObject2[] complexArray = new MockComplexObject2[] { new MockComplexObject2() };
            MockComplexObject2 complexObject = new MockComplexObject2 { Property1 = new MockComplexObject2() };

            SubmitOperation submitOp = null;
            LoadOperation<MockEntity4> loadOp = null;
            MockEntity4 entity = null;

            Uri uri = new Uri(TestURIs.RootURI, "TestDomainServices-NamedUpdates-NamedUpdate_CustomValidation.svc");
            NamedUpdate_CustomValidation ctx = null;

            Action<object, bool, ValidationResult, ValidationResult> typeVariation = (invalidParam, deepValidation, registeredResult, expectedResult) =>
            {
                this.EnqueueCallback(() =>
                {
                    ctx = new NamedUpdate_CustomValidation(uri);
                    loadOp = ctx.Load(ctx.GetEntities4Query(), TestHelperMethods.DefaultOperationAction, null);
                });

                this.EnqueueConditional(() => loadOp.IsComplete);

                this.EnqueueCallback(() =>
                {
                    TestHelperMethods.AssertOperationSuccess(loadOp);
                    entity = loadOp.Entities.First();

                    if (invalidParam == null)
                    {
                        invalidParam = entity;
                    }

                    // make param invalid
                    DynamicTestValidator.Reset();
                    DynamicTestValidator.ForcedValidationResults.Add(invalidParam, registeredResult);

                    // deep type validation should not throw on call
                    if (deepValidation)
                    {
                        entity.NamedUpdateWithTypeValidation(complexArray, complexObject);
                        ctx.MockEntity4s.Clear();
                        entity.Reset();
                        ctx.MockEntity4s.Attach(entity);
                    }
                    // shallow type validation should throw on call
                    else
                    {
                        ValidationException vex = ExceptionHelper.ExpectValidationException(() =>
                        {
                            entity.NamedUpdateWithTypeValidation(complexArray, complexObject);
                        }, expectedResult.ErrorMessage, typeof(CustomValidationAttribute), invalidParam);
                        // The exception varies from the expected because the framework does not alter the result.
                        // This means the expected MemberNames is equivalent to the registered MemberNames
                        UnitTestHelper.AssertValidationResultsAreEqual(new ValidationResult[] { new ValidationResult(expectedResult.ErrorMessage, registeredResult.MemberNames) }, new ValidationResult[] { vex.ValidationResult });
                    }

                    // ensure type validation fails on client submit
                    DynamicTestValidator.Reset();
                    entity.NamedUpdateWithTypeValidation(complexArray, complexObject);

                    // make param invalid once again
                    DynamicTestValidator.ForcedValidationResults.Add(invalidParam, registeredResult);

                    // actually submit
                    submitOp = ctx.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                });

                this.EnqueueConditional(() => submitOp.IsComplete);

                this.EnqueueCallback(() =>
                {
                    Assert.IsTrue(submitOp.HasError, "No error on client submit of an incorrect entity.");

                    // compare to expected
                    UnitTestHelper.AssertValidationResultsAreEqual(new ValidationResult[] { expectedResult }, entity.ValidationResultCollection);

                    // ensure type validation fails on server submit
                    DynamicTestValidator.Reset();
                    submitOp = ctx.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                });

                this.EnqueueConditional(() => submitOp.IsComplete);

                this.EnqueueCallback(() =>
                {
                    Assert.IsTrue(submitOp.HasError, "No error on server submit of an incorrect entity.");

                    // compare to expected, the server errors no matter what we send it, so its
                    // expected validation failures can be static.
                    IEnumerable<ValidationResult> expectedServerValidationResults = CustomMethodTests.GetExpectedErrors("NamedUpdateWithTypeValidation");
                    UnitTestHelper.AssertValidationResultsAreEqual(expectedServerValidationResults, entity.ValidationResultCollection);
                });
            };

            // Run variations
            string format1 = "{0} invalid on the client";
            typeVariation(null, false,
                CustomMethodTests.CreateRegisteredValidationResult(format1, typeof(MockEntity4)),
                CustomMethodTests.CreateExpectedValidationResult(format1, typeof(MockEntity4), false));

            typeVariation(complexArray[0], true,
                CustomMethodTests.CreateRegisteredValidationResult(format1, typeof(MockComplexObject2)),
                CustomMethodTests.CreateExpectedValidationResult(format1, typeof(MockComplexObject2), false, "NamedUpdateWithTypeValidation.array().PlaceholderName"));

            typeVariation(complexObject, false,
                CustomMethodTests.CreateRegisteredValidationResult(format1, typeof(MockComplexObject2)),
                CustomMethodTests.CreateExpectedValidationResult(format1, typeof(MockComplexObject2), false, "NamedUpdateWithTypeValidation.complexObject.PlaceholderName"));
            
            typeVariation(complexObject.Property1, true,
                CustomMethodTests.CreateRegisteredValidationResult(format1, typeof(MockComplexObject2)),
                CustomMethodTests.CreateExpectedValidationResult(format1, typeof(MockComplexObject2), false, "NamedUpdateWithTypeValidation.complexObject.Property1.PlaceholderName"));

            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verifies that custom update property validation runs successfully during the call and submit.")]
        public void NamedUpdate_CustomValidation_Property()
        {
            MockComplexObject1[] complexArray = new MockComplexObject1[] { new MockComplexObject1() { } };
            MockComplexObject1 complexObject = new MockComplexObject1 { Property1 = new MockComplexObject1() };

            SubmitOperation submitOp = null;
            LoadOperation<MockEntity3> loadOp = null;
            MockEntity3 entity = null;

            Uri uri = new Uri(TestURIs.RootURI, "TestDomainServices-NamedUpdates-NamedUpdate_CustomValidation.svc");
            NamedUpdate_CustomValidation ctx = null;

            Action<object, ValidationResult, ValidationResult, ValidationResult> paramVariation =
                (invalidParam, registeredResult, expectedClientResult, expectedServerResult) =>
                {
                    this.EnqueueCallback(() =>
                    {
                        ctx = new NamedUpdate_CustomValidation(uri);
                        loadOp = ctx.Load(ctx.GetEntities3Query(), TestHelperMethods.DefaultOperationAction, null);
                    });

                    this.EnqueueConditional(() => loadOp.IsComplete);

                    this.EnqueueCallback(() =>
                    {
                        TestHelperMethods.AssertOperationSuccess(loadOp);
                        entity = loadOp.Entities.First();

                        // reset property values
                        entity.ValidatedProperty = null;
                        complexArray[0].ValidatedProperty = null;
                        complexObject.ValidatedProperty = null;
                        complexObject.Property1.ValidatedProperty = null;

                        // make the correct param invalid.
                        string invalidProperty = "Invalid";
                        bool deepValidation;
                        if (invalidParam == null)
                        {
                            entity.ValidatedProperty = invalidProperty;
                            deepValidation = false;
                        }
                        else if (invalidParam == complexArray)
                        {
                            complexArray[0].ValidatedProperty = invalidProperty;
                            deepValidation = true;
                        }
                        else if (invalidParam == complexObject)
                        {
                            complexObject.ValidatedProperty = invalidProperty;
                            deepValidation = false;
                        }
                        else
                        {
                            complexObject.Property1.ValidatedProperty = invalidProperty;
                            deepValidation = true;
                        }

                        // make property invalid
                        DynamicTestValidator.Reset();
                        DynamicTestValidator.ForcedValidationResults.Add(invalidProperty, registeredResult);

                        // deep property validation should not throw on call
                        if (deepValidation)
                        {
                            entity.NamedUpdateWithPropValidation(complexArray, complexObject);
                            ctx.MockEntity3s.Clear();
                            entity.Reset();
                            ctx.MockEntity3s.Attach(entity);
                        }
                        // shallow property validation should throw on call
                        else
                        {
                            ValidationException vex = ExceptionHelper.ExpectValidationException(() =>
                            {
                                entity.NamedUpdateWithPropValidation(complexArray, complexObject);
                            }, expectedClientResult.ErrorMessage, typeof(CustomValidationAttribute), invalidProperty);
                            // The exception varies from the expected because the framework does not alter the result.
                            // This means the expected MemberNames is equivalent to the registered MemberNames
                            UnitTestHelper.AssertValidationResultsAreEqual(new ValidationResult[] { new ValidationResult(expectedClientResult.ErrorMessage, registeredResult.MemberNames) }, new ValidationResult[] { vex.ValidationResult });
                        }

                        // ensure property validation fails on client submit
                        DynamicTestValidator.Reset();
                        entity.NamedUpdateWithPropValidation(complexArray, complexObject);

                        // make param invalid once again
                        DynamicTestValidator.ForcedValidationResults.Add(invalidProperty, registeredResult);

                        // actually submit
                        submitOp = ctx.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                    });

                    this.EnqueueConditional(() => submitOp.IsComplete);

                    this.EnqueueCallback(() =>
                    {
                        Assert.IsTrue(submitOp.HasError, "No error on client submit of an incorrect entity.");

                        // compare to expected
                        UnitTestHelper.AssertValidationResultsAreEqual(new ValidationResult[] { expectedClientResult }, entity.ValidationResultCollection);

                        // ensure property validation fails on server submit
                        DynamicTestValidator.Reset();
                        submitOp = ctx.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                    });

                    this.EnqueueConditional(() => submitOp.IsComplete);

                    this.EnqueueCallback(() =>
                    {
                        Assert.IsTrue(submitOp.HasError, "No error on server submit of an incorrect entity.");

                        // compare to expected
                        IEnumerable<ValidationResult> expectedServerValidationResults = new ValidationResult[] { expectedServerResult };
                        UnitTestHelper.AssertValidationResultsAreEqual(expectedServerValidationResults, entity.ValidationResultCollection);
                    });
                };

            // Run variations
            string clientFormat1 = "{0} invalid on the client";
            string propName = "ValidatedProperty";
            string serverErrorMessage = DynamicTestValidator.GetMemberError("Property validation failed.", propName);
            string methodName = "NamedUpdateWithPropValidation.";

            paramVariation(null,
                CustomMethodTests.CreateRegisteredValidationResult(clientFormat1, propName),
                CustomMethodTests.CreateExpectedPropertyValidationResult(clientFormat1, propName),
                CustomMethodTests.CreateExpectedValidationResult(serverErrorMessage, propName));

            string memberPath = methodName + "array()";
            paramVariation(complexArray,
                CustomMethodTests.CreateRegisteredValidationResult(clientFormat1, propName),
                CustomMethodTests.CreateExpectedPropertyValidationResult(clientFormat1, propName, memberPath),
                CustomMethodTests.CreateExpectedValidationResult(serverErrorMessage, propName, memberPath));

            memberPath = methodName + "complexObject";
            paramVariation(complexObject,
                CustomMethodTests.CreateRegisteredValidationResult(clientFormat1, propName),
                CustomMethodTests.CreateExpectedPropertyValidationResult(clientFormat1, propName, memberPath),
                CustomMethodTests.CreateExpectedValidationResult(serverErrorMessage, propName, memberPath));

            memberPath = methodName + "complexObject.Property1";
            paramVariation(complexObject.Property1,
                CustomMethodTests.CreateRegisteredValidationResult(clientFormat1, propName),
                CustomMethodTests.CreateExpectedPropertyValidationResult(clientFormat1, propName, memberPath),
                CustomMethodTests.CreateExpectedValidationResult(serverErrorMessage, propName, memberPath));

            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verifies that custom update with identical nested properties are not pushed down through ValidationResultCollection.")]
        public void NamedUpdate_CustomValidation_CommonProperties()
        {
            string methodName = "NamedUpdateWithCommonProperties";
            MockComplexObject3 complexObject = CustomMethodTests.CreateMockComplexObject3();

            SubmitOperation submitOp = null;
            LoadOperation<MockEntity5> loadOp = null;
            MockEntity5 entity = null;

            Uri uri = new Uri(TestURIs.RootURI, "TestDomainServices-NamedUpdates-NamedUpdate_CustomValidation.svc");
            NamedUpdate_CustomValidation ctx = null;

            Action<MockComplexObject4, ValidationResult, ValidationResult> paramVariation =
                (invalidObject, registeredResult, expectedClientResult) =>
                {
                    this.EnqueueCallback(() =>
                        {
                            ctx = new NamedUpdate_CustomValidation(uri);
                            loadOp = ctx.Load(ctx.GetEntities5Query(), TestHelperMethods.DefaultOperationAction, null);
                        });

                    this.EnqueueConditional(() => loadOp.IsComplete);

                    this.EnqueueCallback(() =>
                        {
                            TestHelperMethods.AssertOperationSuccess(loadOp);
                            entity = loadOp.Entities.First();
                            entity.NamedUpdateWithCommonProperties(complexObject);

                            // Make only the ComplexObject's property invalid. This should produce an error
                            // that can't be pushed down into the entity's property's collection.
                            DynamicTestValidator.ForcedValidationResults.Add(invalidObject, registeredResult);

                            // Submit and watch it fail.
                            submitOp = ctx.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                        });

                    this.EnqueueConditional(() => submitOp.IsComplete);

                    this.EnqueueCallback(() =>
                        {
                            Assert.IsTrue(submitOp.HasError, "Client call: Submit should have failed.");

                            // The client should fail validation of only the argument's property.
                            Assert.AreEqual(1, entity.ValidationErrors.Count(), "Client call: Entity should have an error.");
                            Assert.AreEqual(0, entity.CommonProperty.ValidationErrors.Count(), "Client call: Validation error was pushed down to property.");
                            Assert.AreEqual(0, entity.CommonArray[0].ValidationErrors.Count(), "Client call: Validation error was pushed down to array.");

                            // Reset the validator and get the result from the server.
                            DynamicTestValidator.Reset();
                            submitOp = ctx.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                        });

                    this.EnqueueConditional(() => submitOp.IsComplete);

                    this.EnqueueCallback(() =>
                        {
                            Assert.IsTrue(submitOp.HasError, "Server call: Submit should have failed.");
                            // The server should fail validation of all MockEntity4 types.
                            IEnumerable<ValidationResult> expectedServerValidationResults = CustomMethodTests.GetExpectedErrors(methodName);
                            UnitTestHelper.AssertValidationResultsAreEqual(expectedServerValidationResults, entity.ValidationResultCollection);
                            Assert.AreEqual(1, entity.CommonProperty.ValidationErrors.Count(), "Server call: Validation error was pushed down to property.");

                            // Errors cannot get pushed to arrays.
                            Assert.AreEqual(0, entity.CommonArray[0].ValidationErrors.Count(), "Server call: Validation error was pushed down to array.");
                        });
                };

            string format1 = "{0} invalid on client.";
            string paramPrefix = methodName + ".complexObject.";
            paramVariation(complexObject.CommonProperty,
                CustomMethodTests.CreateRegisteredValidationResult(format1, typeof(MockComplexObject4), "CommonProperty.PlacehoderName"),
                CustomMethodTests.CreateExpectedValidationResult(format1, typeof(MockComplexObject4), false, "CommonProperty.PlacehoderName"));
            paramVariation(complexObject.CommonProperty.Property1,
                CustomMethodTests.CreateRegisteredValidationResult(format1, typeof(MockComplexObject4), "CommonProperty.Property1.PlacehoderName"),
                CustomMethodTests.CreateExpectedValidationResult(format1, typeof(MockComplexObject4), false, "CommonProperty.Property1.PlacehoderName"));
            paramVariation(complexObject.CommonArray[0],
                CustomMethodTests.CreateRegisteredValidationResult(format1, typeof(MockComplexObject4), "CommonArray().PlacehoderName"),
                CustomMethodTests.CreateExpectedValidationResult(format1, typeof(MockComplexObject4), false, paramPrefix + "CommonArray().PlacehoderName"));
            paramVariation(complexObject.CommonArray[0].Property1,
                CustomMethodTests.CreateRegisteredValidationResult(format1, typeof(MockComplexObject4), "CommonArray().Property1.PlacehoderName"),
                CustomMethodTests.CreateExpectedValidationResult(format1, typeof(MockComplexObject4), false, paramPrefix + "CommonArray().Property1.PlacehoderName"));

            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verifies that custom update type validation runs successfully when the entity does not require validation.")]
        public void NamedUpdate_CustomValidation_NoEntityValidation()
        {
            string methodName = "NamedUpdateWithNoEntityValidation";
            MockComplexObject2 complexObject = new MockComplexObject2();

            string format1 = "{0} invalid on client.";
            ValidationResult registeredResult = CustomMethodTests.CreateRegisteredValidationResult(format1, typeof(MockComplexObject2));
            ValidationResult expectedClientResult = CustomMethodTests.CreateExpectedValidationResult(format1, typeof(MockComplexObject2), true, methodName + ".complexObject.PlaceholderName");

            SubmitOperation submitOp = null;
            LoadOperation<MockEntity6> loadOp = null;
            MockEntity6 entity = null;

            Uri uri = new Uri(TestURIs.RootURI, "TestDomainServices-NamedUpdates-NamedUpdate_CustomValidation.svc");
            NamedUpdate_CustomValidation ctx = null;

            this.EnqueueCallback(() =>
                {
                    ctx = new NamedUpdate_CustomValidation(uri);
                    loadOp = ctx.Load(ctx.GetEntities6Query(), TestHelperMethods.DefaultOperationAction, null);
                });

            this.EnqueueConditional(() => loadOp.IsComplete);

            this.EnqueueCallback(() =>
                {
                    TestHelperMethods.AssertOperationSuccess(loadOp);
                    entity = loadOp.Entities.First();

                    // Make the param invalid and the call should fail.
                    DynamicTestValidator.ForcedValidationResults.Add(typeof(MockComplexObject2), registeredResult);
                    ExceptionHelper.ExpectValidationException(() =>
                        {
                            entity.NamedUpdateWithNoEntityValidation(complexObject);
                        }, expectedClientResult.ErrorMessage, typeof(CustomValidationAttribute), complexObject);

                    DynamicTestValidator.Reset();
                    entity.NamedUpdateWithNoEntityValidation(complexObject);

                    // This time the client submit should fail.
                    DynamicTestValidator.ForcedValidationResults.Add(typeof(MockComplexObject2), registeredResult);
                    submitOp = ctx.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                });

            this.EnqueueConditional(() => submitOp.IsComplete);

            this.EnqueueCallback(() =>
                {
                    Assert.IsTrue(submitOp.HasError, "Client call: Submit should have failed.");
                    UnitTestHelper.AssertValidationResultsAreEqual(new ValidationResult[] { expectedClientResult }, entity.ValidationErrors);

                    // Now the server submit should fail.
                    DynamicTestValidator.Reset();
                    submitOp = ctx.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                });

            this.EnqueueConditional(() => submitOp.IsComplete);

            this.EnqueueCallback(() =>
                {
                    Assert.IsTrue(submitOp.HasError, "Server call: Submit should have failed.");
                    var expectedServerResults = CustomMethodTests.GetExpectedErrors(methodName);
                    UnitTestHelper.AssertValidationResultsAreEqual(expectedServerResults, entity.ValidationErrors);
                });

            this.EnqueueTestComplete();
        }
        #endregion // Named Update Methods

        #region Helpers
        private void AssertCompletedWithoutErrors(OperationBase operation)
        {
            Assert.IsTrue(operation.IsComplete, "Expected operation to complete.");
            Assert.IsFalse(operation.IsCanceled, "Expected operation to complete without cancelling.");
            Assert.IsFalse(operation.HasError, "Expected operation to complete without errors.");
            Assert.IsNull(operation.Error, "Expected operation to complete without errors.");
        }

        private void AssertCompletedWithError(OperationBase operation, string errorMessage)
        {
            Assert.IsTrue(operation.IsComplete, "Expected operation to complete.");
            Assert.IsFalse(operation.IsCanceled, "Expected operation to complete without cancelling.");
            Assert.IsTrue(operation.HasError, "Expected operation to complete with an error.");
            Assert.IsNotNull(operation.Error, "Expected operation to complete with an error.");
            Assert.AreEqual(errorMessage, operation.Error.Message);
        }

        private static ValidationResult CreateRegisteredValidationResult(string format1, Type paramType, string memberName = "PlaceholderName")
        {
            string errorMessage = string.Format(format1, paramType.Name);
            return new ValidationResult(errorMessage, new string[] { memberName });
        }

        private static ValidationResult CreateRegisteredValidationResult(string format1, string memberName)
        {
            string errorMessage = string.Format(format1, memberName);
            return new ValidationResult(errorMessage, new string[] { memberName });
        }

        private static ValidationResult CreateExpectedValidationResult(string format1, Type paramType, bool typeError, string memberName = "PlaceholderName")
        {
            // Basic error
            string errorMessage = string.Format(format1, paramType.Name);

            // Apply Test Validator transformation
            if (typeError)
            {
                errorMessage = DynamicTestValidator.GetTypeError(errorMessage);
            }

            return CustomMethodTests.CreateExpectedValidationResult(errorMessage, memberName);
        }

        private static ValidationResult CreateExpectedPropertyValidationResult(string format1, string memberName, string memberPath = "")
        {
            // Basic error
            string errorMessage = string.Format(format1, memberName);
            
            // Apply Test Validator transformation
            errorMessage = DynamicTestValidator.GetMemberError(errorMessage, memberName);
            string dottedPath = memberName;
            if (!string.IsNullOrEmpty(memberPath))
            {
                dottedPath = memberPath + "." + memberName;
            }

            return CustomMethodTests.CreateExpectedValidationResult(errorMessage, dottedPath);
        }

        private static ValidationResult CreateExpectedValidationResult(string errorMessage, string memberName, string memberPath = "")
        {
            if (!string.IsNullOrEmpty(memberPath))
            {
                memberName = memberPath + "." + memberName;
            }
            string[] memberNames = new string[] { memberName };
            return new ValidationResult(errorMessage, memberNames);
        }

        private static IEnumerable<ValidationResult> GetExpectedErrors(string method)
        {
            string format1 = "Validation failed. {0}";

            if (method == "NamedUpdateWithParamValidation")
            {
                // This method fails with param validation for all 3 parameters.
                return new ValidationResult[]
                {
                    CustomMethodTests.CreateExpectedValidationResult(format1, typeof(MockEntity3), true),
                    CustomMethodTests.CreateExpectedValidationResult(format1, typeof(MockComplexObject1[]), true, "NamedUpdateWithParamValidation.array().PlaceholderName"),
                    CustomMethodTests.CreateExpectedValidationResult(format1, typeof(MockComplexObject1), true, "NamedUpdateWithParamValidation.complexObject.PlaceholderName"),
                };
            }
            else if (method == "NamedUpdateWithTypeValidation")
            {
                return new ValidationResult[]
                {
                    CustomMethodTests.CreateExpectedValidationResult(format1, typeof(MockEntity4), true),
                    CustomMethodTests.CreateExpectedValidationResult(format1, typeof(MockComplexObject2), true, "NamedUpdateWithTypeValidation.array().PlaceholderName"),
                    CustomMethodTests.CreateExpectedValidationResult(format1, typeof(MockComplexObject2), true, "NamedUpdateWithTypeValidation.complexObject.Property1.PlaceholderName"),

                };
            }
            else if (method == "NamedUpdateWithCommonProperties")
            {
                string paramPrefix = "NamedUpdateWithCommonProperties.complexObject.";
                return new ValidationResult[]
                {
                    // entity errors
                    CustomMethodTests.CreateExpectedValidationResult(format1, typeof(MockComplexObject4), true, "CommonProperty.Property1.PlaceholderName"),
                    CustomMethodTests.CreateExpectedValidationResult(format1, typeof(MockComplexObject4), true, "CommonArray().Property1.PlaceholderName"),
                    // param errors
                    CustomMethodTests.CreateExpectedValidationResult(format1, typeof(MockComplexObject4), true, paramPrefix + "CommonProperty.Property1.PlaceholderName"),
                    CustomMethodTests.CreateExpectedValidationResult(format1, typeof(MockComplexObject4), true, paramPrefix + "CommonArray().Property1.PlaceholderName"),
                };
            }
            else if (method == "NamedUpdateWithNoEntityValidation")
            {
                return new ValidationResult[]
                {
                    CustomMethodTests.CreateExpectedValidationResult(format1, typeof(MockComplexObject2), true, "NamedUpdateWithNoEntityValidation.complexObject.PlaceholderName"),
                };
            }

            throw new NotSupportedException();
        }

        private static MockComplexObject3 CreateMockComplexObject3()
        {
            return new MockComplexObject3()
            {
                CommonProperty = new MockComplexObject4()
                {
                    Property1 = new MockComplexObject4(),
                },
                CommonArray = new MockComplexObject4[]
                {
                    new MockComplexObject4()
                    {
                        Property1 = new MockComplexObject4(),
                    }
                },
            };
        }

        public class TestEntityContainer : EntityContainer
        {
            public TestEntityContainer()
            {
                CreateEntitySet<City>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
                CreateEntitySet<Zip>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
                CreateEntitySet<State>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
                CreateEntitySet<County>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
            }
        }
        #endregion
    }
}
