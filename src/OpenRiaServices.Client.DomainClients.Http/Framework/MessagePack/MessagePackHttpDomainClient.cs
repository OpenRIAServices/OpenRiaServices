using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Nerdbank.MessagePack;
using OpenRiaServices.Client.DomainClients.Http;
using PolyType;

namespace OpenRiaServices.Client.DomainClients.MessagePack
{
    /// <summary>
    /// <see cref="DomainClient"/> implementation which uses MessagePack over HTTP.
    /// </summary>
    sealed class MessagePackHttpDomainClient : HttpDomainClient
    {
        internal const string MediaType = "application/vnd.msgpack";
        private static readonly HttpMethod s_queryMethod = new("QUERY");
        private MessagePackSerializer _serializerCache;
        private readonly MessagePackHttpDomainClientFactory _factory;

        private MessagePackSerializer Serializer => (_serializerCache ??= _factory.GetSerializer(ServiceInterface, base.EntityTypes));

        public MessagePackHttpDomainClient(HttpClient httpClient, Type serviceInterface, MessagePackHttpDomainClientFactory factory)
            : base(httpClient, serviceInterface, factory)
        {
            _factory = factory;
            base.DefaultHttpCompletionOption = HttpCompletionOption.ResponseHeadersRead;
        }

        private protected override Task<HttpResponseMessage> PostAsync(string operationName, IDictionary<string, object> parameters, List<ServiceQueryPart> queryOptions, CancellationToken cancellationToken)
            => SendWithBodyAsync(HttpMethod.Post, operationName, parameters, queryOptions, cancellationToken);

        private protected override Task<HttpResponseMessage> QueryAsync(string operationName, IDictionary<string, object> parameters, List<ServiceQueryPart> queryOptions, CancellationToken cancellationToken)
            => SendWithBodyAsync(s_queryMethod, operationName, parameters, queryOptions, cancellationToken);

        private async Task<HttpResponseMessage> SendWithBodyAsync(HttpMethod method, string operationName, IDictionary<string, object> parameters, List<ServiceQueryPart> queryOptions, CancellationToken cancellationToken)
        {
            using var request = CreateRequestMessage(method, operationName);
            MethodParameters methodParameters = GetMethodParameters(operationName);
            var envelope = CreateRequestEnvelope(operationName, methodParameters, parameters, queryOptions);

            using var stream = new MemoryStream();
            // TODO: If possible, replace this buffering with a custom HttpContent override of SerializeToStream
            // so the request payload can be serialized asynchronously directly to the outgoing request stream.
            var envelopeType = envelope.GetType();
            var envelopeShape = _factory.TypeShapeProvider.GetTypeShape(envelopeType) ?? ProtocolTypeShapes.GeneratedTypeShapeProvider.GetTypeShape(envelopeType);
            Serializer.SerializeObject(stream, envelope, envelopeShape, cancellationToken);

            if (!stream.TryGetBuffer(out ArraySegment<byte> buffer))
                buffer = new ArraySegment<byte>(stream.ToArray());

            request.Content = new ByteArrayContent(buffer.Array, buffer.Offset, buffer.Count);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(MediaType);

            return await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        }

        private static MessagePackRequestEnvelopeBase CreateRequestEnvelope(string operationName, MethodParameters methodParameters, IDictionary<string, object> parameters, List<ServiceQueryPart> queryOptions)
        {
            MessagePackMethodParameters requestParameters = (parameters is { Count: > 0 })
                ? new(methodParameters, parameters) : null;

            if (queryOptions is not null && queryOptions.Count > 0)
            {
                var request = new MessagePackQueryRequestEnvelope()
                {
                    QueryOptions = new List<ServiceQueryPart>(queryOptions.Count),
                    Parameters = requestParameters
                };
                foreach (var queryOption in queryOptions)
                {
                    if (string.Equals(queryOption.QueryOperator, "includeTotalCount", StringComparison.OrdinalIgnoreCase)
                        && bool.TryParse(queryOption.Expression, out bool includeTotalCount))
                    {
                        request.IncludeTotalCount = includeTotalCount;
                    }
                    else
                    {
                        request.QueryOptions.Add(queryOption);
                    }
                }
                return request;
            }

            return string.Equals(operationName, "SubmitChanges", StringComparison.Ordinal)
                ? new MessagePackSubmitRequestEnvelope() { Parameters = requestParameters }
                : new MessagePackInvokeRequestEnvelope() { Parameters = requestParameters };
        }

        private protected override async Task<object> ReadResponseAsync(HttpResponseMessage response, string operationName, Type returnType, CancellationToken cancellationToken)
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


#if NET
                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
                using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
                PipeReader pipeReader = PipeReader.Create(stream, _factory.ResponsePipeReaderOptions);

