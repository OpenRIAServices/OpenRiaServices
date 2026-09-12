using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using OpenRiaServices.Client.Internal;

#nullable enable

namespace OpenRiaServices.Client
{
    internal sealed class EntitySetIndexManager
    {
        private readonly EntitySet _entitySet;
        private readonly PrimaryKeyEntityIndex _primaryKeyIndex = new();
        private readonly Dictionary<EntityAssociationAttribute, AssociationLookupMetadata?> _associationLookupMetadata = new();
        private readonly Dictionary<string, AssociationIndexDefinition> _singleMemberAssociationDefinitions = new(StringComparer.Ordinal);
        private readonly Dictionary<CompositeAssociationMemberNames, AssociationIndexDefinition> _compositeAssociationDefinitions = new();
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
            if (!TryGetAssociationLookupMetadata(association, sourceEntity.MetaType, out AssociationLookupMetadata? metadata))
            {
                entities = null;
                return false;
            }

            return TryGetAssociationEntities(GetOrCreateUniqueAssociationIndex(metadata.IndexDefinition), sourceEntity, metadata, out entities);
        }

        public bool TryGetMultiValueAssociationEntities(EntityAssociationAttribute association, Entity sourceEntity, out IEnumerable<Entity>? entities)
        {
            if (!TryGetAssociationLookupMetadata(association, sourceEntity.MetaType, out AssociationLookupMetadata? metadata))
            {
                entities = null;
                return false;
            }

            return TryGetAssociationEntities(GetOrCreateMultiValueAssociationIndex(metadata.IndexDefinition), sourceEntity, metadata, out entities);
        }

        private static bool TryGetAssociationEntities(AssociationEntityIndexBase index, Entity sourceEntity, AssociationLookupMetadata metadata, out IEnumerable<Entity>? entities)
        {
            if (!metadata.TryCreateLookupKey(sourceEntity, out AssociationIndexKey key))
            {
                entities = null;
                return false;
            }

            entities = index.Lookup(key);
            return true;
        }

        private UniqueAssociationEntityIndex GetOrCreateUniqueAssociationIndex(AssociationIndexDefinition definition)
        {
            if (!_uniqueAssociationIndexes.TryGetValue(definition, out UniqueAssociationEntityIndex? index))
            {
                index = new UniqueAssociationEntityIndex(_entitySet, definition);
                LoadAssociationIndex(index);
                _uniqueAssociationIndexes.Add(definition, index);
            }

            return index;
        }

        private MultiValueAssociationEntityIndex GetOrCreateMultiValueAssociationIndex(AssociationIndexDefinition definition)
        {
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
            for (int i = 0; i < _entitySet.List.Count; i++)
            {
                index.Add((Entity)_entitySet.List[i]!);
            }
        }

        private bool TryGetAssociationLookupMetadata(EntityAssociationAttribute association, MetaType sourceMetaType, out AssociationLookupMetadata? metadata)
        {
            if (_associationLookupMetadata.TryGetValue(association, out metadata))
            {
                return metadata != null;
            }

            metadata = CreateAssociationLookupMetadata(association, sourceMetaType);
            _associationLookupMetadata.Add(association, metadata);
            return metadata != null;
        }

        private AssociationLookupMetadata? CreateAssociationLookupMetadata(EntityAssociationAttribute association, MetaType sourceMetaType)
        {
            if (association.ThisKeyMembers.Count != association.OtherKeyMembers.Count || association.OtherKeyMembers.Count == 0)
            {
                return null;
            }

            MetaMember[] sourceMembers = new MetaMember[association.ThisKeyMembers.Count];
            for (int i = 0; i < association.ThisKeyMembers.Count; i++)
            {
                MetaMember member = sourceMetaType[association.ThisKeyMembers[i]];
                if (member == null)
                {
                    return null;
                }

                sourceMembers[i] = member;
            }

            AssociationIndexDefinition definition = GetOrCreateAssociationIndexDefinition(association);
            return definition == null ? null : new AssociationLookupMetadata(definition, sourceMembers);
        }

        private AssociationIndexDefinition? GetOrCreateAssociationIndexDefinition(EntityAssociationAttribute association)
        {
            if (association.OtherKeyMembers.Count == 1)
            {
                string memberName = association.OtherKeyMembers[0];
                if (!_singleMemberAssociationDefinitions.TryGetValue(memberName, out AssociationIndexDefinition? definition))
                {
                    MetaMember member = MetaType.GetMetaType(_entitySet.EntityType)[memberName];
                    if (member == null)
                    {
                        return null;
                    }

                    definition = new AssociationIndexDefinition(memberName, member);
                    _singleMemberAssociationDefinitions.Add(memberName, definition);
                }

                return definition;
            }

            CompositeAssociationMemberNames memberNames = new CompositeAssociationMemberNames(association.OtherKeyMembers);
            if (!_compositeAssociationDefinitions.TryGetValue(memberNames, out AssociationIndexDefinition? compositeDefinition))
            {
                MetaMember[] members = new MetaMember[association.OtherKeyMembers.Count];
                MetaType metaType = MetaType.GetMetaType(_entitySet.EntityType);
                for (int i = 0; i < association.OtherKeyMembers.Count; i++)
                {
                    MetaMember member = metaType[association.OtherKeyMembers[i]];
                    if (member == null)
                    {
                        return null;
                    }

                    members[i] = member;
                }

                compositeDefinition = new AssociationIndexDefinition(memberNames, members);
                _compositeAssociationDefinitions.Add(memberNames, compositeDefinition);
            }

            return compositeDefinition;
        }

