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

        public ObjectConverterFactory(HashSet<Type> allTypes, ITypeShapeProvider entityShapeProvider, MessagePackSerializer objectSerializer)
        {
            Dictionary<Type, MessagePackConverter> converters = new(capacity: allTypes.Count);
            foreach (var item in ComputeKnownTypeSet(allTypes))
            {
                var knownTypes = item.Value;

                System.Diagnostics.Debug.Assert(knownTypes.IsSubsetOf(allTypes), "Known types should always contain at least the type itself.");
                //allTypes.UnionWith(knownTypes);

                // Skip mapping with no derived types
                knownTypes.Add(item.Key);
                if (knownTypes.Count <= 1)
                    continue;

                converters.Add(item.Key, (MessagePackConverter)Activator.CreateInstance(typeof(ObjectConverter<>).MakeGenericType(item.Key), [knownTypes, entityShapeProvider, objectSerializer])!);
            }
            converters.Add(typeof(object), new ObjectConverter<object>(allTypes, entityShapeProvider, objectSerializer));

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

        /// <summary>
        /// Computes the closure of known types for all the <paramref name="types" />.
        /// </summary>
        /// <returns>A dictionary, keyed by type and containing all the
        /// declared known types for it, including the transitive closure.
        /// </returns>
        private static Dictionary<Type, HashSet<Type>> ComputeKnownTypeSet(IEnumerable<Type> types)
        {
            Dictionary<Type, HashSet<Type>> closure = new Dictionary<Type, HashSet<Type>>();

            // Gather all the explicit derived types from attributes.
            foreach (Type entityType in types)
            {
                IEnumerable<Type> knownTypes = Server.KnownTypeUtilities.ImportDerivedTypes(entityType, /* inherit */ true);
                closure[entityType] = new HashSet<Type>(knownTypes);
            }

            // 2nd pass -- add all the derived types' known types back to their base so we have the closure
            foreach (Type entityType in types)
            {
                HashSet<Type> knownTypes = closure[entityType];
                for (Type? baseType = entityType.BaseType;
                     baseType != null && baseType != typeof(Entity);
                     baseType = baseType.BaseType)
                {
                    if (closure.TryGetValue(baseType, out HashSet<Type>? hash))
                    {
                        hash.UnionWith(knownTypes);
                    }
                }
            }
            return closure;
        }
    }
}
