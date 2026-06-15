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
using OpenRiaServices.Server;
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
        private readonly bool _useDiscriminator = true;
        private readonly FrozenDictionary<Type, byte[]?> _discriminators;
        private readonly FrozenDictionary<byte[], Type> _typeLookup;
        private readonly FrozenDictionary<Type, Func<object, object>> _surrogateFactory;
        private readonly Type _surrogateBase = typeof(TSurrogate);

        public SurrogateConverter(DomainServiceDescription description, Wcf.DomainServiceSerializationSurrogate surrogateProvider)
        {
            _surrogateProvider = surrogateProvider;

            IEnumerable<Type> knownTypes;
            IEnumerable<Type> surrogateTypes;
            Func<Type, byte[]> discriminatorFunc;
            if (typeof(T) == typeof(object))
            {
                surrogateTypes = _surrogateProvider.SurrogateTypes;
                discriminatorFunc = MessagePackUtility.GetDiscriminator;

                HashSet<Type> allKnownTypes = new(description.EntityTypes);
                //allKnownTypes.Add(description.ComplexTypes);
                foreach (var (entityType, entityKnownTypes) in description.EntityKnownTypes)
                {
                    allKnownTypes.UnionWith(entityKnownTypes);
                }
                knownTypes = allKnownTypes;
            }
            else
            {
                // Entity types can have derived types
                if (description.EntityKnownTypes.TryGetValue(typeof(T), out var knownSet))
                {
                    knownTypes = knownSet.Contains(typeof(T)) ? knownSet : [typeof(T), .. knownSet];
                }
                else
                {
                    knownTypes = [typeof(T)];
                }

                _surrogateBase = surrogateProvider.GetSurrogateType(typeof(T));
                Debug.Assert(_surrogateBase == typeof(TSurrogate));

                surrogateTypes = knownTypes.Select(surrogateProvider.GetSurrogateType);
                discriminatorFunc = (t) => (t != _surrogateBase) ? MessagePackUtility.GetDiscriminator(t) : [];
            }

            // TODO: Consider a dictionary for surrogate factory lookup
            // This avoids double dictionary lookups in surrogateProvider
            _surrogateFactory = knownTypes
                .Where(t => !t.IsAbstract)
                .ToDictionary(t => t, _surrogateProvider.GetSurrogateFactory).ToFrozenDictionary();
            _typeLookup = surrogateTypes.ToFrozenDictionary(discriminatorFunc, new MessagePackUtility.ByteArrayComparer());
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

            //object surrogate = _surrogateProvider.GetObjectToSerialize(value, typeof(TSurrogate));
            object surrogate = _surrogateFactory.TryGetValue(value.GetType(), out var factory) ? factory(value)
                // Below code handles derived proxy types and similar
                : GetSurrogateTypeSlow(value);

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

            object GetSurrogateTypeSlow(in T value)
            {
                Type? baseType = value.GetType().BaseType;
                while (baseType != null)
                {
                    if (_surrogateFactory.TryGetValue(baseType, out var factory))
                    {
                        return factory(value);
                    }
                    baseType = baseType.BaseType;
                }

                throw new InvalidOperationException($"Could not get surrogate type for {value.GetType()}");
            }
        }


        // TODO: Handle async serialization if the surrogate converter supports it
    }
}
