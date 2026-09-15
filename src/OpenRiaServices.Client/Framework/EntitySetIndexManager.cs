using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using OpenRiaServices.Client.Internal;
using static OpenRiaServices.Client.Internal.MetaMember;

#nullable enable

namespace OpenRiaServices.Client
{
    /// <summary>
    /// Maintains identity and association indexes so entity relationship resolution does not require repeatedly scanning an <see cref="EntitySet"/>.
    /// </summary>
    internal sealed partial class EntitySetIndexManager
    {
        private readonly EntitySet _entitySet;
        private readonly IdentityKeyIndex _primaryKeyIndex;
        private readonly Dictionary<CompositeAssociationMemberNames, EntityIndex> _associationIndexes = new();
        private readonly List<EntityAssociationIndex> _secondaryIndexes = new();

        public EntitySetIndexManager(EntitySet entitySet)
        {
            _entitySet = entitySet ?? throw new ArgumentNullException(nameof(entitySet));
            MetaType metaType = MetaType.GetMetaType(entitySet.EntityType);
            _primaryKeyIndex = IdentityKeyIndex.Create(metaType.KeyMembers);
            // Allow primary key to be used as index for association properties
            _associationIndexes.Add(
                new CompositeAssociationMemberNames(metaType.KeyMembers.Select(static member => member.Name).ToArray()),
                _primaryKeyIndex);
        }

        public void Clear()
        {
            _primaryKeyIndex.Clear();

            foreach (EntityAssociationIndex index in _secondaryIndexes)
            {
                index.Clear();
            }
        }

        /// <summary>
        /// Lookup an entity by identity. Returns <see langword="true"/> if the entity was found; otherwise, <see langword="false"/>.
        /// </summary>
        public bool TryGetByPrimary(object identity, [NotNullWhen(true)] out Entity? entity)
        {
            return _primaryKeyIndex.TryGetValue(identity, out entity);
        }

        /// <summary>
        /// Lookup currently tracked entity (if any) with the same identity as the provided entity
        /// </summary>
        public bool TryGetByPrimary(Entity identity, bool throwOnNull, [NotNullWhen(true)] out Entity? entity)
        {
            return _primaryKeyIndex.TryGetByIdentity(identity, throwOnNull, out entity);
        }

        /// <summary>
        /// Add entity to primary index, throwing if an entity with the same identity is already tracked.
        /// </summary>
        public void AddPrimary(Entity entity)
        {
            _primaryKeyIndex.Add(entity);
        }

        /// <summary>
        /// Removes an entity from primary index, does nothing if the entity does not already exist.
        /// </summary>
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

            foreach (EntityAssociationIndex index in _secondaryIndexes)
            {
                index.Add(entity);
            }
        }

        public void RemoveAssociationEntity(Entity entity)
        {
            foreach (EntityAssociationIndex index in _secondaryIndexes)
            {
                index.Remove(entity);
            }
        }

        public void UpdateAssociationIndexes(Entity entity, string propertyName)
        {
            foreach (EntityAssociationIndex index in _secondaryIndexes)
            {
                index.Update(entity, propertyName);
            }
        }

        public bool TryGetAssociationEntities(EntityAssociationAttribute association, Entity sourceEntity, out IEnumerable<Entity>? entities)
        {
            if (!TryGetAssociationIndex(association, out EntityIndex? index))
            {
                entities = null;
                return false;
            }

            return index.TryLookup(association, sourceEntity, out entities);
        }

        private void LoadAssociationIndex(EntityAssociationIndex index)
        {
            foreach (Entity entity in _entitySet.List)
                index.Add(entity);
        }

