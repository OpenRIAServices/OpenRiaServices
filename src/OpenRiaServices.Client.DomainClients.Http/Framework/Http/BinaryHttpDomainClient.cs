using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.Client.DomainClients.Http
{
    // Pass in HttpDomainClientFactory to ctor,
    // pass in HttpClient ?
    internal partial class BinaryHttpDomainClient : DomainClient
    {
        /// ResponseContentRead seems to give better results on .Net framework for local network with low latency and high bandwidth
        /// This is probably due to less kernel time
        /// It would be good to do measurements on .net core as well as over the internet
        /// - response headers read should teoretically give lower latency since result can be 
        /// deserialized as content is received
        private const HttpCompletionOption DefaultHttpCompletionOption = HttpCompletionOption.ResponseContentRead;
        private static readonly DataContractSerializer s_faultSerializer = new DataContractSerializer(typeof(DomainServiceFault));
        private static readonly Task<HttpResponseMessage> s_skipGetUsePostInstead = Task.FromResult<HttpResponseMessage>(null);
        private static readonly Dictionary<Type, BinaryHttpDomainClientSerializationHelper> s_globalCacheHelpers = new Dictionary<Type, BinaryHttpDomainClientSerializationHelper>();
        
        private readonly BinaryHttpDomainClientSerializationHelper _localCacheHelper;

        public override bool SupportsCancellation => true;

        public BinaryHttpDomainClient(HttpClient httpClient, Type serviceInterface)
        {

            HttpClient = httpClient;
            lock(s_globalCacheHelpers)
            {
                if (!s_globalCacheHelpers.TryGetValue(serviceInterface, out _localCacheHelper))
                {
                    _localCacheHelper = new BinaryHttpDomainClientSerializationHelper(serviceInterface);
                    s_globalCacheHelpers.Add(serviceInterface, _localCacheHelper);
                }
            }
        }

        HttpClient HttpClient { get; set; }

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
                var response = await responseTask;
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
            Task<HttpResponseMessage> response = s_skipGetUsePostInstead;

            if (!hasSideEffects)
            {
                response = GetAsync(operationName, parameters, queryOptions, cancellationToken);
            }
            // It is a POST, or GET returned null (maybe due to too large request uri)
            if (ReferenceEquals(response, s_skipGetUsePostInstead))
            {
                response = PostAsync(operationName, parameters, queryOptions, cancellationToken);
            }

            return response;
        }

        /// <summary>
        /// Initiates a POST request for the given operation and return the server respose (as a task).
        /// </summary>
        /// <param name="operationName">Name of operation</param>
        /// <param name="parameters">The parameters to the server method, or <c>null</c> if no parameters.</param>
        /// <param name="queryOptions">The query options if any.</param>
        /// <param name="cancellationToken"></param>
        private Task<HttpResponseMessage> PostAsync(string operationName, IDictionary<string, object> parameters, List<ServiceQueryPart> queryOptions, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, operationName)
            {
                Content = new BinaryXmlContent(this, operationName, parameters, queryOptions),
            };

            return HttpClient.SendAsync(request, DefaultHttpCompletionOption, cancellationToken);
        }

        /// <summary>
        /// Initiates a GET request for the given operation and return the server respose (as a task).
        /// </summary>
        /// <param name="operationName">Name of operation</param>
        /// <param name="parameters">The parameters to the server method, or <c>null</c> if no parameters.</param>
        /// <param name="queryOptions">The query options if any.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>A task for the pending operation, or <c>null</c> if operation was not attempted</returns>
        private Task<HttpResponseMessage> GetAsync(string operationName, IDictionary<string, object> parameters, IList<ServiceQueryPart> queryOptions, CancellationToken cancellationToken)
        {
            int i = 0;
            var uriBuilder = new StringBuilder(256);
            uriBuilder.Append(operationName);

            // Parameters
            if (parameters != null && parameters.Count > 0)
            {
                foreach (var param in parameters)
                {
                    // TODO: We nned to look at using the interface instead
                    // This is sort of a hack, we should ideally get the parameterType
                    // null string should be emtpy string, null for other types should be "null"
                    if (param.Value != null)
                    {
                        uriBuilder.Append(i++ == 0 ? '?' : '&');
                        uriBuilder.Append(Uri.EscapeDataString(param.Key));
                        uriBuilder.Append('=');
                        var value = WebQueryStringConverter.ConvertValueToString(param.Value, param.Value.GetType());
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
                    uriBuilder.Append("=");
                    // Query strings seems to be double encoded
                    uriBuilder.Append(Uri.EscapeDataString(Uri.EscapeDataString(queryPart.Expression)));
                }
            }

            var uri = uriBuilder.ToString();

            // Switch to POST if uri becomes to long based on default IIS hosting settings
            // we can do so by returning the special null task s_skipGetUsePostInstead
            // * https://docs.microsoft.com/en-us/iis/configuration/system.webserver/security/requestfiltering/requestlimits/
            // * https://docs.microsoft.com/en-us/dotnet/api/system.web.configuration.httpruntimesection.maxurllength?view=netframework-4.8#system-web-configuration-httpruntimesection-maxurllength
            // - default maximum query string length in IIS is 2048 bytes
            // - MaxUrlLength is 260 per default, but we dont check it since POST will get same lenght
            // - maxUrl defaults to 4096 bytes, but we assume it will not be an issue since we limit the query string length
            if (uri.Length - operationName.Length > 2048) // uri contains query + operationName, so subract operationName to only get query string length
                return s_skipGetUsePostInstead;

            return HttpClient.GetAsync(uri, DefaultHttpCompletionOption, cancellationToken);
        }
        #endregion

        #region Private methods for reading responses

        /// <summary>
        /// Reads a response from the service and converts it to the specified return type.
        /// </summary>
        /// <param name="response">the <see cref="HttpResponseMessage"/> to deserialize</param>
        /// <param name="operationName">name of operation invoked, used to verify returned xml</param>
        /// <param name="returnType">Type which should be returned.</param>
        /// <returns></returns>
        /// <exception cref="DomainOperationException">On server errors which did not produce expected output</exception>
        /// <exception cref="FaultException{DomainServiceFault}">If server returned a DomainServiceFault</exception>
        private async Task<object> ReadResponseAsync(HttpResponseMessage response, string operationName, Type returnType)
        {
            // Always dispose using finally block below  respnse or we can leak connections
            using (response)
            {
                // TODO: OpenRia 5.0 returns different status codes
                // Need to read content and parse it even if status code is not 200
                // It would make sens to one  check content type and only pase on msbin
                if (!response.IsSuccessStatusCode && response.Content.Headers.ContentType?.MediaType != "application/msbin1")
                {
                    var message = string.Format(Resources.DomainClient_UnexpectedHttpStatusCode, (int)response.StatusCode, response.StatusCode);

                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        throw new DomainOperationException(message, OperationErrorStatus.NotSupported, (int)response.StatusCode, null);
                    else if (response.StatusCode == HttpStatusCode.Unauthorized)
                        throw new DomainOperationException(message, OperationErrorStatus.Unauthorized, (int)response.StatusCode, null);
                    else
                        throw new DomainOperationException(message, OperationErrorStatus.ServerError, (int)response.StatusCode, null);
                }

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var reader = System.Xml.XmlDictionaryReader.CreateBinaryReader(stream, System.Xml.XmlDictionaryReaderQuotas.Max))
                {
                    reader.Read();

                    // Domain Fault
                    if (reader.LocalName == "Fault")
                    {
                        throw ReadFaultException(reader, operationName);
                    }
                    else
                    {
                        // Validate that we are no on ****Response node
                        VerifyReaderIsAtNode(reader, operationName, "Response");
                        reader.ReadStartElement(); // Read to next which should be ****Result

                        if (reader.NodeType == System.Xml.XmlNodeType.EndElement
                            || reader.IsEmptyElement)
                            return null;

                        // Validate that we are no on ****Result node
                        VerifyReaderIsAtNode(reader, operationName, "Result");

                        var serializer = GetSerializer(returnType);

                        // XmlElemtnt returns the "ResultNode" unless we step into the contents
                        if (returnType == typeof(System.Xml.Linq.XElement))
                            reader.ReadStartElement();

                        return serializer.ReadObject(reader, verifyObjectName: false);
                    }
                }
            }
        }
        
        /// <summary>
        /// Verifies the reader is at node with LocalName equal to operationName + postfix.
        /// If the reader is at any other node, then a <see cref="DomainOperationException"/> is thrown
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="operationName">Name of the operation.</param>
        /// <param name="postfix">The postfix.</param>
        /// <exception cref="DomainOperationException">If reader is not at the expected xml element</exception>
        private static void VerifyReaderIsAtNode(System.Xml.XmlDictionaryReader reader, string operationName, string postfix)
        {
            // localName should be operationName + postfix
            if (!(reader.LocalName.Length == operationName.Length + postfix.Length
                && reader.LocalName.StartsWith(operationName, StringComparison.Ordinal)
                && reader.LocalName.EndsWith(postfix, StringComparison.Ordinal)))
            {
                throw new DomainOperationException(
                    string.Format(Resources.DomainClient_UnexpectedResultContent, operationName + postfix, reader.LocalName)
                    , OperationErrorStatus.ServerError, 0, null);
            }
        }

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
            else if (serviceFault.ErrorCode == 401)
            {
                return new DomainOperationException(serviceFault.ErrorMessage, OperationErrorStatus.Unauthorized, serviceFault.ErrorCode, serviceFault.StackTrace);
            }
            else
            {
                // for anything else: map to ServerError
                return new DomainOperationException(serviceFault.ErrorMessage, OperationErrorStatus.ServerError, serviceFault.ErrorCode, serviceFault.StackTrace);
            }
        }

        /// <summary>
        /// Reads a Fault reply from the service.
        /// </summary>
        /// <param name="reader">The reader, which should start at the "Fault" element.</param>
        /// <param name="operationName">Name of the operation.</param>
        /// <returns>A FaultException with the details in the server reply</returns>
        private static FaultException ReadFaultException(System.Xml.XmlDictionaryReader reader, string operationName)
        {
            FaultCode faultCode = null;
            FaultReason faultReason = null;
            var faultReasons = new List<FaultReasonText>();
            FaultCode subCode = null;

            reader.ReadStartElement("Fault"); // <Fault>

            if (reader.IsStartElement("Code"))
            {
                reader.ReadStartElement("Code");  // <Code>
                reader.ReadStartElement("Value"); // <Value>
                var code = reader.ReadContentAsString();
                reader.ReadEndElement(); // </Value>
                if (reader.IsStartElement("Subcode"))
                {
                    reader.ReadStartElement();
                    reader.ReadStartElement("Value");
                    subCode = new FaultCode(reader.ReadContentAsString());
                    reader.ReadEndElement(); // </Value>
                    reader.ReadEndElement(); // </Subcode>
                }
                reader.ReadEndElement(); // </Code>
                faultCode = new FaultCode(code, subCode);
            }

            if (reader.IsStartElement("Reason"))
            {
                reader.ReadStartElement("Reason");
                while (reader.LocalName == "Text")
                {
                    var lang = reader.XmlLang;
                    reader.ReadStartElement("Text");
                    var text = reader.ReadContentAsString();
                    reader.ReadEndElement();

                    faultReasons.Add(new FaultReasonText(text, lang));
                }
                reader.ReadEndElement(); // </Reason>
                faultReason = new FaultReason(faultReasons);
            }

            if (reader.IsStartElement("Detail"))
            {
                reader.ReadStartElement("Detail"); // <Detail>
                var fault = (DomainServiceFault)s_faultSerializer.ReadObject(reader);
                reader.ReadEndElement(); // </ Detail>
                return new FaultException<DomainServiceFault>(fault, faultReason, faultCode, operationName);
            }
            else
            {
                return new FaultException(faultReason, faultCode, operationName);
            }
        }
        #endregion

        #region Serialization helpers

        /// <summary>
        /// Get parameter names and types for method
        /// </summary>
        /// <param name="methodName">The name of the method</param>
        /// <returns>MethodParameters object containing the method parameters</returns>
        internal MethodParameters GetMethodParameters(string methodName) 
            => _localCacheHelper.GetParametersForMethod(methodName);

        /// <summary>
        /// Gets a <see cref="DataContractSerializer"/> which can be used to serialized the specified type.
        /// The serializers are cached for performance reasons.
        /// </summary>
        /// <param name="type">type which should be serializable.</param>
        /// <returns>A <see cref="DataContractSerializer"/> which can be used to serialize the type</returns>
        internal DataContractSerializer GetSerializer(Type type) 
            => _localCacheHelper.GetSerializer(type, EntityTypes);

        #endregion
    }
}
