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

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlHttpDomainClientFactory"/> class using the specified server base URI and an HTTP client factory.
        /// </summary>
        /// <param name="serverBaseUri">The base URI of the server used to resolve service endpoints.</param>
        /// <param name="httpClientFactory">A function that creates an <see cref="HttpClient"/> for the provided service <see cref="Uri"/>.</param>
        public XmlHttpDomainClientFactory(Uri serverBaseUri, Func<Uri, HttpClient> httpClientFactory) : base(serverBaseUri, httpClientFactory)
        {
        }

        /// <summary>
        /// Creates a DomainClient that communicates using XML over HTTP for the specified service contract and service URI.
        /// </summary>
        /// <param name="serviceContract">The service contract type implemented by the remote service.</param>
        /// <param name="serviceUri">The endpoint URI of the service.</param>
        /// <param name="requiresSecureEndpoint">Whether the client should target a secure endpoint.</param>
        /// <returns>An XmlHttpDomainClient configured to communicate with the service using XML.</returns>
        protected override DomainClient CreateDomainClientCore(Type serviceContract, Uri serviceUri, bool requiresSecureEndpoint)
        {
            HttpClient httpClient = CreateHttpClient(serviceUri, XmlHttpDomainClient.MediaType);

            return new XmlHttpDomainClient(httpClient, serviceContract);
        }
    }
}