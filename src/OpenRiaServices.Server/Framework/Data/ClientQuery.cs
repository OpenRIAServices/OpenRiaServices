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
        private readonly Func<IQueryable<T>, IQueryable<T>> _applyQuery;
        private IQueryable<T> _totalCountQuery;
        private bool _hasCapturedTotalCountQuery;
        private int? _totalCount;

        /// <summary>
        /// Initializes a new instance with a function that applies a query to the source.
        /// </summary>
        /// <param name="applyQuery">A function that applies the client query to a source.</param>
        public ClientQuery(Func<IQueryable<T>, IQueryable<T>> applyQuery)
            : this(applyQuery, includeTotalCount: false)
        {
        }

        internal ClientQuery(IQueryable query, bool includeTotalCount)
            : this(source => query == null ? source : (IQueryable<T>)QueryComposer.Compose(source, query), includeTotalCount)
        {
        }

        private ClientQuery(Func<IQueryable<T>, IQueryable<T>> applyQuery, bool includeTotalCount)
        {
            ArgumentNullException.ThrowIfNull(applyQuery);
            _applyQuery = applyQuery;
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
        /// This method can be called multiple times. It does not capture the total count.
        /// </remarks>
        /// <param name="source">The query to which the client query should be applied.</param>
        /// <returns>The composed query.</returns>
        public IQueryable<T> ApplyTo(IQueryable<T> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return ApplyClientQuery(source);
        }

        /// <summary>
        /// Applies the client query and captures its unpaged result count.
        /// </summary>
        /// <param name="source">The query to which the client query should be applied.</param>
        /// <returns>The composed query.</returns>
        /// <exception cref="InvalidOperationException">A query count has already been captured.</exception>
        public IQueryable<T> ApplyToAndCaptureTotalCount(IQueryable<T> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            IQueryable<T> composedQuery = ApplyClientQuery(source);
            IQueryable countQuery = QueryComposer.TryComposeWithoutPaging(composedQuery, out IQueryable unpagedQuery)
                ? unpagedQuery
                : composedQuery;
            SetTotalCountFromQuery((IQueryable<T>)countQuery);
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
        /// Supplies a query that returns the matching entities before paging is applied.
        /// </summary>
        /// <param name="source">A query for all matching entities without client paging.</param>
        /// <exception cref="InvalidOperationException">A query count has already been captured.</exception>
        public void SetTotalCountFromQuery(IQueryable<T> source)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (_hasCapturedTotalCountQuery)
            {
                throw new InvalidOperationException("The total count can only be captured from a query once.");
            }

            _hasCapturedTotalCountQuery = true;
            _totalCountQuery = source;
            _totalCount = null;
        }

        /// <summary>
        /// Supplies the total number of matching entities before paging is applied.
        /// </summary>
        /// <remarks>
        /// Multiple calls replace the previously supplied count.
        /// </remarks>
        /// <param name="totalCount">The non-negative total number of matching entities.</param>
        public void SetTotalCount(int totalCount)
        {
#if NETFRAMEWORK
            if (totalCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalCount));
            }
#else
            ArgumentOutOfRangeException.ThrowIfNegative(totalCount);
#endif

            _totalCount = totalCount;
            _totalCountQuery = null;
        }

        internal int? TotalCount => _totalCount;

        internal IQueryable<T> TotalCountQuery => _totalCountQuery;

        private IQueryable<T> ApplyClientQuery(IQueryable<T> source)
        {
            IQueryable<T> query = _applyQuery(source);
            ArgumentNullException.ThrowIfNull(query);
            return query;
        }

    }

    internal static class ClientQuery
    {
        internal static bool IsClientQueryType(Type type) =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ClientQuery<>);
    }
}
