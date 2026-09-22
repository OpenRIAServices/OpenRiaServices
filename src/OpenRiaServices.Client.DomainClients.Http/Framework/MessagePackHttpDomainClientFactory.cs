using Nerdbank.MessagePack;
using OpenRiaServices.Client.DomainClients.MessagePack;
using OpenRiaServices.Client.DomainClients.MessagePack.Converters;
using PolyType;
using PolyType.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Http;
using System.Reflection;

#nullable enable

namespace OpenRiaServices.Client.DomainClients
{
    /// <summary>
    /// A <see cref="DomainClientFactory"/> that communicates with the server using MessagePack over HTTP.
    /// </summary>
    public class MessagePackHttpDomainClientFactory : HttpDomainClientFactory
    {
        private readonly ConcurrentDictionary<Type, MessagePackSerializer> _serializerCache = new ConcurrentDictionary<Type, MessagePackSerializer>();

        /// <summary>
        /// Gets or sets the options used when creating the response <see cref="PipeReader" /> used for reading MessagePack responses.
        /// The default buffer size is increased to 256 KB
        /// </summary>
        /// <remarks>Ensure at least 64 KB is used to avoid very slow request parsing.</remarks>
        public StreamPipeReaderOptions ResponsePipeReaderOptions
        {
            get;
            init => field = value ?? throw new ArgumentNullException(nameof(value));
        } = new StreamPipeReaderOptions(bufferSize: 256 * 1024, leaveOpen: true);

        /// <inheritdoc />
        public MessagePackHttpDomainClientFactory(Uri serverBaseUri, Func<Uri, HttpClient> httpClientFactory, MessagePackSerializer? serializer = null, ITypeShapeProvider? typeShapeProvider = null)
            : base(serverBaseUri, httpClientFactory)
        {
            BaseSerializerSerializer = MessagePackUtility.ConfigureSerializer(serializer ?? new MessagePackSerializer(),
                [new MessagePackMethodParametersConverter(), new ChangeSetEntryConverter()]);

            TypeShapeProvider = typeShapeProvider ?? PolyType.ReflectionProvider.ReflectionTypeShapeProvider.Default;
        }

        /// <inheritdoc />
        protected override DomainClient CreateDomainClientCore(Type serviceContract, Uri serviceUri, bool requiresSecureEndpoint)
        {
            HttpClient httpClient = CreateHttpClient(serviceUri, MessagePackHttpDomainClient.MediaType);
            return new MessagePackHttpDomainClient(httpClient, serviceContract, this);
        }

        internal MessagePackSerializer BaseSerializerSerializer { get; }
        internal ITypeShapeProvider TypeShapeProvider { get; }

        internal MessagePackSerializer GetSerializer(Type service, IEnumerable<Type> knownTypes)
        {
            return _serializerCache.GetOrAdd(service, static (_, args) =>
            {
                var converterFactory = new ObjectConverterFactory(args.knownTypes);
                return args.Item1.BaseSerializerSerializer with
                {
                    ConverterFactories = [converterFactory, .. args.Item1.BaseSerializerSerializer.ConverterFactories],
                    DerivedTypeUnions = [.. converterFactory.GetDerivedTypeUnions(), .. args.Item1.BaseSerializerSerializer.DerivedTypeUnions]
                };

            }, (this, knownTypes));
        }

    }
}