                object result = null;
                DomainServiceFault fault = null;
                try
                {
                    uint count = await ReadResponseMapHeaderAsync(pipeReader, cancellationToken).ConfigureAwait(false);
                    for (uint i = 0; i < count; i++)
                    {
                        switch (await ReadResponseMemberAsync(pipeReader, cancellationToken).ConfigureAwait(false))
                        {
                            case ResponseMember.Fault:
                                fault = await Serializer.DeserializeAsync<DomainServiceFault, ProtocolTypeShapes>(pipeReader, cancellationToken)
                                    .ConfigureAwait(false);
                                break;
                            case ResponseMember.Result when returnType != typeof(void):
                                var resultShape = _factory.TypeShapeProvider.GetTypeShape(returnType)
                                    ?? ProtocolTypeShapes.GeneratedTypeShapeProvider.GetTypeShapeOrThrow(returnType);
                                result = await Serializer.DeserializeObjectAsync(pipeReader, resultShape, cancellationToken).ConfigureAwait(false);
                                break;
                            default:
                                await SkipResponseValueAsync(pipeReader, Serializer.StartingContext, cancellationToken).ConfigureAwait(false);
                                break;
                        }
                    }
                }
                finally
                {
                    await pipeReader.CompleteAsync().ConfigureAwait(false);
                }

                if (fault is not null)
                {
                    throw new FaultException<DomainServiceFault>(
                        fault,
                        new FaultReason(new[] { new FaultReasonText(fault.ErrorMessage, CultureInfo.CurrentCulture.Name) }),
                        new FaultCode("Sender"),
                        operationName);
                }

                return result;
            }
        }

        // The caller completes the pipe on failure. On success, return the streaming reader
        // and dispose its owner to hand the pipe off at the next token, not the end of the buffer.
        private static async ValueTask<uint> ReadResponseMapHeaderAsync(PipeReader pipe, CancellationToken cancellationToken)
        {
            var reader = new MessagePackAsyncReader(pipe) { CancellationToken = cancellationToken };
            await reader.ReadAsync().ConfigureAwait(false);
            var streaming = reader.CreateStreamingReader() with { CancellationToken = cancellationToken };
            bool isNil;
            while (NeedsMoreResponseBytes(streaming.TryReadNil(out isNil)))
                streaming = new(await streaming.FetchMoreBytesAsync().ConfigureAwait(false));

            uint count = 0;
            if (!isNil)
            {
                while (NeedsMoreResponseBytes(streaming.TryReadMapHeader(out count)))
                    streaming = new(await streaming.FetchMoreBytesAsync().ConfigureAwait(false));
            }

            reader.ReturnReader(ref streaming);
            reader.Dispose();
            return count;
        }

        private static async ValueTask<ResponseMember> ReadResponseMemberAsync(PipeReader pipe, CancellationToken cancellationToken)
        {
            var reader = new MessagePackAsyncReader(pipe) { CancellationToken = cancellationToken };
            await reader.ReadAsync().ConfigureAwait(false);
            var streaming = reader.CreateStreamingReader() with { CancellationToken = cancellationToken };
            ReadOnlySequence<byte> name;
            while (NeedsMoreResponseBytes(streaming.TryReadStringSequence(out name)))
                streaming = new(await streaming.FetchMoreBytesAsync().ConfigureAwait(false));

            var result = Matches(name, "Result"u8) ? ResponseMember.Result
                : Matches(name, "Fault"u8) ? ResponseMember.Fault : ResponseMember.Unknown;
            reader.ReturnReader(ref streaming);
            reader.Dispose();
            return result;

            static bool Matches(ReadOnlySequence<byte> name, ReadOnlySpan<byte> expected)
            {
                if (name.Length != expected.Length)
                    return false;

                foreach (var segment in name)
                {
                    if (!expected.StartsWith(segment.Span))
                        return false;

                    expected = expected.Slice(segment.Length);
                }

                return true;
            }
        }

        private static async ValueTask SkipResponseValueAsync(PipeReader pipe, SerializationContext context, CancellationToken cancellationToken)
        {
            var reader = new MessagePackAsyncReader(pipe) { CancellationToken = cancellationToken };
            await reader.ReadAsync().ConfigureAwait(false);
            var streaming = reader.CreateStreamingReader() with { CancellationToken = cancellationToken };
            while (NeedsMoreResponseBytes(streaming.TrySkip(ref context)))
                streaming = new(await streaming.FetchMoreBytesAsync().ConfigureAwait(false));

            reader.ReturnReader(ref streaming);
            reader.Dispose();
        }

        private static bool NeedsMoreResponseBytes(MessagePackPrimitives.DecodeResult result) => result switch
        {
            MessagePackPrimitives.DecodeResult.Success => false,
            MessagePackPrimitives.DecodeResult.EmptyBuffer or MessagePackPrimitives.DecodeResult.InsufficientBuffer => true,
            _ => throw new MessagePackSerializationException("Invalid MessagePack response envelope."),
        };

        private enum ResponseMember
        {
            Unknown,
            Result,
            Fault,
        }
    }

    [GenerateShapeFor<DomainServiceFault>]
    [GenerateShapeFor<MessagePackQueryRequestEnvelope>]
    [GenerateShapeFor<MessagePackInvokeRequestEnvelope>]
    [GenerateShapeFor<MessagePackSubmitRequestEnvelope>]
    [GenerateShapeFor<IEnumerable<ChangeSetEntry>>]
    partial class ProtocolTypeShapes;
}