        private bool TryGetAssociationIndex(EntityAssociationAttribute association, [NotNullWhen(true)] out EntityIndex? index)
        {
            if (association.ThisKeyMembers.Count != association.OtherKeyMembers.Count)
            {
                index = null;
                return false;
            }

            CompositeAssociationMemberNames memberNames = new(association.OtherKeyMembers);
            if (_associationIndexes.TryGetValue(memberNames, out index))
            {
                return true;
            }

            MetaType metaType = MetaType.GetMetaType(_entitySet.EntityType);
            var keyMembers = association.OtherKeyMembers;
            if (keyMembers.Count == 1)
            {
                string memberName = keyMembers[0];
                MetaMember member = metaType[memberName];
                if (member == null)
                {
                    index = null;
                    return false;
                }

                index = SingleValueIndex.Create(member);
            }
            else
            {
                MetaMember[] members = new MetaMember[keyMembers.Count];
                for (int i = 0; i < keyMembers.Count; i++)
                {
                    MetaMember member = metaType[keyMembers[i]];
                    if (member == null)
                    {
                        index = null;
                        return false;
                    }

                    members[i] = member;
                }

                index = new CompositeAssociationEntityIndex(memberNames, members);
            }

            EntityAssociationIndex secondaryIndex = (EntityAssociationIndex)index;
            LoadAssociationIndex(secondaryIndex);
            _secondaryIndexes.Add(secondaryIndex);
            _associationIndexes.Add(memberNames, index);
            return true;
        }

        private static bool ShouldIndexEntity(Entity entity)
        {
            return entity.EntitySet != null && entity.EntityState != EntityState.New;
        }

