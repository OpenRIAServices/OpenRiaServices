using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.Client.DomainClients.Http
{
    /// <summary>
    /// Base class for <see cref="DomainClient"/>s communicating with server over <see cref="HttpClient"/>.
    /// </summary>
    abstract class HttpDomainClient : DomainClient
    {
        /// ResponseContentRead seems to give better results on .Net framework for local network with low latency and high bandwidth
        /// This is probably due to less kernel time
        /// It would be good to do measurements on .net core as well as over the internet
        /// - response headers read should theoretically give lower latency since result can be
        /// deserialized as content is received
        private const HttpCompletionOption DefaultHttpCompletionOption = HttpCompletionOption.ResponseContentRead;
        private static readonly Task<HttpResponseMessage> s_mustSendQueryInBody = Task.FromResult<HttpResponseMessage>(null);
        private static readonly ConcurrentDictionary<(Type serviceInterface, string operationName), MethodParameters> s_methodParametersCache = new ConcurrentDictionary<(Type serviceInterface, string operationName), MethodParameters>();

        private readonly OpenRiaServices.Client.DomainClients.HttpDomainClientFactory _factory;
        private readonly Type _serviceInterface;

        /// <inheritdoc/>
        public override bool SupportsCancellation => true;

        private protected HttpClient HttpClient { get; }

        private protected HttpDomainClient(HttpClient httpClient, Type serviceInterface, OpenRiaServices.Client.DomainClients.HttpDomainClientFactory factory)
        {
            ArgumentNullException.ThrowIfNull(httpClient);
            ArgumentNullException.ThrowIfNull(serviceInterface);
            ArgumentNullException.ThrowIfNull(factory);

            HttpClient = httpClient;
            _serviceInterface = serviceInterface;
            _factory = factory;
        }

        private protected abstract Task<HttpResponseMessage> PostAsync(string operationName, IDictionary<string, object> parameters, List<ServiceQueryPart> queryOptions, CancellationToken cancellationToken);
        private protected abstract Task<HttpResponseMessage> QueryAsync(string operationName, IDictionary<string, object> parameters, List<ServiceQueryPart> queryOptions, CancellationToken cancellationToken);
        private protected abstract Task<object> ReadResponseAsync(HttpResponseMessage response, string operationName, Type returnType);

        private protected MethodParameters GetMethodParameters(string operationName)
        {
            return s_methodParametersCache.GetOrAdd((_serviceInterface, operationName), static key => new MethodParameters(key.serviceInterface, key.operationName));
        }

        private protected string GetParameterValueAsString(string operationName, string parameterName, object parameterValue)
        {
            var parameterType = GetMethodParameters(operationName).GetTypeForMethodParameter(parameterName);
            return WebQueryStringConverter.ConvertValueToString(parameterValue, parameterType);
        }

        #region Invoke/Query/Submit Methods

        protected override async Task<InvokeCompletedResult> InvokeAsyncCore(InvokeArgs invokeArgs, CancellationToken cancellationToken)
        {
            var response = await ExecuteRequestAsync(invokeArgs.OperationName, invokeArgs.HasSideEffects, invokeArgs.Parameters, queryOptions: null, cancellationToken: cancellationToken)
                 .ConfigureAwait(false);

            IEnumerable<ValidationResult> validationErrors = null;
            object returnValue = null;

            try
            {
                returnValue = await ReadResponseAsync(response, invokeArgs.OperationName, invokeArgs.ReturnType)
                     .ConfigureAwait(false);
            }
            catch (FaultException<DomainServiceFault> fe)
            {
                if (fe.Detail.OperationErrors != null)
                {
                    validationErrors = fe.Detail.GetValidationErrors();
                }
                else
                {
                    throw GetExceptionFromServiceFault(fe.Detail);
                }
            }

            return new InvokeCompletedResult(returnValue, validationErrors ?? Enumerable.Empty<ValidationResult>());
        }

        protected override async Task<SubmitCompletedResult> SubmitAsyncCore(EntityChangeSet changeSet, CancellationToken cancellationToken)
        {
            const string operationName = "SubmitChanges";
            var entries = changeSet.GetChangeSetEntries().ToList();
            var parameters = new Dictionary<string, object>() {
                     {"changeSet", entries}
                };

            var response = await ExecuteRequestAsync(operationName, hasSideEffects: true, parameters: parameters, queryOptions: null, cancellationToken: cancellationToken)
                 .ConfigureAwait(false);

            try
            {
                var returnValue = (IEnumerable<ChangeSetEntry>)await ReadResponseAsync(response, operationName, typeof(IEnumerable<ChangeSetEntry>))
                     .ConfigureAwait(false);
                return new SubmitCompletedResult(changeSet, returnValue ?? Enumerable.Empty<ChangeSetEntry>());
            }
            catch (FaultException<DomainServiceFault> fe)
            {
                throw GetExceptionFromServiceFault(fe.Detail);
            }
        }

        protected override Task<QueryCompletedResult> QueryAsyncCore(EntityQuery query, CancellationToken cancellationToken)
        {
            List<ServiceQueryPart> queryOptions = query.Query != null ? QuerySerializer.Serialize(query.Query) : null;

            if (query.IncludeTotalCount)
            {
                queryOptions = queryOptions ?? new List<ServiceQueryPart>();
                queryOptions.Add(new ServiceQueryPart()
                {
                    QueryOperator = "includeTotalCount",
                    Expression = "True"
                });
            }

            var responseTask = ExecuteRequestAsync(query.QueryName, query.HasSideEffects, query.Parameters, queryOptions, cancellationToken);
            return QueryAsyncCoreContinuation();

            // Move async statemachine to separate func so that
            // any exception from query parsing is thrown immediately and not wrapped in task
            async Task<QueryCompletedResult> QueryAsyncCoreContinuation()
            {
                var response = await responseTask.ConfigureAwait(false);
                IEnumerable<ValidationResult> validationErrors = null;
                try
                {
                    var queryType = typeof(QueryResult<>).MakeGenericType(query.EntityType);
                    var queryResult = (QueryResult)await ReadResponseAsync(response, query.QueryName, queryType)
                         .ConfigureAwait(false);
                    if (queryResult != null)
                    {
                        return new QueryCompletedResult(
                             queryResult.GetRootResults().Cast<Entity>(),
                             queryResult.GetIncludedResults().Cast<Entity>(),
                             queryResult.TotalCount,
                             Enumerable.Empty<ValidationResult>());
                    }
                }
                catch (FaultException<DomainServiceFault> fe)
                {
                    if (fe.Detail.OperationErrors != null)
                    {
                        validationErrors = fe.Detail.GetValidationErrors();
                    }
                    else
                    {
                        throw GetExceptionFromServiceFault(fe.Detail);
                    }
                }

                return new QueryCompletedResult(
                          Enumerable.Empty<Entity>(),
                          Enumerable.Empty<Entity>(),
                     /* totalCount */ 0,
                          validationErrors ?? Enumerable.Empty<ValidationResult>());
            }
        }
        #endregion

        #region Private methods for making requests
        /// <summary>
        /// Invokes a web request for the operation <paramref name="operationName"/>
        /// </summary>
        /// <param name="operationName">name of operation</param>
        /// <param name="hasSideEffects">if set to <c>true</c> then the request will always be a POST operation.</param>
        /// <param name="parameters">The parameters to the server method, or <c>null</c> if no parameters.</param>
        /// <param name="queryOptions">The query options, or <c>null</c>.</param>
        /// <param name="cancellationToken"></param>
        private Task<HttpResponseMessage> ExecuteRequestAsync(string operationName, bool hasSideEffects, IDictionary<string, object> parameters,
             List<ServiceQueryPart> queryOptions,
             CancellationToken cancellationToken)
        {
            if (hasSideEffects)
                return PostAsync(operationName, parameters, queryOptions, cancellationToken);

            var response = GetAsync(operationName, parameters, queryOptions, cancellationToken);
            // GET returned the sentinel value, meaning the query string is too long - fall back to POST or QUERY
            if (ReferenceEquals(response, s_mustSendQueryInBody))
            {
                response = _factory.UseQueryHttpMethod
                    ? QueryAsync(operationName, parameters, queryOptions, cancellationToken)
                    : PostAsync(operationName, parameters, queryOptions, cancellationToken);
            }

            return response;
        }

        /// <summary>
        /// Initiates a GET request for the given operation and return the server response (as a task).
        /// </summary>
        /// <param name="operationName">Name of operation</param>
        /// <param name="parameters">The parameters to the server method, or <c>null</c> if no parameters.</param>
        /// <param name="queryOptions">The query options if any.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>A task for the pending operation, or <c>null</c> if operation was not attempted</returns>
        private Task<HttpResponseMessage> GetAsync(string operationName, IDictionary<string, object> parameters, List<ServiceQueryPart> queryOptions, CancellationToken cancellationToken)
        {
            int i = 0;
            var uriBuilder = new StringBuilder(256);
            uriBuilder.Append(operationName);

            // Parameters
            if (parameters != null && parameters.Count > 0)
            {
                foreach (var param in parameters)
                {
                    if (param.Value != null)
                    {
                        uriBuilder.Append(i++ == 0 ? '?' : '&');
                        uriBuilder.Append(Uri.EscapeDataString(param.Key));
                        uriBuilder.Append('=');
                        var value = GetParameterValueAsString(operationName, param.Key, param.Value);
                        uriBuilder.Append(Uri.EscapeDataString(value));
                    }
                }
            }

            // Query options
            if (queryOptions != null && queryOptions.Count > 0)
            {
                foreach (var queryPart in queryOptions)
                {
                    uriBuilder.Append(i++ == 0 ? "?$" : "&$");
                    uriBuilder.Append(queryPart.QueryOperator);
                    uriBuilder.Append('=');
                    // Query strings seems to be double encoded
                    uriBuilder.Append(Uri.EscapeDataString(Uri.EscapeDataString(queryPart.Expression)));
                }
            }

            var uri = uriBuilder.ToString();

            // Switch to POST/QUERY if uri becomes too long based on configured max query string length
            // we can do so by returning the special null task s_mustSendQueryInBody
            // * https://docs.microsoft.com/en-us/iis/configuration/system.webserver/security/requestfiltering/requestlimits/
            // * https://docs.microsoft.com/en-us/dotnet/api/system.web.configuration.httpruntimesection.maxurllength?view=netframework-4.8#system-web-configuration-httpruntimesection-maxurllength
            // - default maximum query string length in IIS is 2048 bytes
            // - MaxUrlLength is 260 per default, but we don't check it since POST will get same length
            // - maxUrl defaults to 4096 bytes, but we assume it will not be an issue since we limit the query string length
            var maxQueryStringLength = _factory.MaxQueryStringLength;
            if (uri.Length - operationName.Length > maxQueryStringLength) // uri contains query + operationName, so subtract operationName to only get query string length
                return s_mustSendQueryInBody;

            return HttpClient.GetAsync(uri, DefaultHttpCompletionOption, cancellationToken);
        }
        #endregion

        /// <summary>
        /// Constructs an exception based on a service fault.
        /// </summary>
        /// <param name="serviceFault">The fault received from a service.</param>
        /// <returns>The constructed exception.</returns>
        private static Exception GetExceptionFromServiceFault(DomainServiceFault serviceFault)
        {
            // Status was OK but there still was a server error. We need to transform
            // the error into the appropriate client exception
            if (serviceFault.IsDomainException)
            {
                return new DomainException(serviceFault.ErrorMessage, serviceFault.ErrorCode, serviceFault.StackTrace);
            }
            else if (serviceFault.ErrorCode == 400)
            {
                return new DomainOperationException(serviceFault.ErrorMessage, OperationErrorStatus.NotSupported, serviceFault.ErrorCode, serviceFault.StackTrace);
            }
            else if (serviceFault.ErrorCode is 401 or 403)
            {
                return new DomainOperationException(serviceFault.ErrorMessage, OperationErrorStatus.Unauthorized, serviceFault.ErrorCode, serviceFault.StackTrace);
            }
            else
            {
                // for anything else: map to ServerError
                return new DomainOperationException(serviceFault.ErrorMessage, OperationErrorStatus.ServerError, serviceFault.ErrorCode, serviceFault.StackTrace);
            }
        }
    }
}
