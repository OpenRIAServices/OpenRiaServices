using Nerdbank.MessagePack;
using OpenRiaServices.Hosting.AspNetCore.Serialization.MessagePack;
using OpenRiaServices.Hosting.AspNetCore.Serialization.MessagePack.Converters;
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
            _serializer = ConfigureSerializer(options.Serializer);
        }

        public RequestSerializer GetRequestSerializer(DomainOperationEntry operation)
            => new MessagePackRequestSerializer(operation, _serializer, _filteredTypeShapeProvider, _options.TypeShapeProvider);

        private static MessagePackSerializer ConfigureSerializer(MessagePackSerializer serializer)
        {
            MessagePackConverter[] converters = serializer.Converters.Concat(new MessagePackConverter[] { new MethodParametersConverter() }).ToArray();

            serializer = serializer
                .WithHiFiDateTime();

            return serializer with
            {
                Converters = ConverterCollection.Create(converters),
                 //PreserveReferences
                  
            };

            /*
             *             MessagePackSerializer serializer = new()
            {
                PreserveReferences = ReferencePreservationMode.RejectCycles,
                //PropertyNamingPolicy =   
                StartingContext = new SerializationContext()
                {
                    //CancellationToken = ct,
                    //TypeShapeProvider
                    MaxDepth = 256
                }
            };

            // TODO: Look it TypeShapeMapping should be generated or not (KnownTypes can be enough)
            // Allow DerivedShapeMapping
            // Can we add Object and allow all entity types for it ?

            //DerivedShapeMapping<Animal> mapping = new();
            //mapping.Add<Horse>(1);
            //mapping.Add<Cow>(2);
            //return serializer with { DerivedTypeMappings = [.. serializer.DerivedTypeMappings, mapping] };

            */
        }
    }

    internal sealed class MessagePackRequestSerializer : RequestSerializer
    {
        private readonly MessagePackSerializer _serializer;
        private readonly ITypeShapeProvider _typeShapeProvider;
        private readonly DomainOperationEntry _operation;
        private readonly MessagePackSerializer _operationSerializer;

        public MessagePackRequestSerializer(DomainOperationEntry operation, MessagePackSerializer serializer, ITypeShapeProvider typeShapeProvider, ITypeShapeProvider envelopeTypeShapeProvider)
        {
            _operation = operation ?? throw new ArgumentNullException(nameof(operation));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _typeShapeProvider = typeShapeProvider ?? throw new ArgumentNullException(nameof(typeShapeProvider));
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

        public override async Task WriteErrorAsync(Microsoft.AspNetCore.Http.HttpContext context, DomainServiceFault fault, DomainOperationEntry operation)
        {
            context.Response.Headers.ContentType = MimeTypes.MessagePack;
            await _operationSerializer.SerializeObjectAsync(
                context.Response.Body,
                new MessagePackResponseEnvelopeBase { Fault = fault},
                _typeShapeProvider.GetTypeShapeOrThrow<MessagePackResponseEnvelopeBase>(),
                context.RequestAborted).ConfigureAwait(false);
        }

        public override Task WriteResponseAsync(Microsoft.AspNetCore.Http.HttpContext context, object? result, DomainOperationEntry operation)
            => WriteEnvelopeAsync(context, CreateResponseEnvelope(operation, result, fault: null));

        private async Task WriteEnvelopeAsync(Microsoft.AspNetCore.Http.HttpContext context, object envelope)
        {
            context.Response.Headers.ContentType = MimeTypes.MessagePack;
            await _operationSerializer.SerializeObjectAsync(
                context.Response.Body,
                envelope,
                _typeShapeProvider.GetTypeShapeOrThrow(envelope.GetType()),
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
                _ => typeof(MessagePackRequestEnvelopeBase),
            };

            return (MessagePackRequestEnvelopeBase?)await _operationSerializer.DeserializeObjectAsync(
                context.Request.Body,
                _typeShapeProvider.GetTypeShapeOrThrow(envelopeType),
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
            // iF the operation is a query, the return type of the envelope is QueryResult<T>, where T is the associated type of the operation. For submit operations, the return type is IEnumerable<ChangeSetEntry>. For invoke operations, the return type is the declared return type of the operation.

            if (fault is not null)
                return new MessagePackResponseEnvelopeBase { Fault = fault };


            if (operation.Operation == DomainOperation.Custom && operation.Name == "Submit")
            {
                return new MessagePackSubmitResponseEnvelope((IEnumerable<ChangeSetEntry>)result!);
            }


            Type responseEnvelopeType = operation.Operation switch
            {
                DomainOperation.Query => typeof(MessagePackQueryResponseEnvelope<>).MakeGenericType(operation.AssociatedType!),
                DomainOperation.Invoke => typeof(MessagePackInvokeResponseEnvelope<>).MakeGenericType(GetReturnType(operation)),
                _ => throw new NotSupportedException()
            };

            object envelope = Activator.CreateInstance(responseEnvelopeType, result)!;
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
 
}
