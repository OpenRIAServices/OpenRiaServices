using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace OpenRiaServices.EntityFrameworkCore
{
    static class QueryHelper
    {
        public static ValueTask<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        {
            // EF will throw if provider is not a IDbAsyncQueryProvider
            if (query.Provider is IAsyncQueryProvider)
                return new ValueTask<int>(query.CountAsync(cancellationToken));
            else
                return new ValueTask<int>(query.Count());
        }
    }
}
