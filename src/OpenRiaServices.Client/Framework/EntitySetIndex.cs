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
        private readonly Dictionary<AssociationIndexDefinition, AssociationEntityIndexBase> _uniqueAssociationIndexes = new();
        private readonly Dictionary<AssociationIndexDefinition, AssociationEntityIndexBase> _multiValueAssociationIndexes = new();

        public EntitySetIndexManager(EntitySet entitySet)
        {
            _entitySet = entitySet ?? throw new ArgumentNullException(nameof(entitySet));
        }

        public void Clear()
        {
            _primaryKeyIndex.Clear();

            foreach (AssociationEntityIndexBase index in _uniqueAssociationIndexes.Values)
            {
                index.Clear();
            }

            foreach (AssociationEntityIndexBase index in _multiValueAssociationIndexes.Values)
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

            foreach (AssociationEntityIndexBase index in _uniqueAssociationIndexes.Values)
            {
                index.Add(entity);
            }

            foreach (AssociationEntityIndexBase index in _multiValueAssociationIndexes.Values)
            {
                index.Add(entity);
            }
        }

        public void RemoveAssociationEntity(Entity entity)
        {
            foreach (AssociationEntityIndexBase index in _uniqueAssociationIndexes.Values)
            {
                index.Remove(entity);
            }

            foreach (AssociationEntityIndexBase index in _multiValueAssociationIndexes.Values)
            {
                index.Remove(entity);
            }
        }

        public void UpdateAssociationIndexes(Entity entity, string propertyName)
        {
            foreach (AssociationEntityIndexBase index in _uniqueAssociationIndexes.Values)
            {
                index.Update(entity, propertyName);
            }

            foreach (AssociationEntityIndexBase index in _multiValueAssociationIndexes.Values)
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

            AssociationLookupMetadata resolvedMetadata = metadata;
            return resolvedMetadata.TryLookup(GetOrCreateUniqueAssociationIndex(resolvedMetadata.IndexDefinition), sourceEntity, out entities);
        }

        public bool TryGetMultiValueAssociationEntities(EntityAssociationAttribute association, Entity sourceEntity, out IEnumerable<Entity>? entities)
        {
            if (!TryGetAssociationLookupMetadata(association, sourceEntity.MetaType, out AssociationLookupMetadata? metadata))
            {
                entities = null;
                return false;
            }

            AssociationLookupMetadata resolvedMetadata = metadata;
            return resolvedMetadata.TryLookup(GetOrCreateMultiValueAssociationIndex(resolvedMetadata.IndexDefinition), sourceEntity, out entities);
        }

        private AssociationEntityIndexBase GetOrCreateUniqueAssociationIndex(AssociationIndexDefinition definition)
        {
            if (!_uniqueAssociationIndexes.TryGetValue(definition, out AssociationEntityIndexBase? index))
            {
                index = definition.CreateUniqueIndex();
                LoadAssociationIndex(index);
                _uniqueAssociationIndexes.Add(definition, index);
            }

            return index;
        }

        private AssociationEntityIndexBase GetOrCreateMultiValueAssociationIndex(AssociationIndexDefinition definition)
        {
            if (!_multiValueAssociationIndexes.TryGetValue(definition, out AssociationEntityIndexBase? index))
            {
                index = definition.CreateMultiValueIndex();
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

            AssociationIndexDefinition? indexDefinition = GetOrCreateAssociationIndexDefinition(association);
            if (indexDefinition == null)
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

            return indexDefinition.CreateLookupMetadata(sourceMembers);
        }

        private AssociationIndexDefinition? GetOrCreateAssociationIndexDefinition(EntityAssociationAttribute association)
        {
            if (association.OtherKeyMembers.Count == 1)
            {
                string memberName = association.OtherKeyMembers[0];
                if (!_singleMemberAssociationDefinitions.TryGetValue(memberName, out AssociationIndexDefinition? singleDefinition))
                {
                    MetaMember member = MetaType.GetMetaType(_entitySet.EntityType)[memberName];
                    if (member == null)
                    {
                        return null;
                    }

                    singleDefinition = new SingleValueAssociationIndexDefinition(memberName, member);
                    _singleMemberAssociationDefinitions.Add(memberName, singleDefinition);
                }

                return singleDefinition;
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

                compositeDefinition = new CompositeAssociationIndexDefinition(memberNames, members);
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
            public abstract void Clear();

            public abstract void Add(Entity entity);

            public abstract void Remove(Entity entity);

            public abstract void Update(Entity entity, string propertyName);
        }

        private abstract class SingleValueIndex<TKey> : AssociationEntityIndexBase where TKey : notnull
        {
            private readonly string _memberName;
            private readonly MetaMember.ISingleValueAccessor<TKey> _keyAccessor;
            private readonly Dictionary<TKey, List<Entity>> _entitiesByKey = new();
            private readonly Dictionary<Entity, SingleValueIndexKey<TKey>> _keysByEntity = new();
            private List<Entity>? _entitiesWithNullKey;

            protected SingleValueIndex(string memberName, MetaMember.ISingleValueAccessor<TKey> keyAccessor)
            {
                _memberName = memberName;
                _keyAccessor = keyAccessor;
            }

            public override void Clear()
            {
                _entitiesByKey.Clear();
                _keysByEntity.Clear();
                _entitiesWithNullKey = null;
            }

            public override void Add(Entity entity)
            {
                if (!ShouldIndexEntity(entity))
                {
                    return;
                }

                AddOrUpdateEntity(entity);
            }

            public override void Remove(Entity entity)
            {
                if (_keysByEntity.TryGetValue(entity, out SingleValueIndexKey<TKey> key))
                {
                    RemoveFromBucket(key, entity);
                    _keysByEntity.Remove(entity);
                }
            }

            public override void Update(Entity entity, string propertyName)
            {
                if (propertyName != nameof(Entity.EntityState)
                    && !string.Equals(propertyName, _memberName, StringComparison.Ordinal))
                {
                    return;
                }

                if (!ShouldIndexEntity(entity))
                {
                    Remove(entity);
                    return;
                }

                AddOrUpdateEntity(entity);
            }

            public IEnumerable<Entity> Lookup(TKey key)
            {
                if (_entitiesByKey.TryGetValue(key, out List<Entity>? entities))
                {
                    return entities;
                }

                return Array.Empty<Entity>();
            }

            public IEnumerable<Entity> LookupNull()
            {
                if (_entitiesWithNullKey != null)
                {
                    return _entitiesWithNullKey;
                }

                return Array.Empty<Entity>();
            }

            private void AddOrUpdateEntity(Entity entity)
            {
                SingleValueIndexKey<TKey> newKey = CreateKey(entity);
                if (_keysByEntity.TryGetValue(entity, out SingleValueIndexKey<TKey> existingKey))
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

            private SingleValueIndexKey<TKey> CreateKey(Entity entity)
            {
                return _keyAccessor.TryGetValue(entity, out TKey key)
                    ? SingleValueIndexKey<TKey>.Create(key)
                    : SingleValueIndexKey<TKey>.Null;
            }

            private List<Entity> GetOrCreateBucket(SingleValueIndexKey<TKey> key)
            {
                if (!key.HasValue)
                {
                    return _entitiesWithNullKey ??= new List<Entity>();
                }

                if (!_entitiesByKey.TryGetValue(key.Value!, out List<Entity>? entities))
                {
                    entities = new List<Entity>();
                    _entitiesByKey.Add(key.Value!, entities);
                }

                return entities;
            }

            private void RemoveFromBucket(SingleValueIndexKey<TKey> key, Entity entity)
            {
                List<Entity>? entities;
                if (!key.HasValue)
                {
                    entities = _entitiesWithNullKey;
                }
                else if (!_entitiesByKey.TryGetValue(key.Value!, out entities))
                {
                    return;
                }

                if (entities == null)
                {
                    return;
                }

                for (int i = 0; i < entities.Count; i++)
                {
                    if (ReferenceEquals(entities[i], entity))
                    {
                        entities.RemoveAt(i);
                        if (entities.Count == 0)
                        {
                            if (!key.HasValue)
                            {
                                _entitiesWithNullKey = null;
                            }
                            else
                            {
                                _entitiesByKey.Remove(key.Value!);
                            }
                        }

                        return;
                    }
                }
            }
        }

        private sealed class UniqueSingleValueIndex<TKey> : SingleValueIndex<TKey> where TKey : notnull
        {
            public UniqueSingleValueIndex(string memberName, MetaMember.ISingleValueAccessor<TKey> keyAccessor)
                : base(memberName, keyAccessor)
            {
            }
        }

        private sealed class MultiValueSingleValueIndex<TKey> : SingleValueIndex<TKey> where TKey : notnull
        {
            public MultiValueSingleValueIndex(string memberName, MetaMember.ISingleValueAccessor<TKey> keyAccessor)
                : base(memberName, keyAccessor)
            {
            }
        }

        private sealed class ObjectSingleValueIndex : AssociationEntityIndexBase
        {
            private readonly string _memberName;
            private readonly MetaMember _member;
            private readonly Dictionary<object, List<Entity>> _entitiesByKey = new();
            private readonly Dictionary<Entity, ObjectSingleValueIndexKey> _keysByEntity = new();
            private List<Entity>? _entitiesWithNullKey;

            public ObjectSingleValueIndex(string memberName, MetaMember member)
            {
                _memberName = memberName;
                _member = member;
            }

            public override void Clear()
            {
                _entitiesByKey.Clear();
                _keysByEntity.Clear();
                _entitiesWithNullKey = null;
            }

            public override void Add(Entity entity)
            {
                if (!ShouldIndexEntity(entity))
                {
                    return;
                }

                AddOrUpdateEntity(entity);
            }

            public override void Remove(Entity entity)
            {
                if (_keysByEntity.TryGetValue(entity, out ObjectSingleValueIndexKey key))
                {
                    RemoveFromBucket(key, entity);
                    _keysByEntity.Remove(entity);
                }
            }

            public override void Update(Entity entity, string propertyName)
            {
                if (propertyName != nameof(Entity.EntityState)
                    && !string.Equals(propertyName, _memberName, StringComparison.Ordinal))
                {
                    return;
                }

                if (!ShouldIndexEntity(entity))
                {
                    Remove(entity);
                    return;
                }

                AddOrUpdateEntity(entity);
            }

            public IEnumerable<Entity> Lookup(object? key)
            {
                if (key == null)
                {
                    if (_entitiesWithNullKey != null)
                    {
                        return _entitiesWithNullKey;
                    }

                    return Array.Empty<Entity>();
                }

                if (_entitiesByKey.TryGetValue(key, out List<Entity>? entities))
                {
                    return entities;
                }

                return Array.Empty<Entity>();
            }

            private void AddOrUpdateEntity(Entity entity)
            {
                ObjectSingleValueIndexKey newKey = ObjectSingleValueIndexKey.Create(_member.GetValue(entity));
                if (_keysByEntity.TryGetValue(entity, out ObjectSingleValueIndexKey existingKey))
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

            private List<Entity> GetOrCreateBucket(ObjectSingleValueIndexKey key)
            {
                if (!key.HasValue)
                {
                    return _entitiesWithNullKey ??= new List<Entity>();
                }

                if (!_entitiesByKey.TryGetValue(key.Value!, out List<Entity>? entities))
                {
                    entities = new List<Entity>();
                    _entitiesByKey.Add(key.Value!, entities);
                }

                return entities;
            }

            private void RemoveFromBucket(ObjectSingleValueIndexKey key, Entity entity)
            {
                List<Entity>? entities;
                if (!key.HasValue)
                {
                    entities = _entitiesWithNullKey;
                }
                else if (!_entitiesByKey.TryGetValue(key.Value!, out entities))
                {
                    return;
                }

                if (entities == null)
                {
                    return;
                }

                for (int i = 0; i < entities.Count; i++)
                {
                    if (ReferenceEquals(entities[i], entity))
                    {
                        entities.RemoveAt(i);
                        if (entities.Count == 0)
                        {
                            if (!key.HasValue)
                            {
                                _entitiesWithNullKey = null;
                            }
                            else
                            {
                                _entitiesByKey.Remove(key.Value!);
                            }
                        }

                        return;
                    }
                }
            }
        }

        private sealed class CompositeAssociationEntityIndex : AssociationEntityIndexBase
        {
            private readonly CompositeAssociationMemberNames _memberNames;
            private readonly MetaMember[] _members;
            private readonly Dictionary<AssociationIndexKey, List<Entity>> _entitiesByKey = new();
            private readonly Dictionary<Entity, AssociationIndexKey> _keysByEntity = new();

            public CompositeAssociationEntityIndex(CompositeAssociationMemberNames memberNames, MetaMember[] members)
            {
                _memberNames = memberNames;
                _members = members;
            }

            public override void Clear()
            {
                _entitiesByKey.Clear();
                _keysByEntity.Clear();
            }

            public override void Add(Entity entity)
            {
                if (!ShouldIndexEntity(entity))
                {
                    return;
                }

                AddOrUpdateEntity(entity);
            }

            public override void Remove(Entity entity)
            {
                if (_keysByEntity.TryGetValue(entity, out AssociationIndexKey key))
                {
                    RemoveFromBucket(key, entity);
                    _keysByEntity.Remove(entity);
                }
            }

            public override void Update(Entity entity, string propertyName)
            {
                if (propertyName != nameof(Entity.EntityState) && !_memberNames.Contains(propertyName))
                {
                    return;
                }

                if (!ShouldIndexEntity(entity))
                {
                    Remove(entity);
                    return;
                }

                AddOrUpdateEntity(entity);
            }

            public IEnumerable<Entity> Lookup(AssociationIndexKey key)
            {
                if (_entitiesByKey.TryGetValue(key, out List<Entity>? entities))
                {
                    return entities;
                }

                return Array.Empty<Entity>();
            }

            private void AddOrUpdateEntity(Entity entity)
            {
                AssociationIndexKey newKey = CreateKey(entity);
                if (_keysByEntity.TryGetValue(entity, out AssociationIndexKey existingKey))
                {
                    if (existingKey.Equals(newKey))
                    {
                        return;
                    }

                    RemoveFromBucket(existingKey, entity);
                }

                if (!_entitiesByKey.TryGetValue(newKey, out List<Entity>? entities))
                {
                    entities = new List<Entity>();
                    _entitiesByKey.Add(newKey, entities);
                }

                entities.Add(entity);
                _keysByEntity[entity] = newKey;
            }

            private AssociationIndexKey CreateKey(Entity entity)
            {
                object?[] values = new object?[_members.Length];
                for (int i = 0; i < _members.Length; i++)
                {
                    values[i] = _members[i].GetValue(entity);
                }

                return AssociationIndexKey.Create(values);
            }

            private void RemoveFromBucket(AssociationIndexKey key, Entity entity)
            {
                if (!_entitiesByKey.TryGetValue(key, out List<Entity>? entities))
                {
                    return;
                }

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

        private abstract class AssociationIndexDefinition
        {
            public abstract AssociationEntityIndexBase CreateUniqueIndex();

            public abstract AssociationEntityIndexBase CreateMultiValueIndex();

            public abstract AssociationLookupMetadata? CreateLookupMetadata(MetaMember[] sourceMembers);
        }

        private sealed class SingleValueAssociationIndexDefinition : AssociationIndexDefinition
        {
            private readonly string _memberName;
            private readonly SingleValueIndexFactory _factory;
            private readonly int _hashCode;

            public SingleValueAssociationIndexDefinition(string memberName, MetaMember member)
            {
                _memberName = memberName;
                _factory = SingleValueIndexFactory.Create(member);
                _hashCode = StringComparer.Ordinal.GetHashCode(memberName);
            }

            public override AssociationEntityIndexBase CreateUniqueIndex()
            {
                return _factory.CreateUniqueIndex(_memberName);
            }

            public override AssociationEntityIndexBase CreateMultiValueIndex()
            {
                return _factory.CreateMultiValueIndex(_memberName);
            }

            public override AssociationLookupMetadata? CreateLookupMetadata(MetaMember[] sourceMembers)
            {
                if (sourceMembers.Length != 1)
                {
                    return null;
                }

                return _factory.CreateLookupMetadata(this, sourceMembers[0]);
            }

            public override bool Equals(object? obj)
            {
                return obj is SingleValueAssociationIndexDefinition other
                    && string.Equals(_memberName, other._memberName, StringComparison.Ordinal);
            }

            public override int GetHashCode() => _hashCode;
        }

        private sealed class CompositeAssociationIndexDefinition : AssociationIndexDefinition
        {
            private readonly CompositeAssociationMemberNames _memberNames;
            private readonly MetaMember[] _members;

            public CompositeAssociationIndexDefinition(CompositeAssociationMemberNames memberNames, MetaMember[] members)
            {
                _memberNames = memberNames;
                _members = members;
            }

            public override AssociationEntityIndexBase CreateUniqueIndex()
            {
                return new CompositeAssociationEntityIndex(_memberNames, _members);
            }

            public override AssociationEntityIndexBase CreateMultiValueIndex()
            {
                return new CompositeAssociationEntityIndex(_memberNames, _members);
            }

            public override AssociationLookupMetadata? CreateLookupMetadata(MetaMember[] sourceMembers)
            {
                return new CompositeAssociationLookupMetadata(this, sourceMembers);
            }

            public override bool Equals(object? obj)
            {
                return obj is CompositeAssociationIndexDefinition other
                    && _memberNames.Equals(other._memberNames);
            }

            public override int GetHashCode() => _memberNames.GetHashCode();
        }

        private abstract class SingleValueIndexFactory
        {
            public static SingleValueIndexFactory Create(MetaMember member)
            {
                if (member.TryGetSingleValueAccessor(out MetaMember.ISingleValueAccessor? accessor))
                {
                    if (accessor is MetaMember.ISingleValueAccessor<int> intAccessor)
                    {
                        return new TypedSingleValueIndexFactory<int>(intAccessor);
                    }
                    if (accessor is MetaMember.ISingleValueAccessor<long> longAccessor)
                    {
                        return new TypedSingleValueIndexFactory<long>(longAccessor);
                    }
                    if (accessor is MetaMember.ISingleValueAccessor<Guid> guidAccessor)
                    {
                        return new TypedSingleValueIndexFactory<Guid>(guidAccessor);
                    }
                    if (accessor is MetaMember.ISingleValueAccessor<string> stringAccessor)
                    {
                        return new TypedSingleValueIndexFactory<string>(stringAccessor);
                    }
                    if (accessor is MetaMember.ISingleValueAccessor<DateTime> dateTimeAccessor)
                    {
                        return new TypedSingleValueIndexFactory<DateTime>(dateTimeAccessor);
                    }
                }

                return new ObjectSingleValueIndexFactory(member);
            }

            public abstract AssociationEntityIndexBase CreateUniqueIndex(string memberName);

            public abstract AssociationEntityIndexBase CreateMultiValueIndex(string memberName);

            public abstract AssociationLookupMetadata CreateLookupMetadata(AssociationIndexDefinition definition, MetaMember sourceMember);
        }

        private sealed class TypedSingleValueIndexFactory<TKey> : SingleValueIndexFactory where TKey : notnull
        {
            private readonly MetaMember.ISingleValueAccessor<TKey> _targetAccessor;

            public TypedSingleValueIndexFactory(MetaMember.ISingleValueAccessor<TKey> targetAccessor)
            {
                _targetAccessor = targetAccessor;
            }

            public override AssociationEntityIndexBase CreateUniqueIndex(string memberName)
            {
                return new UniqueSingleValueIndex<TKey>(memberName, _targetAccessor);
            }

            public override AssociationEntityIndexBase CreateMultiValueIndex(string memberName)
            {
                return new MultiValueSingleValueIndex<TKey>(memberName, _targetAccessor);
            }

            public override AssociationLookupMetadata CreateLookupMetadata(AssociationIndexDefinition definition, MetaMember sourceMember)
            {
                if (sourceMember.TryGetSingleValueAccessor(out MetaMember.ISingleValueAccessor? accessor)
                    && accessor is MetaMember.ISingleValueAccessor<TKey> typedAccessor)
                {
                    return new TypedSingleValueAssociationLookupMetadata<TKey>(definition, typedAccessor);
                }

                return new ObjectSingleValueAssociationLookupMetadata(definition, sourceMember);
            }
        }

        private sealed class ObjectSingleValueIndexFactory : SingleValueIndexFactory
        {
            private readonly MetaMember _member;

            public ObjectSingleValueIndexFactory(MetaMember member)
            {
                _member = member;
            }

            public override AssociationEntityIndexBase CreateUniqueIndex(string memberName)
            {
                return new ObjectSingleValueIndex(memberName, _member);
            }

            public override AssociationEntityIndexBase CreateMultiValueIndex(string memberName)
            {
                return new ObjectSingleValueIndex(memberName, _member);
            }

            public override AssociationLookupMetadata CreateLookupMetadata(AssociationIndexDefinition definition, MetaMember sourceMember)
            {
                return new ObjectSingleValueAssociationLookupMetadata(definition, sourceMember);
            }
        }

        private abstract class AssociationLookupMetadata
        {
            protected AssociationLookupMetadata(AssociationIndexDefinition indexDefinition)
            {
                IndexDefinition = indexDefinition;
            }

            public AssociationIndexDefinition IndexDefinition { get; }

            public abstract bool TryLookup(AssociationEntityIndexBase index, Entity sourceEntity, out IEnumerable<Entity>? entities);
        }

        private sealed class TypedSingleValueAssociationLookupMetadata<TKey> : AssociationLookupMetadata where TKey : notnull
        {
            private readonly MetaMember.ISingleValueAccessor<TKey> _sourceAccessor;

            public TypedSingleValueAssociationLookupMetadata(AssociationIndexDefinition indexDefinition, MetaMember.ISingleValueAccessor<TKey> sourceAccessor)
                : base(indexDefinition)
            {
                _sourceAccessor = sourceAccessor;
            }

            public override bool TryLookup(AssociationEntityIndexBase index, Entity sourceEntity, out IEnumerable<Entity>? entities)
            {
                SingleValueIndex<TKey> typedIndex = (SingleValueIndex<TKey>)index;
                entities = _sourceAccessor.TryGetValue(sourceEntity, out TKey key)
                    ? typedIndex.Lookup(key)
                    : typedIndex.LookupNull();
                return true;
            }
        }

        private sealed class ObjectSingleValueAssociationLookupMetadata : AssociationLookupMetadata
        {
            private readonly MetaMember _sourceMember;

            public ObjectSingleValueAssociationLookupMetadata(AssociationIndexDefinition indexDefinition, MetaMember sourceMember)
                : base(indexDefinition)
            {
                _sourceMember = sourceMember;
            }

            public override bool TryLookup(AssociationEntityIndexBase index, Entity sourceEntity, out IEnumerable<Entity>? entities)
            {
                entities = ((ObjectSingleValueIndex)index).Lookup(_sourceMember.GetValue(sourceEntity));
                return true;
            }
        }

        private sealed class CompositeAssociationLookupMetadata : AssociationLookupMetadata
        {
            private readonly MetaMember[] _sourceMembers;

            public CompositeAssociationLookupMetadata(AssociationIndexDefinition indexDefinition, MetaMember[] sourceMembers)
                : base(indexDefinition)
            {
                _sourceMembers = sourceMembers;
            }

            public override bool TryLookup(AssociationEntityIndexBase index, Entity sourceEntity, out IEnumerable<Entity>? entities)
            {
                object?[] values = new object?[_sourceMembers.Length];
                for (int i = 0; i < _sourceMembers.Length; i++)
                {
                    values[i] = _sourceMembers[i].GetValue(sourceEntity);
                }

                entities = ((CompositeAssociationEntityIndex)index).Lookup(AssociationIndexKey.Create(values));
                return true;
            }
        }

        private readonly struct SingleValueIndexKey<TKey> : IEquatable<SingleValueIndexKey<TKey>> where TKey : notnull
        {
            private SingleValueIndexKey(TKey? value, bool hasValue)
            {
                Value = value;
                HasValue = hasValue;
            }

            public static SingleValueIndexKey<TKey> Null => default;

            public static SingleValueIndexKey<TKey> Create(TKey value)
            {
                return new SingleValueIndexKey<TKey>(value, true);
            }

            public TKey? Value { get; }

            public bool HasValue { get; }

            public bool Equals(SingleValueIndexKey<TKey> other)
            {
                return HasValue == other.HasValue
                    && (!HasValue || EqualityComparer<TKey>.Default.Equals(Value!, other.Value!));
            }

            public override bool Equals(object? obj) => obj is SingleValueIndexKey<TKey> other && Equals(other);

            public override int GetHashCode()
            {
                return HasValue ? EqualityComparer<TKey>.Default.GetHashCode(Value!) : 0;
            }
        }

        private readonly struct ObjectSingleValueIndexKey : IEquatable<ObjectSingleValueIndexKey>
        {
            private ObjectSingleValueIndexKey(object? value, bool hasValue)
            {
                Value = value;
                HasValue = hasValue;
            }

            public static ObjectSingleValueIndexKey Create(object? value)
            {
                return new ObjectSingleValueIndexKey(value, value != null);
            }

            public object? Value { get; }

            public bool HasValue { get; }

            public bool Equals(ObjectSingleValueIndexKey other)
            {
                return HasValue == other.HasValue
                    && (!HasValue || Equals(Value, other.Value));
            }

            public override bool Equals(object? obj) => obj is ObjectSingleValueIndexKey other && Equals(other);

            public override int GetHashCode()
            {
                return HasValue ? Value!.GetHashCode() : 0;
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
            private readonly object?[] _values;
            private readonly int _hashCode;

            private AssociationIndexKey(object?[] values)
            {
                _values = values;

                HashCode hashCode = new HashCode();
                hashCode.Add(values.Length);
                for (int i = 0; i < values.Length; i++)
                {
                    hashCode.Add(values[i]);
                }

                _hashCode = hashCode.ToHashCode();
            }

            public static AssociationIndexKey Create(object?[] values)
            {
                return new AssociationIndexKey(values);
            }

            public bool Equals(AssociationIndexKey other)
            {
                if (_values.Length != other._values.Length)
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

            public override bool Equals(object? obj) => obj is AssociationIndexKey other && Equals(other);

            public override int GetHashCode() => _hashCode;
        }
    }
}
