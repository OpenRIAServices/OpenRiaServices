using System;
using System.Linq;
using System.Collections.Frozen;
using System.Collections.Generic;
using Nerdbank.MessagePack;
using PolyType;
using PolyType.ReflectionProvider;
using System.Text;
using System.Reflection;

#nullable enable

namespace OpenRiaServices.Client.DomainClients.MessagePack.Converters
{
    /// <summary>
    /// The ObjectConverterFactory helps with making it possible to serialize/deserialize objects in the middle of inheritance
    /// hierarchies.
    /// The generated code have <see cref="System.Runtime.Serialization.KnownTypeAttribute"/> applied at the base class,
    /// but polytype currently ignores those attributes on derived classes.
    /// Instead we use <see cref="ObjectConverter{T}"/> as a workaround to serialize types in the middle of the inheritance hierarchy,
    /// and this factory to return those converters when needed.
    /// </summary>
    sealed class ObjectConverterFactory : IMessagePackConverterFactory
    {
        private readonly FrozenDictionary<Type, MessagePackConverter> _converters;

        public ObjectConverterFactory(Dictionary<Type, HashSet<Type>> allTypeHierarchies, ITypeShapeProvider entityShapeProvider, MessagePackSerializer objectSerializer)
        {
            Dictionary<Type, MessagePackConverter> converters = new(capacity: allTypeHierarchies.Count);
            foreach (var item in allTypeHierarchies)
            {
                var knownTypes = item.Value;

                // Skip mapping with no derived types
                if (knownTypes.Count <= 1)
                    continue;

                converters.Add(item.Key, (MessagePackConverter)Activator.CreateInstance(typeof(ObjectConverter<>).MakeGenericType(item.Key), [knownTypes, entityShapeProvider, objectSerializer])!);
            }
            converters.Add(typeof(object), new ObjectConverter<object>(allTypeHierarchies.Keys, entityShapeProvider, objectSerializer));

            _converters = converters.ToFrozenDictionary();
        }

        MessagePackConverter? IMessagePackConverterFactory.CreateConverter(Type type, ITypeShape? shape, in ConverterContext context)
        {
            // Only return type shapes for specified converter, this allows nerdbank default converters to be generated for other providers
            if (_converters.TryGetValue(type, out var converter))
                return converter;
            else
                return null;
        }

 
    }
}
