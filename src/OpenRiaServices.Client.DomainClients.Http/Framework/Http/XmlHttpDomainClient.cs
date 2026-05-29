using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Xml;

namespace OpenRiaServices.Client.DomainClients.Http
{
    /// <summary>
    /// <see cref="DomainClient"/> implementation which uses plain utf8 encoded text Xml over HTTP using <see cref="DataContractSerializer"/> for serialisation
    /// </summary>
    sealed class XmlHttpDomainClient : DataContractHttpDomainClient
    {
        internal const string MediaType = "application/xml";

        private readonly System.Text.Encoding _encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        private readonly XmlDictionaryReaderQuotas _readerQuotas;

        public XmlHttpDomainClient(HttpClient httpClient, Type serviceInterface, OpenRiaServices.Client.DomainClients.HttpDomainClientFactory factory, XmlDictionaryReaderQuotas readerQuotas)
            : base(httpClient, serviceInterface, factory)
        {
            ArgumentNullException.ThrowIfNull(readerQuotas);
            _readerQuotas = CreateQuotasCopy(readerQuotas);
        }

        private protected override string ContentType => MediaType;

        private protected override System.Xml.XmlDictionaryReader CreateReader(Stream stream)
        {
            return XmlDictionaryReader.CreateTextReader(stream, _readerQuotas);
        }

        private protected override System.Xml.XmlDictionaryWriter CreateWriter(Stream stream)
        {
            return XmlDictionaryWriter.CreateTextWriter(stream, _encoding, ownsStream: false);
        }

        private static XmlDictionaryReaderQuotas CreateQuotasCopy(XmlDictionaryReaderQuotas source)
        {
            var copy = new XmlDictionaryReaderQuotas();
            source.CopyTo(copy);
            return copy;
        }
    }
}
