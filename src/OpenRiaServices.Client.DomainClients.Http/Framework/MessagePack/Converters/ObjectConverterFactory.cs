using System;
using System.Linq;
using System.Collections.Frozen;
using System.Collections.Generic;
using Nerdbank.MessagePack;
using PolyType;
using PolyType.ReflectionProvider;

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

        // We need to use a seprate type shape provider for looking up converters
        // otherwise we will get union converters instead of the object converter for the underlying type 
        internal static readonly ReflectionTypeShapeProvider ObjectConverterTypeShapeProvider = ReflectionTypeShapeProvider.Create(new()
        {
            // Note: We need to specify a new unique set of assemblies to avoid conflicts with the main type shape provider
            TypeShapeExtensionAssemblies = new[] { typeof(DomainClient).Assembly }
        });

        public ObjectConverterFactory(IEnumerable<Type> entityTypes)
        {
            HashSet<Type> allTypes = new HashSet<Type>(entityTypes);
            foreach (Type knownType in entityTypes)
                allTypes.UnionWith(Server.KnownTypeUtilities.ImportKnownTypes(knownType, true));

            Dictionary<Type, MessagePackConverter> converters = new(capacity: allTypes.Count);
            foreach (var item in ComputeKnownTypeSet(allTypes))
            {
                var knownTypes = item.Value;

                // Add all discovered trypes
                allTypes.UnionWith(knownTypes);
                knownTypes.Add(item.Key);

                //// Skip mapping with no derived types
                if (knownTypes.Count <= 1)
                    continue;

                // Skip base types, it has KnownType attributes and is handled correctly
                if (item.Key.BaseType == typeof(Entity))
                    continue;

                converters.Add(item.Key, (MessagePackConverter)Activator.CreateInstance(typeof(ObjectConverter<>).MakeGenericType(item.Key), [knownTypes])!);
            }
            converters.Add(typeof(object), new ObjectConverter<object>(allTypes));

            _converters = converters.ToFrozenDictionary();
        }

        MessagePackConverter? IMessagePackConverterFactory.CreateConverter(Type type, ITypeShape? shape, in ConverterContext context)
        {
            // Only return type shapes for specified converter, this allows nerdbank default converters to be generated for other providers
            if (!object.ReferenceEquals(shape?.Provider, ObjectConverterTypeShapeProvider)
                && _converters.TryGetValue(type, out var converter))
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

            // Gather all the explicit known types from attributes.
            // Because we ask to inherit [KnownType], we will collect the full closure
            foreach (Type entityType in types)
            {
                // Get all [KnownType]'s and subselect only those that actually derive from this entity
                IEnumerable<Type> knownTypes = Server.KnownTypeUtilities.ImportKnownTypes(entityType, /* inherit */ true)
                    .Where(t => entityType.IsAssignableFrom(t));
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
