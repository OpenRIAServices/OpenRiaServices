using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.DomainServices.Client;
using System.Windows.Data;

namespace System.Windows.Controls
{
    /// <summary>
    /// Internal collection owned by the <see cref="DomainDataSource"/> and exposed to its <see cref="EntityCollectionView"/>.
    /// </summary>
    internal class PagedEntityCollection :
        Collection<Entity>,
        IPagedEntityList
    {
        #region Member Fields

        /// <summary>
        /// Represents the MoveToPage callback to use when invoking a <see cref="MoveToPage"/> operation.
        /// </summary>
        private Func<int, bool> _moveToPageCallback;

        /// <summary>
        /// Represents the minimum number of items known to be in the source collection.
        /// </summary>
        private int _itemCount;

        /// <summary>
        /// The number of pages loaded.
        /// </summary>
        private int _pageCount;

        /// <summary>
        /// Represents the current page index.
        /// </summary>
        private int _pageIndex = -1;

        /// <summary>
        /// Represents the page size.
        /// </summary>
        private int _pageSize;

        /// <summary>
        /// Map of what entities are on what page indexes.  This is used to
        /// maintain page affinity as the collection is modified and pages
        /// are navigated.
        /// </summary>
        private Dictionary<int, List<Entity>> _pageTracking;

        /// <summary>
        /// Whether or not to raise collection changed events.  During a
        /// load operation, events are suppressed.
        /// </summary>
        private bool _raiseCollectionChangedEvents = true;

        /// <summary>
        /// Underlying entity set
        /// </summary>
        private EntitySet _sourceEntitySet;

        /// <summary>
        /// Type of entity in the list
        /// </summary>
        private Type _entityType;

        /// <summary>
        /// Represents the index of the first page of items provided by the source collection.
        /// </summary>
        private int _startPageIndex;

        /// <summary>
        /// Represents the total number of items in the source collection, or -1 if that value is unknown.
        /// </summary>
        private int _totalItemCount = -1;

        private bool _isPagingOperationPending;

        #endregion Member Fields

        #region Constructors

        /// <summary>
        /// Default constructor, accepting a callback for when <see cref="MoveToPage"/> is invoked.
        /// </summary>
        /// <param name="moveToPageCallback">
        /// The function to call when a <see cref="MoveToPage"/> operation is invoked.
        /// <para>
        /// The <c>int</c> of the <paramref name="moveToPageCallback"/> represents the pageIndex
        /// parameter from the <see cref="MoveToPage"/> method.
        /// </para>
        /// <para>
        /// The <c>bool</c> of the <paramref name="moveToPageCallback"/> represents the return
        /// value of the <see cref="MoveToPage"/> method, indicating whether or not
        /// the paging operation was successfully handled.
        /// </para>
        /// </param>
        public PagedEntityCollection(Func<int, bool> moveToPageCallback)
        {
            if (moveToPageCallback == null)
            {
                throw new ArgumentNullException("moveToPageCallback");
            }

            this._moveToPageCallback = moveToPageCallback;

            this.CalculateIsPagingOperationPending();
        }

        #endregion Constructors

        #region INotifyPropertyChanged Members

        /// <summary>
        /// Event exposed by the INotifyPropertyChanged interface
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion INotifyPropertyChanged Members

        #region INotifyCollectionChanged Members

        /// <summary>
        /// Occurs when this collection has changed.
        /// </summary>
        //// TODO Check why EntitySet declares this event as non-CLS compliant
        //// [CLSCompliant(false)]
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion INotifyCollectionChanged Members

        #region IPagedEntityList Members

        /// <summary>
        /// Gets or sets the source entity collection
        /// </summary>
        public EntitySet BackingEntitySet
        {
            get
            {
                return this._sourceEntitySet;
            }

            set
            {
                if (this._sourceEntitySet != value)
                {
                    if (this._sourceEntitySet != null)
                    {
                        this.UnhookSourceCollectionChangeNotifications();
                    }

                    this._sourceEntitySet = value;
                    this.RaisePropertyChanged("BackingEntitySet");

                    if (value != null)
                    {
                        this.HookupSourceCollectionChangeNotifications();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the type of <see cref="Entity"/> within the list
        /// </summary>
        public Type EntityType
        {
            get
            {
                return this._entityType;
            }

            set
            {
                if (this._entityType != value)
                {
                    this._entityType = value;
                    this.RaisePropertyChanged("EntityType");
                }
            }
        }

        /// <summary>
        /// Gets a value indicating that paging is enabled but the data hasn't yet been loaded.
        /// </summary>
        public bool IsPagingOperationPending
        {
            get
            {
                return this._isPagingOperationPending;
            }

            private set
            {
                if (this._isPagingOperationPending != value)
                {
                    this._isPagingOperationPending = value;
                    this.RaisePropertyChanged("IsPagingOperationPending");
                }
            }
        }

        /// <summary>
        /// Gets a value indicating the minimum number of items known to be in the source collection.
        /// </summary>
        public int ItemCount
        {
            get
            {
                return this._itemCount;
            }

            set
            {
                if (this._itemCount != value)
                {
                    this._itemCount = value;
                    this.RaisePropertyChanged("ItemCount");
                }
            }
        }

        /// <summary>
        /// Gets or sets the current page index
        /// </summary>
        public int PageIndex
        {
            get
            {
                return this._pageIndex;
            }
            set
            {
                if (this._pageIndex != value)
                {
                    this._pageIndex = value;
                    this.RaisePropertyChanged("PageIndex");
                    this.CalculateIsPagingOperationPending();
                }
            }
        }

        /// <summary>
        /// Gets or sets the number of items to display on a page.
        /// </summary>
        public int PageSize
        {
            get
            {
                return this._pageSize;
            }

            set
            {
                if (this._pageSize != value)
                {
                    this._pageSize = value;
                    this.RaisePropertyChanged("PageSize");
                }
            }
        }

        /// <summary>
        /// Gets the total number of items in the source collection, 
        /// or -1 if that value is unknown.
        /// </summary>
        public int TotalItemCount
        {
            get
            {
                return this._totalItemCount;
            }

            set
            {
                if (this._totalItemCount != value)
                {
                    this._totalItemCount = value;
                    this.RaisePropertyChanged("TotalItemCount");
                }
            }
        }

        /// <summary>
        /// Transmits the request for a page move.
        /// </summary>
        /// <param name="pageIndex">Requested page index</param>
        /// <returns>True if an asynchronous page move was initiated, False otherwise</returns>
        public bool MoveToPage(int pageIndex)
        {
            // If we've not loaded this pageIndex before, then call the moveToPageCallback
            if (this._pageTracking == null || !this._pageTracking.ContainsKey(pageIndex))
            {
                if (this._moveToPageCallback(pageIndex))
                {
                    return true;
                }
            }
            else
            {
                // Since we're not going to invoke a new load when moving backwards,
                // we need to indicate that we have data already loaded for the page
                // index requested. This ensures that IsPagingOperationPending
                // doesn't return true.
                if (pageIndex < this.StartPageIndex)
                {
                    this.StartPageIndex = pageIndex;
                }
            }

            if (this._pageTracking != null)
            {
                // Don't raise events
                base.ClearItems();
                this.PageIndex = pageIndex;

                this.AddTrackedItems(e => this.InsertItemWithoutEvents(this.Count, e));
                this.RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            return false;
        }

        /// <summary>
        /// Raised when a page change has completed.
        /// </summary>
        public event EventHandler<EventArgs> PageChanged;

        #endregion

        #region Internal Properties

        /// <summary>
        /// Gets the count of non-new entities that are being tracked, starting with the start page index
        /// and ending based on our load size / page size (how many pages are loaded at once).
        /// <para>
        /// When paging is disabled, it just returns Count.
        /// </para>
        /// </summary>
        internal int LoadedItemCount
        {
            get
            {
                if (this._pageTracking != null && this.PageSize > 0)
                {
                    return this._pageTracking.Keys
                        .Where(p => p >= this.StartPageIndex && p < this.StartPageIndex + this.PageCount)
                        .Sum(p => this._pageTracking[p].Count(e => e.EntityState != EntityState.New));
                }

                return this.Count;
            }
        }

        /// <summary>
        /// Gets or sets the number of pages loaded.
        /// </summary>
        internal int PageCount
        {
            get
            {
                return this._pageCount;
            }
            set
            {
                if (this._pageCount != value)
                {
                    this._pageCount = value;
                    this.RaisePropertyChanged("PageCount");
                    this.RaisePropertyChanged("ItemCount");
                }
            }
        }

        /// <summary>
        /// Gets the index of the first page of items provided by the source collection.
        /// </summary>
        internal int StartPageIndex
        {
            get
            {
                return this._startPageIndex;
            }

            set
            {
                if (this._startPageIndex != value)
                {
                    this._startPageIndex = value;
                    this.RaisePropertyChanged("StartPageIndex");
                    this.CalculateIsPagingOperationPending();
                }
            }
        }

        /// <summary>
        /// Gets the total number of pages based on the <see cref="TotalItemCount"/>
        /// and the <see cref="PageSize"/>.
        /// </summary>
        internal bool IsLastPage
        {
            get
            {
                if (this.PageSize == 0)
                {
                    return true;
                }

                if (this.TotalItemCount == -1)
                {
                    return false;
                }

                int totalPageCount = PagingHelper.CalculatePageCount(this.TotalItemCount, this.PageSize);
                return (totalPageCount == this.PageIndex + 1);
            }
        }

        #endregion Internal Properties

        #region Collection<Entity> Overrides

        /// <summary>
        /// Clear the items and raise a Reset CollectionChanged event.
        /// </summary>
        protected override void ClearItems()
        {
            base.ClearItems();

            if (this._raiseCollectionChangedEvents)
            {
                this.RaiseCollectionChanged(NotifyCollectionChangedAction.Reset, null, -1);
            }
        }

        /// <summary>
        /// Track what page an item is inserted to.  If inserted to the current page,
        /// then insert the item to the underlying collection, otherwise just record
        /// its existence on another page.
        /// </summary>
        /// <param name="index">The desired index for insertion.</param>
        /// <param name="item">The item to be inserted.</param>
        protected override void InsertItem(int index, Entity item)
        {
            int? trackedPage = this.GetEntityPageTracking(item);

            if (trackedPage == null)
            {
                this.TrackEntityPage(item, this.PageIndex);
                trackedPage = this.PageIndex;
            }

            if (trackedPage == this.PageIndex)
            {
                base.InsertItem(index, item);

                if (this._raiseCollectionChangedEvents)
                {
                    this.RaiseCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
                }
            }

            if ((this.PageSize == 0 || trackedPage >= this.StartPageIndex) && this._raiseCollectionChangedEvents)
            {
                this.RaisePropertyChanged("ItemCount");
            }
        }

        /// <summary>
        /// Remove an item from the underlying collection and raise the CollectionChanged event.
        /// </summary>
        /// <param name="index">The index of the item to be removed.</param>
        protected override void RemoveItem(int index)
        {
            Entity item = this[index];

            base.RemoveItem(index);
            this.RaiseCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
        }

        /// <summary>
        /// Update an item in the list and raise a CollectionChanged event.
        /// </summary>
        /// <param name="index">The index of the item to be replaced.</param>
        /// <param name="item">The new item.</param>
        protected override void SetItem(int index, Entity item)
        {
            base.SetItem(index, item);
            this.RaiseCollectionChanged(NotifyCollectionChangedAction.Replace, item, index);
        }

        #endregion Collection<Entity> Overrides

        #region Internal Methods

        /// <summary>
        /// Adds a loaded entity into the collection, tracking the start page index
        /// that was used to load the entity.
        /// </summary>
        /// <param name="loadedEntity">The entity to be added.</param>
        internal void AddLoadedEntity(Entity loadedEntity)
        {
            if (this.PageSize > 0)
            {
                if (this._pageTracking == null)
                {
                    this._pageTracking = new Dictionary<int, List<Entity>>();
                }

                int? existingPage = this.GetEntityPageTracking(loadedEntity);
                int trackedPageIndex = this.StartPageIndex;

                if (existingPage != trackedPageIndex)
                {
                    // When the start page index is full (of other entities that aren't new), overflow onto subsequent pages
                    while (this._pageTracking.ContainsKey(trackedPageIndex)
                        && this._pageTracking[trackedPageIndex].Count(e => e != loadedEntity && e.EntityState != EntityState.New) >= this.PageSize)
                    {
                        ++trackedPageIndex;
                    }

                    if (!this._pageTracking.ContainsKey(trackedPageIndex))
                    {
                        this._pageTracking.Add(trackedPageIndex, new List<Entity>());
                    }

                    // If we've found that this entity is not being tracked on the target page
                    if (!this._pageTracking[trackedPageIndex].Contains(loadedEntity))
                    {
                        // Remove from an existing page if found
                        if (existingPage != null)
                        {
                            this._pageTracking[existingPage.Value].Remove(loadedEntity);
                        }

                        this._pageTracking[trackedPageIndex].Add(loadedEntity);
                    }
                }
            }

            if (!this.Contains(loadedEntity))
            {
                this.Add(loadedEntity);
            }
        }

        /// <summary>
        /// Indicates that a load is beginning and collection changed
        /// events should be suppressed.
        /// </summary>
        internal void BeginLoad()
        {
            this._raiseCollectionChangedEvents = false;
        }

        /// <summary>
        /// Clear the collection, specifying whether or not it's being
        /// cleared for an initial load of entities.
        /// </summary>
        /// <param name="isInitialLoad">Indicates whether or not this clear
        /// is being done as part of an initial load of data.</param>
        internal void Clear(bool isInitialLoad)
        {
            this.Clear();

            if (isInitialLoad)
            {
                // Any added entities still need to be tracked
                this.ClearPageTracking(false);
            }
        }

        /// <summary>
        /// Clear page tracking, optionally clearing the added items' page tracking.
        /// </summary>
        /// <param name="clearAddedItemTracking">Whether or not to also clear the page tracking
        /// for added items.</param>
        internal void ClearPageTracking(bool clearAddedItemTracking)
        {
            if (this._pageTracking == null)
            {
                return;
            }

            if (!clearAddedItemTracking)
            {
                Dictionary<int, List<Entity>> oldTracking = this._pageTracking;
                this._pageTracking = null;

                foreach (int page in oldTracking.Keys)
                {
                    foreach (Entity entity in oldTracking[page])
                    {
                        if (entity.EntityState == EntityState.New)
                        {
                            this.TrackEntityPage(entity, page);
                        }
                    }
                }
            }
            else
            {
                this._pageTracking = null;
            }
        }

        /// <summary>
        /// Indicate that a load has completed, raising a collection changed notification for the Reset
        /// </summary>
        internal void CompleteLoad()
        {
            // Ensure that the added entities are exposed in the collection
            this.AddTrackedItems(e => this.Add(e));

            this.RaisePropertyChanged("ItemCount");
            this.RaiseCollectionChanged(NotifyCollectionChangedAction.Reset, null, -1);
            this._raiseCollectionChangedEvents = true;
        }

        /// <summary>
        /// Updates the <see cref="StartPageIndex"/> and <see cref="PageIndex"/> properties.
        /// </summary>
        /// <param name="startPageIndex">Final start page index</param>
        /// <param name="pageIndex">Final page index</param>
        internal void NotifyPageChanged(int startPageIndex, int pageIndex)
        {
            this.StartPageIndex = startPageIndex;
            this.PageIndex = pageIndex;

            EventHandler<EventArgs> handler = this.PageChanged;

            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        #endregion Internal Methods

        #region Private Methods

        /// <summary>
        /// Ensures that any entities that have been recorded as on the current page
        /// are added to the collection.  When there are tracked items that need to be
        /// added, the specified <paramref name="addAction"/> is used to perform the add.
        /// </summary>
        /// <param name="addAction">The action to call for each entity that needs to be added.</param>
        private void AddTrackedItems(Action<Entity> addAction)
        {
            if (this._pageTracking != null)
            {
                // If this is the last page and there are tracked pages beyond
                // this page, then move added items from those pages onto this page.
                if (this.IsLastPage)
                {
                    int maxPage = this._pageTracking.Keys.Max();

                    for (int page = this.PageIndex + 1; page <= maxPage; page++)
                    {
                        if (this._pageTracking.ContainsKey(page))
                        {
                            foreach (Entity entity in this._pageTracking[page].Where(e => e.EntityState == EntityState.New))
                            {
                                this._pageTracking[this.PageIndex].Add(entity);
                            }

                            this._pageTracking.Remove(page);
                        }
                    }
                }

                if (this._pageTracking.ContainsKey(this.PageIndex))
                {
                    // Add the existing items first and then the added items, to retain the correct order
                    foreach (Entity entity in this._pageTracking[this.PageIndex].Where(e => e.EntityState == EntityState.Modified || e.EntityState == EntityState.Unmodified))
                    {
                        if (!this.Contains(entity))
                        {
                            addAction(entity);
                        }
                    }

                    foreach (Entity entity in this._pageTracking[this.PageIndex].Where(e => e.EntityState == EntityState.New))
                    {
                        if (!this.Contains(entity))
                        {
                            addAction(entity);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calculates <see cref="IsPagingOperationPending"/> using <see cref="StartPageIndex"/>
        /// and <see cref="PageIndex"/>.
        /// </summary>
        private void CalculateIsPagingOperationPending()
        {
            // When the start page index is greater than the page index,
            // then we haven't loaded the data for the page index yet.
            this.IsPagingOperationPending = this.StartPageIndex > this.PageIndex;
        }

        /// <summary>
        /// Get the page tracking for an entity
        /// </summary>
        /// <param name="entity">The entity being tracked</param>
        /// <returns>A nullable int that represents the page the entity is being
        /// tracked against, or null if the entity is not being tracked against a page.
        /// </returns>
        private int? GetEntityPageTracking(Entity entity)
        {
            if (this._pageTracking == null)
            {
                return null;
            }

            return (from p in this._pageTracking.Keys
                    where this._pageTracking[p].Contains(entity)
                    select p).Cast<int?>().FirstOrDefault();
        }

        /// <summary>
        /// Attaches to the source's INotifyPropertyChanged and INotifyCollectionChanged events.
        /// </summary>
        private void HookupSourceCollectionChangeNotifications()
        {
            Debug.Assert(this._sourceEntitySet != null, "Unexpected _sourceEntityCollection == null");

            ((INotifyCollectionChanged)this._sourceEntitySet).CollectionChanged += new NotifyCollectionChangedEventHandler(this.SourceEntitySet_CollectionChanged);
        }

        /// <summary>
        /// Insert an item without raising <see cref="CollectionChanged"/> events.
        /// </summary>
        /// <param name="index">The index at which to add the item.</param>
        /// <param name="item">The <see cref="Entity"/> to add.</param>
        private void InsertItemWithoutEvents(int index, Entity item)
        {
            base.InsertItem(index, item);
        }

        /// <summary>
        /// Notifies the consuming <see cref="EntityCollectionView"/> of the provided source collection change.
        /// </summary>
        /// <param name="e">Event argument to use for the notification</param>
        private void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventHandler handler = this.CollectionChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Notifies the consuming PagedCollectionView of a source collection change.
        /// </summary>
        /// <param name="action">Type of collection change</param>
        /// <param name="entity">Potential <see cref="Entity"/> affected by the change</param>
        /// <param name="index">Index of that potential entity</param>
        private void RaiseCollectionChanged(NotifyCollectionChangedAction action, Entity entity, int index)
        {
            Debug.Assert(action != NotifyCollectionChangedAction.Replace, "Unexpected action == Replace");

            NotifyCollectionChangedEventHandler handler = this.CollectionChanged;
            if (handler != null)
            {
                NotifyCollectionChangedEventArgs args = null;
                if (action == NotifyCollectionChangedAction.Add)
                {
                    args = new NotifyCollectionChangedEventArgs(action, entity, index);
                }
                else if (action == NotifyCollectionChangedAction.Remove)
                {
                    args = new NotifyCollectionChangedEventArgs(action, entity, index);
                }
                else if (action == NotifyCollectionChangedAction.Reset)
                {
                    args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                }

                handler(this, args);
            }
        }

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Reset the collection, clearing all items and tracking all items in the
        /// source collection as added items, so that our list then contains the
        /// items that are being tracked that remain in the source collection.
        /// </summary>
        private void Reset()
        {
            try
            {
                this._raiseCollectionChangedEvents = false;

                this.Clear(false);
                this.TrackAddedItems(this._sourceEntitySet.Cast<Entity>());
            }
            finally
            {
                this._raiseCollectionChangedEvents = true;
            }

            this.RaiseCollectionChanged(NotifyCollectionChangedAction.Reset, null, -1);
        }

        /// <summary>
        /// Handles <see cref="INotifyCollectionChanged.CollectionChanged"/> events from the source
        /// <see cref="EntitySet"/>, synchronizing those changes to the local collection.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event argument</param>
        private void SourceEntitySet_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    this.TrackAddedItems(e.NewItems.Cast<Entity>());
                    break;

                case NotifyCollectionChangedAction.Remove:
                    this.TrackRemovedItems(e.OldItems.Cast<Entity>());
                    break;

                case NotifyCollectionChangedAction.Reset:
                    this.Reset();
                    break;
            }
        }

        /// <summary>
        /// Tracks items that were added to the source entity set.  If an entity
        /// is being added after it was removed from our list, then we add it back.
        /// </summary>
        /// <param name="addedItems">The <see cref="Entity"/> items that were added to the source entity set.</param>
        private void TrackAddedItems(IEnumerable<Entity> addedItems)
        {
            if (this._pageTracking == null)
            {
                // We aren't tracking any items, so don't add anything
                return;
            }

            foreach (Entity addedEntity in addedItems)
            {
                if (addedEntity != null && addedEntity.EntityState != EntityState.Deleted && this.GetEntityPageTracking(addedEntity).HasValue && !this.Contains(addedEntity))
                {
                    // This will only add to our Collection; it won't try to add
                    // to the source collection.
                    this.Add(addedEntity);
                }
            }
        }

        /// <summary>
        /// Track the page index that an entity is loaded or added onto
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> for which the page needs to be tracked.</param>
        /// <param name="trackedPageIndex">The page index for the <paramref name="entity"/> being tracked.</param>
        private void TrackEntityPage(Entity entity, int trackedPageIndex)
        {
            if (this._pageTracking == null)
            {
                this._pageTracking = new Dictionary<int, List<Entity>>();
            }

            if (!this._pageTracking.ContainsKey(trackedPageIndex))
            {
                this._pageTracking.Add(trackedPageIndex, new List<Entity>());
            }

            if (!this._pageTracking[trackedPageIndex].Contains(entity))
            {
                this._pageTracking[trackedPageIndex].Add(entity);
            }
        }

        /// <summary>
        /// Track items that were removed from the source entity set.  If an entity
        /// is being removed and it's also in our list, then we remove it.
        /// </summary>
        /// <param name="removedItems">The list of <see cref="Entity"/> items being removed from the collection.</param>
        private void TrackRemovedItems(IEnumerable<Entity> removedItems)
        {
            if (this._pageTracking == null)
            {
                // We aren't tracking any items, so don't add anything
                return;
            }

            foreach (Entity removedEntity in removedItems)
            {
                // If removing a new entity, we need to discard the page tracking for it
                if (removedEntity.EntityState == EntityState.New)
                {
                    int? pageIndex = this.GetEntityPageTracking(removedEntity);

                    if (pageIndex.HasValue)
                    {
                        this._pageTracking[pageIndex.Value].Remove(removedEntity);
                    }
                }

                if (this.Contains(removedEntity))
                {
                    // Remove from our Collection, but don't call Remove because that would
                    // also try to remove from the source collection.
                    this.RemoveItem(this.IndexOf(removedEntity));
                }
            }
        }

        /// <summary>
        /// Detaches from the source's INotifyPropertyChanged and INotifyCollectionChanged events.
        /// </summary>
        private void UnhookSourceCollectionChangeNotifications()
        {
            Debug.Assert(this._sourceEntitySet != null, "Unexpected _sourceEntityCollection == null");

            ((INotifyCollectionChanged)this._sourceEntitySet).CollectionChanged -= new NotifyCollectionChangedEventHandler(this.SourceEntitySet_CollectionChanged);
        }

        #endregion Private Methods
    }
}
