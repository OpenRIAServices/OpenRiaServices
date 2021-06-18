using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace OpenRiaServices.Client.HttpDomainClient
{
    public class BinaryHttpDomainClientFactory
        : DomainClientFactory
    {
        private Func<HttpClient> httpClientFactory;

        public BinaryHttpDomainClientFactory()
            : this(new HttpClientHandler()
            {
                CookieContainer = new System.Net.CookieContainer(),
                UseCookies = true,
                AutomaticDecompression = System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.GZip,
            })
        {

        }

        public BinaryHttpDomainClientFactory(HttpMessageHandler messageHandler)
            : this(() => new HttpClient(messageHandler, disposeHandler: false))
        {
        }

        public BinaryHttpDomainClientFactory(Func<HttpClient> httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        protected override DomainClient CreateDomainClientCore(Type serviceContract, Uri serviceUri, bool requiresSecureEndpoint)
        {
            var httpClient = httpClientFactory();
            httpClient.BaseAddress = new Uri(serviceUri.AbsoluteUri + "/binary/", UriKind.Absolute);
        //    httpClient.DefaultRequestHeaders.Add("Content-Type", "application/msbin1");

            return new BinaryHttpDomainClient(httpClient, serviceContract);
        }

        public HttpMessageHandler HttpMessageHandler
        {
            set
            {
                this.httpClientFactory = () => new HttpClient(value, disposeHandler: false);
            }
        }
    }
}
