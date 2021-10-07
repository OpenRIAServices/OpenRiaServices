using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;

namespace OpenRiaServices.Client.DomainClients.Http
{
    /// <summary>
    /// When sending EntityAction invocations we send parameters as object[] so all types needs to be registered
    /// as known types. We cannot hovewer register more than one "array like type" (implementing IEnumerable{T}) 
    /// for each T so for these types we use <see cref="TryGetEquivalentContractType"/> to get the type to register
    /// and then we use the resolver to allow other collection types at runtime (with same xml name and namespace as the 
    /// collection type registered).
    /// </summary>
    class SubmitDataContractResolver : DataContractResolver
    {
        private readonly ConcurrentDictionary<Type, (XmlDictionaryString typeName, XmlDictionaryString typeNamespace)> _knownTypes
            = new ConcurrentDictionary<Type, (XmlDictionaryString, XmlDictionaryString)>();

        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
        {
            return knownTypeResolver.ResolveName(typeName, typeNamespace, declaredType, knownTypeResolver);
        }

        public override bool TryResolveType(Type type, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            if (knownTypeResolver.TryResolveType(type, declaredType, null, out typeName, out typeNamespace))
                return true;

            // Check cached types
            if (_knownTypes.TryGetValue(type, out var match))
            {
                typeName = match.typeName;
                typeNamespace = match.typeNamespace;

                return true;
            }

            // Collections are normally serialized as arrays (same xml naming for all)
            // - we register all as array, so for e.g List<T> we get the equivalent array T[] 
            // and then recursivly lookup the name for T[] instead
            // We do the recursive approach instead of setting the names ourselvs since the rules are not trivial
            // https://docs.microsoft.com/en-us/dotnet/framework/wcf/feature-details/collection-types-in-data-contracts#collection-naming
            if (TryGetEquivalentContractType(type, out var collectionType)
                && type != collectionType)
            {
                if (TryResolveType(collectionType, declaredType, knownTypeResolver, out typeName, out typeNamespace))
                    _knownTypes.TryAdd(type, (typeName, typeNamespace));
                return true;
            }

            typeName = null;
            typeNamespace = null;
            return true;
        }

        /// <summary>
        /// For collection types such as List{T} returns array of T (T[])
        /// For dictionary types returns Dictionary{K,V}
        /// </summary>
        public static bool TryGetEquivalentContractType(Type type, out Type result)
        {
            // Collections are normally serialized as arrays (same xml naming for all)
            // - ve register all as list so 
            var elementType = TypeUtility.GetElementType(type);
            if (elementType != type)
            {
                if (elementType.IsGenericType
                    && elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)
                    && typeof(IDictionary<,>)
                        .MakeGenericType(elementType.GetGenericArguments())
                        .IsAssignableFrom(type))
                {
                    result = typeof(Dictionary<,>).MakeGenericType(elementType.GetGenericArguments());
                }
                else // general array
                {
                    result = typeof(IEnumerable<>).MakeGenericType(elementType);
                }

                return true;
            }

            result = null;
            return false;
        }
    }
}
