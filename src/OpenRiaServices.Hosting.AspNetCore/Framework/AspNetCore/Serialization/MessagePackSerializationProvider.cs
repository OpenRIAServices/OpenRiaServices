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
            IReadOnlyList<DerivedTypeUnion> derivedTypeMappings = BuildDerivedTypeMappings(domainServiceType);
            if (derivedTypeMappings.Count == 0)
            {
                return _serializer;
            }

            return _serializer with
            {
                DerivedTypeUnions = [.. _serializer.DerivedTypeUnions, .. derivedTypeMappings],
            };
        }

        private IReadOnlyList<DerivedTypeUnion> BuildDerivedTypeMappings(Type domainServiceType)
        {
            DomainServiceDescription description = DomainServiceDescription.GetDescription(domainServiceType);
            Type[] exposedTypes = description.EntityTypes.Concat(description.ComplexTypes).ToArray();
            List<DerivedTypeUnion> mappings = new();

            foreach (Type baseType in exposedTypes)
            {
                List<Type> derivedTypes = exposedTypes
                    .Where(candidate => candidate != baseType && baseType.IsAssignableFrom(candidate))
                    .ToList();

                if (derivedTypes.Count == 0)
                {
                    continue;
                }

                object mapping = CreateDerivedShapeMapping(baseType);
                AddDerivedTypes(mapping, derivedTypes);
                mappings.Add((DerivedTypeUnion)mapping);
            }

            return mappings;
        }

        private static object CreateDerivedShapeMapping(Type baseType)
        {
            Type mappingType = typeof(DerivedShapeMapping<>).MakeGenericType(baseType);
            return Activator.CreateInstance(mappingType)!;
        }

        private void AddDerivedTypes(object mapping, IEnumerable<Type> derivedTypes)
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
                MethodInfo addMethod = addMethodDefinition.MakeGenericMethod(derivedType);
                DerivedTypeIdentifier unionIdentifier = new DerivedTypeIdentifier(derivedType.Name);
                addMethod.Invoke(mapping, new object[] { unionIdentifier, _filteredTypeShapeProvider });
            }
        }

        private static MessagePackSerializer ConfigureSerializer(MessagePackSerializer serializer)
        {
            // TODO: Write custom DateTime converter which mimics WCF
            // Local => Local but where .ToUtc gives same value
            serializer = serializer.WithHiFiDateTime();

            MessagePackConverter[] converters = serializer.Converters.Concat(new MessagePackConverter[] { new MethodParametersConverter() }).ToArray();

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

            // TODO: Look it TypeShapeMapping should be generated or not (KnownTypes can be enough)
            // Allow DerivedShapeMapping
            // Can we add Object and allow all entity types for it ?

            //DerivedShapeMapping<Animal> mapping = new();
            //mapping.Add<Horse>(1);
            //mapping.Add<Cow>(2);
            //return serializer with { DerivedTypeMappings = [.. serializer.DerivedTypeMappings, mapping] };

            */
        }
    }
}
