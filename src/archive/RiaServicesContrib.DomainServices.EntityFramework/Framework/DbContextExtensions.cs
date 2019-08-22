using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.ServiceModel.DomainServices.Server;
using OpenRiaServices.DomainServices.EntityFramework;

namespace System.ServiceModel.DomainServices.EntityFramework
{
    /// <summary>
    /// DbContext extension methods for DbDomainService authors.
    /// </summary>
    public static class DbContextExtensions
    {
        /// <summary>
        /// Extension method used to attach the specified entity as modified,
        /// with the specified original state.
        /// </summary>
        /// <typeparam name="T">The entity Type</typeparam>
        /// <param name="dbSet">The <see cref="DbSet"/> to attach to.</param>
        /// <param name="current">The current entity.</param>
        /// <param name="original">The original entity.</param>
        /// <param name="dbContext">The corresponding <see cref="DbContext"/></param>
        public static void AttachAsModified<T>(this DbSet<T> dbSet, T current, T original, DbContext dbContext) where T : class
        {
            if (dbSet == null)
            {
                throw new ArgumentNullException("dbSet");
            }
            if (current == null)
            {
                throw new ArgumentNullException("current");
            }
            if (original == null)
            {
                throw new ArgumentNullException("original");
            }
            if (dbContext == null)
            {
                throw new ArgumentNullException("dbContext");
            }

            DbEntityEntry<T> entityEntry = dbContext.Entry(current);
            if (entityEntry.State == EntityState.Detached)
            {
                dbSet.Attach(current);
            }
            else
            {
                entityEntry.State = EntityState.Modified;
            }

            ObjectContext objectContext = (dbContext as IObjectContextAdapter).ObjectContext;
            ObjectStateEntry stateEntry = ObjectContextUtilities.AttachAsModifiedInternal(current, original, objectContext);            

            if (stateEntry.State != EntityState.Modified)
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
        /// <param name="dbSet">The <see cref="DbSet"/> to attach to</param>
        /// <param name="entity">The current entity</param>
        /// <param name="dbContext">The coresponding <see cref="DbContext"/></param>
        public static void AttachAsModified<T>(this DbSet<T> dbSet, T entity, DbContext dbContext) where T : class
        {
            if (dbSet == null)
            {
                throw new ArgumentNullException("dbSet");
            }
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            if (dbContext == null)
            {
                throw new ArgumentNullException("dbContext");
            }

            DbEntityEntry<T> entityEntry = dbContext.Entry(entity);
            if (entityEntry.State == EntityState.Detached)
            {
                // attach the entity
                dbSet.Attach(entity);
            }

            // transition the entity to the modified state
            entityEntry.State = EntityState.Modified;
        }
    }
}
