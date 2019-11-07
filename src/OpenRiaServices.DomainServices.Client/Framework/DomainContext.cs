using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using OpenRiaServices.DomainServices.Client.Data;

namespace OpenRiaServices.DomainServices.Client
{
    /// <summary>
    /// A <see cref="DomainContext"/> is a stateful client side representation of a DomainService, providing
    /// access to all functionality of the service.
    /// </summary>
    public abstract class DomainContext : INotifyPropertyChanged
    {
        internal const int TotalCountUndefined = -1;

        private static readonly MethodInfo s_invokeOperationAsync = typeof(DomainContext)
                .GetMethod(nameof(InvokeOperationAsync), new Type[] {
                    typeof(string),
                    typeof(IDictionary<string, object>),
                    typeof(bool),
                    typeof(Type),
                    typeof(CancellationToken)
                });

        private int _activeLoadCount;
        private readonly DomainClient _domainClient;
        private EntityContainer _entityContainer;
        private readonly TaskScheduler _syncContextScheduler;
        private ValidationContext _validationContext;
        private bool _isSubmitting;
        private readonly Dictionary<string, bool> requiresValidationMap = new Dictionary<string, bool>();
        private readonly object _syncRoot = new object();
        private static IDomainClientFactory s_domainClientFactory;

        /// <summary>
        /// Protected constructor
        /// </summary>
        /// <param name="domainClient">The <see cref="DomainClient"/> instance this <see cref="DomainContext"/> should use</param>
        protected DomainContext(DomainClient domainClient)
        {
            if (domainClient == null)
            {
                throw new ArgumentNullException(nameof(domainClient));
            }

            this._domainClient = domainClient;
            this._syncContextScheduler = SynchronizationContext.Current != null ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Default;
        }

        /// <summary>
        /// Creates a WebDomainClient for the specified service contract and uri
        /// </summary>
        /// <param name="serviceContract">The service contract.</param>
        /// <param name="serviceUri">The service URI.</param>
        /// <param name="usesHttps"><c>true</c> to use https instead of http</param>
        /// <remarks>
        ///  This implementation should be replaced with a call to an IDomainClientFactory interface so 
        ///  the logic can be replaced. If no specific IDomainClientFactory is choosen by user then we can 
        ///  fall back to a "DefaultDomainClientFactory" with the same behaviour as in this function.
        /// </remarks>
        /// <returns>A domain client which can be used to access the service which the serviceContract reference</returns>
        protected static DomainClient CreateDomainClient(Type serviceContract, Uri serviceUri, bool usesHttps)
        {
            return DomainClientFactory.CreateDomainClient(serviceContract, serviceUri, usesHttps);
        }

        /// <summary>
        /// Gets or sets the <see cref="IDomainClientFactory"/> used to create <see cref="DomainClient"/> instances.
        /// </summary>
        /// <value>
        /// The domain client factory.
        /// </value>
        /// <exception cref="System.ArgumentNullException">if trying to set the property to null</exception>
        public static IDomainClientFactory DomainClientFactory
        {
            get
            {
                // We don't perform syncronization here, but it is ok since in worst case we might end up creating two different instances of the _domainClientFactory
                return s_domainClientFactory ?? (s_domainClientFactory = CreateDomainClientFactory());
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                s_domainClientFactory = value;
            }
        }

