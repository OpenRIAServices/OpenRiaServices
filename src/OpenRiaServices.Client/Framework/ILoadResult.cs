using System.Collections.Generic;

namespace OpenRiaServices.Client
{
    /// <summary>
    /// Interface for non-generic access to <see cref="LoadResult{TEntity}"/>
    /// used by <see cref="LoadOperation"/>
    /// </summary>
    interface ILoadResult
    {
        /// <summary>
        /// Gets all the entities loaded by the operation, including any
        /// entities referenced by the top level entities. The collection returned implements
        /// <see cref="System.Collections.Specialized.INotifyCollectionChanged"/>.
        /// </summary>
        IReadOnlyCollection<Entity> AllEntities { get; }

        /// <summary>
        ///  /// Gets all the top level entities loaded by the operation. The collection returned implements
        /// <see cref="System.Collections.Specialized.INotifyCollectionChanged"/>.
        /// </summary>
        IReadOnlyCollection<Entity> Entities { get; }

        /// <summary>
        /// Gets the total server entity count for the query used by this operation. Automatic
        /// evaluation of the total server entity count requires the property <see cref="OpenRiaServices.Client.EntityQuery.IncludeTotalCount"/>
        /// on the query for the load operation to be set to <c>true</c>.
        /// </summary>
        int TotalEntityCount { get; }
    }
}
