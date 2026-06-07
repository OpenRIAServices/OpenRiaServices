using Nerdbank.MessagePack;
using OpenRiaServices.Client.DomainClients.MessagePack;
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
            return _serializerCache.GetOrAdd(service, (type, args) =>
            {
                HashSet<Type> allTypes = new HashSet<Type>(args.knownTypes);
                foreach (Type knownType in args.knownTypes)
                    foreach (var derivedType in Server.KnownTypeUtilities.ImportKnownTypes(knownType, true))
                        allTypes.Add(derivedType);


                DerivedShapeMapping<Entity> entityMapping = new();
                AddDerivedTypes(entityMapping, allTypes, static t => t.FullName!);

                DerivedShapeMapping<object> objectMapping = new();
                AddDerivedTypes(objectMapping, allTypes, static t => t.FullName!);

                return args.Item1.BaseSerializerSerializer with
                {
                    DerivedTypeUnions = [.. args.Item1.BaseSerializerSerializer.DerivedTypeUnions, entityMapping, objectMapping]
                };

            }, (this, knownTypes));
        }

        private static MessagePackSerializer ConfigureSerializer(MessagePackSerializer serializer)
        {

            serializer = serializer.WithHiFiDateTime();

            MessagePackConverter[] converters = [.. serializer.Converters, new MessagePack.MessagePackMethodParametersConverter()];

            return serializer with {
                PreserveReferences = ReferencePreservationMode.Off,
                Converters = ConverterCollection.Create(converters)
            };
        }

        private void AddDerivedTypes<T>(DerivedShapeMapping<T> mapping, IEnumerable<Type> derivedTypes, Func<Type, string> discriminator)
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
    }
}
