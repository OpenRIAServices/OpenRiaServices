using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRiaServices.Server
{
    /// <summary>
    /// Provides a query method with the filtering, ordering, and paging requested by the client.
    /// </summary>
    /// <typeparam name="T">The entity type returned by the query method.</typeparam>
    public sealed class ClientQuery<T>
    {
        private readonly IQueryable _query;
        private readonly int _resultLimit;
        private IQueryable<T> _totalCountQuery;
        private int _applyCount;
        private int? _totalCount;

        internal ClientQuery(IQueryable query, bool includeTotalCount, int resultLimit)
        {
            _query = query;
            _resultLimit = resultLimit;
            IncludeTotalCount = includeTotalCount;
        }

        /// <summary>
        /// Gets a value indicating whether the client requested the total number of matching
        /// entities before paging is applied.
        /// </summary>
        public bool IncludeTotalCount { get; }

        /// <summary>
        /// Applies the client filtering, ordering, and paging to <paramref name="source"/>.
        /// </summary>
        /// <remarks>
        /// This method can be called multiple times when <see cref="IncludeTotalCount"/> is
        /// <see langword="false"/> or after <see cref="SetTotalCount(int)"/> has supplied the
        /// total count explicitly.
        /// </remarks>
        /// <param name="source">The query to which the client query should be applied.</param>
        /// <returns>The composed query.</returns>
        public IQueryable<T> ApplyTo(IQueryable<T> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (IncludeTotalCount && !_totalCount.HasValue && _applyCount != 0)
            {
                throw new InvalidOperationException("ClientQuery.ApplyTo can only be called once when an automatic total count is requested. Call SetTotalCount before applying multiple queries.");
            }

            IQueryable<T> composedQuery = source;
            if (_query != null)
            {
                composedQuery = (IQueryable<T>)QueryComposer.Compose(source, _query);
            }

            _applyCount++;

            if (IncludeTotalCount && !_totalCount.HasValue)
            {
                bool hasPaging = QueryComposer.TryComposeWithoutPaging(composedQuery, out IQueryable countQuery);
                if (!hasPaging)
                {
                    countQuery = composedQuery;
                }

                if (hasPaging || _resultLimit > 0)
                {
                    _totalCountQuery = (IQueryable<T>)countQuery;
                }
            }

            if (_resultLimit > 0)
            {
                composedQuery = composedQuery.Take(_resultLimit);
            }

            return composedQuery;
        }

        /// <summary>
        /// Applies the client filtering, ordering, and paging to an in-memory sequence.
        /// </summary>
        /// <param name="source">The sequence to which the client query should be applied.</param>
        /// <returns>The composed sequence.</returns>
        public IEnumerable<T> ApplyTo(IEnumerable<T> source)
        {
            ArgumentNullException.ThrowIfNull(source);
            return ApplyTo(source.AsQueryable());
        }

        /// <summary>
        /// Supplies the total number of matching entities before paging is applied.
        /// </summary>
        /// <remarks>
        /// Supplying the total count disables automatic count-query tracking and permits
        /// multiple calls to <see cref="ApplyTo(IQueryable{T})"/>.
        /// </remarks>
        /// <param name="totalCount">The non-negative total number of matching entities.</param>
        public void SetTotalCount(int totalCount)
        {
            if (totalCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalCount));
            }

            _totalCount = totalCount;
            _totalCountQuery = null;
        }

        internal int ApplyCount => _applyCount;

        internal int? TotalCount => _totalCount;

        internal IQueryable<T> TotalCountQuery => _totalCountQuery;
    }

    internal static class ClientQuery
    {
        internal static bool IsClientQueryType(Type type) =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ClientQuery<>);
    }
}
