using Nerdbank.MessagePack;
using OpenRiaServices.Hosting.AspNetCore.Serialization.MessagePack;
using OpenRiaServices.Hosting.AspNetCore.Serialization.MessagePack.Converters;
using OpenRiaServices.Server;
using PolyType;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization
{
    internal sealed class MessagePackSerializationProvider : ISerializationProvider
    {
        private readonly ITypeShapeProvider _typeShapeProvider;
        private readonly MessagePackSerializer _serializer;
        private readonly ConcurrentDictionary<Type, MessagePackSerializer> _serializerByDomainServiceType = new();

        public MessagePackSerializationProvider(MessagePackSerializationOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            _typeShapeProvider = options.TypeShapeProvider;
            _serializer = MessagePackUtility.ConfigureSerializer(options.Serializer, [new MethodParametersConverter()]);
        }

        public RequestSerializer GetRequestSerializer(DomainOperationEntry operation)
            => new MessagePackRequestSerializer(operation, GetSerializerForDomainService(operation.DomainServiceType), _typeShapeProvider);

        private MessagePackSerializer GetSerializerForDomainService(Type domainServiceType)
            => _serializerByDomainServiceType.GetOrAdd(domainServiceType, CreateSerializerForDomainService);

        private MessagePackSerializer CreateSerializerForDomainService(Type domainServiceType)
        {
            DomainServiceDescription description = DomainServiceDescription.GetDescription(domainServiceType);
            ValidateSurrogateAttributes(new HashSet<Type>(description.EntityTypes.Concat(description.ComplexTypes)));
            Wcf.DomainServiceSerializationSurrogate surrogateProvider = new Wcf.DomainServiceSerializationSurrogate(description);

            // Create converters to handle surrogates,
            Dictionary<Type, DerivedTypeUnion> derivedTypeUnions = _serializer.DerivedTypeUnions.ToDictionary(du => du.BaseType);
            List<MessagePackConverter> converters = new();
            foreach (var entityType in description.EntityTypes.Concat(description.ComplexTypes))
            {
                if (surrogateProvider.GetSurrogateType(entityType) is Type surrogateType)
                {
                    converters.Add((MessagePackConverter)
                        Activator.CreateInstance(typeof(SurrogateConverter<,>).MakeGenericType(surrogateType, entityType), args: [description, surrogateProvider])!);

                    // Prevent Nerdbank.MessagePack from adding an additional discriminator.
                    derivedTypeUnions[entityType] = DerivedTypeUnion.CreateDisabled(entityType);
                }
            }

            // All surrogates implement IClonable, use that as common base class for surrogates
            converters.Add(new SurrogateConverter<ICloneable, object>(description, surrogateProvider));
            converters.Add(new ChangeSetEntryConverter());

            return _serializer with
            {
                Converters = [.. _serializer.Converters, .. converters],
                DerivedTypeUnions = [.. derivedTypeUnions.Values],
            };
        }

        /// <summary>Validates attributes that cannot be used with MessagePack serialization surrogates.</summary>
        internal static void ValidateSurrogateAttributes(HashSet<Type> exposedTypes)
        {
            foreach (Type type in exposedTypes)
            {
                AttributeCollection attributes = TypeDescriptor.GetAttributes(type);
                if (attributes[typeof(MessagePackConverterAttribute)] != null)
                {
                    throw new InvalidOperationException($"MessagePackConverterAttribute on exposed type '{type.FullName}' is not supported with serialization surrogates.");
                }

                if (attributes[typeof(TypeShapeAttribute)] is TypeShapeAttribute { Marshaler: not null })
                {
                    throw new InvalidOperationException($"TypeShapeAttribute.Marshaler on exposed type '{type.FullName}' is not supported with serialization surrogates.");
                }

                foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(type))
                {
                    if (SerializationUtility.IsSerializableDataMember(property)
                        && exposedTypes.Contains(property.PropertyType)
                        && property.Attributes[typeof(MessagePackConverterAttribute)] != null)
                    {
                        throw new InvalidOperationException($"MessagePackConverterAttribute on property '{type.FullName}.{property.Name}' is not supported because its type '{property.PropertyType.FullName}' uses a serialization surrogate.");
                    }
                }
            }
        }
    }
}
