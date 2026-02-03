using System;
using System.Diagnostics;
using System.Net.Http;
using OpenRiaServices.Client.DomainClients.Http;

namespace OpenRiaServices.Client.DomainClients
{
    /// <summary>
    ///  A <see cref="DomainClientFactory"/> which uses <see cref="System.Runtime.Serialization.DataContractSerializer"/> to 
    ///  communicates with the server by sending plain (text based) Xml over <see cref="HttpClient"/> using <c>application/xml</c> media type.
    /// </summary>
    public class XmlHttpDomainClientFactory : HttpDomainClientFactory
    {
        /// <inheritdoc />
        public XmlHttpDomainClientFactory(Uri serverBaseUri, HttpMessageHandler messageHandler) : base(serverBaseUri, messageHandler)
        {
        }

        /// <inheritdoc />
        public XmlHttpDomainClientFactory(Uri serverBaseUri, Func<Uri, HttpClient> httpClientFactory) : base(serverBaseUri, httpClientFactory)
        {
        }

        /// <inheritdoc />
        protected override DomainClient CreateDomainClientCore(Type serviceContract, Uri serviceUri, bool requiresSecureEndpoint)
        {
            HttpClient httpClient = CreateHttpClient(serviceUri, XmlHttpDomainClient.MediaType);

            return new XmlHttpDomainClient(httpClient, serviceContract);
        }
    }
}