        private static bool ShouldIndexEntity(Entity entity)
        {
            return entity.EntitySet != null && entity.EntityState != EntityState.New;
        }

        private sealed class PrimaryKeyEntityIndex
        {
            private readonly Dictionary<object, Entity> _entities = new();
            private readonly Dictionary<Entity, object> _identitiesByEntity = new();

            public void Clear()
            {
                _entities.Clear();
                _identitiesByEntity.Clear();
            }

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

        private abstract class AssociationEntityIndexBase
        {
            private readonly AssociationIndexDefinition _definition;
            private readonly Dictionary<AssociationIndexKey, List<Entity>> _entitiesByKey = new();
            private readonly Dictionary<Entity, AssociationIndexKey> _keysByEntity = new();

            protected AssociationEntityIndexBase(EntitySet entitySet, AssociationIndexDefinition definition)
            {
                _definition = definition;
            }

            public void Clear()
            {
                _entitiesByKey.Clear();
                _keysByEntity.Clear();
            }

            public void Add(Entity entity)
            {
                if (!ShouldIndexEntity(entity))
                {
                    return;
                }

                AssociationIndexKey key = _definition.CreateKey(entity);
                if (_keysByEntity.TryGetValue(entity, out AssociationIndexKey existingKey))
                {
                    if (existingKey.Equals(key))
                    {
                        return;
                    }

                    RemoveFromBucket(existingKey, entity);
                }

                GetOrCreateBucket(key).Add(entity);
                _keysByEntity[entity] = key;
            }

            public void Remove(Entity entity)
            {
                if (_keysByEntity.TryGetValue(entity, out AssociationIndexKey key))
                {
                    RemoveFromBucket(key, entity);
                    _keysByEntity.Remove(entity);
                }
            }

            public void Update(Entity entity, string propertyName)
            {
                if (propertyName != nameof(Entity.EntityState) && !_definition.ContainsMemberName(propertyName))
                {
                    return;
                }

                if (!ShouldIndexEntity(entity))
                {
                    Remove(entity);
                    return;
                }

                AssociationIndexKey newKey = _definition.CreateKey(entity);
                if (_keysByEntity.TryGetValue(entity, out AssociationIndexKey existingKey))
                {
                    if (existingKey.Equals(newKey))
                    {
                        return;
                    }

                    RemoveFromBucket(existingKey, entity);
                }

                GetOrCreateBucket(newKey).Add(entity);
                _keysByEntity[entity] = newKey;
            }

            public IEnumerable<Entity> Lookup(AssociationIndexKey key)
            {
                if (_entitiesByKey.TryGetValue(key, out List<Entity>? entities))
                {
                    return entities;
                }

                return Array.Empty<Entity>();
            }

            private List<Entity> GetOrCreateBucket(AssociationIndexKey key)
            {
                if (!_entitiesByKey.TryGetValue(key, out List<Entity>? entities))
                {
                    entities = new List<Entity>();
                    _entitiesByKey.Add(key, entities);
                }

                return entities;
            }

