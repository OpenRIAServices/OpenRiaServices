using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using OpenRiaServices.DomainServices;
using OpenRiaServices.DomainServices.Client;

namespace OpenRiaServices.Controls
{
    /// <summary>
    /// A standard implementation of <see cref="ICollectionView"/> and <see cref="IEditableCollectionView"/> that also represents
    /// an <see cref="IEnumerable{TEntity}"/> for <see cref="Entity"/> types.
    /// </summary>
    /// <remarks>
    /// This view implementation uses delegates defined in an <see cref="EntityCollectionView.CollectionViewDelegates"/>
    /// instance to define what methods to call for functionality that must be implemented in the underlying <see cref="IEnumerable{TEntity}"/>.
    /// </remarks>
    /// <typeparam name="TEntity">The type of <see cref="Entity"/> enumerable that this view represents.</typeparam>
    internal class EntityCollectionView<TEntity> : EntityCollectionView, IEnumerable<TEntity> where TEntity : Entity
    {
        #region All Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityCollectionView{TEntity}"/> class
        /// that will wrap around the specified <paramref name="source"/>.
        /// </summary>
        /// <remarks>
        /// We default to support <see cref="EntitySetOperations.Add"/> and <see cref="EntitySetOperations.Remove"/>
        /// <see cref="EntitySetOperations"/> using the default implementations of methods such as
        /// <see cref="IList.Add"/> and <see cref="IList.Remove"/> when the <paramref name="source"/> implements
        /// <see cref="IList"/>.
        /// <para>
        /// If the <paramref name="source"/> doesn't implement <see cref="IList"/>, then
        /// the view will disable the functions that cannot be utilized.
        /// </para>
        /// <para>
        /// Because there is no default implementation if <see cref="IEditableCollectionView.EditItem"/>, we don't allow that
        /// operation by default.
        /// </para>
        /// </remarks>
        /// <param name="source">Any enumerable of a type that derives from <see cref="Entity"/>.</param>
        internal EntityCollectionView(IEnumerable<TEntity> source)
            : this(source, EntitySetOperations.Add | EntitySetOperations.Remove, new CollectionViewDelegates())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityCollectionView{TEntity}"/> class
        /// that will wrap around the specified <paramref name="source"/>.
        /// </summary>
        /// <param name="source">Any enumerable of a type that derives from <see cref="Entity"/>.</param>
        /// <param name="supportedOperations">Which <see cref="EntitySetOperations"/> to support in the view.</param>
        internal EntityCollectionView(IEnumerable<TEntity> source, EntitySetOperations supportedOperations)
            : this(source, supportedOperations, new CollectionViewDelegates())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityCollectionView{TEntity}"/> class
        /// that will wrap around the specified <paramref name="source"/>.
        /// </summary>
        /// <param name="source">Any enumerable of a type that derives from <see cref="Entity"/>.</param>
        /// <param name="supportedOperations">Which <see cref="EntitySetOperations"/> to support in the view.</param>
        /// <param name="delegates">
        /// How to call back into the <paramref name="source"/> collection for operations such as
        /// <see cref="EntityCollectionView.CollectionViewDelegates.Add"/> and
        /// <see cref="EntityCollectionView.CollectionViewDelegates.Remove"/>.
        /// </param>
        internal EntityCollectionView(IEnumerable<TEntity> source, EntitySetOperations supportedOperations, CollectionViewDelegates delegates)
            : base(source, supportedOperations, delegates)
        {
        }

        #endregion

        #region IEnumerable<T> Members

        /// <summary>
        /// Gets the enumerator for this <see cref="IEnumerable{TEntity}"/>.
        /// </summary>
        /// <returns>An enumerator for the source enumerable.</returns>
        IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator()
        {
            return this.OfType<TEntity>().GetEnumerator();
        }

        #endregion
    }

    /// <summary>
    /// A standard implementation of <see cref="ICollectionView"/> and <see cref="IEditableCollectionView"/> that also represents
    /// an <see cref="IEnumerable"/> for <see cref="Entity"/> types.
    /// </summary>
    /// <remarks>
    /// This view implementation uses delegates defined in an <see cref="EntityCollectionView.CollectionViewDelegates"/>
    /// instance to define what methods to call for functionality that must be implemented in the underlying <see cref="IEnumerable{TEntity}"/>.
    /// </remarks>
    internal class EntityCollectionView : IEnumerable, ICollectionView, IEditableCollectionView, INotifyCollectionChanged, INotifyPropertyChanged
    {
        #region Static Fields and Constants

        /// <summary>
        /// Since there's nothing in the un-cancelable event args that is mutable,
        /// just create one instance to be used universally.
        /// </summary>
        private static readonly CurrentChangingEventArgs uncancelableCurrentChangingEventArgs = new CurrentChangingEventArgs(false);

        #endregion

        #region Member Fields

        private IEnumerable _source;
        private CollectionViewDelegates _delegates;
        private CultureInfo _culture;
        private Type _entityType;

        private object _currentItem;
        private int _currentPosition = -1;
        private bool _isCurrentBeforeFirst = true;
        private bool _isCurrentAfterLast = true;

        private EntitySetOperations _supportedOperations;

        private bool _canAddNew;
        private bool _canCancelEdit;
        private bool _canRemove;
        private Entity _currentAddItem;
        private Entity _currentEditItem;
        private bool _isAddingNew;
        private bool _isEditingItem;

        /// <summary>
        /// Private accessor for the monitor we use to prevent recursion
        /// </summary>
        private SimpleMonitor _currentChangedMonitor = new SimpleMonitor();

        /// <summary>
        /// Private field to indicate if there is a pending <see cref="CurrentChanged"/> event,
        /// indicating that no further <see cref="CurrentChanging"/> events should be raiesd and
        /// that a <see cref="CurrentChanged"/> event needs to be raised.
        /// </summary>
        private bool _isCurrentChangedEventPending = false;
        private object _currentChangedEventPendingLock = new object();

        /// <summary>
        /// The number of defers that have been nested.
        /// </summary>
        private int _deferLevel;

        #endregion

