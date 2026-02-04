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
        /// Create a <see cref="HttpDomainClientFactory"/> where all requests share a single <see cref="HttpMessageHandler"/>
        /// <para>IMPORTANT: To handle DNS updates you need to configure <c>System.Net.ServicePointManager.ConnectionLeaseTimeout</c> on .Net framework</para>
        /// </summary>
        /// <param name="serverBaseUri">The value base all service Uris on (see <see cref="DomainClientFactory.ServerBaseUri"/>)</param>
        /// <param name="messageHandler"><see cref="HttpMessageHandler"/> to be shared by all requests,
        /// <summary>
        /// Creates a HttpDomainClientFactory that uses the provided <see cref="HttpMessageHandler"/> to construct HttpClient instances.
        /// </summary>
        /// <param name="serverBaseUri">The base URI used as the default <see cref="HttpClient.BaseAddress"/> for clients created by this factory.</param>
        /// <param name="messageHandler">The shared message handler used when constructing <see cref="HttpClient"/> instances; the handler is reused and will not be disposed by the created HttpClient.</param>
        private protected HttpDomainClientFactory(Uri serverBaseUri, HttpMessageHandler messageHandler)
        : this(serverBaseUri, () => new HttpClient(messageHandler, disposeHandler: false))
        {
        }

        /// <summary>
        /// Constructor intended for .Net Core where the actual creation is handled by <c>IHttpClientFactory</c> or similar
        /// </summary>
        /// <param name="serverBaseUri">The value base all service Uris on (see <see cref="DomainClientFactory.ServerBaseUri"/>)</param>
        /// <summary>
        /// Initializes the factory with a function that produces configured <see cref="HttpClient"/> instances for a given server base URI.
        /// </summary>
        /// <param name="serverBaseUri">The base URI for the server; assigned to <see cref="DomainClientFactory.ServerBaseUri"/>.</param>
        /// <param name="httpClientFactory">A factory that creates a new <see cref="HttpClient"/> instance; the returned client must not be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="httpClientFactory"/> is <see langword="null"/>.</exception>
        private protected HttpDomainClientFactory(Uri serverBaseUri, Func<HttpClient> httpClientFactory)
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
        /// <summary>
        /// Initializes the factory with a base server URI and a factory that produces an HttpClient for a given server URI.
        /// </summary>
        /// <param name="serverBaseUri">The base URI of the server to assign to <see cref="DomainClientFactory.ServerBaseUri"/>.</param>
        /// <param name="httpClientFactory">A delegate that creates an <see cref="HttpClient"/> for the provided server <see cref="Uri"/>; must not return <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpClientFactory"/> is <see langword="null"/>.</exception>
        private protected HttpDomainClientFactory(Uri serverBaseUri, Func<Uri, HttpClient> httpClientFactory)
        {
            base.ServerBaseUri = serverBaseUri;
            this._httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        // We do not make this virtual and protected at the momement since to do that the some thought must be put into
        /// <summary>
        /// Create and configure an <see cref="HttpClient"/> for the given service URI and media type.
        /// </summary>
        /// <param name="serviceUri">The target service URI; if the path ends with ".svc" the URI is adjusted by appending "/binary/".</param>
        /// <param name="contentType">The media type to add to the client's Accept header when no Accept header is present.</param>
        /// <returns>An <see cref="HttpClient"/> whose <see cref="HttpClient.BaseAddress"/> is set to the (possibly adjusted) service URI and ensured to end with '/', and which has an Accept header containing <paramref name="contentType"/> if none were present.</returns>
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