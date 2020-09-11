using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using OpenRiaServices.Client;
using System.Windows.Common;
using System.Windows.Data;

namespace OpenRiaServices.Controls
{
    /// <summary>
    /// An <see cref="ICollectionView"/>, <see cref="IEditableCollectionView"/>, and <see cref="IPagedCollectionView"/>
    /// implementation used by the <see cref="DomainDataSource"/>.
    /// </summary>
    /// <remarks>
    /// This class is responsible for managing currency, sorting, grouping, paging, add, edit, and remove transactions.
    /// <para>
    /// An <see cref="IPagedEntityList"/> is provided that supplies the entities to be exposed, and <see cref="PagedEntityCollectionView"/>
    /// will apply sorting and grouping on top of that list of entities, so that the entities are returned from this view in the
    /// correct order.
    /// </para>
    /// </remarks>
    /// <typeparam name="TEntity">The type of <see cref="Entity"/> contained in the view.</typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "WPF Compatability")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "WPF Compatibility for naming")]
    internal class PagedEntityCollectionView<TEntity> : PagedEntityCollectionView, IPagedCollectionView, IEnumerable<TEntity>
        where TEntity : Entity
    {
        #region All Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PagedEntityCollectionView"/> class, wrapping around
        /// the provided <see cref="IEnumerable"/> source.
        /// </summary>
        /// <remarks>
        /// By default, all <see cref="EntitySetOperations"/> are supported so that design-time support is fully functional.
        /// </remarks>
        /// <param name="source">The enumerable source that will be used for this view.</param>
        public PagedEntityCollectionView(IEnumerable<TEntity> source)
            : base(source, () => { }, EntitySetOperations.All)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PagedEntityCollectionView"/> class.
        /// </summary>
        /// <param name="source">The <see cref="IPagedEntityList"/> to use as the source of this view.</param>
        /// <param name="refreshCallback">The method to call when a <see cref="ICollectionView.Refresh"/> is triggered.</param>
        /// <param name="supportedOperations">The operations supported by the source.</param>
        public PagedEntityCollectionView(IPagedEntityList source, Action refreshCallback, EntitySetOperations supportedOperations)
            : base(source, refreshCallback, supportedOperations)
        {
        }

        #endregion

        #region IEnumerable<T> Members

        /// <summary>
        /// Gets the enumerator for this <see cref="IEnumerable"/>.
        /// </summary>
        /// <returns>An enumerator for the source enumerable.</returns>
        IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator()
        {
            return this.OfType<TEntity>().GetEnumerator();
        }

        #endregion
    }

    /// <summary>
    /// An <see cref="ICollectionView"/>, <see cref="IEditableCollectionView"/>, and <see cref="IPagedCollectionView"/>
    /// implementation used by the <see cref="DomainDataSource"/>.
    /// </summary>
    /// <remarks>
    /// This class is responsible for managing currency, sorting, grouping, paging, add, edit, and remove transactions.
    /// <para>
    /// An <see cref="IPagedEntityList"/> is provided that supplies the entities to be exposed, and <see cref="PagedEntityCollectionView"/>
    /// will apply sorting and grouping on top of that list of entities, so that the entities are returned from this view in the
    /// correct order.
    /// </para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "WPF Compatability")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "WPF Compatibility for naming")]
    internal class PagedEntityCollectionView : EntityCollectionView, IPagedCollectionView
    {
        #region Private Fields

        /// <summary>
        /// Value that we cache for the PageIndex if we are in a DeferRefresh,
        /// and the user has attempted to move to a different page.
        /// </summary>
        private int _cachedPageIndex = -1;

        /// <summary>
        /// Value that we cache for the PageSize if we are in a DeferRefresh,
        /// and the user has attempted to change the PageSize.
        /// </summary>
        private int _cachedPageSize;

        /// <summary>
        /// Private accessor for <see cref="CanAdd"/>.
        /// </summary>
        private bool _canAdd = false;

        /// <summary>
        /// Private accessor for <see cref="CanChangePage"/>.
        /// </summary>
        private bool _canChangePage = true;

        /// <summary>
        /// Private accessor for <see cref="CanLoad"/>, which
        /// the <see cref="DomainDataSource"/> will keep in sync
        /// with its own <see cref="DomainDataSource.CanLoad"/> property.
        /// </summary>
        private bool _canLoad = true;

        /// <summary>
        /// Private accessor for the <see cref="CollectionViewFlags"/>.
        /// </summary>
        private CollectionViewFlags _flags = CollectionViewFlags.ShouldProcessCollectionChanged;

        /// <summary>
        /// Private accessor for the Grouping data.
        /// </summary>
        private readonly CollectionViewGroupRoot _group;

        /// <summary>
        /// Private accessor for the <see cref="InternalList"/>.
        /// </summary>
        private List<Entity> _internalList;

        /// <summary>
        /// Keeps track of whether groups have been applied to the
        /// collection already or not. Note that this can still be set
        /// to false even though we specify a GroupDescription, as the 
        /// collection may not have gone through the PrepareGroups function.
        /// </summary>
        private bool _isGrouping;

        /// <summary>
        /// Represents the known number of items in the source collection
        /// that verify the potential filter.
        /// </summary>
        private int _itemCount;

        /// <summary>
        /// Private accessor for the <see cref="PageIndex"/>.
        /// </summary>
        private int _pageIndex = -1;

        /// <summary>
        /// The number of pages in the view.
        /// </summary>
        private int _pageCount;

        /// <summary>
        /// The size of the pages in the view.
        /// </summary>
        private int _pageSize;

        /// <summary>
        /// Private accessor for the entity list behind this view.
        /// </summary>
        private readonly IEnumerable _source;

        /// <summary>
        /// The callback action to invoked when a <see cref="Refresh"/> is triggered,
        /// either directly or through a <see cref="DeferRefresh"/> disposal.
        /// </summary>
        private readonly Action _refreshCallback;

        /// <summary>
        /// Private accessor for the <see cref="SortDescriptions"/> collection.
        /// </summary>
        private SortDescriptionCollection _sortDescriptions;

        /// <summary>
        /// Private accessor for <see cref="TotalItemCount"/>. Represents the total
        /// number of items in the source collection, or -1 if the total number is unknown.
        /// </summary>
        private int _totalItemCount;

