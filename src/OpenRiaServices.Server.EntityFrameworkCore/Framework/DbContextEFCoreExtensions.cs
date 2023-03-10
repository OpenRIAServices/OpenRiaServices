using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OpenRiaServices.Server;

namespace OpenRiaServices.Server.EntityFrameworkCore
{
    /// <summary>
    /// DbContext extension methods for DbDomainService authors.
    /// </summary>
    public static class DbContextEFCoreExtensions
    {
        /// <summary>
        /// Extension method used to attach the specified entity as modified,
        /// with the specified original state.
        /// </summary>
        /// <typeparam name="T">The entity Type</typeparam>
        /// <param name="dbSet">The <see cref="DbSet{T}"/> to attach to.</param>
        /// <param name="current">The current entity.</param>
        /// <param name="original">The original entity.</param>
        /// <param name="dbContext">The corresponding <see cref="DbContext"/></param>
        public static void AttachAsModified<T>(this DbSet<T> dbSet, T current, T original, DbContext dbContext) where T : class
        {
            if (dbSet == null)
            {
                throw new ArgumentNullException(nameof(dbSet));
            }
            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }
            if (original == null)
            {
                throw new ArgumentNullException(nameof(original));
            }
            if (dbContext == null)
            {
                throw new ArgumentNullException(nameof(dbContext));
            }

            EntityEntry<T> entityEntry = dbContext.Entry(current);
            if (entityEntry.State == EntityState.Detached)
            {
                dbSet.Attach(current);
            }
            else
            {
                entityEntry.State = EntityState.Modified;
            }

            var changeTracker = dbContext.ChangeTracker;
            AttachAsModifiedInternal(entityEntry, original, changeTracker);

            if (entityEntry.State != EntityState.Modified)
            {
                // Ensure that when we leave this method, the entity is in a
                // Modified state. For example, if current and original are the
                // same, we still need to force the state transition
                entityEntry.State = EntityState.Modified;
            }
        }

        /// <summary>
        /// Extension method used to attach the specified entity as modified. This overload
        /// can be used in cases where the entity has a Timestamp member.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="dbSet">The <see cref="DbSet{T}"/> to attach to</param>
        /// <param name="entity">The current entity</param>
        /// <param name="dbContext">The coresponding <see cref="DbContext"/></param>
        public static void AttachAsModified<T>(this DbSet<T> dbSet, T entity, DbContext dbContext) where T : class
        {
            if (dbSet == null)
            {
                throw new ArgumentNullException(nameof(dbSet));
            }
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }
            if (dbContext == null)
            {
                throw new ArgumentNullException(nameof(dbContext));
            }

            EntityEntry<T> entityEntry = dbContext.Entry(entity);
            if (entityEntry.State == EntityState.Detached)
            {
                // attach the entity
                dbSet.Attach(entity);
            }

            // transition the entity to the modified state
            entityEntry.State = EntityState.Modified;
        }

        private static void AttachAsModifiedInternal<T>(EntityEntry<T> stateEntry, T original, ChangeTracker objectContext)
            where T : class
        {
            // Apply original vaules
            var originalValues = objectContext.Context.Entry(original).CurrentValues;

            Type entityType = stateEntry.Entity.GetType();
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(entityType);
            AttributeCollection attributes = TypeDescriptor.GetAttributes(entityType);
            bool isRoundtripType = attributes[typeof(RoundtripOriginalAttribute)] != null;

            foreach (var member in stateEntry.CurrentValues.Properties)
            {
                PropertyDescriptor property = properties[member.Name];

                if (property != null) // Exclude shadow properties
                {
                    stateEntry.OriginalValues[member] = originalValues[member];

                    // For any members that don't have RoundtripOriginal applied, EF can't determine modification
                    // state by doing value comparisons. To avoid losing updates in these cases, we must explicitly
                    // mark such members as modified.
                    if (member.IsPrimaryKey() ||
                        isRoundtripType || property.Attributes[typeof(RoundtripOriginalAttribute)] != null ||
                         property.Attributes[typeof(ExcludeAttribute)] != null)
                    {
                        stateEntry.Property(member.Name).IsModified = !object.Equals(stateEntry.OriginalValues[member], originalValues[member]);
                    }
                    else
                    {
                        stateEntry.Property(member.Name).IsModified = true;
                    }
                }
            }
        }
    }
}
