using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OpenRiaServices.Client
{
    /// <summary>
    /// Internal interface providing loosely typed access to <see cref="EntityCollection&lt;TEntity&gt;"/> members needed
    /// by the framework
    /// </summary>
    // TODO : Consider making this interface (or a subset of it) public
    internal interface IEntityCollection
    {
        /// <summary>
        /// Gets the AssociationAttribute for this collection.
        /// </summary>
        EntityAssociationAttribute Association
        {
            get;
        }

        /// <summary>
        /// Gets the collection of entities, loading the collection if it hasn't been loaded
        /// already. To avoid the deferred load, inspect the HasValues property first.
        /// </summary>
        IEnumerable<Entity> Entities
        {
            get;
        }

        /// <summary>
        /// Gets a value indicating whether this EntityCollection has been loaded or
        /// has had entities added to it.
        /// </summary>
        bool HasValues
        {
            get;
        }

        /// <summary>
        /// Adds the specified entity to the collection.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        void Add(Entity entity);

        /// <summary>
        /// Removes the specified entity from the collection.
        /// </summary>
        /// <param name="entity">The entity to remove.</param>
        void Remove(Entity entity);
    }
}
