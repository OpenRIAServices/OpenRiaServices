using System;

#nullable enable

namespace OpenRiaServices.Client
{
    /// <summary>
    /// Represents a reference to an associated entity whose association members are
    /// the primary key of the associated entity.
    /// </summary>
    /// <typeparam name="TEntity">The type of the associated <see cref="Entity"/>.</typeparam>
    /// <remarks>
    /// This reference uses the target <see cref="EntitySet"/> identity index instead of
    /// scanning the set. It must only be used when the association's other-key members
    /// exactly match the associated entity's primary key members in identity order.
    /// </remarks>
    public sealed class EntityRefByKey<TEntity> where TEntity : Entity
    {
        private readonly Func<object?> _keyGetter;
        private readonly EntityRef<TEntity> _entityRef;
        private int _identityVersion;

        /// <summary>
        /// Initializes a new key-based entity reference.
        /// </summary>
        /// <param name="parent">The entity that owns the association.</param>
        /// <param name="memberName">The association member name on <paramref name="parent"/>.</param>
        /// <param name="keyGetter">A function that returns the target entity identity.</param>
        public EntityRefByKey(Entity parent, string memberName, Func<object?> keyGetter)
        {
            ArgumentNullException.ThrowIfNull(keyGetter);

            this._keyGetter = keyGetter;
            this._entityRef = new EntityRef<TEntity>(parent, memberName, this.KeyEquals, this.FindEntity);
        }

        /// <summary>
        /// Gets or sets the associated entity.
        /// </summary>
        public TEntity? Entity
        {
            get => this._entityRef.Entity;
            set => this._entityRef.Entity = value;
        }

        private bool KeyEquals(TEntity entity)
        {
            return object.Equals(entity.GetIdentity(), this._keyGetter());
        }

        private TEntity? FindEntity(EntitySet set)
        {
            object? key = this._keyGetter();
            if (key == null)
            {
                return null;
            }

            if (this._identityVersion != set.IdentityVersion)
            {
                this._identityVersion = set.IdentityVersion;
                TEntity? match = null;
                foreach (TEntity candidate in set)
                {
                    if (candidate.EntityState != EntityState.New && this.KeyEquals(candidate))
                    {
                        if (match != null)
                        {
                            return null;
                        }

                        match = candidate;
                    }
                }

                return match;
            }

            TEntity? entity = set.GetEntityByIdentity(key) as TEntity;
            return entity?.EntityState != EntityState.New ? entity : null;
        }
    }
}
