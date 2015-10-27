using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using OpenRiaServices.DomainServices.Server;

namespace OpenRiaServices.DomainServices.EntityFramework.ObjectContextExtensions
{
    /// <summary>
    /// ObjectContext extension methods useful to LinqToEntitiesDomainService authors
    /// </summary>
    public static class ObjectContextExtensions
    {
        /// <summary>
        /// Extension method used to attach the specified entity as modified,
        /// with the specified original state.
        /// </summary>
        /// <typeparam name="T">The entity Type</typeparam>
        /// <param name="objectSet">The ObjectSet to attach to</param>
        /// <param name="current">The current entity state</param>
        /// <param name="original">The original entity state</param>
        public static void AttachAsModified<T>(this ObjectSet<T> objectSet, T current, T original) where T : class
        {
            if (objectSet == null)
            {
                throw new ArgumentNullException("objectSet");
            }
            if (current == null)
            {
                throw new ArgumentNullException("current");
            }
            if (original == null)
            {
                throw new ArgumentNullException("original");
            }

            // Attach the entity if it is not already attached, or if it is already
            // attached, transition to Modified
            EntityState currState = ObjectContextUtilities.GetEntityState(objectSet.Context, current);
            if (currState == EntityState.Detached)
            {
                objectSet.Attach(current);
            }
            else
            {
                objectSet.Context.ObjectStateManager.ChangeObjectState(current, EntityState.Modified);
            }

            ObjectStateEntry stateEntry = ObjectContextUtilities.AttachAsModifiedInternal(current, original, objectSet.Context);

            if (stateEntry.State != EntityState.Modified)
            {
                // Ensure that when we leave this method, the entity is in a
                // Modified state. For example, if current and original are the
                // same, we still need to force the state transition
                objectSet.Context.ObjectStateManager.ChangeObjectState(current, EntityState.Modified);
            }
        }

        /// <summary>
        /// Extension method used to attach the specified entity as modified. This overload
        /// can be used in cases where the entity has a Timestamp member.
        /// </summary>
        /// <typeparam name="T">The entity Type</typeparam>
        /// <param name="objectSet">The ObjectSet to attach to</param>
        /// <param name="entity">The current entity state</param>
        public static void AttachAsModified<T>(this ObjectSet<T> objectSet, T entity) where T : class
        {
            if (objectSet == null)
            {
                throw new ArgumentNullException("objectSet");
            }
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            ObjectContext context = objectSet.Context;
            EntityState currState = ObjectContextUtilities.GetEntityState(context, entity);
            if (currState == EntityState.Detached)
            {
                // attach the entity
                objectSet.Attach(entity);
            }

            // transition the entity to the modified state
            context.ObjectStateManager.ChangeObjectState(entity, EntityState.Modified);
        }
    }
}
