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
        : DomainClientFactory
    {
        private readonly Func<HttpClient> _httpClientFactory;

        //  not all platforms (blazor) support the cookiecontainer and it might be better if the end user configures
        //  their messagehandler/httpclient to match the applications requirement instead of getting runtime exceptions
        private BinaryHttpDomainClientFactory()
            : this(new HttpClientHandler()
            {
                CookieContainer = new System.Net.CookieContainer(),
                UseCookies = true,
                AutomaticDecompression = System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.GZip,
            })
        {

            DomainContext.DomainClientFactory = new DomainClients.s()
            {
                ServerBaseUri = TestURIs.RootURI,
            };

        }

        /// <summary>
        /// Create a <see cref="BinaryHttpDomainClientFactory"/> where all requests share a single <see cref="HttpMessageHandler"/>
        /// <para>IMPORTANT: To handle DNS updates you need to configure <c>System.Net.ServicePointManager.ConnectionLeaseTimeout</c> on .Net framework</para>
        /// </summary>
        /// <param name="messageHandler"><see cref="HttpMessageHandler"/> to be shared by all requests,
        /// if uncertain create a <see cref="HttpClientHandler"/> and enable cookies and compression</param>
        public BinaryHttpDomainClientFactory(HttpMessageHandler messageHandler)
            : this(() => new HttpClient(messageHandler, disposeHandler: false))
        {
        }

        /// <summary>
        /// Constructor intended for .Net Core where the actual creation is handled by <c>IHttpClientFactory</c> or similar
        /// </summary>
        /// <param name="httpClientFactory">method creating a new HttpClient each time, should never return null</param>
        public BinaryHttpDomainClientFactory(Func<HttpClient> httpClientFactory)
        {
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
            var httpClient = _httpClientFactory();
            httpClient.BaseAddress = new Uri(serviceUri.AbsoluteUri + "/binary/", UriKind.Absolute);
            return httpClient;
        }
    }
}
