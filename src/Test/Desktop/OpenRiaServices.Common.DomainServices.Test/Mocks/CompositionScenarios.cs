using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using OpenRiaServices.Server;

namespace TestDomainServices
{
    /// <summary>
    /// This service includes explicit CUD operations for composed children
    /// and verifies that the operations are called in the correct order.
    /// </summary>
    [EnableClientAccess]
    public partial class CompositionScenarios_Explicit : DomainService
    {
        private readonly HashSet<ChangeSetEntry> _invokedOperations = new HashSet<ChangeSetEntry>();
        private readonly List<string> invokedCustomMethods = new List<string>();

        public IQueryable<Parent> GetParents()
        {
            return CompositionHelper.CreateCompositionHierarchy().AsQueryable();
        }

        #region Parent operations
        public void InsertParent(Parent parent)
        {
            CompositionHelper.Validate(parent);
            SetOperationInvoked(parent);

            ((CompositionEntityBase)parent).OperationResult = "Insert";
        }

        public void UpdateParent(Parent parent)
        {
            CompositionHelper.Validate(parent);
            SetOperationInvoked(parent);

            ((CompositionEntityBase)parent).OperationResult = "Update";
        }

        public void DeleteParent(Parent parent)
        {
            CompositionHelper.Validate(parent);
            SetOperationInvoked(parent);

            ((CompositionEntityBase)parent).OperationResult = "Delete";
        }

        public void CustomOp_Parent(Parent parent)
        {
            CompositionHelper.Validate(parent);
            this.SetOperationInvoked(parent, "CustomOp_Parent");
            ((CompositionEntityBase)parent).OperationResult += ",CustomOp_Parent";
        }
        #endregion

        #region Child operations
        public void InsertChild(Child child)
        {
            SetOperationInvoked(child);

            ((CompositionEntityBase)child).OperationResult = "Insert";
        }

        public void UpdateChild(Child child)
        {
            SetOperationInvoked(child);

            ((CompositionEntityBase)child).OperationResult = "Update";
        }

        public void DeleteChild(Child child)
        {
            SetOperationInvoked(child);

            ((CompositionEntityBase)child).OperationResult = "Delete";
        }

        public void CustomOp_Child(Child child)
        {
            this.SetOperationInvoked(child, "CustomOp_Child");
            ((CompositionEntityBase)child).OperationResult += ",CustomOp_Child";
        }
        #endregion

        #region GrandChild operations
        public void InsertGrandChild(GrandChild grandChild)
        {
            SetOperationInvoked(grandChild);

            ((CompositionEntityBase)grandChild).OperationResult = "Insert";
        }

        public void UpdateGrandChild(GrandChild grandChild)
        {
            SetOperationInvoked(grandChild);

            ((CompositionEntityBase)grandChild).OperationResult = "Update";
        }

        public void DeleteGrandChild(GrandChild grandChild)
        {
            SetOperationInvoked(grandChild);

            ((CompositionEntityBase)grandChild).OperationResult = "Delete";
        }

        public void CustomOp_GrandChild(GrandChild grandChild)
        {
            this.SetOperationInvoked(grandChild, "CustomOp_GrandChild");
            ((CompositionEntityBase)grandChild).OperationResult += ",CustomOp_GrandChild";
        }
        #endregion

        #region GreatGrandChild operations
        public void InsertGreatGrandChild(GreatGrandChild greatGrandChild)
        {
            SetOperationInvoked(greatGrandChild);

            ((CompositionEntityBase)greatGrandChild).OperationResult = "Insert";
        }

        public void UpdateGreatGrandChild(GreatGrandChild greatGrandChild)
        {
            SetOperationInvoked(greatGrandChild);

            ((CompositionEntityBase)greatGrandChild).OperationResult = "Update";
        }

        public void DeleteGreatGrandChild(GreatGrandChild greatGrandChild)
        {
            SetOperationInvoked(greatGrandChild);

            ((CompositionEntityBase)greatGrandChild).OperationResult = "Delete";
        }

