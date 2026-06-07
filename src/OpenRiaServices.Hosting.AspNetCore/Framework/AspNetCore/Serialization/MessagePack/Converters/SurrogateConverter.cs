using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Nerdbank.MessagePack;
using OpenRiaServices.Hosting.Wcf;
using PolyType.ReflectionProvider;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization.MessagePack.Converters
{
    sealed class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[]? x, byte[]? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x is null || y is null)
                return false;

            return x.AsSpan().SequenceEqual(y);
        }

        int IEqualityComparer<byte[]>.GetHashCode(byte[] obj)
        {
            return  (int)System.IO.Hashing.XxHash32.HashToUInt32(obj);
        }
    }

    /// <summary>
    /// Serializes an instance of <typeparamref name="T"/> using a surrogate type <typeparamref name="TSurrogate"/>
    /// </summary>
    internal sealed class SurrogateConverter<TSurrogate, T> : MessagePackConverter<T>
        where T : class
    {
        private readonly DomainServiceSerializationSurrogate _surrogateProvider;
        private readonly bool _useDiscriminator = true;
        private readonly FrozenDictionary<Type, byte[]> _discriminators;
        private readonly Dictionary<byte[], Type> _typeLookup;
        Type _surrogateBase = typeof(TSurrogate); 

        public SurrogateConverter(Wcf.DomainServiceSerializationSurrogate surrogateProvider)
        {
            _surrogateProvider = surrogateProvider;

            IEnumerable<Type> knownTypes;
            Func<Type, byte[]> discriminatorFunc;
            if (typeof(T) == typeof(object))
            {
                knownTypes = _surrogateProvider.SurrogateTypes;
                discriminatorFunc = static (t) => System.Text.Encoding.UTF8.GetBytes(t.FullName!);
            }
            else
            {
                _surrogateBase = surrogateProvider.GetSurrogateType(typeof(T));
                Debug.Assert(_surrogateBase == typeof(TSurrogate));
                knownTypes = _surrogateProvider.SurrogateTypes.Where(t => t.IsAssignableTo(_surrogateBase));
                discriminatorFunc = static (t) => System.Text.Encoding.UTF8.GetBytes(t.Name!);
            }

            _typeLookup = knownTypes.ToDictionary(discriminatorFunc, new ByteArrayComparer());

            _discriminators = FrozenDictionary.ToFrozenDictionary(
                _typeLookup
                    .Select(k => new KeyValuePair<Type, byte[]>(k.Value, k.Key)))
                ;

            _useDiscriminator = _typeLookup.Count > 1 || typeof(T) == typeof(object);
        }

        public override T? Read(ref MessagePackReader reader, SerializationContext context)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            Type surrogateType;
            if (_useDiscriminator)
            {
                // READ array
                // of 2 items
                // 1'st is type name (string)
                // then lookup converter for surrogate type based on type name and deserialize surrogate
                // if the type is a base type, then use original reader which consumes the type name and surrogate,
                if (!reader.TryReadArrayHeader(out int count) || count != 2)
                {
                    throw new MessagePackSerializationException("SurrogateConverter encountered array of wrong length");
                }

                if (!reader.TryReadStringSpan(out var discriminator))
                    throw new MessagePackSerializationException("SurrogateConverter failed to parse discriminator");

                // TODO: add cache for looking up types based on span
                // Need to use StructuralComparisons.StructuralEqualityComparer in dicationary
                surrogateType = _typeLookup[discriminator.ToArray()];
            }
            else
            {
                surrogateType = _surrogateBase;
            }

            MessagePackConverter converter = context.GetConverter(surrogateType, context.TypeShapeProvider);
            object? surrogate = converter.ReadObject(ref reader, context);
            return (T)_surrogateProvider.GetDeserializedObject(surrogate, typeof(T));

        }

        public override void Write(ref MessagePackWriter writer, in T? value, SerializationContext context)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            TSurrogate surrogate = (TSurrogate)_surrogateProvider.GetObjectToSerialize(value, typeof(TSurrogate));

            if (_useDiscriminator)
            {
                writer.WriteArrayHeader(2);

                byte[] discriminator = _discriminators[surrogate.GetType()];
                writer.WriteString(discriminator);
            }


            var converter = context.GetConverter(surrogate.GetType(), context.TypeShapeProvider);
            converter.WriteObject(ref writer, surrogate, context);
        }

        // TODO: Handle async serialization if the surrogate converter supports it
    }
}
