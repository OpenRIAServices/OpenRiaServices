using Nerdbank.MessagePack;
using OpenRiaServices.Client.DomainClients.MessagePack;
using OpenRiaServices.Client.DomainClients.MessagePack.Converters;
using PolyType;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;

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
            TypeShapeProvider = typeShapeProvider ?? PolyType.ReflectionProvider.ReflectionTypeShapeProvider.Default;

            BaseSerializerSerializer = MessagePackUtility.ConfigureSerializer(serializer ?? new MessagePackSerializer(),
                [new MessagePackMethodParametersConverter(TypeShapeProvider), new ChangeSetEntryConverter()]);
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
                MessagePackHttpDomainClientFactory This = args.Item1;

                MethodInfo createMapping = typeof(MessagePackHttpDomainClientFactory).GetMethod(nameof(CreateDerivedTypeMapping), BindingFlags.NonPublic | BindingFlags.Instance)!;

                // Supress inheritance 
                HashSet<Type> allTypes = new HashSet<Type>(args.knownTypes);
                foreach (Type knownType in args.knownTypes)
                    allTypes.UnionWith(Server.KnownTypeUtilities.ImportDerivedTypes(knownType, true));

                Dictionary<Type, HashSet<Type>> allTypeHierarchies = ComputeKnownTypeSet(allTypes);

                List<DerivedTypeUnion> derivedTypeUnions = new List<DerivedTypeUnion>();
                foreach (var kvp in allTypeHierarchies)
                {
                    Type baseType = kvp.Key;
                    HashSet<Type> derivedTypes = kvp.Value;

                    // Skip base types since they have all KnownTypeAttributes
                    if (baseType.BaseType == typeof(Entity))
                        continue;

                    if (derivedTypes.Count > 1 || (derivedTypes.Count == 1 && !derivedTypes.Contains(baseType)))
                    {
                        derivedTypeUnions.Add((DerivedTypeUnion)createMapping.MakeGenericMethod(baseType).Invoke(This, [derivedTypes])!);
                    }
                }

                var objectConverter = new ObjectConverter<object>(allTypes, This.TypeShapeProvider, This.BaseSerializerSerializer);

                // Create the serializer
                return args.Item1.BaseSerializerSerializer with
                {
                    Converters = [objectConverter, .. args.Item1.BaseSerializerSerializer.Converters],
                    DerivedTypeUnions = [.. derivedTypeUnions, .. args.Item1.BaseSerializerSerializer.DerivedTypeUnions]
                };
            }, (this, knownTypes));
        }


        CustomDerivedShapeMapping<TBase> CreateDerivedTypeMapping<TBase>(HashSet<Type> derviedTypes)
        {
            var mapping = new CustomDerivedShapeMapping<TBase>();
            foreach (var derivedType in derviedTypes)
            {
                if (derivedType == typeof(TBase))
                    continue;

                var typeShape = TypeShapeProvider.GetTypeShapeOrThrow(derivedType);
                DerivedTypeIdentifier alias = new DerivedTypeIdentifier(Encoding.UTF8.GetString(MessagePackUtility.GetDiscriminator(typeShape.Type)));
                mapping.AddDerived(alias, typeShape);
            }

            return mapping;
        }

        /// <summary>
        /// Need to subclass in order to allow calling non-generic method to add mapping
        /// </summary>
        class CustomDerivedShapeMapping<TBase> : DerivedShapeMapping<TBase>
        {
            public void AddDerived(DerivedTypeIdentifier alias, ITypeShape typeShape)
            {
                base.Add(alias, typeShape);
            }
        }

        /// <summary>
        /// Computes the closure of known types for all the <paramref name="types" />.
        /// </summary>
        /// <returns>A dictionary, keyed by type and containing all the
        /// declared known types for it (including itself), including the transitive closure.
        /// </returns>
        private static Dictionary<Type, HashSet<Type>> ComputeKnownTypeSet(IEnumerable<Type> types)
        {
            Dictionary<Type, HashSet<Type>> closure = new Dictionary<Type, HashSet<Type>>();

            // Gather all the explicit derived types from attributes.
            foreach (Type entityType in types)
            {
                HashSet<Type> knownTypes = Server.KnownTypeUtilities.ImportDerivedTypes(entityType, /* inherit */ true);
                knownTypes.Add(entityType);
                closure[entityType] = knownTypes;
            }

            // 2nd pass -- add all the derived types' known types back to their base so we have the closure
            foreach (Type entityType in types)
            {
                HashSet<Type> knownTypes = closure[entityType];
                for (Type? baseType = entityType.BaseType;
                     baseType != null && baseType != typeof(Entity);
                     baseType = baseType.BaseType)
                {
                    if (closure.TryGetValue(baseType, out HashSet<Type>? hash))
                    {
                        hash.UnionWith(knownTypes);
                    }
                }

                System.Diagnostics.Debug.Assert(knownTypes.IsSubsetOf(types), "Known types should always contain at least the type itself.");
            }
            return closure;
        }
    }
}
