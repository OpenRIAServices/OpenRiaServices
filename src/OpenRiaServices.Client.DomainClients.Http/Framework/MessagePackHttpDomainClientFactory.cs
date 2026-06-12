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
            BaseSerializerSerializer = ConfigureSerializer(serializer ?? new MessagePackSerializer());
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
                    foreach (var derivedType in Server.KnownTypeUtilities.ImportKnownTypes(knownType, true))
                        allTypes.Add(derivedType);

                List<DerivedTypeUnion> mappings = new List<DerivedTypeUnion>();
                foreach(var item in ComputeKnownTypeSet(allTypes))
                {
                    // Skip base type since they have KnownType attribute already
                    if (item.Key.BaseType == typeof(Entity))
                        continue;

                    var knownTypes = item.Value;
                    // Skip mapping with no derived types
                    if (knownTypes.Count == 0
                        || (knownTypes.Count == 1 && knownTypes.Contains(item.Key)))
                        continue;

                    var mapping = CreateDerivedShapeMapping(item.Key);
                    AddDerivedTypes(mapping, knownTypes, static t => t.Name!);
                    mappings.Add(mapping);
                }

                DerivedShapeMapping<Entity> entityMapping = new();
                AddDerivedTypes(entityMapping, allTypes, static t => t.Name!);
                mappings.Add(entityMapping);

                DerivedShapeMapping<object> objectMapping = new();
                AddDerivedTypes(objectMapping, allTypes, static t => t.Name!);
                mappings.Add(objectMapping);

                return args.Item1.BaseSerializerSerializer with
                {
                    DerivedTypeUnions = [.. args.Item1.BaseSerializerSerializer.DerivedTypeUnions, .. mappings]
                };

            }, (this, knownTypes));
        }

        private static MessagePackSerializer ConfigureSerializer(MessagePackSerializer serializer)
        {

            serializer = serializer.WithHiFiDateTime();

            MessagePackConverter[] converters = [.. serializer.Converters, new MessagePackMethodParametersConverter(), new ChangeSetEntryConverter()];

            return serializer with {
                PreserveReferences = ReferencePreservationMode.Off,
                Converters = ConverterCollection.Create(converters)
            };
        }

        private static DerivedTypeUnion CreateDerivedShapeMapping(Type baseType)
        {
            Type mappingType = typeof(DerivedShapeMapping<>).MakeGenericType(baseType);
            return (DerivedTypeUnion)Activator.CreateInstance(mappingType)!;
        }

        private void AddDerivedTypes(DerivedTypeUnion mapping, IEnumerable<Type> derivedTypes, Func<Type, string> discriminator)
        {
            MethodInfo? addMethodDefinition = mapping.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(m => m.Name == "Add" && m.IsGenericMethodDefinition)
                .FirstOrDefault(m =>
                {
                    ParameterInfo[] parameters = m.GetParameters();
                    return parameters.Length == 2
                        && parameters[0].ParameterType == typeof(DerivedTypeIdentifier)
                        && parameters[1].ParameterType.IsGenericType
                        && parameters[1].ParameterType.GetGenericTypeDefinition() == typeof(PolyType.ITypeShape<>);
                });

            if (addMethodDefinition == null)
            {
                throw new InvalidOperationException($"Unable to locate DerivedShapeMapping.Add<TDerived>(..., ...) on {mapping.GetType().FullName}.");
            }

            foreach (Type derivedType in derivedTypes)
            {
                MethodInfo addMethod = addMethodDefinition.MakeGenericMethod(derivedType);
                DerivedTypeIdentifier unionIdentifier = new DerivedTypeIdentifier(discriminator(derivedType));
                ITypeShape typeShape = TypeShapeProvider.GetTypeShapeOrThrow(derivedType);

                // Ensure we register the actual type shape, in case it is a base class (union)
                // To avoid multiple discriminators for the same type
                while (typeShape is IUnionTypeShape unionShape)
                    typeShape = unionShape.BaseType;

                addMethod.Invoke(mapping, [unionIdentifier, typeShape]);
            }
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
