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
        private readonly MessagePackSerializer _serializer;

        public MessagePackSerializationProvider(MessagePackSerializationOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _filteredTypeShapeProvider = new FilteredTypeShapeProvider(options.TypeShapeProvider);
            _serializer = AddMethodParametersConverter(options.Serializer);
        }

        public RequestSerializer GetRequestSerializer(DomainOperationEntry operation)
            => new MessagePackRequestSerializer(operation, _serializer, _filteredTypeShapeProvider, _options.TypeShapeProvider);
        private static MessagePackSerializer AddMethodParametersConverter(MessagePackSerializer serializer)
        {
            MessagePackConverter[] converters = serializer.Converters.Concat(new MessagePackConverter[] { new MethodParametersConverter() }).ToArray();
            return serializer with { Converters = ConverterCollection.Create(converters) };
        }
    }

    internal sealed class MessagePackRequestSerializer : RequestSerializer
    {
        private readonly MessagePackSerializer _serializer;
        private readonly ITypeShapeProvider _typeShapeProvider;
        private readonly ITypeShapeProvider _envelopeTypeShapeProvider;
        private readonly DomainOperationEntry _operation;
        private readonly MessagePackSerializer _operationSerializer;

        public MessagePackRequestSerializer(DomainOperationEntry operation, MessagePackSerializer serializer, ITypeShapeProvider typeShapeProvider, ITypeShapeProvider envelopeTypeShapeProvider)
        {
            _operation = operation ?? throw new ArgumentNullException(nameof(operation));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _typeShapeProvider = typeShapeProvider ?? throw new ArgumentNullException(nameof(typeShapeProvider));
            _envelopeTypeShapeProvider = envelopeTypeShapeProvider ?? throw new ArgumentNullException(nameof(envelopeTypeShapeProvider));
            _operationSerializer = CreateOperationSerializer(_serializer, _operation, _typeShapeProvider);
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

                    throw new Microsoft.AspNetCore.Http.BadHttpRequestException($"No value provided for parameter '{parameter.Name}'", (int)HttpStatusCode.BadRequest);
                }

                if (value is null)
                {
                    if (!parameter.IsNullable)
                        throw new Microsoft.AspNetCore.Http.BadHttpRequestException($"Null value provided for parameter '{parameter.Name}'", (int)HttpStatusCode.BadRequest);

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
                _typeShapeProvider.GetTypeShapeOrThrow<MessagePackResponseEnvelopeBase>(),
        }

        public override Task WriteResponseAsync(Microsoft.AspNetCore.Http.HttpContext context, object? result, DomainOperationEntry operation)
            => WriteEnvelopeAsync(context, CreateResponseEnvelope(operation, result, fault: null));

        private async Task WriteEnvelopeAsync(Microsoft.AspNetCore.Http.HttpContext context, object envelope)
        {
            context.Response.Headers.ContentType = MimeTypes.MessagePack;
            await _operationSerializer.SerializeObjectAsync(
                context.Response.Body,
                envelope,
                _envelopeTypeShapeProvider.GetTypeShapeOrThrow(envelope.GetType()),
                context.RequestAborted).ConfigureAwait(false);
        }


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

            return (MessagePackRequestEnvelopeBase?)await _operationSerializer.DeserializeObjectAsync(
                context.Request.Body,
                _envelopeTypeShapeProvider.GetTypeShapeOrThrow(envelopeType),
                context.RequestAborted).ConfigureAwait(false)
                ?? (MessagePackRequestEnvelopeBase)Activator.CreateInstance(envelopeType)!;
        }

        private static MessagePackSerializer CreateOperationSerializer(MessagePackSerializer serializer, DomainOperationEntry operation, ITypeShapeProvider typeShapeProvider)
        {
            SerializationContext context = serializer.StartingContext;
            context[MethodParametersConverter.OperationKey] = operation;
            context[MethodParametersConverter.TypeShapeProviderKey] = typeShapeProvider;
            return serializer with { StartingContext = context };
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
        public Dictionary<string, object?> Values { get; set; } = new(StringComparer.Ordinal);
    }

    internal sealed class MethodParametersConverter : MessagePackConverter<MethodParameters?>
    {
        internal static readonly object OperationKey = new();
        internal static readonly object TypeShapeProviderKey = new();

        public override bool PreferAsyncSerialization => true;

        public override MethodParameters? Read(ref MessagePackReader reader, SerializationContext context)
        {
            if (reader.TryReadNil())
                return null;

            context.DepthStep();
            DomainOperationEntry operation = GetOperation(context);
            var parametersByName = operation.Parameters.ToDictionary(p => p.Name, StringComparer.Ordinal);
            var result = new MethodParameters();

            int count = reader.ReadMapHeader();
            for (int i = 0; i < count; i++)
            {
                string? name = reader.ReadString();
                if (name is not null && parametersByName.TryGetValue(name, out DomainOperationParameter parameter))
                {
                    result.Values[name] = ReadValue(ref reader, parameter.ParameterType, context);
                }
                else
                {
                    reader.Skip(context);
                }
            }

            return result;
        }

        public override void Write(ref MessagePackWriter writer, in MethodParameters? value, SerializationContext context)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            context.DepthStep();
            DomainOperationEntry operation = GetOperation(context);
            var parametersByName = operation.Parameters.ToDictionary(p => p.Name, StringComparer.Ordinal);
            writer.WriteMapHeader(value.Values.Count);

            foreach (var parameterValue in value.Values)
            {
                writer.Write(parameterValue.Key);
                if (parametersByName.TryGetValue(parameterValue.Key, out DomainOperationParameter parameter))
                {
                    WriteValue(ref writer, parameterValue.Value, parameter.ParameterType, context);
                }
                else
                {
                    writer.WriteNil();
                }
            }
        }

        public override async ValueTask<MethodParameters?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
        {
            await reader.BufferNextStructureAsync(context).ConfigureAwait(false);
            MessagePackReader bufferedReader = reader.CreateBufferedReader();
            if (bufferedReader.TryReadNil())
            {
                reader.ReturnReader(ref bufferedReader);
                return null;
            }

            context.DepthStep();
            DomainOperationEntry operation = GetOperation(context);
            var parametersByName = operation.Parameters.ToDictionary(p => p.Name, StringComparer.Ordinal);
            var result = new MethodParameters();

            int count = bufferedReader.ReadMapHeader();
            reader.ReturnReader(ref bufferedReader);

            for (int i = 0; i < count; i++)
            {
                await reader.BufferNextStructureAsync(context).ConfigureAwait(false);
                bufferedReader = reader.CreateBufferedReader();
                string? name = bufferedReader.ReadString();
                reader.ReturnReader(ref bufferedReader);

                if (name is not null && parametersByName.TryGetValue(name, out DomainOperationParameter parameter))
                {
                    result.Values[name] = await ReadValueAsync(reader, parameter.ParameterType, context).ConfigureAwait(false);
                }
                else
                {
                    await reader.BufferNextStructureAsync(context).ConfigureAwait(false);
                    bufferedReader = reader.CreateBufferedReader();
                    bufferedReader.Skip(context);
                    reader.ReturnReader(ref bufferedReader);
                }
            }

            return result;
        }

        public override async ValueTask WriteAsync(MessagePackAsyncWriter writer, MethodParameters? value, SerializationContext context)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            context.DepthStep();
            DomainOperationEntry operation = GetOperation(context);
            var parametersByName = operation.Parameters.ToDictionary(p => p.Name, StringComparer.Ordinal);
            writer.WriteMapHeader(value.Values.Count);

            foreach (var parameterValue in value.Values)
            {
                writer.Write(static (ref MessagePackWriter syncWriter, string key) => syncWriter.Write(key), parameterValue.Key);
                if (parametersByName.TryGetValue(parameterValue.Key, out DomainOperationParameter parameter))
                {
                    await WriteValueAsync(writer, parameterValue.Value, parameter.ParameterType, context).ConfigureAwait(false);
                }
                else
                {
                    writer.WriteNil();
                }

                await writer.FlushIfAppropriateAsync(context).ConfigureAwait(false);
            }
        }

        private static DomainOperationEntry GetOperation(SerializationContext context)
            => (DomainOperationEntry?)context[OperationKey]
                ?? throw new MessagePackSerializationException("Domain operation metadata is required to serialize method parameters.");

        private static ITypeShapeProvider GetTypeShapeProvider(SerializationContext context)
            => (ITypeShapeProvider?)context[TypeShapeProviderKey] ?? context.TypeShapeProvider;

        private static object? ReadValue(ref MessagePackReader reader, Type parameterType, SerializationContext context)
        {
            if (reader.TryReadNil())
                return null;

            return context.GetConverter(parameterType, GetTypeShapeProvider(context)).ReadObject(ref reader, context);
        }

        private static async ValueTask<object?> ReadValueAsync(MessagePackAsyncReader reader, Type parameterType, SerializationContext context)
        {
            await reader.BufferNextStructureAsync(context).ConfigureAwait(false);
            MessagePackReader bufferedReader = reader.CreateBufferedReader();
            if (bufferedReader.TryReadNil())
            {
                reader.ReturnReader(ref bufferedReader);
                return null;
            }

            reader.ReturnReader(ref bufferedReader);
            return await context.GetConverter(parameterType, GetTypeShapeProvider(context)).ReadObjectAsync(reader, context).ConfigureAwait(false);
        }

        private static void WriteValue(ref MessagePackWriter writer, object? value, Type parameterType, SerializationContext context)
        {
            if (value is null)
                writer.WriteNil();
            else
                context.GetConverter(parameterType, GetTypeShapeProvider(context)).WriteObject(ref writer, value, context);
        }

        private static ValueTask WriteValueAsync(MessagePackAsyncWriter writer, object? value, Type parameterType, SerializationContext context)
            => value is null
                ? WriteNilAsync(writer)
                : context.GetConverter(parameterType, GetTypeShapeProvider(context)).WriteObjectAsync(writer, value, context);

        private static ValueTask WriteNilAsync(MessagePackAsyncWriter writer)
        {
            writer.WriteNil();
            return default;
        }
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
