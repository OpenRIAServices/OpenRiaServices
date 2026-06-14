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
                    allTypes.UnionWith(Server.KnownTypeUtilities.ImportKnownTypes(knownType, true));

                List<DerivedTypeUnion> mappings = new List<DerivedTypeUnion>();
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

                    //// Remove built in union support
                    //if (item.Key.BaseType == typeof(Entity))
                    //    continue;

                    //var mapping = CreateDerivedShapeMapping(item.Key);
                    //AddDerivedTypes(mapping, knownTypes, static t => t.Name!);
                    //mappings.Add(mapping);
                }

                // TODO: Remove and replace with something else
                // nerdbank may select base types
                converters.Add(new ObjectConverter<Entity>(allTypes));
                converters.Add(new ObjectConverter<object>(allTypes));
                //DerivedShapeMapping<Entity> entityMapping = new();
                //AddDerivedTypes(entityMapping, allTypes, static t => t.Name!);
                //mappings.Add(entityMapping);

                //DerivedShapeMapping<object> objectMapping = new();
                //AddDerivedTypes(objectMapping, allTypes, static t => t.Name!);
                //mappings.Add(objectMapping);

                return args.Item1.BaseSerializerSerializer with
                {
                    Converters = [.. args.Item1.BaseSerializerSerializer.Converters, .. converters],
                    DerivedTypeUnions = [.. args.Item1.BaseSerializerSerializer.DerivedTypeUnions, .. mappings],
                };

            }, (this, knownTypes));
        }

        private static MessagePackSerializer ConfigureSerializer(MessagePackSerializer serializer)
        {

            serializer = serializer.WithHiFiDateTime();

            MessagePackConverter[] converters = [.. serializer.Converters, new MessagePackMethodParametersConverter(), new ChangeSetEntryConverter()];

            return serializer with
            {
                PreserveReferences = ReferencePreservationMode.Off,
                Converters = ConverterCollection.Create(converters)
            };
        }

        private static DerivedTypeUnion CreateDerivedShapeMapping(Type baseType)
        {
            Type mappingType = typeof(DerivedShapeMapping<>).MakeGenericType(baseType);
            return (DerivedTypeUnion)Activator.CreateInstance(mappingType)!;
        }

        private void AddDerivedTypes(DerivedTypeUnion mapping, HashSet<Type> derivedTypes, Func<Type, string> discriminator)
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

            // MessagePack resolved types in the order they were registered, so we need to ensure derived types are registered before their base types to ensure correct deserialization.
            // We achieve this by sorting the types topologically based on their inheritance hierarchy.
            List<Type> sortedTypes = SortTypesTopologically(derivedTypes);

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

        // MessagePack resolved types in the order they were registered, so we need to ensure derived types are registered before their base types to ensure correct deserialization.
        // We achieve this by sorting the types topologically based on their inheritance hierarchy.
        private List<Type> SortTypesTopologically(HashSet<Type> types)
        {
            List<Type> result = new List<Type>(types.Count);
            HashSet<Type> remaining = new HashSet<Type>(types);


            while (remaining.Count > 0)
            {
                foreach (Type type in remaining)
                {
                    // If any base type is still in the remaining set, we need to process it first
                    bool canRemove = true;
                    for (Type? baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
                    {
                        if (remaining.Contains(baseType))
                        {
                            canRemove = false;
                            break;
                        }
                    }

                    if (canRemove)
                    {
                        result.Add(type);
                        remaining.Remove(type);
                    }
                }
            }

            result.Reverse();
            return result;

            //ArgumentNullException.ThrowIfNull(types);

            //static string GetSortKey(Type type) => type.FullName ?? type.Name;

            //Dictionary<Type, HashSet<Type>> edges = new(types.Count);
            //Dictionary<Type, int> indegree = new(types.Count);

            //foreach (Type type in types)
            //{
            //    edges[type] = [];
            //    indegree[type] = 0;
            //}

            //// Edge direction is: derived -> base (or implemented interface)
            //// so topological order registers more-derived types first.
            //for (int i = 0; i < types.Count; i++)
            //{
            //    Type current = types[i];

            //    for (int j = 0; j < types.Count; j++)
            //    {
            //        if (i == j)
            //        {
            //            continue;
            //        }

            //        Type candidateBase = types[j];
            //        if (!candidateBase.IsAssignableFrom(current))
            //        {
            //            continue;
            //        }

            //        if (edges[current].Add(candidateBase))
            //        {
            //            indegree[candidateBase]++;
            //        }
            //    }
            //}

            //var available = indegree
            //    .Where(kvp => kvp.Value == 0)
            //    .Select(kvp => kvp.Key)
            //    .OrderBy(GetSortKey, StringComparer.Ordinal)
            //    .ToList();

            //List<Type> result = new(types.Count);

            //while (available.Count > 0)
            //{
            //    Type current = available[0];
            //    available.RemoveAt(0);
            //    result.Add(current);

            //    foreach (Type dependent in edges[current])
            //    {
            //        indegree[dependent]--;
            //        if (indegree[dependent] == 0)
            //        {
            //            available.Add(dependent);
            //        }
            //    }

            //    available.Sort((a, b) => StringComparer.Ordinal.Compare(GetSortKey(a), GetSortKey(b)));
            //}

            //// Defensive fallback: if something unexpected prevented a full topological order,
            //// append remaining types deterministically.
            //if (result.Count != types.Count)
            //{
            //    HashSet<Type> added = new(result);
            //    result.AddRange(types.Where(t => !added.Contains(t)).OrderBy(GetSortKey, StringComparer.Ordinal));
            //}

            return result;
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
