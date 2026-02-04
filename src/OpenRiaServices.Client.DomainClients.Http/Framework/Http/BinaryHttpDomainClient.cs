using System;
using System.IO;
using System.Net.Http;

namespace OpenRiaServices.Client.DomainClients.Http
{
    sealed class BinaryHttpDomainClient : DataContractHttpDomainClient
    {
        internal const string MediaType = "application/msbin1";

        /// <summary>
        /// Initializes a new BinaryHttpDomainClient configured to communicate using the binary media type.
        /// </summary>
        /// <param name="httpClient">The HttpClient used to send HTTP requests.</param>
        /// <param name="serviceInterface">The service interface type that this client will target.</param>
        public BinaryHttpDomainClient(HttpClient httpClient, Type serviceInterface) : base(httpClient, serviceInterface)
        {
        }

        private protected override string ContentType => MediaType;

        /// <summary>
        /// Creates an XmlDictionaryReader that reads binary XML from the provided stream.
        /// </summary>
        /// <param name="stream">The input stream to read binary XML from.</param>
        /// <returns>An XmlDictionaryReader configured for binary XML reading from the supplied stream with maximum reader quotas.</returns>
        private protected override System.Xml.XmlDictionaryReader CreateReader(Stream stream)
        {
            return System.Xml.XmlDictionaryReader.CreateBinaryReader(stream, System.Xml.XmlDictionaryReaderQuotas.Max);
        }

        /// <summary>
        /// Creates an XmlDictionaryWriter configured to write binary XML to the given stream.
        /// </summary>
        /// <param name="stream">The stream to which binary XML will be written.</param>
        /// <returns>An XmlDictionaryWriter that emits binary XML to <paramref name="stream"/>; disposing the writer does not close the underlying stream.</returns>
        private protected override System.Xml.XmlDictionaryWriter CreateWriter(Stream stream)
        {
            return System.Xml.XmlDictionaryWriter.CreateBinaryWriter(stream, null, null, ownsStream: false);
        }
    }

    sealed class XmlHttpDomainClient : DataContractHttpDomainClient
    {
        internal const string MediaType = "application/xml";

        private readonly System.Text.Encoding _encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        /// <summary>
        /// Initializes a new XmlHttpDomainClient configured to use XML media type for communication.
        /// </summary>
        /// <param name="httpClient">The HttpClient used to send and receive HTTP requests.</param>
        /// <param name="serviceInterface">The service interface type this domain client targets.</param>
        public XmlHttpDomainClient(HttpClient httpClient, Type serviceInterface) : base(httpClient, serviceInterface)
        {
        }

        private protected override string ContentType => MediaType;

        /// <summary>
        /// Creates a text XML reader over the provided stream.
        /// </summary>
        /// <returns>An XmlDictionaryReader that reads text from the provided stream using maximum quotas.</returns>
        private protected override System.Xml.XmlDictionaryReader CreateReader(Stream stream)
        {
            return System.Xml.XmlDictionaryReader.CreateTextReader(stream, System.Xml.XmlDictionaryReaderQuotas.Max);
        }

        /// <summary>
        /// Creates a text-based XmlDictionaryWriter that writes XML to the provided stream using the client's UTF-8 encoding.
        /// </summary>
        /// <param name="stream">The output stream to which XML will be written.</param>
        /// <returns>An XmlDictionaryWriter that writes text XML to the supplied stream.</returns>
        private protected override System.Xml.XmlDictionaryWriter CreateWriter(Stream stream)
        {
            return System.Xml.XmlDictionaryWriter.CreateTextWriter(stream, _encoding, ownsStream: false);
        }
    }
}