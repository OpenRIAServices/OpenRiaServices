using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;

namespace OpenRiaServices.Client.DomainClients.Http
{
    /// <summary>
    /// <see cref="DomainClient"/> implementation which uses plain utf8 encoded text Xml over HTTP using <see cref="DataContractSerializer"/> for serialisation
    /// </summary>
    sealed class XmlHttpDomainClient : DataContractHttpDomainClient
    {
        internal const string MediaType = "application/xml";

        private readonly System.Text.Encoding _encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public XmlHttpDomainClient(HttpClient httpClient, Type serviceInterface) : base(httpClient, serviceInterface)
        {
        }

        private protected override string ContentType => MediaType;

        private protected override System.Xml.XmlDictionaryReader CreateReader(Stream stream)
        {
            return System.Xml.XmlDictionaryReader.CreateTextReader(stream, System.Xml.XmlDictionaryReaderQuotas.Max);
        }

        private protected override System.Xml.XmlDictionaryWriter CreateWriter(Stream stream)
        {
            return System.Xml.XmlDictionaryWriter.CreateTextWriter(stream, _encoding, ownsStream: false);
        }
    }
}
