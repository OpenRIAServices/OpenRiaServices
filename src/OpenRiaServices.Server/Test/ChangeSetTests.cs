using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Client.Test;
using TestDomainServices;

namespace OpenRiaServices.Server.Test
{
    using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

    [TestClass]
    public class ChangeSetTests
    {
        /// <summary>
        /// Verify ChangeSet validation when specifying/requesting original for Insert operations.
        /// </summary>
        [TestMethod]
        public void Changeset_OriginalInvalidForInserts()
        {
            // can't specify an original for an insert operation
            TimestampEntityA curr = new TimestampEntityA { ID = 1, Version = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 }, ValueA = "Foo", ValueB = "Bar" } ;
            TimestampEntityA orig = new TimestampEntityA { ID = 1, Version = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 }, ValueA = "x", ValueB = "x" };
            ChangeSetEntry entry = new ChangeSetEntry(1, curr, orig, DomainOperation.Insert);
            ChangeSet cs = null;
            ExceptionHelper.ExpectInvalidOperationException(delegate 
            {
                cs = new ChangeSet(new ChangeSetEntry[] { entry });
            }, 
            string.Format(Resource.InvalidChangeSet, Resource.InvalidChangeSet_InsertsCantHaveOriginal));

            // get original should throw for insert operations
            entry = new ChangeSetEntry(1, curr, null, DomainOperation.Insert);
            cs = new ChangeSet(new ChangeSetEntry[] { entry });
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                cs.GetOriginal(curr);
            },
            string.Format(Resource.ChangeSet_OriginalNotValidForInsert));
        }

        /// <summary>
        /// Test various combinations of invalid parameters
        /// </summary>
        [TestMethod]
        public void Changeset_GetAssociatedChanges_ParamValidation()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(CompositionScenarios_Explicit));

            // test where entity passed in does not exist in the changeset
            ChangeSet cs = new ChangeSet(new ChangeSetEntry[] { new ChangeSetEntry(1, new Parent { Children = new List<Child> { new Child() } }, null, DomainOperation.Insert) });
            Parent parent = cs.ChangeSetEntries.Select(p => p.Entity).Cast<Parent>().First();
            ExceptionHelper.ExpectArgumentException(delegate
            {
                cs.GetAssociatedChanges(new Parent(), p => p.Children);
            }, Resource.ChangeSet_ChangeSetEntryNotFound, "entity");

            // test where the changeset is empty
            cs = new ChangeSet(Array.Empty<ChangeSetEntry>());
            ExceptionHelper.ExpectArgumentException(delegate
            {
                cs.GetAssociatedChanges(new Parent(), p => p.Children);
            }, Resource.ChangeSet_ChangeSetEntryNotFound, "entity");

            ExceptionHelper.ExpectArgumentNullException(delegate
            {
                cs.GetAssociatedChanges(null, (Parent p) => p.Children);
            }, "entity");

            ExceptionHelper.ExpectArgumentNullException(delegate
            {
                cs.GetAssociatedChanges(parent, (Expression<Func<Parent, Child>>)null);
            }, "expression");

            // try to pass in a non compositional member
            cs = new ChangeSet(new ChangeSetEntry[] { new ChangeSetEntry(1, new Parent { Children = new List<Child> { new Child() } }, null, DomainOperation.Insert) });
            parent = cs.ChangeSetEntries.Select(p => p.Entity).Cast<Parent>().First();
            ExceptionHelper.ExpectArgumentException(delegate
            {
                cs.GetAssociatedChanges(parent, p => p.OperationResult);
            }, string.Format(Resource.MemberNotAnAssociation, typeof(CompositionEntityBase), "OperationResult"), "expression");
        }

        /// <summary>
        /// Verify GetAssociatedChanges for a collection member
        /// </summary>
        [TestMethod]
        public void Changeset_GetAssociatedChanges_Enumerable()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(CompositionScenarios_Explicit));

            #region build up a compositional hierarchy
            int id = 1;
            Parent currParent = new Parent();
            List<ChangeSetEntry> changeSetEntries = new List<ChangeSetEntry>();
            ChangeSetEntry parentUpdateOperation = new ChangeSetEntry(id++, currParent, null, DomainOperation.Update);
            List<int> currentAssociatedChildren = new List<int>();
            changeSetEntries.Add(parentUpdateOperation);

            // add 3 unmodified children
            List<Child> unmodifiedChildren = new List<Child>() { new Child(), new Child(), new Child() };
            foreach (Child c in unmodifiedChildren)
            {
                ChangeSetEntry operation = new ChangeSetEntry(id++, c, c, DomainOperation.None);
                changeSetEntries.Add(operation);
                currParent.Children.Add(c);
                currentAssociatedChildren.Add(operation.Id);
            }

            // add 2 child edits
            List<Child> originalEditedChildren = new List<Child> { new Child(), new Child() };
            List<Child> currentEditedChildren = new List<Child> { new Child(), new Child() };
            for (int i = 0; i < originalEditedChildren.Count; i++)
            {
                Child currChild = currentEditedChildren[i];
                Child origChild = originalEditedChildren[i];
                ChangeSetEntry operation = new ChangeSetEntry(id++, currChild, origChild, DomainOperation.Update);
                changeSetEntries.Add(operation);
                currParent.Children.Add(currChild);
                currentAssociatedChildren.Add(operation.Id);
            }

            // add a 2 new children
            List<Child> newChildren = new List<Child> { new Child(), new Child() };
            foreach (Child c in newChildren)
            {
                ChangeSetEntry operation = new ChangeSetEntry(id++, c, null, DomainOperation.Insert);
                changeSetEntries.Add(operation);
                currParent.Children.Add(c);
                currentAssociatedChildren.Add(operation.Id);
            }

            // add 2 removes by adding operations for the deleted children
            // and setting up the original association
            List<Child> removedChildren = new List<Child> { new Child(), new Child() };
            List<int> deletedChildren = new List<int>();
            foreach (Child c in removedChildren)
            {
                int deletedId = id++;
                changeSetEntries.Add(new ChangeSetEntry(deletedId, c, c, DomainOperation.Delete));
                deletedChildren.Add(deletedId);
            }
            parentUpdateOperation.OriginalAssociations = new Dictionary<string, int[]>() { { "Children", deletedChildren.ToArray() } };
            parentUpdateOperation.Associations = new Dictionary<string, int[]>() { { "Children", currentAssociatedChildren.ToArray() } };
            #endregion

            // verify unmodified children
            ChangeSet cs = new ChangeSet(changeSetEntries);
            IEnumerable<Child> childChanges = cs.GetAssociatedChanges(currParent, p => p.Children, ChangeOperation.None).Cast<Child>();
            Assert.AreEqual(3, childChanges.Count());
            foreach (Child c in childChanges)
            {
                Assert.IsTrue(unmodifiedChildren.Contains(c));
            }

            // verify inserted children
            childChanges = cs.GetAssociatedChanges(currParent, p => p.Children, ChangeOperation.Insert).Cast<Child>();
            Assert.AreEqual(2, childChanges.Count());
            foreach (Child c in childChanges)
            {
                Assert.IsTrue(newChildren.Contains(c));
            }

            // verify deleted children
            childChanges = cs.GetAssociatedChanges(currParent, p => p.Children, ChangeOperation.Delete).Cast<Child>();
            Assert.AreEqual(2, childChanges.Count());
            foreach (Child c in childChanges)
            {
                Assert.IsTrue(removedChildren.Contains(c));
            }

            // verify modified children
            childChanges = cs.GetAssociatedChanges(currParent, p => p.Children, ChangeOperation.Update).Cast<Child>();
            Assert.AreEqual(2, childChanges.Count());
            foreach (Child c in childChanges)
            {
                Assert.IsTrue(currentEditedChildren.Contains(c));
            }

            // verify overload that returns all - should return all unmodified
            // in addition to all changed
            IEnumerable<Child> allChildren = cs.GetAssociatedChanges(currParent, p => p.Children).Cast<Child>();
            Assert.AreEqual(9, allChildren.Count());
            foreach (Child c in allChildren)
            {
                ChangeOperation operationType = cs.GetChangeOperation(c);
                switch (operationType)
                {
                    case ChangeOperation.None:
                        Assert.IsTrue(unmodifiedChildren.Contains(c));
                        break;
                    case ChangeOperation.Insert:
                        Assert.IsTrue(newChildren.Contains(c));
                        break;
                    case ChangeOperation.Update:
                        Assert.IsTrue(currentEditedChildren.Contains(c));
                        break;
                    case ChangeOperation.Delete:
                        Assert.IsTrue(removedChildren.Contains(c));
                        break;
                };
            }

            // verify calls that return empty
            cs = new ChangeSet(new ChangeSetEntry[] { new ChangeSetEntry(1, currParent, null, DomainOperation.None) });
            IEnumerable<Child> children = cs.GetAssociatedChanges(currParent, p => p.Children).Cast<Child>();
            Assert.AreEqual(0, children.Count());
            children = cs.GetAssociatedChanges(currParent, p => p.Children).Cast<Child>();
            Assert.AreEqual(0, children.Count());

            // test null collection properties
            currParent = new Parent { Children = null };
            cs = new ChangeSet(new ChangeSetEntry[] { new ChangeSetEntry(1, currParent, new Parent { Children = null }, DomainOperation.None) });
            Assert.AreEqual(0, cs.GetAssociatedChanges(currParent, p => p.Children).Cast<Child>().Count());
            Assert.AreEqual(0, cs.GetAssociatedChanges(currParent, p => p.Children, ChangeOperation.None).Cast<Child>().Count());
        }

        [TestMethod]
        public void Changeset_GetAssociatedChanges_Singleton()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(CompositionScenarios_Explicit));

            // verify singleton change of None
            GreatGrandChild unmodifiedGgc = new GreatGrandChild();
            GrandChild currGrandChild = new GrandChild
            {
                Child = unmodifiedGgc
            };
            GrandChild origGrandChild = new GrandChild
            {
                Child = unmodifiedGgc
            };
            ChangeSetEntry gcOperation = new ChangeSetEntry(1, currGrandChild, origGrandChild, DomainOperation.Update);
            gcOperation.Associations = new Dictionary<string, int[]> { { "Child", new int[] { 2 } } };
            ChangeSetEntry ggcOperation = new ChangeSetEntry(2, unmodifiedGgc, null, DomainOperation.None);
            ChangeSet cs = new ChangeSet(new ChangeSetEntry[] { gcOperation, ggcOperation });
            GreatGrandChild ggcChange = cs.GetAssociatedChanges(currGrandChild, p => p.Child, ChangeOperation.None).Cast<GreatGrandChild>().SingleOrDefault();
            Assert.AreSame(unmodifiedGgc, ggcChange);

            // verify singleton insert
            GreatGrandChild newGgc = new GreatGrandChild();
            currGrandChild = new GrandChild
            {
                Child = newGgc
            };
            origGrandChild = new GrandChild
            {
                Child = null
            };
            gcOperation = new ChangeSetEntry(1, currGrandChild, origGrandChild, DomainOperation.Update);
            gcOperation.Associations = new Dictionary<string, int[]> { { "Child", new int[] { 2 } } };
            ggcOperation = new ChangeSetEntry(2, newGgc, null, DomainOperation.Insert);
            cs = new ChangeSet(new ChangeSetEntry[] { gcOperation, ggcOperation });
            ggcChange = cs.GetAssociatedChanges(currGrandChild, p => p.Child, ChangeOperation.Insert).Cast<GreatGrandChild>().SingleOrDefault();
            Assert.AreSame(newGgc, ggcChange);
            Assert.AreEqual(ChangeOperation.Insert, cs.GetChangeOperation(newGgc));

            // verify singleton update
            GreatGrandChild modifiedGgc = new GreatGrandChild();
            currGrandChild = new GrandChild
            {
                Child = modifiedGgc
            };
            origGrandChild = new GrandChild
            {
                Child = modifiedGgc
            };
            gcOperation = new ChangeSetEntry(1, currGrandChild, origGrandChild, DomainOperation.Update);
            gcOperation.Associations = new Dictionary<string, int[]> { { "Child", new int[] { 2 } } };
            ggcOperation = new ChangeSetEntry(2, modifiedGgc, unmodifiedGgc, DomainOperation.Update);
            cs = new ChangeSet(new ChangeSetEntry[] { gcOperation, ggcOperation });
            ggcChange = cs.GetAssociatedChanges(currGrandChild, p => p.Child, ChangeOperation.Update).Cast<GreatGrandChild>().SingleOrDefault();
            Assert.AreSame(modifiedGgc, ggcChange);
            Assert.AreSame(unmodifiedGgc, cs.GetOriginal(modifiedGgc));
            Assert.AreEqual(ChangeOperation.Update, cs.GetChangeOperation(modifiedGgc));

            // verify singleton delete
            GreatGrandChild deletedGgc = new GreatGrandChild();
            currGrandChild = new GrandChild
            {
                Child = null
            };
            origGrandChild = new GrandChild
            {
                Child = deletedGgc
            };
            gcOperation = new ChangeSetEntry(1, currGrandChild, null, DomainOperation.Update);
            gcOperation.OriginalAssociations = new Dictionary<string, int[]>() { { "Child", new int[] { 2 } } };
            ggcOperation = new ChangeSetEntry(2, deletedGgc, null, DomainOperation.Delete);
            gcOperation.OriginalAssociations = new Dictionary<string, int[]>() { { "Child", new int[] { 2 } } };
            cs = new ChangeSet(new ChangeSetEntry[] { gcOperation, ggcOperation });
            ggcChange = cs.GetAssociatedChanges(currGrandChild, p => p.Child, ChangeOperation.Delete).Cast<GreatGrandChild>().SingleOrDefault();
            Assert.AreSame(deletedGgc, ggcChange);
            Assert.AreSame(null, cs.GetOriginal(deletedGgc));
            Assert.AreEqual(ChangeOperation.Delete, cs.GetChangeOperation(deletedGgc));
        }

        [TestMethod]
        [TestDescription("Verifies that the constructors behave as expected.")]
        public void Constructor_Initialization()
        {
            ChangeSet changeSet;
            IEnumerable<ChangeSetEntry> ops = this.GenerateEntityOperations(false);

            changeSet = new ChangeSet(ops);
            Assert.AreEqual(ops.Count(), ops.Intersect(changeSet.ChangeSetEntries).Count(), "Expected ChangeSetEntries count to match what was provided to the constructor");

            ChangeSetEntry changeSetEntry = new ChangeSetEntry(0, new E(), new E() { E_ID2 = 5 }, DomainOperation.Update);
            Assert.IsTrue(changeSetEntry.HasMemberChanges);
        }

        [TestMethod]
        [TestDescription("Verifies that the constructor throws when invalid arguments are provided")]
        public void Constructor_InvalidArgs()
        {
            ExceptionHelper.ExpectArgumentNullException(
                () => new ChangeSet(null),
                "changeSetEntries");
        }

        [TestMethod]
        [TestDescription("Verifies that errors are reported correctly")]
        public void Constructor_HasErrors()
        {
            ChangeSet changeSet = this.GenerateChangeSet();
            Assert.IsFalse(changeSet.HasError);

            changeSet = this.GenerateChangeSet();
            changeSet.ChangeSetEntries.First().ValidationErrors = new List<ValidationResultInfo>() { new ValidationResultInfo("Error", new[] { "Error" }) };
            Assert.IsTrue(changeSet.HasError, "Expected ChangeSet to have errors");
        }

        [TestMethod]
        [TestDescription("Verifies that the GetOriginal method returns as expected")]
        public void GetOriginal()
        {
            ChangeSet changeSet = this.GenerateChangeSet();
            ChangeSetEntry op = changeSet.ChangeSetEntries.First();

            A currentEntity = new A(), originalEntity = new A();

            op.Entity = currentEntity;
            op.OriginalEntity = originalEntity;

            A changeSetOriginalEntity = changeSet.GetOriginal(currentEntity);

            // Verify we returned the original
            Assert.AreSame(originalEntity, changeSetOriginalEntity, "Expected to find original entity.");
        }

        [TestMethod]
        [TestDescription("Verifies that the GetOriginal method returns as expected")]
        public void GetOriginal_EntityExistsMoreThanOnce()
        {
            ChangeSet changeSet = this.GenerateChangeSet();
            ChangeSetEntry op1 = changeSet.ChangeSetEntries.Skip(0).First();
            ChangeSetEntry op2 = changeSet.ChangeSetEntries.Skip(1).First();
            ChangeSetEntry op3 = changeSet.ChangeSetEntries.Skip(2).First();

            A currentEntity = new A(), originalEntity = new A();

            op1.Entity = currentEntity;
            op1.OriginalEntity = originalEntity;

            op2.Entity = currentEntity;
            op2.OriginalEntity = originalEntity;

            op3.Entity = currentEntity;
            op3.OriginalEntity = null;

            A changeSetOriginalEntity = changeSet.GetOriginal(currentEntity);

            // Verify we returned the original
            Assert.AreSame(originalEntity, changeSetOriginalEntity, "Expected to find original entity.");
        }

        [TestMethod]
        [TestDescription("Verifies that the GetOriginal method reports errors as expected.")]
        public void GetOriginal_InvalidArgs()
        {
            ChangeSet changeSet = this.GenerateChangeSet();

            ExceptionHelper.ExpectArgumentNullException(
                () => changeSet.GetOriginal<A>(null),
                "clientEntity");
        }

        [TestMethod]
        [TestDescription("Verifies that the GetOriginal method reports errors as expected.")]
        public void GetOriginal_EntityOperationNotFound()
        {
            ChangeSet changeSet = this.GenerateChangeSet();

            ExceptionHelper.ExpectArgumentException(
                () => changeSet.GetOriginal(new A()),
                Resource.ChangeSet_ChangeSetEntryNotFound);
        }

        [TestMethod]
        [TestDescription("Verifies that the Replace method behaves as expected.")]
        public void Replace()
        {
            ChangeSet changeSet = this.GenerateChangeSet();
            ChangeSetEntry op = changeSet.ChangeSetEntries.First();

            A currentEntity = new A(), originalEntity = new A(), returnedEntity = new A();

            op.Entity = currentEntity;
            op.OriginalEntity = originalEntity;

            changeSet.Replace(currentEntity, returnedEntity);

            Assert.AreSame(op.Entity, currentEntity, "Expected to find current entity.");
            Assert.AreSame(op.OriginalEntity, originalEntity, "Expected to find original entity.");
            Assert.AreSame(returnedEntity, changeSet.EntitiesToReplace[op.Entity], "Expected to find returned entity.");
        }

        [TestMethod]
        [TestDescription("Verifies that the Replace method behaves as expected.")]
        public void Replace_EntityExistsMoreThanOnce()
        {
            ChangeSet changeSet = this.GenerateChangeSet();
            ChangeSetEntry op1 = changeSet.ChangeSetEntries.Skip(0).First();
            ChangeSetEntry op2 = changeSet.ChangeSetEntries.Skip(1).First();
            ChangeSetEntry op3 = changeSet.ChangeSetEntries.Skip(2).First();

            A currentEntity = new A(), originalEntity = new A(), returnedEntity = new A();

            op1.Entity = currentEntity;
            op1.OriginalEntity = originalEntity;

            op2.Entity = currentEntity;
            op2.OriginalEntity = originalEntity;

            op3.Entity = currentEntity;
            op3.OriginalEntity = null;

            changeSet.Replace(currentEntity, returnedEntity);

            // Verify we returned the original
            Assert.AreSame(op1.Entity, currentEntity, "Expected to find the current entity.");
            Assert.AreSame(op1.OriginalEntity, originalEntity, "Expected to find the original entity.");
            Assert.AreSame(returnedEntity, changeSet.EntitiesToReplace[op1.Entity], "Expected to find the returned entity.");
            Assert.AreSame(op2.Entity, currentEntity, "Expected to find the current entity.");
            Assert.AreSame(op2.OriginalEntity, originalEntity, "Expected to find the original entity.");
            Assert.AreSame(returnedEntity, changeSet.EntitiesToReplace[op2.Entity], "Expected to find the returned entity.");
            Assert.AreSame(op3.Entity, currentEntity, "Expected to find the current entity.");
            Assert.IsNull(op3.OriginalEntity, "Expected to find a null original entity.");
            Assert.AreSame(returnedEntity, changeSet.EntitiesToReplace[op3.Entity], "Expected to find the returned entity.");
        }

        [TestMethod]
        [TestDescription("Verifies that the Replace method reports argument errors as expected.")]
        public void Replace_InvalidArgs()
        {
            ChangeSet changeSet = this.GenerateChangeSet();

            ExceptionHelper.ExpectArgumentNullException(
                () => changeSet.Replace(null, new A()),
                "clientEntity");

            ExceptionHelper.ExpectArgumentNullException(
                () => changeSet.Replace(new A(), null),
                "returnedEntity");
        }

        [TestMethod]
        [TestDescription("Verifies that Replace method reports errors as expected when entities are related by inheritance.")]
        public void Associate_InvalidArgs_Inheritance()
        {
            ChangeSet changeSet = this.GenerateChangeSet();

            ExceptionHelper.ExpectInvalidOperationException(
                () => changeSet.Replace(new MockEntity1(), new MockDerivedEntity()),
                string.Format(Resource.ChangeSet_Replace_EntityTypesNotSame, typeof(MockEntity1), typeof(MockDerivedEntity)));
        }

        [TestMethod]
        [TestDescription("Verifies that the Replace method reports 'not found' exceptions as expected.")]
        public void Replace_EntityOperationNotFound()
        {
            ChangeSet changeSet = this.GenerateChangeSet();

            ExceptionHelper.ExpectArgumentException(
                () => changeSet.Replace(new A(), new A()),
                Resource.ChangeSet_ChangeSetEntryNotFound,
                "entity");
        }

        [TestMethod]
        [TestDescription("Verifies that Associate method reports argument errors as expected.")]
        public void Associate_InvalidArgs()
        {
            ChangeSet changeSet = this.GenerateChangeSet();

            ExceptionHelper.ExpectArgumentNullException(
                () => changeSet.Associate<object, object>(null, new object(), (a, b) => { }),
                "clientEntity");

            ExceptionHelper.ExpectArgumentNullException(
                () => changeSet.Associate<object, object>(new object(), null, (a, b) => { }),
                "storeEntity");

            ExceptionHelper.ExpectArgumentNullException(
                () => changeSet.Associate<object, object>(new object(), new object(), null),
                "storeToClientTransform");
        }

        [TestMethod]
        [TestDescription("Verifies that associated entity transforms behave as expected.")]
        public void Associate_EntityTransform()
        {
            ChangeSet changeSet = this.GenerateChangeSet();

            MockEntity1 clientEntity = (MockEntity1)changeSet.ChangeSetEntries.First().Entity;
            MockStoreEntity storeEntity = new MockStoreEntity()
            {
                ID = 1,
                FirstName = "StoreFName",
                LastName = "StoreLName"
            };

            changeSet.Associate(
                clientEntity,
                storeEntity,
                (c, s) => c.FullName = (s.FirstName + " " + s.LastName));

            Assert.AreEqual("FName0 LName0", clientEntity.FullName, "Expected clientEntity FullName to remain unchanged.");
            Assert.AreEqual("StoreFName", storeEntity.FirstName, "Expected storeEntity FirstName to remain unchanged.");
            Assert.AreEqual("StoreLName", storeEntity.LastName, "Expected storeEntity LastName to remain unchanged.");

            changeSet.ApplyAssociatedStoreEntityTransforms();

            Assert.AreEqual("StoreFName StoreLName", clientEntity.FullName, "Expected clientEntity FullName to have changed.");
            Assert.AreEqual("StoreFName", storeEntity.FirstName, "Expected storeEntity FirstName to remain unchanged.");
            Assert.AreEqual("StoreLName", storeEntity.LastName, "Expected storeEntity LastName to remain unchanged.");
        }

        [TestMethod]
        [TestDescription("Verifies that associated multiple entity transforms behave as expected.")]
        public void Associate_EntityTransform_Multiple()
        {
            ChangeSet changeSet = this.GenerateChangeSet();

            MockEntity1 clientEntity = (MockEntity1)changeSet.ChangeSetEntries.First().Entity;
            MockStoreEntity storeEntity1 = new MockStoreEntity()
            {
                ID = 1,
                FirstName = "Store1FName",
                LastName = "Store1LName"
            };
            MockStoreEntity storeEntity2 = new MockStoreEntity()
            {
                ID = 2,
                FirstName = "Store2FName",
                LastName = "Store2LName"
            };

            changeSet.Associate(clientEntity, storeEntity1, (c, s) => c.FullName += s.FirstName);
            changeSet.Associate(clientEntity, storeEntity2, (c, s) => c.FullName += s.FirstName);

            Assert.AreEqual("FName0 LName0", clientEntity.FullName, "Expected clientEntity FullName to remain unchanged.");
            Assert.AreEqual("Store1FName", storeEntity1.FirstName, "Expected storeEntity1 FirstName to remain unchanged.");
            Assert.AreEqual("Store1LName", storeEntity1.LastName, "Expected storeEntity1 LastName to remain unchanged.");
            Assert.AreEqual("Store2FName", storeEntity2.FirstName, "Expected storeEntity2 FirstName to remain unchanged.");
            Assert.AreEqual("Store2LName", storeEntity2.LastName, "Expected storeEntity2 LastName to remain unchanged.");

            changeSet.ApplyAssociatedStoreEntityTransforms();

            Assert.AreEqual("FName0 LName0Store1FNameStore2FName", clientEntity.FullName, "Expected clientEntity FullName to have changed.");
            Assert.AreEqual("Store1FName", storeEntity1.FirstName, "Expected storeEntity1 FirstName to remain unchanged.");
            Assert.AreEqual("Store1LName", storeEntity1.LastName, "Expected storeEntity1 LastName to remain unchanged.");
            Assert.AreEqual("Store2FName", storeEntity2.FirstName, "Expected storeEntity2 FirstName to remain unchanged.");
            Assert.AreEqual("Store2LName", storeEntity2.LastName, "Expected storeEntity2 LastName to remain unchanged.");
        }

        [TestMethod]
        [TestDescription("Verifies that the appropriate exception is thrown for entities not in the ChangeSet.")]
        public void Associate_EntityNotInChangeSet()
        {
            ChangeSet changeSet = this.GenerateChangeSet();

            ExceptionHelper.ExpectArgumentException(
                () => changeSet.Associate(new MockEntity1(), new MockStoreEntity(), (c, s) => { }),
                Resource.ChangeSet_ChangeSetEntryNotFound,
                "entity");
        }

        [TestMethod]
        [TestDescription("Verifies that associated entity transforms handle exceptions as expected.")]
        public void Associate_EntityTransform_InvocationException()
        {
            ChangeSet changeSet = this.GenerateChangeSet();
            MockEntity1 clientEntity = changeSet.ChangeSetEntries.Select(e => e.Entity).OfType<MockEntity1>().First();

            changeSet.Associate(
                clientEntity,
                new MockStoreEntity(),
                (c, s) => { throw new NotSupportedException("Not Supported!"); });

            ExceptionHelper.ExpectException<NotSupportedException>(
                () => changeSet.ApplyAssociatedStoreEntityTransforms(),
                "Not Supported!");
        }

        [TestMethod]
        [TestDescription("Verifies that the GetAssociatedEntities method behaves as expected.")]
        public void Associate_GetAssociatedEntities()
        {
            ChangeSet changeSet = new ChangeSet(this.GenerateEntityOperations(true));

            MockEntity1 clientEntity1 = changeSet.ChangeSetEntries.Select(e => e.Entity).OfType<MockEntity1>().First();
            MockEntity2 clientEntity2 = changeSet.ChangeSetEntries.Select(e => e.Entity).OfType<MockEntity2>().First();
            MockStoreEntity storeEntity = new MockStoreEntity();

            changeSet.Associate(clientEntity1, storeEntity, (c, s) => { });
            changeSet.Associate(clientEntity2, storeEntity, (c, s) => { });

            IEnumerable<MockEntity1> clientEntities1 = changeSet.GetAssociatedEntities<MockEntity1, MockStoreEntity>(storeEntity);
            Assert.IsNotNull(clientEntities1, "Expected to clientEntities1 to not be null.");
            Assert.AreEqual(1, clientEntities1.Count(), "Expected to find 1 clientEntity entry.");
            Assert.AreSame(clientEntity1, clientEntities1.Single(), "Expected to find clientEntity1.");

            IEnumerable<MockEntity2> clientEntities2 = changeSet.GetAssociatedEntities<MockEntity2, MockStoreEntity>(storeEntity);
            Assert.IsNotNull(clientEntities2, "Expected to clientEntities to not be null.");
            Assert.AreEqual(1, clientEntities2.Count(), "Expected to find 1 clientEntity entry.");
            Assert.AreSame(clientEntity2, clientEntities2.Single(), "Expected to find clientEntity2.");

            IEnumerable<object> clientEntities3 = changeSet.GetAssociatedEntities<object, MockStoreEntity>(storeEntity);
            Assert.IsNotNull(clientEntities3, "Expected to clientEntities to not be null.");
            Assert.AreEqual(2, clientEntities3.Count(), "Expected to find 2 clientEntity entries.");
            Assert.IsTrue(clientEntities3.Contains(clientEntity1), "Expected to find clientEntity1.");
            Assert.IsTrue(clientEntities3.Contains(clientEntity2), "Expected to find clientEntity2.");
        }

        [TestMethod]
        [TestDescription("Verifies that the GetAssociatedEntities method behaves as expected.")]
        public void Associate_GetAssociatedEntities_NotFound()
        {
            ChangeSet changeSet = this.GenerateChangeSet();

            MockEntity1 clientEntity = (MockEntity1)changeSet.ChangeSetEntries.First().Entity;
            MockStoreEntity storeEntity = new MockStoreEntity();

            changeSet.Associate(clientEntity, storeEntity, (c, s) => { });

            IEnumerable<MockEntity1> clientEntities = changeSet.GetAssociatedEntities<MockEntity1, MockStoreEntity>(new MockStoreEntity());

            Assert.IsNotNull(clientEntities, "Expected to clientEntities to not be null.");
            Assert.AreEqual(0, clientEntities.Count(), "Expected to find 0 clientEntity entries.");
        }

        [TestMethod]
        [TestDescription("Verifies that the GetAssociatedEntities method behaves as expected.")]
        public void Associate_GetAssociatedEntities_Multiple()
        {
            ChangeSet changeSet = this.GenerateChangeSet();

            MockEntity1 clientEntity1 = (MockEntity1)changeSet.ChangeSetEntries.Skip(0).First().Entity;
            MockEntity1 clientEntity2 = (MockEntity1)changeSet.ChangeSetEntries.Skip(1).First().Entity;
            MockEntity1 clientEntity3 = (MockEntity1)changeSet.ChangeSetEntries.Skip(2).First().Entity;
            MockStoreEntity storeEntity = new MockStoreEntity();

            changeSet.Associate(clientEntity1, storeEntity, (c, s) => { });
            changeSet.Associate(clientEntity2, storeEntity, (c, s) => { });
            changeSet.Associate(clientEntity3, storeEntity, (c, s) => { });

            IEnumerable<MockEntity1> clientEntities = changeSet.GetAssociatedEntities<MockEntity1, MockStoreEntity>(storeEntity);

            Assert.IsNotNull(clientEntities, "Expected to find a non-null return value.");
            Assert.AreEqual(3, clientEntities.Count(), "Expected to find 3 clientEntities entries.");
            Assert.IsTrue(clientEntities.Contains(clientEntity1), "Expected to find clientEntities1 in the associated entities collection.");
            Assert.IsTrue(clientEntities.Contains(clientEntity2), "Expected to find clientEntities2 in the associated entities collection.");
            Assert.IsTrue(clientEntities.Contains(clientEntity3), "Expected to find clientEntities3 in the associated entities collection.");
        }

        private ChangeSet GenerateChangeSet()
        {
            return new ChangeSet(this.GenerateEntityOperations(false));
        }

        private IEnumerable<ChangeSetEntry> GenerateEntityOperations(bool alternateTypes)
        {
            List<ChangeSetEntry> ops = new List<ChangeSetEntry>(10);

            int id = 1;
            for (int i = 0; i < ops.Capacity; ++i)
            {
                object entity, originalEntity;

                if (!alternateTypes || i % 2 == 0)
                {
                    entity = new MockEntity1() { FullName = string.Format("FName{0} LName{0}", i) };
                    originalEntity = new MockEntity1() { FullName = string.Format("OriginalFName{0} OriginalLName{0}", i) };
                }
                else
                {
                    entity = new MockEntity2() { FullNameAndID = string.Format("FName{0} LName{0} ID{0}", i) };
                    originalEntity = new MockEntity2() { FullNameAndID = string.Format("OriginalFName{0} OriginalLName{0} OriginalID{0}", i) };
                }

                ops.Add(new ChangeSetEntry(id++, entity, originalEntity, DomainOperation.Update));
            }

            return ops;
        }

        public class MockStoreEntity
        {
            public int ID { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        public class MockEntity1
        {
            public string FullName { get; set; }
        }

        public class MockEntity2
        {
            public string FullNameAndID { get; set; }
        }

        public class MockDerivedEntity : MockEntity1
        {
        }
    }
}
