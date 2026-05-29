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

        public BinaryHttpDomainClient(HttpClient httpClient, Type serviceInterface, XmlDictionaryReaderQuotas readerQuotas, IXmlDictionary dictionary)
            : base(httpClient, serviceInterface)
        {
            ArgumentNullException.ThrowIfNull(readerQuotas);
            _readerQuotas = CreateQuotasCopy(readerQuotas);
            _dictionary = dictionary;
        }

        private protected override string ContentType => MediaType;


        private protected override System.Xml.XmlDictionaryReader CreateReader(Stream stream)
        {
            return XmlDictionaryReader.CreateBinaryReader(stream, _dictionary, _readerQuotas);
        }

        private protected override System.Xml.XmlDictionaryWriter CreateWriter(Stream stream)
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
