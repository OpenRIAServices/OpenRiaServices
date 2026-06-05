using Nerdbank.MessagePack;
using OpenRiaServices.Client.DomainClients.MessagePack;
using PolyType;
using System;
using System.Linq;
using System.Net.Http;

#nullable enable

namespace OpenRiaServices.Client.DomainClients
{
    /// <summary>
    /// A <see cref="DomainClientFactory"/> that communicates with the server using MessagePack over HTTP.
    /// </summary>
    public class MessagePackHttpDomainClientFactory : HttpDomainClientFactory
    {
        /// <inheritdoc />
        public MessagePackHttpDomainClientFactory(Uri serverBaseUri, HttpMessageHandler messageHandler)
            : base(serverBaseUri, messageHandler)
        {
        }

        /// <inheritdoc />
        public MessagePackHttpDomainClientFactory(Uri serverBaseUri, Func<Uri, HttpClient> httpClientFactory)
            : base(serverBaseUri, httpClientFactory)
        {
        }

        /// <inheritdoc />
        protected override DomainClient CreateDomainClientCore(Type serviceContract, Uri serviceUri, bool requiresSecureEndpoint)
        {
            HttpClient httpClient = CreateHttpClient(serviceUri, MessagePackHttpDomainClient.MediaType);
            return new MessagePackHttpDomainClient(httpClient, serviceContract, this);
        }

        internal MessagePackSerializer? BaseSerializerSerializer { get; } = ConfigureSerializer(new MessagePackSerializer());
        internal ITypeShapeProvider TypeShapeProver { get; } = PolyType.ReflectionProvider.ReflectionTypeShapeProvider.Default;


        private static MessagePackSerializer ConfigurePerServiceSerializer(Type serviceContract, MessagePackSerializer serializer)
        {
            //// TODO
            //// Register all entities as object for ChangeSetEntry
            //DerivedShapeMapping<Entity> objectMapping = new();
            //AddDerivedTypes(objectMapping, description.EntityTypes);
            //mappings.Add(objectMapping);

            serializer = serializer.WithHiFiDateTime();

            MessagePackConverter[] converters = serializer.Converters.Concat(new MessagePackConverter[] { new MessagePack.MessagePackMethodParametersConverter() }).ToArray();
            return serializer with { Converters = ConverterCollection.Create(converters) };
        }

        private static MessagePackSerializer ConfigureSerializer(MessagePackSerializer serializer)
        {
            //// TODO
            //// Register all entities as object for ChangeSetEntry
            //DerivedShapeMapping<Entity> objectMapping = new();
            //AddDerivedTypes(objectMapping, description.EntityTypes);
            //mappings.Add(objectMapping);

            serializer = serializer.WithHiFiDateTime();

            MessagePackConverter[] converters = serializer.Converters.Concat(new MessagePackConverter[] { new MessagePack.MessagePackMethodParametersConverter() }).ToArray();
            return serializer with { Converters = ConverterCollection.Create(converters) };
        }
    }
}
