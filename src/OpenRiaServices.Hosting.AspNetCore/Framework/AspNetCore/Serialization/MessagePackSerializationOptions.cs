using Nerdbank.MessagePack;
using PolyType;
using PolyType.ReflectionProvider;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization
{
    /// <summary>
    /// Options for MessagePack wire format serialization.
    /// </summary>
    public sealed class MessagePackSerializationOptions
    {
        /// <summary>
        /// Gets or sets the serializer instance used for MessagePack read/write operations.
        /// </summary>
        public MessagePackSerializer Serializer { get; set; } = new();

        /// <summary>
        /// Gets or sets the type-shape provider used to resolve runtime type metadata for serialization.
        /// </summary>
        public ITypeShapeProvider TypeShapeProvider { get; set; } = ReflectionTypeShapeProvider.Default;
    }
}
