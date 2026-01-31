using System;
using System.Diagnostics;
using System.Net.Http;
using OpenRiaServices.Client.DomainClients.Http;

namespace OpenRiaServices.Client.DomainClients
{

    public class XmlHttpDomainClientFactory : HttpDomainClientFactory
    {
        /// <summary>
        /// Create a <see cref="XmlHttpDomainClientFactory"/> where all requests share a single <see cref="HttpMessageHandler"/>
        /// <para>IMPORTANT: To handle DNS updates you need to configure <c>System.Net.ServicePointManager.ConnectionLeaseTimeout</c> on .Net framework</para>
        /// </summary>
        /// <param name="serverBaseUri">The value base all service Uris on (see <see cref="DomainClientFactory.ServerBaseUri"/>)</param>
        /// <param name="messageHandler"><see cref="HttpMessageHandler"/> to be shared by all requests,
        /// if uncertain create a <see cref="HttpClientHandler"/> and enable cookies and compression</param>
        public XmlHttpDomainClientFactory(Uri serverBaseUri, HttpMessageHandler messageHandler) : base(serverBaseUri, messageHandler)
        {
        }

        /// <summary>
        /// Constructor intended for .Net Core where the actual creation is handled by <c>IHttpClientFactory</c> or similar
        /// </summary>
        /// <param name="serverBaseUri">The value base all service Uris on (see <see cref="DomainClientFactory.ServerBaseUri"/>)</param>
        /// <param name="httpClientFactory">method creating a new HttpClient each time, should never return null</param>
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
