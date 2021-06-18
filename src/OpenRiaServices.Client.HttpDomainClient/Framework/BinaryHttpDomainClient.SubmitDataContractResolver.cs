using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace OpenRiaServices.Client.HttpDomainClient
{
    internal partial class BinaryHttpDomainClient
    {
        class SubmitDataContractResolver : DataContractResolver
        {
            private readonly HashSet<Type> _knownInterfaces = new HashSet<Type>();
            private readonly ConcurrentDictionary<Type, (System.Xml.XmlDictionaryString typeName, System.Xml.XmlDictionaryString typeNamespace)> _knownTypes
                = new ConcurrentDictionary<Type, (System.Xml.XmlDictionaryString, System.Xml.XmlDictionaryString)>();

            public void AddInterface(Type type)
                => _knownInterfaces.Add(type);

            public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
            {
                return knownTypeResolver.ResolveName(typeName, typeNamespace, declaredType, knownTypeResolver);
            }

            public bool TryGetEquivalentContractType(Type type, out Type result)
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

            public override bool TryResolveType(Type type, Type declaredType, DataContractResolver knownTypeResolver, out System.Xml.XmlDictionaryString typeName, out System.Xml.XmlDictionaryString typeNamespace)
            {
                if (knownTypeResolver.TryResolveType(type, declaredType, null, out typeName, out typeNamespace))
                    return true;

                if (_knownTypes.TryGetValue(type, out var match))
                {
                    typeName = match.typeName;
                    typeNamespace = match.typeNamespace;

                    return true;
                }

                // Collections are normally serialized as arrays (same xml naming for all)
                // - ve register all as list so 
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
        }
    }
}