        /// <summary>
        /// Event raised whenever a <see cref="DomainContext"/> property changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the <see cref="DomainClient"/> for this context
        /// </summary>
        public DomainClient DomainClient
        {
            get
            {
                // if the set of entity Types hasn't been externally initialized, initialize it now
                if (this._domainClient.EntityTypes == null)
                {
                    this._domainClient.EntityTypes = this.EntityContainer.EntitySets.Select(p => p.EntityType);
                }

                return this._domainClient;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="DomainContext"/> is currently performing a Load operation.
        /// </summary>
        public bool IsLoading
        {
            get
            {
                return (this._activeLoadCount > 0);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="DomainContext"/> is currently performing a Submit operation.
        /// </summary>
        public bool IsSubmitting
        {
            get
            {
                return this._isSubmitting;
            }
            private set
            {
                this._isSubmitting = value;
                this.RaisePropertyChanged(nameof(IsSubmitting));
            }
        }

        /// <summary>
        /// Gets the <see cref="EntityContainer"/> holding all entities loaded by this context.
        /// </summary>
        public EntityContainer EntityContainer
        {
            get
            {
                if (this._entityContainer == null)
                {
                    this._entityContainer = this.CreateEntityContainer();
                    if (this._entityContainer == null)
                    {
                        throw new InvalidOperationException(Resource.DomainContext_EntityContainerCannotBeNull);
                    }
                    this._entityContainer.ValidationContext = this.ValidationContext;
                    this._entityContainer.PropertyChanged += new PropertyChangedEventHandler(this.EntityContainerPropertyChanged);
                }
                return this._entityContainer;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this context has any pending changes
        /// </summary>
        public bool HasChanges
        {
            get
            {
                return this.EntityContainer.HasChanges;
            }
        }

        /// <summary>
        /// Gets or sets the optional <see cref="ValidationContext"/> to use
        /// for all validation operations invoked by the <see cref="DomainContext"/>.
        /// </summary>
        /// <value>
        /// This value may be set by the developer at any time to be used as the backing
        /// <see cref="IServiceProvider"/> and source of
        /// <see cref="System.ComponentModel.DataAnnotations.ValidationContext.Items"/>,
        /// making these services and items available to each
        /// <see cref="ValidationAttribute"/> involved in validation.
        /// </value>
        public ValidationContext ValidationContext
        {
            get
            {
                return this._validationContext;
            }
            set
            {
                this._validationContext = value;

                // Flow this validation context to the entity container too,
                // making it available to the entities (through their entity sets).
                this.EntityContainer.ValidationContext = value;
            }
        }

        /// <summary>
        /// Creates and returns an entity container configured with <see cref="EntitySet"/>s for all 
        /// entities this <see cref="DomainContext"/> will provide access to. The return must be non-null.
        /// </summary>
        /// <returns>The container</returns>
        protected abstract EntityContainer CreateEntityContainer();

        /// <summary>
        /// Adds a reference to an external <see cref="DomainContext"/>. Once a reference is established, referenced
        /// <see cref="DomainContext"/> instances will be consulted when resolving the <see cref="EntitySet"/> for an
        /// <see cref="Entity"/> type.
        /// </summary>
        /// <param name="entityType">The entity type to lookup in the <paramref name="domainContext"/>.</param>
        /// <param name="domainContext">A <see cref="DomainContext"/> to register as an external reference.</param>
        public void AddReference(Type entityType, DomainContext domainContext)
        {
            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }

            if (domainContext == null)
            {
                throw new ArgumentNullException(nameof(domainContext));
            }

            EntitySet entitySet = domainContext.EntityContainer.GetEntitySet(entityType);
            this.EntityContainer.AddReference(entitySet);
        }

        /// <summary>
        /// Revert all pending changes for this <see cref="DomainContext"/>.
        /// </summary>
        public void RejectChanges()
        {
            ((IRevertibleChangeTracking)this.EntityContainer).RejectChanges();
        }

        /// <summary>
        /// Submit all pending changes to the DomainService. If the submit fails,
        /// an exception will be thrown.
        /// </summary>
        /// <returns>The <see cref="SubmitOperation"/>.</returns>
        public SubmitOperation SubmitChanges()
        {
            return this.SubmitChanges(null, null);
        }

        /// <summary>
        /// Submit all pending changes to the DomainService.
        /// </summary>
        /// <param name="callback">Optional callback for the submit operation</param>
        /// <param name="userState">Optional user state to associate with the operation.
        /// </param>
        /// <returns>The <see cref="SubmitOperation"/>.</returns>
        public virtual SubmitOperation SubmitChanges(Action<SubmitOperation> callback, object userState)
        {
            if (this.IsSubmitting)
            {
                throw new InvalidOperationException(Resource.DomainContext_SubmitAlreadyInProgress);
            }

            // Set state
            this.IsSubmitting = true;

            SubmitOperation submitOperation;
            EntityChangeSet changeSet = null;
            try
            {
                // Build and validate the changeset
                changeSet = this.EntityContainer.GetChanges();
                bool validChangeset = this.ValidateChangeSet(changeSet, this.ValidationContext);

                submitOperation = new SubmitOperation(changeSet, callback, userState, DomainClient.SupportsCancellation);

                // Exit early if we have no changes or if there are validation errors
                if (changeSet.IsEmpty || !validChangeset)
                {
                    Task.Factory.StartNew(() =>
                    {
                        this.IsSubmitting = false;

                        // Need to check if the operation has already completed, for
                        // example if the operation was cancelled.
                        if (!submitOperation.IsComplete)
                        {
                            if (!validChangeset)
                            {
                                submitOperation.SetError(OperationErrorStatus.ValidationFailed);
                            }
                            else
                            {
                                submitOperation.Complete();
                            }
                        }
                    }
                    , CancellationToken.None
                    , TaskCreationOptions.None
                    , _syncContextScheduler);
                }
                else
                {
                    foreach (Entity entity in changeSet)
                    {
                        // Prevent any changes to the entities while we are submitting.
                        entity.IsSubmitting = true;
                    }

                    this.DomainClient.SubmitAsync(changeSet, submitOperation.CancellationToken)
                        .ContinueWith((submitTask, state) =>
                        {
                            this.CompleteSubmitChanges(submitTask, (SubmitOperation)state);
                        }
                        , submitOperation
                        , CancellationToken.None
                        , TaskContinuationOptions.HideScheduler
                        , _syncContextScheduler);
                }
            }
            catch
            {
                // if an exception is thrown revert our state
                this.IsSubmitting = false;
                if (changeSet != null)
                {
                    foreach (Entity entity in changeSet)
                    {
                        entity.IsSubmitting = false;
                    }
                }
                throw;
            }

            return submitOperation;
        }

        /// <summary>
        /// Submit all pending changes to the DomainService asyncronously.
        /// </summary>
        /// <returns>The <see cref="SubmitResult"/>.</returns>
        public Task<SubmitResult> SubmitChangesAsync()
        {
            return SubmitChangesAsync(CancellationToken.None);
        }

        /// <summary>
        /// Submit all pending changes to the DomainService asyncronously.
        /// </summary>
        /// <returns>The <see cref="SubmitResult"/>.</returns>
        /// <param name="cancellationToken">cancellation token</param>
        public virtual Task<SubmitResult> SubmitChangesAsync(CancellationToken cancellationToken)
        {
            TaskCompletionSource<SubmitResult> tcs = new TaskCompletionSource<SubmitResult>();

            var submitOp = this.SubmitChanges((res) => SetTaskResult(res, tcs, (op) => new SubmitResult(op.ChangeSet)), userState: null);
            RegisterCancellationToken(submitOp, cancellationToken);

            return tcs.Task;
        }

        /// <summary>
        /// Tries to set the return value on the TaskCompletionSource, using the TrySetXXX function family based on the status of a completed operation. 
        /// </summary>
        /// <typeparam name="TOperation">The type of the operation.</typeparam>
        /// <typeparam name="TResult">The type of the return type.</typeparam>
        /// <param name="operation">The operation which has completed.</param>
        /// <param name="tcs">The TaskCompletionSource used to create a task for the specified operation.</param>
        /// <param name="toResult">Function used to convert an operation into the corresponding result-type.</param>
        private void SetTaskResult<TOperation, TResult>(TOperation operation, TaskCompletionSource<TResult> tcs, Func<TOperation, TResult> toResult)
            where TOperation : OperationBase
        {
            if (operation.IsCanceled)
                tcs.TrySetCanceled();
            else if (operation.HasError)
            {
                operation.MarkErrorAsHandled();
                tcs.TrySetException(operation.Error);
            }
            else
            {
                tcs.TrySetResult(toResult(operation));
            }
        }

        /// <summary>
        /// Registers the operation with the CancellationToken so that the operation is cancelled whenever the cancellation is requested.
        /// </summary>
        /// <param name="operation">The operation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private static void RegisterCancellationToken(OperationBase operation, CancellationToken cancellationToken)
        {
            if (cancellationToken.CanBeCanceled && operation.CanCancel)
            {
                cancellationToken.Register((state) =>
                {
                    var op = (OperationBase)state;

                    if (op.CanCancel)
                        op.Cancel();
                }, operation, useSynchronizationContext: true);
            }
        }

        /// <summary>
        /// Creates an <see cref="EntityQuery"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity Type the query applies to.</typeparam>
        /// <param name="queryName">The name of the query method.</param>
        /// <param name="parameters">Optional parameters to the query method. Specify null
        /// if the query operation takes no parameters.</param>
        /// <param name="hasSideEffects">True if the query has side-effects, false otherwise.</param>
        /// <param name="isComposable">True if the query supports composition, false otherwise.</param>
        /// <returns>The query.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected EntityQuery<TEntity> CreateQuery<TEntity>(string queryName, IDictionary<string, object> parameters, bool hasSideEffects, bool isComposable) where TEntity : Entity
        {
            return new EntityQuery<TEntity>(this.DomainClient, queryName, parameters, hasSideEffects, isComposable);
        }

        /// <summary>
        /// Initiates a load operation for the specified query. If the operation fails, an
        /// exception will be thrown.
        /// </summary>
        /// <typeparam name="TEntity">The entity Type being loaded.</typeparam>
        /// <param name="query">The query to invoke.</param>
        /// <returns>The load operation.</returns>
        public LoadOperation<TEntity> Load<TEntity>(EntityQuery<TEntity> query) where TEntity : Entity
        {
            return (LoadOperation<TEntity>)this.Load((EntityQuery)query, LoadBehavior.KeepCurrent, null, null);
        }

        /// <summary>
        /// Initiates a load operation for the specified query.
        /// </summary>
        /// <typeparam name="TEntity">The entity Type being loaded.</typeparam>
        /// <param name="query">The query to invoke.</param>
        /// <param name="throwOnError">True if an unhandled error should result in an exception, false otherwise.
        /// To handle an operation error, <see cref="OperationBase.MarkErrorAsHandled()"/> can be called from the
        /// operation completion callback or from a <see cref="OperationBase.Completed"/> event handler.
        /// </param>
        /// <returns>The load operation.</returns>
        public LoadOperation<TEntity> Load<TEntity>(EntityQuery<TEntity> query, bool throwOnError) where TEntity : Entity
        {
            return this.Load<TEntity>(query, LoadBehavior.KeepCurrent, throwOnError);
        }

        /// <summary>
        /// Initiates a load operation for the specified query.
        /// </summary>
        /// <typeparam name="TEntity">The entity Type to be loaded.</typeparam>
        /// <param name="query">The query to invoke.</param>
        /// <param name="loadBehavior">The <see cref="LoadBehavior"/> to apply.</param>
        /// <param name="throwOnError">True if an unhandled error should result in an exception, false otherwise.
        /// To handle an operation error, <see cref="OperationBase.MarkErrorAsHandled"/> can be called from the
        /// operation completion callback or from a <see cref="OperationBase.Completed"/> event handler.
        /// </param>
        /// <returns>The load operation.</returns>
        public LoadOperation<TEntity> Load<TEntity>(EntityQuery<TEntity> query, LoadBehavior loadBehavior, bool throwOnError) where TEntity : Entity
        {
            Action<LoadOperation<TEntity>> callback = null;
            if (!throwOnError)
            {
                callback = (op) =>
                {
                    if (op.HasError)
                    {
                        op.MarkErrorAsHandled();
                    }
                };
            }

            return this.Load<TEntity>(query, loadBehavior, callback, null);
        }

        /// <summary>
        /// Initiates a load operation for the specified query.
        /// </summary>
        /// <typeparam name="TEntity">The entity Type being loaded.</typeparam>
        /// <param name="query">The query to invoke.</param>
        /// <param name="callback">Optional callback to be called when the load operation completes.</param>
        /// <param name="userState">Optional user state.</param>
        /// <returns>The load operation.</returns>
        public LoadOperation<TEntity> Load<TEntity>(EntityQuery<TEntity> query, Action<LoadOperation<TEntity>> callback, object userState) where TEntity : Entity
        {
            return this.Load<TEntity>(query, LoadBehavior.KeepCurrent, callback, userState);
        }

        /// <summary>
        /// Initiates a load operation for the specified query.
        /// </summary>
        /// <typeparam name="TEntity">The entity Type being loaded.</typeparam>
        /// <param name="query">The query to invoke.</param>
        /// <param name="loadBehavior">The <see cref="LoadBehavior"/> to apply.</param>
        /// <param name="callback">Optional callback to be called when the load operation completes.</param>
        /// <param name="userState">Optional user state.</param>
        /// <returns>The load operation.</returns>
        public virtual LoadOperation<TEntity> Load<TEntity>(EntityQuery<TEntity> query, LoadBehavior loadBehavior, Action<LoadOperation<TEntity>> callback, object userState) where TEntity : Entity
        {
            var loadOperation = new LoadOperation<TEntity>(query, loadBehavior, callback, userState, DomainClient.SupportsCancellation);

            LoadAsync(query, loadBehavior, loadOperation.CancellationToken)
                .ContinueWith((loadTask, state) =>
               {
                   var operation = (LoadOperation<TEntity>)state;


                   if (loadTask.IsCanceled)
                   {
                       operation.SetCancelled();
                   }
                   else if (loadTask.Exception != null)
                   {
                       operation.SetError(ExceptionHandlingUtility.GetUnwrappedException(loadTask.Exception));
                   }
                   else
                   {
                       operation.Complete(loadTask.Result);
                   }
               }
                , (object)loadOperation
                , CancellationToken.None
                , TaskContinuationOptions.HideScheduler
                , _syncContextScheduler);

            return loadOperation;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public LoadOperation Load(EntityQuery query, LoadBehavior loadBehavior, Action<LoadOperation> callback, object userState)
        {
            // Get MethodInfo for Load<TEntity>(EntityQuery<TEntity>, LoadBehavior, Action<LoadOperation<TEntity>>, object, LoadOperation<TEntity>)
            var method = new Func<EntityQuery<Entity>, LoadBehavior, Action<LoadOperation<Entity>>, object, LoadOperation<Entity>>(this.Load);
            var loadMethod = method.Method.GetGenericMethodDefinition();

            try
            {
                return (LoadOperation)loadMethod
                    .MakeGenericMethod(query.EntityType)
                    .Invoke(this, new object[] { query, loadBehavior, callback, userState });
            }
            catch (TargetInvocationException tie)
            {
                if (tie.InnerException != null)
                {
                    throw tie.InnerException;
                }
                throw;
            }
        }

        /// <summary>
        /// Initiates a load operation for the specified query.
        /// </summary>
        /// <param name="query">The query to invoke.</param>
        public Task<LoadResult<TEntity>> LoadAsync<TEntity>(EntityQuery<TEntity> query)
            where TEntity : Entity
        {
            return LoadAsync(query, LoadBehavior.KeepCurrent, CancellationToken.None);
        }

        /// <summary>
        /// Initiates a load operation for the specified query.
        /// </summary>
        /// <param name="query">The query to invoke.</param>
        /// <param name="cancellationToken">cancellation token</param>
        public Task<LoadResult<TEntity>> LoadAsync<TEntity>(EntityQuery<TEntity> query, CancellationToken cancellationToken)
            where TEntity : Entity
        {
            return LoadAsync(query, LoadBehavior.KeepCurrent, cancellationToken);
        }

        /// <summary>
        /// Initiates a load operation for the specified query.
        /// </summary>
        /// <param name="query">The query to invoke.</param>
        /// <param name="loadBehavior">The <see cref="LoadBehavior"/> to apply.</param>
        public Task<LoadResult<TEntity>> LoadAsync<TEntity>(EntityQuery<TEntity> query, LoadBehavior loadBehavior)
                where TEntity : Entity
        {
            return LoadAsync(query, loadBehavior, CancellationToken.None);
        }

        /// <summary>
        /// Initiates a load operation for the specified query.
        /// </summary>
        /// <param name="query">The query to invoke.</param>
        /// <param name="loadBehavior">The <see cref="LoadBehavior"/> to apply.</param>
        /// <param name="cancellationToken">cancellation token</param>
        public virtual Task<LoadResult<TEntity>> LoadAsync<TEntity>(EntityQuery<TEntity> query, LoadBehavior loadBehavior, CancellationToken cancellationToken)
                where TEntity : Entity
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            // verify the specified query was created by this DomainContext
            if (this.DomainClient != query.DomainClient)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DomainContext_InvalidEntityQueryDomainClient, query.QueryName));
            }

            this.IncrementLoadCount();

            // Proceed with query
            var domainClientTask = this.DomainClient.QueryAsync(query, cancellationToken);

            var continueationCts = new CancellationTokenSource();
            return domainClientTask.ContinueWith(result =>
            {
                IReadOnlyCollection<Entity> loadedEntities = null;
                List<Entity> allLoadedEntities = null;
                int totalCount;

                QueryCompletedResult results = null;
                try
                {
                    lock (this._syncRoot)
                    {
                        // The task is known to be completed so this will never block
                        results = result.GetAwaiter().GetResult();

                        // load the entities into the entity container
                        loadedEntities = this.EntityContainer.LoadEntities(results.Entities, loadBehavior);

                        var loadedIncludedEntities = this.EntityContainer.LoadEntities(results.IncludedEntities, loadBehavior);
                        allLoadedEntities = new List<Entity>(loadedEntities.Count + loadedIncludedEntities.Count);
                        allLoadedEntities.AddRange(loadedEntities);
                        allLoadedEntities.AddRange(loadedIncludedEntities);
                        totalCount = results.TotalCount;
                    }
                }
                catch (TaskCanceledException)
                {
                    continueationCts.Cancel();
                    throw new OperationCanceledException(continueationCts.Token);
                }
                catch (DomainException)
                {
                    // DomainExceptions should not be modified
                    throw;
                }
                catch (Exception ex)
                {
                    if (ex.IsFatal())
                    {
                        throw;
                    }

                    string message = string.Format(CultureInfo.CurrentCulture,
                        Resource.DomainContext_LoadOperationFailed,
                        query.QueryName, ex.Message);

                    throw ex is DomainOperationException domainOperationException
                        ? new DomainOperationException(message, domainOperationException)
                        : new DomainOperationException(message, ex);
                }
                finally
                {
                    this.DecrementLoadCount();
                }

                if (results.ValidationErrors.Any())
                {
                    string message = string.Format(CultureInfo.CurrentCulture,
  Resource.DomainContext_LoadOperationFailed_Validation,
  query.QueryName);
                    throw new DomainOperationException(message, results.ValidationErrors);
                }
                else
                {
                    return new LoadResult<TEntity>(query, loadBehavior, loadedEntities.Cast<TEntity>(), allLoadedEntities, totalCount);
                }
            }
            , continueationCts.Token
            , TaskContinuationOptions.HideScheduler
            , _syncContextScheduler);
        }

        /// <summary>
        /// Process the submit results by handling any validation or conflict errors, performing any required
        /// member auto-sync, etc. If there were no errors, all changes are accepted.
        /// </summary>
        /// <param name="changeSet">The submitted <see cref="EntityChangeSet"/>.</param>
        /// <param name="changeSetResults">The operation results returned from the submit request.</param>
        private static void ProcessSubmitResults(EntityChangeSet changeSet, IEnumerable<ChangeSetEntry> changeSetResults)
        {
            bool hasErrors = false;
            Dictionary<Entity, List<ValidationResult>> entityValidationErrorMap = new Dictionary<Entity, List<ValidationResult>>();
            foreach (ChangeSetEntry changeSetEntry in changeSetResults)
            {
                if (changeSetEntry.ValidationErrors != null && changeSetEntry.ValidationErrors.Any())
                {
                    hasErrors = true;
                    AddEntityErrors(changeSetEntry.ClientEntity, changeSetEntry.ValidationErrors, entityValidationErrorMap);
                }

                if (changeSetEntry.HasConflict)
                {
                    EntityConflict conflict = new EntityConflict(changeSetEntry.ClientEntity, changeSetEntry.StoreEntity, changeSetEntry.ConflictMembers, changeSetEntry.IsDeleteConflict);
                    changeSetEntry.ClientEntity.EntityConflict = conflict;
                    hasErrors = true;
                }
            }

            // If there were any errors we don't want to process any further
            if (hasErrors)
            {
                return;
            }

            // perform any member auto-synchronization
            ApplyMemberSynchronizations(changeSetResults);

            // Do deletes before inserts, such that the identity cache can deal with replaces.
            // Since AcceptChanges is recursive for composed children, we only call accept on top level parent entities or on children whose parents are not removed. 
            // This ensures that AcceptChanges is called once and only once for each removed entity.
            foreach (Entity entity in changeSet.RemovedEntities.Where(p => p.Parent == null || p.Parent.EntityState != EntityState.Deleted))
            {
                ((IRevertibleChangeTracking)entity).AcceptChanges();
            }

            // Accept changes for all entities in the completed changeset. We're not using Entities.AcceptChanges
            // since we only want to accept changes for this changeset (there might have been changes to other
            // entities since this changeset was submitted)
            foreach (Entity entity in changeSet.AddedEntities.Concat(changeSet.ModifiedEntities))
            {
                ((IRevertibleChangeTracking)entity).AcceptChanges();
            }
        }

        /// <summary>
        /// Add a list of <see cref="ValidationResult"/> to the entity's <see cref="Entity.ValidationErrors" /> collection.
        /// </summary>
        /// <param name="failedEntity">entity that has failed</param>
        /// <param name="errors">list of errors that have occurred during the operation</param>
        /// <param name="entityErrorMap">dictionary of accumulated entity error mapping</param>
        private static void AddEntityErrors(Entity failedEntity, IEnumerable<ValidationResultInfo> errors, Dictionary<Entity, List<ValidationResult>> entityErrorMap)
        {
            // We need to accumulate all the errors on an entity in the entityErrorMap Entity.ValidationErrors are IEnumerable. 
            Debug.Assert(failedEntity != null, "failedEntity should not be null");

            List<ValidationResult> entityErrors = null;
            if (!entityErrorMap.TryGetValue(failedEntity, out entityErrors))
            {
                entityErrors = errors.Select(e => new ValidationResult(e.Message, e.SourceMemberNames)).ToList();
                entityErrorMap[failedEntity] = entityErrors;

                ValidationUtilities.ApplyValidationErrors(failedEntity, entityErrors);
            }
            else
            {
                // entity was involved in multiple operations: only add the errors into Entity.ValidationErrors that are not already in the list
                foreach (ValidationResultInfo operationError in errors)
                {
                    ValidationResult validationResult = new ValidationResult(operationError.Message, operationError.SourceMemberNames);
                    if (!entityErrors.Contains<ValidationResult>(validationResult, new ValidationResultEqualityComparer()))
                    {
                        entityErrors.Add(validationResult);
                    }
                }
            }
        }

        /// <summary>
        /// Apply any member synchronizations specified in the results.
        /// </summary>
        /// <param name="changeSetResults">The operation results to process</param>
        private static void ApplyMemberSynchronizations(IEnumerable<ChangeSetEntry> changeSetResults)
        {
            // we apply member synchronizations for all operation types except
            // delete operations
            foreach (ChangeSetEntry changeSetEntry in changeSetResults.Where(p => p.Operation != EntityOperationType.Delete))
            {
                if (changeSetEntry.Entity != null)
                {
                    changeSetEntry.ClientEntity.Merge(changeSetEntry.Entity, LoadBehavior.RefreshCurrent);
                }
            }
        }

        /// <summary>
        /// Event handler for property changed events on our EntityContainer
        /// </summary>
        /// <param name="sender">The EntityContainer</param>
        /// <param name="e">The event args</param>
        private void EntityContainerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.CompareOrdinal(e.PropertyName, "HasChanges") == 0)
            {
                // just pass the event on
                this.RaisePropertyChanged(nameof(HasChanges));
            }
        }

