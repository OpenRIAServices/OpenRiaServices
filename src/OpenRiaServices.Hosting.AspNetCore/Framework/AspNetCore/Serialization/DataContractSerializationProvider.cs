using OpenRiaServices.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;


namespace OpenRiaServices.Hosting.AspNetCore.Serialization
{
    internal class BinaryXmlSerializationProvider : DataContractSerializationProvider
    {
        protected override DataContractRequestSerializer CreateOperationRequestSerialiser(DomainOperationEntry operation, DataContractCache dataContractCache)
            => new BinaryXmlDataContractRequestSerializer(operation, dataContractCache);
    }

    internal class TextXmlSerializationProvider : DataContractSerializationProvider
    {
        protected override DataContractRequestSerializer CreateOperationRequestSerialiser(DomainOperationEntry operation, DataContractCache dataContractCache)
            => new TextXmlDataContractRequestSerializer(operation, dataContractCache);
    }

    internal abstract class DataContractSerializationProvider() : ISerializationProvider
    {
        private readonly ConcurrentDictionary<(Type, string), DataContractRequestSerializer> _serializers = new();
        internal ConcurrentDictionary<Type, DataContractCache> _perDomainServiceSerializationHelper = new();

        public RequestSerializer GetRequestSerializer(DomainOperationEntry operation)
        {
            var key = (operation.DomainServiceType, operation.Name);

            if (_serializers.TryGetValue(key, out var serializer))
                return serializer;

            var helper = _perDomainServiceSerializationHelper.GetOrAdd(operation.DomainServiceType, static (type) =>
                new DataContractCache(DomainServiceDescription.GetDescription(type)));

            serializer = CreateOperationRequestSerialiser(operation, helper);
            return _serializers.GetOrAdd(key, serializer);
        }

        protected abstract DataContractRequestSerializer CreateOperationRequestSerialiser(DomainOperationEntry operation, DataContractCache serializationHelper);
    }
}
