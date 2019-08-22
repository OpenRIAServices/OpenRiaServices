using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using OpenRiaServices.DomainServices.Client;

namespace OpenRiaServices.Controls
{
    /// <summary>
    /// An interface to define how paging can be layered on top of an <see cref="EntitySet"/>.
    /// </summary>
    internal interface IPagedEntityList : IEnumerable<Entity>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        /// <summary>
        /// The <see cref="EntitySet"/> that is used as the backing store
        /// for the entities within this <see cref="IPagedEntityList"/>.
        /// </summary>
        /// <remarks>
        /// There may be additional entities in the backing entity set that
        /// are not included in this <see cref="IPagedEntityList"/>.
        /// </remarks>
        EntitySet BackingEntitySet { get; }

        /// <summary>
        /// Gets the type of <see cref="Entity"/> for this list.  This can
        /// differ from the <see cref="EntitySet.EntityType"/> from the
        /// <see cref="BackingEntitySet"/> in inheritance scenarios.
        /// </summary>
        Type EntityType { get; }

        /// <summary>
        /// Gets a value indicating that paging is enabled but the data hasn't yet been loaded.
        /// </summary>
        bool IsPagingOperationPending { get; }

        /// <summary>
        /// Gets a value indicating the minimum number of items known to be in the source collection.
        /// </summary>
        int ItemCount { get; }

        /// <summary>
        /// Gets the current page index.
        /// </summary>
        int PageIndex { get; }

        /// <summary>
        /// Gets the number of items to display on a page.
        /// </summary>
        int PageSize { get; set; }

        /// <summary>
        /// Gets the total number of items in the source collection, 
        /// or -1 if that value is unknown.
        /// </summary>
        int TotalItemCount { get; }

        /// <summary>
        /// Adds an entity to the list.
        /// </summary>
        /// <param name="item">The <see cref="Entity"/> to add.</param>
        void Add(Entity item);

        /// <summary>
        /// Invokes the operation for moving to a new <paramref name="pageIndex"/>.
        /// </summary>
        /// <param name="pageIndex">Requested page index</param>
        /// <returns><c>true</c> if the page move was initiated, otherwise <c>false</c>.</returns>
        bool MoveToPage(int pageIndex);

        /// <summary>
        /// Removes an entity from the list.
        /// </summary>
        /// <param name="item">The <see cref="Entity"/> to add.</param>
        /// <returns>Whether or not the removal was successful.</returns>
        bool Remove(Entity item);

        /// <summary>
        /// Raised when a page change has completed.
        /// </summary>
        event EventHandler<EventArgs> PageChanged;
    }
}