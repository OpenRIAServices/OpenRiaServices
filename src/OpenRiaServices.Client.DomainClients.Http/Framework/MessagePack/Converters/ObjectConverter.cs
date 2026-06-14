using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Nerdbank.MessagePack;
using PolyType;
using PolyType.ReflectionProvider;

#nullable enable

namespace OpenRiaServices.Client.DomainClients.MessagePack.Converters
{
    internal sealed class ObjectConverter<T> : MessagePackConverter<T>
        where T : class
    {
        private readonly FrozenDictionary<Type, byte[]> _discriminators;
        private readonly Dictionary<byte[], Type> _typeLookup;
        // We need to use a seprate type shape provider for looking up converters
        // otherwise we will get union converters instead of the object converter for the underlying type 
        private static readonly ReflectionTypeShapeProvider s_reflectionTypeShapeProvider = ReflectionTypeShapeProvider.Create(new()
        {
            // Note: We need to specify a new unique set of assemblies to avoid conflicts with the main type shape provider
            TypeShapeExtensionAssemblies = new[] { typeof(DomainClient).Assembly }
        });

        // TODO: Pass in IEnumerator<Type> derivedTypes
        // This will allow us to build _surrogateFactory, as well as to create dictionaries with only relevant surrogate types
        public ObjectConverter(IEnumerable<Type> knownTypes)
        {
            // TODO: Prefer discriminator from [DerivedTypeShapeAttribute]
            // Take namespace from datacontract if any ???
            Func<Type, byte[]> discriminatorFunc = static (t) => System.Text.Encoding.UTF8.GetBytes(t.Name!);

            var comparer = StructuralEqualityComparer.GetDefault(s_reflectionTypeShapeProvider.GetTypeShape<byte[]>());

            // This avoids double dictionary lookups in surrogateProvider
            _typeLookup = knownTypes.Where(t => !t.IsAbstract).ToDictionary(discriminatorFunc, comparer);
            _discriminators = FrozenDictionary.ToFrozenDictionary(_typeLookup.Select(k => new KeyValuePair<Type, byte[]>(k.Value, k.Key)));

        }

        public override T? Read(ref MessagePackReader reader, SerializationContext context)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            Type type;
            // READ array
            // of 2 items
            // 1'st is type name (string)
            // then lookup converter for surrogate type based on type name and deserialize surrogate
            // if the type is a base type, then use original reader which consumes the type name and surrogate,
            if (!reader.TryReadArrayHeader(out int count) || count != 2)
            {
                throw new MessagePackSerializationException("SurrogateConverter encountered array of wrong length");
            }

            // Handle null input
            if (reader.TryReadNil())
            {
                if (typeof(T) == typeof(Entity) || typeof(T) == typeof(object))
                {
                    throw new MessagePackSerializationException("SurrogateConverter failed to parse discriminator");
                }
                else
                    type = typeof(T);
            }
            else
            {
                if (!reader.TryReadStringSpan(out var discriminator))
                    throw new MessagePackSerializationException("SurrogateConverter failed to parse discriminator");

                // TODO: add cache for looking up types based on span
#if NET10_0_OR_GREATER
                    surrogateType = _typeLookup.GetAlternateLookup<ReadOnlySpan<byte>>()[discriminator];
#else
                type = _typeLookup[discriminator.ToArray()];
#endif
            }

            MessagePackConverter converter = GetConverter(type, ref context);
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


            writer.WriteArrayHeader(2);
            if (typeof(T) == type && (typeof(T) != typeof(Entity) && typeof(T) != typeof(object)))
            {
                writer.WriteNil();
            }
            else
            {
                // TODO: find first known type to handle inheritance without
                //if (_discriminators.TryGetValue
                writer.WriteString(_discriminators[type]);
            }

            MessagePackConverter converter = GetConverter(type, ref context);
            converter.WriteObject(ref writer, value, context);
        }

        private static MessagePackConverter GetConverter(Type type, ref SerializationContext context)
        {
            ITypeShape shape = s_reflectionTypeShapeProvider.GetTypeShapeOrThrow(type);
            if (shape is PolyType.Abstractions.IUnionTypeShape unionShape)
                shape = unionShape.BaseType;

            MessagePackConverter converter = context.GetConverter(shape);

            Debug.Assert(converter.GetType()!.FullName.StartsWith("Nerdbank.MessagePack.Converters.Object", StringComparison.Ordinal), "Must not get union converter");

            return converter;
        }
        // TODO: Handle async serialization if the surrogate converter supports it
    }
}
