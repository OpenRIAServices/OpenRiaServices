using System;
using System.Diagnostics;
using System.Net.Http;
using OpenRiaServices.Client.DomainClients.Http;

namespace OpenRiaServices.Client.DomainClients
{
    /// <summary>
    ///  A new <see cref="HttpClient"/> based approach for connecting to services using the default REST with binary encoding protocol.
    ///  It is easier to extend than the original WCF based WebDomainClient
    /// </summary>
    public class BinaryHttpDomainClientFactory
        : DomainClientFactory
    {
        private readonly Func<Uri, HttpClient> _httpClientFactory;

        /// <summary>
        /// Create a <see cref="BinaryHttpDomainClientFactory"/> where all requests share a single <see cref="HttpMessageHandler"/>
        /// <para>IMPORTANT: To handle DNS updates you need to configure <c>System.Net.ServicePointManager.ConnectionLeaseTimeout</c> on .Net framework</para>
        /// </summary>
        /// <param name="serverBaseUri">The value base all service Uris on (see <see cref="DomainClientFactory.ServerBaseUri"/>)</param>
        /// <param name="messageHandler"><see cref="HttpMessageHandler"/> to be shared by all requests,
        /// if uncertain create a <see cref="HttpClientHandler"/> and enable cookies and compression</param>
        public BinaryHttpDomainClientFactory(Uri serverBaseUri, HttpMessageHandler messageHandler)
            : this(serverBaseUri, () => new HttpClient(messageHandler, disposeHandler: false))
        {
        }

        /// <summary>
        /// Constructor intended for .Net Core where the actual creation is handled by <c>IHttpClientFactory</c> or similar
        /// </summary>
        /// <param name="serverBaseUri">The value base all service Uris on (see <see cref="DomainClientFactory.ServerBaseUri"/>)</param>
        /// <param name="httpClientFactory">method creating a new HttpClient each time, should never return null</param>
        public BinaryHttpDomainClientFactory(Uri serverBaseUri, Func<HttpClient> httpClientFactory)
        {
            base.ServerBaseUri = serverBaseUri;
            if (httpClientFactory is null)
                throw new ArgumentNullException(nameof(httpClientFactory));

            this._httpClientFactory = (Uri uri) =>
            {
                HttpClient httpClient = httpClientFactory();
                httpClient.BaseAddress = uri;
                return httpClient;
            };
        }

        /// <summary>
        /// Constructor intended for .Net Core where the actual creation is handled by <c>IHttpClientFactory</c> or similar
        /// </summary>
        /// <param name="serverBaseUri">The value base all service Uris on (see <see cref="DomainClientFactory.ServerBaseUri"/>)</param>
        /// <param name="httpClientFactory">method creating a new HttpClient each time, should never return null</param>
        public BinaryHttpDomainClientFactory(Uri serverBaseUri, Func<Uri, HttpClient> httpClientFactory)
        {
            base.ServerBaseUri = serverBaseUri;
            this._httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        /// <inheritdoc />
        protected override DomainClient CreateDomainClientCore(Type serviceContract, Uri serviceUri, bool requiresSecureEndpoint)
        {
            HttpClient httpClient = CreateHttpClient(serviceUri);

            return new BinaryHttpDomainClient(httpClient, serviceContract);
        }

        // We do not make this virtual and protected at the momement since to do that the some thought must be put into
        // what parameters to support, it might make sens to do changes per DomainService/DomainContext
        private HttpClient CreateHttpClient(Uri serviceUri)
        {
            // Add /binary only for WCF style Uris
            if (serviceUri.AbsolutePath.EndsWith(".svc", StringComparison.Ordinal))
            {
               serviceUri = new Uri(serviceUri.AbsoluteUri + "/binary/");
            }
            

            var httpClient = _httpClientFactory(serviceUri);
            httpClient.BaseAddress ??= serviceUri;

            // Ensure Uri always end with "/" so that we can call Get and Post with just the method name
            if (!httpClient.BaseAddress.AbsoluteUri.EndsWith("/", StringComparison.Ordinal))
            {
                httpClient.BaseAddress = new Uri(httpClient.BaseAddress.AbsoluteUri + '/');
            }

            return httpClient;
        }
    }
}
