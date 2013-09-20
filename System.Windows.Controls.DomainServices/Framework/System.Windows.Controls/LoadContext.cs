namespace System.Windows.Controls
{
    /// <summary>
    /// Stores the characteristics of a load operation 
    /// </summary>
    internal class LoadContext
    {
        /// <summary>
        /// Value of the <see cref="System.Windows.Controls.DomainDataSource.LoadSize" /> property for the load operation,
        /// at the time the load operation was initiated.
        /// </summary>
        internal int LoadSize { get; set; }

        /// <summary>
        /// <see cref="LoadType" /> of the load operation.
        /// </summary>
        internal LoadType LoadType { get; set; }

        /// <summary>
        /// Value of the <see cref="System.Windows.Controls.DomainDataSource.PageSize" /> property for the load operation,
        /// at the time the load operation was initiated.
        /// </summary>
        internal int PageSize { get; set; }

        /// <summary>
        /// Determines whether there is a pending page move. When <c>true</c>, the internal
        /// <see cref="PagedEntityCollection" /> needs to notify the
        /// <see cref="PagedEntityCollectionView" /> of the page move's result.
        /// </summary>
        internal bool RaisePageChanged { get; set; }

        /// <summary>
        /// Potential page index requested via <see cref="PagedEntityCollection.MoveToPage" />.
        /// </summary>
        internal int RequestedPageIndex { get; set; }

        /// <summary>
        /// Value of the <see cref="PagedEntityCollection.StartPageIndex" /> property for the load operation.
        /// </summary>
        internal int StartPageIndex { get; set; }

        /// <summary>
        /// Indicates whether or not the <see cref="LoadType"/> represents an initial load.
        /// </summary>
        /// <remarks>
        /// An initial load is one that results in going back to the first page because a new query was
        /// invoked, such as filtering, sorting, or grouping.  But paging operations and incremental
        /// loads do not qualify.
        /// </remarks>
        /// <value>
        /// <c>true</c> when the <see cref="LoadType"/> is one that represents an initial load
        /// (<see cref="System.Windows.Controls.LoadType.LoadAll"/>, <see cref="System.Windows.Controls.LoadType.LoadFirstItems"/>,
        /// or <see cref="System.Windows.Controls.LoadType.LoadFirstPages"/>, otherwise <c>false</c>.
        /// </value>
        internal bool IsInitialLoad
        {
            get
            {
                return (this.LoadType == LoadType.LoadAll || this.LoadType == LoadType.LoadFirstItems || this.LoadType == LoadType.LoadFirstPages);
            }
        }
    }
}
