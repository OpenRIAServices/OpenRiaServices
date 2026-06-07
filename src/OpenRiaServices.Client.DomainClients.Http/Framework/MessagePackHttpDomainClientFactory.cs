using Nerdbank.MessagePack;
using OpenRiaServices.Client.DomainClients.MessagePack;
using PolyType;
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
            //// TODO
            //// Register all entities as object for ChangeSetEntry
            //DerivedShapeMapping<Entity> objectMapping = new();
            //AddDerivedTypes(objectMapping, description.EntityTypes);
            //mappings.Add(objectMapping);

            serializer = serializer.WithHiFiDateTime();

            MessagePackConverter[] converters = [.. serializer.Converters, new MessagePack.MessagePackMethodParametersConverter()];
            return serializer with { Converters = ConverterCollection.Create(converters) };
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
                        && parameters[1].ParameterType == typeof(PolyType.ITypeShapeProvider);
                });

            if (addMethodDefinition == null)
            {
                throw new InvalidOperationException($"Unable to locate DerivedShapeMapping.Add<TDerived>(..., ...) on {mapping.GetType().FullName}.");
            }


            foreach (Type derivedType in derivedTypes)
            {
                // TODO: Determin if base type should be excluded or not, without base type will be encoded as null..
                //if (mapping.BaseType == derivedType)
                //{
                //    continue;
                //}

                MethodInfo addMethod = addMethodDefinition.MakeGenericMethod(derivedType);
                DerivedTypeIdentifier unionIdentifier = new DerivedTypeIdentifier(discriminator(derivedType));
                addMethod.Invoke(mapping, new object[] { unionIdentifier, TypeShapeProvider });
            }
        }
    }
}
