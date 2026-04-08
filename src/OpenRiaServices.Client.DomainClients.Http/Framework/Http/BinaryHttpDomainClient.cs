using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;

namespace OpenRiaServices.Client.DomainClients.Http
{
    /// <summary>
    /// <see cref="DomainClient"/> implementation which uses plain binary Xml using <see cref="DataContractSerializer"/> using the default format
    /// since WCF RIA Services.
    /// </summary>
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
}
