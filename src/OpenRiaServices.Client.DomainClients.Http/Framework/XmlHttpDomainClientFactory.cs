using System;
using System.Net.Http;
using System.Xml;
using OpenRiaServices.Client.DomainClients.Http;

namespace OpenRiaServices.Client.DomainClients
{
    /// <summary>
    ///  A <see cref="DomainClientFactory"/> which uses <see cref="System.Runtime.Serialization.DataContractSerializer"/> to 
    ///  communicates with the server by sending plain (text based) Xml over <see cref="HttpClient"/> using <c>application/xml</c> media type.
    /// </summary>
    public class XmlHttpDomainClientFactory : HttpDomainClientFactory
    {
        /// <summary>
        /// Gets or sets the quotas used by <see cref="XmlDictionaryReader"/> when reading responses.
        /// </summary>
        public XmlDictionaryReaderQuotas ReaderQuotas { get; set; } = CreateMaxQuotas();

        /// <inheritdoc />
        public XmlHttpDomainClientFactory(Uri serverBaseUri, HttpMessageHandler messageHandler) : base(serverBaseUri, messageHandler)
        {
        }

        /// <inheritdoc />
        public XmlHttpDomainClientFactory(Uri serverBaseUri, Func<Uri, HttpClient> httpClientFactory) : base(serverBaseUri, httpClientFactory)
        {
        }

        /// <inheritdoc />
        protected override DomainClient CreateDomainClientCore(Type serviceContract, Uri serviceUri, bool requiresSecureEndpoint)
        {
            HttpClient httpClient = CreateHttpClient(serviceUri, XmlHttpDomainClient.MediaType);

            return new XmlHttpDomainClient(httpClient, serviceContract, this, ReaderQuotas);
        }

        private static XmlDictionaryReaderQuotas CreateMaxQuotas()
        {
            var quotas = new XmlDictionaryReaderQuotas();
            XmlDictionaryReaderQuotas.Max.CopyTo(quotas);
            return quotas;
        }
    }
}
