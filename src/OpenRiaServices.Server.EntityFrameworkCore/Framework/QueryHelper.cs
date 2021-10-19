using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.EntityFramework
{
    static class QueryHelper
    {
        public static ValueTask<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        {
            // EF will throw if provider is not a IDbAsyncQueryProvider
            if (query.Provider is IDbAsyncQueryProvider)
                return new ValueTask<int>(query.CountAsync(cancellationToken));
            else
                return new ValueTask<int>(query.Count());
        }

        public static async ValueTask<IReadOnlyCollection<T>> EnumerateAsyncEnumerable<T>(IDbAsyncEnumerable<T> asyncEnumerable, int estimatedResultCount, CancellationToken cancellationToken)
        {
            using (var enumerator = asyncEnumerable.GetAsyncEnumerator())
            {
                List<T> result = new List<T>(capacity: estimatedResultCount);
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    result.Add(enumerator.Current);
                }

                return result;
            }
        }
    }
}
