using Nerdbank.MessagePack;
using OpenRiaServices.Hosting.AspNetCore.Serialization.MessagePack;
using OpenRiaServices.Hosting.AspNetCore.Serialization.MessagePack.Converters;
using OpenRiaServices.Server;
using System;
using System.Linq;
using System.Runtime.Serialization;

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
            => new MessagePackRequestSerializer(operation, _serializer, _filteredTypeShapeProvider);

        private static MessagePackSerializer ConfigureSerializer(MessagePackSerializer serializer)
        {
            // TODO: Write custom DateTime converter which mimics WCF
            // Local => Local but where .ToUtc gives same value
            serializer = serializer.WithHiFiDateTime();

            MessagePackConverter[] converters = serializer.Converters.Concat(new MessagePackConverter[] { new MethodParametersConverter() }).ToArray();

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
}
