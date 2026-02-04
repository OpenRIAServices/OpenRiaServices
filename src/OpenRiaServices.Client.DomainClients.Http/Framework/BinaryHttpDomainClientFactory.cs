using System;
using System.Net.Http;
using OpenRiaServices.Client.DomainClients.Http;

namespace OpenRiaServices.Client.DomainClients
{
    /// <summary>
    ///  A new <see cref="HttpClient"/> based approach for connecting to services using the default REST with binary encoding protocol.
    ///  It is easier to extend than the original WCF based WebDomainClient
    /// </summary>
    public class BinaryHttpDomainClientFactory
        : HttpDomainClientFactory
    {
        /// <inheritdoc />
        public BinaryHttpDomainClientFactory(Uri serverBaseUri, HttpMessageHandler messageHandler)
            : base(serverBaseUri, messageHandler)
        {
        }

        /// <inheritdoc />
        public BinaryHttpDomainClientFactory(Uri serverBaseUri, Func<HttpClient> httpClientFactory)
            : base(serverBaseUri, httpClientFactory)
        {
        }

        /// <inheritdoc />
        public BinaryHttpDomainClientFactory(Uri serverBaseUri, Func<Uri, HttpClient> httpClientFactory)
            : base(serverBaseUri, httpClientFactory)
        {
        }

        /// <summary>
        /// Create a BinaryHttpDomainClient configured for the specified service contract and service URI.
        /// </summary>
        /// <param name="serviceContract">The service contract type implemented by the domain service.</param>
        /// <param name="serviceUri">The endpoint URI of the domain service.</param>
        /// <param name="requiresSecureEndpoint">Whether the created client must target a secure endpoint.</param>
        /// <returns>A DomainClient that communicates using the binary HTTP protocol for the specified service contract.</returns>
        protected override DomainClient CreateDomainClientCore(Type serviceContract, Uri serviceUri, bool requiresSecureEndpoint)
        {
            HttpClient httpClient = CreateHttpClient(serviceUri, BinaryHttpDomainClient.MediaType);

            return new BinaryHttpDomainClient(httpClient, serviceContract);
        }
    }
}