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
using TestDomainServices;

using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.DomainServices.Client.Test
{
    using Resource = SSmDsClient::OpenRiaServices.DomainServices.Client.Resource;

    /// <summary>
    /// Tests for compositional hierarchy features.
    /// </summary>
    [TestClass]
    public class CompositionInheritanceTests : UnitTestBase
    {
        private static Uri CompositionInheritanceScenarios_Uri = new Uri(TestURIs.RootURI, "TestDomainServices-CompositionInheritanceScenarios.svc");

        [TestMethod]
        [Asynchronous]
        [Description("Loads a hierarchy containing composed children including derived child types")]
        public void Composition_Inheritance_HierarchyQuery()
        {
            CompositionInheritanceScenarios ctxt = new CompositionInheritanceScenarios(CompositionInheritanceScenarios_Uri);

            LoadOperation lo = ctxt.Load(ctxt.GetParentsQuery(), false);

            EnqueueConditional(() => lo.IsComplete);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);
                // 3 parents, each with 4 children
                Assert.AreEqual(15, lo.AllEntities.Count(), "Unexpected number of entities in hierarchy");

                Assert.AreEqual(3, ctxt.CI_Parents.Count, "Expected this many total parent entities");
                
                foreach (CI_Parent p in ctxt.CI_Parents)
                {
                    VerifyHierarchy(p);
                    IEnumerable<CI_Child> baseChildren = p.Children.Where(c => c.GetType() == typeof(CI_Child));
                    Assert.AreEqual(2, baseChildren.Count(), "Wrong number of child entities");
                    IEnumerable<CI_AdoptedChild> adoptedChildren = p.Children.OfType<CI_AdoptedChild>();
                    Assert.AreEqual(2, adoptedChildren.Count(), "Wrong number of adopted child entities");
                }
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Updates a composition child base type on the parent base entity type")]
        public void Composition_Inheritance_Update_Base_Child_On_Base_Parent()
        {
            CompositionInheritanceScenarios ctxt = new CompositionInheritanceScenarios(CompositionInheritanceScenarios_Uri);

            CI_Parent parent = null;
            SubmitOperation so = null;
            IEnumerable<Entity> expectedUpdates = null;
            LoadOperation lo = ctxt.Load(ctxt.GetParentsQuery(), false);

            EnqueueConditional(() => lo.IsComplete);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                parent = ctxt.CI_Parents.First(p => p.GetType() == typeof(CI_Parent));
                CI_Child existingChild = parent.Children.First(c => c.GetType() == typeof(CI_Child));
                Assert.AreSame(parent, ((Entity)existingChild).Parent);

                // update derived comp child
                existingChild.Age++; ;

                EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(cs.ModifiedEntities.Count == 2, "wrong modified count");
                Assert.IsTrue(cs.RemovedEntities.Count == 0, "wrong removed count");
                Assert.IsTrue(cs.ModifiedEntities.Contains(parent));
                Assert.IsTrue(cs.ModifiedEntities.Contains(existingChild));

                // verify that original associations are set up correctly
                IEnumerable<ChangeSetEntry> entityOps = ChangeSetBuilder.Build(cs);
                this.ValidateEntityOperationAssociations(entityOps);

                expectedUpdates = cs.Where(p => p.HasChildChanges || p.HasPropertyChanges).ToArray();
                so = ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(delegate
            {
                this.VerifySuccess(ctxt, so, expectedUpdates);
            });

            EnqueueTestComplete();
        }


        [TestMethod]
        [Asynchronous]
        [Description("Updates a derived composition child on the base entity type")]
	[WorkItem(864736)]	// bug was having an unmodified derived composition child in changeset
        public void Composition_Inheritance_Update_Derived_Child_On_Base_Parent()
        {
            CompositionInheritanceScenarios ctxt = new CompositionInheritanceScenarios(CompositionInheritanceScenarios_Uri);

            CI_Parent parent = null;
            SubmitOperation so = null;
            IEnumerable<Entity> expectedUpdates = null;
            LoadOperation lo = ctxt.Load(ctxt.GetParentsQuery(), false);

            EnqueueConditional(() => lo.IsComplete);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                parent = ctxt.CI_Parents.First(p => p.GetType() == typeof(CI_Parent));
                CI_AdoptedChild existingChild = parent.Children.OfType<CI_AdoptedChild>().First();
                Assert.AreSame(parent, ((Entity)existingChild).Parent);

                // update derived comp child
                existingChild.Age++; ;

                EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(cs.ModifiedEntities.Count == 2, "wrong modified count");
                Assert.IsTrue(cs.RemovedEntities.Count == 0, "wrong removed count");
                Assert.IsTrue(cs.ModifiedEntities.Contains(parent));
                Assert.IsTrue(cs.ModifiedEntities.Contains(existingChild));

                // verify that original associations are set up correctly
                IEnumerable<ChangeSetEntry> entityOps = ChangeSetBuilder.Build(cs);
                this.ValidateEntityOperationAssociations(entityOps);

                expectedUpdates = cs.Where(p => p.HasChildChanges || p.HasPropertyChanges).ToArray();
                so = ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(delegate
            {
                this.VerifySuccess(ctxt, so, expectedUpdates);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Updates a base composition child on the derived parent entity type")]
        public void Composition_Inheritance_Update_Base_Child_On_Derived_Parent()
        {
            CompositionInheritanceScenarios ctxt = new CompositionInheritanceScenarios(CompositionInheritanceScenarios_Uri);

            CI_SpecialParent parent = null;
            SubmitOperation so = null;
            IEnumerable<Entity> expectedUpdates = null;
            LoadOperation lo = ctxt.Load(ctxt.GetParentsQuery(), false);

            EnqueueConditional(() => lo.IsComplete);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                parent = ctxt.CI_Parents.OfType<CI_SpecialParent>().First();
                CI_Child existingChild = parent.Children.First(c => c.GetType() == typeof(CI_Child));
                Assert.AreSame(parent, ((Entity)existingChild).Parent);

                // update derived comp child
                existingChild.Age++; ;

                EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(cs.ModifiedEntities.Count == 2, "wrong modified count");
                Assert.IsTrue(cs.RemovedEntities.Count == 0, "wrong removed count");
                Assert.IsTrue(cs.ModifiedEntities.Contains(parent));
                Assert.IsTrue(cs.ModifiedEntities.Contains(existingChild));

                // verify that original associations are set up correctly
                IEnumerable<ChangeSetEntry> entityOps = ChangeSetBuilder.Build(cs);
                this.ValidateEntityOperationAssociations(entityOps);

                expectedUpdates = cs.Where(p => p.HasChildChanges || p.HasPropertyChanges).ToArray();
                so = ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(delegate
            {
                this.VerifySuccess(ctxt, so, expectedUpdates);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Updates a derived composition child on the derived entity parent")]
        public void Composition_Inheritance_Update_Derived_Child_On_Derived_Parent()
        {
            CompositionInheritanceScenarios ctxt = new CompositionInheritanceScenarios(CompositionInheritanceScenarios_Uri);

            CI_SpecialParent parent = null;
            SubmitOperation so = null;
            IEnumerable<Entity> expectedUpdates = null;
            LoadOperation lo = ctxt.Load(ctxt.GetParentsQuery(), false);

            EnqueueConditional(() => lo.IsComplete);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                parent = ctxt.CI_Parents.OfType<CI_SpecialParent>().First();
                CI_AdoptedChild existingChild = parent.Children.OfType<CI_AdoptedChild>().First();
                Assert.AreSame(parent, ((Entity)existingChild).Parent);

                // update derived comp child
                existingChild.Age++; ;

                EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(cs.ModifiedEntities.Count == 2, "wrong modified count");
                Assert.IsTrue(cs.RemovedEntities.Count == 0, "wrong removed count");
                Assert.IsTrue(cs.ModifiedEntities.Contains(parent));
                Assert.IsTrue(cs.ModifiedEntities.Contains(existingChild));

                // verify that original associations are set up correctly
                IEnumerable<ChangeSetEntry> entityOps = ChangeSetBuilder.Build(cs);
                this.ValidateEntityOperationAssociations(entityOps);

                expectedUpdates = cs.Where(p => p.HasChildChanges || p.HasPropertyChanges).ToArray();
                so = ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(delegate
            {
                this.VerifySuccess(ctxt, so, expectedUpdates);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Adds a derived composition child on the derived entity parent")]
        public void Composition_Inheritance_Add_Derived_Child_To_Derived_Parent()
        {
            CompositionInheritanceScenarios ctxt = new CompositionInheritanceScenarios(CompositionInheritanceScenarios_Uri);

            CI_SpecialParent parent = null;
            SubmitOperation so = null;
            IEnumerable<Entity> expectedUpdates = null;
            LoadOperation lo = ctxt.Load(ctxt.GetParentsQuery(), false);

            EnqueueConditional(() => lo.IsComplete);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                parent = ctxt.CI_Parents.OfType<CI_SpecialParent>().First();
                CI_AdoptedChild newChild = new CI_AdoptedChild()
                {
                    Age = 5,
                };
                parent.Children.Add(newChild);
                Assert.AreSame(parent, ((Entity)newChild).Parent);

                EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(cs.ModifiedEntities.Count == 1, "wrong modified count");
                Assert.IsTrue(cs.AddedEntities.Count == 1, "wrong added count");
                Assert.IsTrue(cs.RemovedEntities.Count == 0, "wrong removed count");
                Assert.IsTrue(cs.ModifiedEntities.Contains(parent));
                Assert.IsTrue(cs.AddedEntities.Contains(newChild));

                // verify that original associations are set up correctly
                IEnumerable<ChangeSetEntry> entityOps = ChangeSetBuilder.Build(cs);
                this.ValidateEntityOperationAssociations(entityOps);

                expectedUpdates = cs.Where(p => p.HasChildChanges || p.HasPropertyChanges).ToArray();
                so = ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(delegate
            {
                this.VerifySuccess(ctxt, so, expectedUpdates);
            });

            EnqueueTestComplete();
        }



        #region Test Helpers
        /// <summary>
        /// Verify successful completion of an update operation
        /// </summary>
        private void VerifySuccess(CompositionInheritanceScenarios ctxt, SubmitOperation so, IEnumerable<Entity> expectedUpdates)
        {
            // verify operation completed successfully
            TestHelperMethods.AssertOperationSuccess(so);

            // verify that all operations were executed
            EntityChangeSet cs = so.ChangeSet;
            VerifyOperationResults(cs.AddedEntities, expectedUpdates);

            // verify that all changes have been accepted
            cs = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(cs.IsEmpty);
        }

        /// <summary>
        /// Verify that each node in the hierarchy has the expected children
        /// and all Parent back pointers are valid.
        /// </summary>
        private static void VerifyHierarchy(CI_Parent parent)
        {
            Assert.IsTrue(parent.Children.Count > 0);
            foreach (CI_Child c in parent.Children)
            {
                Assert.AreSame(parent, c.Parent);
            }
        }

        /// <summary>
        /// For each operation in the changeset, verify that the server side
        /// operation was executed by comparing the OperationResult with the
        /// expected value.
        /// </summary>
        private void VerifyOperationResults(IEnumerable<Entity> expectedInserts, IEnumerable<Entity> expectedUpdates)
        {
            foreach (Entity entity in expectedInserts)
            {
                VerifyOperationResult(entity, "Insert");
            }
            foreach (Entity entity in expectedUpdates)
            {
                VerifyOperationResult(entity, "Update");
            }
            // custom methods should be validated externally
            // we don't send back updates for deleted entities
        }

        private void VerifyOperationResult(object entity, string expectedResult)
        {
            PropertyInfo prop = entity.GetType().GetProperty("OperationResult", BindingFlags.Instance | BindingFlags.Public);
            string[] updateResults = ((string)prop.GetValue(entity, null)).Split(',');
            Assert.IsTrue(updateResults.Contains(expectedResult));
        }

        private void ValidateEntityOperationAssociations(IEnumerable<ChangeSetEntry> changeSetEntries)
        {
            Dictionary<int, ChangeSetEntry> opIdMap = changeSetEntries.ToDictionary(p => p.Id);
            foreach (ChangeSetEntry op in changeSetEntries)
            {
                if (op.Associations != null)
                {
                    foreach (var assoc in op.Associations)
                    {
                        System.Reflection.PropertyInfo assocProp = op.Entity.GetType().GetProperty(assoc.Key, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (assocProp.GetCustomAttributes(typeof(CompositionAttribute), false).Any())
                        {
                            // if there are original ids for this association, capture them
                            int[] origAssocIds = new int[0];
                            if (op.OriginalAssociations != null)
                            {
                                if (op.OriginalAssociations.ContainsKey(assoc.Key))
                                {
                                    origAssocIds = op.OriginalAssociations[assoc.Key];
                                }
                            }

                            // inspect and validate current associations
                            foreach (int currId in assoc.Value)
                            {
                                ChangeSetEntry childOperation = opIdMap[currId];

                                // should never be any deleted entities in the current associations
                                // set
                                Assert.IsTrue(childOperation.Operation != EntityOperationType.Delete);

                                // ensure that all non-new entities in the current association
                                // set are also present in the original set
                                if (childOperation.Operation != EntityOperationType.Insert)
                                {
                                    // if the entity is not new, we expect to find it
                                    // in the original ids
                                    Assert.IsTrue(origAssocIds.Contains(currId));
                                }
                            }

                            // inspect and validate original associations
                            foreach (int currId in origAssocIds)
                            {
                                ChangeSetEntry childOperation = opIdMap[currId];

                                // shouldn't be any new entities in the original
                                // associations set
                                Assert.IsTrue(childOperation.Operation != EntityOperationType.Insert);
                            }
                        }
                    }
                }

                if (op.OriginalAssociations != null)
                {
                    // validate here?
                }
            }
        }
        #endregion
    }
}
