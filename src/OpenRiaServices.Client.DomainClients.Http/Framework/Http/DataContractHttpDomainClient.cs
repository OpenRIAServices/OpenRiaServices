using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace OpenRiaServices.Client.DomainClients.Http
{
    /// <summary>
    /// Base class for <see cref="DomainClient"/>s using <see cref="DataContractSerializer"/> serialization and talking to the server using <see cref="HttpClient"/>.
    /// </summary>
    abstract class DataContractHttpDomainClient : HttpDomainClient
    {
        private static readonly DataContractSerializer s_faultSerializer = new DataContractSerializer(typeof(DomainServiceFault));
        private static readonly HttpMethod s_queryMethod = new HttpMethod("QUERY");
        private static readonly Dictionary<Type, DataContractSerializationHelper> s_globalCacheHelpers = new Dictionary<Type, DataContractSerializationHelper>();

        private readonly DataContractSerializationHelper _localCacheHelper;

        private protected DataContractHttpDomainClient(HttpClient httpClient, Type serviceInterface, OpenRiaServices.Client.DomainClients.HttpDomainClientFactory factory)
            : base(httpClient, serviceInterface, factory)
        {
            ArgumentNullException.ThrowIfNull(serviceInterface);

            lock (s_globalCacheHelpers)
            {
                if (!s_globalCacheHelpers.TryGetValue(serviceInterface, out _localCacheHelper))
                {
                    _localCacheHelper = new DataContractSerializationHelper(serviceInterface);
                    s_globalCacheHelpers.Add(serviceInterface, _localCacheHelper);
                }
            }
        }

        /// <summary>
        /// The mime/content type used for communication with the server.
        /// </summary>
        private protected abstract string ContentType { get; }

        private protected abstract XmlDictionaryWriter CreateWriter(Stream stream);
        private protected abstract XmlDictionaryReader CreateReader(Stream stream);

        /// <summary>
        /// Initiates a POST request for the given operation and return the server response (as a task).
        /// </summary>
        private protected override Task<HttpResponseMessage> PostAsync(string operationName, IDictionary<string, object> parameters, List<ServiceQueryPart> queryOptions, CancellationToken cancellationToken)
        {
            return SendWithBodyAsync(HttpMethod.Post, operationName, parameters, queryOptions, cancellationToken);
        }

        /// <summary>
        /// Initiates a QUERY request for the given operation and returns the server response (as a task).
        /// The QUERY method is a safe, idempotent HTTP method that allows a request body,
        /// suitable for read operations with complex query parameters that exceed URI length limits.
        /// </summary>
        private protected override Task<HttpResponseMessage> QueryAsync(string operationName, IDictionary<string, object> parameters, List<ServiceQueryPart> queryOptions, CancellationToken cancellationToken)
        {
            return SendWithBodyAsync(s_queryMethod, operationName, parameters, queryOptions, cancellationToken);
        }

        /// <summary>
        /// Sends an HTTP request with a body for the given operation.
        /// Used by both POST and QUERY methods.
        /// </summary>
        private async Task<HttpResponseMessage> SendWithBodyAsync(HttpMethod method, string operationName, IDictionary<string, object> parameters, List<ServiceQueryPart> queryOptions, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(method, operationName);

            using (var ms = new MemoryStream())
            using (var writer = CreateWriter(ms))
            {
                // Write message
                var rootNamespace = "http://tempuri.org/";
                bool hasQueryOptions = queryOptions != null && queryOptions.Count > 0;

                if (hasQueryOptions)
                {
                    writer.WriteStartElement("MessageRoot");
                    writer.WriteStartElement("QueryOptions");
                    foreach (var queryOption in queryOptions)
                    {
                        writer.WriteStartElement("QueryOption");
                        writer.WriteAttributeString("Name", queryOption.QueryOperator);
                        writer.WriteAttributeString("Value", queryOption.Expression);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
                writer.WriteStartElement(operationName, rootNamespace); // <OperationName>

                // Write all parameters
                if (parameters != null && parameters.Count > 0)
                {
                    MethodParameters methodParameter = GetMethodParameters(operationName);
                    foreach (var param in parameters)
                    {
                        writer.WriteStartElement(param.Key);  // <ParameterName>
                        if (param.Value != null)
                        {
                            var parameterType = methodParameter.GetTypeForMethodParameter(param.Key);
                            var serializer = GetSerializer(parameterType);
                            serializer.WriteObjectContent(writer, param.Value);
                        }
                        else
                        {
                            // Null input
                            writer.WriteAttributeString("i", "nil", "http://www.w3.org/2001/XMLSchema-instance", "true");
                        }
                        writer.WriteEndElement();            // </ParameterName>
                    }
                }

                writer.WriteEndDocument(); // </OperationName> and </MessageRoot> if present
                writer.Flush();

                ms.TryGetBuffer(out ArraySegment<byte> buffer);
                request.Content = new ByteArrayContent(buffer.Array, buffer.Offset, buffer.Count);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(ContentType);
            }

            return await HttpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reads a response from the service and converts it to the specified return type.
        /// </summary>
        /// <param name="response">the <see cref="HttpResponseMessage"/> to deserialize</param>
        /// <param name="operationName">name of operation invoked, used to verify returned xml</param>
        /// <param name="returnType">Type which should be returned.</param>
        /// <returns></returns>
        /// <exception cref="DomainOperationException">On server errors which did not produce expected output</exception>
        /// <exception cref="FaultException{DomainServiceFault}">If server returned a DomainServiceFault</exception>
        private protected override async Task<object> ReadResponseAsync(HttpResponseMessage response, string operationName, Type returnType)
        {
            // Always dispose using finally block below response or we can leak connections
            using (response)
            {
                // Need to read content and parse it even if status code is not 200
                if (!response.IsSuccessStatusCode && response.Content.Headers.ContentType?.MediaType != ContentType)
                {
                    var message = string.Format(CultureInfo.InvariantCulture, Resources.DomainClient_UnexpectedHttpStatusCode, (int)response.StatusCode, response.StatusCode);

                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        throw new DomainOperationException(message, OperationErrorStatus.NotSupported, (int)response.StatusCode, null);
                    else if (response.StatusCode == HttpStatusCode.Unauthorized)
                        throw new DomainOperationException(message, OperationErrorStatus.Unauthorized, (int)response.StatusCode, null);
                    else if (response.StatusCode == HttpStatusCode.NotFound)
                        throw new DomainOperationException(message, OperationErrorStatus.NotFound, (int)response.StatusCode, null);
                    else
                        throw new DomainOperationException(message, OperationErrorStatus.ServerError, (int)response.StatusCode, null);
                }

                using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var reader = CreateReader(stream))
                {
                    reader.Read();

                    // Domain Fault
                    if (reader.LocalName == "Fault")
                    {
                        throw ReadFaultException(reader, operationName);
                    }
                    else
                    {
                        // Validate that we are now on ****Response node
                        VerifyReaderIsAtNode(reader, operationName, "Response");
                        reader.ReadStartElement(); // Read to next which should be ****Result

                        if (returnType == typeof(void)
                          || (reader.GetAttribute("nil", "http://www.w3.org/2001/XMLSchema-instance") == "true"))
                        {
                            return null;
                        }

                        // Validate that we are now on ****Result node
                        VerifyReaderIsAtNode(reader, operationName, "Result");

                        var serializer = GetSerializer(returnType);

                        // XmlElement returns the "ResultNode" unless we step into the contents
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
        private static void VerifyReaderIsAtNode(XmlDictionaryReader reader, string operationName, string postfix)
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
        /// Reads a Fault reply from the service.
        /// </summary>
        /// <param name="reader">The reader, which should start at the "Fault" element.</param>
        /// <param name="operationName">Name of the operation.</param>
        /// <returns>A FaultException with the details in the server reply</returns>
        private static FaultException ReadFaultException(XmlDictionaryReader reader, string operationName)
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
                    bool isEmpty = reader.IsEmptyElement;

                    var lang = reader.XmlLang;
                    reader.ReadStartElement("Text");
                    var text = reader.ReadContentAsString();

                    if (!isEmpty)
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

        /// <summary>
        /// Gets a <see cref="DataContractSerializer"/> which can be used to serialized the specified type.
        /// The serializers are cached for performance reasons.
        /// </summary>
        /// <param name="type">type which should be serializable.</param>
        /// <returns>A <see cref="DataContractSerializer"/> which can be used to serialize the type</returns>
        private DataContractSerializer GetSerializer(Type type)
             => _localCacheHelper.GetSerializer(type, EntityTypes);
    }
}