        /// <summary>
        /// Completes an event-based asynchronous <see cref="SubmitChanges()"/> operation.
        /// </summary>
        private void CompleteSubmitChanges(Task<SubmitCompletedResult> submitTask, SubmitOperation submitOperation)
        {
            IEnumerable<ChangeSetEntry> operationResults = null;
            Exception error = null;

            try
            {
                // This needs to be inside try statement, since code in finally must run
                if (!submitTask.IsCanceled)
                {
                    SubmitCompletedResult submitResults = submitTask.GetAwaiter().GetResult();

                    // If the request was successful, process the results
                    ProcessSubmitResults(submitResults.ChangeSet, submitResults.Results.Cast<ChangeSetEntry>());

                    operationResults = submitResults.Results;
                }
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }
                error = ex;
            }
            finally
            {
                foreach (Entity entity in submitOperation.ChangeSet)
                {
                    entity.IsSubmitting = false;
                }
                this.IsSubmitting = false;
            }

            if (submitTask.IsCanceled)
            {
                submitOperation.SetCancelled();
            }
            else
            {
                if (error == null)
                {
                    if (operationResults.Any(p => p.HasError))
                    {
                        if (operationResults.Any(p => p.ValidationErrors != null && p.ValidationErrors.Any()))
                        {
                            submitOperation.SetError(OperationErrorStatus.ValidationFailed);
                        }
                        else if (operationResults.Any(p => p.HasConflict))
                        {
                            submitOperation.SetError(OperationErrorStatus.Conflicts);
                        }
                    }
                    else
                    {
                        // if we've completed successfully, all changes should have been accepted
                        Debug.Assert(!this.HasChanges, "Submit has completed. HasChanges must be false.");

                        submitOperation.Complete();
                    }
                }
                else
                {
                    submitOperation.SetError(error);
                }
            }
        }

