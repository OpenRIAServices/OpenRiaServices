using Nerdbank.MessagePack;
using OpenRiaServices.Hosting.Serialization.MessagePack;
using OpenRiaServices.Server;
using PolyType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization
{
    internal sealed class MessagePackSerializationProvider : ISerializationProvider
    {
        private readonly MessagePackSerializationOptions _options;
        private readonly FilteredTypeShapeProvider _filteredTypeShapeProvider;

        public MessagePackSerializationProvider(MessagePackSerializationOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _filteredTypeShapeProvider = new FilteredTypeShapeProvider(options.TypeShapeProvider);
        }

        public RequestSerializer GetRequestSerializer(DomainOperationEntry operation)
            => new MessagePackRequestSerializer(operation, _options.Serializer, _filteredTypeShapeProvider, _options.TypeShapeProvider);
    }

    internal sealed class MessagePackRequestSerializer : RequestSerializer
    {
        private readonly MessagePackSerializer _serializer;
        private readonly ITypeShapeProvider _typeShapeProvider;
        private readonly ITypeShapeProvider _envelopeTypeShapeProvider;
        private readonly DomainOperationEntry _operation;

        public MessagePackRequestSerializer(DomainOperationEntry operation, MessagePackSerializer serializer, ITypeShapeProvider typeShapeProvider, ITypeShapeProvider envelopeTypeShapeProvider)
        {
            _operation = operation ?? throw new ArgumentNullException(nameof(operation));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _typeShapeProvider = typeShapeProvider ?? throw new ArgumentNullException(nameof(typeShapeProvider));
            _envelopeTypeShapeProvider = envelopeTypeShapeProvider ?? throw new ArgumentNullException(nameof(envelopeTypeShapeProvider));
        }

        public override bool CanRead(ReadOnlySpan<char> contentType)
            => MatchesMediaType(contentType, MimeTypes.MessagePack);

        public override bool CanWrite(ReadOnlySpan<char> contentType)
            => MatchesMediaType(contentType, MimeTypes.MessagePack);

        public override async Task<(ServiceQuery?, object?[])> ReadParametersFromBodyAsync(Microsoft.AspNetCore.Http.HttpContext context, DomainOperationEntry operation)
        {
            MessagePackRequestEnvelopeBase envelope;
            try
            {
                envelope = await DeserializeRequestEnvelopeAsync(context, operation).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ExceptionHandlingUtility.IsFatal(ex))
            {
                throw new Microsoft.AspNetCore.Http.BadHttpRequestException($"Failed to read body: {ex.Message}", ex);
            }

            var parameters = operation.Parameters;
            object?[] values = new object?[parameters.Count];
            var payload = envelope.Parameters?.Values ?? new Dictionary<string, byte[]?>(StringComparer.Ordinal);

            for (int i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                if (!payload.TryGetValue(parameter.Name, out var bytes))
                {
                    if (parameter.IsNullable)
                    {
                        values[i] = null;
                        continue;
                    }

                    throw new Microsoft.AspNetCore.Http.BadHttpRequestException($"No value provided for parameter '{parameter.Name}'", (int)HttpStatusCode.BadRequest);
                }

                if (bytes is null)
                {
                    if (!parameter.IsNullable)
                        throw new Microsoft.AspNetCore.Http.BadHttpRequestException($"Null value provided for parameter '{parameter.Name}'", (int)HttpStatusCode.BadRequest);

                    values[i] = null;
                    continue;
                }

                values[i] = Deserialize(bytes, parameter.ParameterType);
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

        public override async Task<IEnumerable<ChangeSetEntry>> ReadSubmitRequestAsync(Microsoft.AspNetCore.Http.HttpContext context)
        {
            (_, object?[] parameters) = await ReadParametersFromBodyAsync(context, _operation).ConfigureAwait(false);
            return (IEnumerable<ChangeSetEntry>?)parameters.FirstOrDefault() ?? Array.Empty<ChangeSetEntry>();
        }

        public override Task WriteSubmitResponseAsync(Microsoft.AspNetCore.Http.HttpContext context, IEnumerable<ChangeSetEntry> result)
            => WriteResponseAsync(context, result, _operation);

        public override Task WriteErrorAsync(Microsoft.AspNetCore.Http.HttpContext context, DomainServiceFault fault, DomainOperationEntry operation)
        {
            return WriteEnvelopeAsync(context, CreateResponseEnvelope(operation, result: null, fault));
        }

        public override Task WriteResponseAsync(Microsoft.AspNetCore.Http.HttpContext context, object? result, DomainOperationEntry operation)
            => WriteEnvelopeAsync(context, CreateResponseEnvelope(operation, result, fault: null));

        private async Task WriteEnvelopeAsync(Microsoft.AspNetCore.Http.HttpContext context, object envelope)
        {
            context.Response.Headers.ContentType = MimeTypes.MessagePack;
            await _serializer.SerializeObjectAsync(
                context.Response.Body,
                envelope,
                _envelopeTypeShapeProvider.GetTypeShapeOrThrow(envelope.GetType()),
                context.RequestAborted).ConfigureAwait(false);
        }

        private object? Deserialize(byte[] bytes, Type type)
            => _serializer.DeserializeObject(bytes, _typeShapeProvider.GetTypeShapeOrThrow(type));

        private static Type GetReturnType(DomainOperationEntry operation)
            => operation.Operation switch
            {
                DomainOperation.Query => typeof(QueryResult<>).MakeGenericType(operation.AssociatedType!),
                DomainOperation.Invoke => operation.ReturnType,
                DomainOperation.Custom when operation.Name == "Submit" => typeof(IEnumerable<ChangeSetEntry>),
                _ => throw new NotSupportedException()
            };

        private async Task<MessagePackRequestEnvelopeBase> DeserializeRequestEnvelopeAsync(Microsoft.AspNetCore.Http.HttpContext context, DomainOperationEntry operation)
        {
            Type envelopeType = operation.Operation switch
            {
                DomainOperation.Query => typeof(MessagePackQueryRequestEnvelope),
                DomainOperation.Custom when operation.Name == "Submit" => typeof(MessagePackSubmitRequestEnvelope),
                _ => typeof(MessagePackInvokeRequestEnvelope),
            };

            return (MessagePackRequestEnvelopeBase?)await _serializer.DeserializeObjectAsync(
                context.Request.Body,
                _envelopeTypeShapeProvider.GetTypeShapeOrThrow(envelopeType),
                context.RequestAborted).ConfigureAwait(false)
                ?? (MessagePackRequestEnvelopeBase)Activator.CreateInstance(envelopeType)!;
        }

        private object CreateResponseEnvelope(DomainOperationEntry operation, object? result, DomainServiceFault? fault)
        {
            Type responseEnvelopeType = operation.Operation switch
            {
                DomainOperation.Query => typeof(MessagePackQueryResponseEnvelope<>).MakeGenericType(GetReturnType(operation)),
                DomainOperation.Custom when operation.Name == "Submit" => typeof(MessagePackSubmitResponseEnvelope<>).MakeGenericType(GetReturnType(operation)),
                _ => typeof(MessagePackInvokeResponseEnvelope<>).MakeGenericType(GetReturnType(operation)),
            };

            object envelope = Activator.CreateInstance(responseEnvelopeType)!;
            responseEnvelopeType.GetProperty(nameof(MessagePackResponseEnvelopeBase.Fault))!.SetValue(envelope, fault);
            if (result is not null)
                responseEnvelopeType.GetProperty("Result")!.SetValue(envelope, result);

            return envelope;
        }

        private static bool MatchesMediaType(ReadOnlySpan<char> value, ReadOnlySpan<char> expected)
        {
            int separator = value.IndexOf(';');
            if (separator >= 0)
                value = value[..separator];

            return value.Equals(expected, StringComparison.OrdinalIgnoreCase);
        }
    }

    [DataContract]
    internal abstract class MessagePackRequestEnvelopeBase
    {
        [DataMember]
        public MethodParameters? Parameters { get; set; } = new();
    }

    [DataContract]
    internal sealed class MessagePackQueryRequestEnvelope : MessagePackRequestEnvelopeBase
    {
        [DataMember]
        public List<ServiceQueryPart>? QueryOptions { get; set; }
        [DataMember]
        public bool IncludeTotalCount { get; set; }
    }

    [DataContract]
    internal sealed class MessagePackInvokeRequestEnvelope : MessagePackRequestEnvelopeBase
    {
    }

    [DataContract]
    internal sealed class MessagePackSubmitRequestEnvelope : MessagePackRequestEnvelopeBase
    {
    }

    [DataContract]
    internal sealed class MethodParameters
    {
        [DataMember]
        public Dictionary<string, byte[]?> Values { get; set; } = new(StringComparer.Ordinal);
    }

    [DataContract]
    internal abstract class MessagePackResponseEnvelopeBase
    {
        [DataMember]
        public DomainServiceFault? Fault { get; set; }
    }

    [DataContract]
    internal sealed class MessagePackQueryResponseEnvelope<TResult> : MessagePackResponseEnvelopeBase
    {
        [DataMember]
        public TResult? Result { get; set; }
    }

    [DataContract]
    internal sealed class MessagePackInvokeResponseEnvelope<TResult> : MessagePackResponseEnvelopeBase
    {
        [DataMember]
        public TResult? Result { get; set; }
    }

    [DataContract]
    internal sealed class MessagePackSubmitResponseEnvelope<TResult> : MessagePackResponseEnvelopeBase
    {
        [DataMember]
        public TResult? Result { get; set; }
    }
}
