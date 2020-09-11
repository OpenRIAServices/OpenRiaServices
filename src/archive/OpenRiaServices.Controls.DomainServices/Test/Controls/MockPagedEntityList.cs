using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using OpenRiaServices.Client;

namespace OpenRiaServices.Controls.DomainServices.Test
{
    /// <summary>
    /// A mock <see cref="IPagedEntityList"/> that can be used for testing consumers of an
    /// <see cref="IPagedEntityList"/> such as the <see cref="EntityCollectionView"/>.
    /// </summary>
    /// <remarks>
    /// This is not a fully-functioning mock; it's a stub that can be used as an
    /// <see cref="IPagedEntityList"/> by providing an <see cref="EntitySet"/>
    /// as the <see cref="BackingEntitySet"/>, and an action to call when
    /// <see cref="MoveToPage"/> is invoked.
    /// <para>The <see cref="Add"/> and <see cref="Remove"/> methods will
    /// add and remove items from a local list, raising
    /// <see cref="INotifyCollectionChanged.CollectionChanged"/> events.</para>
    /// <para>Aside from the <see cref="BackingEntitySet"/> property, all
    /// other properties are read-write to allow consumers of this mock
    /// to control their values.  These property values are not kept
    /// in sync with the set of entities.</para>
    /// </remarks>
    internal class MockPagedEntityList : IPagedEntityList
    {
        #region Member Fields

        private bool _isPagingOperationPending;
        private int _itemCount;
        private readonly List<Entity> _localList = new List<Entity>();
        private readonly Func<int, bool> _moveToPage;
        private int _pageIndex;
        private int _pageSize;
        private int _totalItemCount;

        #endregion

        /// <summary>
        /// Initialize a new instance of the <see cref="MockPagedEntityList"/> class.
        /// </summary>
        /// <param name="entitySet"></param>
        /// <param name="moveToPage"></param>
        public MockPagedEntityList(EntitySet entitySet, Func<int, bool> moveToPage)
        {
            this.BackingEntitySet = entitySet;
            this._moveToPage = moveToPage;
        }

        #region IPagedEntityList Members

        /// <summary>
        /// The <see cref="EntitySet"/> that is used as the backing store
        /// for the entities within this <see cref="IPagedEntityList"/>.
        /// </summary>
        /// <remarks>
        /// There may be additional entities in the backing entity list that
        /// are not included in this <see cref="IPagedEntityList"/>.
        /// </remarks>
        public EntitySet BackingEntitySet { get; private set; }

        /// <summary>
        /// Gets the type of <see cref="Entity"/> for this list.
        /// </summary>
        public Type EntityType
        {
            get
            {
                return this.BackingEntitySet.EntityType;
            }
        }

        /// <summary>
        /// MOCK: Gets or sets a value indicating that paging is enabled but the data hasn't yet been loaded.
        /// </summary>
        public bool IsPagingOperationPending
        {
            get
            {
                return this._isPagingOperationPending;
            }
            set
            {
                if (this._isPagingOperationPending != value)
                {
                    this._isPagingOperationPending = value;
                    this.PropertyChanged(this, new PropertyChangedEventArgs("IsPagingOperationPending"));
                }
            }
        }

        /// <summary>
        /// MOCK: Gets or sets a value indicating the minimum number of items known to be in the source collection.
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
                    this.PropertyChanged(this, new PropertyChangedEventArgs("ItemCount"));
                }
            }
        }

        /// <summary>
        /// MOCK: Gets or sets the current page index.
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
                    this.PropertyChanged(this, new PropertyChangedEventArgs("PageIndex"));
                }
            }
        }

        /// <summary>
        /// MOCK: Gets or sets the number of items to display on a page.
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
                    this.PropertyChanged(this, new PropertyChangedEventArgs("PageSize"));
                }
            }
        }

        /// <summary>
        /// MOCK: Gets or sets the total number of items in the source collection, 
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
                    this.PropertyChanged(this, new PropertyChangedEventArgs("TotalItemCount"));
                }
            }
        }

        /// <summary>
        /// Adds an entity to the list.
        /// </summary>
        /// <remarks>
        /// The <paramref name="item"/> will be added to the local list, BUT NOT the
        /// <see cref="BackingEntitySet"/>, and a <see cref="INotifyCollectionChanged.CollectionChanged"/>
        /// event will be raised.
        /// </remarks>
        /// <param name="item">The <see cref="Entity"/> to add.</param>
        public void Add(Entity item)
        {
            this._localList.Add(item);
            this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, 0));
        }

        /// <summary>
        /// Will invoke the <see cref="Func{int,bool}"/> provided to this
        /// <see cref="MockPagedEntityList"/>'s constructor, if any, and then
        /// raise the <see cref="PageChanged"/> event.
        /// </summary>
        /// <param name="pageIndex">The page index to use in the invocation.</param>
        /// <returns>The result of the <see cref="Func{int,bool}"/> call when specified, otherwise <c>true</c>.</returns>
        public bool MoveToPage(int pageIndex)
        {
            if (this._moveToPage != null)
            {
                return this._moveToPage(pageIndex);
            }

            this.PageChanged(this, EventArgs.Empty);

            return true;
        }

        /// <summary>
        /// Removes an entity from the list.
        /// </summary>
        /// <remarks>
        /// The <paramref name="item"/> will be removed from the local list BUT NOT the
        /// <see cref="BackingEntitySet"/>, and a <see cref="INotifyCollectionChanged.CollectionChanged"/>
        /// event will be raised.
        /// </remarks>
        /// <param name="item">The <see cref="Entity"/> to add.</param>
        /// <returns>Whether or not the removal was successful.</returns>
        public bool Remove(Entity item)
        {
            this._localList.Remove(item);
            this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, 0));
            return true;
        }

        /// <summary>
        /// Gets an enumerator for this list.
        /// </summary>
        /// <returns>The <see cref="IEnumerator"/> from the local list.</returns>
        public IEnumerator<Entity> GetEnumerator()
        {
            return this._localList.GetEnumerator();
        }

        /// <summary>
        /// Gets an enumerator for this list for the <see cref="IEnumerable"/> interface.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Raised after <see cref="MoveToPage"/> is invoked.
        /// </summary>
        public event EventHandler<EventArgs> PageChanged = delegate { };

        /// <summary>
        /// Raised whenever <see cref="Add"/> or <see cref="Remove"/> is invoked.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged = delegate { };

        /// <summary>
        /// Raised whenever any property values are changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        #endregion
    }
}