        #endregion Private Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PagedEntityCollectionView"/> class, wrapping around
        /// the provided <see cref="IEnumerable"/> source.
        /// </summary>
        /// <remarks>
        /// By default, all <see cref="EntitySetOperations"/> are supported so that design-time support is fully functional.
        /// </remarks>
        /// <param name="source">The enumerable source that will be used for this view.</param>
        /// <param name="refreshCallback">The method to call when a <see cref="Refresh"/> is triggered.</param>
        /// <param name="supportedOperations">The operations supported for the source.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors",
            Justification = "This type is never exposed publicly.")]
        public PagedEntityCollectionView(IEnumerable source, Action refreshCallback, EntitySetOperations supportedOperations)
            : base(source, supportedOperations)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (refreshCallback == null)
            {
                throw new ArgumentNullException("refreshCallback");
            }

            this._source = source;

            if (this.SourcePagedEntityList != null)
            {
                this.SourcePagedEntityList.PropertyChanged += new PropertyChangedEventHandler(this.SourceCollection_PropertyChanged);
                this.SourcePagedEntityList.PageChanged += (sender, args) => this.CompletePageMove(this.SourcePagedEntityList.PageIndex);

                this.ItemCount = this.SourcePagedEntityList.ItemCount;
                this.TotalItemCount = this.SourcePagedEntityList.TotalItemCount;
            }

            this._refreshCallback = refreshCallback;

            this._group = new CollectionViewGroupRoot(this, false);
            this._group.GroupDescriptionChanged += new EventHandler(this.OnGroupDescriptionChanged);
            this._group.GroupDescriptions.CollectionChanged += new NotifyCollectionChangedEventHandler(this.OnGroupByChanged);

            this.CopySourceToInternalList();
            this.CalculateAllCalculatedProperties();

            // Set flag for whether the collection is empty
            this.SetFlag(CollectionViewFlags.CachedIsEmpty, this.Count == 0);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PagedEntityCollectionView"/> class.
        /// </summary>
        /// <param name="source">The <see cref="IPagedEntityList"/> to use as the source of this view.</param>
        /// <param name="refreshCallback">The method to call when a <see cref="Refresh"/> is triggered.</param>
        public PagedEntityCollectionView(IPagedEntityList source, Action refreshCallback)
            : this(source, refreshCallback, EntitySetOperations.None)
        {
        }

        #endregion Constructors

        #region Events

        /// <summary>
        /// Raised when a page index change completed
        /// </summary>
        public event EventHandler<EventArgs> PageChanged;

        /// <summary>
        /// Raised when a page index change is requested
        /// </summary>
        public event EventHandler<PageChangingEventArgs> PageChanging;

        #endregion Events

        #region Public Properties

        /// <summary>
        /// Gets a value indicating whether a new item can be added to the view using
        /// <see cref="Add"/>.
        /// </summary>
        public bool CanAdd
        {
            get
            {
                return this._canAdd;
            }
            private set
            {
                if (this._canAdd != value)
                {
                    this._canAdd = value;
                    this.RaisePropertyChanged(nameof(CanAdd));
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="PageIndex"/> value is allowed to change.
        /// </summary>
        /// <remarks>
        /// This is <c>false</c> when <see cref="CanLoad"/> is false or when <see cref="PageSize"/> is 0.
        /// </remarks>
        public bool CanChangePage
        {
            get
            {
                return this._canChangePage;
            }
            private set
            {
                if (this._canChangePage != value)
                {
                    this._canChangePage = value;
                    this.RaisePropertyChanged(nameof(CanChangePage));
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this view supports grouping.
        /// </summary>
        /// <remarks>
        /// This property value doesn't respect <see cref="CanLoad"/> because
        /// <see cref="ICollectionView.CanGroup"/> must be true at the time a
        /// <see cref="NotifyCollectionChangedAction.Reset"/> event is fired
        /// in order for existing groups to be respected.
        /// </remarks>
        public override bool CanGroup
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether changes to the <see cref="SortDescriptions"/> are presently
        /// allowed with this <see cref="ICollectionView"/>.
        /// </summary>
        /// <remarks>
        /// This will return <c>true</c> whenever the underlying <see cref="DomainDataSource"/>'s
        /// <see cref="DomainDataSource.CanLoad"/> property is <c>true</c>.
        /// </remarks>
        public override bool CanSort
        {
            get { return this.CanLoad; }
        }

        /// <summary>
        /// Gets the number of records in the view after 
        /// filtering, sorting, and paging.
        /// </summary>
        public int Count
        {
            get
            {
                this.VerifyRefreshNotDeferred();

                // if we're still being initialized, just return 0
                if (this._source == null)
                {
                    return 0;
                }

                // if we have paging
                if (this.PageSize > 0)
                {
                    // if we have not loaded the new data from a paging operation yet
                    if (this.SourcePagedEntityList.IsPagingOperationPending)
                    {
                        return 0;
                    }

                    if (this.IsGrouping)
                    {
                        return this._group.ItemCount;
                    }
                    else
                    {
                        return this.InternalCount;
                    }
                }
                else
                {
                    if (this.IsGrouping)
                    {
                        return this._group.ItemCount;
                    }
                    else
                    {
                        return this.InternalCount;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the description of grouping, indexed by level.
        /// </summary>
        public override ObservableCollection<GroupDescription> GroupDescriptions
        {
            get
            {
                return this._group.GroupDescriptions;
            }
        }

        /// <summary>
        /// Gets the top-level groups, constructed according to the descriptions
        /// given in GroupDescriptions.
        /// </summary>
        public override ReadOnlyObservableCollection<object> Groups
        {
            get
            {
                if (!this.IsGrouping)
                {
                    return null;
                }

                return this._group.Items;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the resulting (filtered) view is empty.
        /// </summary>
        public override bool IsEmpty
        {
            get
            {
                return this.InternalCount == 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether a page index change is in process or not.
        /// </summary>
        public bool IsPageChanging
        {
            get
            {
                return this.CheckFlag(CollectionViewFlags.IsPageChanging);
            }

            internal set
            {
                if (this.CheckFlag(CollectionViewFlags.IsPageChanging) != value)
                {
                    this.SetFlag(CollectionViewFlags.IsPageChanging, value);
                    this.RaisePropertyChanged(nameof(IsPageChanging));
                }
            }
        }

        /// <summary>
        /// Gets a count of items known to be in the source collection after filtering
        /// is applied, but before paging.
        /// </summary>
        /// <remarks>
        /// Value is based on the number of items that have been loaded and what pages
        /// those items were loaded into.  If <see cref="PageIndex"/> of 0 or 1 were never
        /// loaded, but <see cref="PageIndex"/> of 2 was loaded successfully, then
        /// we can assume that page indexes of 0 and 1 exist and are full.
        /// </remarks>
        /// <example>
        /// While on <see cref="PageIndex"/> of 2 (the 3rd page), with <see cref="PageSize"/>
        /// of 5, and the page having 5 items, the return value will be at least 15.
        /// <para>
        /// If the <see cref="PageIndex"/> has been greater than 2 previously, we know that items
        /// exist beyond the 3rd page, so the return value could be greater than 15.
        /// </para>
        /// </example>
        /// <seealso cref="TotalItemCount"/>
        public int ItemCount
        {
            get
            {
                return this._itemCount;
            }

            private set
            {
                if (this._itemCount != value)
                {
                    this._itemCount = value;
                    this.RaisePropertyChanged(nameof(ItemCount));
                }
            }
        }

        /// <summary>
        /// Gets the current page we are on. (zero based)
        /// </summary>
        public int PageIndex
        {
            get
            {
                return this._pageIndex;
            }
            private set
            {
                if (this._pageIndex != value)
                {
                    this._pageIndex = value;
                    this.RaisePropertyChanged(nameof(PageIndex));
                }
            }
        }

        /// <summary>
        /// Gets or sets the number of items to display on a page. If the
        /// PageSize = 0, then we are not paging, and will display all items
        /// in the collection. Otherwise, we will have separate pages for 
        /// the items to display.
        /// </summary>
        public int PageSize
        {
            get
            {
                return this._pageSize;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(EntityCollectionViewResources.InvalidPageSize);
                }

                if (this.SourcePagedEntityList != null)
                {
                    // if the Refresh is currently deferred, cache the desired PageSize
                    // and set the flag so that once the defer is over, we can then
                    // update the PageSize.
                    if (this.IsRefreshDeferred)
                    {
                        // set cached value and flag so that we update the PageSize on EndDefer
                        this._cachedPageSize = value;
                        this.SetFlag(CollectionViewFlags.IsUpdatePageSizeDeferred, true);
                        return;
                    }

                    // to see whether or not to fire an OnPropertyChanged
                    int oldCount = this.Count;

                    if (this.SourcePagedEntityList.PageSize != value)
                    {
                        // Remember current currency values for upcoming OnPropertyChanged notifications
                        Entity oldCurrentItem = (Entity)this.CurrentItem;
                        int oldCurrentPosition = this.CurrentPosition;
                        bool oldIsCurrentAfterLast = this.IsCurrentAfterLast;
                        bool oldIsCurrentBeforeFirst = this.IsCurrentBeforeFirst;

                        // Check if there is a current edited or new item so changes can be committed first.
                        if (this.IsAddingNew || this.IsEditingItem)
                        {
                            // Check with the ICollectionView.CurrentChanging listeners if it's OK to
                            // change the currency. If not, then we can't fire the event to allow them to
                            // commit their changes. So, we will not be able to change the PageSize.
                            if (!this.OkToChangeCurrent())
                            {
                                throw new InvalidOperationException(EntityCollectionViewResources.ChangingPageSizeNotAllowedDuringAddOrEdit);
                            }

                            // Currently CommitNew()/CommitEdit()/CancelNew()/CancelEdit() can't handle committing or 
                            // cancelling an item that is no longer on the current page. That's acceptable and means that
                            // the potential this._newItem or this._editItem needs to be committed before this PageSize change.
                            // The reason why we temporarily reset currency here is to give a chance to the bound
                            // controls to commit or cancel their potential edits/addition. The DataForm calls ForceEndEdit()
                            // for example as a result of changing currency.
                            this.SetCurrentToPosition(-1);
                            this.RaiseCurrentChanged(oldCurrentItem, oldCurrentPosition, oldIsCurrentBeforeFirst, oldIsCurrentAfterLast);

                            // If the bound controls did not successfully end their potential item editing/addition, we 
                            // need to throw an exception to show that the PageSize change failed. 
                            if (this.IsAddingNew || this.IsEditingItem)
                            {
                                throw new InvalidOperationException(EntityCollectionViewResources.ChangingPageSizeNotAllowedDuringAddOrEdit);
                            }
                        }

                        this.SourcePagedEntityList.PageSize = value;
                        this.RaisePropertyChanged(nameof(PageSize));
                        this.CalculatePageCount();
                        this.CalculateCanChangePage();
                        this.CalculateCanAdd();
                        this.CalculateCanAddNew();

                        if (this.SourcePagedEntityList.PageSize == 0)
                        {
                            // update the groups for the current page
                            this.PrepareGroups();

                            // if we are not paging
                            this.MoveToPage(-1);
                        }
                        else if (this.SourcePagedEntityList.PageIndex > 0)
                        {
                            if (!this.CheckFlag(CollectionViewFlags.IsMoveToPageDeferred))
                            {
                                // because of asynchronous loading, when we switch page size,
                                // we will not be able to get to any other page than the first, 
                                // as we are not guaranteed to have all the items loaded yet.
                                this.MoveToFirstPage();
                            }
                        }
                        else if (this.IsGrouping)
                        {
                            // update the groups for the current page
                            this.PrepareGroups();
                        }

                        // if the count has changed
                        if (this.Count != oldCount)
                        {
                            this.RaisePropertyChanged(nameof(Count));
                        }

                        // reset currency values
                        this.ResetCurrencyValues(oldCurrentItem, oldIsCurrentBeforeFirst, oldIsCurrentAfterLast);

                        // send a notification that our collection has been updated
                        this.OnCollectionChanged(
                            new NotifyCollectionChangedEventArgs(
                                NotifyCollectionChangedAction.Reset));

                        // now raise currency changes at the end
                        this.RaiseCurrentChanged(oldCurrentItem, oldCurrentPosition, oldIsCurrentBeforeFirst, oldIsCurrentAfterLast);
                    }
                }

                if (this._pageSize != value)
                {
                    this._pageSize = value;
                    this.RaisePropertyChanged(nameof(PageSize));
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="SortDescription"/>s that are used to sort the collection.
        /// </summary>
        public override SortDescriptionCollection SortDescriptions
        {
            get
            {
                if (this._sortDescriptions == null)
                {
                    this.SetSortDescriptions(new SortDescriptionCollection());
                }

                return this._sortDescriptions;
            }
        }

        /// <summary>
        /// Gets the total number of items in the view before paging is applied,
        /// or <c>-1</c> if that total number is unknown.
        /// </summary>
        /// <remarks>
        /// The provider of the source collection is in charge of setting this value. In those cases, the value
        /// can be temporary only. That is, the provider can set TotalItemCount to a positive number or <c>-1</c>
        /// based on its latest information, but that may no longer reflect the reality at a later time, given
        /// the fact that the source may not be of a fixed length.
        /// </remarks>
        public int TotalItemCount
        {
            get
            {
                return this._totalItemCount;
            }

            private set
            {
                if (this._totalItemCount != value)
                {
                    this._totalItemCount = value;
                    this.RaisePropertyChanged(nameof(TotalItemCount));
                    this.CalculatePageCount();
                }
            }
        }

        #endregion Public Properties

        #region Internal Properties

        /// <summary>
        /// Gets or sets a value indicating whether or not loads can currently be
        /// performed by the underlying <see cref="DomainDataSource"/>.
        /// </summary>
        /// <remarks>
        /// This property is replicated on the <see cref="PagedEntityCollectionView"/> because
        /// we don't have a mechanism for listening to change notifications on the underlying
        /// <see cref="DomainDataSource"/>.<see cref="DomainDataSource.CanLoad"/> property.
        /// Therefore, we expose a property on the <see cref="PagedEntityCollectionView"/>
        /// so that the <see cref="DomainDataSource"/> can update it directly.
        /// </remarks>
        internal bool CanLoad
        {
            get
            {
                return this._canLoad;
            }
            set
            {
                if (this._canLoad != value)
                {
                    this._canLoad = value;
                    this.CalculateCanChangePage();
                }
            }
        }

        /// <summary>
        /// Whether or not a <see cref="Refresh"/> is being queued up
        /// </summary>
        internal bool IsRefreshing { get; private set; }

        #endregion Internal Properties

        #region Private Properties

        /// <summary>
        /// Gets a value indicating whether or not an entity can be created
        /// based on current state.
        /// </summary>
        private bool CanCreateEntity
        {
            get
            {
                return this.SourcePagedEntityList != null
                    && this.SourcePagedEntityList.EntityType != null
                    && !this.SourcePagedEntityList.EntityType.IsAbstract;
            }
        }

        /// <summary>
        /// Gets the private count without taking paging or
        /// placeholders into account
        /// </summary>
        private int InternalCount
        {
            get { return this.InternalList.Count; }
        }

        /// <summary>
        /// Gets the InternalList
        /// </summary>
        private List<Entity> InternalList
        {
            get { return this._internalList; }
        }

        /// <summary>
        /// Gets a value indicating whether or not we have grouping 
        /// taking place in this collection.
        /// </summary>
        private bool IsGrouping
        {
            get { return this._isGrouping; }
        }

        /// <summary>
        /// Gets the count of the pages in this view.
        /// </summary>
        /// <remarks>
        /// When <see cref="PageSize"/> is 0, the <see cref="PageCount"/> will also be 0.
        /// </remarks>
        public int PageCount
        {
            get
            {
                return this._pageCount;
            }

            private set
            {
                if (this._pageCount != value)
                {
                    this._pageCount = value;
                    this.RaisePropertyChanged(nameof(PageCount));
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="EntitySet"/> that serves as the backing store for our
        /// <see cref="IPagedEntityList"/>.
        /// </summary>
        private EntitySet SourceEntitySet
        {
            get { return this.SourcePagedEntityList != null ? this.SourcePagedEntityList.BackingEntitySet : null; }
        }

        /// <summary>
        /// Gets the source collection as an <see cref="IPagedEntityList"/>
        /// </summary>
        private IPagedEntityList SourcePagedEntityList
        {
            get { return this._source as IPagedEntityList; }
        }

        /// <summary>
        /// Gets a value indicating whether a private copy of the data is needed for sorting or paging.
        /// </summary>
        private bool UsesLocalArray
        {
            get
            {
                return this.SortDescriptions.Count > 0 || this.PageSize > 0 || this.GroupDescriptions.Count > 0;
            }
        }

        #endregion Private Properties

        #region Indexers

        /// <summary>
        /// Return the item at the specified index
        /// </summary>
        /// <param name="index">Index of the item we want to retrieve</param>
        /// <returns>The item at the specified index</returns>
        public object this[int index]
        {
            get { return this.GetItemAt(index); }
        }

        #endregion Indexers

        #region Methods

        /// <summary>
        /// Adds a new entity to the underlying collection.
        /// </summary>
        /// <param name="item">The entity to add.</param>
        public void Add(object item)
        {
            Entity entity = (Entity)item;

            if (!this.CanAdd)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.NotSupported, "Add"));
            }

            try
            {
                // temporarily disable the CollectionChanged event
                // handler so filtering, sorting, or grouping
                // doesn't get applied yet
                this.SetFlag(CollectionViewFlags.ShouldProcessCollectionChanged, false);
                this.SourcePagedEntityList.Add(entity);
                this.SourceEntitySet.Add(entity);
            }
            finally
            {
                this.SetFlag(CollectionViewFlags.ShouldProcessCollectionChanged, true);
            }

            // add the new item to the end of the internal list, but processing sorting/grouping for repositioning
            int addIndex = this.Count;
            this.Insert(entity, ref addIndex);

            if (this.IsGrouping)
            {
                this.InsertGroup(item, addIndex);
            }

            this.OnCollectionChanged(
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add,
                    item,
                    addIndex));

            this.MoveCurrentTo(entity);
        }

        /// <summary>
        /// When an entity is committed as an edit, we need to ensure the item's
        /// position is correct based on grouping and sorting.  Additionally,
        /// we will ensure the item has been added to the internal list.
        /// </summary>
        /// <param name="editItem">The entity that was committed.</param>
        protected override void CommittedEdit(Entity editItem)
        {
            // If there are validation errors on the committed item, the DataForm will throw an exception on
            // currency changes. To avoid this situation, we first give it a chance to cancel the change. Our
            // expectation is that the item will have to be successfully edited before we attempt to update
            // its position.
            if (this.OkToChangeCurrent())
            {
                // Update the item's position based on grouping and sorting
                this.UpdateItemPosition(editItem);

                if (!this.UsesLocalArray && !this.Contains(editItem))
                {
                    // if the item did not belong to the collection, add it
                    this.InternalList.Add(editItem);
                }
            }
        }

        /// <summary>
        /// When an entity is committed as new, we need to ensure the item's
        /// position is correct based on grouping and sorting.
        /// </summary>
        /// <param name="newItem">The entity that was committed.</param>
        protected override void CommittedNew(Entity newItem)
        {
            // If there are validation errors on the committed item, the DataForm will throw an exception on
            // currency changes. To avoid this situation, we first give it a chance to cancel the change. Our
            // expectation is that the item will have to be successfully edited before we attempt to update
            // its position.
            if (this.OkToChangeCurrent())
            {
                // Update the item's position based on grouping and sorting
                this.UpdateItemPosition(newItem);
            }
        }

        /// <summary>
        /// Enter a <see cref="DeferRefresh"/> cycle.
        /// Defer cycles are used to coalesce changes to the <see cref="ICollectionView"/>.
        /// </summary>
        /// <returns><see cref="IDisposable"/> used to notify that we no longer need to defer, when we dispose.</returns>
        public override IDisposable DeferRefresh()
        {
            if (!this.CanLoad)
            {
                throw new InvalidOperationException(DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Refresh);
            }

            return base.DeferRefresh();
        }

        /// <summary>
        /// Override the enumerator to enumerate over the internal list
        /// which is sorted based on grouping and sorting.
        /// </summary>
        /// <returns>An enumerator that will enumerate over the sorted data.</returns>
        public override IEnumerator GetEnumerator()
        {
            this.VerifyRefreshNotDeferred();

            // if we are paging but don't have a page index yet
            if (this.PageSize > 0 && this.PageIndex < 0)
            {
                return new List<Entity>().GetEnumerator();
            }

            return this.InternalList.GetEnumerator();
        }

        /// <summary>
        /// Retrieve item at the given zero-based index in this EntityCollectionView, after the source collection
        /// is filtered, sorted, and paged.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"> is thrown if <paramref name="index"/> is out of range.
        /// </exception>
        /// <param name="index">Index of the item we want to retrieve.</param>
        /// <returns>The <see cref="Entity"/> at specified index.</returns>
        public Entity GetItemAt(int index)
        {
            this.VerifyRefreshNotDeferred();

            // for indicies larger than the count
            if (index >= this.Count || index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            if (this.IsGrouping)
            {
                return (Entity)this._group.LeafAt(index);
            }

            return this.InternalItemAt(index);
        }

        /// <summary> 
        /// Return the index where the given <paramref name="item"/> appears, or <c>-1</c> if doesn't appear.
        /// </summary>
        /// <param name="item"><see cref="Entity"/> we are searching for.</param>
        /// <returns>Index of specified item.</returns>
        /// <exception cref="InvalidCastException">When the <paramref name="item"/> is not an <see cref="Entity"/>.</exception>
        public int IndexOf(object item)
        {
            Entity entity = (Entity)item;

            this.VerifyRefreshNotDeferred();

            if (this.IsGrouping)
            {
                return this._group.LeafIndexOf(entity);
            }

            return this.InternalIndexOf(entity);
        }

        /// <summary>
        /// Moves to the first page.
        /// </summary>
        /// <returns>Whether or not the move was successful.</returns>
        public bool MoveToFirstPage()
        {
            return this.MoveToPage(0);
        }

        /// <summary>
        /// Moves to the last page.
        /// The move is only attempted when TotalItemCount is known.
        /// </summary>
        /// <returns>Whether or not the move was successful.</returns>
        public bool MoveToLastPage()
        {
            if (this.TotalItemCount != -1 && this.PageSize > 0)
            {
                return this.MoveToPage(this.PageCount - 1);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Moves to the page after the current page we are on.
        /// </summary>
        /// <returns>Whether or not the move was successful.</returns>
        public bool MoveToNextPage()
        {
            return this.MoveToPage(this._pageIndex + 1);
        }

        /// <summary>
        /// Moves to the page before the current page we are on.
        /// </summary>
        /// <returns>Whether or not the move was successful.</returns>
        public bool MoveToPreviousPage()
        {
            return this.MoveToPage(this._pageIndex - 1);
        }

        /// <summary>
        /// Requests a page move to page <paramref name="pageIndex"/>.
        /// </summary>
        /// <param name="pageIndex">Index of the target page</param>
        /// <returns>Whether or not the move was successfully initiated.</returns>
        public bool MoveToPage(int pageIndex)
        {
            // Boundary checks for negative pageIndex
            if (pageIndex < -1)
            {
                return false;
            }

            // Check for no-op
            if (this.PageIndex == pageIndex)
            {
                return false;
            }

            // if the Refresh is deferred, cache the requested PageIndex so that we
            // can move to the desired page when EndDefer is called.
            if (this.IsRefreshDeferred)
            {
                // set cached value and flag so that we move to the page when a DeferRefresh is disposed
                this._cachedPageIndex = pageIndex;
                this.SetFlag(CollectionViewFlags.IsMoveToPageDeferred, true);
                return false;
            }

            if (!this.CanLoad)
            {
                throw new InvalidOperationException(DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Paging);
            }

            // If we are in the middle of adding/editing an item, we need to 
            // commit changes before moving away from the current page
            if (!this.CanChangePage && pageIndex >= 0)
            {
                return false;
            }

            // check for invalid pageIndex
            if (pageIndex == -1 && this.PageSize > 0)
            {
                return false;
            }

            if (this.RaisePageChanging(pageIndex) && pageIndex != -1)
            {
                // Page move was cancelled. Abort the move, but only if the target index isn't -1.
                return false;
            }

            this.IsPageChanging = true;

            if (pageIndex != -1)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() => this.RequestPageMove(pageIndex));
            }
            else
            {
                this.CompletePageMove(pageIndex);
            }

            return true;
        }

        /// <summary>
        /// Re-create the view, using any SortDescriptions and/or Filters.
        /// </summary>
        public override void Refresh()
        {
            IEditableCollectionView ecv = this as IEditableCollectionView;
            if (ecv != null && (ecv.IsAddingNew || ecv.IsEditingItem))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.OperationNotAllowedDuringAddOrEdit, "Refresh"));
            }

            if (this.IsRefreshDeferred)
            {
                throw new InvalidOperationException(EntityCollectionViewResources.RefreshWithinDeferRefresh);
            }

            int? moveToPage = null;

            try
            {
                this.IsRefreshing = true;

                if (this.CheckFlag(CollectionViewFlags.IsUpdatePageSizeDeferred))
                {
                    this.SetFlag(CollectionViewFlags.IsUpdatePageSizeDeferred, false);
                    this.PageSize = this._cachedPageSize;
                }

                if (this.CheckFlag(CollectionViewFlags.IsMoveToPageDeferred))
                {
                    this.SetFlag(CollectionViewFlags.IsMoveToPageDeferred, false);
                    moveToPage = this._cachedPageIndex;
                    this._cachedPageIndex = -1;
                }
            }
            finally
            {
                this.IsRefreshing = false;
            }

            if (moveToPage.HasValue)
            {
                this.RequestPageMove(moveToPage.Value);
            }
            else
            {
                this._refreshCallback();
            }
        }

        /// <summary>
        /// The implementation used for the <see cref="EntityCollectionView"/>'s
        /// <see cref="EntityCollectionView.Remove"/> method.
        /// </summary>
        /// <remarks>
        /// Find the entity's index and remove it.
        /// </remarks>
        /// <param name="item">The entity to remove.</param>
        private void RemoveItem(Entity item)
        {
            int index = this.IndexOf(item);
            this.RemoveCore(item, index);
        }

        /// <summary>
        /// The implementation used for the <see cref="EntityCollectionView"/>'s
        /// <see cref="EntityCollectionView.Remove"/> method.
        /// </summary>
        /// <remarks>
        /// Find the entity by index and remove it.
        /// </remarks>
        /// <param name="index">The index of the entity to remove.</param>
        private void RemoveIndex(int index)
        {
            Entity item = this.GetItemAt(index);
            this.RemoveCore(item, index);
        }

        /// <summary>
        /// Remove an entity given its instance and index.
        /// </summary>
        /// <param name="item">The entity to remove.</param>
        /// <param name="index">The index of the entity to remove.</param>
        private void RemoveCore(Entity item, int index)
        {
            this.VerifyRefreshNotDeferred();

            try
            {
                // temporarily disable the CollectionChanged event
                // handler so filtering, sorting, or grouping
                // doesn't get applied yet
                this.SetFlag(CollectionViewFlags.ShouldProcessCollectionChanged, false);
                this.SourcePagedEntityList.Remove((Entity)item);
                this.SourceEntitySet.Remove(item);
            }
            finally
            {
                this.SetFlag(CollectionViewFlags.ShouldProcessCollectionChanged, true);
            }

            // remove the item from the internal list
            this.InternalList.Remove(item);

            if (this.IsGrouping)
            {
                this._group.RemoveFromSubgroups(item);
            }

            object oldCurrentItem = this.CurrentItem;
            int oldCurrentPosition = this.CurrentPosition;
            bool oldIsCurrentAfterLast = this.IsCurrentAfterLast;
            bool oldIsCurrentBeforeFirst = this.IsCurrentBeforeFirst;

            this.EnsureValidCurrency();

            // fire remove notification
            this.OnCollectionChanged(
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove,
                    item,
                    index));

            this.RaiseCurrentChanged(oldCurrentItem, oldCurrentPosition, oldIsCurrentBeforeFirst, oldIsCurrentAfterLast);
        }

        #endregion Methods

        #region Static Methods

        /// <summary>
        /// Helper for SortList to handle nested properties (e.g. Address.Street)
        /// </summary>
        /// <param name="item">parent object</param>
        /// <param name="propertyPath">property names path</param>
        /// <param name="propertyType">property type that we want to check for</param>
        /// <returns>child object</returns>
        private static object InvokePath(object item, string propertyPath, Type propertyType)
        {
            Exception exception;
            object propertyValue = TypeHelper.GetNestedPropertyValue(item, propertyPath, propertyType, out exception);
            if (exception != null)
            {
                throw exception;
            }
            return propertyValue;
        }

        #endregion Static Methods

        #region Protected and Private Methods

        /// <summary>
        /// Override the default delegates that get used by the view so that efficient, specific
        /// implementations are used.
        /// </summary>
        protected override void ApplyDefaultDelegates()
        {
            this.Delegates.Contains = item => this.IndexOf(item) >= 0;
            this.Delegates.IndexOf = this.IndexOf;
            this.Delegates.GetItemAt = this.GetItemAt;
            this.Delegates.Count = () => this.Count;
            this.Delegates.Add = this.Add;
            this.Delegates.Remove = this.RemoveItem;
            this.Delegates.RemoveAt = this.RemoveIndex;
            this.Delegates.CreateInstance = this.CreateEntity;
        }

        /// <summary>
        /// Calculates all of the properties that are calculated through CalculateX methods.
        /// </summary>
        protected override void CalculateAllCalculatedProperties()
        {
            this.CalculateCanChangePage();
            this.CalculatePageCount();
            this.CalculateCanAdd();
            this.CalculateSupportedOperations();

            // Execute the base calculations last since some of
            // them depend on the SupportedOperations property
            base.CalculateAllCalculatedProperties();
        }

        /// <summary>
        /// Calculates <see cref="EntityCollectionView.CanAddNew"/> using <see cref="EntityCollectionView.IsOperationSupported"/>,
        /// <see cref="PageSize"/>, and <see cref="IPagedEntityList.IsPagingOperationPending"/>.
        /// </summary>
        protected override void CalculateCanAddNew()
        {
            this.CanAddNew =
                this.IsOperationSupported(EntitySetOperations.Add) &&
                ((this.PageSize == 0) || !this.SourcePagedEntityList.IsPagingOperationPending);
        }

        /// <summary>
        /// Calculates <see cref="CanAdd"/> using <see cref="PageSize"/> and <see cref="IPagedEntityList.IsPagingOperationPending"/>
        /// and by determining whether the <see cref="EntitySet"/> supports <see cref="EntitySetOperations.Add"/>.
        /// </summary>
        private void CalculateCanAdd()
        {
            this.CanAdd =
                ((this.SourceEntitySet != null) && this.SourceEntitySet.CanAdd) &&
                ((this.PageSize == 0) || !this.SourcePagedEntityList.IsPagingOperationPending);
        }

        /// <summary>
        /// Calculates <see cref="CanChangePage"/> from the <see cref="IEditableCollectionView.IsAddingNew"/>
        /// and <see cref="IEditableCollectionView.IsEditingItem"/> properties as well as the <see cref="CanLoad"/> property.
        /// </summary>
        private void CalculateCanChangePage()
        {
            if (this._source == null)
            {
                this.CanChangePage = false;
            }
            else
            {
                this.CanChangePage = !this.IsAddingNew && !this.IsEditingItem && this.CanLoad && this.PageSize > 0;
            }
        }


        /// <summary>
        /// Calculates <see cref="PageCount"/> from the <see cref="PageSize"/> and <see cref="TotalItemCount"/> properties.
        /// </summary>
        private void CalculatePageCount()
        {
            int pageCount = 0;

            if (this._source != null)
            {
                if (this.PageSize != 0)
                {
                    pageCount = Math.Max(1, PagingHelper.CalculatePageCount(this.TotalItemCount, this.PageSize));
                }
            }

            this.PageCount = pageCount;
        }

        /// <summary>
        /// Calculates what operations are supported based on the <see cref="SourceEntitySet"/>.
        /// </summary>
        private void CalculateSupportedOperations()
        {
            EntitySetOperations supported = this.SupportedOperations;

            if (this.SourceEntitySet != null)
            {
                // For Add, we must also ensure that we can create entities
                if (this.SourceEntitySet.CanAdd && this.CanCreateEntity)
                {
                    supported |= EntitySetOperations.Add;
                }

                if (this.SourceEntitySet.CanEdit)
                {
                    supported |= EntitySetOperations.Edit;
                }

                if (this.SourceEntitySet.CanRemove)
                {
                    supported |= EntitySetOperations.Remove;
                }
            }

            this.SupportedOperations = supported;
        }

        /// <summary>
        /// Returns true if specified flag in flags is set.
        /// </summary>
        /// <param name="flags">Flag we are checking for</param>
        /// <returns>Whether the specified flag is set</returns>
        private bool CheckFlag(CollectionViewFlags flags)
        {
            return (this._flags & flags) != 0;
        }

        /// <summary>
        /// Called either when the page is local or when the paged source collection
        /// notified this EntityCollectionView of a page move completion.
        /// </summary>
        /// <param name="pageIndex">Final page index</param>
        private void CompletePageMove(int pageIndex)
        {
            // to see whether or not to fire an OnPropertyChanged
            object oldCurrentItem = this.CurrentItem;
            int oldCurrentPosition = this.CurrentPosition;
            bool oldIsCurrentAfterLast = this.IsCurrentAfterLast;
            bool oldIsCurrentBeforeFirst = this.IsCurrentBeforeFirst;

            if (this.PageIndex != pageIndex)
            {
                // update currency
                this.MoveCurrentToFirst();
                this.PageIndex = pageIndex;

                // update the groups
                if (this.IsGrouping && this.PageSize > 0)
                {
                    this.PrepareGroups();
                }
            }

            if (this.IsPageChanging)
            {
                this.IsPageChanging = false;
                this.RaisePageChanged();

                this.OnCollectionChanged(
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Reset));

                this.RaiseCurrentChanged(oldCurrentItem, oldCurrentPosition, oldIsCurrentBeforeFirst, oldIsCurrentAfterLast);
            }
        }

        /// <summary>
        /// Copy all items from the source collection to the internal list for processing.
        /// </summary>
        private void CopySourceToInternalList()
        {
            this._internalList = new List<Entity>();

            IEnumerator enumerator = this._source.GetEnumerator();

            while (enumerator.MoveNext())
            {
                this._internalList.Add((Entity)enumerator.Current);
            }
        }

        /// <summary>
        /// Creates an <see cref="Entity"/> using the <see cref="IPagedEntityList.EntityType"/>.
        /// </summary>
        /// <returns>A new <see cref="Entity"/> instance of the <see cref="IPagedEntityList.EntityType"/>.</returns>
        private Entity CreateEntity()
        {
            return (Entity)Activator.CreateInstance(this.SourcePagedEntityList.EntityType);
        }

        /// <summary>
        /// Notification that a <see cref="DeferRefresh"/> cycle has ended.
        /// </summary>
        protected override void DeferRefreshEnded()
        {
            // Always process the refresh regardless of whether or not
            // we've identified that a refresh is needed, so that the behavior
            // is consistent: disposing a DeferRefresh will always call the callback
            // thus invoking a load for the DomainDataSource.
            this.Refresh();
        }

        /// <summary>
        /// Insert a new group for the specified <paramref name="item"/>,
        /// with the item being added at the specified <paramref name="addIndex"/>.
        /// </summary>
        /// <param name="item">The item being inserted into this new group.</param>
        /// <param name="addIndex">The index at which this item is being inserted.</param>
        private void InsertGroup(object item, int addIndex)
        {
            object insertedBefore = null;
            if (addIndex < this.Count)
            {
                // Since we haven't added the new group yet, we don't need to
                // add one to the addIndex.  GetItemAt(addIndex) will return
                // the item we're being inserted before.
                insertedBefore = this.GetItemAt(addIndex);
            }

            this._group.AddToSubgroups(item, false /*loading*/, insertedBefore);
        }

        /// <summary>
        /// Return index of item in the internal list.
        /// </summary>
        /// <param name="item">The <see cref="Entity"/> we are checking</param>
        /// <returns>Integer value on where in the InternalList the object is located</returns>
        private int InternalIndexOf(Entity item)
        {
            return this.InternalList.IndexOf(item);
        }

        /// <summary>
        /// Return item at the given index in the internal list.
        /// </summary>
        /// <param name="index">The index we are checking</param>
        /// <returns>The <see cref="Entity"/> at the specified index</returns>
        private Entity InternalItemAt(int index)
        {
            if (index >= 0 && index < this.InternalList.Count)
            {
                return this.InternalList[index];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Notify listeners that this View has changed
        /// </summary>
        /// <remarks>
        /// CollectionViews (and sub-classes) should take their filter/sort/grouping/paging
        /// into account before calling this method to forward CollectionChanged events.
        /// </remarks>
        /// <param name="args">
        /// The NotifyCollectionChangedEventArgs to be passed to the EventHandler
        /// </param>
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            // Don't raise events when an item is added beyond the end of our page (when paging)
            if (args.Action != NotifyCollectionChangedAction.Add || this.PageSize == 0 || args.NewStartingIndex < this.Count)
            {
                base.OnCollectionChanged(args);

                // Bug 706239 - Work around the DataGrid bug 709185 by double-raising the Reset event whenever it's fired
                if (args.Action == NotifyCollectionChangedAction.Reset)
                {
                    base.OnCollectionChanged(args);
                }
            }

            // Collection changes change the count unless an item is being
            // replaced within the collection.
            if (args.Action != NotifyCollectionChangedAction.Replace)
            {
                this.RaisePropertyChanged(nameof(Count));
            }

            bool listIsEmpty = this.IsEmpty;
            if (listIsEmpty != this.CheckFlag(CollectionViewFlags.CachedIsEmpty))
            {
                this.SetFlag(CollectionViewFlags.CachedIsEmpty, listIsEmpty);
                this.RaisePropertyChanged(nameof(IsEmpty));
            }
        }

        /// <summary>
        /// GroupBy changed handler
        /// </summary>
        /// <param name="sender">CollectionViewGroup whose GroupBy has changed</param>
        /// <param name="e">Arguments for the NotifyCollectionChanged event</param>
        private void OnGroupByChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.IsAddingNew || this.IsEditingItem)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.OperationNotAllowedDuringAddOrEdit, "Grouping"));
            }
        }

        /// <summary>
        /// GroupDescription changed handler
        /// </summary>
        /// <param name="sender">CollectionViewGroup whose GroupDescription has changed</param>
        /// <param name="e">Arguments for the GroupDescriptionChanged event</param>
        private void OnGroupDescriptionChanged(object sender, EventArgs e)
        {
            if (this.IsAddingNew || this.IsEditingItem)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.OperationNotAllowedDuringAddOrEdit, "Grouping"));
            }
        }

        /// <summary>
        /// Use the GroupDescriptions to place items into their respective groups.
        /// </summary>
        private void PrepareGroups()
        {
            this._group.Clear();
            this._group.Initialize();

            this._group.IsDataInGroupOrder = true;

            // set to false so that we access internal collection items
            // instead of the group items, as they have been cleared
            this._isGrouping = false;

            if (this._group.GroupDescriptions.Count > 0)
            {
                foreach (object item in this._internalList)
                {
                    this._group.AddToSubgroups(item, true /*loading*/, null);
                }
            }

            this._isGrouping = this._group.GroupBy != null;

            // now we set the value to false, so that subsequent adds will insert
            // into the correct groups.
            this._group.IsDataInGroupOrder = false;
        }

        /// <summary>
        /// Create and sort the local index array.
        /// </summary>
        /// <param name="enumerable">The <see cref="IEnumerable"/> containing the items.</param>
        private void PrepareLocalArray(IEnumerable enumerable)
        {
            Debug.Assert(enumerable != null, "Input list to filter/sort should not be null");

            // filter the collection's array into the local array
            this._internalList = new List<Entity>();

            foreach (Entity item in enumerable)
            {
                int index = this._internalList.Count;
                this.Insert(item, ref index);
            }
        }

        /// <summary>
        /// Process <see cref="INotifyCollectionChanged.CollectionChanged"/> events from
        /// the source collection.
        /// </summary>
        /// <param name="args">The <see cref="NotifyCollectionChangedEventArgs"/> to process.</param>
        protected override void SourceCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            // if we do not want to handle the CollectionChanged event, return
            if (!this.CheckFlag(CollectionViewFlags.ShouldProcessCollectionChanged))
            {
                return;
            }

            if (args.Action == NotifyCollectionChangedAction.Reset)
            {
                if ((this.IsAddingNew && !this.SourceCollection.Cast<object>().Contains(this.CurrentAddItem)) ||
                    (this.IsEditingItem && !this.SourceCollection.Cast<object>().Contains(this.CurrentEditItem)))
                {
                    throw new InvalidOperationException(string.Format(
                        CultureInfo.InvariantCulture,
                        EntityCollectionViewResources.OperationNotAllowedDuringAddOrEdit,
                        "Removing"));
                }

                // if we have no items now, clear our own internal list
                if (!this.SourceCollection.GetEnumerator().MoveNext())
                {
                    this._internalList.Clear();
                }

                // calling Refresh, will fire the collectionchanged event
                this.RefreshOrDefer();
                return;
            }

            Entity removedItem = (args.OldItems != null) ? args.OldItems.OfType<Entity>().Single() : null;

            // fire notifications for removes
            if (args.Action == NotifyCollectionChangedAction.Remove ||
                args.Action == NotifyCollectionChangedAction.Replace)
            {
                if ((this.IsAddingNew && (removedItem == this.CurrentAddItem)) ||
                    (this.IsEditingItem && (removedItem == this.CurrentEditItem)))
                {
                    throw new InvalidOperationException(string.Format(
                        CultureInfo.InvariantCulture,
                        EntityCollectionViewResources.OperationNotAllowedDuringAddOrEdit,
                        "Removing"));
                }

                this.HandleSourceCollectionRemove(args);
            }

            // fire notifications for adds
            if (args.Action == NotifyCollectionChangedAction.Add ||
                args.Action == NotifyCollectionChangedAction.Replace)
            {
                this.HandleSourceCollectionAdd(args);
            }
        }

        /// <summary>
        /// Handle additions to the source collection, inserting the item,
        /// and if necessary, adding it into the groups.
        /// </summary>
        /// <param name="e">The event args from the source collection change.</param>
        private void HandleSourceCollectionAdd(NotifyCollectionChangedEventArgs e)
        {
            if (!this.CheckFlag(CollectionViewFlags.ShouldProcessCollectionChanged))
            {
                return;
            }

            Entity addedItem = (e.NewItems != null) ? e.NewItems.OfType<Entity>().Single() : null;

            // process the add by filtering and sorting the item
            int addIndex = e.NewStartingIndex;
            this.Insert(addedItem, ref addIndex);

            // if we need to add the item into the current group
            // that will be displayed
            if (this.IsGrouping)
            {
                this.InsertGroup(addedItem, addIndex);
            }

            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add,
                addedItem,
                addIndex));
        }

        /// <summary>
        /// Handle removals from the source collection, removing the item,
        /// and if necessary, removing it from the groups.
        /// </summary>
        /// <param name="e">The event args from the source collection change.</param>
        private void HandleSourceCollectionRemove(NotifyCollectionChangedEventArgs e)
        {
            if (!this.CheckFlag(CollectionViewFlags.ShouldProcessCollectionChanged))
            {
                return;
            }

            Entity removedItem = (e.OldItems != null) ? e.OldItems.OfType<Entity>().Single() : null;

            int removeIndex = this.IndexOf(removedItem);
            Debug.Assert(removeIndex >= 0, "We assume all removed items were in the list");

            // remove the item from the collection
            this._internalList.Remove(removedItem);

            if (this.IsGrouping)
            {
                this._group.RemoveFromSubgroups(removedItem);
            }

            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Remove,
                removedItem,
                removeIndex));
        }

        /// <summary>
        /// Handles adding an <see cref="Entity"/> into the collection, and applying sorting (through both groups and sorts).
        /// </summary>
        /// <param name="item">The <see cref="Entity"/> to insert in the collection.</param>
        /// <param name="index">Index to insert item into, updated if the position is altered due to sorting.</param>
        private void Insert(Entity item, ref int index)
        {
            if (this.GroupDescriptions.Count > 0 || this.SortDescriptions.Count > 0)
            {
                // create the SortFieldComparer to use
                SortFieldComparer sortFieldComparer = new SortFieldComparer(this.GroupDescriptions, this.SortDescriptions);

                // check if the item would be in sorted order if inserted into the specified index
                // otherwise, calculate the correct sorted index
                if (
                    /* if the new item was not originally part of list */
                    (index < 0) ||
                    /* or the new item should be before the item that would precede it */
                    ((index > 0) && (sortFieldComparer.Compare(item, this.InternalItemAt(index - 1)) < 0)) ||
                    /* or the new item should be after the item that would follow it */
                    ((index < this.InternalList.Count) && (sortFieldComparer.Compare(item, this.InternalItemAt(index)) > 0)))
                {
                    /* then find the correct sorted index for the new item */
                    index = sortFieldComparer.FindInsertIndex(item, this._internalList);
                }
            }

            // make sure that the specified insert index is within the valid range
            // otherwise, just add it to the end. the index can be set to an invalid
            // value if the item was originally not in the collection, on a different
            // page, or if it had been previously filtered out.
            if (index < 0 || index > this._internalList.Count)
            {
                index = this._internalList.Count;
            }

            this._internalList.Insert(index, item);
        }

        /// <summary>
        /// Raises the PageChanging event
        /// </summary>
        /// <param name="newPageIndex">Index of the requested page</param>
        /// <returns>True if the event is cancelled (e.Cancel was set to True), False otherwise</returns>
        private bool RaisePageChanging(int newPageIndex)
        {
            EventHandler<PageChangingEventArgs> handler = this.PageChanging;
            if (handler != null)
            {
                PageChangingEventArgs pageChangingEventArgs = new PageChangingEventArgs(newPageIndex);
                handler(this, pageChangingEventArgs);
                return pageChangingEventArgs.Cancel;
            }

            return false;
        }

        /// <summary>
        /// Raises the PageChanged event
        /// </summary>
        private void RaisePageChanged()
        {
            EventHandler<EventArgs> handler = this.PageChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// If we are not being deferred, then refresh the view.
        /// </summary>
        /// <remarks>
        /// We do not queue refreshes when deferred, because when the
        /// defer cycles ends, we'll call our refresh callback on
        /// the <see cref="DomainDataSource"/>, which will result in
        /// <see cref="Refresh"/> being called after data is reloaded.
        /// </remarks>
        private void RefreshOrDefer()
        {
            if (!this.IsRefreshDeferred)
            {
                this.RefreshView();
            }
        }

        /// <summary>
        /// Refresh the view, applying sorts and groups, and updating currency information.
        /// <para>This will not call the refresh callback.</para>
        /// </summary>
        internal void RefreshView()
        {
            Entity oldCurrentItem = (Entity)this.CurrentItem;
            int oldCurrentPosition = this.CurrentPosition;
            bool oldIsCurrentAfterLast = this.IsCurrentAfterLast;
            bool oldIsCurrentBeforeFirst = this.IsCurrentBeforeFirst;

            // set IsGrouping to false
            this._isGrouping = false;

            // force currency off the collection (gives user a chance to save dirty information)
            if (this.CurrentItem != null)
            {
                this.RaiseCurrentChanging();
            }

            // if there's no sorting/grouping, just use the collection's array
            if (this.UsesLocalArray)
            {
                try
                {
                    // create a new sorted internal list
                    this.PrepareLocalArray(this._source);

                    // apply grouping
                    this.PrepareGroups();
                }
                catch (TargetInvocationException e)
                {
                    // If there's an exception while invoking PrepareLocalArray,
                    // we want to unwrap it and throw its inner exception
                    if (e.InnerException != null)
                    {
                        throw e.InnerException;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            else
            {
                this.CopySourceToInternalList();
            }

            // reset currency values
            this.ResetCurrencyValues(oldCurrentItem, oldIsCurrentBeforeFirst, oldIsCurrentAfterLast);

            if (!this.IsPageChanging)
            {
                this.OnCollectionChanged(
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Reset));
            }

            // now raise currency changes at the end
            this.RaiseCurrentChanged(oldCurrentItem, oldCurrentPosition, oldIsCurrentBeforeFirst, oldIsCurrentAfterLast);
        }

        /// <summary>
        /// Set currency back to the previous value it had if possible. If the item is no longer in view
        /// then either use the first item in the view, or if the list is empty, use <c>null</c>.
        /// </summary>
        /// <param name="oldCurrentItem"><see cref="ICollectionView.CurrentItem"/> before processing changes.</param>
        /// <param name="oldIsCurrentBeforeFirst"><see cref="ICollectionView.IsCurrentBeforeFirst"/> before processing changes.</param>
        /// <param name="oldIsCurrentAfterLast"><see cref="ICollectionView.IsCurrentAfterLast"/> before processing changes.</param>
        private void ResetCurrencyValues(Entity oldCurrentItem, bool oldIsCurrentBeforeFirst, bool oldIsCurrentAfterLast)
        {
            if (oldIsCurrentBeforeFirst || this.IsEmpty)
            {
                this.SetCurrentToPosition(-1);
            }
            else if (oldIsCurrentAfterLast)
            {
                this.SetCurrentToPosition(this.Count);
            }
            else
            {
                // try to set currency back to old current item
                // if there are duplicates, use the position of the first matching item
                int newPosition = this.IndexOf(oldCurrentItem);

                // if the old current item is no longer in view
                if (newPosition < 0)
                {
                    // if we are adding a new item, set it as the current item, otherwise, set it to null
                    newPosition = 0;

                    if (newPosition < this.Count)
                    {
                        this.SetCurrentToPosition(newPosition);
                    }
                    else if (!this.IsEmpty)
                    {
                        this.SetCurrentToPosition(0);
                    }
                    else
                    {
                        this.SetCurrentToPosition(-1);
                    }
                }
                else
                {
                    this.SetCurrentToPosition(newPosition);
                }
            }
        }

        /// <summary>
        /// Requests a page move to the potential paged source collection.
        /// Completes the move if that paged source does not initiate a move
        /// (because the data is already local) or if the source is not paged.
        /// </summary>
        /// <param name="pageIndex">Requested page index</param>
        private void RequestPageMove(int pageIndex)
        {
            if (this.SourcePagedEntityList.MoveToPage(pageIndex))
            {
                return;
            }

            this.CompletePageMove(pageIndex);
        }

        /// <summary>
        /// Sets the specified Flag(s)
        /// </summary>
        /// <param name="flags">Flags we want to set</param>
        /// <param name="value">Value we want to set these flags to</param>
        private void SetFlag(CollectionViewFlags flags, bool value)
        {
            if (value)
            {
                this._flags = this._flags | flags;
            }
            else
            {
                this._flags = this._flags & ~flags;
            }
        }

        /// <summary>
        /// Set new SortDescription collection; re-hook collection change notification handler
        /// </summary>
        /// <param name="descriptions">SortDescriptionCollection to set the property value to</param>
        private void SetSortDescriptions(SortDescriptionCollection descriptions)
        {
            if (this._sortDescriptions != null)
            {
                ((INotifyCollectionChanged)this._sortDescriptions).CollectionChanged -= new NotifyCollectionChangedEventHandler(this.SortDescriptionsChanged);
            }

            this._sortDescriptions = descriptions;

            if (this._sortDescriptions != null)
            {
                Debug.Assert(this._sortDescriptions.Count == 0, "must be empty SortDescription collection");
                ((INotifyCollectionChanged)this._sortDescriptions).CollectionChanged += new NotifyCollectionChangedEventHandler(this.SortDescriptionsChanged);
            }
        }

        /// <summary>
        /// SortDescription was added/removed, refresh EntityCollectionView
        /// </summary>
        /// <param name="sender">Sender that triggered this handler</param>
        /// <param name="e">NotifyCollectionChangedEventArgs for this change</param>
        private void SortDescriptionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.IsAddingNew || this.IsEditingItem)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, EntityCollectionViewResources.OperationNotAllowedDuringAddOrEdit, "Sorting"));
            }
        }

        /// <summary>
        /// Called when the source collection raises its PropertyChanged event.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> used in the event.</param>
        private void SourceCollection_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ItemCount":
                    this.ItemCount = this.SourcePagedEntityList.ItemCount;
                    break;

                case "PageIndex":
                    this.PageIndex = this.SourcePagedEntityList.PageIndex;
                    break;

                case "TotalItemCount":
                    this.TotalItemCount = this.SourcePagedEntityList.TotalItemCount;
                    break;

                case "BackingEntitySet":
                    this.CalculateCanAdd();
                    this.CalculateSupportedOperations();
                    this.CalculateCanAddNew();
                    break;

                case "EntityType":
                    this.CalculateSupportedOperations();
                    this.CalculateCanAddNew();
                    break;

                case "PageSize":
                    this.PageSize = this.SourcePagedEntityList.PageSize;
                    break;

                case "IsPagingOperationPending":
                    this.CalculateCanAdd();
                    this.CalculateCanAddNew();
                    break;
            }
        }

        /// <summary>
        /// Update an item's position based on sorting and grouping.  If the item was
        /// the <see cref="ICollectionView.CurrentItem"/> then maintain the currency
        /// on that item in its potentially new position.
        /// </summary>
        /// <param name="item">The <see cref="Entity"/> to update position for.</param>
        private void UpdateItemPosition(Entity item)
        {
            if (this.UsesLocalArray)
            {
                // If the item was on the current page, then we need to remove/re-add to make sure it's in
                // the correct position
                int removeIndex = this.InternalIndexOf(item);

                if (removeIndex >= 0)
                {
                    bool wasCurrentItem = this.CurrentItem == item;
                    this._internalList.Remove(item);

                    if (this.IsGrouping)
                    {
                        // we can't just call RemoveFromSubgroups, as the group name
                        // for the item may have changed during the edit.
                        this._group.RemoveItemFromSubgroupsByExhaustiveSearch(item);
                    }

                    object oldCurrentItem = this.CurrentItem;
                    int oldCurrentPosition = this.CurrentPosition;
                    bool oldIsCurrentAfterLast = this.IsCurrentAfterLast;
                    bool oldIsCurrentBeforeFirst = this.IsCurrentBeforeFirst;

                    this.EnsureValidCurrency();

                    // next process adding it into the correct location
                    int addIndex = removeIndex;
                    this.Insert(item, ref addIndex);

                    if (this.IsGrouping)
                    {
                        this.InsertGroup(item, addIndex);
                    }

                    // Raise the events for the remove and the add, but only if the index actually changed
                    if (addIndex != removeIndex)
                    {
                        this.OnCollectionChanged(
                            new NotifyCollectionChangedEventArgs(
                                NotifyCollectionChangedAction.Remove,
                                item,
                                removeIndex));

                        // check the index of the item again, just to be sure it didn't change during the remove event
                        addIndex = this.IndexOf(item);

                        this.OnCollectionChanged(
                            new NotifyCollectionChangedEventArgs(
                                NotifyCollectionChangedAction.Add,
                                item,
                                addIndex));

                        // check the index of the item again, just to be sure it didn't change during the add event
                        if (wasCurrentItem)
                        {
                            addIndex = this.IndexOf(item);
                        }
                    }

                    if (wasCurrentItem)
                    {
                        this.ResetCurrencyValues((Entity)oldCurrentItem, oldIsCurrentBeforeFirst, oldIsCurrentAfterLast);
                        this.RaiseCurrentChanged(oldCurrentItem, oldCurrentPosition, oldIsCurrentBeforeFirst, oldIsCurrentAfterLast);
                    }
                }
            }
        }

        #endregion Protected and Private Methods

        #region Nested Classes / Enums

        /// <summary>
        /// IComparer class to sort by class property value (using reflection).
        /// </summary>
        internal class SortFieldComparer : IComparer
        {
            #region Constructors

            internal SortFieldComparer() { }

            /// <summary>
            /// Create a comparer, using the <paramref name="groupDescriptions"/> and/or
            /// <paramref name="sortFields"/> and the <see cref="Type"/> of each property.
            /// Tries to find a reflection <see cref="PropertyInfo"/> for each property name.
            /// </summary>
            /// <remarks>
            /// Groups are used first, and then sorts.  This keeps the groups together.
            /// </remarks>
            /// <param name="groupDescriptions">List of group descriptions to respect
            /// (ascending, unless there is a descending sort also applied).</param>
            /// <param name="sortFields">List of sort descriptions to respect (ascending or descending).</param>
            public SortFieldComparer(ObservableCollection<GroupDescription> groupDescriptions, SortDescriptionCollection sortFields)
            {
                this._groupFields = groupDescriptions;
                this._sortFields = sortFields;
                this._fields = CreatePropertyInfo(this._groupFields, this._sortFields);
            }

            #endregion

            #region Public Methods

            /// <summary>
            /// Compare the two items using the groups and sorts provided.
            /// </summary>
            /// <remarks>
            /// Uses reflection to find the <see cref="Type"/> of each field being sorted
            /// (groups first, then sorts), and assigns a comparer for each field.  Each
            /// comparer is then used to compare the property from both. 
            /// </remarks>
            /// <param name="x">First item to compare.</param>
            /// <param name="y">Second item to compare.</param>
            /// <returns>Negative number if <paramref name="x"/> should be grouped/sorted
            /// before than <paramref name="y"/>; zero if <paramref name="x"/> and <paramref name="y"/>
            /// have matching fields for the groups and sorts; and a positive number if
            /// <paramref name="x"/> should be grouped/sorted after <paramref name="y"/>.</returns>
            public int Compare(object x, object y)
            {
                int result = 0;

                // compare both objects by each of the properties until property values don't match
                for (int k = 0; k < this._fields.Length; ++k)
                {
                    // if the property type is not yet determined, try
                    // obtaining it from the objects
                    Type propertyType = this._fields[k].PropertyType;
                    if (propertyType == null)
                    {
                        if (x != null)
                        {
                            this._fields[k].PropertyType = x.GetType().GetNestedPropertyType(this._fields[k].PropertyPath);
                            propertyType = this._fields[k].PropertyType;
                        }
                        if (this._fields[k].PropertyType == null && y != null)
                        {
                            this._fields[k].PropertyType = y.GetType().GetNestedPropertyType(this._fields[k].PropertyPath);
                            propertyType = this._fields[k].PropertyType;
                        }
                    }

                    object v1 = this._fields[k].GetValue(x);
                    object v2 = this._fields[k].GetValue(y);

                    // try to also set the value for the comparer if this was 
                    // not already calculated
                    IComparer comparer = this._fields[k].Comparer;
                    if (propertyType != null && comparer == null)
                    {
                        this._fields[k].Comparer = (typeof(Comparer<>).MakeGenericType(propertyType).GetProperty("Default")).GetValue(null, null) as IComparer;
                        comparer = this._fields[k].Comparer;
                    }

                    result = (comparer != null) ? comparer.Compare(v1, v2) : 0 /*both values equal*/;
                    if (this._fields[k].Descending)
                    {
                        result = -result;
                    }

                    if (result != 0)
                    {
                        break;
                    }
                }

                return result;
            }

            /// <summary>
            /// Steps through the given list using the comparer to find where
            /// to insert the specified item to maintain sorted order
            /// </summary>
            /// <param name="x">Item to insert into the list</param>
            /// <param name="list">List where we want to insert the item</param>
            /// <returns>Index where we should insert into</returns>
            public int FindInsertIndex(object x, IList list)
            {
                int min = 0;
                int max = list.Count - 1;
                int index;

                // run a binary search to find the right index
                // to insert into.
                while (min <= max)
                {
                    index = (min + max) / 2;

                    int result = this.Compare(x, list[index]);
                    if (result == 0)
                    {
                        return index;
                    }
                    else if (result > 0)
                    {
                        min = index + 1;
                    }
                    else
                    {
                        max = index - 1;
                    }
                }

                return min;
            }

            #endregion

            #region Private Methods

            /// <summary>
            /// Builds an array of <see cref="SortPropertyInfo"/> objects by combining the groups and the sorts into
            /// a single list of sorts.  This will ensure that the groups remain contiguous and that the items
            /// within the groups are sorted within the groups.
            /// </summary>
            /// <remarks>
            /// Fields that are grouped will be sorted in ascending order by default, but if there is also a sort
            /// specified for that field and it is sorted in descending order, then the group will respect the
            /// descending order as well.
            /// </remarks>
            /// <param name="groupFields">The <see cref="GroupDescription"/> list for the fields being grouped.  Must not be null.</param>
            /// <param name="sortFields">The <see cref="SortDescriptionCollection"/> for the fields being sorted.  Must not be null.</param>
            /// <returns>
            /// A single array of <see cref="SortPropertyInfo"/> that will apply the grouping and then sorting in the
            /// necessary order to have items grouped and then sorted within the groups.
            /// </returns>
            private static SortPropertyInfo[] CreatePropertyInfo(ObservableCollection<GroupDescription> groupFields, SortDescriptionCollection sortFields)
            {
                Debug.Assert(groupFields != null, "Unexpected null groupFields");
                Debug.Assert(sortFields != null, "Unexpected null sortFields");

                // Set the field count to be the sum of the group count and sort count
                int groupCount = groupFields.Count;
                int sortCount = sortFields.Count;

                // Create our array of fields based on the known size
                SortPropertyInfo[] fields = new SortPropertyInfo[groupCount + sortCount];

                // Loop through the sort fields first, so that we can keep track of which fields are sorted
                // in Descending order, as we need that information when looping through the group fields
                List<string> descendingSorts = null;

                for (int s = 0; s < sortCount; ++s)
                {
                    // Remember PropertyPath and Direction, used when actually sorting
                    // Store the sort fields after the group fields that will be added, to ensure that
                    // grouping happens first.
                    fields[groupCount + s].PropertyPath = sortFields[s].PropertyName;
                    fields[groupCount + s].Descending = (sortFields[s].Direction == ListSortDirection.Descending);

                    // If this field was sorted in descending order and we're also grouping
                    // then record this field as a descending field so that if there is a group
                    // on this field as well, then the group can be applied in descending order
                    if (fields[groupCount + s].Descending && (groupCount > 0))
                    {
                        if (descendingSorts == null)
                        {
                            descendingSorts = new List<string>();
                        }

                        descendingSorts.Add(fields[groupCount + s].PropertyPath);
                    }
                }

                // Now loop through the groups, adding sorts for each one (earlier in the list than the sorts)
                // so that groups are kept contiguous.  Respect any descending sorts on the grouped fields though.
                for (int g = 0; g < groupCount; ++g)
                {
                    fields[g].PropertyPath = (groupFields[g] as PropertyGroupDescription).PropertyName;
                    fields[g].Descending = (descendingSorts != null && descendingSorts.Contains(fields[g].PropertyPath));
                }

                return fields;
            }

            #endregion

            #region Private Fields

            struct SortPropertyInfo
            {
                internal IComparer Comparer;
                internal bool Descending;
                internal string PropertyPath;
                internal Type PropertyType;

                internal object GetValue(object o)
                {
                    object value;
                    if (String.IsNullOrEmpty(this.PropertyPath))
                    {
                        value = (this.PropertyType == o.GetType()) ? o : null;
                    }
                    else
                    {
                        value = PagedEntityCollectionView.InvokePath(o, this.PropertyPath, this.PropertyType);
                    }

                    return value;
                }
            }

            private readonly SortPropertyInfo[] _fields;
            private readonly ObservableCollection<GroupDescription> _groupFields;
            private readonly SortDescriptionCollection _sortFields;

            #endregion
        }

        /// <summary>
        /// Enum for CollectionViewFlags
        /// </summary>
        [Flags]
        private enum CollectionViewFlags
        {
            /// <summary>
            /// Whether we should process the collection changed event
            /// </summary>
            ShouldProcessCollectionChanged = 0x01,

            /// <summary>
            /// Whether we cache the IsEmpty value
            /// </summary>
            CachedIsEmpty = 0x02,

            /// <summary>
            /// Indicates whether a page index change is in process or not
            /// </summary>
            IsPageChanging = 0x04,

            /// <summary>
            /// Whether we need to move to another page after a DeferRefresh is disposed
            /// </summary>
            IsMoveToPageDeferred = 0x08,

            /// <summary>
            /// Whether we need to update the PageSize after a DeferRefresh is disposed
            /// </summary>
            IsUpdatePageSizeDeferred = 0x10
        }

        #endregion Nested Classes / Enums
    }
}
