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
            var envelope = CreateRequestEnvelope(method, operationName, parameters, queryOptions);

            using var stream = new MemoryStream();
            // TODO: If possible, replace this buffering with a custom HttpContent override of SerializeToStream
            // so the request payload can be serialized asynchronously directly to the outgoing request stream.
            await _serializer.SerializeObjectAsync(stream, envelope, _typeShapeProvider.GetTypeShapeOrThrow(envelope.GetType()), cancellationToken).ConfigureAwait(false);

            var bytes = stream.ToArray();
            request.Content = new ByteArrayContent(bytes);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(MediaType);

            return await HttpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
        }

        private MessagePackRequestEnvelopeBase CreateRequestEnvelope(HttpMethod method, string operationName, IDictionary<string, object> parameters, List<ServiceQueryPart> queryOptions)
        {
            MessagePackRequestEnvelopeBase result = method == s_queryMethod
                ? new MessagePackQueryRequestEnvelope()
                : string.Equals(operationName, "SubmitChanges", StringComparison.Ordinal)
                    ? new MessagePackSubmitRequestEnvelope()
                    : new MessagePackInvokeRequestEnvelope();
            if (parameters is not null && parameters.Count > 0)
            {
                MethodParameters methodParameters = GetMethodParameters(operationName);
                foreach (var param in parameters)
                {
                    if (param.Value is null)
                    {
                        result.Parameters.Values[param.Key] = null;
                    }
                    else
                    {
                        var parameterType = methodParameters.GetTypeForMethodParameter(param.Key);
                        result.Parameters.Values[param.Key] = SerializeValue(param.Value, parameterType);
                    }
                }
            }

            if (result is MessagePackQueryRequestEnvelope queryRequest && queryOptions is not null && queryOptions.Count > 0)
            {
                queryRequest.QueryOptions = new List<ServiceQueryPart>(queryOptions.Count);
                foreach (var queryOption in queryOptions)
                {
                    if (string.Equals(queryOption.QueryOperator, "includeTotalCount", StringComparison.OrdinalIgnoreCase)
                        && bool.TryParse(queryOption.Expression, out bool includeTotalCount))
                    {
                        queryRequest.IncludeTotalCount = includeTotalCount;
                    }
                    else
                    {
                        queryRequest.QueryOptions.Add(queryOption);
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
                Type envelopeType = GetResponseEnvelopeType(operationName, returnType);
                var envelope = (MessagePackResponseEnvelopeBase?)await _serializer.DeserializeObjectAsync(stream, _typeShapeProvider.GetTypeShapeOrThrow(envelopeType)).ConfigureAwait(false)
                    ?? (MessagePackResponseEnvelopeBase)Activator.CreateInstance(envelopeType);

                if (envelope.Fault is not null)
                {
                    throw new FaultException<DomainServiceFault>(
                        envelope.Fault,
                        new FaultReason(new[] { new FaultReasonText(envelope.Fault.ErrorMessage, CultureInfo.CurrentCulture.Name) }),
                        new FaultCode("Sender"),
                        operationName);
                }

                object result = envelope.GetResult();
                if (returnType == typeof(void) || result is null)
                    return null;

                return result;
            }
        }

        private byte[] SerializeValue(object value, Type type)
        {
            using var stream = new MemoryStream();
            _serializer.SerializeObject(stream, value, _typeShapeProvider.GetTypeShapeOrThrow(type));
            return stream.ToArray();
        }

        private static Type GetResponseEnvelopeType(string operationName, Type returnType)
        {
            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(QueryResult<>))
                return typeof(MessagePackQueryResponseEnvelope<>).MakeGenericType(returnType);
            if (string.Equals(operationName, "SubmitChanges", StringComparison.Ordinal))
                return typeof(MessagePackSubmitResponseEnvelope<>).MakeGenericType(returnType);
            return typeof(MessagePackInvokeResponseEnvelope<>).MakeGenericType(returnType);
        }

        private abstract class MessagePackRequestEnvelopeBase
        {
            public MessagePackMethodParameters Parameters { get; set; } = new();
        }

        private sealed class MessagePackQueryRequestEnvelope : MessagePackRequestEnvelopeBase
        {
            public List<ServiceQueryPart> QueryOptions { get; set; }
            public bool IncludeTotalCount { get; set; }
        }

        private sealed class MessagePackInvokeRequestEnvelope : MessagePackRequestEnvelopeBase
        {
        }

        private sealed class MessagePackSubmitRequestEnvelope : MessagePackRequestEnvelopeBase
        {
        }

        private sealed class MessagePackMethodParameters
        {
            public Dictionary<string, byte[]> Values { get; set; } = new(StringComparer.Ordinal);
        }

        private abstract class MessagePackResponseEnvelopeBase
        {
            public DomainServiceFault Fault { get; set; }
            public abstract object GetResult();
        }

        private sealed class MessagePackQueryResponseEnvelope<TResult> : MessagePackResponseEnvelopeBase
        {
            public TResult Result { get; set; }
            public override object GetResult() => Result;
        }

        private sealed class MessagePackInvokeResponseEnvelope<TResult> : MessagePackResponseEnvelopeBase
        {
            public TResult Result { get; set; }
            public override object GetResult() => Result;
        }

        private sealed class MessagePackSubmitResponseEnvelope<TResult> : MessagePackResponseEnvelopeBase
        {
            public TResult Result { get; set; }
            public override object GetResult() => Result;
        }
    }
}
