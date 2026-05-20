using System.Xml;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization
{
    /// <summary>
    /// Base options class for DataContract-based serializers used by OpenRIA Services.
    /// </summary>
    public class DataContractSerializerOptions
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
        /// Gets or sets the XML dictionary used for binary format compression.
        /// Exposed publicly on <see cref="BinaryDataContractSerializerOptions"/> only.
        /// </summary>
        internal IXmlDictionary? Dictionary { get; set; }

        /// <summary>
        /// Creates a new mutable <see cref="XmlDictionaryReaderQuotas"/> with all quotas set to their maximum values.
        /// </summary>
        private static XmlDictionaryReaderQuotas CreateMaxQuotas()
        {
            var quotas = new XmlDictionaryReaderQuotas();
            XmlDictionaryReaderQuotas.Max.CopyTo(quotas);
            return quotas;
        }
    }

    /// <summary>
    /// Options for the binary XML (<c>application/msbin1</c>) DataContract serializer.
    /// </summary>
    public sealed class BinaryDataContractSerializerOptions : DataContractSerializerOptions
    {
        /// <summary>
        /// Gets or sets the XML dictionary used for binary format compression when reading and writing.
        /// </summary>
        /// <remarks>
        /// When set, the same dictionary is applied to both the <see cref="System.Xml.XmlDictionaryReader"/>
        /// and the <see cref="System.Xml.XmlDictionaryWriter"/> to enable consistent compression of element
        /// and attribute names. Client and server must share the same dictionary.
        /// </remarks>
        public new IXmlDictionary? Dictionary
        {
            get => base.Dictionary;
            set => base.Dictionary = value;
        }
    }

    /// <summary>
    /// Options for the text XML (<c>application/xml</c>) DataContract serializer.
    /// </summary>
    public sealed class XmlDataContractSerializerOptions : DataContractSerializerOptions
    {
    }
}
