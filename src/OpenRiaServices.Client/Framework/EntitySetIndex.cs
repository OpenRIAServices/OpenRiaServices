using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Globalization;
using OpenRiaServices.Client.Internal;

#nullable enable

namespace OpenRiaServices.Client
{
    /// <summary>
    /// Maintains identity and association indexes so entity relationship resolution does not require repeatedly scanning an <see cref="EntitySet"/>.
    /// </summary>
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

            return metadata.TryLookup(GetOrCreateUniqueAssociationIndex(metadata.IndexDefinition), sourceEntity, out entities);
        }

        public bool TryGetMultiValueAssociationEntities(EntityAssociationAttribute association, Entity sourceEntity, out IEnumerable<Entity>? entities)
        {
            if (!TryGetAssociationLookupMetadata(association, sourceEntity.MetaType, out AssociationLookupMetadata? metadata))
            {
                entities = null;
                return false;
            }

            return metadata.TryLookup(GetOrCreateMultiValueAssociationIndex(metadata.IndexDefinition), sourceEntity, out entities);
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
            foreach (Entity entity in _entitySet.List)
                index.Add(entity);
        }

        private bool TryGetAssociationLookupMetadata(EntityAssociationAttribute association, MetaType sourceMetaType, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out AssociationLookupMetadata? metadata)
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

        /// <summary>
        /// Maintains bidirectional identity mappings so entities can be removed even when their key has subsequently changed.
        /// </summary>
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

        /// <summary>
        /// Defines the lifecycle operations shared by lazily-created association indexes.
        /// </summary>
        private abstract class AssociationEntityIndexBase
        {
            /// <summary>
            /// Removes all cached relationship mappings when the owning entity set is reset.
            /// </summary>
            public abstract void Clear();

            /// <summary>
            /// Adds an entity to this index when its current state can participate in association resolution.
            /// </summary>
            /// <param name="entity">The entity to index.</param>
            public abstract void Add(Entity entity);

            /// <summary>
            /// Removes an entity's cached relationship mapping when it no longer belongs in this index.
            /// </summary>
            /// <param name="entity">The entity to remove.</param>
            public abstract void Remove(Entity entity);

            /// <summary>
            /// Reconciles an entity's mapping after a property or state change, avoiding a full index rebuild.
            /// </summary>
            /// <param name="entity">The changed entity.</param>
            /// <param name="propertyName">The name of the changed property.</param>
            public abstract void Update(Entity entity, string propertyName);
        }

        /// <summary>
        /// Indexes entities by one association member while retaining each entity's previous key to support incremental updates.
        /// </summary>
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

                int index = entities.IndexOf(entity);
                if (index >= 0)
                {
                    entities.RemoveAt(index);
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
                }
            }
        }

        /// <summary>
        /// Represents the index used by <see cref="EntityRef{TEntity}"/> lookups, where an association resolves from a source entity to its single related entity.
        /// </summary>
        private sealed class UniqueSingleValueIndex<TKey> : SingleValueIndex<TKey> where TKey : notnull
        {
            public UniqueSingleValueIndex(string memberName, MetaMember.ISingleValueAccessor<TKey> keyAccessor)
                : base(memberName, keyAccessor)
            {
            }
        }

        /// <summary>
        /// Represents the index used by <see cref="EntityCollection{TEntity}"/> lookups, where an association resolves from a source entity to all matching related entities.
        /// </summary>
        private sealed class MultiValueSingleValueIndex<TKey> : SingleValueIndex<TKey> where TKey : notnull
        {
            public MultiValueSingleValueIndex(string memberName, MetaMember.ISingleValueAccessor<TKey> keyAccessor)
                : base(memberName, keyAccessor)
            {
            }
        }

        /// <summary>
        /// Indexes entities by an ordered set of association members when no single member uniquely identifies the relationship.
        /// </summary>
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

                int index = entities.IndexOf(entity);
                if (index >= 0)
                {
                    entities.RemoveAt(index);
                    if (entities.Count == 0)
                    {
                        _entitiesByKey.Remove(key);
                    }
                }
            }
        }

        /// <summary>
        /// Describes how to construct a reusable association index and its compatible source-side lookup metadata.
        /// </summary>
        private abstract class AssociationIndexDefinition
        {
            /// <summary>
            /// Creates an index dedicated to a single-related-entity association, keeping it separate from collection association caches.
            /// </summary>
            /// <returns>An index for a unique association lookup.</returns>
            public abstract AssociationEntityIndexBase CreateUniqueIndex();

            /// <summary>
            /// Creates an index dedicated to a collection association, allowing its lifecycle to remain independent of unique association caches.
            /// </summary>
            /// <returns>An index for a multi-value association lookup.</returns>
            public abstract AssociationEntityIndexBase CreateMultiValueIndex();

            /// <summary>
            /// Creates the source-side key adapter when the association members can be matched to this index definition.
            /// </summary>
            /// <param name="sourceMembers">The source members that provide lookup key values.</param>
            /// <returns>The compatible lookup metadata, or <see langword="null"/> when the member shapes do not match.</returns>
            public abstract AssociationLookupMetadata? CreateLookupMetadata(MetaMember[] sourceMembers);
        }

        /// <summary>
        /// Describes an association index keyed by one target member, allowing equivalent associations to share an index.
        /// </summary>
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

        /// <summary>
        /// Describes an association index keyed by an ordered group of target members.
        /// </summary>
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

        /// <summary>
        /// Selects typed index implementations where possible to avoid object-based key handling during relationship lookup.
        /// </summary>
        private abstract class SingleValueIndexFactory
        {
            /// <summary>
            /// Chooses a specialized factory for supported member types, falling back to object keys to support all other association members.
            /// </summary>
            /// <param name="member">The target association member to index.</param>
            /// <returns>A factory compatible with <paramref name="member"/>.</returns>
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

                    if (accessor.GetType().GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MetaMember.ISingleValueAccessor<>))
                            is Type genericAccessorInterface)
                    {
                        Type indexType = typeof(TypedSingleValueIndexFactory<>).MakeGenericType(genericAccessorInterface.GetGenericArguments()[0]);
                        return (SingleValueIndexFactory)Activator.CreateInstance(indexType, accessor)!;
                    }
                }

                return new TypedSingleValueIndexFactory<object>(member.GetObjectSingleValueAccessor());
            }

            /// <summary>
            /// Creates the typed representation used for a single-related-entity association.
            /// </summary>
            /// <param name="memberName">The indexed target member name.</param>
            /// <returns>An index for unique association lookup.</returns>
            public abstract AssociationEntityIndexBase CreateUniqueIndex(string memberName);

            /// <summary>
            /// Creates the typed representation used for a collection association.
            /// </summary>
            /// <param name="memberName">The indexed target member name.</param>
            /// <returns>An index for multi-value association lookup.</returns>
            public abstract AssociationEntityIndexBase CreateMultiValueIndex(string memberName);

            /// <summary>
            /// Creates a lookup adapter that reads a source key in the most efficient compatible form.
            /// </summary>
            /// <param name="definition">The index definition the adapter will query.</param>
            /// <param name="sourceMember">The source member that provides the lookup key.</param>
            /// <returns>Metadata that can query the associated index.</returns>
            public abstract AssociationLookupMetadata CreateLookupMetadata(AssociationIndexDefinition definition, MetaMember sourceMember);
        }

        /// <summary>
        /// Creates type-preserving indexes and lookups for a supported association key type.
        /// </summary>
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

                return new TypedSingleValueAssociationLookupMetadata<object>(definition, sourceMember.GetObjectSingleValueAccessor());
            }
        }

        /// <summary>
        /// Bridges source association members to a compatible target index so lookup code remains independent of key shape.
        /// </summary>
        private abstract class AssociationLookupMetadata
        {
            protected AssociationLookupMetadata(AssociationIndexDefinition indexDefinition)
            {
                IndexDefinition = indexDefinition;
            }

            /// <summary>
            /// Gets the index definition this metadata can query.
            /// </summary>
            public AssociationIndexDefinition IndexDefinition { get; }

            /// <summary>
            /// Uses the source entity's association key to query a compatible index without exposing its key representation to callers.
            /// </summary>
            /// <param name="index">The association index to query.</param>
            /// <param name="sourceEntity">The entity that provides the lookup key.</param>
            /// <param name="entities">The matching entities when the lookup is supported.</param>
            /// <returns><see langword="true"/> when this metadata supports the supplied index; otherwise, <see langword="false"/>.</returns>
            public abstract bool TryLookup(AssociationEntityIndexBase index, Entity sourceEntity, out IEnumerable<Entity>? entities);
        }

        /// <summary>
        /// Performs a type-preserving source-key lookup against a single-member association index.
        /// </summary>
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

        /// <summary>
        /// Builds ordered source keys for lookup against a composite association index.
        /// </summary>
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

        /// <summary>
        /// Distinguishes a null association key from a populated typed key without requiring nullable dictionary keys.
        /// </summary>
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

        /// <summary>
        /// Preserves member order when identifying composite index definitions, because key order determines association compatibility.
        /// </summary>
        private readonly struct CompositeAssociationMemberNames : IEquatable<CompositeAssociationMemberNames>
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

            public bool Equals(CompositeAssociationMemberNames other)
            {
                if (other._memberNames.Length != _memberNames.Length)
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

            public override bool Equals(object? obj) => obj is CompositeAssociationMemberNames other && Equals(other);

            public override int GetHashCode() => _hashCode;
        }

        /// <summary>
        /// Represents an ordered composite association key for dictionary indexing.
        /// </summary>
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
