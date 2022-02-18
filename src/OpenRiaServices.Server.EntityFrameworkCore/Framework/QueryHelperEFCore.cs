﻿using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System.Collections.Generic;
using System;

namespace OpenRiaServices.EntityFrameworkCore
{
    static class QueryHelperEFCore
    {
        public static ValueTask<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        {
            // EF will throw if provider is not a IDbAsyncQueryProvider
            if (query.Provider is IAsyncQueryProvider)
                return new ValueTask<int>(query.CountAsync(cancellationToken));
            else
                return new ValueTask<int>(query.Count());
        }

        internal static async ValueTask<IReadOnlyCollection<T>> EnumerateAsyncEnumerable<T>(IAsyncEnumerable<T> asyncEnumerable, int estimatedResultCount, CancellationToken cancellationToken)
        {
            await using (var enumerator = asyncEnumerable.GetAsyncEnumerator(cancellationToken))
            {
                List<T> result = new List<T>(capacity: estimatedResultCount);
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    result.Add(enumerator.Current);
                }

                return result;
            }
        }
    }
}
