using Nerdbank.MessagePack;
using PolyType;
using PolyType.ReflectionProvider;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.Client.DomainClients.Http
{
    /// <summary>
    /// <see cref="DomainClient"/> implementation which uses MessagePack over HTTP.
    /// </summary>
    sealed class MessagePackHttpDomainClient : HttpDomainClient
    {
        internal const string MediaType = "application/vnd.msgpack";
        private static readonly HttpMethod s_queryMethod = new("QUERY");
        private readonly MessagePackSerializer _serializer = new();
        private readonly ITypeShapeProvider _typeShapeProvider = ReflectionTypeShapeProvider.Default;

        public MessagePackHttpDomainClient(HttpClient httpClient, Type serviceInterface, MessagePackHttpDomainClientFactory factory)
            : base(httpClient, serviceInterface, factory)
        {
        }

        private protected override Task<HttpResponseMessage> PostAsync(string operationName, IDictionary<string, object> parameters, List<ServiceQueryPart> queryOptions, CancellationToken cancellationToken)
            => SendWithBodyAsync(HttpMethod.Post, operationName, parameters, queryOptions, cancellationToken);

        private protected override Task<HttpResponseMessage> QueryAsync(string operationName, IDictionary<string, object> parameters, List<ServiceQueryPart> queryOptions, CancellationToken cancellationToken)
            => SendWithBodyAsync(s_queryMethod, operationName, parameters, queryOptions, cancellationToken);

        private async Task<HttpResponseMessage> SendWithBodyAsync(HttpMethod method, string operationName, IDictionary<string, object> parameters, List<ServiceQueryPart> queryOptions, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(method, operationName);
            var envelope = CreateRequestEnvelope(operationName, parameters, queryOptions);

            using var stream = new MemoryStream();
            _serializer.SerializeObject(stream, envelope, _typeShapeProvider.GetTypeShapeOrThrow(typeof(MessagePackRequestEnvelope)), cancellationToken);

            var bytes = stream.ToArray();
            request.Content = new ByteArrayContent(bytes);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(MediaType);

            return await HttpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
        }

        private MessagePackRequestEnvelope CreateRequestEnvelope(string operationName, IDictionary<string, object> parameters, List<ServiceQueryPart> queryOptions)
        {
            var result = new MessagePackRequestEnvelope();
            if (parameters is not null && parameters.Count > 0)
            {
                MethodParameters methodParameters = GetMethodParameters(operationName);
                foreach (var param in parameters)
                {
                    if (param.Value is null)
                    {
                        result.Parameters[param.Key] = null;
                    }
                    else
                    {
                        var parameterType = methodParameters.GetTypeForMethodParameter(param.Key);
                        result.Parameters[param.Key] = SerializeValue(param.Value, parameterType);
                    }
                }
            }

            if (queryOptions is not null && queryOptions.Count > 0)
            {
                result.QueryOptions = new List<ServiceQueryPart>(queryOptions.Count);
                foreach (var queryOption in queryOptions)
                {
                    if (string.Equals(queryOption.QueryOperator, "includeTotalCount", StringComparison.OrdinalIgnoreCase)
                        && bool.TryParse(queryOption.Expression, out bool includeTotalCount))
                    {
                        result.IncludeTotalCount = includeTotalCount;
                    }
                    else
                    {
                        result.QueryOptions.Add(queryOption);
                    }
                }
            }

            return result;
        }

        private protected override async Task<object> ReadResponseAsync(HttpResponseMessage response, string operationName, Type returnType)
        {
            using (response)
            {
                if (!response.IsSuccessStatusCode && response.Content.Headers.ContentType?.MediaType != MediaType)
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

                using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                var envelope = (MessagePackResponseEnvelope)_serializer.DeserializeObject(stream, _typeShapeProvider.GetTypeShapeOrThrow(typeof(MessagePackResponseEnvelope)))
                    ?? new MessagePackResponseEnvelope();

                if (envelope.Fault is not null)
                {
                    throw new FaultException<DomainServiceFault>(
                        envelope.Fault,
                        new FaultReason(new[] { new FaultReasonText(envelope.Fault.ErrorMessage, CultureInfo.CurrentCulture.Name) }),
                        new FaultCode("Sender"),
                        operationName);
                }

                if (returnType == typeof(void) || envelope.Result is null)
                    return null;

                return DeserializeValue(envelope.Result, returnType);
            }
        }

        private byte[] SerializeValue(object value, Type type)
        {
            using var stream = new MemoryStream();
            _serializer.SerializeObject(stream, value, _typeShapeProvider.GetTypeShapeOrThrow(type));
            return stream.ToArray();
        }

        private object DeserializeValue(byte[] bytes, Type type)
            => _serializer.DeserializeObject(bytes, _typeShapeProvider.GetTypeShapeOrThrow(type));

        private sealed class MessagePackRequestEnvelope
        {
            public Dictionary<string, byte[]> Parameters { get; set; } = new(StringComparer.Ordinal);
            public List<ServiceQueryPart> QueryOptions { get; set; }
            public bool IncludeTotalCount { get; set; }
        }

        private sealed class MessagePackResponseEnvelope
        {
            public byte[] Result { get; set; }
            public DomainServiceFault Fault { get; set; }
        }
    }
}
