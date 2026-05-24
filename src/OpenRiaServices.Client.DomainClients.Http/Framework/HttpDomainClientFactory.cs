using System;
using System.Net.Http;

namespace OpenRiaServices.Client.DomainClients
{
    /// <summary>
    /// Base class for <see cref="DomainClientFactory"/> implementations that use <see cref="HttpClient" /> for transport.
    /// </summary>
    public abstract class HttpDomainClientFactory
        : DomainClientFactory
    {
        private readonly Func<Uri, HttpClient> _httpClientFactory;

        /// <summary>
        /// Gets or sets the maximum query string length (in characters) before a GET request is automatically
        /// converted to a POST (or QUERY if <see cref="UseQueryHttpMethod"/> is enabled) request.
        /// </summary>
        /// <remarks>
        /// The default value is 2048, matching the default IIS query string length limit.
        /// Increase this value if your server is configured to allow longer query strings.
        /// </remarks>
        public int MaxUriLength { get; set; } = 2048;

        /// <summary>
        /// Gets or sets a value indicating whether the QUERY HTTP method should be used instead of POST
        /// when the request URI or query string becomes too long (exceeds <see cref="MaxUriLength"/>).
        /// </summary>
        /// <remarks>
        /// The QUERY HTTP method is a safe, idempotent method that allows sending a request body,
        /// making it suitable for read operations with complex query parameters that exceed URI length limits.
        /// When enabled, the server must also support the QUERY method (requires compatible server hosting configuration).
        /// Defaults to <see langword="false" />.
        /// </remarks>
        public bool UseQueryHttpMethod { get; set; }

        /// <summary>
        /// Create a <see cref="HttpDomainClientFactory"/> where all requests share a single <see cref="HttpMessageHandler"/>
        /// <para>IMPORTANT: To handle DNS updates you need to configure <c>System.Net.ServicePointManager.ConnectionLeaseTimeout</c> on .Net framework</para>
        /// </summary>
        /// <param name="serverBaseUri">The value base all service Uris on (see <see cref="DomainClientFactory.ServerBaseUri"/>)</param>
        /// <param name="messageHandler"><see cref="HttpMessageHandler"/> to be shared by all requests,
        /// if uncertain create a <see cref="HttpClientHandler"/> and enable cookies and compression</param>
        private protected HttpDomainClientFactory(Uri serverBaseUri, HttpMessageHandler messageHandler)
        : this(serverBaseUri, () => new HttpClient(messageHandler, disposeHandler: false))
        {
        }

        /// <summary>
        /// Constructor intended for .Net Core where the actual creation is handled by <c>IHttpClientFactory</c> or similar
        /// </summary>
        /// <param name="serverBaseUri">The value base all service Uris on (see <see cref="DomainClientFactory.ServerBaseUri"/>)</param>
        /// <param name="httpClientFactory">method creating a new HttpClient each time, should never return <see langword="null" /></param>
        private protected HttpDomainClientFactory(Uri serverBaseUri, Func<HttpClient> httpClientFactory)
        {
            base.ServerBaseUri = serverBaseUri;
            ArgumentNullException.ThrowIfNull(httpClientFactory);

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
        /// <param name="httpClientFactory">method creating a new HttpClient given the <see cref="Uri"/> of the server endpoint, should never return <see langword="null" /></param>
        private protected HttpDomainClientFactory(Uri serverBaseUri, Func<Uri, HttpClient> httpClientFactory)
        {
            base.ServerBaseUri = serverBaseUri;
            this._httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        // We do not make this virtual and protected at the momement since to do that the some thought must be put into
        // what parameters to support, it might make sens to do changes per DomainService/DomainContext
        private protected HttpClient CreateHttpClient(Uri serviceUri, string contentType)
        {
            // Add /binary only for WCF style Uris
            if (serviceUri.AbsolutePath.EndsWith(".svc", StringComparison.Ordinal))
            {
                serviceUri = new Uri(serviceUri.AbsoluteUri + "/binary/");
            }

            var httpClient = _httpClientFactory(serviceUri);
            httpClient.BaseAddress ??= serviceUri;
            if (httpClient.DefaultRequestHeaders.Accept.Count == 0)
                httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(contentType));

            // Ensure Uri always end with "/" so that we can call Get and Post with just the method name
            if (!httpClient.BaseAddress.AbsoluteUri.EndsWith("/", StringComparison.Ordinal))
            {
                httpClient.BaseAddress = new Uri(httpClient.BaseAddress.AbsoluteUri + '/');
            }

            return httpClient;
        }
    }
}
