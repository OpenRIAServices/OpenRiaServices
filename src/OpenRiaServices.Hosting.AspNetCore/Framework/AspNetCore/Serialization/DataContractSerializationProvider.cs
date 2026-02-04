using OpenRiaServices.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;


namespace OpenRiaServices.Hosting.AspNetCore.Serialization
{
    internal sealed class BinaryXmlSerializationProvider : DataContractSerializationProvider
    {
        /// <summary>
        /// Creates a Binary-XML DataContract based serializer for the specified domain operation.
        /// </summary>
        /// <param name="operation">The domain operation description for which to create the serializer.</param>
        /// <param name="dataContractCache">The data contract cache containing type metadata used by the serializer.</param>
        /// <returns>A <see cref="BinaryXmlDataContractRequestSerializer"/> configured for the given operation and data contract cache.</returns>
        protected override DataContractRequestSerializer CreateOperationRequestSerialiser(DomainOperationEntry operation, DataContractCache dataContractCache)
            => new BinaryXmlDataContractRequestSerializer(operation, dataContractCache);
    }

    internal sealed class TextXmlSerializationProvider : DataContractSerializationProvider
    {
        /// <summary>
        /// Creates a (Text) XML DataContract based serializer for the specified domain operation.
        /// </summary>
        /// <param name="operation">The domain operation entry the serializer will handle.</param>
        /// <param name="dataContractCache">The data contract metadata cache for the operation's domain service type.</param>
        /// <returns>A <see cref="DataContractRequestSerializer"/> that serializes request payloads using text XML.</returns>
        protected override DataContractRequestSerializer CreateOperationRequestSerialiser(DomainOperationEntry operation, DataContractCache dataContractCache)
            => new TextXmlDataContractRequestSerializer(operation, dataContractCache);
    }

    internal abstract class DataContractSerializationProvider() : ISerializationProvider
    {
        private readonly ConcurrentDictionary<(Type, string), DataContractRequestSerializer> _serializers = new();
        internal ConcurrentDictionary<Type, DataContractCache> _perDomainServiceDataContractCache = new();

        /// <summary>
        /// Retrieve or create a cached RequestSerializer for the specified domain operation.
        /// </summary>
        /// <param name="operation">The DomainOperationEntry that identifies the domain service type and operation name to get a request serializer for.</param>
        /// <returns>The RequestSerializer for the specified operation; created and stored in the provider's cache if not already present.</returns>
        public RequestSerializer GetRequestSerializer(DomainOperationEntry operation)
        {
            var key = (operation.DomainServiceType, operation.Name);

            if (_serializers.TryGetValue(key, out var serializer))
                return serializer;

            var cache = _perDomainServiceDataContractCache.GetOrAdd(operation.DomainServiceType, static (type) =>
                new DataContractCache(DomainServiceDescription.GetDescription(type)));

            serializer = CreateOperationRequestSerialiser(operation, cache);
            return _serializers.GetOrAdd(key, serializer);
        }

        /// <summary>
        /// Override in derived classes to create a DataContractRequestSerializer for the specified domain operation using the provided data contract cache.
        /// </summary>
        /// <param name="operation">The domain operation entry that identifies the domain service operation and its metadata used to configure the serializer.</param>
        /// <param name="dataContractCache">The cached DataContractCache for the domain service type containing data contract metadata required to construct the serializer.</param>
        /// <returns>A DataContractRequestSerializer configured for the given operation and data contract cache.</returns>
        protected abstract DataContractRequestSerializer CreateOperationRequestSerialiser(DomainOperationEntry operation, DataContractCache dataContractCache);
    }
}
