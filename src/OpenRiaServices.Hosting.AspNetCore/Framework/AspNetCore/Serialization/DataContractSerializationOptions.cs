using System.Xml;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization
{
    /// <summary>
    /// Private interface for DataContract-based serializer options used internally by OpenRIA Services.
    /// </summary>
    internal interface IDataContractSerializationOptions
    {
        XmlDictionaryReaderQuotas ReaderQuotas { get; }
        IXmlDictionary? Dictionary { get; }
    }

    /// <summary>
    /// Options for the binary XML (<c>application/msbin1</c>) DataContract serializer.
    /// </summary>
    public sealed class BinarySerializationOptions : IDataContractSerializationOptions
    {
        /// <summary>
        /// Gets or sets the quotas applied to <see cref="XmlDictionaryReader"/> instances during deserialization.
        /// </summary>
        /// <remarks>
        /// Defaults to a mutable instance equivalent to <see cref="XmlDictionaryReaderQuotas.Max"/> to preserve existing behavior.
        /// Restrict these quotas to limit resource consumption and mitigate denial-of-service attacks.
        /// </remarks>
        public XmlDictionaryReaderQuotas ReaderQuotas { get; set; } = CreateMaxQuotas();

        /// <summary>
        /// Gets or sets the XML dictionary used for binary format compression when reading and writing.
        /// </summary>
        /// <remarks>
        /// When set, the same dictionary is applied to both the <see cref="System.Xml.XmlDictionaryReader"/>
        /// and the <see cref="System.Xml.XmlDictionaryWriter"/> to enable consistent compression of element
        /// and attribute names. Client and server must share the same dictionary.
        /// <para>See <see cref="XmlDictionary"/> for additional information.</para>
        /// </remarks>
        public IXmlDictionary? Dictionary { get; set; }

        private static XmlDictionaryReaderQuotas CreateMaxQuotas()
        {
            var quotas = new XmlDictionaryReaderQuotas();
            XmlDictionaryReaderQuotas.Max.CopyTo(quotas);
            return quotas;
        }
    }

    /// <summary>
    /// Options for the text XML (<c>application/xml</c>) DataContract serializer.
    /// </summary>
    public sealed class XmlSerializationOptions : IDataContractSerializationOptions
    {
        /// <summary>
        /// Gets or sets the quotas applied to <see cref="XmlDictionaryReader"/> instances during deserialization.
        /// </summary>
        /// <remarks>
        /// Defaults to a mutable instance equivalent to <see cref="XmlDictionaryReaderQuotas.Max"/> to preserve existing behavior.
        /// Restrict these quotas to limit resource consumption and mitigate denial-of-service attacks.
        /// </remarks>
        public XmlDictionaryReaderQuotas ReaderQuotas { get; set; } = CreateMaxQuotas();

        IXmlDictionary? IDataContractSerializationOptions.Dictionary => null;

        private static XmlDictionaryReaderQuotas CreateMaxQuotas()
        {
            var quotas = new XmlDictionaryReaderQuotas();
            XmlDictionaryReaderQuotas.Max.CopyTo(quotas);
            return quotas;
        }
    }
}
