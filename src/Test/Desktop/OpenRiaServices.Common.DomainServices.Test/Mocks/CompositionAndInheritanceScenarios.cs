using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using OpenRiaServices.Server;

namespace TestDomainServices
{
    /// <summary>
    /// This service includes explicit CUD operations for composed children
    /// and for derived parents that have other composition elements
    /// and verifies that the operations are called in the correct order.
    /// </summary>
    [EnableClientAccess]
    public partial class CompositionInheritanceScenarios : DomainService
    {
        private readonly HashSet<ChangeSetEntry> _invokedOperations = new HashSet<ChangeSetEntry>();
        private readonly List<string> invokedCustomMethods = new List<string>();

        public IQueryable<CI_Parent> GetParents()
        {
            return CompositionInheritanceHelper.CreateCompositionHierarchy().AsQueryable();
        }

        #region Parent operations
        public void InsertParent(CI_Parent parent)
        {
            //CompositionHelper.Validate(parent);
            SetOperationInvoked(parent);

            parent.OperationResult = "Insert";
        }

        public void UpdateParent(CI_Parent parent)
        {
            //CompositionHelper.Validate(parent);
            SetOperationInvoked(parent);

            parent.OperationResult = "Update";
        }

        public void DeleteParent(CI_Parent parent)
        {
            //CompositionHelper.Validate(parent);
            SetOperationInvoked(parent);

            parent.OperationResult = "Delete";
        }

        public void CustomOp_Parent(CI_Parent parent)
        {
            //CompositionHelper.Validate(parent);
            this.SetOperationInvoked(parent, "CustomOp_Parent");
            parent.OperationResult += ",CustomOp_Parent";
        }
        #endregion

        #region Child operations
        public void InsertChild(CI_Child child)
        {
            SetOperationInvoked(child);

            child.OperationResult = "Insert";
        }

        public void UpdateChild(CI_Child child)
        {
            SetOperationInvoked(child);

           child.OperationResult = "Update";
        }

        public void DeleteChild(CI_Child child)
        {
            SetOperationInvoked(child);

            child.OperationResult = "Delete";
        }

        public void CustomOp_Child(CI_Child child)
        {
            this.SetOperationInvoked(child, "CustomOp_Child");
            child.OperationResult += ",CustomOp_Child";
        }
        #endregion

        #region AdoptedChild operations
        public void InsertAdoptedChild(CI_AdoptedChild car)
        {
            SetOperationInvoked(car);

            car.OperationResult = "Insert";
        }

        public void UpdateAdoptedChild(CI_AdoptedChild child)
        {
            SetOperationInvoked(child);

            child.OperationResult = "Update";
        }

        public void DeleteAdoptedChild(CI_AdoptedChild child)
        {
            SetOperationInvoked(child);

            child.OperationResult = "Delete";
        }

        public void CustomOp_AdoptedChild(CI_AdoptedChild child)
        {
            this.SetOperationInvoked(child, "CustomOp_AdoptedChild");
            child.OperationResult += ",CustomOp_AdoptedChild";
        }
        #endregion

        #region Test Helper methods
        /// <summary>
        /// Overridden to do some pre-validation of the changeset
        /// </summary>
        protected override ValueTask<bool> ExecuteChangeSetAsync(CancellationToken cancellationToken)
        {
            foreach (ChangeSetEntry operation in this.ChangeSet.ChangeSetEntries.Where(p => p.Entity.GetType() == typeof(CI_Parent)))
            {
                CI_Parent parent = (CI_Parent)operation.Entity;

                NavigateChildChanges(parent);

                // verify that all child collections contain unique instances
                // and contain valid operations
                HashSet<object> visited = new HashSet<object>();
                this.VerifyUniqueCollection(parent.Children);
            }

            return base.ExecuteChangeSetAsync(cancellationToken);
        }

