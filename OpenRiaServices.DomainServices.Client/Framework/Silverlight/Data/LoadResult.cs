using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace OpenRiaServices.DomainServices.Client
{

    public class LoadResult<TEntity> : IEnumerable<TEntity>, ICollection
        where TEntity : Entity
    {
        private IEnumerable<TEntity> _loadedEntites;

        public LoadResult(LoadOperation<TEntity> op)
        {
            if (op.IsCanceled || op.HasError || !op.IsComplete)
                throw new InvalidOperationException();

            _loadedEntites = op.Entities;
            AllEntities = op.AllEntities;
            EntityQuery = op.EntityQuery;
            TotalEntityCount = op.TotalEntityCount;
            LoadBehavior = op.LoadBehavior;
        }


        public IEnumerable<TEntity> Entities { get { return _loadedEntites; } }
        public IEnumerable<Entity> AllEntities
        {
            get;
            private set;
        }
        public EntityQuery<TEntity> EntityQuery { get; private set; }

        public int TotalEntityCount { get; private set; }

        public LoadBehavior LoadBehavior { get; private set; }

        // implements ICollection.Count


        public int Count { get { return _loadedEntites.Count(); } }

        IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator() { return _loadedEntites.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return _loadedEntites.GetEnumerator(); }

        //TODO: Implement properly
        void ICollection.CopyTo(Array array, int index)
        {
            _loadedEntites.ToArray().CopyTo(array, index);
        }

        //TODO: Implement properly
        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        //TODO: Implement properly
        object ICollection.SyncRoot
        {
            get { return this; }
        }
    }
}