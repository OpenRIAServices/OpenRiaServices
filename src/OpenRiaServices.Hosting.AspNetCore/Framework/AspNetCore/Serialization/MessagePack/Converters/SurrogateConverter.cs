using System;
using System.Buffers;
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
    sealed class ByteArrayComparer : IEqualityComparer<byte[]?>
#if NET10_0_OR_GREATER
        , IAlternateEqualityComparer<ReadOnlySpan<byte>, byte[]?>
#endif
    {
        public bool Equals(byte[]? x, byte[]? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x is null || y is null)
                return false;

            return x.AsSpan().SequenceEqual(y);
        }

        int IEqualityComparer<byte[]?>.GetHashCode(byte[]? obj)
        {
            return obj == null ? 0 : (int)System.IO.Hashing.XxHash32.HashToUInt32(obj);
        }

#if NET10_0_OR_GREATER
        byte[]? IAlternateEqualityComparer<ReadOnlySpan<byte>, byte[]?>.Create(ReadOnlySpan<byte> alternate)
            => alternate.ToArray();

        bool IAlternateEqualityComparer<ReadOnlySpan<byte>, byte[]?>.Equals(ReadOnlySpan<byte> alternate, byte[]? other)
            => other is not null && alternate.SequenceEqual(other);

        int IAlternateEqualityComparer<ReadOnlySpan<byte>, byte[]?>.GetHashCode(ReadOnlySpan<byte> alternate)
            => (int)System.IO.Hashing.XxHash32.HashToUInt32(alternate);
#endif
    }

    /// <summary>
    /// Serializes an instance of <typeparamref name="T"/> using a surrogate type <typeparamref name="TSurrogate"/>
    /// </summary>
    internal sealed class SurrogateConverter<TSurrogate, T> : MessagePackConverter<T>
        where T : class
    {
        private readonly DomainServiceSerializationSurrogate _surrogateProvider;
        private readonly bool _useDiscriminator = true;
        private readonly FrozenDictionary<Type, byte[]?> _discriminators;
        private readonly Dictionary<byte[], Type> _typeLookup;
        //private readonly FrozenDictionary<Type, Func<object, object>> _surrogateFactory;
        private readonly Type _surrogateBase = typeof(TSurrogate);

        // TODO: Pass in IEnumerator<Type> derivedTypes
        // This will allow us to build _surrogateFactory, as well as to create dictionaries with only relevant surrogate types
        public SurrogateConverter(Wcf.DomainServiceSerializationSurrogate surrogateProvider)
        {
            _surrogateProvider = surrogateProvider;

            IEnumerable<Type> knownTypes;
            Func<Type, byte[]> discriminatorFunc;
            if (typeof(T) == typeof(object))
            {
                knownTypes = _surrogateProvider.SurrogateTypes;

                // TODO: Prefer discriminator from [DerivedTypeShapeAttribute]
                // Take namespace from datacontract if any ???
                discriminatorFunc = static (t) => System.Text.Encoding.UTF8.GetBytes(t.Name!);
            }
            else
            {
                _surrogateBase = surrogateProvider.GetSurrogateType(typeof(T));
                Debug.Assert(_surrogateBase == typeof(TSurrogate));
                knownTypes = _surrogateProvider.SurrogateTypes.Where(t => t.IsAssignableTo(_surrogateBase));

                // Take namespace from datacontract ???
                discriminatorFunc = (t) => (t != _surrogateBase) ? System.Text.Encoding.UTF8.GetBytes(t.Name!) : [];
            }

            // TODO: Consider a dictionary for surrogate factory lookup
            // This avoids double dictionary lookups in surrogateProvider
            _typeLookup = knownTypes.ToDictionary(discriminatorFunc, new ByteArrayComparer());
            //_surrogateFactory = knownTypes.ToDictionary(t => t, _surrogateProvider.GetSurrogateFactory).ToFrozenDictionary();
            _discriminators = FrozenDictionary.ToFrozenDictionary(
                _typeLookup
                    .Select(k => new KeyValuePair<Type, byte[]?>(k.Value, k.Key is [] ? null : k.Key)));

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

                if (reader.TryReadNil())
                {
                    surrogateType = _typeLookup[[]];
                }
                else
                {
                    if (reader.NextMessagePackType != MessagePackType.String)
                    {
                        throw new MessagePackSerializationException("SurrogateConverter expected string discriminator");
                    }

                    if (reader.TryReadStringSpan(out var discriminator))
                    {
                        // TODO: add cache for looking up types based on span
#if NET10_0_OR_GREATER
                        surrogateType = _typeLookup.GetAlternateLookup<ReadOnlySpan<byte>>()[discriminator];
#else
                        surrogateType = _typeLookup[discriminator.ToArray()];
#endif
                    }
                    else if (reader.ReadStringSequence() is { } sequence)
                    {
                        surrogateType = _typeLookup[BuffersExtensions.ToArray(sequence)];
                    }
                    else
                    {
                        throw new MessagePackSerializationException("SurrogateConverter expected string discriminator");
                    }
                }
            }
            else
            {
                surrogateType = _surrogateBase;
            }

            MessagePackConverter converter = context.GetConverter(surrogateType, context.TypeShapeProvider);
            object? surrogate = converter.ReadObject(ref reader, context);
            return (T)DomainServiceSerializationSurrogate.GetDeserializedObject(surrogate);

        }

        public override void Write(ref MessagePackWriter writer, in T? value, SerializationContext context)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            object surrogate = _surrogateProvider.GetObjectToSerialize(value, typeof(TSurrogate));
            // _surrogateFactory.TryGetValue(value.GetType(), out var factory) ? factory(value)
            // Below code handles derived proxy types and similar
            //: GetSurrogateTypeSlow(value);

            if (_useDiscriminator)
            {
                writer.WriteArrayHeader(2);

                byte[]? discriminator = _discriminators[surrogate.GetType()];

                if (discriminator is null)
                    writer.WriteNil();
                else
                    writer.WriteString(discriminator);
            }

            var converter = context.GetConverter(surrogate.GetType(), context.TypeShapeProvider);
            converter.WriteObject(ref writer, surrogate, context);

            //object GetSurrogateTypeSlow(in T value)
            //{
            //    Type? baseType = value.GetType().BaseType;
            //    while (baseType != null)
            //    {
            //        if (_surrogateFactory.TryGetValue(baseType, out var factory))
            //        {
            //            return factory(value);
            //        }
            //    }

            //    throw new InvalidOperationException($"Could not get surrogate type for {value.GetType()}");
            //}
        }


        // TODO: Handle async serialization if the surrogate converter supports it
    }
}