        /// <summary>
        /// Initiates an Invoke operation.
        /// </summary>
        /// <typeparam name="TValue">The type of value that will be returned.</typeparam>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="returnType">The return Type of the operation.</param>
        /// <param name="parameters">Optional parameters to the operation. Specify null
        /// if the operation takes no parameters.</param>
        /// <param name="hasSideEffects">True if the operation has side-effects, false otherwise.</param>
        /// <param name="callback">Optional callback to be called when the operation completes.</param>
        /// <param name="userState">Optional user state for the operation.</param>
        /// <returns>The invoke operation.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual InvokeOperation<TValue> InvokeOperation<TValue>(string operationName, Type returnType, IDictionary<string, object> parameters, bool hasSideEffects, Action<InvokeOperation<TValue>> callback, object userState)
        {
            if (string.IsNullOrEmpty(operationName))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resource.Parameter_NullOrEmpty, "operationName"));
            }
            if (returnType == null)
            {
                throw new ArgumentNullException(nameof(returnType));
            }

            InvokeOperation<TValue> invokeOperation = new InvokeOperation<TValue>(operationName, parameters, callback, userState, this.DomainClient.SupportsCancellation);
            InvokeOperationAsync<TValue>(operationName, parameters, hasSideEffects, returnType, invokeOperation.CancellationToken)
                 .ContinueWith((loadTask, state) =>
                 {
                     var operation = (InvokeOperation<TValue>)state;

                     if (loadTask.IsCanceled)
                     {
                         operation.SetCancelled();
                     }
                     else if (loadTask.Exception != null)
                     {
                         operation.SetError(ExceptionHandlingUtility.GetUnwrappedException(loadTask.Exception));
                     }
                     else
                     {
                         operation.Complete(loadTask.Result);
                     }
                 }
                , (object)invokeOperation
                , CancellationToken.None
                , TaskContinuationOptions.HideScheduler
                , _syncContextScheduler);

            return invokeOperation;
        }

        /// <summary>
        /// Invokes an invoke operation.
        /// </summary>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="returnType">The return Type of the operation.</param>
        /// <param name="parameters">Optional parameters to the operation. Specify null
        /// if the operation takes no parameters.</param>
        /// <param name="hasSideEffects">True if the operation has side-effects, false otherwise.</param>
        /// <param name="callback">Optional callback to be called when the operation completes.</param>
        /// <param name="userState">Optional user state for the operation.</param>
        /// <returns>The invoke operation.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public InvokeOperation InvokeOperation(string operationName, Type returnType, IDictionary<string, object> parameters, bool hasSideEffects, Action<InvokeOperation> callback, object userState)
        {
            // We only expect void types for generated code
            // Use InvokeOperation<object> return type for these
            if (returnType == typeof(void) || returnType == typeof(object))
            {
                return InvokeOperation<object>(operationName, returnType, parameters, hasSideEffects, callback, userState);
            }
            else
            {
                try
                {
                    return (InvokeOperation)s_invokeOperationAsync
                        .MakeGenericMethod(returnType)
                        .Invoke(this, new object[] { operationName, returnType, parameters, hasSideEffects, callback, userState });
                }
                catch (TargetInvocationException tie)
                {
                    if (tie.InnerException != null)
                    {
                        throw tie.InnerException;
                    }
                    throw;
                }
            }
        }

        /// <summary>
        /// Invokes an invoke operation.
        /// </summary>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="parameters">Optional parameters to the operation. Specify null
        /// if the operation takes no parameters.</param>
        /// <param name="hasSideEffects">True if the operation has side-effects, false otherwise.</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>The invoke operation.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<InvokeResult> InvokeOperationAsync(string operationName,
            IDictionary<string, object> parameters, bool hasSideEffects,
            CancellationToken cancellationToken)
        {
            // Do not do use await since parameter validation are not thrown instantly
            return InvokeOperationAsync<object>(operationName, parameters, hasSideEffects, typeof(void), cancellationToken)
                .ContinueWith(res =>
                {
                    return (InvokeResult)res.GetAwaiter().GetResult();
                }
                , CancellationToken.None
                , TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.ExecuteSynchronously
                , TaskScheduler.Default);
        }

        /// <summary>
        /// Invokes an invoke operation.
        /// </summary>
        /// <typeparam name="TValue">The type of value that will be returned.</typeparam>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="parameters">Optional parameters to the operation. Specify null
        /// if the operation takes no parameters.</param>
        /// <param name="hasSideEffects">True if the operation has side-effects, false otherwise.</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>The invoke operation.</returns>
        public Task<InvokeResult<TValue>> InvokeOperationAsync<TValue>(string operationName,
            IDictionary<string, object> parameters, bool hasSideEffects,
            CancellationToken cancellationToken)
        {
            return InvokeOperationAsync<TValue>(operationName, parameters, hasSideEffects, typeof(TValue), cancellationToken);
        }

        protected virtual Task<InvokeResult<TValue>> InvokeOperationAsync<TValue>(string operationName,
            IDictionary<string, object> parameters, bool hasSideEffects,
            Type returnType,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(operationName))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resource.Parameter_NullOrEmpty, "operationName"));
            }

            if (returnType is null)
            {
                throw new ArgumentNullException(nameof(returnType));
            }

            InvokeArgs invokeArgs = new InvokeArgs(operationName, typeof(TValue), parameters, hasSideEffects);
            return this.DomainClient.InvokeAsync(invokeArgs, cancellationToken)
                                    .ContinueWith((Task<InvokeCompletedResult> task, object state) =>
            {
                InvokeCompletedResult results;
                string operation = (string)state;
                try
                {
                    results = task.GetAwaiter().GetResult();
                }
                catch (DomainException)
                {
                    // DomainExceptions should not be modified
                    throw;
                }
                catch (Exception ex)
                {
                    if (ex.IsFatal())
                    {
                        throw;
                    }
                    string message = string.Format(CultureInfo.CurrentCulture,
               Resource.DomainContext_InvokeOperationFailed,
               operation, ex.Message);

                    throw ex is DomainOperationException domainOperationException
                        ? new DomainOperationException(message, domainOperationException)
                        : new DomainOperationException(message, ex);
                }

                if (results.ValidationErrors.Count == 0)
                {
                    return new InvokeResult<TValue>((TValue)results.ReturnValue);
                }
                else
                {
                    string message = string.Format(CultureInfo.CurrentCulture,
             Resource.DomainContext_InvokeOperationFailed_Validation,
             operation);
                    throw new DomainOperationException(message, results.ValidationErrors);
                }
            }
            , operationName
            , CancellationToken.None
            , TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.HideScheduler
            , _syncContextScheduler);
        }

        /// <summary>
        /// Raises the PropertyChanged event for the specified property
        /// </summary>
        /// <param name="propertyName">The property to raise the event for</param>
        protected void RaisePropertyChanged(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void IncrementLoadCount()
        {
            Debug.Assert(this._activeLoadCount >= 0, "Load count should never be less than zero.");

            Interlocked.Increment(ref this._activeLoadCount);

            if (this._activeLoadCount == 1)
            {
                this.RaisePropertyChanged(nameof(IsLoading));
            }
        }

        private void DecrementLoadCount()
        {
            Debug.Assert(this._activeLoadCount > 0, "Load count out of sync.");

            Interlocked.Decrement(ref this._activeLoadCount);

            if (this._activeLoadCount == 0)
            {
                this.RaisePropertyChanged(nameof(IsLoading));
            }
        }

        /// <summary>
        /// Validates a method call.
        /// </summary>
        /// <param name="methodName">The method to validate.</param>
        /// <param name="parameters">The parameters to the method.</param>
        protected void ValidateMethod(string methodName, IDictionary<string, object> parameters)
        {
            if (string.IsNullOrEmpty(methodName))
            {
                throw new ArgumentNullException(nameof(methodName));
            }

            object[] paramValues = parameters != null ? parameters.Values.ToArray() : Array.Empty<object>();
            if (!this.MethodRequiresValidation(methodName, paramValues))
            {
                // method validation is expensive, so skip it if we can
                return;
            }

            // validate the method call, including all parameters
            ValidationContext validationContext = ValidationUtilities.CreateValidationContext(this, this.ValidationContext);
            ValidationUtilities.ValidateMethodCall(methodName, validationContext, paramValues);
        }

        /// <summary>
        /// Validates the specified change-set.
        /// </summary>
        /// <param name="changeSet">The change-set to validate.</param>
        /// <param name="validationContext">The ValidationContext to use.</param>
        /// <returns>True if the change-set is valid, false otherwise.</returns>
        private bool ValidateChangeSet(EntityChangeSet changeSet, ValidationContext validationContext)
        {
            if (!changeSet.Validate(validationContext))
            {
                return false;
            }

            // Validate named update methods against the context-specific methods. A mismatch means
            // the user called the wrong context method on shared entity.
            foreach (Entity entity in changeSet.ModifiedEntities)
            {
                foreach (var customMethod in entity.EntityActions)
                {
                    try
                    {
                        // TODO: REVIEW and see if we should perform validation against the entity instead
                        // ValidationUtilities.GetMethod(entity, entityAction.Name, entityAction.parameters);

                        // DomainContext custom methods always differ from the entity version because
                        // the first param is the entity. Ensure the entity is the first param in the list.
                        object[] parameters = new object[customMethod.Parameters.Count() + 1];
                        parameters[0] = entity;
                        int i = 1;
                        foreach (var parameter in customMethod.Parameters)
                        {
                            parameters[i++] = parameter;
                        }

                        // Validate the method exists.
                        ValidationUtilities.GetMethod(this, customMethod.Name, parameters);
                    }
                    catch (MissingMethodException innerException)
                    {
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DomainContext_NamedUpdateMethodDoesNotExist, customMethod.Name, entity.GetType(), this.GetType()), innerException);
                    }

                }
            }

            return true;
        }

        /// <summary>
        /// Determines if the specified method on this DomainContext requires validation. Since
        /// validation is expensive, we want to skip it for methods not requiring it.
        /// </summary>
        /// <param name="methodName">The method to check</param>
        /// <param name="paramValues">The parameter values</param>
        /// <returns>True if the method requires validation, false otherwise.</returns>
        private bool MethodRequiresValidation(string methodName, object[] paramValues)
        {
            bool requiresValidation = false;
            if (!this.requiresValidationMap.TryGetValue(methodName, out requiresValidation))
            {
                MethodInfo method = ValidationUtilities.GetMethod(this, methodName, paramValues);
                requiresValidation = ValidationUtilities.MethodRequiresValidation(method);
                this.requiresValidationMap[methodName] = requiresValidation;
            }
            return requiresValidation;
        }

        /// <summary>
        /// Creates a domain client factory to use, in case the user has not set the <see cref="DomainClientFactory"/> property.
        /// </summary>
        /// <returns>A WebDomainClientFactory if found otherwise a <see cref="DefaultDomainClientFactory"/></returns>
        private static IDomainClientFactory CreateDomainClientFactory()
        {
            // 1; Check if any known DomainClientFactory can be found

            // Check for DomainClient in OpenRiaServices.DomainServices.Client.Web assembly
            var typeName = "OpenRiaServices.DomainServices.Client.WebDomainClientFactory, "
                                    + typeof(DomainClient).Assembly.FullName.Replace("OpenRiaServices.DomainServices.Client", "OpenRiaServices.DomainServices.Client.Web");
            var webDomainClientFactoryType = Type.GetType(typeName);
            if (webDomainClientFactoryType != null)
                return (IDomainClientFactory)Activator.CreateInstance(webDomainClientFactoryType);

            // Fallback to default implementation, this should only ever happening if the user is using a
            // an up-to-date version if the client library but an old version of the Client.Web assembly
            return new DefaultDomainClientFactory();
        }
    }
}
