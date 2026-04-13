using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using OpenRiaServices.Client.Internal;

#nullable enable

namespace OpenRiaServices.Client
{
    /// <summary>
    /// Represents a collection of associated Entities.
    /// </summary>
    /// <typeparam name="TEntity">The type of <see cref="Entity"/> in the collection</typeparam>
    public sealed class EntityCollection<TEntity> : IEntityCollection, IEntityCollection<TEntity>, IList, IReadOnlyList<TEntity>, IList<TEntity>
#if HAS_COLLECTIONVIEW
        , ICollectionViewFactory
#endif
        where TEntity : Entity
    {
        private readonly Action<TEntity>? _attachAction;
        private readonly Action<TEntity>? _detachAction;
        private readonly Entity _parent;
        private readonly MetaMember _metaMember;
        private EntitySet? _sourceSet;
        private readonly Func<TEntity, bool> _entityPredicate;
        private List<TEntity>? _entities;
        private HashSet<TEntity>? _entitiesHashSet;
        private NotifyCollectionChangedEventHandler? _collectionChangedEventHandler;
        private PropertyChangedEventHandler? _propertyChangedEventHandler;
        private TEntity? _attachingEntity;
        private TEntity? _detachingEntity;
        private bool _entitiesLoaded;
        private bool _entitiesAdded;

        /// <summary>
        /// NOTE: This list is only used when Adding or Removing items through the IList interface
        /// Entities removed from an EntityCollection aren't typically removed from the source EntitySet.
        /// However, we need to track entities added through the view and manually remove them from the
        /// source EntitySet to achieve correct AddNew/CancelNew behavior.
        /// </summary>
        private List<TEntity>? _addedEntities;

        private EntityAssociationAttribute AssocAttribute => _metaMember.AssociationAttribute;
        private bool IsComposition => _metaMember.IsComposition;

        /// <summary>
        /// Initializes a new instance of the EntityCollection class
        /// </summary>
        /// <param name="parent">The entity that this collection is a member of</param>
        /// <param name="memberName">The name of this EntityCollection member on the parent entity</param>
        /// <param name="entityPredicate">The function used to filter the associated entities, determining
        /// which are members of this collection.</param>
        public EntityCollection(Entity parent, string memberName, Func<TEntity, bool> entityPredicate)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }
            if (string.IsNullOrEmpty(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }
            if (entityPredicate == null)
            {
                throw new ArgumentNullException(nameof(entityPredicate));
            }

            this._parent = parent;
            this._entityPredicate = entityPredicate;
            this._metaMember = this._parent.MetaType[memberName];

            if (this._metaMember == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resource.Property_Does_Not_Exist, parent.GetType(), memberName), nameof(memberName));
            }
            if (!this._metaMember.IsAssociationMember)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resource.MemberMustBeAssociation, memberName), nameof(memberName));
            }

            // register our callback so we'll be notified whenever the
            // parent entity is added or removed from an EntitySet
            this._parent.RegisterSetChangedCallback(this.OnEntitySetChanged);

            this._parent.PropertyChanged += this.ParentEntityPropertyChanged;
        }

        /// <summary>
        /// Initializes a new instance of the EntityCollection class
        /// </summary>
        /// <param name="parent">The entity that this collection is a member of</param>
        /// <param name="memberName">The name of this EntityCollection member on the parent entity</param>
        /// <param name="entityPredicate">The function used to filter the associated entities, determining
        /// which are members of this collection.</param>
        /// <param name="attachAction">The function used to establish a back reference from an associated entity
        /// to the parent entity.</param>
        /// <param name="detachAction">The function used to remove the back reference from an associated entity
        /// to the parent entity.</param>
        public EntityCollection(Entity parent, string memberName, Func<TEntity, bool> entityPredicate, Action<TEntity> attachAction, Action<TEntity> detachAction)
            : this(parent, memberName, entityPredicate)
        {
            if (attachAction == null)
            {
                throw new ArgumentNullException(nameof(attachAction));
            }
            if (detachAction == null)
            {
                throw new ArgumentNullException(nameof(detachAction));
            }

            this._attachAction = attachAction;
            this._detachAction = detachAction;
        }

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator for this collection
        /// </summary>
        /// <returns>An enumerator for this collection</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Event raised whenever an <see cref="Entity"/> is added to this collection
        /// </summary>
        public event EventHandler<EntityCollectionChangedEventArgs<TEntity>>? EntityAdded;

        /// <summary>
        /// Event raised whenever an <see cref="Entity"/> is removed from this collection
        /// </summary>
        public event EventHandler<EntityCollectionChangedEventArgs<TEntity>>? EntityRemoved;

        /// <summary>
        /// Gets the internal list of entities, creating it if it is null.
        /// </summary>
        private List<TEntity> Entities
        {
            get
            {
                if (this._entities == null)
                {
                    this._entities = new List<TEntity>();
                }
                return this._entities;
            }
        }

        /// <summary>
        /// Gets the internal <see cref="HashSet{T}"/> of entities, creating it if it is null.
        /// </summary>
        /// <remarks>
        /// This property has been created because of performance reasons. Invoking Contains method on <see cref="Entities"/> can take significant amount of time
        /// if there are large number of entities.
        /// </remarks>
        private HashSet<TEntity> EntitiesHashSet
        {
            get
            {
                if (this._entitiesHashSet == null)
                {
                    this._entitiesHashSet = new HashSet<TEntity>();
                }
                return this._entitiesHashSet;
            }
        }

        /// <summary>
        /// Gets the current count of entities in this collection
        /// </summary>
        public int Count
        {
            get
            {
                this.Load();
                return this.Entities.Count;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the EntityCollection source is external.
        /// </summary>
        private bool IsSourceExternal
        {
            get
            {
                return this.SourceSet != null && this.SourceSet.EntityContainer != this._parent.EntitySet.EntityContainer;
            }
        }

        private EntitySet? SourceSet
        {
            get
            {
                if (this._parent.EntitySet != null)
                {
                    this._sourceSet = this._parent.EntitySet.EntityContainer.GetEntitySet(typeof(TEntity));
                }
                return this._sourceSet;
            }
        }

        /// <summary>
        /// Add the specified entity to this collection. If the entity is unattached, it
        /// will be added to its <see cref="EntitySet"/> automatically.
        /// </summary>
        /// <param name="entity">The entity to add</param>
        public void Add(TEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (this.IsSourceExternal)
            {
                // Modifications are not allowed when the entity set source is external.
                throw new InvalidOperationException(Resource.EntityCollection_ModificationNotAllowedForExternalReference);
            }

            if (entity == this._attachingEntity)
            {
                return;
            }

            if (this.SourceSet != null)
            {
                this.SourceSet.EntityContainer.CheckCrossContainer(entity);
            }

            this.Attach(entity);

            if (!this.EntitiesHashSet.Contains(entity))
            {
                bool addedToSet = false;
                if (this.SourceSet != null)
                {
                    if (!this.SourceSet.IsAttached(entity))
                    {
                        // if an unattached entity is added to the collection, we infer it
                        // as an Add on its EntitySet
                        entity.IsInferred = true;
                        this.SourceSet.Add(entity);
                        addedToSet = true;
                    }
                    else if (this.IsComposition && entity.EntityState == EntityState.Deleted)
                    {
                        // if a deleted entity is added to a compositional association,
                        // the delete should be undone
                        this.SourceSet.Add(entity);
                        addedToSet = true;
                    }
                }

                // we may have to check for containment once more, since the EntitySet.Add calls
                // above can cause a dynamic add to this EntityCollection behind the scenes
                if (TryAddEntityToCollection(entity) || addedToSet)
                {
                    this.RaiseCollectionChangedNotification(NotifyCollectionChangedAction.Add, entity, this.Entities.Count - 1);
                }

                this._entitiesAdded = true;
            }

            // When entities are added, we must load the collection to ensure
            // we're monitoring the source entity set from here on.
            this.Load();

            if (this.IsComposition)
            {
                entity.Parent.OnChildUpdate();
            }
        }

        /// <summary>
        /// Remove the specified entity from this collection.
        /// </summary>
        /// <param name="entity">The entity to remove</param>
        public void Remove(TEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (entity == this._detachingEntity)
            {
                return;
            }

            int idx = this.Entities.IndexOf(entity);
            if (idx == -1 && !this._entityPredicate(entity))
            {
                // If the entity is not in this collection and the FK doesn't
                // match throw
                throw new InvalidOperationException(Resource.Entity_Not_In_Collection);
            }

            if (this.IsSourceExternal)
            {
                // Modifications are not allowed when the entity set source is external.
                throw new InvalidOperationException(Resource.EntityCollection_ModificationNotAllowedForExternalReference);
            }

            this.Detach(entity);

            if (idx != -1)
            {
                if (this.RemoveEntityFromCollection(entity, idx))
                {
                    // If the entity was removed, raise a collection changed notification. Note that the Detach call above might
                    // have caused a dynamic removal behind the scenes resulting in the entity no longer being in the collection,
                    // with the event already having been raised
                    this.RaiseCollectionChangedNotification(NotifyCollectionChangedAction.Remove, entity, idx);
                }
            }

            if (this.IsComposition)
            {
                // when a composed entity is removed from its collection,
                // it's inferred as a delete
                if (this._sourceSet != null && this._sourceSet.IsAttached(entity))
                {
                    this._sourceSet.Remove(entity);
                }

                entity.Parent.OnChildUpdate();
            }
        }

        /// <summary>
        /// Returns a <see cref="String"/> that represents the <see cref="EntityCollection&lt;TEntity&gt;"/>.
        /// </summary>
        /// <returns>A <see cref="String"/> that represents the <see cref="EntityCollection&lt;TEntity&gt;"/>.</returns>
        public override string ToString()
        {
            return typeof(TEntity).Name;
        }

        /// <summary>
        /// Add the specified <paramref name="entity"/> this collection, setting its
        /// Parent if this is a compositional association. Whenever an
        /// entity is added to the underlying physical collection, it
        /// should be done through this method.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/>to add.</param>
        private bool TryAddEntityToCollection(TEntity entity)
        {
            if (this.EntitiesHashSet.Add(entity))
            {
                this.Entities.Add(entity);

                if (this.IsComposition)
                {
                    entity.SetParent(this._parent, this.AssocAttribute);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool RemoveEntityFromCollection(TEntity entity, int index)
        {
            if (this.EntitiesHashSet.Remove(entity))
            {
                Debug.Assert(object.ReferenceEquals(entity, Entities[index]));
                this.Entities.RemoveAt(index);
                return true;
            }
            Debug.Fail("Expected item to be part of Set");
            return false;
        }

        /// <summary>
        /// Remove the entity if part of the collection and returns it's index through <paramref name="index"/>.(-1 if no removal)
        /// </summary>
        /// <param name="entity">entity to remove</param>
        /// <param name="index">the index of the entity before removal, or -1 if not removed</param>
        private bool TryRemoveEntityFromCollection(TEntity entity, out int index)
        {
            if (this.EntitiesHashSet.Remove(entity))
            {
                index = this.Entities.IndexOf(entity);
                this.Entities.RemoveAt(index);
                return true;
            }
            index = -1;
            return false;
        }

        /// <summary>
        /// Calls the attach method to set the entity association reference.
        /// </summary>
        /// <param name="entity">entity to attach</param>
        private void Attach(TEntity entity)
        {
            if (this._attachAction != null)
            {
                TEntity? prev = this._attachingEntity;
                this._attachingEntity = entity;
                try
                {
                    this._attachAction(entity);
                }
                finally
                {
                    this._attachingEntity = prev;
                }
            }
        }

        /// <summary>
        /// Calls the detach method to set the entity association reference.
        /// </summary>
        /// <param name="entity">entity to detach</param>
        private void Detach(TEntity entity)
        {
            if (this._detachAction != null)
            {
                TEntity? prev = this._detachingEntity;
                this._detachingEntity = entity;
                try
                {
                    this._detachAction(entity);
                }
                finally
                {
                    this._detachingEntity = prev;
                }
            }
        }

        /// <summary>
        /// If not already loaded, this method runs our predicate against the source
        /// EntitySet
        /// </summary>
        private void Load()
        {
            if ((this._parent.EntitySet == null) || this._entitiesLoaded)
            {
                return;
            }

            // Get associated entity set and filter based on FK predicate
            EntitySet set = this._parent.EntitySet.EntityContainer.GetEntitySet(typeof(TEntity));
            foreach (TEntity entity in set.OfType<TEntity>().Where(this.Filter))
            {
                this.TryAddEntityToCollection(entity);
            }

            // once we've loaded entities, we're caching them, so we need to update
            // our cached collection any time the source EntitySet is updated
            this._entitiesLoaded = true;
            this.MonitorEntitySet();
        }

        /// <summary>
        /// When filtering entities during query execution against the source set, or during
        /// source set collection changed notifications, we don't want to include New entities, 
        /// to ensure that we don't get false positives in cases where the entity's
        /// FK members are auto-generated on the server or haven't been set yet.
        /// </summary>
        /// <param name="entity">The entity to filter</param>
        /// <returns>A <see cref="Boolean"/> value indicating whether or not the <paramref name="entity"/> should be filtered.</returns>
        private bool Filter(TEntity entity)
        {
            return entity.EntityState != EntityState.New && this._entityPredicate(entity);
        }

        /// <summary>
        /// PropertyChanged handler for the parent entity.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The property changed event arguments.</param>
        private void ParentEntityPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Reset the loaded entities as needed.
            if (this._entitiesLoaded && this.AssocAttribute.ThisKeyMembers.Contains(e.PropertyName))
            {
                // A FK member for this association has changed on the parent
                // so we need to reset the cached collection
                this.ResetLoadedEntities();
            }
        }

        #region IEnumerable<TEntity> Members

        /// <summary>
        /// Returns an enumerator for this collection
        /// </summary>
        /// <returns>An enumerator for this collection</returns>
        public IEnumerator<TEntity> GetEnumerator()
        {
            this.Load();

            // To support iterations that also remove entities
            // from this EntityCollection or the source EntitySet
            // we must return a copy, since those operations
            // will modify our entities collection.
            return this.Entities.ToList().GetEnumerator();
        }

        #endregion

        #region INotifyCollectionChanged Members

        /// <summary>
        /// Event raised whenever the contents of the collection changes
        /// </summary>
        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add
            {
                this._collectionChangedEventHandler = (NotifyCollectionChangedEventHandler?)Delegate.Combine(this._collectionChangedEventHandler, value);
            }
            remove
            {
                this._collectionChangedEventHandler = (NotifyCollectionChangedEventHandler?)Delegate.Remove(this._collectionChangedEventHandler, value);
            }
        }

        #region INotifyPropertyChanged Members
        /// <summary>
        /// Event raised whenever a property on this collection changes
        /// </summary>
        event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                this._propertyChangedEventHandler = (PropertyChangedEventHandler?)Delegate.Combine(this._propertyChangedEventHandler, value);
            }
            remove
            {
                this._propertyChangedEventHandler = (PropertyChangedEventHandler?)Delegate.Remove(this._propertyChangedEventHandler, value);
            }
        }

        #endregion

        /// <summary>
        /// Called whenever the parent entity's <see cref="EntitySet"/> membership changes,
        /// allowing us to navigate to our source set for this collection.
        /// </summary>
        private void OnEntitySetChanged()
        {
            if (this._parent.EntitySet != null && this._sourceSet == null)
            {
                // if we were detached and we're now being attached, we want to
                // force the collection to reload next time it is inspected, since
                // our EntitySet has changed
                this._entitiesLoaded = false;
            }

            this.MonitorEntitySet();
        }

        /// <summary>
        /// Based on our current load status and our parent's attach status to an <see cref="EntityContainer"/>,
        /// update our event subscription to the source set's CollectionChanged event, the goal being to monitor
        /// the source set if and only if our parent is attached and we have loaded entities (this._entitiesLoaded == true)
        /// and need to keep our cached set in sync.
        /// </summary>
        private void MonitorEntitySet()
        {
            if (this._parent.EntitySet != null)
            {
                // it's expensive to monitor the source set for changes, so we only monitor when
                // entities have been added or loaded
                if (this._entitiesAdded || this._entitiesLoaded)
                {
                    if (this._sourceSet != null)
                    {
                        // Make sure we unsubscribe from any sets we may have already subscribed to (e.g. in case 
                        // of inferred adds). If we didn't already subscribe, this will be a no-op.
                        ((INotifyCollectionChanged)this._sourceSet).CollectionChanged -= this.SourceSet_CollectionChanged;
                        this._sourceSet.RegisterAssociationCallback(this.AssocAttribute, this.OnEntityAssociationUpdated, false);
                    }

                    // subscribe to the source set CollectionChanged event
                    this._sourceSet = this._parent.EntitySet.EntityContainer.GetEntitySet(typeof(TEntity));
                    ((INotifyCollectionChanged)this._sourceSet).CollectionChanged += this.SourceSet_CollectionChanged;
                    this._sourceSet.RegisterAssociationCallback(this.AssocAttribute, this.OnEntityAssociationUpdated, true);
                }
            }
            else if (this._parent.EntitySet == null && this._sourceSet != null)
            {
                // If the parent entity has been detached and we were monitoring,
                // we need to remove our event handler
                ((INotifyCollectionChanged)this._sourceSet).CollectionChanged -= this.SourceSet_CollectionChanged;
                this._sourceSet.RegisterAssociationCallback(this.AssocAttribute, this.OnEntityAssociationUpdated, false);
                this._sourceSet = null;
            }
        }

        /// <summary>
        /// Callback for when an entity in the source set changes such that we need to reevaluate
        /// it's membership in our collection. This could be because an FK member for the association
        /// has changed, or when the entity state transitions to Unmodified.
        /// </summary>
        /// <param name="entity">The entity that has changed</param>
        private void OnEntityAssociationUpdated(Entity entity)
        {
            if ((entity == this._attachingEntity) || (entity == this._detachingEntity))
            {
                // avoid reentrancy issues in cases where the
                // entity is currently being processed by an Add/Remove
                // on this collection.
                return;
            }

            if (entity.EntityState == EntityState.New && entity.IsMergingState)
            {
                // We don't want to perform dynamic updates when merging store state
                // into new entities.
                return;
            }

            TEntity? typedEntity = entity as TEntity;
            if (typedEntity != null && this._entitiesLoaded)
            {
                bool containsEntity = this.EntitiesHashSet.Contains(typedEntity);

                // We allow the parent entity to be New during the AcceptChanges phase of a submit (AcceptChanges called on the other entity)
                // of a successfull Submit operation, in which case we know that it will soon be unmodified.
                // Without this exception we will fail to raise property changed for the member property if the other entities changes are accepted first
                if (!containsEntity
                    && (this._parent.EntityState != EntityState.New || (this._parent.IsSubmitting && entity.IsSubmitting && entity.EntityState == EntityState.Unmodified))
                    && this.Filter(typedEntity))
                {
                    // Add matching entity to our set. When adding, we use the stronger Filter to
                    // filter out New entities
                    if (this.TryAddEntityToCollection(typedEntity))
                        this.RaiseCollectionChangedNotification(NotifyCollectionChangedAction.Add, typedEntity, this.Entities.Count - 1);
                }
                // The entity is in our set but is no longer a match, so we need to remove it.
                // Here we use the predicate directly, since even if the entity is New if it
                // no longer matches it should be removed.
                else if (!this._entityPredicate(typedEntity) && this.TryRemoveEntityFromCollection(typedEntity, out int idx))
                {
                    this.RaiseCollectionChangedNotification(NotifyCollectionChangedAction.Remove, typedEntity, idx);
                }
            }
        }

        /// <summary>
        /// Whenever the source set changes, we need to run our predicate against the
        /// added/removed entities and if we get any matches we propagate the event and
        /// merge the modifications into our cached set if we are in a loaded state.
        /// </summary>
        /// <param name="sender">The caller who raised the collection changed event.</param>
        /// <param name="args">The collection changed event arguments.</param>
        private void SourceSet_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
        {
            if (this._parent.EntityState != EntityState.New &&
                args.Action == NotifyCollectionChangedAction.Add
                && args.NewItems is not null)
            {
                List<TEntity> newEntities = args.NewItems.OfType<TEntity>().Where(this.Filter).ToList();
                if (newEntities.Count > 0)
                {
                    int newStartingIdx = this.Entities.Count;
                    List<object> affectedEntities = new List<object>();
                    foreach (TEntity newEntity in newEntities)
                    {
                        if (this.TryAddEntityToCollection(newEntity))
                        {
                            affectedEntities.Add(newEntity);
                        }
                    }

                    if (affectedEntities.Count > 0)
                    {
                        this.RaiseCollectionChangedNotification(args.Action, affectedEntities, newStartingIdx);
                    }
                }
            }
            else if (args.Action == NotifyCollectionChangedAction.Remove
                && args.OldItems is not null)
            {
                // if the entity is in our cached collection, remove it
                foreach (TEntity entityToRemove in args.OldItems.OfType<TEntity>())
                {
                    // If entity was part of the collection and removed, raise an event
                    if (this.TryRemoveEntityFromCollection(entityToRemove, out int idx))
                    {
                        // Should we do a single reset event if multiple entitites are removed ??
                        this.RaiseCollectionChangedNotification(args.Action, entityToRemove, idx);
                    }
                }
            }
            else if (args.Action == NotifyCollectionChangedAction.Reset)
            {
                if (this._entitiesLoaded)
                {
                    this.ResetLoadedEntities();
                }
            }
        }

        private void RaiseCollectionChangedNotification(NotifyCollectionChangedAction action, TEntity entity, int startingIndex)
        {
            // Reset notifications are handled elsewhere for the EntityRemoved event.
            switch (action)
            {
                case NotifyCollectionChangedAction.Add:
                    this.EntityAdded?.Invoke(this, new EntityCollectionChangedEventArgs<TEntity>(entity));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    this.EntityRemoved?.Invoke(this, new EntityCollectionChangedEventArgs<TEntity>(entity));
                    break;
            }

            this._collectionChangedEventHandler?.Invoke(this, new NotifyCollectionChangedEventArgs(action, entity, startingIndex));
            this._propertyChangedEventHandler?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        }

        private void RaiseCollectionChangedNotification(NotifyCollectionChangedAction action, IList entities, int startingIndex)
        {
            // Reset notifications are handled elsewhere for the EntityRemoved event.
            switch (action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (this.EntityAdded != null)
                    {
                        foreach (TEntity entity in entities)
                            this.EntityAdded.Invoke(this, new EntityCollectionChangedEventArgs<TEntity>(entity));
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (this.EntityRemoved != null)
                    {
                        foreach (TEntity entity in entities)
                            this.EntityRemoved.Invoke(this, new EntityCollectionChangedEventArgs<TEntity>(entity));
                    }
                    break;
            }

            this._collectionChangedEventHandler?.Invoke(this, new NotifyCollectionChangedEventArgs(action, entities, startingIndex));
            this._propertyChangedEventHandler?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        }

        /// <summary>
        /// Removes all non-New entities from the loaded entities collection and raises 
        /// any required EntityRemoved events.
        /// </summary>
        private void ResetLoadedEntities()
        {
            IEnumerable<TEntity> loadedEntities = this.Entities;
            this._entities = this.Entities.Where(p => p.EntityState == EntityState.New).ToList();
            this._entitiesHashSet = new HashSet<TEntity>(this._entities);
            this._entitiesLoaded = false;

            if (this.EntityRemoved != null)
            {
                // for each removed entity, we need to raise a notification
                foreach (TEntity entity in loadedEntities.Where(p => !this._entitiesHashSet.Contains(p)))
                {
                    this.EntityRemoved(this, new EntityCollectionChangedEventArgs<TEntity>(entity));
                }
            }

            this.RaiseCollectionChangedNotification(NotifyCollectionChangedAction.Reset, (IList)null!, -1);
        }
        #endregion

        #region IEntityCollection Members
        EntityAssociationAttribute IEntityCollection.Association
        {
            get
            {
                return this.AssocAttribute;
            }
        }

        bool IEntityCollection.HasValues
        {
            get
            {
                return this._entities != null && this._entities.Count > 0;
            }
        }

        IEnumerable<Entity> IEntityCollection.Entities
        {
            get
            {
                return this;
            }
        }

        void IEntityCollection.Add(Entity entity)
        {
            this.Add((TEntity)entity);
        }

        void IEntityCollection.Remove(Entity entity)
        {
            this.Remove((TEntity)entity);
        }
        #endregion

        #region ICollectionViewFactory
#if HAS_COLLECTIONVIEW
        /// <summary>
        /// Returns a custom view for specialized sorting, filtering, grouping, and currency.
        /// </summary>
        /// <returns>A custom view for specialized sorting, filtering, grouping, and currency</returns>
        ICollectionView ICollectionViewFactory.CreateView()
        {
            return new NonLeakingListCollectionView(this);
        }
#endif
        #endregion

        #region ICollection<TEntity>, IReadOnlyList<TEntity> Members

        /// <summary>
        /// Gets the entity at the specified index in the collection.
        /// </summary>
        /// <remarks>**Important**: Make sure to check <see cref="Count"/> first to ensure the collection is initialized</remarks>
        /// <param name="index">The zero-based index of the entity to retrieve.</param>
        /// <returns>The entity located at the specified index.</returns>
        public TEntity this[int index] => Entities[index];

        bool ICollection<TEntity>.IsReadOnly
        {
            get
            {
                // Modifications are not allowed when the entity set source is external.
                return IsSourceExternal;
            }
        }

        void ICollection<TEntity>.CopyTo(TEntity[] array, int arrayIndex)
        {
            this.Load();
            this.Entities.CopyTo(array, arrayIndex);
        }

        bool ICollection<TEntity>.Contains(TEntity item)
        {
            this.Load();
            return this.EntitiesHashSet.Contains(item);
        }

        bool ICollection<TEntity>.Remove(TEntity item)
        {
            this.Load();
            if (this.EntitiesHashSet.Contains(item))
            {
                Remove(item);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes all items.
        /// </summary>
        void ICollection<TEntity>.Clear()
        {
            this.Load();
            foreach (var item in this.Entities.ToList())
                Remove(item);
        }

        #endregion

        #region IList, ICollection
        bool IList.IsFixedSize => this.IsSourceExternal;

        bool IList.IsReadOnly => this.IsSourceExternal;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => ((ICollection)Entities).SyncRoot;

        TEntity IList<TEntity>.this[int index]
        {
            get => this[index];
            set => throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, Resource.IsNotSupported, "Index setter"));
        }

        /// <inheritdoc cref="this[int]"/>
        object? IList.this[int index]
        {
            get
            {
                var list = Entities;
                if (((uint)index) < (uint)list.Count)
                {
                    return list[index];
                }
                else
                {
                    // We run into this scenario when the association reference is changed during an
                    // AddNew. The scenario is not supported, but we're trying to improve the error
                    // message. Instead of throwing an ArgumentOutOfRangeException, we'll simply return
                    // null and allow the view to inform us the added item is not at the requested index.
                    return null;
                }
            }
            set => throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, Resource.IsNotSupported, "Index setter"));
        }

        int IList.Add(object? value)
        {
            int countBefore = this.Count;
            TEntity entity = (TEntity)value!;
            Add(entity);

            if (this.Count == countBefore)
                return -1;

            _addedEntities ??= [];
            _addedEntities.Add(entity);

            if (this.Count == countBefore + 1)
                return countBefore;
            else
                return Entities.IndexOf(entity, countBefore);
        }

        void IList.Clear()
        {
            ((ICollection<TEntity>)this).Clear();
        }

        bool IList.Contains(object? value)
        {
            return value is TEntity entity && ((ICollection<TEntity>)this).Contains(entity);
        }

        int IList.IndexOf(object? value)
        {
            if (value is not TEntity entity)
                return -1;

            Load();
            return Entities.IndexOf(entity);
        }

        void IList.Insert(int index, object? value)
        {
            throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, Resource.IsNotSupported, "Insert"));
        }

        void IList.Remove(object? value)
        {
            TEntity? entity = value as TEntity;
            if (entity == null)
            {
                return;
            }

            this.Remove(entity);
            if (this._addedEntities?.Remove(entity) == true
                // In case of Composition, the entity in the SourceSet may already be removed via this.Remove above.
                && SourceSet?.Contains(entity) == true)
            {
                this.SourceSet.Remove(entity);
            }
        }

        void IList.RemoveAt(int index)
        {
            // Need to call into IList variant of remove to handle _addedEntities
            ((IList)this).Remove(this[index]);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            this.Load();
            ((ICollection)Entities).CopyTo(array, index);
        }

        int IList<TEntity>.IndexOf(TEntity item)
            => Entities.IndexOf(item);

        void IList<TEntity>.Insert(int index, TEntity item)
            => throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, Resource.IsNotSupported, "Insert"));

        void IList<TEntity>.RemoveAt(int index)
            => Remove(this[index]);
        #endregion
    }
}

