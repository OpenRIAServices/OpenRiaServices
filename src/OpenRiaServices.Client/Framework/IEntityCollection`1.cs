using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace OpenRiaServices.DomainServices.Client
{
    /// <summary>
    ///   Defines methods for working with collections of <see cref="Entity"/>s which support change notifications.
    ///   <para>
    ///   It allows code to handle both <see cref="EntityCollection{TEntity}"/>, <see cref="EntitySet{TEntity}"/> and specialised collections 
    ///   such as the one supplied by "OpenRiaServices.M2M" in a uniform way.
    ///   </para>
    /// </summary>
    /// <typeparam name="TEntity"> The type of the elements in the collection </typeparam>
    public interface IEntityCollection<TEntity> : ICollection<TEntity>, IReadOnlyCollection<TEntity>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        /// <summary>
        ///   Event raised whenever an <see cref="Entity"/> is added to this collection
        /// </summary>
        event EventHandler<EntityCollectionChangedEventArgs<TEntity>> EntityAdded;

        /// <summary>
        ///   Event raised whenever an  <see cref="Entity"/> is removed from this collection
        /// </summary>
        event EventHandler<EntityCollectionChangedEventArgs<TEntity>> EntityRemoved;
    }
}
