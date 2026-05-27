using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Xml;

namespace OpenRiaServices.Client.DomainClients.Http
{
    // Pass in HttpDomainClientFactory to ctor,

    /// <summary>
    /// Base class for <see cref="DomainClient"/>s using <see cref="DataContractSerializer"/> serialization and talking to the server using <see cref="System.Net.Http.HttpClient"/>.
    /// </summary>
    abstract class DataContractHttpDomainClient : HttpDomainClient
    {
        private static readonly Dictionary<Type, DataContractSerializationHelper> s_globalCacheHelpers = new Dictionary<Type, DataContractSerializationHelper>();

        private readonly DataContractSerializationHelper _localCacheHelper;

        private protected DataContractHttpDomainClient(HttpClient httpClient, Type serviceInterface)
            : base(httpClient)
        {
            ArgumentNullException.ThrowIfNull(serviceInterface);

            lock (s_globalCacheHelpers)
            {
                if (!s_globalCacheHelpers.TryGetValue(serviceInterface, out _localCacheHelper))
                {
                    _localCacheHelper = new DataContractSerializationHelper(serviceInterface);
                    s_globalCacheHelpers.Add(serviceInterface, _localCacheHelper);
                }
            }
        }

        private protected abstract override string ContentType { get; }
        private protected abstract override XmlDictionaryWriter CreateWriter(Stream stream);
        private protected abstract override XmlDictionaryReader CreateReader(Stream stream);

        #region Serialization helpers

        /// <summary>
        /// Get parameter names and types for method
        /// </summary>
        /// <param name="methodName">The name of the method</param>
        /// <returns>MethodParameters object containing the method parameters</returns>
        private protected override MethodParameters GetMethodParameters(string methodName)
             => _localCacheHelper.GetParametersForMethod(methodName);

        /// <summary>
        /// Gets a <see cref="DataContractSerializer"/> which can be used to serialized the specified type.
        /// The serializers are cached for performance reasons.
        /// </summary>
        /// <param name="type">type which should be serializable.</param>
        /// <returns>A <see cref="DataContractSerializer"/> which can be used to serialize the type</returns>
        private protected override DataContractSerializer GetSerializer(Type type)
             => _localCacheHelper.GetSerializer(type, EntityTypes);

        #endregion
    }
}