            private void RemoveFromBucket(AssociationIndexKey key, Entity entity)
            {
                if (_entitiesByKey.TryGetValue(key, out List<Entity>? entities))
                {
                    for (int i = 0; i < entities.Count; i++)
                    {
                        if (ReferenceEquals(entities[i], entity))
                        {
                            entities.RemoveAt(i);
                            if (entities.Count == 0)
                            {
                                _entitiesByKey.Remove(key);
                            }

                            return;
                        }
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
            private readonly CompositeAssociationMemberNames? _compositeMemberNames;

            public AssociationIndexDefinition(string memberName, MetaMember member)
            {
                SingleMemberName = memberName;
                SingleMember = member;
                _hashCode = StringComparer.Ordinal.GetHashCode(memberName);
            }

            public AssociationIndexDefinition(CompositeAssociationMemberNames memberNames, MetaMember[] members)
            {
                _compositeMemberNames = memberNames;
                Members = members;
                _hashCode = memberNames.GetHashCode();
            }

            public string? SingleMemberName { get; }

            public MetaMember? SingleMember { get; }

            public MetaMember[]? Members { get; }

            public bool IsSingleMember => SingleMember != null;

            public bool ContainsMemberName(string propertyName)
            {
                if (SingleMemberName != null)
                {
                    return string.Equals(SingleMemberName, propertyName, StringComparison.Ordinal);
                }

                return _compositeMemberNames != null && _compositeMemberNames.Contains(propertyName);
            }

            public AssociationIndexKey CreateKey(Entity entity)
            {
                if (SingleMember != null)
                {
                    return AssociationIndexKey.Create(SingleMember.GetValue(entity));
                }

                MetaMember[] members = Members!;
                object?[] values = new object?[members.Length];
                for (int i = 0; i < members.Length; i++)
                {
                    values[i] = members[i].GetValue(entity);
                }

                return AssociationIndexKey.Create(values);
            }

            public bool Equals(AssociationIndexDefinition? other)
            {
                if (other == null || IsSingleMember != other.IsSingleMember)
                {
                    return false;
                }

                if (SingleMemberName != null)
                {
                    return string.Equals(SingleMemberName, other.SingleMemberName, StringComparison.Ordinal);
                }

                return _compositeMemberNames != null && _compositeMemberNames.Equals(other._compositeMemberNames);
            }

            public override bool Equals(object? obj) => Equals(obj as AssociationIndexDefinition);

            public override int GetHashCode() => _hashCode;
        }

        private sealed class AssociationLookupMetadata
        {
            private readonly MetaMember[]? _compositeSourceMembers;
            private readonly MetaMember? _singleSourceMember;

            public AssociationLookupMetadata(AssociationIndexDefinition indexDefinition, MetaMember[] sourceMembers)
            {
                IndexDefinition = indexDefinition;
                if (sourceMembers.Length == 1)
                {
                    _singleSourceMember = sourceMembers[0];
                }
                else
                {
                    _compositeSourceMembers = sourceMembers;
                }
            }

            public AssociationIndexDefinition IndexDefinition { get; }

            public bool TryCreateLookupKey(Entity entity, out AssociationIndexKey key)
            {
                if (_singleSourceMember != null)
                {
                    key = AssociationIndexKey.Create(_singleSourceMember.GetValue(entity));
                    return true;
                }

                MetaMember[] sourceMembers = _compositeSourceMembers!;
                object?[] values = new object?[sourceMembers.Length];
                for (int i = 0; i < sourceMembers.Length; i++)
                {
                    values[i] = sourceMembers[i].GetValue(entity);
                }

                key = AssociationIndexKey.Create(values);
                return true;
            }
        }

        private sealed class CompositeAssociationMemberNames : IEquatable<CompositeAssociationMemberNames>
        {
            private readonly string[] _memberNames;
            private readonly int _hashCode;

            public CompositeAssociationMemberNames(IReadOnlyList<string> memberNames)
            {
                _memberNames = new string[memberNames.Count];

                HashCode hashCode = new HashCode();
                hashCode.Add(memberNames.Count);
                for (int i = 0; i < memberNames.Count; i++)
                {
                    string memberName = memberNames[i];
                    _memberNames[i] = memberName;
                    hashCode.Add(memberName, StringComparer.Ordinal);
                }

                _hashCode = hashCode.ToHashCode();
            }

            public bool Contains(string propertyName)
            {
                for (int i = 0; i < _memberNames.Length; i++)
                {
                    if (string.Equals(_memberNames[i], propertyName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                return false;
            }

            public bool Equals(CompositeAssociationMemberNames? other)
            {
                if (other == null || other._memberNames.Length != _memberNames.Length)
                {
                    return false;
                }

                for (int i = 0; i < _memberNames.Length; i++)
                {
                    if (!string.Equals(_memberNames[i], other._memberNames[i], StringComparison.Ordinal))
                    {
                        return false;
                    }
                }

                return true;
            }

            public override bool Equals(object? obj) => Equals(obj as CompositeAssociationMemberNames);

            public override int GetHashCode() => _hashCode;
        }

        private readonly struct AssociationIndexKey : IEquatable<AssociationIndexKey>
        {
            private readonly object? _singleValue;
            private readonly object?[]? _compositeValues;
            private readonly int _hashCode;

            private AssociationIndexKey(object? singleValue, object?[]? compositeValues, int hashCode)
            {
                _singleValue = singleValue;
                _compositeValues = compositeValues;
                _hashCode = hashCode;
            }

            public static AssociationIndexKey Create(object? value)
            {
                return new AssociationIndexKey(value, null, HashCode.Combine(1, value));
            }

            public static AssociationIndexKey Create(object?[] values)
            {
                HashCode hashCode = new HashCode();
                hashCode.Add(values.Length);
                for (int i = 0; i < values.Length; i++)
                {
                    hashCode.Add(values[i]);
                }

                return new AssociationIndexKey(null, values, hashCode.ToHashCode());
            }

            public bool Equals(AssociationIndexKey other)
            {
                if (_compositeValues == null || other._compositeValues == null)
                {
                    return _compositeValues == other._compositeValues && Equals(_singleValue, other._singleValue);
                }

                if (_compositeValues.Length != other._compositeValues.Length)
                {
                    return false;
                }

                for (int i = 0; i < _compositeValues.Length; i++)
                {
                    if (!Equals(_compositeValues[i], other._compositeValues[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            public override bool Equals(object? obj) => obj is AssociationIndexKey other && Equals(other);

            public override int GetHashCode() => _hashCode;
        }
    }
}