        /// <summary>
        /// Defines the lifecycle operations shared by lazily-created association indexes.
        /// </summary>
        private abstract class EntityIndex
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
            /// Uses the source entity's association key to query a compatible index.
            /// </summary>
            /// <param name="association">The association that identifies the source members.</param>
            /// <param name="sourceEntity">The entity that provides the lookup key.</param>
            /// <param name="entities">The matching entities when the lookup is supported.</param>
            /// <returns><see langword="true"/> when the association can be queried; otherwise, <see langword="false"/>.</returns>
            public abstract bool TryLookup(EntityAssociationAttribute association, Entity sourceEntity, [NotNullWhen(true)] out IEnumerable<Entity>? entities);
        }

        private abstract class EntityAssociationIndex : EntityIndex
        {
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
        private sealed class SingleValueIndex<TKey> : EntityAssociationIndex where TKey : notnull
        {
            private readonly string _memberName;
            private readonly MetaMember.IValueAccessor<TKey> _keyAccessor;
            private readonly Dictionary<TKey, List<Entity>> _entitiesByKey = new(EqualityComparer<TKey>.Default);
            private readonly Dictionary<Entity, SingleValueIndexKey<TKey>> _keysByEntity = new(ReferenceEqualityComparer<Entity>.Instance);
            private List<Entity>? _entitiesWithNullKey;

            public SingleValueIndex(string memberName, MetaMember.IValueAccessor<TKey> keyAccessor)
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

            public override bool TryLookup(EntityAssociationAttribute association, Entity sourceEntity, [NotNullWhen(true)] out IEnumerable<Entity>? entities)
            {
                var thisKeyMembes = association.ThisKeyMembers;

                if (thisKeyMembes.Count == 1
                    && sourceEntity.MetaType[thisKeyMembes[0]] is MetaMember sourceMember
                    && sourceMember.GetValueAccessor() is IValueAccessor<TKey> typedAccessor)
                {
                    entities = typedAccessor.TryGetValue(sourceEntity, out TKey key)
                        ? Lookup(key)
                        : LookupNull();
                    return true;
                }

                // Types does not match, so we cannot perform the lookup. This is a programming error, so throw an exception.
                entities = null;
                return false;
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
        /// Indexes entities by an ordered set of association members when no single member uniquely identifies the relationship.
        /// </summary>
        private sealed class CompositeAssociationEntityIndex : EntityAssociationIndex
        {
            private readonly CompositeAssociationMemberNames _targetMemberNames;
            private readonly MetaMember[] _targetMembers;
            private readonly Dictionary<AssociationIndexKey, List<Entity>> _entitiesByKey = new(EqualityComparer<AssociationIndexKey>.Default);
            private readonly Dictionary<Entity, AssociationIndexKey> _keysByEntity = new();

            public CompositeAssociationEntityIndex(CompositeAssociationMemberNames memberNames, MetaMember[] members)
            {
                _targetMemberNames = memberNames;
                _targetMembers = members;
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
                if (propertyName != nameof(Entity.EntityState) && !_targetMemberNames.Contains(propertyName))
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
                object?[] values = new object?[_targetMembers.Length];
                for (int i = 0; i < _targetMembers.Length; i++)
                {
                    values[i] = _targetMembers[i].GetValue(entity);
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

            public override bool TryLookup(EntityAssociationAttribute association, Entity sourceEntity, [NotNullWhen(true)] out IEnumerable<Entity>? entities)
            {
                object?[] values = new object?[_targetMembers.Length];
                for (int i = 0; i < _targetMembers.Length; i++)
                {
                    MetaMember sourceMember = sourceEntity.MetaType[association.ThisKeyMembers[i]];
                    if (sourceMember == null)
                    {
                        entities = null;
                        return false;
                    }

                    values[i] = sourceMember.GetValue(sourceEntity);
                }

                entities = Lookup(AssociationIndexKey.Create(values));
                return true;
            }
        }

        /// <summary>
        /// Selects typed index implementations where possible to avoid object-based key handling during relationship lookup.
        /// </summary>
        private static class SingleValueIndex
        {
            /// <summary>
            /// Chooses a specialized factory for supported member types, falling back to object keys to support all other association members.
            /// </summary>
            /// <param name="member">The target association member to index.</param>
            /// <returns>A factory compatible with <paramref name="member"/>.</returns>
            public static EntityAssociationIndex Create(MetaMember member)
            {
                var accessor = member.GetValueAccessor();

                switch (accessor)
                {
                    case IValueAccessor<int> intAccessor:
                        return new SingleValueIndex<int>(member.Name, intAccessor);
                    case IValueAccessor<long> longAccessor:
                        return new SingleValueIndex<long>(member.Name, longAccessor);
                    case IValueAccessor<Guid> guidAccessor:
                        return new SingleValueIndex<Guid>(member.Name, guidAccessor);
                    case IValueAccessor<string> stringAccessor:
                        return new SingleValueIndex<string>(member.Name, stringAccessor);
                    case IValueAccessor<DateTime> dateTimeAccessor:
                        return new SingleValueIndex<DateTime>(member.Name, dateTimeAccessor);
                }

                if (accessor.GetType().GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MetaMember.IValueAccessor<>))
                        is Type genericAccessorInterface)
                {
                    Type indexType = typeof(SingleValueIndex<>).MakeGenericType(genericAccessorInterface.GetGenericArguments()[0]);
                    return (EntityAssociationIndex)Activator.CreateInstance(indexType, [member.Name, accessor])!;
                }

                return new SingleValueIndex<object>(member.Name, member.GetObjectValueAccessor());
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
            private readonly IReadOnlyList<string> _memberNames;
            private readonly int _hashCode;

            public CompositeAssociationMemberNames(IReadOnlyList<string> memberNames)
            {
                _memberNames = memberNames;

                HashCode hashCode = new HashCode();
                for (int i = 0; i < memberNames.Count; i++)
                {
                    hashCode.Add(memberNames[i], StringComparer.Ordinal);
                }

                _hashCode = hashCode.ToHashCode();
            }

            public bool Contains(string propertyName)
            {
                for (int i = 0; i < _memberNames.Count; i++)
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
                if (other._memberNames.Count != _memberNames.Count)
                {
                    return false;
                }

                for (int i = 0; i < _memberNames.Count; i++)
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

        private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
        {
            public static ReferenceEqualityComparer<T> Instance { get; } = new();

            private ReferenceEqualityComparer()
            {
            }

            public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

            // Note: RuntimeHelpers.GetHashCode returns a hash code based on the object reference, not the object's contents. This is important for reference equality comparisons.
            public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj!);
        }
    }
}