        #region All Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityCollectionView"/> class
        /// that will wrap around the specified <paramref name="source"/>.
        /// </summary>
        /// <remarks>
        /// We default to support <see cref="EntitySetOperations.Add"/> and <see cref="EntitySetOperations.Remove"/>
        /// <see cref="EntitySetOperations"/> using the default implementations of methods such as
        /// <see cref="IList.Add"/> and <see cref="IList.Remove"/> when the <paramref name="source"/> implements
        /// <see cref="IList"/>.
        /// <para>
        /// If the <paramref name="source"/> doesn't implement <see cref="IList"/>, then
        /// the view will disable the functions that cannot be utilized.
        /// </para>
        /// <para>
        /// Because there is no default implementation if <see cref="EditItem"/>, we don't allow that
        /// operation by default.
        /// </para>
        /// </remarks>
        /// <param name="source">Any enumerable of a type that derives from <see cref="Entity"/>.</param>
        internal EntityCollectionView(IEnumerable source)
            : this(source, EntitySetOperations.Add | EntitySetOperations.Remove, new CollectionViewDelegates())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityCollectionView"/> class
        /// that will wrap around the specified <paramref name="source"/>.
        /// </summary>
        /// <param name="source">Any enumerable of a type that derives from <see cref="Entity"/>.</param>
        /// <param name="supportedOperations">Which <see cref="EntitySetOperations"/> to support in the view.</param>
        internal EntityCollectionView(IEnumerable source, EntitySetOperations supportedOperations)
            : this(source, supportedOperations, new CollectionViewDelegates())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityCollectionView"/> class
        /// that will wrap around the specified <paramref name="source"/>.
        /// </summary>
        /// <param name="source">Any enumerable of a type that derives from <see cref="Entity"/>.</param>
        /// <param name="supportedOperations">Which <see cref="EntitySetOperations"/> to support in the view.</param>
        /// <param name="delegates">
        /// How to call back into the <paramref name="source"/> collection for operations such as
        /// <see cref="CollectionViewDelegates.Add"/> and <see cref="CollectionViewDelegates.Remove"/>.
        /// </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors",
            Justification = "This type is never exposed publicly.")]
        internal EntityCollectionView(IEnumerable source, EntitySetOperations supportedOperations, CollectionViewDelegates delegates)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (!EntityCollectionView.IsEnumerableEntityType(source.GetType(), out this._entityType))
            {
                throw new ArgumentException("Source must implement the generic IEnumerable with a type argument deriving from Entity.");
            }

            if (delegates == null)
            {
                throw new ArgumentNullException("delegates");
            }

            this._source = source;
            this.SubscribeToCollectionChanged();

            this._delegates = delegates;
            this._supportedOperations = supportedOperations;

            this.ApplyDefaultDelegates();
            this.CalculateAllCalculatedProperties();
            this.MoveCurrentToFirst();

            this.Culture = CultureInfo.CurrentCulture;
        }

        #endregion

        #region Private/Protected/Internal Properties

        /// <summary>
        /// Gets the source collection as an <see cref="IList"/> so that we can easily test
        /// for this implementation and utilize <see cref="IList.Add"/> and <see cref="IList.Remove"/>
        /// methods if applicable.
        /// </summary>
        private IList SourceList
        {
            get
            {
                return this._source as IList;
            }
        }

        /// <summary>
        /// Gets the source collection as an <see cref="IEnumerable{TEntity}"/> of type <see cref="Entity"/>.
        /// </summary>
        private IEnumerable<Entity> SourceEntities
        {
            get { return this._source.OfType<Entity>(); }
        }

        /// <summary>
        /// Gets a value indicating whether there is a pending <see cref="CurrentChanged"/> event.
        /// </summary>
        /// <remarks>
        /// This is used to help ensure symmetry between <see cref="CurrentChanging"/> and
        /// <see cref="CurrentChanged"/> events.
        /// </remarks>
        protected bool IsCurrentChangedEventPending
        {
            get { return this._isCurrentChangedEventPending; }
        }

        /// <summary>
        /// Gets a value indicating whether <see cref="CurrentItem"/> and <see cref="CurrentPosition"/>
        /// are up-to-date with the state and content of the collection.
        /// </summary>
        private bool IsCurrentInSync
        {
            get
            {
                if (this.IsCurrentInView)
                {
                    return Object.Equals(this._delegates.GetItemAt(this.CurrentPosition), this.CurrentItem);
                }
                else
                {
                    return this.CurrentItem == null;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current item is in the view.
        /// </summary>
        private bool IsCurrentInView
        {
            get
            {
                return this._delegates.IndexOf((Entity)this.CurrentItem) >= 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether there is still an outstanding
        /// <see cref="DeferRefresh"/> in use.  If at all possible,
        /// derived classes should not call <see cref="Refresh"/> if
        /// <see cref="IsRefreshDeferred"/> is <c>true</c>.
        /// </summary>
        internal bool IsRefreshDeferred
        {
            get { return this._deferLevel > 0; }
        }

        /// <summary>
        /// Handle the disposal of a <see cref="DeferRefresh"/>, decrementing our defer level
        /// and when <c>0</c>, calling the <see cref="DeferRefreshEnded"/> method.
        /// </summary>
        private void DeferRefreshDisposed()
        {
            Debug.Assert(this._deferLevel > 0, "Unexpected negative _deferLevel");

            if (System.Threading.Interlocked.Decrement(ref this._deferLevel) == 0)
            {
                this.DeferRefreshEnded();
            }
        }

        /// <summary>
        /// Virtual method called when a <see cref="DeferRefresh"/> cycle has ended,
        /// allowing derived classes to implement a refresh differently.
        /// </summary>
        protected virtual void DeferRefreshEnded()
        {
            this.Refresh();
        }

        /// <summary>
        /// Gets the set of <see cref="CollectionViewDelegates"/> that
        /// provide implementations on the source collection.
        /// </summary>
        protected internal CollectionViewDelegates Delegates
        {
            get
            {
                return this._delegates;
            }
        }

        /// <summary>
        /// Gets or sets the set of <see cref="EntitySetOperations"/>
        /// supported by this view.
        /// </summary>
        protected EntitySetOperations SupportedOperations
        {
            get
            {
                return this._supportedOperations;
            }
            set
            {
                if (this._supportedOperations != value)
                {
                    this._supportedOperations = value;

                    // Calculate properties that are based on delegates
                    this.CalculateCanAddNew();
                    this.CalculateCanCancelEdit();
                    this.CalculateCanRemove();
                }
            }
        }

        #endregion

        #region Private/Protected Methods

        private static bool IsEnumerableEntityType(Type type, out Type entityType)
        {
            entityType = null;

            // Determine if the type implements IEnumerable<T>, getting the IEnumerable<T>
            // type if it does so that the T can be extracted.
            Type enumerableType;
            if (!typeof(IEnumerable<>).DefinitionIsAssignableFrom(type, out enumerableType))
            {
                return false;
            }

            // Extract the T from IEnumerable<T>
            Type elementType = enumerableType.GetGenericArguments()[0];

            // Ensure T : Entity
            if (typeof(Entity).IsAssignableFrom(elementType))
            {
                entityType = elementType;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Apply default delegates that were not supplied.  This allows views to be
        /// created with default implementations for methods such as
        /// <see cref="CollectionViewDelegates.Add"/> and <see cref="CollectionViewDelegates.Remove"/>.
        /// </summary>
        protected virtual void ApplyDefaultDelegates()
        {
            this._delegates.Contains = this._delegates.Contains ?? this.DefaultContainsImplementation;
            this._delegates.IndexOf = this._delegates.IndexOf ?? this.DefaultIndexOfImplementation;
            this._delegates.GetItemAt = this._delegates.GetItemAt ?? this.DefaultGetItemAtImplementation;
            this._delegates.Count = this._delegates.Count ?? this.DefaultCountImplementation;
            this._delegates.CreateInstance = this._delegates.CreateInstance ?? new Func<Entity>(() => (Entity)Activator.CreateInstance(this._entityType));

            // Arrays implement IList but they have a fixed size.  Other IList implementations can be read only
            // Only apply the default Add delegate if the IList supports it.
            if (this._delegates.Add == null && this.SourceList != null && !this.SourceList.IsFixedSize && !this.SourceList.IsReadOnly)
            {
                this._delegates.Add = value => this.SourceList.Add(value);
            }

            // Arrays implement IList but they have a fixed size.  Other IList implementations can be read only
            // Only apply the default Remove delegate if the IList supports it.
            if (this._delegates.Remove == null && this.SourceList != null && !this.SourceList.IsFixedSize && !this.SourceList.IsReadOnly)
            {
                this._delegates.Remove = value => this.SourceList.Remove(value);
            }

            // The default RemoveAt delegate will perform a lookup by index and then call the Remove delegate
            this._delegates.RemoveAt = this._delegates.RemoveAt ?? this.DefaultRemoveAtImplementation;
        }

        /// <summary>
        /// The default implementation for the <see cref="CollectionViewDelegates.Contains"/> delegate.
        /// </summary>
        /// <param name="item">The item to be tested.</param>
        /// <returns>Whether or not the source list contains the <paramref name="item"/>.</returns>
        private bool DefaultContainsImplementation(Entity item)
        {
            if (this.SourceList != null)
            {
                return this.SourceList.Contains(item);
            }

            return this.SourceEntities.Contains(item);
        }

        /// <summary>
        /// The default implementation for the <see cref="CollectionViewDelegates.IndexOf"/> delegate.
        /// </summary>
        /// <param name="item">The item to be tested.</param>
        /// <returns>
        /// The index of the <paramref name="item"/> within the source list, or <c>-1</c> if the item is not
        /// in the list.
        /// </returns>
        private int DefaultIndexOfImplementation(Entity item)
        {
            // If the source list is an IList, use its IndexOf method
            if (this.SourceList != null)
            {
                return this.SourceList.IndexOf(item);
            }

            // Enumerate the source and discover the index.  This is a potentially expensive operation
            // but when there is a more efficient implementation, a consumer can provide it as a
            // custom delegate implementation.
            int index = 0;
            foreach (Entity entity in this.SourceEntities)
            {
                if (Object.Equals(entity, item))
                {
                    return index;
                }

                ++index;
            }

            // The item was not found
            return -1;
        }

        /// <summary>
        /// The default implementation for the <see cref="CollectionViewDelegates.GetItemAt"/> delegate.
        /// </summary>
        /// <param name="index">The index of the entity to retrieve.</param>
        /// <returns>
        /// The <see cref="Entity"/> at the specified index, or <c>null</c> if the index is out of bounds.
        /// </returns>
        private Entity DefaultGetItemAtImplementation(int index)
        {
            return this.SourceEntities.ElementAtOrDefault(index);
        }

        /// <summary>
        /// The default implementation for the <see cref="CollectionViewDelegates.Count"/> delegate.
        /// </summary>
        /// <returns>The count of items in the source list.</returns>
        private int DefaultCountImplementation()
        {
            // If the source list is an IList, use its Count property.
            if (this.SourceList != null)
            {
                return this.SourceList.Count;
            }

            // Otherwise use the (potentially inefficient) count extension for the enumerable.
            // This can be replaced with a more efficient implementation with a custom delegate.
            return this.SourceEntities.Count();
        }

        /// <summary>
        /// The default implementation for the <see cref="CollectionViewDelegates.RemoveAt"/> delegate.
        /// </summary>
        /// <param name="index">The index of the item to be removed.</param>
        private void DefaultRemoveAtImplementation(int index)
        {
            // If the source list is an IList, use its RemoveAt method.
            if (this.SourceList != null)
            {
                this.SourceList.RemoveAt(index);
            }
            else
            {
                // Ensure the index is valid
                if (index < 0 || index >= this._delegates.Count())
                {
                    throw new ArgumentOutOfRangeException("index", EntityCollectionViewResources.IndexOutOfRange);
                }

                // And remove the item, finding it by index
                // This is potentially inefficient, but a more efficient implementation can be
                // provided as a custom delegate.
                this.Remove(this._delegates.GetItemAt(index));
            }
        }

        /// <summary>
        /// Calculates all of the properties that are calculated through CalculateX methods.
        /// </summary>
        protected virtual void CalculateAllCalculatedProperties()
        {
            this.CalculateIsAddingNew();
            this.CalculateIsEditingItem();
            this.CalculateCanAddNew();
            this.CalculateCanCancelEdit();
            this.CalculateCanRemove();
        }

        /// <summary>
        /// Calculates <see cref="CanAddNew"/> by determining if an <see cref="CollectionViewDelegates.Add"/> delegate
        /// exists and the <see cref="EntitySetOperations.Add"/> operation is supported.
        /// </summary>
        protected virtual void CalculateCanAddNew()
        {
            this.CanAddNew = this._delegates.Add != null && this.IsOperationSupported(EntitySetOperations.Add);
        }

        /// <summary>
        /// Calculates <see cref="CanCancelEdit"/> by determining if the <see cref="EntitySetOperations.Edit"/>
        /// operation is supported.
        /// </summary>
        private void CalculateCanCancelEdit()
        {
            this.CanCancelEdit = this.IsOperationSupported(EntitySetOperations.Edit) && this.IsEditingItem;
        }

        /// <summary>
        /// Calculates <see cref="CanRemove"/> from the <see cref="IsAddingNew"/>, <see cref="IsEditingItem"/>, and supported operations.
        /// </summary>
        private void CalculateCanRemove()
        {
            this.CanRemove = !this.IsAddingNew && !this.IsEditingItem && this._delegates.Remove != null && this.IsOperationSupported(EntitySetOperations.Remove);
        }

        /// <summary>
        /// Calculates <see cref="IsAddingNew"/> from the <see cref="CurrentAddItem"/> property.
        /// </summary>
        private void CalculateIsAddingNew()
        {
            this.IsAddingNew = (this.CurrentAddItem != null);
        }

        /// <summary>
        /// Calculates <see cref="IsEditingItem"/> from the <see cref="CurrentEditItem"/> property.
        /// </summary>
        private void CalculateIsEditingItem()
        {
            this.IsEditingItem = (this.CurrentEditItem != null);
        }

        /// <summary>
        /// Virtual method to be called after an entity has had an edit committed.
        /// </summary>
        /// <remarks>
        /// This allows derived views to provide post-processing when an edit is committed.
        /// </remarks>
        /// <param name="editItem">The <see cref="Entity"/> that was committed.</param>
        protected virtual void CommittedEdit(Entity editItem)
        {
        }

        /// <summary>
        /// Virtual method to be called after an entity has been committed as a new entity.
        /// </summary>
        /// <remarks>
        /// This allows derived views to provide post-processing when an entity is committed as new.
        /// </remarks>
        /// <param name="newItem">The <see cref="Entity"/> that was committed.</param>
        protected virtual void CommittedNew(Entity newItem)
        {
        }

        /// <summary>
        /// Common functionality used by <see cref="CommitNew"/>, <see cref="CancelNew"/>,
        /// and when the new item is removed by <see cref="Remove"/> or <see cref="Refresh"/>.
        /// </summary>
        /// <param name="cancel">Whether we canceled the add</param>
        /// <returns>The new item we ended adding</returns>
        private Entity EndAddNew(bool cancel)
        {
            Entity newItem = (Entity)this.CurrentAddItem;
            this.CurrentAddItem = null;

            if (cancel)
            {
                ((IEditableObject)newItem).CancelEdit();
            }
            else
            {
                ((IEditableObject)newItem).EndEdit();
            }

            return newItem;
        }

        /// <summary>
        /// Ensure that our currency is valid within the view, adjusting the <see cref="CurrentPosition"/>
        /// as necessary and raising <see cref="CurrentChanging"/> and <see cref="CurrentChanged"/> events
        /// for changes that have occurred.
        /// </summary>
        protected void EnsureValidCurrency()
        {
            // We'll need to know the count a few times
            int count = this._delegates.Count();

            // Get the newly current item and position
            object newItem = this.CurrentItem;
            int newPosition = this.CurrentPosition;

            if (count <= 1)
            {
                // If we have zero or one item, then we can easily force currency
                newItem = this.SourceEntities.FirstOrDefault();
                newPosition = count - 1;
            }
            else
            {
                // Ensure that the position and item are in sync by finding
                // the current index of the current item
                newPosition = this._delegates.IndexOf((Entity)this.CurrentItem);

                if (newPosition == -1)
                {
                    newPosition = Math.Min(Math.Max(0, this.CurrentPosition), count - 1);
                    newItem = this._delegates.GetItemAt(newPosition);
                }
            }

            // If there have been changes to currency, then raise the necessary events
            if (this.CurrentItem != newItem || this.CurrentPosition != newPosition)
            {
                object oldCurrentItem = this.CurrentItem;
                int oldCurrentPosition = this.CurrentPosition;
                bool oldIsCurrentBeforeFirst = this.IsCurrentBeforeFirst;
                bool oldIsCurrentAfterLast = this.IsCurrentAfterLast;

                // Set the current item, which will set IsCurrentChangedEventPending to true
                // if a CurrentChanging event was raised
                this.SetCurrent(newItem, newPosition, count);

                // If necessary, raise the CurrentChanged event
                if (this.IsCurrentChangedEventPending)
                {
                    this.RaiseCurrentChanged(oldCurrentItem, oldCurrentPosition, oldIsCurrentBeforeFirst, oldIsCurrentAfterLast);
                }
            }
        }

        /// <summary>
        /// Gets a value that indicates whether an operation is supported by this view.
        /// </summary>
        /// <param name="operation">The <see cref="EntitySetOperations"/> value to check.</param>
        /// <returns><c>true</c> if the view supports the operations, otherwise <c>false</c>.</returns>
        protected bool IsOperationSupported(EntitySetOperations operation)
        {
            return (this._supportedOperations & operation) != 0;
        }

        /// <summary>
        /// Called when the collection has changed and a <see cref="CollectionChanged"/> event needs
        /// to be raised.
        /// </summary>
        /// <remarks>
        /// This method is virtual to allow derived views to implement additional logic into the flow
        /// of raising collection changed events.
        /// </remarks>
        /// <param name="args">The event args to use on the raised event.</param>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            NotifyCollectionChangedEventHandler handler = this.CollectionChanged;

            if (handler != null)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Handle a <see cref="CollectionChanged"/> event from the source collection.
        /// </summary>
        /// <param name="sender">The sender of the event being handled.</param>
        /// <param name="e">The event arguments of the event being handled.</param>
        private void HandleSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // If a reset, remove, or replace event is raised while we're in the middle of a transaction,
            // ensure that the collection still contains the item being edited
            if (e.Action == NotifyCollectionChangedAction.Reset ||
                e.Action == NotifyCollectionChangedAction.Remove ||
                e.Action == NotifyCollectionChangedAction.Replace)
            {
                if ((this.IsAddingNew && !this._delegates.Contains((Entity)this.CurrentAddItem)) ||
                    (this.IsEditingItem && !this._delegates.Contains((Entity)this.CurrentEditItem)))
                {
                    throw new InvalidOperationException(string.Format(
                        CultureInfo.InvariantCulture,
                        EntityCollectionViewResources.OperationNotAllowedDuringAddOrEdit,
                        "Removing"));
                }
            }

            this.SourceCollectionChanged(e);
        }

        /// <summary>
        /// Respond to a <see cref="CollectionChanged"/> event on the source collection.
        /// </summary>
        /// <remarks>
        /// This method is virtual to allow derived views to alter the logic of handling
        /// events from the source collection.
        /// </remarks>
        /// <param name="e">The event arguments of the event from the source collection.</param>
        protected virtual void SourceCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            // Relay the collection changed event
            this.OnCollectionChanged(e);

            // Update our currency if needed.  Take note that this update
            // occurs *after* relaying the collection changed event, such
            // that any controls bound to the collection will reflect the
            // updated collection before trying to update currency.
            this.EnsureValidCurrency();
        }

        /// <summary>
        /// Ask listeners (via <see cref="CurrentChanging"/> event) if it's OK to change currency.
        /// </summary>
        /// <returns><c>false</c> if a listener cancels the change, <c>true</c> otherwise.</returns>
        protected bool OkToChangeCurrent()
        {
            CurrentChangingEventArgs args = new CurrentChangingEventArgs();
            this.OnCurrentChanging(args);
            return !args.Cancel;
        }

        /// <summary>
        /// Raises the <see cref="CurrentChanging"/> event on the view.
        /// </summary>
        /// <param name="args">
        /// <see cref="CancelEventArgs"/> used by the consumer of the event.
        /// <see cref="CancelEventArgs.Cancel"/> will be <c>true</c> after this
        /// call if the <see cref="CurrentItem"/> should not be changed for
        /// any reason.
        /// </param>
        private void OnCurrentChanging(CurrentChangingEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            if (this._currentChangedMonitor.Busy)
            {
                if (args.IsCancelable)
                {
                    args.Cancel = true;
                }

                return;
            }

            // We should only raise the CurrentChanging event
            // if it hasn't already been raised.  If there is
            // already a pending CurrentChanged event, then raising
            // another CurrentChanging event would be redundant.
            lock (this._currentChangedEventPendingLock)
            {
                if (!this._isCurrentChangedEventPending)
                {
                    CurrentChangingEventHandler handler = this.CurrentChanging;

                    if (handler != null)
                    {
                        handler(this, args);
                    }

                    // If the move was not canceled, set this even if we
                    // have no subscribers so that we always properly
                    // open the "transaction"
                    this._isCurrentChangedEventPending = !args.Cancel;
                }
            }
        }

        /// <summary>
        /// Raise a <see cref="CurrentChanging"/> event that is not cancelable.
        /// </summary>
        protected void RaiseCurrentChanging()
        {
            this.OnCurrentChanging(uncancelableCurrentChangingEventArgs);
        }

        /// <summary>
        /// Raises necessary events for when the currency has changed.
        /// </summary>
        /// <param name="oldCurrentItem">The previous <see cref="CurrentItem"/> value.</param>
        /// <param name="oldCurrentPosition">The previous <see cref="CurrentPosition"/> value.</param>
        /// <param name="oldIsCurrentBeforeFirst">The previous <see cref="IsCurrentBeforeFirst"/> value.</param>
        /// <param name="oldIsCurrentAfterLast">The previous <see cref="IsCurrentAfterLast"/> value.</param>
        protected void RaiseCurrentChanged(object oldCurrentItem, int oldCurrentPosition, bool oldIsCurrentBeforeFirst, bool oldIsCurrentAfterLast)
        {
            // Prevent re-entrancy using the monitor
            if (this._currentChangedMonitor.Enter())
            {
                using (this._currentChangedMonitor)
                {
                    lock (this._currentChangedEventPendingLock)
                    {
                        if (this._isCurrentChangedEventPending)
                        {
                            EventHandler handler = this.CurrentChanged;

                            if (handler != null)
                            {
                                handler(this, EventArgs.Empty);
                            }

                            // Set this even if we have no subscribers so that
                            // we always properly close the "transaction" 
                            this._isCurrentChangedEventPending = false;
                        }
                    }
                }
            }

            if (this.CurrentPosition != oldCurrentPosition)
            {
                this.RaisePropertyChanged("CurrentPosition");
            }

            if (this.CurrentItem != oldCurrentItem)
            {
                this.RaisePropertyChanged("CurrentItem");
            }

            if (this.IsCurrentBeforeFirst != oldIsCurrentBeforeFirst)
            {
                this.RaisePropertyChanged("IsCurrentBeforeFirst");
            }

            if (this.IsCurrentAfterLast != oldIsCurrentAfterLast)
            {
                this.RaisePropertyChanged("IsCurrentAfterLast");
            }
        }

        /// <summary>
        /// Raises a <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">The property that has changed.</param>
        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Set <see cref="CurrentItem"/> and <see cref="CurrentPosition"/>.
        /// </summary>
        /// <remarks>
        /// This method can be called from a constructor - it does not call any virtuals.
        /// <para>
        /// The <paramref name="count"/> parameter is substitute for the real Count, used only
        /// when <paramref name="newItem"/>is <c>null</c>.  In that case, this method sets
        /// <see cref="IsCurrentAfterLast"/> to <c>true</c> if and only if
        /// <paramref name="newPosition"/> >= <paramref name="count"/>.  This distinguishes
        /// between a <c>null</c> belonging to the view and the dummy <c>null</c> when
        /// <see cref="CurrentPosition"/> is past the end.
        /// </para>
        /// </remarks>
        /// <param name="newItem">New <see cref="CurrentItem"/>.</param>
        /// <param name="newPosition">New <see cref="CurrentPosition"/>.</param>
        /// <param name="count">Numbers of items in the collection</param>
        private void SetCurrent(object newItem, int newPosition, int count)
        {
            if (count == 0)
            {
                // empty collection - by convention both flags are true and position is -1
                this._isCurrentBeforeFirst = true;
                this._isCurrentAfterLast = true;
                newPosition = -1;
            }
            else
            {
                // null item, possibly within range.
                this._isCurrentBeforeFirst = newPosition < 0;
                this._isCurrentAfterLast = newPosition >= count;
            }

            if (this._currentItem != newItem || this._currentPosition != newPosition)
            {
                this.RaiseCurrentChanging();

                this._currentItem = newItem;
                this._currentPosition = newPosition;
            }
        }

        /// <summary>
        /// Just move it. No argument check, no events, just move current to position.
        /// </summary>
        /// <param name="position">Position to move the current item to</param>
        protected void SetCurrentToPosition(int position)
        {
            int count = this._delegates.Count();

            if (position < 0)
            {
                this.SetCurrent(null, -1, count);
            }
            else if (position >= count)
            {
                this.SetCurrent(null, count, count);
            }
            else
            {
                this.SetCurrent(this._delegates.GetItemAt(position), position, count);
            }
        }

        /// <summary>
        /// Subscribe to the <see cref="CollectionChanged"/> event on the source collection
        /// if it implements <see cref="INotifyCollectionChanged"/>.
        /// </summary>
        private void SubscribeToCollectionChanged()
        {
            INotifyCollectionChanged collection = this._source as INotifyCollectionChanged;
            if (collection != null)
            {
                collection.CollectionChanged += new NotifyCollectionChangedEventHandler(this.HandleSourceCollectionChanged);
            }
        }

        /// <summary>
        /// Verify that there isn't a pending <see cref="DeferRefresh"/>.  Many operations against
        /// the view are blocked during a deferred refresh.
        /// </summary>
        protected void VerifyRefreshNotDeferred()
        {
            if (this.IsRefreshDeferred)
            {
                throw new InvalidOperationException(EntityCollectionViewResources.NoCheckOrChangeWhenDeferred);
            }
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Gets the enumerator for this <see cref="IEnumerable"/>.
        /// </summary>
        /// <returns>An enumerator for the source enumerable.</returns>
        public virtual IEnumerator GetEnumerator()
        {
            return this._source.GetEnumerator();
        }

        #endregion

        #region ICollectionView Members

        /// <summary>
        /// Indicates whether or not this ICollectionView can do any filtering.
        /// </summary>
        public bool CanFilter
        {
            get { return false; }
        }

        /// <summary>
        /// Returns true if this view really supports grouping.
        /// When this returns false, the rest of the interface is ignored.
        /// </summary>
        public virtual bool CanGroup
        {
            get { return false; }
        }

        /// <summary>
        /// Whether or not this ICollectionView does any sorting.
        /// </summary>
        public virtual bool CanSort
        {
            get { return false; }
        }

        /// <summary>
        /// Return true if the item belongs to this view.  No assumptions are
        /// made about the item. This method will behave similarly to IList.Contains().
        /// </summary>
        /// <param name="item">The item to be tested.</param>
        /// <returns><c>true</c> if the item is in the collection, otherwise <c>false</c>.</returns>
        public bool Contains(object item)
        {
            // If the item isn't a T, then we'll return false
            return this._delegates.Contains(item as Entity);
        }

        /// <summary>
        /// Culture contains the CultureInfo used in any operations of the
        /// ICollectionView that may differ by Culture, such as sorting.
        /// </summary>
        public CultureInfo Culture
        {
            get { return this._culture; }
            set { this._culture = value; }
        }

        /// <summary>
        /// Raise this event before change of current item pointer.  Handlers can cancel the change.
        /// </summary>
        public event CurrentChangingEventHandler CurrentChanging;

        /// <summary>
        /// Raise this event after changing to a new current item.
        /// </summary>
        public event EventHandler CurrentChanged;

        /// <summary>
        /// Return current item.
        /// </summary>
        public object CurrentItem
        {
            get { return this._currentItem; }
        }

        /// <summary>
        /// The ordinal position of the <seealso cref="CurrentItem"/> within the view.
        /// </summary>
        public int CurrentPosition
        {
            get { return this._currentPosition; }
        }

        /// <summary>
        /// Enter a Defer Cycle. Defer cycles are used to coalesce changes to the ICollectionView.
        /// </summary>
        /// <returns>An <see cref="IDisposable"/> object that will close the defer cycle when disposed.</returns>
        public virtual IDisposable DeferRefresh()
        {
            if (this.IsAddingNew || this.IsEditingItem)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.OperationNotAllowedDuringAddOrEdit, "DeferRefresh"));
            }

            System.Threading.Interlocked.Increment(ref this._deferLevel);
            return new DeferHelper(this.DeferRefreshDisposed);
        }

        /// <summary>
        /// Predicate-based filtering is not supported by this class.
        /// </summary>
        /// <exception cref="InvalidOperationException">thrown any time the getter or setter for this property is used.</exception>
        public Predicate<object> Filter
        {
            get { throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.NotSupported, "Filter")); }
            set { throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.NotSupported, "Filter")); }
        }

        /// <summary>
        /// The description of grouping, indexed by level.
        /// </summary>
        public virtual ObservableCollection<GroupDescription> GroupDescriptions
        {
            get { throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.NotSupported, "GroupDescriptions")); }
        }

        /// <summary>
        /// The top-level groups, constructed according to the descriptions
        /// given in GroupDescriptions.
        /// </summary>
        public virtual ReadOnlyObservableCollection<object> Groups
        {
            get { throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.NotSupported, "Groups")); }
        }

        /// <summary>
        /// Return true if <seealso cref="CurrentItem"/> is beyond the end of the view.
        /// </summary>
        public bool IsCurrentAfterLast
        {
            get
            {
                this.VerifyRefreshNotDeferred();
                return this._isCurrentAfterLast;
            }
        }

        /// <summary>
        /// Return true if <seealso cref="CurrentItem"/> is before the beginning of the view.
        /// </summary>
        public bool IsCurrentBeforeFirst
        {
            get
            {
                this.VerifyRefreshNotDeferred();
                return this._isCurrentBeforeFirst;
            }
        }

        /// <summary>
        /// Returns true if the view is emtpy.
        /// </summary>
        public virtual bool IsEmpty
        {
            get { return !this.SourceEntities.Any(); }
        }

        /// <summary>
        /// Move <seealso cref="CurrentItem"/> to the given item.
        /// </summary>
        /// <param name="item">Move CurrentItem to this item.</param>
        /// <returns>true if <seealso cref="CurrentItem"/> points to an item within the view.</returns>
        public bool MoveCurrentTo(object item)
        {
            this.VerifyRefreshNotDeferred();

            // Return -1 if item is not a T
            int index = this._delegates.IndexOf(item as Entity);

            // Index of -1 will move current to BeforeFirst
            return this.MoveCurrentToPosition(index);
        }

        /// <summary>
        /// Move <seealso cref="CurrentItem"/> to the first item.
        /// </summary>
        /// <returns>true if <seealso cref="CurrentItem"/> points to an item within the view.</returns>
        public bool MoveCurrentToFirst()
        {
            this.VerifyRefreshNotDeferred();
            return this.MoveCurrentToPosition(0);
        }

        /// <summary>
        /// Move <seealso cref="CurrentItem"/> to the last item.
        /// </summary>
        /// <returns>true if <seealso cref="CurrentItem"/> points to an item within the view.</returns>
        public bool MoveCurrentToLast()
        {
            this.VerifyRefreshNotDeferred();
            int index = this._delegates.Count() - 1;
            return this.MoveCurrentToPosition(index);
        }

        /// <summary>
        /// Move <seealso cref="CurrentItem"/> to the next item.
        /// </summary>
        /// <returns>true if <seealso cref="CurrentItem"/> points to an item within the view.</returns>
        public bool MoveCurrentToNext()
        {
            this.VerifyRefreshNotDeferred();
            int index = this.CurrentPosition + 1;

            if (index < this._delegates.Count())
            {
                return this.MoveCurrentToPosition(index);
            }
            else
            {
                this.EnsureValidCurrency();
                return false;
            }
        }

        /// <summary>
        /// Move <seealso cref="CurrentItem"/> to the previous item.
        /// </summary>
        /// <returns>true if <seealso cref="CurrentItem"/> points to an item within the view.</returns>
        public bool MoveCurrentToPrevious()
        {
            this.VerifyRefreshNotDeferred();
            int index = this.CurrentPosition - 1;

            if (index >= 0)
            {
                return this.MoveCurrentToPosition(index);
            }
            else
            {
                this.EnsureValidCurrency();
                return false;
            }
        }

        /// <summary>
        /// Move <seealso cref="CurrentItem"/> to the item at the given index.
        /// </summary>
        /// <param name="position">Move CurrentItem to this index</param>
        /// <returns>true if <seealso cref="CurrentItem"/> points to an item within the view.</returns>
        public bool MoveCurrentToPosition(int position)
        {
            this.VerifyRefreshNotDeferred();

            // We want to allow the user to set the currency to just
            // beyond the last item. EnumerableCollectionView in WPF
            // also checks (position > this.Count) though the ListCollectionView
            // looks for (position >= this.Count).
            if (position < -1 || position > this._delegates.Count())
            {
                throw new ArgumentOutOfRangeException("position");
            }

            if ((position != this.CurrentPosition || !this.IsCurrentInSync) && this.OkToChangeCurrent())
            {
                object oldCurrentItem = this.CurrentItem;
                int oldCurrentPosition = this.CurrentPosition;
                bool oldIsCurrentAfterLast = this.IsCurrentAfterLast;
                bool oldIsCurrentBeforeFirst = this.IsCurrentBeforeFirst;

                this.SetCurrentToPosition(position);
                this.RaiseCurrentChanged(oldCurrentItem, oldCurrentPosition, oldIsCurrentBeforeFirst, oldIsCurrentAfterLast);
            }

            return this._currentPosition >= 0;
        }

        /// <summary>
        /// Re-create the view, using any <seealso cref="SortDescriptions"/>.
        /// </summary>
        public virtual void Refresh()
        {
        }

        /// <summary>
        /// Collection of Sort criteria to sort items in this view over the SourceCollection.
        /// </summary>
        public virtual SortDescriptionCollection SortDescriptions
        {
            get { throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.NotSupported, "SortDescriptions")); }
        }

        /// <summary>
        /// SourceCollection is the original un-filtered collection of which
        /// this ICollectionView is a view.
        /// </summary>
        public IEnumerable SourceCollection
        {
            get { return this._source; }
        }

        #endregion

        #region INotifyCollectionChanged Members

        /// <summary>
        /// Occurs when the collection changes, either by adding or removing an item.
        /// </summary>
        /// <remarks>
        /// The event handler receives an argument of type
        /// <seealso cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs" />
        /// containing data related to this event.
        /// </remarks>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        #region IEditableCollectionView Members

        /// <summary>
        /// Add a new item to the underlying collection.  Returns the new item.
        /// After calling AddNew and changing the new item as desired, either
        /// <see cref="CommitNew"/> or <see cref="CancelNew"/> should be called
        /// to complete the transaction.
        /// </summary>
        /// <returns>The item that is created and added.</returns>
        public object AddNew()
        {
            this.VerifyRefreshNotDeferred();

            if (this.IsEditingItem)
            {
                // Implicitly close a previous EditItem
                this.CommitEdit();
            }

            // Implicitly close a previous AddNew
            this.CommitNew();

            if (!this.CanAddNew)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.NotSupported, "AddNew"));
            }

            Entity newItem = this._delegates.CreateInstance();

            this._delegates.Add(newItem);

            // set the current new item and the current item
            this.CurrentAddItem = newItem;
            int index = this._delegates.IndexOf(newItem);
            this.SetCurrent(newItem, index, this._delegates.Count());

            // Begin the edit on the new item
            ((IEditableObject)newItem).BeginEdit();

            return newItem;
        }

        /// <summary>
        /// Return true if the view supports <seealso cref="AddNew"/>.
        /// </summary>
        public bool CanAddNew
        {
            get
            {
                return this._canAddNew;
            }
            protected set
            {
                if (this._canAddNew != value)
                {
                    this._canAddNew = value;
                    this.RaisePropertyChanged("CanAddNew");
                }
            }
        }

        /// <summary>
        /// Returns true if the view supports the notion of "pending changes" on the
        /// current edit item.
        /// </summary>
        public bool CanCancelEdit
        {
            get
            {
                return this._canCancelEdit;
            }
            private set
            {
                if (this._canCancelEdit != value)
                {
                    this._canCancelEdit = value;
                    this.RaisePropertyChanged("CanCancelEdit");
                }
            }
        }

        /// <summary>
        /// Return true if the view supports <seealso cref="Remove"/> and
        /// <seealso cref="RemoveAt"/>.
        /// </summary>
        public bool CanRemove
        {
            get
            {
                return this._canRemove;
            }
            private set
            {
                if (this._canRemove != value)
                {
                    this._canRemove = value;
                    this.RaisePropertyChanged("CanRemove");
                }
            }
        }

        /// <summary>
        /// Complete the transaction started by <seealso cref="EditItem"/>.
        /// The pending changes (if any) to the item are discarded.
        /// </summary>
        public void CancelEdit()
        {
            if (this.IsAddingNew)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.OperationNotAllowedDuringTransaction, "CancelEdit", "AddNew"));
            }
            else if (!this.CanCancelEdit)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.NotSupported, "CancelEdit"));
            }

            this.VerifyRefreshNotDeferred();

            if (!this.IsEditingItem)
            {
                return;
            }

            object editItem = this.CurrentEditItem;
            this.CurrentEditItem = null;

            ((IEditableObject)editItem).CancelEdit();
        }

        /// <summary>
        /// Cancel the transaction started by <seealso cref="AddNew"/>.  The new
        /// item is removed from the collection.
        /// </summary>
        public void CancelNew()
        {
            if (this.IsEditingItem)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.OperationNotAllowedDuringTransaction, "CancelNew", "EditItem"));
            }

            this.VerifyRefreshNotDeferred();

            // If we don't have the capability to remove, then we cannot cancel the add operation.
            // Don't test CanRemove, because this usage of Remove is only dependent upon the Remove
            // delegate being provided and not the other factors of CanRemove.
            if (this._delegates.Remove == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.NotSupported, "CancelNew (Remove)"));
            }

            if (!this.IsAddingNew)
            {
                return;
            }

            // End editing on the item by canceling the edit
            Entity newItem = this.EndAddNew(true);

            this._delegates.Remove(newItem);
        }

        /// <summary>
        /// Complete the transaction started by <seealso cref="EditItem"/>.
        /// The pending changes (if any) to the item are committed.
        /// </summary>
        public void CommitEdit()
        {
            this.VerifyRefreshNotDeferred();

            if (this.IsAddingNew)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.OperationNotAllowedDuringTransaction, "CommitEdit", "AddNew"));
            }

            if (!this.IsEditingItem)
            {
                return;
            }

            Entity editItem = (Entity)this.CurrentEditItem;
            this.CurrentEditItem = null;

            // End the edit if the source collection supports it
            if (this.IsOperationSupported(EntitySetOperations.Edit))
            {
                ((IEditableObject)editItem).EndEdit();
            }

            this.CommittedEdit(editItem);
        }

        /// <summary>
        /// Complete the transaction started by <seealso cref="AddNew"/>.
        /// The pending changes (if any) to the item are committed.
        /// </summary>
        public void CommitNew()
        {
            if (this.IsEditingItem)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.OperationNotAllowedDuringTransaction, "CommitNew", "EditItem"));
            }

            if (!this.IsAddingNew)
            {
                return;
            }

            // End the AddNew transaction
            Entity newItem = this.EndAddNew(false);
            this.CommittedNew(newItem);
        }

        /// <summary>
        /// When an <seealso cref="AddNew"/> transaction is in progress, this property
        /// returns the new item.  Otherwise it returns null.
        /// </summary>
        public object CurrentAddItem
        {
            get
            {
                return this._currentAddItem;
            }
            private set
            {
                if (!Object.Equals(this._currentAddItem, value))
                {
                    this._currentAddItem = (Entity)value;
                    this.RaisePropertyChanged("CurrentAddItem");
                    this.CalculateIsAddingNew();
                }
            }
        }

        /// <summary>
        /// When an <seealso cref="EditItem"/> transaction is in progress, this property
        /// returns the affected item.  Otherwise it returns null.
        /// </summary>
        public object CurrentEditItem
        {
            get
            {
                return this._currentEditItem;
            }
            private set
            {
                if (!Object.Equals(this._currentEditItem, value))
                {
                    this._currentEditItem = (Entity)value;
                    this.RaisePropertyChanged("CurrentEditItem");
                    this.CalculateIsEditingItem();
                }
            }
        }

        /// <summary>
        /// Begins an editing transaction on the given item.  The transaction is
        /// completed by calling either <see cref="CommitEdit"/> or <see cref="CancelEdit"/>.
        /// Any changes made to the item during the transaction are considered "pending", provided 
        /// that the view supports the notion of "pending changes" for the given item.
        /// </summary>
        /// <remarks>
        /// While it's possible that the underlying collection doesn't
        /// support editing, the <see cref="IEditableCollectionView"/> interface does
        /// not have a mechanism for specifying that CanEdit is <c>false</c>.  Throwing
        /// an exception if editing is disallowed would lead to a crash when attempting to edit
        /// the item, without being able to check for the condition beforehand.  By allowing this
        /// operation to succeed, the caller when then get an exception only if changes are made
        /// on the entity's properties, and that can surface as a validation error in the UI.
        /// </remarks>
        /// <param name="item">Item to be edited.</param>
        public void EditItem(object item)
        {
            this.VerifyRefreshNotDeferred();

            Entity entity = item as Entity;

            if (entity == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.ItemNotEntityType, this._entityType.Name), "item");
            }

            if (this.IsAddingNew)
            {
                if (Object.Equals(entity, this.CurrentAddItem))
                {
                    // EditItem(CurrentAddItem) is a no-op
                    return;
                }

                // implicitly close a previous AddNew
                this.CommitNew();
            }

            if (this.IsEditingItem)
            {
                if (Object.Equals(entity, this.CurrentEditItem))
                {
                    // EditItem(CurrentEditItem) is a no-op
                    return;
                }

                // implicitly close a previous EditItem transaction
                this.CommitEdit();
            }

            this.CurrentEditItem = entity;

            // Only begin the edit if the collection supports it, otherwise
            // exceptions will be thrown if any properties are changed.
            if (this.IsOperationSupported(EntitySetOperations.Edit))
            {
                ((IEditableObject)entity).BeginEdit();
            }
        }

        /// <summary>
        /// Returns true if an <seealso cref="AddNew"/> transaction is in progress.
        /// </summary>
        public bool IsAddingNew
        {
            get
            {
                return this._isAddingNew;
            }
            private set
            {
                if (this._isAddingNew != value)
                {
                    this._isAddingNew = value;
                    this.RaisePropertyChanged("IsAddingNew");
                    this.CalculateCanRemove();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether an <see cref="EditItem"/> transaction is in progress.
        /// </summary>
        public bool IsEditingItem
        {
            get
            {
                return this._isEditingItem;
            }

            private set
            {
                if (this._isEditingItem != value)
                {
                    this._isEditingItem = value;
                    this.RaisePropertyChanged("IsEditingItem");
                    this.CalculateCanCancelEdit();
                    this.CalculateCanRemove();
                }
            }
        }

        /// <summary>
        /// Indicates whether to include a placeholder for a new item, and if so,
        /// where to put it.
        /// </summary>
        public NewItemPlaceholderPosition NewItemPlaceholderPosition
        {
            get
            {
                return NewItemPlaceholderPosition.None;
            }
            set
            {
                if ((NewItemPlaceholderPosition)value != NewItemPlaceholderPosition.None)
                {
                    throw new ArgumentException(
                        string.Format(CultureInfo.InvariantCulture,
                            EntityCollectionViewResources.InvalidEnumArgument,
                            "value",
                            value.ToString(),
                            typeof(NewItemPlaceholderPosition).Name));
                }
            }
        }

        /// <summary>
        /// Remove the given item from the underlying collection.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        public void Remove(object item)
        {
            if (this.IsEditingItem || this.IsAddingNew)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.OperationNotAllowedDuringAddOrEdit, "Remove"));
            }
            else if (!this.CanRemove)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.NotSupported, "Remove"));
            }

            Entity entity = item as Entity;
            if (entity == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, EntityCollectionViewResources.ItemNotEntityType, this._entityType.Name), "item");
            }

            this._delegates.Remove(entity);
        }

        /// <summary>
        /// Remove the item at the given index from the underlying collection.
        /// The index is interpreted with respect to the view (not with respect to
        /// the underlying collection).
        /// </summary>
        /// <param name="index">The index of the item to remove.</param>
        public void RemoveAt(int index)
        {
            if (this.IsEditingItem || this.IsAddingNew)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.OperationNotAllowedDuringAddOrEdit, "RemoveAt"));
            }
            else if (!this.CanRemove)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.NotSupported, "RemoveAt"));
            }

            this._delegates.RemoveAt(index);
        }

        #endregion

        #region INotifyPropertyChanged Members

        /// <summary>
        /// Event raised when a public property value has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Nested Classes / Enums / Delegates

        /// <summary>
        /// A simple monitor class to help prevent re-entrant calls
        /// </summary>
        private class SimpleMonitor : IDisposable
        {
            /// <summary>
            /// Whether the monitor is entered
            /// </summary>
            private bool entered;

            /// <summary>
            /// Gets a value indicating whether we have been entered or not
            /// </summary>
            public bool Busy
            {
                get { return this.entered; }
            }

            /// <summary>
            /// Sets a value indicating that we have been entered
            /// </summary>
            /// <returns>Boolean value indicating whether we were already entered</returns>
            public bool Enter()
            {
                if (this.entered)
                {
                    return false;
                }

                this.entered = true;
                return true;
            }

            /// <summary>
            /// Cleanup method called when done using this class
            /// </summary>
            public void Dispose()
            {
                this.entered = false;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// A class that defines the delegates needed for the
        /// <see cref="EntityCollectionView"/>.
        /// </summary>
        /// <remarks>
        /// Consumers of the <see cref="EntityCollectionView"/>
        /// can specify delegates to call for these methods, and
        /// derived collection views can override
        /// <see cref="ApplyDefaultDelegates"/> to override behavior.
        /// </remarks>
        protected internal class CollectionViewDelegates
        {
            /// <summary>
            /// Used from the <see cref="ICollectionView.Contains"/> method.
            /// </summary>
            public Func<Entity, bool> Contains { get; set; }

            /// <summary>
            /// Gets the index of the specified entity.  Used to
            /// maintain valid currency position.
            /// </summary>
            public Func<Entity, int> IndexOf { get; set; }

            /// <summary>
            /// Gets the entity at the specified index.  Used to
            /// maintain valid currency position.
            /// </summary>
            public Func<int, Entity> GetItemAt { get; set; }

            /// <summary>
            /// Used to maintain valid currency position.
            /// </summary>
            public Func<int> Count { get; set; }

            /// <summary>
            /// Used from the <see cref="IEditableCollectionView.AddNew"/> method
            /// to add a created entity to the source.
            /// </summary>
            public Action<Entity> Add { get; set; }

            /// <summary>
            /// Used from the <see cref="IEditableCollectionView.Remove"/> method
            /// to remove an entity from the source.
            /// </summary>
            /// <remarks>
            /// Also used from <see cref="IEditableCollectionView.CancelNew"/> to
            /// remove a newly added entity
            /// </remarks>
            public Action<Entity> Remove { get; set; }

            /// <summary>
            /// Used from the <see cref="IEditableCollectionView.RemoveAt"/> method
            /// to remove an entity from the source by index.
            /// </summary>
            public Action<int> RemoveAt { get; set; }

            /// <summary>
            /// Used from the <see cref="IEditableCollectionView.AddNew"/> method
            /// to construct a new entity to be added to the source.
            /// </summary>
            public Func<Entity> CreateInstance { get; set; }
        }

        #endregion
    }
}
