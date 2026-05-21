using System;
using System.Net.Http;
using System.Xml;
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
        /// <summary>
        /// Gets or sets the quotas used by <see cref="XmlDictionaryReader"/> when reading responses.
        /// </summary>
        public XmlDictionaryReaderQuotas ReaderQuotas { get; set; } = CreateMaxQuotas();

        /// <summary>
        /// Gets or sets the shared dictionary used for binary XML reader/writer compression.
        /// </summary>
        public IXmlDictionary? Dictionary { get; set; }

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

            return new BinaryHttpDomainClient(httpClient, serviceContract, ReaderQuotas, Dictionary);
        }

        private static XmlDictionaryReaderQuotas CreateMaxQuotas()
        {
            var quotas = new XmlDictionaryReaderQuotas();
            XmlDictionaryReaderQuotas.Max.CopyTo(quotas);
            return quotas;
        }
    }
}
