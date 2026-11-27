using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Buffers;
using System.Linq;
using Nerdbank.MessagePack;
using PolyType;
using PolyType.ReflectionProvider;

#pragma warning disable NBMsgPack030 // Converters should not call top-level `MessagePackSerializer` methods
#nullable enable

namespace OpenRiaServices.Client.DomainClients.MessagePack.Converters
{
    /// <summary>
    /// Serializes a class hierarchy by writing an array of 2 items [string? "typename", Instance]
    /// <para>The serialization mimics DataContractSerializer in that it will work efen if T is not the base type</para>
    /// </summary>
    /// <remarks>
    /// PolyType and therefore NerdBank.MessagePack has limitations when it comes to polymorphic serialization, when any other class than the base class is serialized.
    /// The converter uses a dictionary to map between types and their corresponding discriminators, which are generated based on the known types provided to the constructor.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    sealed class ObjectConverter<T> : MessagePackConverter<T>
        where T : class
    {
        private readonly FrozenDictionary<Type, (byte[] discriminator, MessagePackConverter converter)> _discriminators;
        private readonly FrozenDictionary<byte[], MessagePackConverter> _typeLookup;
        // The base type of the hierarchy, used to determine if we need to write a discriminator when the declared type is not the base type
        private readonly Type? _baseType;
        private readonly MessagePackConverter? _baseConverter;

        public ObjectConverter(IEnumerable<Type> knownTypes, CustomMessagePackSerializer converterProvider)
        {
            var comparer = new MessagePackUtility.ByteArrayComparer();

            // This avoids double dictionary lookups in surrogateProvider
            var concreteTypes = knownTypes.Where(t => !t.IsAbstract);
            var converters = converterProvider.CopyConverters(concreteTypes);

            _discriminators = converters
                .ToFrozenDictionary(k => k.Key, v => (MessagePackUtility.GetDiscriminator(v.Key), v.Value));

            _typeLookup = _discriminators.ToFrozenDictionary(x => x.Value.discriminator, x => x.Value.converter, comparer);
          
            if (typeof(Entity).IsAssignableFrom(typeof(T)) && typeof(T) != typeof(Entity))
            {
                // See if T has KnownTypes that attribute ?? to be sure it is generated base class
                var computedBaseType = typeof(T);
                while (computedBaseType.BaseType != typeof(Entity))
                    computedBaseType = computedBaseType.BaseType!;

                this._baseType = computedBaseType;
                _baseConverter = _discriminators.TryGetValue(computedBaseType, out var value) ? value.converter : null;
            }
            else
                this._baseType = null;
        }

        public override T? Read(ref MessagePackReader reader, SerializationContext context)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            Type type;
            // READ array of 2 items
            // 1'st is type name (string)
            // then lookup converter for surrogate type based on type name and deserialize surrogate
            if (!reader.TryReadArrayHeader(out int count) || count != 2)
            {
                throw new MessagePackSerializationException("SurrogateConverter encountered array of wrong length");
            }

            MessagePackConverter converter;
            if (reader.TryReadNil())
            {
                converter = _baseConverter ?? throw new MessagePackSerializationException("SurrogateConverter failed to parse discriminator");
            }
            else
            {
                if (reader.NextMessagePackType != MessagePackType.String)
                {
                    throw new MessagePackSerializationException("SurrogateConverter expected string discriminator");
                }

                if (reader.TryReadStringSpan(out var discriminator))
                {
#if NET10_0_OR_GREATER
                    converter = _typeLookup.GetAlternateLookup<ReadOnlySpan<byte>>()[discriminator];
#else
                    converter = _typeLookup[discriminator.ToArray()];
#endif
                }
                else if (reader.ReadStringSequence() is { } sequence)
                {
                    converter = _typeLookup[BuffersExtensions.ToArray(sequence)];
                }
                else
                    throw new MessagePackSerializationException("SurrogateConverter failed to parse discriminator");
            }

            return (T?)converter.ReadObject(ref reader, context);
        }

        public override void Write(ref MessagePackWriter writer, in T? value, SerializationContext context)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            Type type = value.GetType();
            MessagePackConverter converter;
            writer.WriteArrayHeader(2);
            if (type == _baseType)
            {
                writer.WriteNil();
                converter = _baseConverter!;
            }
            else
            {
                (byte[] discriminator, MessagePackConverter converter) result;
                while (!_discriminators.TryGetValue(type, out result))
                {
                    if (type.BaseType == null)
                        throw new MessagePackSerializationException($"SurrogateConverter failed to find converter for type {value.GetType()}");
                    type = type.BaseType;
                }

                writer.WriteString(result.discriminator);
                converter = result.converter;
            }

            converter.WriteObject(ref writer, value, context);
        }
        // TODO: Handle async serialization if the surrogate converter supports it
    }
}
