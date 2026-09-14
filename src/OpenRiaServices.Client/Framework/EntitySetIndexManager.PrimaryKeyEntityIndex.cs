using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

#nullable enable

namespace OpenRiaServices.Client
{
internal sealed partial class EntitySetIndexManager
    {
        /// <summary>
        /// Maintains bidirectional identity mappings so entities can be removed even when their key has subsequently changed.
        /// </summary>
        private sealed class PrimaryKeyEntityIndex
        {
            private readonly Dictionary<object, Entity> _entities = new(EqualityComparer<object>.Default);
            private readonly Dictionary<Entity, object> _identitiesByEntity = new(ReferenceEqualityComparer<Entity>.Instance);

            public void Clear()
            {
                _entities.Clear();
                _identitiesByEntity.Clear();
            }

            public bool Contains(object identity) => _entities.ContainsKey(identity);

            public bool TryGetValue(object identity, [NotNullWhen(true)] out Entity? entity) => _entities.TryGetValue(identity, out entity);

            public void Add(Entity entity)
            {
                object? identity = entity.GetIdentity();
                if (identity == null)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resource.EntityKey_NullIdentity, entity));
                }

                if (!_entities.TryAdd(identity, entity))
                {
                    throw new InvalidOperationException(Resource.EntitySet_DuplicateIdentity);
                }

                _identitiesByEntity[entity] = identity;
            }

            public void Remove(Entity entity)
            {
                if (_identitiesByEntity.TryGetValue(entity, out object? identity))
                {
                    _entities.Remove(identity);
                    _identitiesByEntity.Remove(entity);
                }
            }
        }
    }
}
