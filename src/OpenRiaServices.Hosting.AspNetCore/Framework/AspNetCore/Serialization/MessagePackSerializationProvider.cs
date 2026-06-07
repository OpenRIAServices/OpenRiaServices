using Nerdbank.MessagePack;
using OpenRiaServices.Hosting.AspNetCore.Serialization.MessagePack;
using OpenRiaServices.Hosting.AspNetCore.Serialization.MessagePack.Converters;
using OpenRiaServices.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization
{
    internal sealed class MessagePackSerializationProvider : ISerializationProvider
    {
        private readonly MessagePackSerializationOptions _options;
        private readonly FilteredTypeShapeProvider _filteredTypeShapeProvider;
        private readonly MessagePackSerializer _serializer;
        private readonly ConcurrentDictionary<Type, MessagePackSerializer> _serializerByDomainServiceType = new();

        public MessagePackSerializationProvider(MessagePackSerializationOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _filteredTypeShapeProvider = new FilteredTypeShapeProvider(options.TypeShapeProvider);
            _serializer = ConfigureSerializer(options.Serializer);
        }

        public RequestSerializer GetRequestSerializer(DomainOperationEntry operation)
            => new MessagePackRequestSerializer(operation, GetSerializerForDomainService(operation.DomainServiceType), _filteredTypeShapeProvider);

        private MessagePackSerializer GetSerializerForDomainService(Type domainServiceType)
            => _serializerByDomainServiceType.GetOrAdd(domainServiceType, CreateSerializerForDomainService);

        // TODO: Add issue to PolyType to support KnownTypeAttribute (when no DataContact is present)
        private MessagePackSerializer CreateSerializerForDomainService(Type domainServiceType)
        {
            DomainServiceDescription description = DomainServiceDescription.GetDescription(domainServiceType);
            Wcf.DomainServiceSerializationSurrogate surrogateProvider = new Wcf.DomainServiceSerializationSurrogate(description);

            IReadOnlyList<DerivedTypeUnion> derivedTypeMappings = BuildDerivedTypeMappings(description, surrogateProvider);

            // Create converters to handle surrogates,
            // TODO: Determine if it should be a provider intead ? 
            List<MessagePackConverter> converters = new();
            foreach (var entityType in description.EntityTypes.Concat(description.ComplexTypes))
            {
                if (surrogateProvider.GetSurrogateType(entityType) is Type surrogateType)
                {
                    // TODO: Consider only doing this if there are any derived types, otherwise we can just register surrogateType directly and avoid the extra surrogate converter (which adds some overhead during serialization)
                    if (description.EntityKnownTypes.TryGetValue(entityType, out var candidates)
                        && (candidates.Count > 1 || (candidates.Count == 1 && !candidates.Contains(entityType))))
                    {
                        // Find base type for surrogate (This type has all BuildDerivedTypeMappings configured) 
                        surrogateType = surrogateProvider.GetSurrogateType(description.GetRootEntityType(entityType));
                    }

                    converters.Add((MessagePackConverter)
                        Activator.CreateInstance(typeof(SurrogateConverter<,>).MakeGenericType([surrogateType, entityType]), args: [surrogateProvider])!);
                }
            }
            // All surrogates implement IClonable, use that as common base class for surrogates
            converters.Add(new SurrogateConverter<ICloneable, object>(surrogateProvider));

            return _serializer with
            {
                //DerivedTypeUnions = [.. _serializer.DerivedTypeUnions, .. derivedTypeMappings],
                Converters = ConverterCollection.Create([.. _serializer.Converters, .. converters])
            };
        }

        private IReadOnlyList<DerivedTypeUnion> BuildDerivedTypeMappings(DomainServiceDescription description, Wcf.DomainServiceSerializationSurrogate surrogateProvider)
        {
            List<DerivedTypeUnion> mappings = new();

            foreach (var baseType in description.RootEntityTypes)
            {
                var candidates = description.EntityKnownTypes[baseType];
                if (candidates.Count == 0 || (candidates.Count == 1 && candidates.Contains(baseType)))
                {
                    continue;
                }

                //if (candidates.Contains(baseType))
                //    System.Diagnostics.Debugger.Break();

                // From original Type
                //object mapping = CreateDerivedShapeMapping(surrogateProvider.GetSurrogateType(baseType));
                //AddDerivedTypes(mapping, candidates.Select(t => surrogateProvider.GetSurrogateType(t)), static t => t.Name);
                DerivedTypeUnion mapping = CreateDerivedShapeMapping(surrogateProvider.GetSurrogateType(baseType));
                AddDerivedTypes(mapping, candidates.Select(t => surrogateProvider.GetSurrogateType(t)), static t => t.Name);
                mappings.Add(mapping);
            }

            // Register all entities as object for ChangeSetEntry
            // when dealing with method parameters
            // OR use marshaller in order to allow setting different converters for method parameters (e.g. DateTime) vs entities (e.g. DateTimeOffset)
            DerivedShapeMapping<ICloneable> objectMapping = new();
            AddDerivedTypes(objectMapping, description.EntityTypes.Select(t => surrogateProvider.GetSurrogateType(t)), static t => t.FullName!);
            mappings.Add(objectMapping);

            return mappings;
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
                addMethod.Invoke(mapping, new object[] { unionIdentifier, _filteredTypeShapeProvider });
            }
        }

        private static MessagePackSerializer ConfigureSerializer(MessagePackSerializer serializer)
        {
            // TODO: Write custom DateTime converter which mimics WCF
            // Local => Local but where .ToUtc gives same value
            serializer = serializer.WithHiFiDateTime();

            MessagePackConverter[] converters = serializer.Converters
                .Concat(new MessagePackConverter[]
                {
                    new MethodParametersConverter(),
                    //new ChangeSetEntryConverter()
                })
                .ToArray();

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
            */
        }
    }
}
