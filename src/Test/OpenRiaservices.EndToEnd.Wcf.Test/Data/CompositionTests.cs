extern alias SSmDsClient;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using TestDomainServices;

using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.Client.Test
{
    using Resource = SSmDsClient::OpenRiaServices.Client.Resource;

    /// <summary>
    /// Tests for compositional hierarchy features.
    /// </summary>
    [TestClass]
    public class CompositionTests : UnitTestBase
    {
        private static Uri CompositionScenarios_Explicit_Uri = new Uri(TestURIs.RootURI, "TestDomainServices-CompositionScenarios_Explicit.svc");
        private static Uri CompositionScenarios_Implicit_Uri = new Uri(TestURIs.RootURI, "TestDomainServices-CompositionScenarios_Implicit.svc");
        private static Uri CompositionScenarios_Various_Uri = new Uri(TestURIs.RootURI, "TestDomainServices-CompositionScenarios_Various.svc");

        /// <summary>
        /// In this bug, moving the child from one parent to another caused the child to be added
        /// TWICE to the destination collection.
        /// </summary>
        [TestMethod]
        [WorkItem(192335)] // actual bug tracking this issue
        [WorkItem(191649)] // related bug
        public void Composition_MoveChildToNewParent()
        {
            ConfigurableEntityContainer container = new ConfigurableEntityContainer();
            container.CreateSet<Parent>(EntitySetOperations.All);
            container.CreateSet<Child>(EntitySetOperations.All);
            container.CreateSet<GrandChild>(EntitySetOperations.All);
            container.CreateSet<GreatGrandChild>(EntitySetOperations.All);
            EntitySet<Parent> parentSet = container.GetEntitySet<Parent>();
            EntitySet<Child> childSet = container.GetEntitySet<Child>();
            EntitySet<GreatGrandChild> ggChildSet = container.GetEntitySet<GreatGrandChild>();

            Parent p1 = new Parent() { ID = 1 };
            Parent p2 = new Parent() { ID = 2 };
            container.LoadEntities(new Entity[] { p1, new Child { ID = 1, ParentID = 1 }, new Child { ID = 2, ParentID = 1 },
                                                  p2, new Child { ID = 3, ParentID = 2 }, new Child { ID = 4, ParentID = 2 } });

            Assert.AreEqual(2, p1.Children.Count);
            Assert.AreEqual(2, p2.Children.Count);

            Child child = p1.Children.ElementAt(0);
            p1.Children.Remove(child);

            Assert.AreEqual(1, p1.Children.Count);
            Assert.AreEqual(2, p2.Children.Count);

            // This is the repro - this addition was causing the counts to be 1 and 4 below
            p2.Children.Add(child);

            Assert.AreEqual(1, p1.Children.Count);
            Assert.AreEqual(3, p2.Children.Count);

            Assert.AreEqual(EntityState.Modified, child.EntityState);
            Assert.AreSame(p2, child.Parent);
        }

        /// <summary>
        /// Verify that AcceptChanges works properly in composition scenarios where
        /// the parent has property modifications in addition to child modifications.
        /// </summary>
        [TestMethod]
        [WorkItem(171266)]
        [Asynchronous]
        public void Composition_ModifiedParentWithModifiedChildren()
        {
            CompositionScenarios_Explicit ctxt = new CompositionScenarios_Explicit(CompositionScenarios_Explicit_Uri);

            LoadOperation lo = ctxt.Load(ctxt.GetParentsQuery(), false);
            SubmitOperation so = null;

            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                Parent parent = ctxt.Parents.First();

                // remove the child and modify the parent
                Child child = parent.Children.First();
                parent.Children.Remove(child);
                parent.Property += "*";

                so = ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                Assert.IsFalse(so.HasError);
                Assert.IsFalse(ctxt.HasChanges);
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// The cause of this bug was Attach ordering inside EntityCollection. The fix for bug#880371 below
        /// required reordering Attach, but that was moved too late in the Add cycle.
        /// </summary>
        [TestMethod]
        [WorkItem(884228)]
        public void Composition_AddChildPreviouslyReferencingOtherParent()
        {
            ConfigurableEntityContainer container = new ConfigurableEntityContainer();
            container.CreateSet<Parent>(EntitySetOperations.All);
            container.CreateSet<Child>(EntitySetOperations.All);
            container.CreateSet<GrandChild>(EntitySetOperations.All);
            container.CreateSet<GreatGrandChild>(EntitySetOperations.All);
            EntitySet<Parent> parentSet = container.GetEntitySet<Parent>();
            EntitySet<Child> childSet = container.GetEntitySet<Child>();
            EntitySet<GreatGrandChild> ggChildSet = container.GetEntitySet<GreatGrandChild>();

            Parent p1 = new Parent() { ID = 1 };
            Parent p2 = new Parent() { ID = 2 };
            container.LoadEntities(new Entity[] { p1, p2 });

            Child child = new Child() { ID = 1, ParentID = 2 };
            p1.Children.Add(child);

            Assert.AreEqual(EntityState.Modified, p1.EntityState);
            Assert.IsTrue(childSet.HasChanges);
        }

        /// <summary>
        /// Verify that as a child is successively removed and readded to a parent
        /// collection, that it state transitions properly.
        /// </summary>
        [TestMethod]
        [WorkItem(880371)]
        public void Composition_MultipleRemoveReadds()
        {
            ConfigurableEntityContainer container = new ConfigurableEntityContainer();
            container.CreateSet<Parent>(EntitySetOperations.All);
            container.CreateSet<Child>(EntitySetOperations.All);
            container.CreateSet<GrandChild>(EntitySetOperations.All);
            container.CreateSet<GreatGrandChild>(EntitySetOperations.All);
            EntitySet<Parent> parentSet = container.GetEntitySet<Parent>();
            EntitySet<Child> childSet = container.GetEntitySet<Child>();
            EntitySet<GreatGrandChild> ggChildSet = container.GetEntitySet<GreatGrandChild>();

            Parent parent = new Parent() { ID = 1 };
            Child child = new Child() { ID = 1, Parent = parent };
            container.LoadEntities(new Entity[] { parent, child });

            parent.Children.Remove(child);
            Assert.AreEqual(EntityState.Deleted, child.EntityState);
            Assert.IsFalse(childSet.Contains(child));

            parent.Children.Add(child);
            Assert.AreEqual(EntityState.Modified, child.EntityState);
            Assert.IsTrue(childSet.Contains(child));

            parent.Children.Remove(child);
            Assert.AreEqual(EntityState.Deleted, child.EntityState);
            Assert.IsFalse(childSet.Contains(child));

            // verify the same scenario for a singleton composition
            GrandChild gc = new GrandChild { ID = 1 };
            GreatGrandChild ggc = new GreatGrandChild { ID = 1, ParentID = 1 };
            container.LoadEntities(new Entity[] { gc, ggc });

            Assert.AreSame(ggc, gc.Child);

            gc.Child = null;
            Assert.AreEqual(EntityState.Deleted, ggc.EntityState);
            Assert.IsFalse(ggChildSet.Contains(ggc));

            gc.Child = ggc;
            Assert.AreEqual(EntityState.Unmodified, ggc.EntityState);
            Assert.IsTrue(ggChildSet.Contains(ggc));

            gc.Child = null;
            Assert.AreEqual(EntityState.Deleted, ggc.EntityState);
            Assert.IsFalse(ggChildSet.Contains(ggc));
        }

        [TestMethod]
        public void CompositionSelfReference_UpdateChildBypassingParent()
        {
            ConfigurableEntityContainer container = new ConfigurableEntityContainer();
            container.CreateSet<SelfReferencingComposition_OneToMany>(EntitySetOperations.All);

            SelfReferencingComposition_OneToMany s1 = new SelfReferencingComposition_OneToMany() { ID = 1 };
            SelfReferencingComposition_OneToMany s2 = new SelfReferencingComposition_OneToMany() { ID = 2, ParentID = 1 };

            container.LoadEntities(new Entity[] { s1, s2 });

            // load the child w/o loading the parent
            SelfReferencingComposition_OneToMany child = container.GetEntitySet<SelfReferencingComposition_OneToMany>().Single(p => p.ID == 2);
            Assert.IsNull(((Entity)child).Parent);
            child.Value += "x";

            // When this bug is fixed, we'd expect an exception during ChangeSetBuilder.Build
            EntityChangeSet cs = container.GetChanges();
            List<ChangeSetEntry> entries = ChangeSetBuilder.Build(cs);
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(879765)]
        public void CompositionSelfReference_UpdateParentWithNoChildren()
        {
            CompositionScenarios_Various ctxt = new CompositionScenarios_Various(CompositionScenarios_Various_Uri);

            LoadOperation lo = ctxt.Load(ctxt.GetSelfReferencingCompositionsQuery(), false);
            SubmitOperation so = null;

            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                SelfReferencingComposition parent = ctxt.SelfReferencingCompositions.Single(p => p.ID == 5);
                Assert.IsNull(parent.Child);
                parent.Value += "x";

                so = ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                Assert.IsFalse(so.HasError);
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// There was a bug while building the changeset related to EntityRef
        /// and compositions. This test is the repro and verifies the changeset
        /// can be created successfully.
        /// </summary>
        [TestMethod]
        [WorkItem(877481)]
        public void Composition_EntityReferenceBug()
        {
            ConfigurableEntityContainer container = new ConfigurableEntityContainer();
            container.CreateSet<SelfReferencingComposition_OneToMany>(EntitySetOperations.All);

            SelfReferencingComposition_OneToMany s1 = new SelfReferencingComposition_OneToMany() { ID = 1 };
            SelfReferencingComposition_OneToMany s2 = new SelfReferencingComposition_OneToMany() { ID = 2, ParentID = 1 };
            SelfReferencingComposition_OneToMany s3 = new SelfReferencingComposition_OneToMany() { ID = 3, ParentID = 1 };
            SelfReferencingComposition_OneToMany s4 = new SelfReferencingComposition_OneToMany() { ID = 4, ParentID = 3 };

            container.LoadEntities(new Entity[] { s1, s2, s3, s4 });

            SelfReferencingComposition_OneToMany child = s1.Children.Single(p => p.ID == 2);
            child.Value += "x";

            EntityChangeSet cs = container.GetChanges();
            Assert.HasCount(2, cs.ModifiedEntities);

            List<ChangeSetEntry> entries = ChangeSetBuilder.Build(cs);
            Assert.HasCount(4, entries);
        }

        /// <summary>
        /// Verifies that an unsupported composition removal leaves the collection and entity states unchanged.
        /// </summary>
        /// <param name="childOperations">The operations supported by the child entity set.</param>
        [TestMethod]
        public void Composition_EntityCollection_Remove_NotSupported()
        {
            var container = new ConfigurableEntityContainer();
            container.CreateSet<Parent>(EntitySetOperations.All);
            container.CreateSet<Child>(EntitySetOperations.None);
            var childSet = container.GetEntitySet<Child>();
            var parent = new Parent { ID = 1 };
            var child = new Child { ID = 1, ParentID = 1 };

            container.LoadEntities([parent, child]);

            var exception = Assert.ThrowsExactly<NotSupportedException>(() => parent.Children.Remove(child));

            Assert.AreEqual(string.Format(System.Globalization.CultureInfo.CurrentCulture,
                Resource.EntitySet_UnsupportedOperation, typeof(Child), EntitySetOperations.Remove), exception.Message);
            Assert.AreEqual(EntityState.Unmodified, child.EntityState);
            Assert.AreEqual(EntityState.Unmodified, parent.EntityState);
            Assert.AreSequenceEqual([child], childSet);
            Assert.AreSequenceEqual([child], parent.Children);
            Assert.AreSame(parent, child.Parent);
            Assert.IsFalse(container.HasChanges);
        }

        [TestMethod]
        public void Composition_EntityCollection_Remove_IdentityPartOfForeignKey()
        {
            var container = new PictureContainer();
            var picture = new Picture { Id = 101 };
            var synced = new Picture { Id = 102 };
            var pictureLabel = new PictureLabel { PictureId = 101, LabelId = 7 };
            var syncedLabel = new PictureLabel { PictureId = 102, LabelId = 7 };

            container.LoadEntities([picture, synced, pictureLabel, syncedLabel]);

            Assert.AreSame(pictureLabel, picture.PictureLabels.Single());
            Assert.AreSame(syncedLabel, synced.PictureLabels.Single());

            // When a link is removed from near.PictureLabels, remove any matching links from far.PictureLabels.
            // Original repro was a sync so link was removed on change, before "Remove" returned
            // It replicated without this, but keep the event handler to verify it still works if we change EntityCollection again to raise notification earlier
            picture.PictureLabels.CollectionChanged += (sender, e) =>
            {
                if (e.Action != NotifyCollectionChangedAction.Remove || e.OldItems == null)
                    return;

                synced.PictureLabels.Remove(syncedLabel);
            };

            // Remove all links from near.PictureLabels, which will trigger the above event handler to remove matching links from far.PictureLabels.
            picture.PictureLabels.Remove(pictureLabel);

            // Entities should be removed
            Assert.AreEqual(EntityState.Deleted, pictureLabel.EntityState);
            Assert.AreEqual(EntityState.Deleted, syncedLabel.EntityState);
            Assert.AreEqual(7, pictureLabel.LabelId);
            Assert.AreEqual(0, picture.PictureLabels.Count);
            Assert.AreEqual(0, synced.PictureLabels.Count);

            // "Replace" with new labels
            _ = new PictureLabel { LabelId = 7, Picture = picture };
            synced.PictureLabels.Add(new PictureLabel { LabelId = 7 });

            Assert.AreEqual(EntityState.Deleted, pictureLabel.EntityState);

            var newLabel = picture.PictureLabels.Single();

            Assert.AreNotSame(pictureLabel, newLabel);
            Assert.AreEqual(101, newLabel.PictureId);
            Assert.AreEqual(7, newLabel.LabelId);
            Assert.AreEqual(EntityState.New, newLabel.EntityState);
        }

        /// <summary>
        /// Verifies removal and replacement of a composed reference whose foreign key is its entire primary key.
        /// </summary>
        /// <param name="replaceDirectly">Whether to replace the metadata without first assigning null.</param>
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Composition_EntityRef_Remove_IdentityPartOfForeignKey(bool replaceDirectly)
        {
            var container = new PictureContainer();
            var picture = new Picture { Id = 101 };
            var metadata = new PictureMetadata { PictureId = 101 };

            container.LoadEntities([picture, metadata]);

            Assert.AreSame(metadata, picture.Metadata);
            Assert.AreSame(picture, metadata.Picture);

            if (!replaceDirectly)
            {
                picture.Metadata = null;

                Assert.IsNull(picture.Metadata);
                Assert.IsNull(metadata.Picture);
                Assert.AreEqual(EntityState.Deleted, metadata.EntityState);
                Assert.AreEqual(0, metadata.PictureId);
                Assert.AreSame(metadata, container.GetChanges().RemovedEntities.Single());
            }

            var replacement = new PictureMetadata();
            picture.Metadata = replacement;

            Assert.AreEqual(EntityState.Deleted, metadata.EntityState);
            Assert.IsNull(metadata.Picture);
            Assert.AreEqual(0, metadata.PictureId);
            Assert.AreSame(replacement, picture.Metadata);
            Assert.AreSame(picture, replacement.Picture);
            Assert.AreEqual(101, replacement.PictureId);
            Assert.AreEqual(EntityState.New, replacement.EntityState);
            Assert.AreEqual(EntityState.Modified, picture.EntityState);

            var changes = container.GetChanges();
            Assert.AreSame(metadata, changes.RemovedEntities.Single());
            Assert.AreSame(replacement, changes.AddedEntities.Single());
            Assert.AreSame(picture, changes.ModifiedEntities.Single());
        }

        [TestMethod]
        [WorkItem(856351)]
        public void Composition_ParentUpdateOnDelete()
        {
            ConfigurableEntityContainer container = new ConfigurableEntityContainer();
            container.CreateSet<Parent>(EntitySetOperations.All);
            container.CreateSet<Child>(EntitySetOperations.All);
            container.CreateSet<GrandChild>(EntitySetOperations.All);
            EntitySet<Parent> parentSet = container.GetEntitySet<Parent>();
            EntitySet<Child> childSet = container.GetEntitySet<Child>();

            Parent parent1 = new Parent() { ID = 1 };
            Child child1 = new Child() { ID = 1, Parent = parent1 };
            container.LoadEntities(new Entity[] { parent1, child1 });

            // insert new parent and child
            Parent newParent = new Parent();
            Child newChild = new Child();
            newChild.Parent = newParent;
            parentSet.Add(newParent);

            // When the parent is removed, all it's children are also removed.
            // This causes the child ParentID to be set to 0, which means it now
            // matches the new Parent.Children collection. The removed child is added to
            // the new parent's collection, which causes child.Parent to be updated.
            // However that add is transitory - once the child is removed from the
            // entity set, it is removed from the new entity's child collection.
            // That invalid update will result in an exception on submit.
            parentSet.Remove(parent1);
            Assert.AreEqual(1, newParent.Children.Count);
            Assert.IsTrue(newParent.Children.Contains(newChild));

            // if we allow the transitory phase, on accept changes we have to be
            // sure things are fixed up
            Assert.AreEqual(EntityState.Deleted, parent1.EntityState);
            Assert.AreEqual(EntityState.Deleted, child1.EntityState);
            Assert.AreSame(newParent, newChild.Parent);

            // changeset validation succeeds, since the parent is deleted along
            // with the child
            EntityChangeSet changeSet = container.GetChanges();
            Assert.IsTrue(changeSet.AddedEntities.Count == 2 && changeSet.RemovedEntities.Count == 2);
            ChangeSetBuilder.CheckForInvalidUpdates(changeSet);
        }

        /// <summary>
        /// Verifies that children cannot change parents.
        /// </summary>
        [TestMethod]
        [WorkItem(793607)]
        public void Composition_InvalidParentUpdate_EntityCollection()
        {
            ConfigurableEntityContainer container = new ConfigurableEntityContainer();
            container.CreateSet<Parent>(EntitySetOperations.All);
            container.CreateSet<Child>(EntitySetOperations.All);
            container.CreateSet<GrandChild>(EntitySetOperations.All);
            EntitySet<Parent> parentSet = container.GetEntitySet<Parent>();
            EntitySet<Child> childSet = container.GetEntitySet<Child>();

            Parent parent1 = new Parent() { ID = 1 };
            Parent parent2 = new Parent() { ID = 2 };
            Child child = new Child() { ID = 1, Parent = parent1 };

            parentSet.Attach(parent1);
            parentSet.Attach(parent2);

            Assert.IsTrue(childSet.IsAttached(child), "Child was not attached automatically.");
            Assert.AreSame(parent1, ((Entity)child).Parent, "Entity.Parent doesn't reflect the parent-child relationship.");

            // point the child to a new parent, which results
            // in the child being reparented (an invalid update)
            child.Parent = parent2;

            EntityChangeSet changeSet = container.GetChanges();
            Assert.AreEqual(EntityState.Modified, child.EntityState);
            Assert.AreEqual(EntityState.Modified, parent1.EntityState);
            Assert.AreEqual(EntityState.Modified, parent2.EntityState);

            // Verify that changing the parent throws an exception when
            // changes are validated
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                ChangeSetBuilder.CheckForInvalidUpdates(changeSet);
            }, string.Format(Resource.Entity_CantReparentComposedChild, child));
        }

        /// <summary>
        /// Verifies that children cannot change parents.
        /// </summary>
        [TestMethod]
        [WorkItem(793607)]
        public void Composition_InvalidParentUpdate_EntityRef()
        {
            ConfigurableEntityContainer container = new ConfigurableEntityContainer();
            container.CreateSet<GrandChild>(EntitySetOperations.All);
            container.CreateSet<GreatGrandChild>(EntitySetOperations.All);
            EntitySet<GrandChild> parentSet = container.GetEntitySet<GrandChild>();
            EntitySet<GreatGrandChild> childSet = container.GetEntitySet<GreatGrandChild>();

            GrandChild parent1 = new GrandChild() { ID = 1 };
            GrandChild parent2 = new GrandChild() { ID = 2 };
            GreatGrandChild child = new GreatGrandChild() { ID = 1, Parent = parent1 };

            parentSet.Attach(parent1);
            parentSet.Attach(parent2);

            Assert.IsTrue(childSet.IsAttached(child), "Child was not attached automatically.");
            Assert.AreSame(parent1, ((Entity)child).Parent, "Entity.Parent doesn't reflect the parent-child relationship.");

            // point the child to a new parent, which results
            // in the child being reparented (an invalid update)
            child.Parent = parent2;

            EntityChangeSet changeSet = container.GetChanges();
            Assert.AreEqual(EntityState.Modified, child.EntityState);
            Assert.AreEqual(EntityState.Modified, parent1.EntityState);
            Assert.AreEqual(EntityState.Modified, parent2.EntityState);

            // Verify that changing the parent throws an exception when
            // changes are validated
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                ChangeSetBuilder.CheckForInvalidUpdates(changeSet);
            }, string.Format(Resource.Entity_CantReparentComposedChild, child));
        }

        /// <summary>
        /// Verifies that children cannot be a parent of themselves.
        /// </summary>
        [TestMethod]
        [WorkItem(797654)]
        public void Entity_ChildCannotBeItsParent()
        {
            ConfigurableEntityContainer container = new ConfigurableEntityContainer();
            container.CreateSet<SelfReferencingComposition>(EntitySetOperations.All);
            EntitySet<SelfReferencingComposition> childSet = container.GetEntitySet<SelfReferencingComposition>();

            SelfReferencingComposition child = new SelfReferencingComposition() { ID = 1 };

            childSet.Attach(child);

            // Verify that changing the parent throws an exception.
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                child.Parent = child;
            }, Resource.Entity_ChildCannotBeItsParent);
        }

        /// <summary>
        /// Verify that removing a parent doesn't leave a child behind.
        /// </summary>
        [TestMethod]
        [WorkItem(797654)]
        public void Entity_RemoveParent()
        {
            ConfigurableEntityContainer container = new ConfigurableEntityContainer();
            container.CreateSet<Parent>(EntitySetOperations.All);
            container.CreateSet<Child>(EntitySetOperations.All);
            container.CreateSet<GrandChild>(EntitySetOperations.All);
            EntitySet<Parent> parentSet = container.GetEntitySet<Parent>();
            EntitySet<Child> childSet = container.GetEntitySet<Child>();

            Parent parent1 = new Parent() { ID = 1 };
            Parent parent2 = new Parent() { ID = 2 };
            Child child = new Child() { ID = 1, Parent = parent1 };

            parentSet.Attach(parent1);
            parentSet.Attach(parent2);

            Assert.IsTrue(childSet.IsAttached(child), "Child was not attached automatically.");
            Assert.AreSame(parent1, ((Entity)child).Parent, "Entity.Parent doesn't reflect the parent-child relationship.");

            parentSet.Remove(parent1);
            EntityChangeSet changeSet = container.GetChanges();
            Assert.AreEqual(2, changeSet.Count());
        }

        [TestMethod]
        [WorkItem(791206)]
        [Asynchronous]
        public void UpdateSelfReferentialComposition()
        {
            CompositionScenarios_Various ctxt = new CompositionScenarios_Various(CompositionScenarios_Various_Uri);

            LoadOperation lo = ctxt.Load(ctxt.GetSelfReferencingCompositionsQuery(), false);
            SubmitOperation so = null;

            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                SelfReferencingComposition parent = ctxt.SelfReferencingCompositions.First(p => p.Parent == null);
                SelfReferencingComposition child = parent.Child;

                parent.Value += "x";
                child.Value += "x";

                List<ChangeSetEntry> changeSetEntries = ChangeSetBuilder.Build(ctxt.EntityContainer.GetChanges());

                so = ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(so);
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Verify that attempting to update a child entity w/o its Parent loaded
        /// results in an exception
        /// </summary>
        [TestMethod]
        [WorkItem(791206)]
        [Asynchronous]
        public void UpdateChildWithParentUnloaded()
        {
            CompositionScenarios_Various ctxt = new CompositionScenarios_Various(CompositionScenarios_Various_Uri);

            LoadOperation lo = ctxt.Load(ctxt.GetChildrenQuery(1), false);
            SubmitOperation so = null;

            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                CompositionScenarios_Child child = lo.Entities.Cast<CompositionScenarios_Child>().Single();
                Assert.IsNull(child.Parent);

                child.A += "x";

                so = ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                Assert.IsTrue(so.HasError);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [WorkItem(213411)]
        [Asynchronous]
        [Description("Test that deleting and adding a child with same key does not throw an exception.")]
        public void Composition_DeleteAndAddChildWithSameKey()
        {
            CompositionScenarios_Explicit ctx = new CompositionScenarios_Explicit(CompositionScenarios_Explicit_Uri);

            LoadOperation lo = ctx.Load(ctx.GetParentsQuery(), false);
            SubmitOperation so = null;

            int childID = 0;
            int parentID = 0;
            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                // Find any parent and delete a child.
                Parent parent = ctx.Parents.First();
                Child child = parent.Children.First();
                parent.Children.Remove(child);

                // Add a new child with the same key as the one deleted.
                Child newChild = new Child { ID = child.ID, ParentID = child.ParentID, Parent = child.Parent, Property = "NewProp" };
                parent.Children.Add(newChild);

                // Save the parent and child keys so that we can check later if the child got added properly.
                childID = child.ID;
                parentID = parent.ID;

                so = ctx.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                Assert.IsFalse(so.HasError);
                Assert.IsFalse(ctx.HasChanges);

                // Reload the entities.
                ctx.EntityContainer.Clear();
                lo = ctx.Load(ctx.GetParentsQuery(), false);
            });
            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                // Make sure the new child got added properly.
                Parent parent1 = ctx.Parents.Where(p => p.ID == parentID).SingleOrDefault();
                Assert.IsNotNull(parent1);
                Assert.IsNotNull(parent1.Children.Where(c => c.ID == childID && c.Property == "NewProp"));
            });
            EnqueueTestComplete();
        }

        #region Composition update tests
        /// <summary>
        /// Load a hierarchy and verify that it is returned correctly
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void HierarchyQuery()
        {
            CompositionScenarios_Explicit ctxt = new CompositionScenarios_Explicit(CompositionScenarios_Explicit_Uri);

            LoadOperation lo = ctxt.Load(ctxt.GetParentsQuery(), false);

            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);
                Assert.HasCount(66, lo.AllEntities);

                Assert.AreEqual(3, ctxt.Parents.Count);
                foreach (Parent p in ctxt.Parents)
                {
                    VerifyHierarchy(p);
                }
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void HierarchyInsert()
        {
            HierarchyInsert(CompositionScenarios_Explicit_Uri);
            HierarchyInsert(CompositionScenarios_Implicit_Uri);
        }

        /// <summary>
        /// Test insert of a hierarchy
        /// </summary>
        private void HierarchyInsert(Uri testUri)
        {
            CompositionScenarios_Explicit ctxt = new CompositionScenarios_Explicit(testUri);

            Parent parent = CreateCompositionHierarchy(3, 3);
            ctxt.Parents.Add(parent);

            EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
            Assert.HasCount(1 + 3 + 9 + 9, cs.AddedEntities);

            SubmitOperation so = ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);

            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                this.VerifySuccess(ctxt, so, Enumerable.Empty<Entity>());
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void HierarchyUpdate()
        {
            this.HierarchyUpdate(CompositionScenarios_Explicit_Uri);
            this.HierarchyUpdate(CompositionScenarios_Implicit_Uri);
        }

        /// <summary>
        /// Make a bunch of multilevel updates to a hierarchy
        /// </summary>
        private void HierarchyUpdate(Uri testUri)
        {
            CompositionScenarios_Explicit ctxt = new CompositionScenarios_Explicit(testUri);

            Parent parent = null;
            SubmitOperation so = null;
            LoadOperation lo = ctxt.Load(ctxt.GetParentsQuery(), false);
            IEnumerable<Entity> expectedUpdates = null;

            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                parent = ctxt.Parents.First();
                Child existingChild = parent.Children.First();
                Assert.AreSame(parent, ((Entity)existingChild).Parent);
                GrandChild existingGrandChild = existingChild.Children.Skip(1).Take(1).Single();

                // add a few new children
                Child newChild = new Child
                {
                    ID = 100
                };
                parent.Children.Add(newChild);
                GrandChild newGc = new GrandChild
                {
                    ID = 100
                };
                existingChild.Children.Add(newGc);
                GreatGrandChild newGgc = new GreatGrandChild
                {
                    ID = 100
                };

                // update a few children
                GrandChild updatedGrandChild = existingChild.Children.First(p => p.EntityState != EntityState.New);
                updatedGrandChild.Property += "x";

                // remove a few children
                GreatGrandChild deletedGreatGrandChild = existingGrandChild.Child;
                existingGrandChild.Child = null;

                // invoke a custom method on the parent
                parent.CustomOp_Parent();

                EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(cs.AddedEntities.Count == 2 && cs.ModifiedEntities.Count == 4 && cs.RemovedEntities.Count == 1);
                Assert.Contains(newChild, cs.AddedEntities);
                Assert.Contains(newGc, cs.AddedEntities);
                Assert.Contains(parent, cs.ModifiedEntities);
                Assert.Contains(existingChild, cs.ModifiedEntities);
                Assert.Contains(existingGrandChild, cs.ModifiedEntities);
                Assert.Contains(updatedGrandChild, cs.ModifiedEntities);
                Assert.Contains(deletedGreatGrandChild, cs.RemovedEntities);

                // direct test verifying that we create the correct set of
                // ChangeSetEntries to send to the server
                int modifiedCount = cs.AddedEntities.Count + cs.ModifiedEntities.Count + cs.RemovedEntities.Count;
                int expectedOperationCount = 24;
                IEnumerable<ChangeSetEntry> entityOps = ChangeSetBuilder.Build(cs);
                int entityOpCount = entityOps.Count();
                Assert.AreEqual(expectedOperationCount, entityOpCount);
                Assert.AreEqual(expectedOperationCount - modifiedCount, entityOps.Count(p => p.Operation == EntityOperationType.None));

                // verify that original associations are set up correctly
                this.ValidateEntityOperationAssociations(entityOps);

                expectedUpdates = cs.Where(p => p.HasChildChanges || p.HasPropertyChanges).ToArray();
                so = ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                this.VerifySuccess(ctxt, so, expectedUpdates);

                // verify that the custom method was invoked
                string[] updateResults = parent.OperationResult.Split(',');
                Assert.IsTrue(updateResults.Contains("CustomOp_Parent"));
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void HierarchyDelete()
        {
            this.HierarchyDelete(CompositionScenarios_Explicit_Uri);
            this.HierarchyDelete(CompositionScenarios_Implicit_Uri);
        }

        /// <summary>
        /// Delete an entire hierarchy by removing the parent and verifying
        /// that all children are recursively removed automatically.
        /// </summary>
        private void HierarchyDelete(Uri testUri)
        {
            CompositionScenarios_Explicit ctxt = new CompositionScenarios_Explicit(testUri);

            Parent parent = null;
            SubmitOperation so = null;
            LoadOperation lo = ctxt.Load(ctxt.GetParentsQuery(), false);

            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                parent = ctxt.Parents.First();
                ctxt.Parents.Remove(parent);

                EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
                Assert.HasCount(1 + 3 + 9 + 9, cs.RemovedEntities);

                so = ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                this.VerifySuccess(ctxt, so, Enumerable.Empty<Entity>());
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Invoke several CMs in a hierarchy and verify operation ordering
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void MultipleChildCustomMethodInvocations()
        {
            this.MultipleChildCustomMethodInvocations(CompositionScenarios_Explicit_Uri);
            this.MultipleChildCustomMethodInvocations(CompositionScenarios_Implicit_Uri);
        }

        private void MultipleChildCustomMethodInvocations(Uri testUri)
        {
            CompositionScenarios_Explicit ctxt = new CompositionScenarios_Explicit(testUri);

            Parent parent = null;
            Child child = null;
            GrandChild grandChild = null;
            GreatGrandChild greatGrandChild = null;
            SubmitOperation so = null;
            IEnumerable<Entity> expectedUpdates = null;

            LoadOperation lo = ctxt.Load(ctxt.GetParentsQuery(), false);

            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                parent = ctxt.Parents.First();
                child = parent.Children.Skip(2).Take(1).Single();
                grandChild = child.Children.Skip(1).Take(1).Single();
                greatGrandChild = parent.Children.First().Children.Skip(1).Take(1).Single().Child;

                // invoke on the leaf node
                greatGrandChild.CustomOp_GreatGrandChild();
                EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(cs.AddedEntities.Count == 0 && cs.ModifiedEntities.Count == 4 && cs.RemovedEntities.Count == 0);

                // invoke on a child
                child.CustomOp_Child();
                cs = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(cs.AddedEntities.Count == 0 && cs.ModifiedEntities.Count == 5 && cs.RemovedEntities.Count == 0);

                // invoke on a grand child
                grandChild.CustomOp_GrandChild();
                cs = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(cs.AddedEntities.Count == 0 && cs.ModifiedEntities.Count == 6 && cs.RemovedEntities.Count == 0);

                // invoke on the parent
                parent.CustomOp_Parent();
                cs = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(cs.AddedEntities.Count == 0 && cs.ModifiedEntities.Count == 6 && cs.RemovedEntities.Count == 0);

                expectedUpdates = cs.Where(p => p.HasChildChanges || p.HasPropertyChanges).ToArray();
                so = ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            this.EnqueueCompletion(() => so);
            EnqueueCallback(delegate
            {
                this.VerifySuccess(ctxt, so, expectedUpdates);

                Assert.IsTrue(parent.OperationResult.Split(',').Contains("CustomOp_Parent"));
                Assert.IsTrue(child.OperationResult.Split(',').Contains("CustomOp_Child"));
                Assert.IsTrue(grandChild.OperationResult.Split(',').Contains("CustomOp_GrandChild"));
                Assert.IsTrue(greatGrandChild.OperationResult.Split(',').Contains("CustomOp_GreatGrandChild"));
            });

            EnqueueTestComplete();
        }
        #endregion

        #region Hierarchical Change Tracking tests
        [TestMethod]
        public void HierarchicalChangeTracking_AcceptChanges_Bug793490()
        {
            CompositionScenarios_Explicit ctxt = new CompositionScenarios_Explicit(CompositionScenarios_Explicit_Uri);
            Parent parent = CreateCompositionHierarchy(3, 3);
            ctxt.Parents.Attach(parent);

            // remove the parent and verify that the delete cascades
            ctxt.Parents.Remove(parent);
            int totalCount = 1 + 3 + 9 + 9;
            EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
            Assert.HasCount(totalCount, cs.RemovedEntities);
            Assert.AreEqual(EntityState.Deleted, parent.EntityState);

            List<Entity> removedEntities = new List<Entity>(cs.RemovedEntities.Cast<Entity>());
            removedEntities.Remove(parent);
            removedEntities.Insert(0, parent);
            foreach (Entity entity in removedEntities)
            {
                ((IRevertibleChangeTracking)entity).AcceptChanges();
            }

            // verify that removes were accepted
            Assert.IsTrue(removedEntities.All(p => p.EntityState == EntityState.Detached));
            Assert.AreEqual(0, ctxt.Parents.Count);
            Assert.AreEqual(0, ctxt.EntityContainer.GetEntitySet<Child>().Count);
            Assert.AreEqual(0, ctxt.EntityContainer.GetEntitySet<GrandChild>().Count);
            Assert.AreEqual(0, ctxt.EntityContainer.GetEntitySet<GreatGrandChild>().Count);

            // verify all changes were undone and the graph
            // references are restored
            cs = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(cs.IsEmpty);
        }

        [TestMethod]
        public void HierarchicalChangeTracking_RejectChanges_Bug799365()
        {
            CompositionScenarios_Explicit ctxt = new CompositionScenarios_Explicit(CompositionScenarios_Explicit_Uri);
            Parent parent = CreateCompositionHierarchy(3, 3);
            ctxt.Parents.Attach(parent);

            // add a new child then reject changes
            Child newChild = new Child { ID = 25 };
            parent.Children.Add(newChild);
            EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(cs.AddedEntities.Count == 1 && cs.ModifiedEntities.Count == 1);

            ctxt.RejectChanges();

            cs = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(cs.IsEmpty);
            Assert.AreEqual(EntityState.Detached, newChild.EntityState);
        }

        [TestMethod]
        [WorkItem(188918)]  // CSDMain
        public void HierarchicalChangeTracking_RejectChanges_MultipleChildChanges()
        {
            CompositionScenarios_Explicit ctxt = new CompositionScenarios_Explicit(CompositionScenarios_Explicit_Uri);
            Parent parent = CreateCompositionHierarchy(3, 3);
            ctxt.Parents.Attach(parent);

            // update the child first
            Child child = parent.Children.First();
            child.OperationResult = "complete";
            // update the grandchild
            child.Children.First().OperationResult = "complete";
            // add a new grandchild
            GrandChild newGrandChild = new GrandChild { ID = 25 };
            child.Children.Add(newGrandChild);

            // Reject changes should revert child before either grandchild (alpha order). We need
            // to make sure this works with multiple grandchildren.
            ctxt.RejectChanges();

            EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(cs.IsEmpty, "cs.IsEmpty is false");
            Assert.AreEqual(EntityState.Unmodified, child.EntityState, "EntityState.Unmodified != child.EntityState");
        }

        [TestMethod]
        [WorkItem(188918)]  // CSDMain
        public void HierarchicalChangeTracking_AcceptChanges_MultipleChildChanges()
        {
            CompositionScenarios_Explicit ctxt = new CompositionScenarios_Explicit(CompositionScenarios_Explicit_Uri);
            Parent parent = CreateCompositionHierarchy(3, 3);
            ctxt.Parents.Attach(parent);

            // update the child first
            Child child = parent.Children.First();
            child.OperationResult = "complete";
            // update the grandchild
            child.Children.First().OperationResult = "complete";
            // add a new grandchild
            GrandChild newGrandChild = new GrandChild { ID = 25 };
            child.Children.Add(newGrandChild);

            // the key to this bug is accepting the child changes BEFORE
            // the grand child changes.
            ((IChangeTracking)child).AcceptChanges();
            ((IChangeTracking)newGrandChild).AcceptChanges();

            EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(cs.IsEmpty);
            Assert.AreEqual(EntityState.Unmodified, child.EntityState);
            Assert.AreEqual(EntityState.Unmodified, newGrandChild.EntityState);
        }

        /// <summary>
        /// Verify that when a child is edited, all its parents up the hierarchy
        /// also transition to modified.
        /// </summary>
        [TestMethod]
        public void HierarchicalChangeTracking_ChildEdits()
        {
            CompositionScenarios_Explicit ctxt = new CompositionScenarios_Explicit(CompositionScenarios_Explicit_Uri);
            Parent parent = CreateCompositionHierarchy(3, 3);
            ctxt.Parents.Attach(parent);

            // verify counts
            Assert.AreEqual(1, ctxt.Parents.Count);
            EntitySet<Child> childSet = ctxt.EntityContainer.GetEntitySet<Child>();
            Assert.AreEqual(3, childSet.Count);
            EntitySet<GrandChild> grandChildSet = ctxt.EntityContainer.GetEntitySet<GrandChild>();
            Assert.AreEqual(9, grandChildSet.Count);
            EntitySet<GreatGrandChild> greatGrandChildSet = ctxt.EntityContainer.GetEntitySet<GreatGrandChild>();
            Assert.AreEqual(9, greatGrandChildSet.Count);

            // change a child - expect all entities up the hierarchy
            // to transition to modified
            Child[] children = parent.Children.ToArray();
            GreatGrandChild ggc = children[1].Children.ToArray()[2].Child;
            ggc.Property += "x";
            Assert.IsTrue(ggc.HasChanges);
            Assert.IsTrue(parent.HasChanges);
            Assert.IsTrue(children[1].HasChanges);
            Assert.IsTrue(children[1].Children.ToArray()[2].HasChanges);
            Assert.IsFalse(children[0].HasChanges);
            Assert.IsTrue(children[1].Children.ToArray()[2].HasChanges);

            EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(cs.AddedEntities.Count == 0 && cs.ModifiedEntities.Count == 4 && cs.RemovedEntities.Count == 0);

            // now revert the child change - we expect all changes
            // to be undone up the hierarchy
            ((IRevertibleChangeTracking)ggc).RejectChanges();
            Assert.IsFalse(ggc.HasChanges);
            Assert.IsFalse(parent.HasChanges);
            children = parent.Children.ToArray();
            Assert.IsFalse(children[1].HasChanges);
            Assert.IsFalse(children[0].HasChanges);
            Assert.IsFalse(children[1].Children.ToArray()[2].HasChanges);
            Assert.IsFalse(ctxt.EntityContainer.HasChanges);
            cs = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(cs.IsEmpty);

            // run the scenario again, this time with some preexisting
            // updates in the hierarchy - those shouldn't be undone
            Child c = children[0];
            GrandChild gc = c.Children.ToArray()[2];
            ggc = gc.Child;
            gc.Property += "x";  // preexisting update
            Assert.IsTrue(gc.HasChanges && c.HasChanges && parent.HasChanges);
            ggc.Property += "x";
            Assert.IsTrue(ggc.HasChanges && gc.HasChanges && c.HasChanges && parent.HasChanges);
            ((IRevertibleChangeTracking)ggc).RejectChanges();
            Assert.IsTrue(gc.HasChanges && c.HasChanges && parent.HasChanges);
            ((IRevertibleChangeTracking)gc).RejectChanges();
            Assert.IsTrue(!gc.HasChanges && !c.HasChanges && !parent.HasChanges);
        }

        /// <summary>
        /// Verify that setting an entity ref (either nulling it out or setting it to a new
        /// entity) results in an eager remove/add, and that all members up the hierarchy
        /// state transition correctly.
        /// </summary>
        [TestMethod]
        public void HierarchicalChangeTracking_ChildAssociationUpdates_EntityRef()
        {
            CompositionScenarios_Explicit ctxt = new CompositionScenarios_Explicit(CompositionScenarios_Explicit_Uri);
            Parent parent = CreateCompositionHierarchy(3, 3);
            ctxt.Parents.Attach(parent);

            // Remove an EntityRef child - expect all entities up the hierarchy
            // to transition to modified
            Child c = parent.Children.First();
            GrandChild gc = c.Children.ToArray()[1];
            GreatGrandChild ggc = gc.Child;
            gc.Child = null;
            Assert.IsTrue(parent.HasChanges);
            Assert.IsTrue(c.HasChanges);
            Assert.IsTrue(gc.HasChanges);
            Assert.IsFalse(ggc.HasChanges);
            Assert.AreEqual(EntityState.Deleted, ggc.EntityState);
            EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(cs.AddedEntities.Count == 0 && cs.ModifiedEntities.Count == 3 && cs.RemovedEntities.Count == 1);

            // Reject the remove and verify state
            ((IRevertibleChangeTracking)gc).RejectChanges();
            Assert.IsFalse(parent.HasChanges);
            Assert.IsFalse(c.HasChanges);
            Assert.IsFalse(gc.HasChanges);
            Assert.IsFalse(ggc.HasChanges);
            cs = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(cs.AddedEntities.Count == 0 && cs.ModifiedEntities.Count == 0 && cs.RemovedEntities.Count == 0);
            Assert.AreSame(ggc, gc.Child);

            // Now test an EntityRef Add
            gc.Child = null;
            ((IChangeTracking)ctxt.EntityContainer).AcceptChanges();
            Assert.IsTrue(ctxt.EntityContainer.GetChanges().IsEmpty);
            gc.Child = new GreatGrandChild
            {
                ID = 100
            };
            cs = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(cs.AddedEntities.Count == 1 && cs.ModifiedEntities.Count == 3 && cs.RemovedEntities.Count == 0);
            ((IChangeTracking)ctxt.EntityContainer).AcceptChanges();
            Assert.IsTrue(ctxt.EntityContainer.GetChanges().IsEmpty);
        }

        /// <summary>
        /// Verify that adding and removing from an entity collection results in 
        /// an eager remove/add, and that all members up the hierarchy state transition 
        /// correctly.
        /// </summary>
        [TestMethod]
        public void HierarchicalChangeTracking_ChildAssociationUpdates_EntityCollection()
        {
            CompositionScenarios_Explicit ctxt = new CompositionScenarios_Explicit(CompositionScenarios_Explicit_Uri);
            Parent parent = CreateCompositionHierarchy(3, 3);
            ctxt.Parents.Attach(parent);

            // Remove a child and add one
            Child c = parent.Children.First();
            GrandChild gc = c.Children.ToArray()[1];
            GreatGrandChild ggc = gc.Child;
            gc.Child = null;   // remove
            GrandChild newGc = new GrandChild
            {
                ID = 100
            };
            GreatGrandChild newGgc = new GreatGrandChild
            {
                ID = 100
            };
            c.Children.Add(newGc);   // add
            newGc.Child = newGgc;
            Assert.IsTrue(parent.HasChanges);
            Assert.IsTrue(c.HasChanges);
            Assert.IsTrue(gc.HasChanges);
            Assert.IsFalse(ggc.HasChanges);
            Assert.AreEqual(EntityState.Deleted, ggc.EntityState);
            EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(cs.AddedEntities.Count == 2 && cs.ModifiedEntities.Count == 3 && cs.RemovedEntities.Count == 1);

            // Reject the remove and verify state
            ((IRevertibleChangeTracking)gc).RejectChanges();
            Assert.IsTrue(parent.HasChanges);
            Assert.IsTrue(c.HasChanges);
            Assert.IsFalse(gc.HasChanges);
            Assert.IsFalse(ggc.HasChanges);
            cs = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(cs.AddedEntities.Count == 2 && cs.ModifiedEntities.Count == 2 && cs.RemovedEntities.Count == 0);
            Assert.AreSame(ggc, gc.Child);

            ((IRevertibleChangeTracking)c).RejectChanges();
            cs = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(cs.AddedEntities.Count == 0 && cs.ModifiedEntities.Count == 0 && cs.RemovedEntities.Count == 0);
        }

        /// <summary>
        /// Verify that Detaching a parent in a hierarchy results in recursive
        /// detach of all children (cascade detach).
        /// </summary>
        [TestMethod]
        [WorkItem(882429)]
        public void HierarchicalChangeTracking_DetachHierarchy()
        {
            CompositionScenarios_Explicit ctxt = new CompositionScenarios_Explicit(CompositionScenarios_Explicit_Uri);
            Parent parent = CreateCompositionHierarchy(3, 3);
            ctxt.Parents.Attach(parent);

            var parentSet = ctxt.EntityContainer.GetEntitySet<Parent>();
            var childSet = ctxt.EntityContainer.GetEntitySet<Child>();
            var grandChildSet = ctxt.EntityContainer.GetEntitySet<GrandChild>();
            var greatGrandChildSet = ctxt.EntityContainer.GetEntitySet<GreatGrandChild>();

            // verify all entities are in the attached / Unmodified state
            Entity[] allEntities = parentSet.Cast<Entity>().Concat(childSet.Cast<Entity>().Concat(grandChildSet.Cast<Entity>().Concat(greatGrandChildSet.Cast<Entity>()))).ToArray();
            Assert.IsTrue(allEntities.All(p => p.EntityState == EntityState.Unmodified));

            Assert.AreEqual(1, parentSet.Count);
            Assert.AreEqual(3, childSet.Count);
            Assert.AreEqual(9, grandChildSet.Count);
            Assert.AreEqual(9, greatGrandChildSet.Count);

            // remove the parent and verify that the detach cascades
            ctxt.Parents.Detach(parent);

            Assert.AreEqual(0, parentSet.Count);
            Assert.AreEqual(0, childSet.Count);
            Assert.AreEqual(0, grandChildSet.Count);
            Assert.AreEqual(0, greatGrandChildSet.Count);

            // verify all entities are in the detached state
            Assert.IsTrue(allEntities.All(p => p.EntityState == EntityState.Detached));

            // even though the hierarchy is detached, child entities should remain in their child collections
            // in the detached state
            VerifyHierarchy(parent);

            Assert.AreEqual(3, parent.Children.Count);
        }

        /// <summary>
        /// Verify that removing the root of a hierarchy results in recursive
        /// removal of all children (cascade delete).
        /// </summary>
        [TestMethod]
        public void HierarchicalChangeTracking_RemoveHierarchy()
        {
            CompositionScenarios_Explicit ctxt = new CompositionScenarios_Explicit(CompositionScenarios_Explicit_Uri);
            Parent parent = CreateCompositionHierarchy(3, 3);
            ctxt.Parents.Attach(parent);

            // remove the parent and verify that the delete cascades
            ctxt.Parents.Remove(parent);
            int totalCount = 1 + 3 + 9 + 9;
            EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
            Assert.HasCount(totalCount, cs.RemovedEntities);
            Assert.AreEqual(EntityState.Deleted, parent.EntityState);

            // To reject all child changes by calling reject on the parent,
            // then readd the parent to undo the delete.
            ((IRevertibleChangeTracking)parent).RejectChanges();
            ctxt.Parents.Add(parent);

            // verify all changes were undone and the graph
            // references are restored
            cs = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(cs.IsEmpty);

            VerifyHierarchy(parent);
        }

        /// <summary>
        /// Call accept changes progressively up a modified hierarchy verifying that
        /// sub-changes are recursively accepted.
        [TestMethod]
        public void HierarchicalChangeTracking_AcceptChanges()
        {
            CompositionScenarios_Explicit ctxt = new CompositionScenarios_Explicit(CompositionScenarios_Explicit_Uri);
            Parent parent = CreateCompositionHierarchy(3, 3);
            ctxt.Parents.Attach(parent);

            // make some changes
            Child child = parent.Children.ToArray()[1];
            GrandChild grandChild = child.Children.ToArray()[0];
            GreatGrandChild greatGrandChild = grandChild.Child;

            greatGrandChild.OperationResult += "x";  // edit
            child.Children.Add(new GrandChild() { ID = 123 });    // add
            child.Children.ToArray()[1].Child = null;          // remove
            EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(cs.AddedEntities.Count == 1 && cs.ModifiedEntities.Count == 5 && cs.RemovedEntities.Count == 1);

            // call accept at various levels of the hierarchy and verify
            // accept the delete of the GreatGrandChild's Child (an edit and a remove undone)
            ((IRevertibleChangeTracking)child.Children.ToArray()[1]).AcceptChanges();
            cs = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(cs.AddedEntities.Count == 1 && cs.ModifiedEntities.Count == 4 && cs.RemovedEntities.Count == 0);

            // accept the edit to the GreatGrandChild (2 edits undone)
            ((IRevertibleChangeTracking)greatGrandChild).AcceptChanges();
            cs = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(cs.AddedEntities.Count == 1 && cs.ModifiedEntities.Count == 2 && cs.RemovedEntities.Count == 0);

            // accept the add to the Child's Children collection (1 add and remaining 2 edits undone)
            ((IRevertibleChangeTracking)child).AcceptChanges();
            cs = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(cs.IsEmpty);

            VerifyHierarchy(parent);
        }

        /// <summary>
        /// Call reject changes progressively up a modified hierarchy verifying that
        /// sub-changes are recursively rejected.
        /// </summary>
        [TestMethod]
        public void HierarchicalChangeTracking_RejectChanges()
        {
            CompositionScenarios_Explicit ctxt = new CompositionScenarios_Explicit(CompositionScenarios_Explicit_Uri);
            Parent parent = CreateCompositionHierarchy(3, 3);
            ctxt.Parents.Attach(parent);

            // make some changes
            Child child = parent.Children.ToArray()[1];
            GrandChild grandChild = child.Children.First();
            GreatGrandChild greatGrandChild = grandChild.Child;

            greatGrandChild.OperationResult += "x";  // edit
            GrandChild addedGrandChild = new GrandChild();
            child.Children.Add(addedGrandChild);    // add
            GreatGrandChild removedGreatGrandChild = child.Children.ToArray()[1].Child;
            child.Children.ToArray()[1].Child = null;          // remove
            EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(cs.AddedEntities.Count == 1 && cs.ModifiedEntities.Count == 5 && cs.RemovedEntities.Count == 1);

            // call reject at various levels of the hierarchy and verify
            // undo the delete of the GreatGrandChild's Child (an edit and a remove undone)
            ((IRevertibleChangeTracking)child.Children.ToArray()[1]).RejectChanges();
            cs = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(cs.AddedEntities.Count == 1 && cs.ModifiedEntities.Count == 4 && cs.RemovedEntities.Count == 0);

            // ensure reference was reestablished
            Assert.AreSame(child.Children.ToArray()[1], removedGreatGrandChild.Parent);

            // undo the edit to the GreatGrandChild (2 edits undone)
            ((IRevertibleChangeTracking)greatGrandChild).RejectChanges();
            cs = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(cs.AddedEntities.Count == 1 && cs.ModifiedEntities.Count == 2 && cs.RemovedEntities.Count == 0);

            // undo the add to the Child's Children collection (1 add and remaining 2 edits undone)
            ((IRevertibleChangeTracking)child).RejectChanges();
            Assert.IsFalse(child.Children.Contains(addedGrandChild));
            Assert.AreEqual(EntityState.Detached, addedGrandChild.EntityState);
            cs = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(cs.IsEmpty);

            VerifyHierarchy(parent);
        }

        #endregion

        #region Test Helpers
        /// <summary>
        /// Verify successful completion of an update operation
        /// </summary>
        private void VerifySuccess(CompositionScenarios_Explicit ctxt, SubmitOperation so, IEnumerable<Entity> expectedUpdates)
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

        private static Parent CreateCompositionHierarchy(int numChildren, int numGrandChildren)
        {
            int parentKey = 1;
            int childKey = 1;
            int grandChildKey = 1;
            int greatGrandChildKey = 1;

            Parent p = new Parent
            {
                ID = parentKey++
            };

            for (int j = 0; j < numChildren; j++)
            {
                Child c = new Child
                {
                    ID = childKey++,
                    ParentID = p.ID,
                    Parent = p
                };
                p.Children.Add(c);
                for (int k = 0; k < numGrandChildren; k++)
                {
                    GrandChild gc = new GrandChild
                    {
                        ID = grandChildKey++,
                        ParentID = c.ID,
                        Parent = c
                    };
                    c.Children.Add(gc);

                    // add singleton child to grand child
                    gc.Child = new GreatGrandChild
                    {
                        ID = greatGrandChildKey++,
                        ParentID = gc.ID,
                        Parent = gc
                    };
                }
            }

            VerifyHierarchy(p);

            return p;
        }

        /// <summary>
        /// Verify that each node in the hierarchy has the expected children
        /// and all Parent back pointers are valid.
        /// </summary>
        private static void VerifyHierarchy(Parent parent)
        {
            Assert.IsGreaterThan(0, parent.Children.Count);
            foreach (Child c in parent.Children)
            {
                Assert.AreSame(parent, c.Parent);

                Assert.IsGreaterThan(0, c.Children.Count);
                foreach (GrandChild gc in c.Children)
                {
                    Assert.AreSame(c, gc.Parent);
                    if (gc.Child != null)
                    {
                        Assert.AreSame(gc, gc.Child.Parent);
                    }
                }
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
                        System.Reflection.PropertyInfo assocProp = op.Entity.GetType().GetProperty(assoc.Key, System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (assocProp.GetCustomAttributes(typeof(CompositionAttribute), false).Any())
                        {
                            // if there are original ids for this association, capture them
                            int[] origAssocIds = Array.Empty<int>();
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
                                Assert.AreNotEqual(EntityOperationType.Delete, childOperation.Operation);

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
                                Assert.AreNotEqual(EntityOperationType.Insert, childOperation.Operation);
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


        #region Picture/Label/PictureLabel Entities, models mapping of N-to-M relationship using composition for link table.
        private sealed class PictureContainer : EntityContainer
        {
            public PictureContainer()
            {
                CreateEntitySet<Picture>(EntitySetOperations.All);
                CreateEntitySet<Label>(EntitySetOperations.All);
                CreateEntitySet<PictureLabel>(EntitySetOperations.All);
                CreateEntitySet<PictureMetadata>(EntitySetOperations.All);
            }
        }
        [DataContract]
        public sealed class Picture : Entity
        {
            private int _id;
            private EntityCollection<PictureLabel> _pictureLabels;
            private EntityRef<PictureMetadata> _metadata;
            [Key, DataMember]
            public int Id
            {
                get => _id;
                set
                {
                    if (_id == value)
                        return;
                    RaiseDataMemberChanging(nameof(Id));
                    ValidateProperty(nameof(Id), value);
                    _id = value;
                    RaiseDataMemberChanged(nameof(Id));
                }
            }
            [Composition]
            [EntityAssociation("RemovalTest_Picture_PictureLabels", "Id", "PictureId")]
            public EntityCollection<PictureLabel> PictureLabels => _pictureLabels ??=
                new EntityCollection<PictureLabel>(this, nameof(PictureLabels),
                    link => link.PictureId == Id, link => link.Picture = this, link => link.Picture = null);
            /// <summary>
            /// Gets or sets the composed metadata with the same identity as this picture.
            /// </summary>
            [Composition]
            [EntityAssociation("RemovalTest_Picture_Metadata", "Id", "PictureId")]
            public PictureMetadata Metadata
            {
                get => (_metadata ??= new EntityRef<PictureMetadata>(this, nameof(Metadata),
                    entity => entity.PictureId == Id)).Entity;
                set
                {
                    var previous = Metadata;
                    if (previous == value)
                        return;
                    ValidateProperty(nameof(Metadata), value);
                    if (previous != null)
                    {
                        _metadata.Entity = null;
                        previous.Picture = null;
                    }
                    _metadata.Entity = value;
                    if (value != null)
                        value.Picture = this;
                    RaisePropertyChanged(nameof(Metadata));
                }
            }
            public override object GetIdentity() => _id;
        }
        /// <summary>
        /// Represents composed metadata whose primary key is also the foreign key to its picture.
        /// </summary>
        [DataContract, RoundtripOriginal]
        public sealed class PictureMetadata : Entity
        {
            private int _pictureId;
            private EntityRef<Picture> _picture;

            /// <summary>
            /// Gets or sets the primary key and foreign key to the picture.
            /// </summary>
            [Key, DataMember]
            public int PictureId
            {
                get => _pictureId;
                set
                {
                    if (_pictureId == value)
                        return;
                    RaiseDataMemberChanging(nameof(PictureId));
                    ValidateProperty(nameof(PictureId), value);
                    _pictureId = value;
                    RaiseDataMemberChanged(nameof(PictureId));
                }
            }

            /// <summary>
            /// Gets or sets the picture that owns this metadata.
            /// </summary>
            [EntityAssociation("RemovalTest_Picture_Metadata", "PictureId", "Id", IsForeignKey = true)]
            public Picture Picture
            {
                get => (_picture ??= new EntityRef<Picture>(this, nameof(Picture),
                    entity => entity.Id == PictureId)).Entity;
                set
                {
                    var previous = Picture;
                    if (previous == value)
                        return;
                    ValidateProperty(nameof(Picture), value);
                    if (previous != null)
                    {
                        _picture.Entity = null;
                        previous.Metadata = null;
                    }
                    PictureId = value?.Id ?? default;
                    _picture.Entity = value;
                    if (value != null)
                        value.Metadata = this;
                    RaisePropertyChanged(nameof(Picture));
                }
            }

            /// <inheritdoc />
            public override object GetIdentity() => _pictureId;
        }
        [DataContract]
        public sealed class Label : Entity
        {
            private int _id;
            [Key, DataMember]
            public int Id
            {
                get => _id;
                set
                {
                    if (_id == value)
                        return;
                    RaiseDataMemberChanging(nameof(Id));
                    ValidateProperty(nameof(Id), value);
                    _id = value;
                    RaiseDataMemberChanged(nameof(Id));
                }
            }
            public override object GetIdentity() => _id;
        }
        /// <summary>
        /// This entity represents a many-to-many link between <see cref="Picture"/> and <see cref="Label"/>.
        /// It is special since both members are part of the composite key, and it is also a composition of <see cref="Picture"/>.
        /// </summary>
        [DataContract, RoundtripOriginal]
        public sealed class PictureLabel : Entity
        {
            private int _pictureId;
            private int _labelId;
            private EntityRef<Picture> _picture;
            private EntityRef<Label> _label;
            [Key, DataMember]
            public int PictureId
            {
                get => _pictureId;
                set
                {
                    if (_pictureId == value)
                        return;
                    RaiseDataMemberChanging(nameof(PictureId));
                    ValidateProperty(nameof(PictureId), value);
                    _pictureId = value;
                    RaiseDataMemberChanged(nameof(PictureId));
                }
            }
            [Key, DataMember]
            public int LabelId
            {
                get => _labelId;
                set
                {
                    if (_labelId == value)
                        return;
                    RaiseDataMemberChanging(nameof(LabelId));
                    ValidateProperty(nameof(LabelId), value);
                    _labelId = value;
                    RaiseDataMemberChanged(nameof(LabelId));
                }
            }
            [EntityAssociation("RemovalTest_Picture_PictureLabels", "PictureId", "Id", IsForeignKey = true)]
            public Picture Picture
            {
                get => (_picture ??= new EntityRef<Picture>(this, nameof(Picture),
                    entity => entity.Id == PictureId)).Entity;
                set
                {
                    var previous = Picture;
                    if (previous == value)
                        return;
                    ValidateProperty(nameof(Picture), value);
                    if (previous != null)
                    {
                        _picture.Entity = null;
                        previous.PictureLabels.Remove(this);
                    }
                    PictureId = value?.Id ?? default;
                    _picture.Entity = value;
                    if (value != null)
                        value.PictureLabels.Add(this);
                    // Match the association notification in the failing reproduction.
                    RaiseDataMemberChanged(nameof(Picture));
                }
            }
            [EntityAssociation("RemovalTest_Label_PictureLabels", "LabelId", "Id", IsForeignKey = true)]
            public Label Label
            {
                get => (_label ??= new EntityRef<Label>(this, nameof(Label),
                    entity => entity.Id == LabelId)).Entity;
                set
                {
                    if (Label == value)
                        return;
                    ValidateProperty(nameof(Label), value);
                    LabelId = value?.Id ?? default;
                    _label.Entity = value;
                    RaiseDataMemberChanged(nameof(Label));
                }
            }
            public override object GetIdentity() => EntityKey.Create(_pictureId, _labelId);
        }
        #endregion
    }
}
