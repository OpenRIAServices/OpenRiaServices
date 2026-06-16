using Nerdbank.MessagePack;
using OpenRiaServices.Hosting.AspNetCore.Serialization.MessagePack;
using OpenRiaServices.Hosting.AspNetCore.Serialization.MessagePack.Converters;
using OpenRiaServices.Server;
using PolyType;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            Wcf.DomainServiceSerializationSurrogate surrogateProvider = new Wcf.DomainServiceSerializationSurrogate(description);

            // Create converters to handle surrogates,
            List<MessagePackConverter> converters = new();
            foreach (var entityType in description.EntityTypes.Concat(description.ComplexTypes))
            {
                if (surrogateProvider.GetSurrogateType(entityType) is Type surrogateType)
                {
                    converters.Add((MessagePackConverter)
                        Activator.CreateInstance(typeof(SurrogateConverter<,>).MakeGenericType(surrogateType, entityType), args: [description, surrogateProvider])!);
                }
            }

            // All surrogates implement IClonable, use that as common base class for surrogates
            converters.Add(new SurrogateConverter<ICloneable, object>(description, surrogateProvider));
            converters.Add(new ChangeSetEntryConverter());

            return _serializer with
            {
                Converters = [.. _serializer.Converters, .. converters]
            };
        }
    }
}