        public void CustomOp_GreatGrandChild(GreatGrandChild greatGrandChild)
        {
            this.SetOperationInvoked(greatGrandChild, "CustomOp_GreatGrandChild");
            ((CompositionEntityBase)greatGrandChild).OperationResult += ",CustomOp_GreatGrandChild";
        }
        #endregion

        #region Test Helper methods
        /// <summary>
        /// Overridden to do some pre-validation of the changeset
        /// </summary>
        protected override ValueTask<bool> ExecuteChangeSetAsync(CancellationToken cancellationToken)
        {
            foreach (ChangeSetEntry operation in this.ChangeSet.ChangeSetEntries.Where(p => p.Entity.GetType() == typeof(Parent)))
            {
                Parent parent = (Parent)operation.Entity;

                NavigateChildChanges(parent);

                // verify that all child collections contain unique instances
                // and contain valid operations
                HashSet<object> visited = new HashSet<object>();
                this.VerifyUniqueCollection(parent.Children);
                foreach (Child child in parent.Children)
                {
                    this.VerifyUniqueCollection(child.Children);
                }
            }

            return base.ExecuteChangeSetAsync(cancellationToken);
        }

        /// <summary>
        /// Use the changeset composition APIs to navigate all child updates
        /// </summary>
        /// <param name="parent"></param>
        private void NavigateChildChanges(Parent parent)
        {
            // if the parent has had property modifications, original will
            // be non-null
            if (this.ChangeSet.GetChangeOperation(parent) != ChangeOperation.Insert)
            {
                Parent originalParent = this.ChangeSet.GetOriginal(parent);
            }

            // navigate all child changes w/o specifying operation type
            Dictionary<object, ChangeOperation> changeOperationMap = new Dictionary<object, ChangeOperation>();
            foreach (Child child in this.ChangeSet.GetAssociatedChanges(parent, p => p.Children))
            {
                ChangeOperation op = this.ChangeSet.GetChangeOperation(child);             
                changeOperationMap[child] = op;

                if (this.ChangeSet.GetChangeOperation(child) != ChangeOperation.Insert)
                {
                    Child originalChild = this.ChangeSet.GetOriginal(child);
                }

                if (op == ChangeOperation.None)
                {

                }
                else if (op == ChangeOperation.Insert)
                {
                    CompositionHelper.Assert(this.ChangeSet.ChangeSetEntries.SingleOrDefault(p => p.Entity == child && p.Operation == DomainOperation.Insert) != null,
                    "Expected corresponding insert operation not found.");
                }
                else if (op == ChangeOperation.Update)
                {
                    CompositionHelper.Assert(this.ChangeSet.ChangeSetEntries.SingleOrDefault(p => p.Entity == child && p.Operation == DomainOperation.Update) != null,
                    "Expected corresponding update operation not found.");
                }
                else if (op == ChangeOperation.Delete)
                {
                    CompositionHelper.Assert(this.ChangeSet.ChangeSetEntries.SingleOrDefault(p => p.Entity == child && p.Operation == DomainOperation.Delete) != null,
                    "Expected corresponding delete operation not found.");
                }

                foreach (GrandChild grandChild in this.ChangeSet.GetAssociatedChanges(child, p => p.Children))
                {
                    changeOperationMap[grandChild] = this.ChangeSet.GetChangeOperation(grandChild);
                    foreach (GreatGrandChild greatGrandChild in this.ChangeSet.GetAssociatedChanges(grandChild, p => p.Child))
                    {
                        changeOperationMap[greatGrandChild] = this.ChangeSet.GetChangeOperation(greatGrandChild);
                    }
                }
            }

            // verify all child operations against the map we built up during enumeration
            // of associated changes to ensure all operations were returned
            foreach (ChangeSetEntry operation in this.ChangeSet.ChangeSetEntries.Where(p => p.Entity.GetType() != typeof(Parent)))
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
            foreach (Child child in this.ChangeSet.GetAssociatedChanges(parent, p => p.Children, ChangeOperation.None))
            {
            }
            foreach (Child child in this.ChangeSet.GetAssociatedChanges(parent, p => p.Children, ChangeOperation.Insert))
            {
            }
            foreach (Child child in this.ChangeSet.GetAssociatedChanges(parent, p => p.Children, ChangeOperation.Update))
            {
            }
            foreach (Child child in this.ChangeSet.GetAssociatedChanges(parent, p => p.Children, ChangeOperation.Delete))
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
            if (entity is Child)
            {
                parent = ((Child)entity).Parent;
            }
            else if (entity is GrandChild)
            {
                parent = ((GrandChild)entity).Parent;
            }
            else if (entity is GreatGrandChild)
            {
                parent = ((GreatGrandChild)entity).Parent;
            }

            // search the changeset for parent operations
            IEnumerable<ChangeSetEntry> parentOperations = this.ChangeSet.ChangeSetEntries.Where(p => p.Entity == parent);
            foreach(ChangeSetEntry parentOperation in parentOperations)
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

    /// <summary>
    /// This service only has operations for the parent type
    /// </summary>
    [EnableClientAccess]
#if !NET6_0
    [ServiceContract(Name = "CompositionScenarios_Explicit")]
#endif
    public partial class CompositionScenarios_Implicit : DomainService
    {
        public IQueryable<Parent> GetParents()
        {
            return CompositionHelper.CreateCompositionHierarchy().AsQueryable();
        }

        public void InsertParent(Parent parent)
        {
            // first validate the hierarchy
            CompositionHelper.Validate(parent);

            ((CompositionEntityBase)parent).OperationResult = "Insert";

            // for a new parent, all children will also be new
            // so we enumerate all and "add" them
            foreach (Child child in parent.Children)
            {
                ((CompositionEntityBase)child).OperationResult = "Insert";
                foreach (GrandChild grandChild in child.Children)
                {
                    ((CompositionEntityBase)grandChild).OperationResult = "Insert";
                    GreatGrandChild greatGrandChild = grandChild.Child;
                    if (greatGrandChild != null)
                    {
                        ((CompositionEntityBase)greatGrandChild).OperationResult = "Insert";
                    }
                }
            }
        }

        public void UpdateParent(Parent parent)
        {
            // first validate the hierarchy
            CompositionHelper.Validate(parent);

            ((CompositionEntityBase)parent).OperationResult = "Update";

            // for an updated, children might be added, updated
            // or deleted. We need to enumerate all and act appropriately
            foreach (Child child in this.ChangeSet.GetAssociatedChanges(parent, p => p.Children))
            {
                ChangeOperation changeOp = this.ChangeSet.GetChangeOperation(child);
                if (changeOp == ChangeOperation.Insert)
                {
                    ((CompositionEntityBase)child).OperationResult = "Insert";
                }
                else if (changeOp == ChangeOperation.Update)
                {
                    ((CompositionEntityBase)child).OperationResult = "Update";
                }
                else if (changeOp == ChangeOperation.Delete)
                {
                    ((CompositionEntityBase)child).OperationResult = "Delete";
                }

                // now process child updates for the child
                foreach (GrandChild grandChild in this.ChangeSet.GetAssociatedChanges(child, p => p.Children))
                {
                    changeOp = this.ChangeSet.GetChangeOperation(grandChild);
                    if (changeOp == ChangeOperation.Insert)
                    {
                        ((CompositionEntityBase)grandChild).OperationResult = "Insert";
                    }
                    else if (changeOp == ChangeOperation.Update)
                    {
                        ((CompositionEntityBase)grandChild).OperationResult = "Update";
                    }
                    else if (changeOp == ChangeOperation.Delete)
                    {
                        ((CompositionEntityBase)grandChild).OperationResult = "Delete";
                    }

                    // finally, process any great grand child updates
                    GreatGrandChild updatedGreateGrandChild = this.ChangeSet.GetAssociatedChanges(grandChild, p => p.Child).Cast<GreatGrandChild>().SingleOrDefault();
                    if (updatedGreateGrandChild != null)
                    {
                        changeOp = this.ChangeSet.GetChangeOperation(updatedGreateGrandChild);
                        if (changeOp == ChangeOperation.Insert)
                        {
                            ((CompositionEntityBase)updatedGreateGrandChild).OperationResult = "Insert";
                        }
                        else if (changeOp == ChangeOperation.Update)
                        {
                            ((CompositionEntityBase)updatedGreateGrandChild).OperationResult = "Update";
                        }
                        else if (changeOp == ChangeOperation.Delete)
                        {
                            ((CompositionEntityBase)updatedGreateGrandChild).OperationResult = "Delete";
                        }
                    }
                }
            }
        }

        public void DeleteParent(Parent parent)
        {
            // normally you wouldn't validate a deleted hierarchy,
            // but we do it here for test validation
            CompositionHelper.Validate(parent);

            ((CompositionEntityBase)parent).OperationResult = "Delete";

            // Delete all children in the hierarchy.
            foreach (Child child in parent.Children)
            {
                foreach (GrandChild grandChild in child.Children)
                {
                    GreatGrandChild greatGrandChild = grandChild.Child;
                    if (greatGrandChild != null)
                    {
                    }
                }
            }
        }

        public void CustomOp_Parent(Parent parent)
        {
            // first validate the hierarchy
            CompositionHelper.Validate(parent);

            ((CompositionEntityBase)parent).OperationResult += ",CustomOp_Parent";
        }

        public void CustomOp_Child(Child child)
        {
            ((CompositionEntityBase)child).OperationResult += ",CustomOp_Child";
        }

        public void CustomOp_GrandChild(GrandChild grandChild)
        {
            ((CompositionEntityBase)grandChild).OperationResult += ",CustomOp_GrandChild";
        }

        public void CustomOp_GreatGrandChild(GreatGrandChild greatGrandChild)
        {
            ((CompositionEntityBase)greatGrandChild).OperationResult += ",CustomOp_GreatGrandChild";
        }
    }

#region Composition scenarios
    /// <summary>
    /// Domain service used to reproduce various bug scenarios etc.
    /// </summary>
    [EnableClientAccess]
    public partial class CompositionScenarios_Various : DomainService
    {
        public IEnumerable<SelfReferencingComposition_OneToMany> GetSelfReferencingComposition_OneToManys()
        {
            return null;
        }

        // expose an entity that is simultaneously both a parent and a child
        // we expect a public entity list and query method for the type
        public IEnumerable<SelfReferencingComposition> GetSelfReferencingCompositions()
        {
            List<SelfReferencingComposition> entities = new List<SelfReferencingComposition>();

            SelfReferencingComposition parent1 = new SelfReferencingComposition { ID = 1, Value = "A" };
            entities.Add(parent1);
            SelfReferencingComposition child1 = new SelfReferencingComposition { ID = 2, ParentID = 1, Parent = parent1, Value = "B" };
            parent1.Child = child1;
            entities.Add(child1);

            SelfReferencingComposition parent2 = new SelfReferencingComposition { ID = 3, Value = "C" };
            entities.Add(parent2);
            SelfReferencingComposition child2 = new SelfReferencingComposition { ID = 4, ParentID = 3, Parent = parent2, Value = "D" };
            parent2.Child = child2;
            entities.Add(child2);

            // parent with no children
            SelfReferencingComposition parent3 = new SelfReferencingComposition { ID = 5, Value = "C" };
            entities.Add(parent3);

            return entities;
        }

        public void UpdateSelfReferencingComposition(SelfReferencingComposition entity)
        {

        }

        // expose query methods for parent AND children. We expect EntityQueries
        // to be generated, but no public child lists
        public IQueryable<CompositionScenarios_Parent> GetParents() 
        { 
            return null; 
        }
        
        public IQueryable<CompositionScenarios_Child> GetChildren(int parentID) 
        {
            return new CompositionScenarios_Child[] { new CompositionScenarios_Child { ID = 1, ParentID = 1 } }.AsQueryable(); 
        }

        public void UpdateParent(CompositionScenarios_Parent parent)
        {
        }
    }

    public class CompositionScenarios_Parent
    {
        [Key]
        public int ID { get; set; }

        public string A { get; set; }

        [Composition]
        [Include]
        [Association("Parent_Child", "ID", "ParentID")]
        public List<CompositionScenarios_Child> Children { get; set; }
    }

    public class CompositionScenarios_Child
    {
        [Key]
        public int ID { get; set; }

        public int ParentID { get; set; }

        public string A { get; set; }

        [Association("Parent_Child", "ParentID", "ID", IsForeignKey = true)]
        public CompositionScenarios_Parent Parent { get; set; }
    }

    [EnableClientAccess]
    public class Composition_TestCyclicComposition : DomainService
    {
        public IEnumerable<CompositionCycle_A> GetAs() { return null; }

        // only expose insert to induce a cyclic lookup
        public void InsertA(CompositionCycle_A a) { }
    }

    public class CompositionCycle_A
    {
        [Key]
        public int ID { get; set; }

        [Composition]
        [Include]
        [Association("A_B", "ID", "ParentID")]
        public List<CompositionCycle_B> Bs { get; set; }
    }

    public class CompositionCycle_B
    {
        [Key]
        public int ID { get; set; }

        public int ParentID { get; set; }

        [Composition]
        [Include]
        [Association("B_C", "ID", "ParentID")]
        public List<CompositionCycle_C> Cs { get; set; }
    }

    public class CompositionCycle_C
    {
        [Key]
        public int ID { get; set; }

        public int ParentID { get; set; }

        [Composition]
        [Include]
        [Association("C_D", "ID", "ParentID")]
        public List<CompositionCycle_D> Ds { get; set; }
    }

    public class CompositionCycle_D
    {
        [Key]
        public int ID { get; set; }

        public int ParentID { get; set; }

        public int BID { get; set; }

        [Composition]
        [Include]
        [Association("D_B", "BID", "ID", IsForeignKey = true)]
        public CompositionCycle_B B { get; set; }
    }

    [EnableClientAccess]
    public partial class CompositionScenarios_NamedUpdate : DomainService
    {
        public IEnumerable<Parent> GetParents()
        {
            return null;
        }

        /// <summary>
        /// Verify that a "named update" method results in the expected
        /// child operations being permitted during codegen
        /// </summary>
        [EntityAction]
        public void CustomParentUpdate(Parent parent)
        {
        }
    }

    [EnableClientAccess]
    public partial class CompositionScenarios_SelfReferencingComposition_Update : DomainService
    {
        public IEnumerable<SelfReferencingComposition> GetSelfReferencingCompositions()
        {
            return null;
        }

        public void Update(SelfReferencingComposition sr)
        {
        }
    }

    [EnableClientAccess]
    public partial class CompositionScenarios_SelfReferencingComposition_NamedUpdate : DomainService
    {
        public IEnumerable<SelfReferencingComposition> GetSelfReferencingCompositions()
        {
            return null;
        }

        [EntityAction]
        public void CustomParentUpdate(SelfReferencingComposition sr)
        {
        }
    }

    public class SelfReferencingComposition
    {
        [Key]
        public int ID {get;set;}

        [Include]
        [Association("Ref_Assoc", "ParentID", "ID", IsForeignKey = true)]
        public SelfReferencingComposition Parent { get;set; }

        /// <summary>
        /// Self referencing composition
        /// </summary>
        [Composition]
        [Include]
        [Association("Ref_Assoc", "ID", "ParentID")]
        public SelfReferencingComposition Child { get; set; }

        public int ParentID { get;set;}

        public string Value { get;set; }
    }

    public class SelfReferencingComposition_OneToMany
    {
        [Key]
        public int ID { get; set; }

        public int ParentID { get; set; }

        public string Value { get; set; }

        [Association("SelfReferencingComposition_OneToMany", "ParentID", "ID", IsForeignKey = true)]
        public SelfReferencingComposition_OneToMany Parent { get; set; }

        [Composition]
        [Include]
        [Association("SelfReferencingComposition_OneToMany", "ID", "ParentID")]
        public List<SelfReferencingComposition_OneToMany> Children { get; set; }
    }
#endregion

    public static class CompositionHelper
    {
        public static void Validate(Parent parent)
        {
            // Simulate a validation pass that enumerates all children.
            foreach (Child child in parent.Children)
            {
                foreach (GrandChild grandChild in child.Children)
                {
                    GreatGrandChild greatGrandChild = grandChild.Child;
                }
            }
        }
       
        /// <summary>
        /// Create a 4 level compositional hierarchy that includes both
        /// collection and singleton compositions
        /// </summary>
        public static List<Parent> CreateCompositionHierarchy()
        {
            int parentKey = 1;
            int childKey = 1;
            int grandChildKey = 1;
            int greatGrandChildKey = 1;

            List<Parent> parents = new List<Parent>();
            for (int i = 0; i < 3; i++)
            {
                Parent p = new Parent
                {
                    ID = parentKey++
                };
                parents.Add(p);
                for (int j = 0; j < 3; j++)
                {
                    Child c = new Child
                    {
                        ID = childKey++, ParentID = p.ID, Parent = p
                    };
                    p.Children.Add(c);
                    for (int k = 0; k < 3; k++)
                    {
                        GrandChild gc = new GrandChild
                        {
                            ID = grandChildKey++, ParentID = c.ID, Parent = c
                        };
                        c.Children.Add(gc);

                        // add singleton child to grand child
                        gc.Child = new GreatGrandChild
                        {
                            ID = greatGrandChildKey++, ParentID = gc.ID, Parent = gc
                        };
                    }
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

    public class CompositionEntityBase
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

    public partial class Parent : CompositionEntityBase
    {
        private List<Child> _children = new List<Child>();

        public int ID
        {
            get;
            set;
        }

        public List<Child> Children
        {
            get
            {
                return this._children;
            }
            set
            {
                this._children = value;
            }
        }
    }

    public partial class Child : CompositionEntityBase
    {
        private List<GrandChild> _children = new List<GrandChild>();

        public int ID
        {
            get;
            set;
        }

        public int ParentID
        {
            get;
            set;
        }

        public Parent Parent
        {
            get;
            set;
        }

        public List<GrandChild> Children
        {
            get
            {
                return this._children;
            }
            set
            {
                this._children = value;
            }
        }
    }

    public partial class GrandChild : CompositionEntityBase
    {
        public int ID
        {
            get;
            set;
        }

        public int ParentID
        {
            get;
            set;
        }

        public Child Parent
        {
            get;
            set;
        }

        /// <summary>
        /// Singleton composition
        /// </summary>
        public GreatGrandChild Child
        {
            get;
            set;
        }
    }

    public partial class GreatGrandChild : CompositionEntityBase
    {
        public int ID
        {
            get;
            set;
        }

        public int ParentID
        {
            get;
            set;
        }

        public GrandChild Parent
        {
            get;
            set;
        }
    }

#region Metadata
    [MetadataType(typeof(ParentMetadata))]
    public partial class Parent
    {
    }

    public static class ParentMetadata
    {
        [Key]
        public static object ID;

        [Composition]
        [Include]
        [Association("Child_Parent", "ID", "ParentID")]
        public static object Children;
    }

    [MetadataType(typeof(ChildMetadata))]
    public partial class Child
    {
    }

    public static class ChildMetadata
    {
        [Key]
        public static object ID;

        [Include]
        [Association("Child_Parent", "ParentID", "ID", IsForeignKey = true)]
        public static object Parent;

        [Composition]
        [Include]
        [Association("GrandChild_Child", "ID", "ParentID")]
        public static object Children;
    }

    [MetadataType(typeof(GrandChildMetadata))]
    public partial class GrandChild
    {
    }

    public static class GrandChildMetadata
    {
        [Key]
        public static object ID;

        [Include]
        [Association("GrandChild_Child", "ParentID", "ID", IsForeignKey = true)]
        public static object Parent;

        [Include]
        [Association("GreatGrandChild_GrandChild", "ID", "ParentID")]
        [Composition]
        public static object Child;
    }

    [MetadataType(typeof(GreatGrandChildMetadata))]
    public partial class GreatGrandChild
    {
    }

    public static class GreatGrandChildMetadata
    {
        [Key]
        public static object ID;

        [Include]
        [Association("GreatGrandChild_GrandChild", "ParentID", "ID", IsForeignKey = true)]
        public static object Parent;
    }
#endregion
}
