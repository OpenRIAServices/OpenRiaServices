using OpenRiaServices.Client.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace OpenRiaServices.Client.DomainClients.Http
{
    internal class BinaryHttpDomainClientSerializationHelper
    {
        private static readonly Dictionary<Type, Dictionary<Type, DataContractSerializer>> s_globalSerializerCache = new Dictionary<Type, Dictionary<Type, DataContractSerializer>>();
        private static readonly Dictionary<Type, Dictionary<string, MethodParameters>> s_globalMethodParametersCache = new Dictionary<Type, Dictionary<string, MethodParameters>>();
        private readonly Dictionary<Type, DataContractSerializer> _serializerCache;
        private readonly Dictionary<string, MethodParameters> _methodParametersCache;
        private readonly Type _serviceInterface;

        public BinaryHttpDomainClientSerializationHelper(Type serviceInterface)
        {
            _serviceInterface = serviceInterface;

            lock (s_globalSerializerCache)
            {
                if (!s_globalSerializerCache.TryGetValue(serviceInterface, out _serializerCache))
                {
                    _serializerCache = new Dictionary<Type, DataContractSerializer>();
                    s_globalSerializerCache.Add(serviceInterface, _serializerCache);
                }
            }

            lock (s_globalMethodParametersCache)
            {
                if (!s_globalMethodParametersCache.TryGetValue(serviceInterface, out _methodParametersCache))
                {
                    _methodParametersCache = new Dictionary<string, MethodParameters>();
                    s_globalMethodParametersCache.Add(_serviceInterface, _methodParametersCache);
                }
            }
        }

        /// <summary>
        /// Get parameter names and types for method
        /// </summary>
        /// <param name="methodName">The name of the method</param>
        /// <returns>MethodParameters object containing the method parameters</returns>
        internal MethodParameters GetParametersForMethod(string methodName)
        {
            MethodParameters methodParameters;
            var serializedMethodName = $"Begin{methodName}";
            
            lock (_methodParametersCache)
            {
                if (!_methodParametersCache.TryGetValue(serializedMethodName, out methodParameters))
                {
                    methodParameters = new MethodParameters(methodName, _serviceInterface.GetMethod(serializedMethodName).GetParameters());
                    _methodParametersCache.Add(serializedMethodName, methodParameters);
                }
            }

            return methodParameters;
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
            DataContractSerializer serializer;
            lock (_serializerCache)
            {
                if (!_serializerCache.TryGetValue(type, out serializer))
                {
                    if (type != typeof(IEnumerable<ChangeSetEntry>))
                    {
                        // optionally we might consider only passing in EntityTypes as knowntypes for queries
                        serializer = new DataContractSerializer(type, entityTypes);
                    }
                    else
                    {
                        // Submit need to be able to serialize all types that are part of entity actions as well
                        // since the parameters are passed in object arrays
                        serializer = new DataContractSerializer(typeof(List<ChangeSetEntry>), GetSubmitDataContractSettings(entityTypes));
                    }
                    _serializerCache.Add(type, serializer);
                }
            }

            return serializer;
        }

        /// <summary>
        /// Submit need to be able to serialize all types that are part of entity actions as well
        /// since the parameters are passed in object arrays.
        /// 
        /// Find all types which are part of parameters and add them
        /// </summary>
        /// <returns></returns>
        private DataContractSerializerSettings GetSubmitDataContractSettings(IEnumerable<Type> entityTypes)
        {
            var visitedTypes = new HashSet<Type>(entityTypes);
            var knownTypes = new HashSet<Type>(visitedTypes);
            var toVisit = new Stack<Type>(knownTypes);

            while (toVisit.Count > 0)
            {
                var entityType = toVisit.Pop();

                // Check any derived types to
                foreach (KnownTypeAttribute derived in entityType.GetCustomAttributes(typeof(KnownTypeAttribute), inherit: false))
                {
                    if (visitedTypes.Add(derived.Type))
                        toVisit.Push(derived.Type);
                }

                // Ensure all parameter types are known
                var metaType = MetaType.GetMetaType(entityType);
                foreach (var entityAction in metaType.GetEntityActions())
                {
                    var method = entityType.GetMethod(entityAction.Name);
                    foreach (var parameter in method.GetParameters())
                    {
                        var type = TypeUtility.GetNonNullableType(parameter.ParameterType);
                        if (visitedTypes.Add(type))
                        {
                            // Most "primitive types" are already registered
                            if (TypeUtility.IsPredefinedSimpleType(type))
                            {
                                if (typeof(DateTimeOffset) == type || type.IsEnum)
                                    knownTypes.Add(type);
                            }
                            else if (SubmitDataContractResolver.TryGetEquivalentContractType(type, out var collectionType))
                            {
                                knownTypes.Add(collectionType);
                                // Add elementType too ??
                            }
                            else
                            {
                                knownTypes.Add(type);
                            }
                        }
                    }
                }
            }

            var resolver = new SubmitDataContractResolver();
            return new DataContractSerializerSettings()
            {
                KnownTypes = knownTypes,
                DataContractResolver = resolver,
            };
        }
    }
}
