using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

#nullable enable

namespace OpenRiaServices.Client
{
    internal sealed class EntitySetIndexManager
    {
        private readonly PrimaryKeyEntityIndex _primaryKeyIndex = new();

        public void Clear()
        {
            _primaryKeyIndex.Clear();
        }

        public bool TryGetPrimaryEntity(object identity, out Entity? entity)
        {
            return _primaryKeyIndex.TryGetValue(identity, out entity);
        }

        public bool ContainsPrimaryIdentity(object identity)
        {
            return _primaryKeyIndex.Contains(identity);
        }

        public void AddPrimary(Entity entity)
        {
            _primaryKeyIndex.Add(entity);
        }

        public void RemovePrimary(Entity entity)
        {
            _primaryKeyIndex.Remove(entity);
        }

        private sealed class PrimaryKeyEntityIndex
        {
            private readonly Dictionary<object, Entity> _entities = new();

            public void Clear() => _entities.Clear();

            public bool Contains(object identity) => _entities.ContainsKey(identity);

            public bool TryGetValue(object identity, out Entity? entity) => _entities.TryGetValue(identity, out entity);

            public void Add(Entity entity)
            {
                object? identity = entity.GetIdentity();
                if (identity == null)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.EntityKey_NullIdentity, entity));
                }

                if (!_entities.TryAdd(identity, entity))
                {
                    throw new InvalidOperationException(Resource.EntitySet_DuplicateIdentity);
                }
            }

            public void Remove(Entity entity)
            {
                object? identity = entity.GetIdentity();
                if (identity != null && _entities.TryGetValue(identity, out Entity? cachedEntity) && cachedEntity == entity)
                {
                    _entities.Remove(identity);
                    return;
                }

                foreach (KeyValuePair<object, Entity> entry in _entities.ToArray())
                {
                    if (ReferenceEquals(entry.Value, entity))
                    {
                        _entities.Remove(entry.Key);
                        break;
                    }
                }
            }
        }
    }
}
