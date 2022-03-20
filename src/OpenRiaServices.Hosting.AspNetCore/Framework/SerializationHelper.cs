using OpenRiaServices.Hosting.Wcf;
using OpenRiaServices.Server;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        /// 
        /// <returns>A <see cref="DataContractSerializer"/> which can be used to serialize the type</returns>
        internal DataContractSerializer GetSerializer(Type type)
        {
            lock (_serializerCache)
            {
                if (!_serializerCache.TryGetValue(type, out var serializer))
                {
                    if (type.IsPrimitive)
                    {
                        serializer = new DataContractSerializer(type);
                    }
                    else if (type != typeof(IEnumerable<ChangeSetEntry>))
                    {
                       // optionally we might consider only passing in EntityTypes as knowntypes for queries
                        serializer = new DataContractSerializer(type, _surrogateProvider.SurrogateTypes);
                        serializer.SetSerializationSurrogateProvider(_surrogateProvider);
                    }
                    else
                    {
                        // Submit need to be able to serialize all types that are part of entity actions as well
                        // since the parameters are passed in object arrays
                        var knownTypes = GetKnownTypesFromCustomMethods(this._domainServiceDescription);
                        knownTypes.UnionWith(this._surrogateProvider.SurrogateTypes);

                        serializer = new DataContractSerializer(typeof(List<ChangeSetEntry>), knownTypes);
                        serializer.SetSerializationSurrogateProvider(_surrogateProvider);
                    }
                    _serializerCache.Add(type, serializer);
                }
                return serializer;
            }
        }

        private HashSet<Type> GetKnownTypesFromCustomMethods(DomainServiceDescription domainServiceDescription)
        {
            var knownTypes = new HashSet<Type>();

            // Register types used in custom methods. Custom methods show up as part of the changeset.
            // KnownTypes are required for all non-primitive and non-string since these types will show up in the change set.
            foreach (DomainOperationEntry customOp in domainServiceDescription.DomainOperationEntries.Where(op => op.Operation == DomainOperation.Custom))
            {
                // KnownTypes will be added during surrogate registration for all entity and
                // complex types. We skip the first parameter because it is an entity type. We also
                // skip all complex types. Note, we do not skip complex type collections because
                // the act of registering surrogates only adds the type, and KnownTypes needs to
                // know about any collections.
                foreach (Type parameterType in customOp.Parameters.Skip(1).Select(p => p.ParameterType).Where(
                    t => !t.IsPrimitive && t != typeof(string) && !domainServiceDescription.ComplexTypes.Contains(t)))
                {
                    knownTypes.Add(parameterType);
                }
            }

            return knownTypes;
        }
    }
}
