using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Query;

namespace OpenRiaServices.Server.EntityFrameworkCore
{
    static class QueryHelper
    {
        public static ValueTask<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        {
            // EF will throw if provider is not a IDbAsyncQueryProvider
#if NET6_0
            if (query.Provider is IAsyncQueryProvider)
                return new ValueTask<int>(query.CountAsync(cancellationToken));
#else
            if (query.Provider is IAsyncQueryProvider)
                return new ValueTask<int>(query.CountAsync(cancellationToken));
#endif
            else
                return new ValueTask<int>(query.Count());
        }

        internal static async ValueTask<IReadOnlyCollection<T>> EnumerateAsyncEnumerable<T>(IAsyncEnumerable<T> asyncEnumerable, int estimatedResultCount, CancellationToken cancellationToken)
        {
            var result = new List<T>(capacity: estimatedResultCount);
            await foreach (var item in asyncEnumerable.ConfigureAwait(false).WithCancellation(cancellationToken))
            {
                result.Add(item);
            }

            return result;
        }
    }
}
