using System;
using System.Net.Http;

namespace OpenRiaServices.Client.DomainClients
{
    public abstract class HttpDomainClientFactory
        : DomainClientFactory
    {
        private readonly Func<Uri, HttpClient> _httpClientFactory;

        internal HttpDomainClientFactory(Uri serverBaseUri, HttpMessageHandler messageHandler)
            : this(serverBaseUri, () => new HttpClient(messageHandler, disposeHandler: false))
        {
        }

        internal HttpDomainClientFactory(Uri serverBaseUri, Func<HttpClient> httpClientFactory)
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
        internal HttpDomainClientFactory(Uri serverBaseUri, Func<Uri, HttpClient> httpClientFactory)
        {
            base.ServerBaseUri = serverBaseUri;
            this._httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        // We do not make this virtual and protected at the momement since to do that the some thought must be put into
        // what parameters to support, it might make sens to do changes per DomainService/DomainContext
        internal protected HttpClient CreateHttpClient(Uri serviceUri, string contentType)
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
