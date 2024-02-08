using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace OpenRiaServices.Server
{
    /// <summary>
    /// Represents a set of changes to be processed by a <see cref="DomainService"/>.
    /// </summary>
    public sealed class ChangeSet
    {
        private Dictionary<object, object> _entitiesToReplace;
        private readonly IEnumerable<ChangeSetEntry> _changeSetEntries;
        private readonly Dictionary<object, ChangeOperation> _entityStatusMap;
        private Dictionary<object, List<AssociatedEntityInfo>> _associatedStoreEntities;
        private Dictionary<object, Dictionary<PropertyDescriptor, IEnumerable<ChangeSetEntry>>> _associatedChangesMap;

        /// <summary>
        /// Initializes a new instance of the ChangeSet class
        /// </summary>
        /// <param name="changeSetEntries">The set of <see cref="ChangeSetEntry"/> items this <see cref="ChangeSet"/> represents.</param>
        /// <exception cref="ArgumentNullException">if <paramref name="changeSetEntries"/> is null.</exception>
        public ChangeSet(IEnumerable<ChangeSetEntry> changeSetEntries)
        {
            if (changeSetEntries == null)
            {
                throw new ArgumentNullException(nameof(changeSetEntries));
            }

            // ensure the changeset is valid
            ValidateChangeSetEntries(changeSetEntries);

            // build a status map for the operations
            bool requiresOrdering = false;
            this._entityStatusMap = new Dictionary<object, ChangeOperation>();
            foreach (ChangeSetEntry changeSetEntry in changeSetEntries)
            {
                object entity = changeSetEntry.Entity;
                switch (changeSetEntry.Operation)
                {
                    case DomainOperation.Insert:
                        this._entityStatusMap[entity] = ChangeOperation.Insert;
                        break;
                    case DomainOperation.Update:
                        this._entityStatusMap[entity] = ChangeOperation.Update;
                        break;
                    case DomainOperation.Delete:
                        this._entityStatusMap[entity] = ChangeOperation.Delete;
                        break;
                    case DomainOperation.None:
                        this._entityStatusMap[entity] = ChangeOperation.None;
                        break;
                }

                if (MetaType.GetMetaType(entity.GetType()).HasComposition)
                {
                    requiresOrdering = true;
                }
            }

            this._changeSetEntries = changeSetEntries;
            if (requiresOrdering)
            {
                this._changeSetEntries = OrderChangeset(changeSetEntries);
            }
        }

        /// <summary>
        /// Validates that the specified entries are well formed.
        /// </summary>
        /// <param name="changeSetEntries">The changeset entries to validate.</param>
        private static void ValidateChangeSetEntries(IEnumerable<ChangeSetEntry> changeSetEntries)
        {
            HashSet<int> idSet = new HashSet<int>();
            HashSet<object> entitySet = new HashSet<object>();
            foreach (ChangeSetEntry entry in changeSetEntries)
            {
                // ensure Entity is not null
                if (entry.Entity == null)
                {
                    throw new InvalidOperationException(
                        string.Format(CultureInfo.CurrentCulture, Resource.InvalidChangeSet, Resource.InvalidChangeSet_NullEntity));
                }

                // ensure unique client IDs
                if (!idSet.Add(entry.Id))
                {
                    throw new InvalidOperationException(
                        string.Format(CultureInfo.CurrentCulture, Resource.InvalidChangeSet, Resource.InvalidChangeSet_DuplicateId));
                }

                // ensure unique entity instances - there can only be a single entry
                // for a given entity instance
                if (!entitySet.Add(entry.Entity))
                {
                    throw new InvalidOperationException(
                        string.Format(CultureInfo.CurrentCulture, Resource.InvalidChangeSet, Resource.InvalidChangeSet_DuplicateEntity));
                }

                // entities must be of the same type
                if (entry.OriginalEntity != null && !(entry.Entity.GetType() == entry.OriginalEntity.GetType()))
                {
                    throw new InvalidOperationException(
                        string.Format(CultureInfo.CurrentCulture, Resource.InvalidChangeSet, Resource.InvalidChangeSet_MustBeSameType));
                }

                if (entry.Operation == DomainOperation.Insert && entry.OriginalEntity != null)
                {
                    throw new InvalidOperationException(
                        string.Format(CultureInfo.CurrentCulture, Resource.InvalidChangeSet, Resource.InvalidChangeSet_InsertsCantHaveOriginal));
                }
            }

            // now that we have the full Id space, we can validate associations
            foreach (ChangeSetEntry entry in changeSetEntries)
            {
                if (entry.Associations != null)
                {
                    ValidateAssociationMap(entry.Entity.GetType(), idSet, entry.Associations);
                }

                if (entry.OriginalAssociations != null)
                {
                    ValidateAssociationMap(entry.Entity.GetType(), idSet, entry.OriginalAssociations);
                }
            }
        }

        /// <summary>
        /// Validates the specified association map.
        /// </summary>
        /// <param name="entityType">The entity type the association is on.</param>
        /// <param name="idSet">The set of all unique Ids in the changeset.</param>
        /// <param name="associationMap">The association map to validate.</param>
        private static void ValidateAssociationMap(Type entityType, HashSet<int> idSet, IDictionary<string, int[]> associationMap)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(entityType);

            foreach (var associationItem in associationMap)
            {
                // ensure that the member is an association member
                string associationMemberName = associationItem.Key;
                PropertyDescriptor associationMember = properties[associationMemberName];
                if (associationMember == null || associationMember.Attributes[typeof(AssociationAttribute)] == null)
                {
                    throw new InvalidOperationException(
                        string.Format(CultureInfo.CurrentCulture, Resource.InvalidChangeSet,
                        string.Format(CultureInfo.CurrentCulture, Resource.InvalidChangeSet_InvalidAssociationMember,
                        entityType, associationMemberName)));
                }
                // ensure that the id collection is not null
                if (associationItem.Value == null)
                {
                    throw new InvalidOperationException(
                        string.Format(CultureInfo.CurrentCulture, Resource.InvalidChangeSet,
                        string.Format(CultureInfo.CurrentCulture, Resource.InvalidChangeSet_AssociatedIdsCannotBeNull,
                        entityType, associationMemberName)));
                }
                // ensure that each Id specified is in the changeset
                foreach (int id in associationItem.Value)
                {
                    if (!idSet.Contains(id))
                    {
                        throw new InvalidOperationException(
                            string.Format(CultureInfo.CurrentCulture, Resource.InvalidChangeSet,
                            string.Format(CultureInfo.CurrentCulture, Resource.InvalidChangeSet_AssociatedIdNotInChangeset,
                            id, entityType, associationMemberName)));
                    }
                }
            }
        }

        /// <summary>
        /// Gets the set of <see cref="ChangeSetEntry"/> items this <see cref="ChangeSet"/> represents.
        /// </summary>
        public ReadOnlyCollection<ChangeSetEntry> ChangeSetEntries
        {
            get
            {
                return this._changeSetEntries.ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Gets a value indicating whether any of the <see cref="ChangeSetEntry"/> items has an error.
        /// </summary>
        public bool HasError
        {
            get
            {
                return this._changeSetEntries.Any(op => op.HasConflict || (op.ValidationErrors != null && op.ValidationErrors.Any()));
            }
        }

        /// <summary>
        /// A dictionary containing entities (key) and associated replace entities (value) as registered by
        /// calling <see cref="ChangeSet.Replace"/>.
        /// </summary>
        internal Dictionary<object, object> EntitiesToReplace
        {
            get
            {
                if (this._entitiesToReplace == null)
                {
                    this._entitiesToReplace = new Dictionary<object, object>();
                }

                return this._entitiesToReplace;
            }
        }

        /// <summary>
        /// A dictionary containing store entities (key) and a collection of associated client entities.
        /// </summary>
        private Dictionary<object, List<AssociatedEntityInfo>> AssociatedStoreEntities
        {
            get
            {
                if (this._associatedStoreEntities == null)
                {
                    this._associatedStoreEntities = new Dictionary<object, List<AssociatedEntityInfo>>();
                }

                return this._associatedStoreEntities;
            }
        }

        /// <summary>
        /// Replaces <paramref name="clientEntity"/> with <paramref name="returnedEntity"/> in all 
        /// <see cref="ChangeSetEntry"/>s contained in the <see cref="ChangeSet"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="clientEntity">The client modified entity.</param>
        /// <param name="returnedEntity">The server modified entity.</param>
        /// <exception cref="ArgumentNullException">if <paramref name="clientEntity"/> or 
        /// <paramref name="returnedEntity"/> is null.</exception>
        /// <exception cref="ArgumentException">if <paramref name="clientEntity"/> is not found in 
        /// the <see cref="ChangeSet"/>'s <see cref="ChangeSetEntry"/> items.</exception>
        public void Replace<TEntity>(TEntity clientEntity, TEntity returnedEntity) where TEntity : class
        {
            if (clientEntity == null)
            {
                throw new ArgumentNullException(nameof(clientEntity));
            }

            if (returnedEntity == null)
            {
                throw new ArgumentNullException(nameof(returnedEntity));
            }

            Type clientEntityType = clientEntity.GetType();
            Type returnedEntityType = returnedEntity.GetType();

            // Currently we do not allow an entity to be replaced
            // by an entity in the same inheritance chain.
            if (clientEntityType != returnedEntityType)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resource.ChangeSet_Replace_EntityTypesNotSame,
                        clientEntityType,
                        returnedEntity));
            }

            this.VerifyExistsInChangeset(clientEntity);

            this.EntitiesToReplace[clientEntity] = returnedEntity;
        }

        /// <summary>
        /// Returns the original unmodified entity for the provided <paramref name="clientEntity"/>.
        /// </summary>
        /// <remarks>
        /// Note that only members marked with <see cref="RoundtripOriginalAttribute"/> will be set
        /// in the returned instance.
        /// </remarks>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="clientEntity">The client modified entity.</param>
        /// <returns>The original unmodified entity for the provided <paramref name="clientEntity"/>.</returns>
        /// <exception cref="ArgumentNullException">if <paramref name="clientEntity"/> is null.</exception>
        /// <exception cref="ArgumentException">if <paramref name="clientEntity"/> is not in the change set.</exception>
        public TEntity GetOriginal<TEntity>(TEntity clientEntity) where TEntity : class
        {
            if (clientEntity == null)
            {
                throw new ArgumentNullException(nameof(clientEntity));
            }

            ChangeSetEntry entry = this._changeSetEntries.FirstOrDefault(p => object.ReferenceEquals(p.Entity, clientEntity));
            if (entry == null)
            {
                throw new ArgumentException(Resource.ChangeSet_ChangeSetEntryNotFound);
            }

            if (entry.Operation == DomainOperation.Insert)
            {
                throw new InvalidOperationException(Resource.ChangeSet_OriginalNotValidForInsert);
            }

            return (TEntity)entry.OriginalEntity;
        }

        /// <summary>
        /// Gets the <see cref="ChangeOperation"/> for the specified member
        /// of this changeset. If the changeset doesn't contain an operation
        /// for the object specified, 'None' is returned.
        /// </summary>
        /// <param name="entity">The entity to get the status for.</param>
        /// <returns>The <see cref="ChangeOperation"/> for the specified object.</returns>
        public ChangeOperation GetChangeOperation(object entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            // Rather than error for objects not in the changeset,
            // we need to return 'None', since this method can be
            // called for unchanged entities.
            ChangeOperation status = ChangeOperation.None;
            this._entityStatusMap.TryGetValue(entity, out status);

            return status;
        }

        /// <summary>
        /// For the compositional association indicated by <paramref name="expression"/>, this 
        /// method returns a collection of children of that association that are in this 
        /// <see cref="ChangeSet"/>. The returned collection will include all children, including
        /// any to be deleted as well as any that are unmodified.
        /// </summary>
        /// <typeparam name="TEntity">The parent Type to get associated changes for.</typeparam>
        /// <typeparam name="TResult">The Type of the association member.</typeparam>
        /// <param name="entity">The parent instance.</param>
        /// <param name="expression">Expression that refers to the compositional association member. The
        /// member must be marked with <see cref="CompositionAttribute"/>.</param>
        /// <returns>The collection of children for the association specified.</returns>
        public IEnumerable GetAssociatedChanges<TEntity, TResult>(TEntity entity, Expression<Func<TEntity, TResult>> expression)
        {
            return this.GetAssociatedChangesInternal(entity, expression, null);
        }

        /// <summary>
        /// For the compositional association indicated by <paramref name="expression"/>, this 
        /// method returns a collection of children of that association that are in this 
        /// <see cref="ChangeSet"/>. The returned collection will include all children, including
        /// any to be deleted as well as any that are unmodified.
        /// </summary>
        /// <typeparam name="TEntity">The parent Type to get associated changes for.</typeparam>
        /// <typeparam name="TResult">The Type of the association member.</typeparam>
        /// <param name="entity">The parent instance.</param>
        /// <param name="expression">Expression that refers to the compositional association member. The
        /// member must be marked with <see cref="CompositionAttribute"/>.</param>
        /// <param name="operationType">The operation type to return changes for.</param>
        /// <returns>The collection of children for the association specified.</returns>
        public IEnumerable GetAssociatedChanges<TEntity, TResult>(TEntity entity, Expression<Func<TEntity, TResult>> expression, ChangeOperation operationType)
        {
            return this.GetAssociatedChangesInternal(entity, expression, operationType);
        }

        private IEnumerable GetAssociatedChangesInternal(object entity, LambdaExpression expression, ChangeOperation? operationType)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            this.VerifyExistsInChangeset(entity);

            MemberInfo associationMember = null;
            MemberExpression mex = expression.Body as MemberExpression;
            if (mex != null)
            {
                associationMember = mex.Member;
            }
            if (associationMember == null)
            {
                throw new ArgumentException(Resource.ChangeSet_InvalidMemberExpression, nameof(expression));
            }

            // validate that the member specified is an compositional association member
            PropertyDescriptor pd = TypeDescriptor.GetProperties(entity.GetType())[associationMember.Name];
            if (pd.Attributes[typeof(AssociationAttribute)] == null)
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture, Resource.MemberNotAnAssociation, associationMember.DeclaringType, associationMember.Name), nameof(expression));
            }
            if (pd.Attributes[typeof(CompositionAttribute)] == null)
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture, Resource.MemberNotAComposition, associationMember.DeclaringType, associationMember.Name), nameof(expression));
            }

            IEnumerable<ChangeSetEntry> associatedChanges = this.GetAssociatedChanges(entity)[pd];
            if (operationType == null)
            {
                // return all results
                return associatedChanges.Select(p => p.Entity);
            }
            else
            {
                // return selected results
                return associatedChanges.Where(p => this.GetChangeOperation(p.Entity) == operationType).Select(p => p.Entity);
            }
        }

        /// <summary>
        /// Determines whether there are any changes in this changeset for
        /// composed children of the specified entity.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <returns>True if there are child changes, false otherwise.</returns>
        internal bool HasChildChanges(object entity)
        {
            return this.GetAssociatedChanges(entity).Count > 0;
        }

        /// <summary>
        /// Returns a map of composition members to associated child operations for the specified
        /// entity, caching the results.
        /// </summary>
        /// <param name="entity">The entity to get associated changes for.</param>
        /// <returns>The map of child changes.</returns>
        private Dictionary<PropertyDescriptor, IEnumerable<ChangeSetEntry>> GetAssociatedChanges(object entity)
        {
            // first check our cache to see if we've already computed the associated changes
            // for the specified entity.
            Dictionary<PropertyDescriptor, IEnumerable<ChangeSetEntry>> associatedChanges = null;
            if (this._associatedChangesMap == null)
            {
                this._associatedChangesMap = new Dictionary<object, Dictionary<PropertyDescriptor, IEnumerable<ChangeSetEntry>>>();
            }
            else if (this._associatedChangesMap.TryGetValue(entity, out associatedChanges))
            {
                return associatedChanges;
            }

            // compute the associated changes for the specified entity
            Dictionary<int, ChangeSetEntry> entityOperationMap = this._changeSetEntries.ToDictionary(p => p.Id);
            associatedChanges = new Dictionary<PropertyDescriptor, IEnumerable<ChangeSetEntry>>();
            IEnumerable<PropertyDescriptor> compositionMembers =
                TypeDescriptor.GetProperties(entity.GetType()).Cast<PropertyDescriptor>()
                .Where(p => p.Attributes[typeof(CompositionAttribute)] != null);
            foreach (PropertyDescriptor compositionMember in compositionMembers)
            {
                // first get any current child operations
                List<ChangeSetEntry> associatedChangesList = new List<ChangeSetEntry>();
                ChangeSetEntry changeSetEntry = this._changeSetEntries.Single(p => p.Entity == entity);

                if (changeSetEntry.Associations != null
                    && changeSetEntry.Associations.TryGetValue(compositionMember.Name, out int[] associatedIds))
                {
                    IEnumerable<ChangeSetEntry> childOperations = associatedIds.Select(p => entityOperationMap[p]);
                    associatedChangesList.AddRange(childOperations);
                }

                // next get any child delete operations
                if (changeSetEntry.OriginalAssociations != null
                    && changeSetEntry.OriginalAssociations.TryGetValue(compositionMember.Name, out int[] originalAssociatedIds))
                {
                    IEnumerable<ChangeSetEntry> deletedChildOperations = originalAssociatedIds
                        .Select(p => entityOperationMap[p])
                        .Where(p => p.Operation == DomainOperation.Delete);
                    associatedChangesList.AddRange(deletedChildOperations);
                }

                associatedChanges[compositionMember] = associatedChangesList;
            }

            this._associatedChangesMap[entity] = associatedChanges;

            return associatedChanges;
        }

        /// <summary>
        /// Reorders the specified changeset operations to respect compositional hierarchy ordering
        /// rules. For compositional hierarchies, all parent operations are ordered before operations
        /// on their children, recursively.
        /// </summary>
        /// <param name="changeSetEntries">The changeset operations to order.</param>
        /// <returns>The ordered operations.</returns>
        private static IEnumerable<ChangeSetEntry> OrderChangeset(IEnumerable<ChangeSetEntry> changeSetEntries)
        {
            Dictionary<int, ChangeSetEntry> cudOpIdMap = changeSetEntries.ToDictionary(p => p.Id);
            Dictionary<ChangeSetEntry, List<ChangeSetEntry>> operationChildMap = new Dictionary<ChangeSetEntry, List<ChangeSetEntry>>();

            // we group by entity type so we can cache per Type computations and lookups
            bool hasComposition = false;
            foreach (var group in changeSetEntries.GroupBy(p => p.Entity.GetType()))
            {
                IEnumerable<PropertyDescriptor> compositionMembers = TypeDescriptor.GetProperties(group.Key).Cast<PropertyDescriptor>().Where(p => p.Attributes[typeof(CompositionAttribute)] != null).ToArray();

                // foreach operation in the changeset, identify all child operations
                // and add them to a map of operation to child operations
                foreach (ChangeSetEntry operation in group)
                {
                    foreach (PropertyDescriptor compositionMember in compositionMembers)
                    {
                        hasComposition = true;

                        // add any current associations
                        List<int> childIds = new List<int>();
                        if (operation.Associations != null 
                            && operation.Associations.TryGetValue(compositionMember.Name, out int[] associatedIds))
                        {
                            childIds.AddRange(associatedIds);
                        }

                        // add any original associations
                        if (operation.OriginalAssociations != null
                            && operation.OriginalAssociations.TryGetValue(compositionMember.Name, out int[] associatedIds))
                        {
                            childIds.AddRange(associatedIds);
                        }

                        // foreach identified child operation, set the parent
                        // and build the composition maps
                        foreach (int id in childIds.Distinct())
                        {
                            // find the operation corresponding to the child entry
                            ChangeSetEntry childOperation = null;
                            if (!cudOpIdMap.TryGetValue(id, out childOperation))
                            {
                                continue;
                            }

                            // set the parent of this operation
                            if (childOperation.ParentOperation != null)
                            {
                                // an child operation can only have a single parent
                                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.ChangeSet_ChildHasMultipleParents, childOperation.Id));
                            }
                            childOperation.ParentOperation = operation;

                            // add the current operation to the list of child operations
                            // for the current operation
                            List<ChangeSetEntry> currChildOps;
                            if (!operationChildMap.TryGetValue(operation, out currChildOps))
                            {
                                currChildOps = new List<ChangeSetEntry>();
                                operationChildMap[operation] = currChildOps;
                            }
                            currChildOps.Add(childOperation);
                        }
                    }
                }
            }

            if (!hasComposition)
            {
                // there were no compositions, so leave the original changeset
                // as is
                return changeSetEntries;
            }

            // For each "root" operation with no child operations, recursively add
            // all child operations in a recursive preorder traversal from each root
            List<ChangeSetEntry> orderedOperations = new List<ChangeSetEntry>();
            IEnumerable<ChangeSetEntry> rootOperations = changeSetEntries.Where(p => p.ParentOperation == null);
            foreach (ChangeSetEntry operation in rootOperations)
            {
                OrderOperations(operation, operationChildMap, orderedOperations);
            }

            // now add any remaining operations
            return orderedOperations.Union(changeSetEntries).ToArray();
        }

        /// <summary>
        /// Recursively orders the specified operation and all child operations, adding them to the
        /// <paramref name="orderedOperations"/> list.
        /// </summary>
        /// <param name="operation">The operation to order.</param>
        /// <param name="operationChildMap">Map of operation to child operations.</param>
        /// <param name="orderedOperations">The list of ordered operations.</param>
        private static void OrderOperations(ChangeSetEntry operation, Dictionary<ChangeSetEntry, List<ChangeSetEntry>> operationChildMap, List<ChangeSetEntry> orderedOperations)
        {
            // first add the operation
            orderedOperations.Add(operation);

            // recursively add all its children
            List<ChangeSetEntry> childOps;
            if (!operationChildMap.TryGetValue(operation, out childOps))
            {
                return;
            }
            foreach (ChangeSetEntry childOperation in childOps)
            {
                OrderOperations(childOperation, operationChildMap, orderedOperations);
            }
        }

        private void VerifyExistsInChangeset(object entity)
        {
            ChangeSetEntry entry = this._changeSetEntries.FirstOrDefault(p => object.ReferenceEquals(entity, p.Entity));
            if (entry == null)
            {
                throw new ArgumentException(Resource.ChangeSet_ChangeSetEntryNotFound, nameof(entity));
            }
        }

        /// <summary>
        /// Associates a given entity with a store entity.  This method is intended for use by scenarios where a
        /// client entity may represent a projection of one or multiple data store entities.
        /// </summary>
        /// <typeparam name="TEntity">The client entity type.</typeparam>
        /// <typeparam name="TStoreEntity">The data store entity type.</typeparam>
        /// <param name="clientEntity">The client entity.</param>
        /// <param name="storeEntity">The data store entity.</param>
        /// <param name="storeToClientTransform">The entity transform. This delegate will be invoked after the <see cref="ChangeSet"/> has
        /// been successfully submitted and is intended to flow <paramref name="storeEntity"/> values back to the
        /// <paramref name="clientEntity"/>.</param>
        /// <exception cref="ArgumentNullException">if <paramref name="clientEntity"/>, <paramref name="storeEntity"/> or 
        /// <paramref name="storeToClientTransform"/> is null.</exception>
        public void Associate<TEntity, TStoreEntity>(TEntity clientEntity, TStoreEntity storeEntity, Action<TEntity, TStoreEntity> storeToClientTransform)
            where TEntity : class
            where TStoreEntity : class
        {
            if (clientEntity == null)
            {
                throw new ArgumentNullException(nameof(clientEntity));
            }

            if (storeEntity == null)
            {
                throw new ArgumentNullException(nameof(storeEntity));
            }

            if (storeToClientTransform == null)
            {
                throw new ArgumentNullException(nameof(storeToClientTransform));
            }

            // Verify the provided client entity exists in our changeset
            this.VerifyExistsInChangeset(clientEntity);

            Action transformAction = () => storeToClientTransform(clientEntity, storeEntity);
            AssociatedEntityInfo associatedModel = new AssociatedEntityInfo(clientEntity, transformAction);

            List<AssociatedEntityInfo> associatedModels = null;
            if (!this.AssociatedStoreEntities.TryGetValue(storeEntity, out associatedModels))
            {
                associatedModels = new List<AssociatedEntityInfo>();
                this.AssociatedStoreEntities[storeEntity] = associatedModels;
            }

            associatedModels.Add(associatedModel);
        }

        /// <summary>
        /// Returns a collection of entities of the given type associated 
        /// with a given data store entity.
        /// </summary>
        /// <typeparam name="TEntity">The client entity type.</typeparam>
        /// <typeparam name="TStoreEntity">The data store entity type.</typeparam>
        /// <param name="storeEntity">The data store entity.</param>
        /// <returns>Returns a collection of associated entities.</returns>
        /// <exception cref="ArgumentNullException">if <paramref name="storeEntity"/> is null.</exception>
        public IEnumerable<TEntity> GetAssociatedEntities<TEntity, TStoreEntity>(TStoreEntity storeEntity)
            where TEntity : class
        {
            if (storeEntity == null)
            {
                throw new ArgumentNullException(nameof(storeEntity));
            }

            List<AssociatedEntityInfo> associatedModels = null;
            if (this.AssociatedStoreEntities.TryGetValue(storeEntity, out associatedModels))
            {
                return associatedModels.Select(ai => ai.ClientEntity).OfType<TEntity>();
            }

            return Enumerable.Empty<TEntity>();
        }

        /// <summary>
        /// Applies all entity transformation actions registered using the <see cref="Associate"/> method.
        /// </summary>
        internal void ApplyAssociatedStoreEntityTransforms()
        {
            foreach (var kvp in this.AssociatedStoreEntities)
            {
                kvp.Value.ForEach(m => m.ApplyTransform());
            }
        }

        /// <summary>
        /// Updates the current changeset entities with the corresponding replacement entities.
        /// </summary>
        /// <seealso cref="ChangeSet.Replace"/>
        internal void CommitReplacedEntities()
        {
            if (this.EntitiesToReplace.Count > 0)
            {
                foreach (ChangeSetEntry operation in this.ChangeSetEntries.Where(eo => !eo.HasError))
                {
                    object associatedEntity;
                    if (this.EntitiesToReplace.TryGetValue(operation.Entity, out associatedEntity))
                    {
                        operation.Entity = associatedEntity;
                    }
                }
            }
        }

        #region Nested Types

        /// <summary>
        /// Tuple used to represent a client entity and its associated transformation delegate.
        /// </summary>
        private class AssociatedEntityInfo
        {
            private readonly object _clientEntity;
            private readonly Action _entityTransform;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="clientEntity">The model entity.</param>
            /// <param name="entityTransform">The entity transform.</param>
            public AssociatedEntityInfo(object clientEntity, Action entityTransform)
            {
                if (clientEntity == null)
                {
                    throw new ArgumentNullException(nameof(clientEntity));
                }

                if (entityTransform == null)
                {
                    throw new ArgumentNullException(nameof(entityTransform));
                }

                this._clientEntity = clientEntity;
                this._entityTransform = entityTransform;
            }

            /// <summary>
            /// Gets the client entity.
            /// </summary>
            public object ClientEntity
            {
                get
                {
                    return this._clientEntity;
                }
            }

            /// <summary>
            /// Invokes the entity transform delegate and unwraps any 
            /// <see cref="TargetInvocationException"/> errors encountered.
            /// </summary>
            public void ApplyTransform()
            {
                try
                {
                    this._entityTransform();
                }
                catch (TargetInvocationException tie)
                {
                    if (tie.InnerException != null)
                    {
                        throw tie.InnerException;
                    }

                    throw;
                }
            }
        }

        #endregion // Nested Types
    }
}
