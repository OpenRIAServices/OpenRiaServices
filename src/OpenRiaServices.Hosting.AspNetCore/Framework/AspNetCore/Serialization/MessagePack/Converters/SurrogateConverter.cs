using System;
using Nerdbank.MessagePack;
using OpenRiaServices.Hosting.Wcf;
using PolyType.ReflectionProvider;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization.MessagePack.Converters
{
    /// <summary>
    /// Serializes an instance of <typeparamref name="T"/> using a surrogate type <typeparamref name="TSurrogate"/>
    /// </summary>
    internal sealed class SurrogateConverter<TSurrogate, T> : MessagePackConverter<T>
        where T : class
    {
        private readonly DomainServiceSerializationSurrogate _surrogateProvider;

        public SurrogateConverter(Wcf.DomainServiceSerializationSurrogate surrogateProvider)
        {
            _surrogateProvider = surrogateProvider;
        }

        public override T? Read(ref MessagePackReader reader, SerializationContext context)
        {
            var converter = context.GetConverter<TSurrogate>(context.TypeShapeProvider);

            TSurrogate? surrogate = converter.Read(ref reader, context);
            return (T)_surrogateProvider.GetDeserializedObject(surrogate, typeof(T));
        }

        public override void Write(ref MessagePackWriter writer, in T? value, SerializationContext context)
        {
            var converter = context.GetConverter<TSurrogate>(context.TypeShapeProvider);

            TSurrogate surrogate = (TSurrogate)_surrogateProvider.GetObjectToSerialize(value, typeof(TSurrogate));
            converter.Write(ref writer, surrogate, context);
        }

        // TODO: Handle async serialization if the surrogate converter supports it

    }
}
