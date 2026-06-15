using System;
using System.Buffers;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Nerdbank.MessagePack;
using OpenRiaServices.Hosting.Wcf;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization.MessagePack.Converters
{
    /// <summary>
    /// Serializes an instance of <typeparamref name="T"/> using a surrogate type <typeparamref name="TSurrogate"/>
    /// </summary>
    internal sealed class SurrogateConverter<TSurrogate, T> : MessagePackConverter<T>
        where T : class
    {
        private readonly bool _useDiscriminator = true;
        private readonly FrozenDictionary<Type, byte[]?> _discriminators;
        private readonly FrozenDictionary<byte[], Type> _typeLookup;
        private readonly FrozenDictionary<Type, Func<object, object>> _surrogateFactory;
        private readonly Type _surrogateBase;

        public SurrogateConverter(DomainServiceDescription description, DomainServiceSerializationSurrogate surrogateProvider)
        {
            _surrogateBase = surrogateProvider.GetSurrogateType(typeof(T));
            IEnumerable<Type> knownTypes;

            // Object is used as property on ChangeSetEntry, where it can be any Entity exposed by the DomainService
            if (typeof(T) == typeof(object))
            {
                HashSet<Type> allKnownTypes = new(description.EntityTypes);
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
                    // We are creating surrogate for a complex type (inheritance is not currently allowed)
                    knownTypes = [typeof(T)];
                }

                _surrogateBase = surrogateProvider.GetSurrogateType(typeof(T));
                Debug.Assert(_surrogateBase == typeof(TSurrogate));
            }


            IEnumerable<Type> surrogateTypes = knownTypes.Select(surrogateProvider.GetSurrogateType);
            Func<Type, byte[]> discriminatorFunc = (Type t) => (t != _surrogateBase) ? MessagePackUtility.GetDiscriminator(t) : [];

            // Setup lookup dictionaries for mapping types to surrogate and discriminators, and from discriminator back to surrogate type
            _surrogateFactory = knownTypes
                .Where(t => !t.IsAbstract)
                .ToDictionary(t => t, surrogateProvider.GetSurrogateFactory).ToFrozenDictionary();
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
                // same as surrogateType = _typeLookup[[]];
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
