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

        /// <inheritdoc />
        protected override DomainClient CreateDomainClientCore(Type serviceContract, Uri serviceUri, bool requiresSecureEndpoint)
        {
            HttpClient httpClient = CreateHttpClient(serviceUri, BinaryHttpDomainClient.MediaType);

            return new BinaryHttpDomainClient(httpClient, serviceContract);
        }
    }
}
