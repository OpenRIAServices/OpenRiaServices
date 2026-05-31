using Nerdbank.MessagePack;
using OpenRiaServices.Server;
using PolyType;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization
{
    internal sealed class MessagePackSerializationProvider : ISerializationProvider
    {
        private readonly MessagePackSerializationOptions _options;

        public MessagePackSerializationProvider(MessagePackSerializationOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public RequestSerializer GetRequestSerializer(DomainOperationEntry operation)
            => new MessagePackRequestSerializer(operation, _options.Serializer, _options.TypeShapeProvider);
    }

    internal sealed class MessagePackRequestSerializer : RequestSerializer
    {
        private readonly MessagePackSerializer _serializer;
        private readonly ITypeShapeProvider _typeShapeProvider;
        private readonly DomainOperationEntry _operation;

        public MessagePackRequestSerializer(DomainOperationEntry operation, MessagePackSerializer serializer, ITypeShapeProvider typeShapeProvider)
        {
            _operation = operation ?? throw new ArgumentNullException(nameof(operation));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _typeShapeProvider = typeShapeProvider ?? throw new ArgumentNullException(nameof(typeShapeProvider));
        }

        public override bool CanRead(ReadOnlySpan<char> contentType)
            => MatchesMediaType(contentType, MimeTypes.MessagePack);

        public override bool CanWrite(ReadOnlySpan<char> contentType)
            => MatchesMediaType(contentType, MimeTypes.MessagePack);

        public override async Task<(ServiceQuery?, object?[])> ReadParametersFromBodyAsync(Microsoft.AspNetCore.Http.HttpContext context, DomainOperationEntry operation)
        {
            MessagePackRequestEnvelope envelope;
            try
            {
                envelope = await _serializer.DeserializeAsync(
                    context.Request.Body,
                    _typeShapeProvider.GetTypeShapeOrThrow<MessagePackRequestEnvelope>(),
                    context.RequestAborted).ConfigureAwait(false)
                    ?? new MessagePackRequestEnvelope();
            }
            catch (Exception ex) when (!ExceptionHandlingUtility.IsFatal(ex))
            {
                throw new Microsoft.AspNetCore.Http.BadHttpRequestException($"Failed to read body: {ex.Message}", ex);
            }

            var parameters = operation.Parameters;
            object?[] values = new object?[parameters.Count];
            var payload = envelope.Parameters ?? new Dictionary<string, byte[]?>(StringComparer.Ordinal);

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
            if ((envelope.QueryOptions?.Count ?? 0) > 0 || envelope.IncludeTotalCount)
            {
                serviceQuery = new ServiceQuery
                {
                    IncludeTotalCount = envelope.IncludeTotalCount,
                    QueryParts = envelope.QueryOptions ?? new List<ServiceQueryPart>(),
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
            var envelope = new MessagePackResponseEnvelope
            {
                Fault = fault,
            };

            return WriteEnvelopeAsync(context, envelope);
        }

        public override Task WriteResponseAsync(Microsoft.AspNetCore.Http.HttpContext context, object? result, DomainOperationEntry operation)
        {
            var envelope = new MessagePackResponseEnvelope
            {
                Result = result is null ? null : Serialize(result, GetReturnType(operation)),
            };

            return WriteEnvelopeAsync(context, envelope);
        }

        private async Task WriteEnvelopeAsync(Microsoft.AspNetCore.Http.HttpContext context, MessagePackResponseEnvelope envelope)
        {
            using var stream = new MemoryStream();
            _serializer.SerializeObject(stream, envelope, _typeShapeProvider.GetTypeShapeOrThrow(typeof(MessagePackResponseEnvelope)));
            stream.Position = 0;

            context.Response.Headers.ContentType = MimeTypes.MessagePack;
            context.Response.ContentLength = stream.Length;
            await stream.CopyToAsync(context.Response.Body, context.RequestAborted).ConfigureAwait(false);
        }

        private byte[] Serialize(object value, Type type)
        {
            using var stream = new MemoryStream();
            _serializer.SerializeObject(stream, value, _typeShapeProvider.GetTypeShapeOrThrow(type));
            return stream.ToArray();
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

        private static bool MatchesMediaType(ReadOnlySpan<char> value, ReadOnlySpan<char> expected)
        {
            int separator = value.IndexOf(';');
            if (separator >= 0)
                value = value[..separator];

            return value.Equals(expected, StringComparison.OrdinalIgnoreCase);
        }
    }

    internal sealed class MessagePackRequestEnvelope
    {
        public Dictionary<string, byte[]?>? Parameters { get; set; } = new(StringComparer.Ordinal);
        public List<ServiceQueryPart>? QueryOptions { get; set; }
        public bool IncludeTotalCount { get; set; }
    }

    internal sealed class MessagePackResponseEnvelope
    {
        public byte[]? Result { get; set; }
        public DomainServiceFault? Fault { get; set; }
    }
}
