using OpenRiaServices.Hosting.Wcf;
using OpenRiaServices.Server;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;

namespace OpenRiaServices.Hosting.AspNetCore
{
    internal class SerializationHelper
    {       
        private readonly Dictionary<Type, DataContractSerializer> _serializerCache = new Dictionary<Type, DataContractSerializer>();
        private readonly DomainServiceDescription _domainServiceDescription;
        private readonly DomainServiceSerializationSurrogate _surrogateProvider;

        public SerializationHelper(DomainServiceDescription domainServiceDescription)
        {
            _domainServiceDescription = domainServiceDescription;
            _surrogateProvider = new DomainServiceSerializationSurrogate(domainServiceDescription);
        }

        /// <summary>
        /// Gets a <see cref="DataContractSerializer"/> which can be used to serialized the specified type.
        /// The serializers are cached for performance reasons.
        /// </summary>
        /// <param name="type">type which should be serializable.</param>
        /// <param name="entityTypes">the collection of Entity Types that the method will operate on.</param>
        /// <returns>A <see cref="DataContractSerializer"/> which can be used to serialize the type</returns>
        internal DataContractSerializer GetSerializer(Type type, IEnumerable<Type> entityTypes)
        {
            lock (_serializerCache)
            {
                if (!_serializerCache.TryGetValue(type, out var serializer))
                {
                    if (type != typeof(IEnumerable<ChangeSetEntry>))
                    {
                       // optionally we might consider only passing in EntityTypes as knowntypes for queries
                        serializer = new DataContractSerializer(type, _surrogateProvider.SurrogateTypes);
                        serializer.SetSerializationSurrogateProvider(_surrogateProvider);
                    }
                    else
                    {
                        // Submit need to be able to serialize all types that are part of entity actions as well
                        // since the parameters are passed in object arrays
                        serializer = new DataContractSerializer(typeof(List<ChangeSetEntry>), _surrogateProvider.SurrogateTypes);
                        serializer.SetSerializationSurrogateProvider(_surrogateProvider);
                    }
                    _serializerCache.Add(type, serializer);
                }
                return serializer;
            }
        }
    }
}
