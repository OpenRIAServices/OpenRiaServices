using Nerdbank.MessagePack;
using OpenRiaServices.Client.DomainClients.MessagePack;
using OpenRiaServices.Client.DomainClients.MessagePack.Converters;
using PolyType;
using PolyType.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        ConcurrentDictionary<Type, MessagePackSerializer> _serializerCache = new ConcurrentDictionary<Type, MessagePackSerializer>();

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
            return _serializerCache.GetOrAdd(service, (serviceType, args) =>
            {
                HashSet<Type> allTypes = new HashSet<Type>(args.knownTypes);
                foreach (Type knownType in args.knownTypes)
                    allTypes.UnionWith(Server.KnownTypeUtilities.ImportKnownTypes(knownType, true));

                List<MessagePackConverter> converters = new List<MessagePackConverter>();
                foreach (var item in ComputeKnownTypeSet(allTypes))
                {
                    var knownTypes = item.Value;

                    // Add all discovered trypes
                    allTypes.UnionWith(knownTypes);

                    //// Skip mapping with no derived types
                    if (knownTypes.Count == 0
                        || (knownTypes.Count == 1 && knownTypes.Contains(item.Key)))
                        continue;
                }

                converters.Add(new ObjectConverter<object>(allTypes));

                return args.Item1.BaseSerializerSerializer with
                {
                    Converters = [.. args.Item1.BaseSerializerSerializer.Converters, .. converters],
                };

            }, (this, knownTypes));
        }

        /// <summary>
        /// Computes the closure of known types for all the <paramref name="types" />.
        /// </summary>
        /// <returns>A dictionary, keyed by type and containing all the
        /// declared known types for it, including the transitive closure.
        /// </returns>
        private Dictionary<Type, HashSet<Type>> ComputeKnownTypeSet(IEnumerable<Type> types)
        {
            Dictionary<Type, HashSet<Type>> closure = new Dictionary<Type, HashSet<Type>>();

            // Gather all the explicit known types from attributes.
            // Because we ask to inherit [KnownType], we will collect the full closure
            foreach (Type entityType in types)
            {
                // Get all [KnownType]'s and subselect only those that actually derive from this entity
                IEnumerable<Type> knownTypes = Server.KnownTypeUtilities.ImportKnownTypes(entityType, /* inherit */ true).Where(t => entityType.IsAssignableFrom(t));
                closure[entityType] = new HashSet<Type>(knownTypes);
            }

            // 2nd pass -- add all the derived types' known types back to their base so we have the closure
            foreach (Type entityType in types)
            {
                HashSet<Type> knownTypes = closure[entityType];
                for (Type? baseType = entityType.BaseType;
                     baseType != null && baseType != typeof(Entity);
                     baseType = baseType.BaseType)
                {
                    HashSet<Type> hash = closure[baseType];
                    hash.UnionWith(knownTypes);
                }
            }
            return closure;
        }
    }
}
