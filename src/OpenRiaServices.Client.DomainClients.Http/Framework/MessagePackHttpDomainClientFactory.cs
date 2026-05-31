using OpenRiaServices.Client.DomainClients.Http;
using System;
using System.Net.Http;

namespace OpenRiaServices.Client.DomainClients
{
    /// <summary>
    /// A <see cref="DomainClientFactory"/> that communicates with the server using MessagePack over HTTP.
    /// </summary>
    public class MessagePackHttpDomainClientFactory : HttpDomainClientFactory
    {
        /// <inheritdoc />
        public MessagePackHttpDomainClientFactory(Uri serverBaseUri, HttpMessageHandler messageHandler)
            : base(serverBaseUri, messageHandler)
        {
        }

        /// <inheritdoc />
        public MessagePackHttpDomainClientFactory(Uri serverBaseUri, Func<Uri, HttpClient> httpClientFactory)
            : base(serverBaseUri, httpClientFactory)
        {
        }

        /// <inheritdoc />
        protected override DomainClient CreateDomainClientCore(Type serviceContract, Uri serviceUri, bool requiresSecureEndpoint)
        {
            HttpClient httpClient = CreateHttpClient(serviceUri, MessagePackHttpDomainClient.MediaType);
            return new MessagePackHttpDomainClient(httpClient, serviceContract, this);
        }
    }
}