        /// <summary>
        /// Use the changeset composition APIs to navigate all child updates
        /// </summary>
        /// <param name="parent"></param>
        private void NavigateChildChanges(CI_Parent parent)
        {
            // if the parent has had property modifications, original will
            // be non-null
            if (this.ChangeSet.GetChangeOperation(parent) != ChangeOperation.Insert)
            {
                CI_Parent originalParent = this.ChangeSet.GetOriginal(parent);
            }

            // navigate all child changes w/o specifying operation type
            Dictionary<object, ChangeOperation> changeOperationMap = new Dictionary<object, ChangeOperation>();
            foreach (CI_Child child in this.ChangeSet.GetAssociatedChanges(parent, p => p.Children))
            {
                ChangeOperation op = this.ChangeSet.GetChangeOperation(child);
                changeOperationMap[child] = op;

                if (this.ChangeSet.GetChangeOperation(child) != ChangeOperation.Insert)
                {
                    CI_Child originalChild = this.ChangeSet.GetOriginal(child);
                }

                if (op == ChangeOperation.None)
                {

                }
                else if (op == ChangeOperation.Insert)
                {
                    CompositionInheritanceHelper.Assert(this.ChangeSet.ChangeSetEntries.SingleOrDefault(p => p.Entity == child && p.Operation == DomainOperation.Insert) != null,
                    "Expected corresponding insert operation not found.");
                }
                else if (op == ChangeOperation.Update)
                {
                    CompositionInheritanceHelper.Assert(this.ChangeSet.ChangeSetEntries.SingleOrDefault(p => p.Entity == child && p.Operation == DomainOperation.Update) != null,
                    "Expected corresponding update operation not found.");
                }
                else if (op == ChangeOperation.Delete)
                {
                    CompositionInheritanceHelper.Assert(this.ChangeSet.ChangeSetEntries.SingleOrDefault(p => p.Entity == child && p.Operation == DomainOperation.Delete) != null,
                    "Expected corresponding delete operation not found.");
                }
            }

            // verify all child operations against the map we built up during enumeration
            // of associated changes to ensure all operations were returned
            foreach (ChangeSetEntry operation in this.ChangeSet.ChangeSetEntries.Where(p => p.Entity.GetType() != typeof(CI_Parent)))
            {
                switch (operation.Operation)
                {
                    case DomainOperation.Insert:
                        CompositionHelper.Assert(changeOperationMap.ContainsKey(operation.Entity) &&
                            changeOperationMap[operation.Entity] == ChangeOperation.Insert,
                            "Expected insert operation was not returned from GetAssociatedChanges.");
                        break;
                    case DomainOperation.Update:
                        CompositionHelper.Assert(changeOperationMap.ContainsKey(operation.Entity) &&
                            changeOperationMap[operation.Entity] == ChangeOperation.Update,
                            "Expected update operation was not returned from GetAssociatedChanges.");
                        break;
                    case DomainOperation.Delete:
                        CompositionHelper.Assert(
                            changeOperationMap.ContainsKey(operation.Entity) &&
                            changeOperationMap[operation.Entity] == ChangeOperation.Delete,
                            "Expected delete operation was not returned from GetAssociatedChanges.");
                        break;
                }
            }

            // navigate all child changes specifying operation type
            foreach (CI_Child child in this.ChangeSet.GetAssociatedChanges(parent, p => p.Children, ChangeOperation.None))
            {
            }
            foreach (CI_Child child in this.ChangeSet.GetAssociatedChanges(parent, p => p.Children, ChangeOperation.Insert))
            {
            }
            foreach (CI_Child child in this.ChangeSet.GetAssociatedChanges(parent, p => p.Children, ChangeOperation.Update))
            {
            }
            foreach (CI_Child child in this.ChangeSet.GetAssociatedChanges(parent, p => p.Children, ChangeOperation.Delete))
            {
            }
        }

        /// <summary>
        /// For the specified collection, verify that each instance in the collection is unique.
        /// </summary>
        /// <param name="entities"></param>
        private void VerifyUniqueCollection(System.Collections.IEnumerable entities)
        {
            int count = entities.Cast<object>().Count();
            if (entities.Cast<object>().Distinct().Count() != count)
            {
                throw new Exception("Duplicate child");
            }
        }

        /// <summary>
        /// Record an operation invocation, and validate that all parent operations
        /// have been completed
        /// </summary>
        /// <param name="entity"></param>
        private void SetOperationInvoked(object entity)
        {
            VerifyOperationOrdering(entity, false);

            // record the operation invocation
            ChangeSetEntry op = this.ChangeSet.ChangeSetEntries.Single(p => p.Entity == entity);
            _invokedOperations.Add(op);
        }

        private void SetOperationInvoked(object entity, string customMethod)
        {
            VerifyOperationOrdering(entity, true);

            // record the operation invocation
            invokedCustomMethods.Add(customMethod);
        }

