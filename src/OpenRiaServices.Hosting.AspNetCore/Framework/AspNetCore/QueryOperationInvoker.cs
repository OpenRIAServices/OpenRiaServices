using Microsoft.AspNetCore.Http;
using OpenRiaServices;
using OpenRiaServices.Hosting;
using OpenRiaServices.Hosting.AspNetCore;
using OpenRiaServices.Hosting.Wcf;
using OpenRiaServices.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

class QueryOperationInvoker<TEntity> : OperationInvoker
{
    public QueryOperationInvoker(DomainOperationEntry operation, SerializationHelper serializationHelper)
            : base(operation, DomainOperationType.Query, serializationHelper, GetRespponseSerializer(operation, serializationHelper))
    {
    }

    private static DataContractSerializer GetRespponseSerializer(DomainOperationEntry operation, SerializationHelper serializationHelper)
    {
        var knownTypes = DomainServiceDescription.GetDescription(operation.DomainServiceType).EntityKnownTypes;
        return serializationHelper.GetSerializer(typeof(QueryResult<TEntity>));
    }

    public override async Task Invoke(HttpContext context)
    {
        DomainService domainService = CreateDomainService(context);

        // TODO: consider using ArrayPool<object>.Shared in future
        object[] inputs;
        ServiceQuery serviceQuery;
        if (context.Request.Method == "GET")
        {
            inputs = GetParametersFromUri(context);

            QueryAttribute queryAttribute = (QueryAttribute)this.operation.OperationAttribute;
            serviceQuery = queryAttribute.IsComposable ? GetServiceQuery(context.Request) : null;
        }
        else // POST
        {
            if (context.Request.ContentType != "application/msbin1")
            {
                context.Response.StatusCode = 400; // maybe 406 / System.Net.HttpStatusCode.NotAcceptable
                return;
            }

            (serviceQuery, inputs) = await ReadParametersFromBodyAsync(context);
        }

        QueryResult<TEntity> result;
        try
        {
            //SetOutputCachingPolicy(httpContext, this.operation);
            result = await QueryProcessor.ProcessAsync<TEntity>(domainService, this.operation, inputs, serviceQuery);
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
            //ClearOutputCachingPolicy(httpContext);
            await WriteError(context, ex, hideStackTrace: domainService.GetDisableStackTraces());
            return;
        }

        if (result.ValidationErrors != null && result.ValidationErrors.Any())
            await WriteError(context, result.ValidationErrors, hideStackTrace: true);
        else
            await WriteResponse(context, result);
    }

    // FROM DomainServiceWebHttpBehavior
    /// <summary>
    /// This method returns a ServiceQuery for the specified URL and query string.
    /// <remarks>
    /// This method must ensure that the original ordering of the query parts is maintained
    /// in the results. We want to do this without doing any custom URL parsing. The approach
    /// taken is to use HttpUtility to parse the query string, and from those results we search
    /// in the full URL for the relative positioning of those elements.
    /// </remarks>
    /// </summary>
    /// <param name="queryString">The query string portion of the URL</param>
    /// <param name="fullRequestUrl">The full request URL</param>
    /// <returns>The corresponding ServiceQuery</returns>
    internal static ServiceQuery GetServiceQuery(HttpRequest httpRequest)
    {
        var queryPartCollection = httpRequest.Query;
        string fullRequestUrl = httpRequest.QueryString.Value;
        bool includeTotalCount = false;

        // Reconstruct a list of all key/value pairs
        List<string> queryParts = new List<string>();
        foreach (string queryPart in queryPartCollection.Keys)
        {
            if (queryPart == null || !queryPart.StartsWith("$", StringComparison.Ordinal))
            {
                // not a special query string
                continue;
            }

            if (queryPart.Equals("$includeTotalCount", StringComparison.OrdinalIgnoreCase))
            {
                string value = queryPartCollection[queryPart].First();
                Boolean.TryParse(value, out includeTotalCount);
                continue;
            }

            foreach (string value in queryPartCollection[queryPart])
            {
                queryParts.Add(queryPart + "=" + value);
            }
        }

        string decodedQueryString = /*HttpUtility.UrlDecode*/ Uri.UnescapeDataString(fullRequestUrl);

        // For each query part, find all occurrences of it in the Url (could be duplicates)
        List<KeyValuePair<string, int>> keyPairIndicies = new List<KeyValuePair<string, int>>();
        foreach (string queryPart in queryParts.Distinct())
        {
            int idx;
            int endIdx = 0;
            while (((idx = decodedQueryString.IndexOf(queryPart, endIdx, StringComparison.Ordinal)) != -1) &&
                    (endIdx < decodedQueryString.Length - 1))
            {
                // We found a match, however, we must ensure that the match is exact. For example,
                // The string "$take=1" will be found twice in query string "?$take=10&$orderby=Name&$take=1",
                // but the first match should be discarded. Therefore, before adding the match, we ensure
                // the next character is EOS or the param separator '&'.
                endIdx = idx + queryPart.Length - 1;
                if ((endIdx == decodedQueryString.Length - 1) ||
                    (endIdx < decodedQueryString.Length - 1 && (decodedQueryString[endIdx + 1] == '&')))
                {
                    keyPairIndicies.Add(new KeyValuePair<string, int>(queryPart, idx));
                }
            }
        }

        // create the list of ServiceQueryParts in order, ordered by
        // their location in the query string
        IEnumerable<string> orderedParts = keyPairIndicies.OrderBy(p => p.Value).Select(p => p.Key);
        IEnumerable<ServiceQueryPart> serviceQueryParts =
            from p in orderedParts
            let idx = p.IndexOf('=')
            select new ServiceQueryPart(p.Substring(1, idx - 1), p.Substring(idx + 1));

        ServiceQuery serviceQuery = new ServiceQuery()
        {
            QueryParts = serviceQueryParts.ToList(),
            IncludeTotalCount = includeTotalCount
        };

        return serviceQuery;
    }
}
