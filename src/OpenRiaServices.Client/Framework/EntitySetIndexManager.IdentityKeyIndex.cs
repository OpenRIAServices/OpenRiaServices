using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using OpenRiaServices.Client.Internal;

#nullable enable

namespace OpenRiaServices.Client
{
    internal sealed partial class EntitySetIndexManager
    {
        /// <summary>
        /// Maintains bidirectional identity mappings so entities can be removed even when their key has subsequently changed.
        /// </summary>
        private abstract class IdentityKeyIndex : EntityIndex
        {
            public static IdentityKeyIndex Create(ReadOnlyCollection<MetaMember> keyMembers)
            {
                if (keyMembers.Count == 1)
                {
                    MetaMember.IValueAccessor accessor = keyMembers[0].GetValueAccessor();
                    Type indexType = typeof(IdentityKeyIndex<>).MakeGenericType(accessor.KeyType);
                    return (IdentityKeyIndex)Activator.CreateInstance(indexType, [accessor])!;
                }

                return new IdentityKeyIndex<object>(IdentityValueAccessor.Instance);
            }

            public abstract bool TryGetValue(object identity, [NotNullWhen(true)] out Entity? entity);

            public abstract bool TryGetByIdentity(Entity identity, bool throwOnNull, [NotNullWhen(true)] out Entity? entity);

            public abstract void Update(Entity entity, string propertyName);

            /// <summary>
            /// A value accessor that retrieves the identity of an entity.
            /// </summary>
            private sealed class IdentityValueAccessor : MetaMember.IValueAccessor<object>
            {
                public static IdentityValueAccessor Instance { get; } = new IdentityValueAccessor();

                Type MetaMember.IValueAccessor.KeyType => typeof(object);

                bool MetaMember.IValueAccessor<object>.TryGetValue(object instance, out object value)
                {
                    object? identity = ((Entity)instance).GetIdentity();
                    if (identity is not null)
                    {
                        value = identity;
                        return true;
                    }
                    value = default!;
                    return false;
                }
            }
        }

        /// <summary>
        /// Maintains bidirectional identity mappings using the identity's CLR type.
        /// </summary>
        /// <typeparam name="TKey">The entity identity type.</typeparam>
        private sealed class IdentityKeyIndex<TKey> : IdentityKeyIndex where TKey : notnull
        {
            private readonly Dictionary<TKey, Entity> _entities = new(EqualityComparer<TKey>.Default);
            private readonly Dictionary<Entity, TKey> _identitiesByEntity = new(ReferenceEqualityComparer<Entity>.Instance);
            private readonly MetaMember.IValueAccessor<TKey> _identityAccessor;

            public IdentityKeyIndex(MetaMember.IValueAccessor<TKey> identityAccessor)
            {
                _identityAccessor = identityAccessor;
            }

            public override void Clear()
            {
                _entities.Clear();
                _identitiesByEntity.Clear();
            }

            public override bool TryGetValue(object identity, [NotNullWhen(true)] out Entity? entity)
            {
                if (identity is TKey typedIdentity)
                {
                    return _entities.TryGetValue(typedIdentity, out entity);
                }

                entity = null;
                return false;
            }

            public override bool TryGetByIdentity(Entity identity, bool throwOnNull, [NotNullWhen(true)] out Entity? entity)
            {
                if (_identityAccessor.TryGetValue(identity, out TKey? typedIdentity))
                {
                    return _entities.TryGetValue(typedIdentity, out entity);
                }

                if (throwOnNull)
                {
                    ThrowEntityKeyNullException(identity);
                }

                entity = null;
                return false;
            }

            public override void Add(Entity entity)
            {
                TKey typedIdentity = GetIdentity(entity);
                if (!_entities.TryAdd(typedIdentity, entity))
                {
                    throw new InvalidOperationException(Resource.EntitySet_DuplicateIdentity);
                }

                _identitiesByEntity[entity] = typedIdentity;
            }

            public override void Remove(Entity entity)
            {
                if (_identitiesByEntity.Remove(entity, out TKey? identity))
                {
                    _entities.Remove(identity);
                }
            }

            public override void Update(Entity entity, string propertyName)
            {
                if (!_identityAccessor.TryGetValue(entity, out TKey identity))
                {
                    Remove(entity);
                    return;
                }

                if (_identitiesByEntity.TryGetValue(entity, out TKey? existingIdentity))
                {
                    if (EqualityComparer<TKey>.Default.Equals(existingIdentity, identity))
                    {
                        return;
                    }

                    if (!_entities.TryAdd(identity, entity))
                    {
                        throw new InvalidOperationException(Resource.EntitySet_DuplicateIdentity);
                    }

                    _entities.Remove(existingIdentity);
                    _identitiesByEntity[entity] = identity;
                }
                else
                {

                    if (!_entities.TryAdd(identity, entity))
                    {
                        throw new InvalidOperationException(Resource.EntitySet_DuplicateIdentity);
                    }

                    _identitiesByEntity[entity] = identity;
                }
            }

            public override bool TryLookup(EntityAssociationAttribute association, Entity sourceEntity, [NotNullWhen(true)] out IEnumerable<Entity>? entities)
            {
                if (!TryLookup(association, sourceEntity, out Entity? entity))
                {
                    entities = null;
                    return false;
                }

                entities = entity is null ? Array.Empty<Entity>() : [entity];
                return true;
            }

            public override bool TryLookup(EntityAssociationAttribute association, Entity sourceEntity, out Entity? entity)
            {
                var sourceMemberNames = association.ThisKeyMembers;
                if (sourceMemberNames.Count == 1)
                {
                    MetaMember sourceMember = sourceEntity.MetaType[sourceMemberNames[0]];
                    if (sourceMember?.GetValueAccessor() is not MetaMember.IValueAccessor<TKey> accessor)
                    {
                        entity = null;
                        return false;
                    }

                    if (!accessor.TryGetValue(sourceEntity, out TKey identity))
                    {
                        entity = null;
                        return true;
                    }

                    if (!_entities.TryGetValue(identity, out entity) || !ShouldIndexEntity(entity))
                    {
                        entity = null;
                    }

                    return true;
                }

                if (typeof(TKey) != typeof(object))
                {
                    entity = null;
                    return false;
                }

                object[] keyValues = new object[sourceMemberNames.Count];
                for (int i = 0; i < sourceMemberNames.Count; i++)
                {
                    MetaMember sourceMember = sourceEntity.MetaType[sourceMemberNames[i]];
                    object? keyValue = sourceMember?.GetValue(sourceEntity);
                    if (keyValue == null)
                    {
                        entity = null;
                        return sourceMember != null;
                    }

                    keyValues[i] = keyValue;
                }

                TKey compositeIdentity = (TKey)(object)EntityKey.Create(keyValues);
                if (!_entities.TryGetValue(compositeIdentity, out entity) || !ShouldIndexEntity(entity))
                {
                    entity = null;
                }

                return true;
            }

            /// <summary>
            /// Get the identity of the entity from the identity accessor, throwing if the identity is null.
            /// </summary>
            /// <exception cref="InvalidOperationException"></exception>
            [System.Diagnostics.StackTraceHidden]
            private TKey GetIdentity(Entity entity)
            {
                if (!_identityAccessor.TryGetValue(entity, out TKey key))
                {
                    ThrowEntityKeyNullException(entity);
                }

                return key;
            }

            [DoesNotReturn]
            [System.Diagnostics.StackTraceHidden]
            private static void ThrowEntityKeyNullException(Entity identity)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.EntityKey_NullIdentity, identity));
            }
        }
    }
}
