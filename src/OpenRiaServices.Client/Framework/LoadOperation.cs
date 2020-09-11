using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using OpenRiaServices.Client.Data;

namespace OpenRiaServices.Client
{
    /// <summary>
    /// Represents an asynchronous load operation
    /// </summary>
    public abstract class LoadOperation : OperationBase, ILoadResult
    {
        private ReadOnlyObservableLoaderCollection<Entity> _entities;
        private ReadOnlyObservableLoaderCollection<Entity> _allEntities;

        private IReadOnlyCollection<ValidationResult> _validationErrors;
        private readonly LoadBehavior _loadBehavior;
        private readonly EntityQuery _query;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadOperation"/> class.
        /// </summary>
        /// <param name="query">The query to load.</param>
        /// <param name="loadBehavior"><see cref="LoadBehavior"/> to use for the load operation.</param>
        /// <param name="userState">Optional user state for the operation.</param>
        /// <param name="supportCancellation"><c>true</c> to enable <see cref="OperationBase.CancellationToken"/> to be cancelled when <see cref="OperationBase.Cancel"/> is called</param>
        private protected LoadOperation(EntityQuery query, LoadBehavior loadBehavior, object userState, bool supportCancellation)
            : base(userState, supportCancellation)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }
            this._query = query;
            this._loadBehavior = loadBehavior;
        }

        /// <summary>
        /// The <see cref="ILoadResult"/> for this operation.
        /// </summary>
        private protected new ILoadResult Result => (ILoadResult)base.Result;

        /// <summary>
        /// The <see cref="EntityQuery"/> for this load operation.
        /// </summary>
        public EntityQuery EntityQuery => this._query;

        /// <summary>
        /// The <see cref="LoadBehavior"/> for this load operation.
        /// </summary>
        public LoadBehavior LoadBehavior => this._loadBehavior;

        /// <summary>
        /// Gets all the top level entities loaded by the operation. The collection returned implements
        /// <see cref="System.Collections.Specialized.INotifyCollectionChanged"/>.
        /// </summary>
        public IReadOnlyCollection<Entity> Entities
        {
            get
            {
                if (this._entities == null)
                {
                    var resultEntities = this.Result != null ? this.Result.Entities : Enumerable.Empty<Entity>();
                    this._entities = AsReadOnlyObservableLoaderCollection(resultEntities);
                }
                return this._entities;
            }
        }

        /// <summary>
        /// Gets all the entities loaded by the operation, including any
        /// entities referenced by the top level entities. The collection returned implements
        /// <see cref="System.Collections.Specialized.INotifyCollectionChanged"/>.
        /// </summary>
        public IReadOnlyCollection<Entity> AllEntities
        {
            get
            {
                if (this._allEntities == null)
                {
                    var resultEntities = this.Result != null ? this.Result.AllEntities : Enumerable.Empty<Entity>();
                    this._allEntities = AsReadOnlyObservableLoaderCollection(resultEntities);
                }
                return this._allEntities;
            }
        }

        /// <summary>
        /// Gets the total server entity count for the query used by this operation. Automatic
        /// evaluation of the total server entity count requires the property <see cref="OpenRiaServices.Client.EntityQuery.IncludeTotalCount"/>
        /// on the query for the load operation to be set to <c>true</c>.
        /// </summary>
        public int TotalEntityCount => this.Result != null ? this.Result.TotalEntityCount : 0;

        /// <summary>
        /// Gets the validation errors.
        /// </summary>
        public IReadOnlyCollection<ValidationResult> ValidationErrors
        {
            get
            {
                // return any errors if set, otherwise return an empty
                // collection
                if (this._validationErrors == null)
                {
                    this._validationErrors = Array.Empty<ValidationResult>();
                }
                return this._validationErrors;
            }
        }

        /// <summary>
        /// Completes the load operation with the specified error.
        /// </summary>
        /// <param name="error">The error.</param>
        internal new void SetError(Exception error)
        {
            if (error is DomainOperationException doe
                && doe.ValidationErrors.Any())
            {
                this._validationErrors = doe.ValidationErrors;
                this.RaisePropertyChanged(nameof(ValidationErrors));
            }

            base.SetError(error);
        }

        /// <summary>
        /// Update the observable result collections.
        /// </summary>
        /// <param name="result">The results of the completed load operation.</param>
        private protected virtual void UpdateResults(ILoadResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            // if the Entities property has been examined, update the backing
            // observable collection
            this._entities?.Reset(result.Entities);

            // if the AllEntities property has been examined, update the backing
            // observable collection
            this._allEntities?.Reset(result.AllEntities);
        }

        private protected static ReadOnlyObservableLoaderCollection<TEntity> AsReadOnlyObservableLoaderCollection<TEntity>(IEnumerable<TEntity> resultEntities)
        {
            return resultEntities as ReadOnlyObservableLoaderCollection<TEntity> ?? new ReadOnlyObservableLoaderCollection<TEntity>(resultEntities);
        }
    }

    /// <summary>
    /// Represents an asynchronous load operation
    /// </summary>
    /// <typeparam name="TEntity">The entity Type being loaded.</typeparam>
    public sealed class LoadOperation<TEntity> : LoadOperation
        where TEntity : Entity
    {
        private ReadOnlyObservableLoaderCollection<TEntity> _entities;
        private readonly Action<LoadOperation<TEntity>> _completeAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadOperation"/> class.
        /// </summary>
        /// <param name="query">The query to load.</param>
        /// <param name="loadBehavior"><see cref="LoadBehavior"/> to use for the load operation.</param>
        /// <param name="completeAction">Action to execute when the operation completes.</param>
        /// <param name="userState">Optional user state for the operation.</param>
        /// <param name="supportCancellation"><c>true</c> to enable <see cref="OperationBase.CancellationToken"/> to be cancelled when <see cref="OperationBase.Cancel"/> is called</param>
        internal LoadOperation(EntityQuery<TEntity> query, LoadBehavior loadBehavior,
            Action<LoadOperation<TEntity>> completeAction, object userState,
            bool supportCancellation)
            : base(query, loadBehavior, userState, supportCancellation)
        {
            this._completeAction = completeAction;
        }

        /// <summary>
        /// The <see cref="EntityQuery"/> for this load operation.
        /// </summary>
        public new EntityQuery<TEntity> EntityQuery => (EntityQuery<TEntity>)base.EntityQuery;

        /// <summary>
        /// Gets all the entities loaded by the operation, including any
        /// entities referenced by the top level entities. The collection returned implements
        /// <see cref="System.Collections.Specialized.INotifyCollectionChanged"/>.
        /// </summary>
        public new IReadOnlyCollection<TEntity> Entities
        {
            get
            {
                if (this._entities == null)
                {
                    var resultEntities = this.Result != null ? this.Result.Entities.Cast<TEntity>() : Enumerable.Empty<TEntity>();
                    this._entities = AsReadOnlyObservableLoaderCollection(resultEntities);
                }

                return this._entities;
            }
        }

        /// <summary>
        /// Update the observable result collections.
        /// </summary>
        /// <param name="result">The results of the completed load operation.</param>
        private void UpdateResults(LoadResult<TEntity> result)
        {
            base.UpdateResults((ILoadResult)result);

            // if the Entities property has been examined, update the backing
            // observable collection
            this._entities?.Reset(result.Entities);
        }

        /// <summary>
        /// Invoke the completion callback.
        /// </summary>
        protected override void InvokeCompleteAction()
        {
            this._completeAction?.Invoke(this);
        }

        /// <summary>
        /// The <see cref="LoadResult{TEntity}"/> for this operation.
        /// </summary>
        private new LoadResult<TEntity> Result => (LoadResult<TEntity>)base.Result;

        /// <summary>
        /// Successfully completes the load operation with the specified result.
        /// </summary>
        /// <param name="result">The result.</param>
        internal void Complete(LoadResult<TEntity> result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            // before calling base, we need to update any cached
            // observable collection results so the correct data
            // is accessible in the completion callback
            if (result.Entities.Any())
            {
                this.UpdateResults(result);
            }

            base.Complete(result);

            // raise our property events after all base property
            // events have been raised
            if (result.Entities.Any())
            {
                this.RaisePropertyChanged(nameof(TotalEntityCount));
            }
        }
    }
}
