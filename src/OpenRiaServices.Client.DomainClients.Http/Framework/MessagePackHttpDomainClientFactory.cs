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
        public MessagePackHttpDomainClientFactory(Uri serverBaseUri, Func<Uri, HttpClient> httpClientFactory, MessagePackSerializer? serializer = null, ITypeShapeProvider? typeShapeProvider = null)
            : base(serverBaseUri, httpClientFactory)
        {
            BaseSerializerSerializer = ConfigureSerializer(serializer ?? new MessagePackSerializer());
            TypeShapeProvider = typeShapeProvider ?? PolyType.ReflectionProvider.ReflectionTypeShapeProvider.Default;
        }

        /// <inheritdoc />
        protected override DomainClient CreateDomainClientCore(Type serviceContract, Uri serviceUri, bool requiresSecureEndpoint)
        {
            HttpClient httpClient = CreateHttpClient(serviceUri, MessagePackHttpDomainClient.MediaType);
            return new MessagePackHttpDomainClient(httpClient, serviceContract, this);
        }

        internal MessagePackSerializer? BaseSerializerSerializer { get; }
        internal ITypeShapeProvider TypeShapeProvider { get; }

        private static MessagePackSerializer ConfigureSerializer(MessagePackSerializer serializer)
        {
            //// TODO
            //// Register all entities as object for ChangeSetEntry
            //DerivedShapeMapping<Entity> objectMapping = new();
            //AddDerivedTypes(objectMapping, description.EntityTypes);
            //mappings.Add(objectMapping);

            serializer = serializer.WithHiFiDateTime();

            MessagePackConverter[] converters = [.. serializer.Converters, new MessagePack.MessagePackMethodParametersConverter()];
            return serializer with { Converters = ConverterCollection.Create(converters) };
        }
    }
}
