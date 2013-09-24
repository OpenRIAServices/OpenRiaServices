using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace OpenRiaServices.Controls
{
    /// <summary>
    /// Collection view for the <see cref="System.Windows.Controls.DomainDataSource"/>.
    /// </summary>
    /// <remarks>
    /// This view supports adding, removing, access, and paging. For other standard view
    /// functions like sorting, filtering, and grouping, use of the Descriptor collections
    /// on the <see cref="System.Windows.Controls.DomainDataSource"/> is recommended.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "It's a view, not a collection.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1010:CollectionsShouldImplementGenericInterface", Justification = "Again, it's a view.")]
    public sealed class DomainDataSourceView :
        ICollectionView,
        IEditableCollectionView,
        IPagedCollectionView,
        INotifyPropertyChanged
    {
        #region Static fields

        // We only support notification for public properties
        private static readonly ReadOnlyCollection<string> notifyProperties =
            new ReadOnlyCollection<string>(new List<string>()
            {
                /* Public Properties */
                "CanAdd", "CanChangePage", "CanRemove", "Count",
                "CurrentItem", "CurrentPosition", "IsEmpty", "IsPageChanging",
                "PageCount", "PageIndex", "PageSize", "TotalItemCount",
                /* Explicit Interface Implementation Properties - Required for SDK controls */
                "ItemCount" /* SSS_DROP_BEGIN */ /* Silverlight Bug 79272 */ /* SSS_DROP_END */
            });

        #endregion

        #region Member fields

        private readonly PagedEntityCollectionView _pagedEntityCollectionView;
        // Explicit event handling
        private NotifyCollectionChangedEventHandler _collectionChangedHandler;
        private PropertyChangedEventHandler _propertyChangedHandler;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainDataSourceView"/> class.
        /// </summary>
        /// <param name="entityCollectionView">The <see cref="System.Windows.Controls.PagedEntityCollectionView"/> to wrap</param>
        internal DomainDataSourceView(PagedEntityCollectionView entityCollectionView)
        {
            Debug.Assert(entityCollectionView != null, "EntityCollectionView cannot be null.");

            this._pagedEntityCollectionView = entityCollectionView;

            // ICollectionView
            this._pagedEntityCollectionView.CollectionChanged += this.OnCollectionViewCollectionChanged;
            this._pagedEntityCollectionView.CurrentChanged += this.OnCollectionViewCurrentChanged;
            this._pagedEntityCollectionView.CurrentChanging += this.OnCollectionViewCurrentChanging;

            // IPagedCollectionView
            this._pagedEntityCollectionView.PageChanged += this.OnPagedCollectionViewPageChanged;
            this._pagedEntityCollectionView.PageChanging += this.OnPagedCollectionViewPageChanging;

            this._pagedEntityCollectionView.PropertyChanged += this.OnPagedEntityCollectionViewPropertyChanged;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a list of the public properties supported in <see cref="INotifyPropertyChanged"/> events.
        /// </summary>
        private static ReadOnlyCollection<string> NotifyProperties
        {
            get { return DomainDataSourceView.notifyProperties; }
        }

        /// <summary>
        /// Gets the item at the specified index.
        /// </summary>
        /// <param name="index">The index to get the item at</param>
        /// <returns>The item at the specified index</returns>
        /// <exception cref="ArgumentOutOfRangeException"> is thrown if <paramref name="index"/> is out of range.
        /// </exception>
        public object this[int index]
        {
            get { return this.PagedEntityCollectionView[index]; }
        }

        /// <summary>
        /// Gets a value indicating whether items can be added to the collection.
        /// </summary>
        public bool CanAdd
        {
            get { return this.PagedEntityCollectionView.CanAdd; }
        }

        /// <summary>
        /// Gets the count of the items currently in this view.
        /// </summary>
        /// <remarks>
        /// This count only applies to the current view and does not represent the total
        /// count of items that may be in a paged view.
        /// </remarks>
        public int Count
        {
            get { return this.PagedEntityCollectionView.Count; }
        }

        /// <summary>
        /// Gets the count of the pages in this view.
        /// </summary>
        /// <remarks>
        /// When <see cref="PageSize"/> is 0, the <see cref="PageCount"/> will also be 0.
        /// </remarks>
        public int PageCount
        {
            get { return this.PagedEntityCollectionView.PageCount; }
        }

        #region Views

        /// <summary>
        /// Gets the underlying collection
        /// </summary>
        private PagedEntityCollectionView PagedEntityCollectionView
        {
            get { return this._pagedEntityCollectionView; }
        }

        /// <summary>
        /// Gets the underlying collection as an <see cref="ICollectionView"/>.
        /// </summary>
        private ICollectionView CollectionView
        {
            get { return this._pagedEntityCollectionView; }
        }

        /// <summary>
        /// Gets the underlying collection as an <see cref="IEditableCollectionView"/>.
        /// </summary>
        private IEditableCollectionView EditableCollectionView
        {
            get { return this._pagedEntityCollectionView; }
        }

        /// <summary>
        /// Gets the underlying collection as an <see cref="IPagedCollectionView"/>.
        /// </summary>
        private IPagedCollectionView PagedCollectionView
        {
            get { return this._pagedEntityCollectionView; }
        }

        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// Adds the specified item to the collection.
        /// </summary>
        /// <param name="item">The item to add</param>
        /// <exception cref="InvalidOperationException"> is thrown if <see cref="CanAdd"/> if <c>false</c>.
        /// </exception>
        public void Add(object item)
        {
            this.PagedEntityCollectionView.Add(item);
        }

        /// <summary>
        /// Gets the item at the specified index.
        /// </summary>
        /// <param name="index">The index to get the item at</param>
        /// <returns>The item at the specified index</returns>
        /// <exception cref="ArgumentOutOfRangeException"> is thrown if <paramref name="index"/> is out of range.
        /// </exception>
        public object GetItemAt(int index)
        {
            return this.PagedEntityCollectionView.GetItemAt(index);
        }

        /// <summary>
        /// Gets the ordinal position of the specified item within the view.
        /// </summary>
        /// <param name="item">The item to get the index of.</param>
        /// <returns>Returns the index.</returns>
        public int IndexOf(object item)
        {
            return this.PagedEntityCollectionView.IndexOf(item);
        }

        #endregion

        #region ICollectionView Members

        #region Events

        /// <summary>
        /// Event raised after the current item has been changed.
        /// </summary>
        /// <seealso cref="ICollectionView.CurrentChanged"/>
        public event EventHandler CurrentChanged;

        /// <summary>
        /// Event raised before changing the current item.
        /// </summary>
        /// <seealso cref="ICollectionView.CurrentChanging"/>
        public event CurrentChangingEventHandler CurrentChanging;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current item in the view.
        /// </summary>
        /// <remarks>
        /// Returns <c>null</c> if there is no current item.
        /// </remarks>
        /// <seealso cref="ICollectionView.CurrentItem"/>
        public object CurrentItem
        {
            get { return this.CollectionView.CurrentItem; }
        }

        /// <summary>
        /// Gets the ordinal position of the <see cref="CurrentItem"/> within the view.
        /// </summary>
        /// <remarks>
        /// Returns -1 if there is no current item.
        /// </remarks>
        /// <seealso cref="ICollectionView.CurrentPosition"/>
        public int CurrentPosition
        {
            get { return this.CollectionView.CurrentPosition; }
        }

        /// <summary>
        /// Gets a value that indicates whether the resulting view is empty.
        /// </summary>
        /// <seealso cref="ICollectionView.IsEmpty"/>
        public bool IsEmpty
        {
            get { return this.CollectionView.IsEmpty; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns a value that indicates whether a given item belongs to this view.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns><c>true</c> if the item belongs to this view; otherwise <c>false</c>.</returns>
        /// <seealso cref="ICollectionView.Contains"/>
        public bool Contains(object item)
        {
            return this.CollectionView.Contains(item);
        }

        /// <summary>
        /// Sets the specified item to be the <see cref="CurrentItem"/> in the view.
        /// </summary>
        /// <param name="item">The item to set as the <see cref="CurrentItem"/>.</param>
        /// <returns><c>true</c> if the resulting <see cref="CurrentItem"/> is within the view; otherwise, <c>false</c>.</returns>
        /// <seealso cref="ICollectionView.MoveCurrentTo"/>
        public bool MoveCurrentTo(object item)
        {
            return this.CollectionView.MoveCurrentTo(item);
        }

        /// <summary>
        /// Sets the first item in the view as the <see cref="CurrentItem"/>.
        /// </summary>
        /// <returns><c>true</c> if the resulting <see cref="CurrentItem"/> is within the view; otherwise, <c>false</c>.</returns>
        /// <seealso cref="ICollectionView.MoveCurrentToFirst"/>
        public bool MoveCurrentToFirst()
        {
            return this.CollectionView.MoveCurrentToFirst();
        }

        /// <summary>
        /// Sets the last item in the view as the <see cref="CurrentItem"/>.
        /// </summary>
        /// <returns><c>true</c> if the resulting <see cref="CurrentItem"/> is within the view; otherwise, <c>false</c>.</returns>
        /// <seealso cref="ICollectionView.MoveCurrentToLast"/>
        public bool MoveCurrentToLast()
        {
            return this.CollectionView.MoveCurrentToLast();
        }

        /// <summary>
        /// Sets the item after the <see cref="CurrentItem"/> in the view as the <see cref="CurrentItem"/>.
        /// </summary>
        /// <returns><c>true</c> if the resulting <see cref="CurrentItem"/> is within the view; otherwise, <c>false</c>.</returns>
        /// <seealso cref="ICollectionView.MoveCurrentToNext"/>
        public bool MoveCurrentToNext()
        {
            return this.CollectionView.MoveCurrentToNext();
        }

        /// <summary>
        /// Sets the item at the specified index to be the <see cref="CurrentItem"/> in the view.
        /// </summary>
        /// <param name="position">The index to set the <see cref="CurrentItem"/> to</param>
        /// <returns><c>true</c> if the resulting <see cref="CurrentItem"/> is within the view; otherwise, <c>false</c>.</returns>
        /// <seealso cref="ICollectionView.MoveCurrentToPosition"/>
        public bool MoveCurrentToPosition(int position)
        {
            return this.CollectionView.MoveCurrentToPosition(position);
        }

        /// <summary>
        /// Sets the item before the <see cref="CurrentItem"/> in the view as the <see cref="CurrentItem"/>.
        /// </summary>
        /// <returns><c>true</c> if the resulting <see cref="CurrentItem"/> is within the view; otherwise, <c>false</c>.</returns>
        /// <seealso cref="ICollectionView.MoveCurrentToPrevious"/>
        public bool MoveCurrentToPrevious()
        {
            return this.CollectionView.MoveCurrentToPrevious();
        }

        /// <summary>
        /// Handles current changed events raised by the <see cref="ICollectionView"/>.
        /// </summary>
        /// <param name="sender">The <see cref="ICollectionView"/></param>
        /// <param name="e">The event to handle</param>
        private void OnCollectionViewCurrentChanged(object sender, EventArgs e)
        {
            this.OnCurrentChanged(e);
        }

        /// <summary>
        /// Raises current changed events
        /// </summary>
        /// <param name="e">The event to raise</param>
        private void OnCurrentChanged(EventArgs e)
        {
            EventHandler handler = this.CurrentChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Handles current changing events raised by the <see cref="ICollectionView"/>.
        /// </summary>
        /// <param name="sender">The <see cref="ICollectionView"/></param>
        /// <param name="e">The event to handle</param>
        private void OnCollectionViewCurrentChanging(object sender, CurrentChangingEventArgs e)
        {
            this.OnCurrentChanging(e);
        }

        /// <summary>
        /// Raises current changing events
        /// </summary>
        /// <param name="e">The event to raise</param>
        private void OnCurrentChanging(CurrentChangingEventArgs e)
        {
            CurrentChangingEventHandler handler = this.CurrentChanging;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        #endregion

        #region Explicit

        bool ICollectionView.CanFilter
        {
            get { return this.CollectionView.CanFilter; }
        }

        bool ICollectionView.CanGroup
        {
            get { return this.CollectionView.CanGroup; }
        }

        bool ICollectionView.CanSort
        {
            get { return this.CollectionView.CanSort; }
        }

        CultureInfo ICollectionView.Culture
        {
            get { return this.CollectionView.Culture; }
            set { this.CollectionView.Culture = value; }
        }

        IDisposable ICollectionView.DeferRefresh()
        {
            return this.CollectionView.DeferRefresh();
        }

        Predicate<object> ICollectionView.Filter
        {
            get { return this.CollectionView.Filter; }
            set { this.CollectionView.Filter = value; }
        }

        ObservableCollection<GroupDescription> ICollectionView.GroupDescriptions
        {
            get { return this.CollectionView.GroupDescriptions; }
        }

        ReadOnlyObservableCollection<object> ICollectionView.Groups
        {
            get { return this.CollectionView.Groups; }
        }

        bool ICollectionView.IsCurrentAfterLast
        {
            get { return this.CollectionView.IsCurrentAfterLast; }
        }

        bool ICollectionView.IsCurrentBeforeFirst
        {
            get { return this.CollectionView.IsCurrentBeforeFirst; }
        }

        void ICollectionView.Refresh()
        {
            this.CollectionView.Refresh();
        }

        SortDescriptionCollection ICollectionView.SortDescriptions
        {
            get { return this.CollectionView.SortDescriptions; }
        }

        IEnumerable ICollectionView.SourceCollection
        {
            get { return this.CollectionView.SourceCollection; }
        }

        #endregion

        #endregion

        #region IEnumerable Members

        #region Explicit

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.CollectionView.GetEnumerator();
        }

        #endregion

        #endregion

        #region INotifyCollectionChanged Members

        #region Explicit

        event NotifyCollectionChangedEventHandler INotifyCollectionChanged.CollectionChanged
        {
            add { this._collectionChangedHandler += value; }
            remove { this._collectionChangedHandler -= value; }
        }

        /// <summary>
        /// Handles collection changed events raised by the <see cref="ICollectionView"/>.
        /// </summary>
        /// <param name="sender">The <see cref="ICollectionView"/></param>
        /// <param name="e">The event to handle</param>
        private void OnCollectionViewCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.OnCollectionChanged(e);
        }

        /// <summary>
        /// Raises collection changed events
        /// </summary>
        /// <param name="e">The event to raise</param>
        private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventHandler handler = this._collectionChangedHandler;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        #endregion

        #endregion

        #region IEditableCollectionView Members

        #region Properties

        /// <summary>
        /// Gets a value that indicates whether an item can be removed from the collection.
        /// </summary>
        /// <seealso cref="IEditableCollectionView.CanRemove"/>
        public bool CanRemove
        {
            get { return this.EditableCollectionView.CanRemove; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Removes the specified item from the collection.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <seealso cref="IEditableCollectionView.Remove"/>
        public void Remove(object item)
        {
            this.EditableCollectionView.Remove(item);
        }

        /// <summary>
        /// Removes the item at the specified position from the collection.
        /// </summary>
        /// <param name="index">The position of the item to remove</param>
        /// <seealso cref="IEditableCollectionView.RemoveAt"/>
        public void RemoveAt(int index)
        {
            this.EditableCollectionView.RemoveAt(index);
        }

        #endregion

        #region Explicit

        object IEditableCollectionView.AddNew()
        {
            return this.EditableCollectionView.AddNew();
        }

        bool IEditableCollectionView.CanAddNew
        {
            get { return this.EditableCollectionView.CanAddNew; }
        }

        bool IEditableCollectionView.CanCancelEdit
        {
            get { return this.EditableCollectionView.CanCancelEdit; }
        }

        void IEditableCollectionView.CancelEdit()
        {
            this.EditableCollectionView.CancelEdit();
        }

        void IEditableCollectionView.CancelNew()
        {
            this.EditableCollectionView.CancelNew();
        }

        void IEditableCollectionView.CommitEdit()
        {
            this.EditableCollectionView.CommitEdit();
        }

        void IEditableCollectionView.CommitNew()
        {
            this.EditableCollectionView.CommitNew();
        }

        object IEditableCollectionView.CurrentAddItem
        {
            get { return this.EditableCollectionView.CurrentAddItem; }
        }

        object IEditableCollectionView.CurrentEditItem
        {
            get { return this.EditableCollectionView.CurrentEditItem; }
        }

        void IEditableCollectionView.EditItem(object item)
        {
            this.EditableCollectionView.EditItem(item);
        }

        bool IEditableCollectionView.IsAddingNew
        {
            get { return this.EditableCollectionView.IsAddingNew; }
        }

        bool IEditableCollectionView.IsEditingItem
        {
            get { return this.EditableCollectionView.IsEditingItem; }
        }

        NewItemPlaceholderPosition IEditableCollectionView.NewItemPlaceholderPosition
        {
            get { return this.EditableCollectionView.NewItemPlaceholderPosition; }
            set { this.EditableCollectionView.NewItemPlaceholderPosition = value; }
        }

        #endregion

        #endregion

        #region IPagedCollectionView Members

        #region Events

        /// <summary>
        /// Event raised after the <see cref="PageIndex"/> has changed.
        /// </summary>
        /// <seealso cref="IPagedCollectionView.PageChanged"/>
        public event EventHandler<EventArgs> PageChanged;

        /// <summary>
        /// Event raised before changing the <see cref="PageIndex"/>.
        /// </summary>
        /// <seealso cref="IPagedCollectionView.PageChanging"/>
        public event EventHandler<PageChangingEventArgs> PageChanging;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value that indicates whether the <see cref="PageIndex"/> value can change.
        /// </summary>
        /// <seealso cref="IPagedCollectionView.CanChangePage"/>
        public bool CanChangePage
        {
            get { return this.PagedCollectionView.CanChangePage; }
        }

        /// <summary>
        /// Gets a value that indicates whether the page index is changing.
        /// </summary>
        /// <seealso cref="IPagedCollectionView.IsPageChanging"/>
        public bool IsPageChanging
        {
            get { return this.PagedCollectionView.IsPageChanging; }
        }

        /// <summary>
        /// Gets the zero-based index of the current page.
        /// </summary>
        /// <seealso cref="IPagedCollectionView.PageIndex"/>
        public int PageIndex
        {
            get { return this.PagedCollectionView.PageIndex; }
        }

        /// <summary>
        /// Gets or sets the number of items to display on a page.
        /// </summary>
        /// <remarks>
        /// When <see cref="PageSize"/> is 0, the view is not paging.
        /// </remarks>
        /// <seealso cref="IPagedCollectionView.PageSize"/>
        public int PageSize
        {
            get { return this.PagedCollectionView.PageSize; }
            set { this.PagedCollectionView.PageSize = value; }
        }

        /// <summary>
        /// Gets the total number of items in the view before paging is applied,
        /// or -1 if that total number is unknown.
        /// </summary>
        /// <seealso cref="IPagedCollectionView.TotalItemCount"/>
        public int TotalItemCount
        {
            get { return this.PagedCollectionView.TotalItemCount; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sets the first page as the current page.
        /// </summary>
        /// <returns><c>true</c> if the operation was successful; otherwise, <c>false</c>.</returns>
        /// <seealso cref="IPagedCollectionView.MoveToFirstPage"/>
        public bool MoveToFirstPage()
        {
            return this.PagedCollectionView.MoveToFirstPage();
        }

        /// <summary>
        /// Sets the last page as the current page.
        /// </summary>
        /// <returns><c>true</c> if the operation was successful; otherwise, <c>false</c>.</returns>
        /// <seealso cref="IPagedCollectionView.MoveToLastPage"/>
        public bool MoveToLastPage()
        {
            return this.PagedCollectionView.MoveToLastPage();
        }

        /// <summary>
        /// Moves to the page after the current page.
        /// </summary>
        /// <returns><c>true</c> if the operation was successful; otherwise, <c>false</c>.</returns>
        /// <seealso cref="IPagedCollectionView.MoveToNextPage"/>
        public bool MoveToNextPage()
        {
            return this.PagedCollectionView.MoveToNextPage();
        }

        /// <summary>
        /// Sets the first page as the current page.
        /// </summary>
        /// <param name="pageIndex">The index of the page to move to</param>
        /// <returns><c>true</c> if the operation was successful; otherwise, <c>false</c>.</returns>
        /// <seealso cref="IPagedCollectionView.MoveToPage"/>
        public bool MoveToPage(int pageIndex)
        {
            return this.PagedCollectionView.MoveToPage(pageIndex);
        }

        /// <summary>
        /// Moves to the page before the current page.
        /// </summary>
        /// <returns><c>true</c> if the operation was successful; otherwise, <c>false</c>.</returns>
        /// <seealso cref="IPagedCollectionView.MoveToPreviousPage"/>
        public bool MoveToPreviousPage()
        {
            return this.PagedCollectionView.MoveToPreviousPage();
        }

        /// <summary>
        /// Handles page changed events raised by the <see cref="IPagedCollectionView"/>.
        /// </summary>
        /// <param name="sender">The <see cref="IPagedCollectionView"/></param>
        /// <param name="e">The event to handle</param>
        private void OnPagedCollectionViewPageChanged(object sender, EventArgs e)
        {
            this.OnPageChanged(e);
        }

        /// <summary>
        /// Raises page changed events
        /// </summary>
        /// <param name="e">The event to raise</param>
        private void OnPageChanged(EventArgs e)
        {
            EventHandler<EventArgs> handler = this.PageChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Handles page changing events raised by the <see cref="IPagedCollectionView"/>.
        /// </summary>
        /// <param name="sender">The <see cref="IPagedCollectionView"/></param>
        /// <param name="e">The event to handle</param>
        private void OnPagedCollectionViewPageChanging(object sender, PageChangingEventArgs e)
        {
            this.OnPageChanging(e);
        }

        /// <summary>
        /// Raises page changing events
        /// </summary>
        /// <param name="e">The event to raise</param>
        private void OnPageChanging(PageChangingEventArgs e)
        {
            EventHandler<PageChangingEventArgs> handler = this.PageChanging;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        #endregion

        #region Explicit

        int IPagedCollectionView.ItemCount
        {
            get { return this.PagedCollectionView.ItemCount; }
        }

        #endregion

        #endregion

        #region INotifyPropertyChanged Members

        #region Explicit

        /// <summary>
        /// Event raised 
        /// </summary>
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { this._propertyChangedHandler += value; }
            remove { this._propertyChangedHandler -= value; }
        }

        /// <summary>
        /// Handles property changed events raised by the <see cref="System.Windows.Controls.PagedEntityCollectionView"/>.
        /// </summary>
        /// <param name="sender">The <see cref="System.Windows.Controls.PagedEntityCollectionView"/></param>
        /// <param name="e">The event to handle</param>
        private void OnPagedEntityCollectionViewPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (DomainDataSourceView.NotifyProperties.Contains(e.PropertyName))
            {
                this.OnPropertyChanged(e);
            }
        }

        /// <summary>
        /// Raises property changed events
        /// </summary>
        /// <param name="e">The event to raise</param>
        private void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = this._propertyChangedHandler;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        #endregion

        #endregion
    }
}
