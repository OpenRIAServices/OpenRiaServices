using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using OpenRiaServices.Client.Internal;

#nullable enable

namespace OpenRiaServices.Client
{
    internal sealed class EntitySetIndexManager
    {
        private readonly EntitySet _entitySet;
        private readonly PrimaryKeyEntityIndex _primaryKeyIndex = new();
        private readonly Dictionary<AssociationIndexDefinition, UniqueAssociationEntityIndex> _uniqueAssociationIndexes = new();
        private readonly Dictionary<AssociationIndexDefinition, MultiValueAssociationEntityIndex> _multiValueAssociationIndexes = new();

        public EntitySetIndexManager(EntitySet entitySet)
        {
            _entitySet = entitySet ?? throw new ArgumentNullException(nameof(entitySet));
        }

        public void Clear()
        {
            _primaryKeyIndex.Clear();

            foreach (UniqueAssociationEntityIndex index in _uniqueAssociationIndexes.Values)
            {
                index.Clear();
            }

            foreach (MultiValueAssociationEntityIndex index in _multiValueAssociationIndexes.Values)
            {
                index.Clear();
            }
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

        public void AddAssociationEntity(Entity entity)
        {
            if (!ShouldIndexEntity(entity))
            {
                return;
            }

            foreach (UniqueAssociationEntityIndex index in _uniqueAssociationIndexes.Values)
            {
                index.Add(entity);
            }

            foreach (MultiValueAssociationEntityIndex index in _multiValueAssociationIndexes.Values)
            {
                index.Add(entity);
            }
        }

        public void RemoveAssociationEntity(Entity entity)
        {
            foreach (UniqueAssociationEntityIndex index in _uniqueAssociationIndexes.Values)
            {
                index.Remove(entity);
            }

            foreach (MultiValueAssociationEntityIndex index in _multiValueAssociationIndexes.Values)
            {
                index.Remove(entity);
            }
        }

        public void UpdateAssociationIndexes(Entity entity, string propertyName)
        {
            foreach (UniqueAssociationEntityIndex index in _uniqueAssociationIndexes.Values)
            {
                index.Update(entity, propertyName);
            }

            foreach (MultiValueAssociationEntityIndex index in _multiValueAssociationIndexes.Values)
            {
                index.Update(entity, propertyName);
            }
        }

        public bool TryGetUniqueAssociationEntities(EntityAssociationAttribute association, Entity sourceEntity, out IEnumerable<Entity>? entities)
        {
            return TryGetAssociationEntities(GetOrCreateUniqueAssociationIndex(association), sourceEntity, association.ThisKeyMembers, out entities);
        }

        public bool TryGetMultiValueAssociationEntities(EntityAssociationAttribute association, Entity sourceEntity, out IEnumerable<Entity>? entities)
        {
            return TryGetAssociationEntities(GetOrCreateMultiValueAssociationIndex(association), sourceEntity, association.ThisKeyMembers, out entities);
        }

        private bool TryGetAssociationEntities(AssociationEntityIndexBase? index, Entity sourceEntity, IReadOnlyList<string> sourceMemberNames, out IEnumerable<Entity>? entities)
        {
            if (index == null || !TryCreateLookupKey(sourceEntity, sourceMemberNames, out EntitySetIndexKey key))
            {
                entities = null;
                return false;
            }

            entities = index.Lookup(key);
            return true;
        }

        private UniqueAssociationEntityIndex? GetOrCreateUniqueAssociationIndex(EntityAssociationAttribute association)
        {
            if (!TryCreateAssociationIndexDefinition(association, out AssociationIndexDefinition definition))
            {
                return null;
            }

            if (!_uniqueAssociationIndexes.TryGetValue(definition, out UniqueAssociationEntityIndex? index))
            {
                index = new UniqueAssociationEntityIndex(_entitySet, definition);
                LoadAssociationIndex(index);
                _uniqueAssociationIndexes.Add(definition, index);
            }

            return index;
        }

        private MultiValueAssociationEntityIndex? GetOrCreateMultiValueAssociationIndex(EntityAssociationAttribute association)
        {
            if (!TryCreateAssociationIndexDefinition(association, out AssociationIndexDefinition definition))
            {
                return null;
            }

            if (!_multiValueAssociationIndexes.TryGetValue(definition, out MultiValueAssociationEntityIndex? index))
            {
                index = new MultiValueAssociationEntityIndex(_entitySet, definition);
                LoadAssociationIndex(index);
                _multiValueAssociationIndexes.Add(definition, index);
            }

            return index;
        }

        private void LoadAssociationIndex(AssociationEntityIndexBase index)
        {
            foreach (Entity entity in _entitySet.List.Cast<Entity>())
            {
                index.Add(entity);
            }
        }

        private bool TryCreateAssociationIndexDefinition(EntityAssociationAttribute association, out AssociationIndexDefinition definition)
        {
            if (association.ThisKeyMembers.Count != association.OtherKeyMembers.Count || association.OtherKeyMembers.Count == 0)
            {
                definition = default!;
                return false;
            }

            MetaType metaType = MetaType.GetMetaType(_entitySet.EntityType);
            MetaMember[] members = new MetaMember[association.OtherKeyMembers.Count];
            for (int i = 0; i < association.OtherKeyMembers.Count; i++)
            {
                MetaMember member = metaType[association.OtherKeyMembers[i]];
                if (member == null)
                {
                    definition = default!;
                    return false;
                }

                members[i] = member;
            }

            definition = new AssociationIndexDefinition(association.OtherKeyMembers.ToArray(), members);
            return true;
        }

        private static bool TryCreateLookupKey(Entity entity, IReadOnlyList<string> memberNames, out EntitySetIndexKey key)
        {
            MetaType metaType = entity.MetaType;
            object?[] values = new object?[memberNames.Count];
            for (int i = 0; i < memberNames.Count; i++)
            {
                MetaMember member = metaType[memberNames[i]];
                if (member == null)
                {
                    key = default!;
                    return false;
                }

                values[i] = member.GetValue(entity);
            }

            key = new EntitySetIndexKey(values);
            return true;
        }

        private static bool ShouldIndexEntity(Entity entity)
        {
            return entity.EntitySet != null && entity.EntityState != EntityState.New;
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

        private abstract class AssociationEntityIndexBase
        {
            private readonly EntitySet _entitySet;
            private readonly AssociationIndexDefinition _definition;
            private readonly Dictionary<EntitySetIndexKey, List<Entity>> _entitiesByKey = new();

            protected AssociationEntityIndexBase(EntitySet entitySet, AssociationIndexDefinition definition)
            {
                _entitySet = entitySet;
                _definition = definition;
            }

            public void Clear() => _entitiesByKey.Clear();

            public void Add(Entity entity)
            {
                if (!ShouldIndexEntity(entity))
                {
                    return;
                }

                EntitySetIndexKey key = CreateKey(entity);
                if (!_entitiesByKey.TryGetValue(key, out List<Entity>? entities))
                {
                    entities = new List<Entity>();
                    _entitiesByKey.Add(key, entities);
                }

                InsertInEntitySetOrder(entities, entity);
            }

            public void Remove(Entity entity)
            {
                foreach (KeyValuePair<EntitySetIndexKey, List<Entity>> entry in _entitiesByKey.ToArray())
                {
                    Remove(entry.Value, entity);
                    if (entry.Value.Count == 0)
                    {
                        _entitiesByKey.Remove(entry.Key);
                    }
                }
            }

            public void Update(Entity entity, string propertyName)
            {
                if (propertyName != nameof(Entity.EntityState) && !_definition.MemberNames.Contains(propertyName))
                {
                    return;
                }

                Remove(entity);
                Add(entity);
            }

            public IEnumerable<Entity> Lookup(EntitySetIndexKey key)
            {
                if (_entitiesByKey.TryGetValue(key, out List<Entity>? entities))
                {
                    return entities;
                }

                return Enumerable.Empty<Entity>();
            }

            private EntitySetIndexKey CreateKey(Entity entity)
            {
                object?[] values = new object?[_definition.Members.Length];
                for (int i = 0; i < _definition.Members.Length; i++)
                {
                    values[i] = _definition.Members[i].GetValue(entity);
                }

                return new EntitySetIndexKey(values);
            }

            private void InsertInEntitySetOrder(List<Entity> entities, Entity entity)
            {
                if (entities.Any(e => ReferenceEquals(e, entity)))
                {
                    return;
                }

                int entityIndex = _entitySet.List.IndexOf(entity);
                for (int i = 0; i < entities.Count; i++)
                {
                    if (_entitySet.List.IndexOf(entities[i]) > entityIndex)
                    {
                        entities.Insert(i, entity);
                        return;
                    }
                }

                entities.Add(entity);
            }

            private static void Remove(List<Entity> entities, Entity entity)
            {
                for (int i = 0; i < entities.Count; i++)
                {
                    if (ReferenceEquals(entities[i], entity))
                    {
                        entities.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        private sealed class UniqueAssociationEntityIndex : AssociationEntityIndexBase
        {
            public UniqueAssociationEntityIndex(EntitySet entitySet, AssociationIndexDefinition definition)
                : base(entitySet, definition)
            {
            }
        }

        private sealed class MultiValueAssociationEntityIndex : AssociationEntityIndexBase
        {
            public MultiValueAssociationEntityIndex(EntitySet entitySet, AssociationIndexDefinition definition)
                : base(entitySet, definition)
            {
            }
        }

        private sealed class AssociationIndexDefinition : IEquatable<AssociationIndexDefinition>
        {
            private readonly int _hashCode;

            public AssociationIndexDefinition(string[] memberNames, MetaMember[] members)
            {
                MemberNames = memberNames;
                Members = members;

                HashCode hashCode = new HashCode();
                hashCode.Add(memberNames.Length);
                foreach (string memberName in memberNames)
                {
                    hashCode.Add(memberName, StringComparer.Ordinal);
                }

                _hashCode = hashCode.ToHashCode();
            }

            public string[] MemberNames { get; }

            public MetaMember[] Members { get; }

            public bool Equals(AssociationIndexDefinition? other)
            {
                return other != null && MemberNames.SequenceEqual(other.MemberNames, StringComparer.Ordinal);
            }

            public override bool Equals(object? obj) => Equals(obj as AssociationIndexDefinition);

            public override int GetHashCode() => _hashCode;
        }

        private sealed class EntitySetIndexKey : IEquatable<EntitySetIndexKey>
        {
            private readonly object?[] _values;
            private readonly int _hashCode;

            public EntitySetIndexKey(object?[] values)
            {
                _values = values;

                HashCode hashCode = new HashCode();
                hashCode.Add(values.Length);
                foreach (object? value in values)
                {
                    hashCode.Add(value);
                }

                _hashCode = hashCode.ToHashCode();
            }

            public bool Equals(EntitySetIndexKey? other)
            {
                if (other == null || other._values.Length != _values.Length)
                {
                    return false;
                }

                for (int i = 0; i < _values.Length; i++)
                {
                    if (!Equals(_values[i], other._values[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            public override bool Equals(object? obj) => Equals(obj as EntitySetIndexKey);

            public override int GetHashCode() => _hashCode;
        }
    }
}
