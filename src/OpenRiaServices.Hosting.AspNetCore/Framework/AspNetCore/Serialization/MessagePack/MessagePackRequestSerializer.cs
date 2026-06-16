using Microsoft.AspNetCore.Http;
using Nerdbank.MessagePack;
using OpenRiaServices.Hosting.AspNetCore.Serialization.MessagePack.Converters;
using OpenRiaServices.Server;
using PolyType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization.MessagePack
{
    /// <summary>
    /// Handles Serialization of requests and responses
    /// </summary>
    internal sealed class MessagePackRequestSerializer : RequestSerializer
    {
        private readonly ITypeShapeProvider _typeShapeProvider;
        private readonly DomainOperationEntry _operation;
        private readonly MessagePackSerializer _operationSerializer;

        public MessagePackRequestSerializer(DomainOperationEntry operation, MessagePackSerializer serializer, ITypeShapeProvider typeShapeProvider)
        {
            _operation = operation ?? throw new ArgumentNullException(nameof(operation));
            _typeShapeProvider = typeShapeProvider ?? throw new ArgumentNullException(nameof(typeShapeProvider));
            _operationSerializer = CreateOperationSerializer(serializer, _operation, _typeShapeProvider);
        }

        public override bool CanRead(ReadOnlySpan<char> contentType)
            => MatchesMediaType(contentType, MimeTypes.MessagePack);

        public override bool CanWrite(ReadOnlySpan<char> contentType)
            => MatchesMediaType(contentType, MimeTypes.MessagePack);

        public override async Task<(ServiceQuery?, object?[])> ReadParametersFromBodyAsync(HttpContext context, DomainOperationEntry operation)
        {
            MessagePackRequestEnvelope envelope;
            try
            {
                envelope = await DeserializeRequestEnvelopeAsync(context, operation).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ExceptionHandlingUtility.IsFatal(ex))
            {
                throw new BadHttpRequestException($"Failed to read body: {ex.Message}", ex);
            }

            var parameters = operation.Parameters;
            object?[] values = new object?[parameters.Count];
            var payload = envelope.Parameters?.Values ?? new Dictionary<string, object?>(StringComparer.Ordinal);

            for (int i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                if (!payload.TryGetValue(parameter.Name, out var value))
                {
                    if (parameter.IsNullable)
                    {
                        values[i] = null;
                        continue;
                    }

                    throw new BadHttpRequestException($"No value provided for parameter '{parameter.Name}'", (int)HttpStatusCode.BadRequest);
                }

                if (value is null)
                {
                    if (!parameter.IsNullable)
                        throw new BadHttpRequestException($"Null value provided for parameter '{parameter.Name}'", (int)HttpStatusCode.BadRequest);

                    values[i] = null;
                    continue;
                }

                values[i] = value;
            }

            ServiceQuery? serviceQuery = null;
            if (envelope is MessagePackQueryRequestEnvelope queryEnvelope
                && ((queryEnvelope.QueryOptions?.Count ?? 0) > 0 || queryEnvelope.IncludeTotalCount))
            {
                serviceQuery = new ServiceQuery
                {
                    IncludeTotalCount = queryEnvelope.IncludeTotalCount,
                    QueryParts = queryEnvelope.QueryOptions ?? new List<ServiceQueryPart>(),
                };
            }

            return (serviceQuery, values);
        }

        public override async Task<IEnumerable<ChangeSetEntry>> ReadSubmitRequestAsync(HttpContext context)
        {
            (_, object?[] parameters) = await ReadParametersFromBodyAsync(context, _operation).ConfigureAwait(false);
            return (IEnumerable<ChangeSetEntry>?)parameters.FirstOrDefault() ?? Array.Empty<ChangeSetEntry>();
        }
        public override async Task WriteErrorAsync(HttpContext context, DomainServiceFault fault, DomainOperationEntry operation)
        {
            context.Response.Headers.ContentType = MimeTypes.MessagePack;
            await _operationSerializer.SerializeAsync(
                context.Response.Body,
                new MessagePackFaultResponse { Fault = fault },
                context.RequestAborted).ConfigureAwait(false);
        }


        public override Task WriteSubmitResponseAsync(HttpContext context, IEnumerable<ChangeSetEntry> result)
            => WriteEnvelopeAsync(context, new MessagePackSubmitResponseEnvelope(result));

        public override Task WriteInvokeResponseAsync(HttpContext context, object? result)
        {
            return WriteEnvelopeAsync(context, CreateInvokeResponseEnvelope(_operation, result));
        }

        public override Task WriteQueryResponseAsync<T>(HttpContext context, QueryResult<T> result)
        {
            return WriteEnvelopeAsync(context, new MessagePackQueryResponseEnvelope<T>(result));
        }

        private async Task WriteEnvelopeAsync(HttpContext context, MessagePackResponseEnvelopeBase envelope)
        {
            context.Response.Headers.ContentType = MimeTypes.MessagePack;
            await _operationSerializer.SerializeObjectAsync(
                context.Response.BodyWriter,
                envelope,
                _typeShapeProvider.GetTypeShapeOrThrow(envelope.GetType()),
                context.RequestAborted).ConfigureAwait(false);
        }

        private async Task<MessagePackRequestEnvelope> DeserializeRequestEnvelopeAsync(HttpContext context, DomainOperationEntry operation)
        {
            ITypeShape envelopeType = operation.Operation switch
            {
                DomainOperation.Query => _typeShapeProvider.GetTypeShapeOrThrow<MessagePackQueryRequestEnvelope>(),
                _ => _typeShapeProvider.GetTypeShapeOrThrow<MessagePackRequestEnvelope>(),
            };

            return (MessagePackRequestEnvelope?)await _operationSerializer.DeserializeObjectAsync(
                context.Request.Body,
                envelopeType,
                context.RequestAborted).ConfigureAwait(false)
                ?? (MessagePackRequestEnvelope)Activator.CreateInstance(envelopeType.Type)!;
        }

        private static MessagePackSerializer CreateOperationSerializer(MessagePackSerializer serializer, DomainOperationEntry operation, ITypeShapeProvider typeShapeProvider)
        {
            SerializationContext context = serializer.StartingContext;
            context[MethodParametersConverter.OperationKey] = operation;
            return serializer with { StartingContext = context };
        }

        private static MessagePackResponseEnvelopeBase CreateInvokeResponseEnvelope(DomainOperationEntry operation, object? result)
        {
            // iF the operation is a query, the return type of the envelope is QueryResult<T>, where T is the associated type of the operation. For submit operations, the return type is IEnumerable<ChangeSetEntry>. For invoke operations, the return type is the declared return type of the operation.
            Type responseEnvelopeType = operation.Operation switch
            {
                DomainOperation.Invoke when operation.ReturnType == typeof(void) => typeof(MessagePackInvokeResponseEnvelope<>).MakeGenericType(typeof(object)),
                DomainOperation.Invoke => typeof(MessagePackInvokeResponseEnvelope<>).MakeGenericType(operation.ReturnType),
                _ => throw new NotSupportedException()
            };

            return (MessagePackResponseEnvelopeBase)Activator.CreateInstance(responseEnvelopeType, result)!;
        }

        private static bool MatchesMediaType(ReadOnlySpan<char> value, ReadOnlySpan<char> expected)
        {
            int separator = value.IndexOf(';');
            if (separator >= 0)
                value = value[..separator];

            return value.Equals(expected, StringComparison.OrdinalIgnoreCase);
        }
    }

}
