using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Xml;

namespace OpenRiaServices.Client.DomainClients.Http
{
    /// <summary>
    /// <see cref="DomainClient"/> implementation which uses plain binary Xml using <see cref="DataContractSerializer"/> using the default format
    /// since WCF RIA Services.
    /// </summary>
    sealed class BinaryHttpDomainClient : DataContractHttpDomainClient
    {
        internal const string MediaType = "application/msbin1";
        private readonly XmlDictionaryReaderQuotas _readerQuotas;
        private readonly IXmlDictionary _dictionary;

        public BinaryHttpDomainClient(HttpClient httpClient, Type serviceInterface, BinaryHttpDomainClientFactory factory)
            : base(httpClient, serviceInterface, factory)
        {
            _readerQuotas = CreateQuotasCopy(factory.ReaderQuotas);
            _dictionary = factory.Dictionary;
        }

        private protected override string ContentType => MediaType;

        private protected override XmlDictionaryReader CreateReader(Stream stream)
        {
            return XmlDictionaryReader.CreateBinaryReader(stream, _dictionary, _readerQuotas);
        }

        private protected override XmlDictionaryWriter CreateWriter(Stream stream)
        {
            return XmlDictionaryWriter.CreateBinaryWriter(stream, _dictionary, null, ownsStream: false);
        }

        private static XmlDictionaryReaderQuotas CreateQuotasCopy(XmlDictionaryReaderQuotas source)
        {
            var copy = new XmlDictionaryReaderQuotas();
            source.CopyTo(copy);
            return copy;
        }
    }
}