        /// <summary>
        /// For the specified entity, recursively verifies that all parent operations
        /// have been completed
        /// </summary>
        private void VerifyOperationOrdering(object entity, bool isCustomMethod)
        {
            // determine the parent
            object parent = null;
            if (entity is CI_AdoptedChild)
            {
                parent = ((CI_AdoptedChild)entity).Parent;
            }
            else if (entity is CI_Child)
            {
                parent = ((CI_Child)entity).Parent;
            }

            // search the changeset for parent operations
            IEnumerable<ChangeSetEntry> parentOperations = this.ChangeSet.ChangeSetEntries.Where(p => p.Entity == parent);
            foreach (ChangeSetEntry parentOperation in parentOperations)
            {
                if (!isCustomMethod)
                {
                    CompositionHelper.Assert(this._invokedOperations.Contains(parentOperation), "Child operation executed before parent!");
                }
                else
                {
                    if (parentOperation.EntityActions != null && parentOperation.EntityActions.Any())
                    {
                        var entityAction = parentOperation.EntityActions.Single();
                        CompositionHelper.Assert(this.invokedCustomMethods.Contains(entityAction.Key), "Child custom method executed before parent!");
                    }
                }

                // now recursively verify
                this.VerifyOperationOrdering(parentOperation.Entity, isCustomMethod);
            }
        }
        #endregion
    }


    public static class CompositionInheritanceHelper
    {
        //public static void Validate(Parent parent)
        //{
        //    // Simulate a validation pass that enumerates all children.
        //    foreach (Child child in parent.Children)
        //    {
        //        foreach (GrandChild grandChild in child.Children)
        //        {
        //            GreatGrandChild greatGrandChild = grandChild.Child;
        //        }
        //    }
        //}

        /// <summary>
        /// Create  compositional hierarchy that includes both
        /// collection and singleton compositions
        /// </summary>
        public static List<CI_Parent> CreateCompositionHierarchy()
        {
            int parentKey = 1;
            int childKey = 1;

            // Create 3 Parents with children.
            List<CI_Parent> parents = new List<CI_Parent>();
            for (int i = 0; i < 3; i++)
            {
                // one of these is a derived parent type
                CI_Parent p = i == 1 ? new CI_SpecialParent() : new CI_Parent();
                
                p.ID = parentKey++;
                parents.Add(p);

                // Create 2 natural children and 2 adopted children
                // It is critical for these scenarios to have the potential
                // for both modified and unmodified derived composition children
                // in a changeset
                for (int j = 0; j < 2; j++)
                {
                    CI_Child c = new CI_Child
                    {
                        ID = childKey++,
                        ParentID = p.ID,
                        Parent = p
                    };
                    p.Children.Add(c);
                }
                for (int j = 0; j < 2; j++)
                {
                    CI_AdoptedChild c = new CI_AdoptedChild
                    {
                        ID = childKey++,
                        ParentID = p.ID,
                        Parent = p
                    };
                    p.Children.Add(c);
                }
            }

            return parents;
        }

        public static void Assert(bool test, string message)
        {
            if (!test)
            {
                throw new Exception(message);
            }
        }
    }

    public class CI_EntityBase
    {
        public string OperationResult
        {
            get;
            set;
        }

        public string Property
        {
            get;
            set;
        }
    }

    public partial class CI_Person : CI_EntityBase
    {
        [Key]
        public int ID
        {
            get;
            set;
        }
    }

    [KnownType(typeof(CI_SpecialParent))]
    public partial class CI_Parent : CI_Person
    {
        private List<CI_Child> _naturalChildren = new List<CI_Child>();

        [Composition]
        [Include]
        [Association("Child_Parent", "ID", "ParentID")]
        public List<CI_Child> Children
        {
            get
            {
                return this._naturalChildren;
            }
            set
            {
                this._naturalChildren = value;
            }
        }
    }

        [KnownType(typeof(CI_AdoptedChild))]
    public partial class CI_Child : CI_Person
    {
        public int Age
        {
            get;
            set;
        }

        public int ParentID
        {
            get;
            set;
        }

        [Include]
        [Association("Child_Parent", "ParentID", "ID", IsForeignKey = true)]
        public CI_Parent Parent
        {
            get;
            set;
        }
    }

    public partial class CI_AdoptedChild : CI_Child
    {
    }

    public partial class CI_SpecialParent : CI_Parent
    {
    }
}
