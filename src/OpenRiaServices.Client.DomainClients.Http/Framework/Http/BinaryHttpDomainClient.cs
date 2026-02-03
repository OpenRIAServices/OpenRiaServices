using System;
using System.IO;
using System.Net.Http;

namespace OpenRiaServices.Client.DomainClients.Http
{
    sealed class BinaryHttpDomainClient : DataContractHttpDomainClient
    {
        internal const string MediaType = "application/msbin1";

        public BinaryHttpDomainClient(HttpClient httpClient, Type serviceInterface) : base(httpClient, serviceInterface)
        {
        }

        private protected override string ContentType => MediaType;

        private protected override System.Xml.XmlDictionaryReader CreateReader(Stream stream)
        {
            return System.Xml.XmlDictionaryReader.CreateBinaryReader(stream, System.Xml.XmlDictionaryReaderQuotas.Max);
        }

        private protected override System.Xml.XmlDictionaryWriter CreateWriter(Stream stream)
        {
            return System.Xml.XmlDictionaryWriter.CreateBinaryWriter(stream, null, null, ownsStream: false);
        }
    }

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
