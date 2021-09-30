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

            // Handle types with datacontract with Name or Namespace missing
            // This might happen for types in other referenced assemblies in some scenarios
            foreach (DataContractAttribute dataContract in type.GetCustomAttributes(typeof(DataContractAttribute), false))
            {
                var xmlDictionary = new XmlDictionary(2);

                // Name defaults to type name if not set, special care is needed for genereic types 
                // but as long as those use datacontract attributes to set Name it should work fine (the fallback here won't work for them)
                // https://docs.microsoft.com/en-us/dotnet/framework/wcf/feature-details/data-contract-names#data-contract-names-1
                typeName = xmlDictionary.Add(dataContract.Name ?? type.Name);

                // Namespace seems to be populated by default even if not set, we do however include a fallback
                // which works, except that it does not account for the ContractNamespaceAttribute set on the target 
                // https://docs.microsoft.com/en-us/dotnet/framework/wcf/feature-details/data-contract-names#data-contract-namespaces
                string @namespace = dataContract.Namespace;
                if (@namespace is null)
                    @namespace = "http://schemas.datacontract.org/2004/07/" + type.Namespace;

                typeNamespace = xmlDictionary.Add(@namespace);
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
                    result = elementType.MakeArrayType();
                }

                return true;
            }

            result = null;
            return false;
        }
    }
}
