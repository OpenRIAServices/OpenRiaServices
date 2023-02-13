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
using OpenRiaServices.Client.Data;

namespace OpenRiaServices.Client
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
        private ValidationContext _validationContext;
        private bool _isSubmitting;
        private readonly Dictionary<string, bool> _requiresValidationMap = new Dictionary<string, bool>();
        private readonly object _syncRoot = new object();
        private static IDomainClientFactory s_domainClientFactory;

        private TaskScheduler CurrrentSynchronizationContextTaskScheduler => SynchronizationContext.Current != null ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Default;

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
        /// <exception cref="System.InvalidOperationException">If trying to get a DomainClientFactory without having one set</exception>
        public static IDomainClientFactory DomainClientFactory
        {
            get
            {
                return s_domainClientFactory ?? throw new InvalidOperationException(Resource.DomainContext_DomainClientFactoryNotSet);
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
            EntityChangeSet changeSet = this.EntityContainer.GetChanges();
            CancellationTokenSource cts = DomainClient.SupportsCancellation ? new CancellationTokenSource() : null;

            var submitTask = SubmitChangesAsync(cts?.Token ?? CancellationToken.None);
            return new SubmitOperation(changeSet, callback, userState, submitTask, cts);
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
        public Task<SubmitResult> SubmitChangesAsync(CancellationToken cancellationToken)
        {
            EntityChangeSet changeSet = this.EntityContainer.GetChanges();
            return SubmitChangesAsync(changeSet, cancellationToken);
        }

        /// <summary>
        /// Submit the specifed pending changes to the DomainService asyncronously.
        /// All submit operations from <see cref="SubmitChanges()"/> and <see cref="SubmitChangesAsync()"/>
        /// will call this which can be overriden for extensibility purposes.
        /// </summary>
        /// <returns>The <see cref="SubmitResult"/>.</returns>
        /// <param name="changeSet">the changes to save</param>
        /// <param name="cancellationToken">cancellation token</param>
        protected virtual Task<SubmitResult> SubmitChangesAsync(EntityChangeSet changeSet, CancellationToken cancellationToken)
        {
            if (changeSet.IsEmpty)
            {
                return Task.FromResult(new SubmitResult(changeSet));
            }

            if (this.IsSubmitting)
            {
                throw new InvalidOperationException(Resource.DomainContext_SubmitAlreadyInProgress);
            }

            // validate the changeset, this might throw InvalidOperation
            if (!this.ValidateChangeSet(changeSet, this.ValidationContext))
            {
                return Task.FromException<SubmitResult>(new SubmitOperationException(changeSet, Resource.DomainContext_SubmitOperationFailed_Validation, OperationErrorStatus.ValidationFailed));
            }

            // Build the changeset entries, this will cause additional validation to run
            // The result is cached so when the DomainClient calls the method again the results are reused
            changeSet.GetChangeSetEntries();

            // Set state
            this.IsSubmitting = true;
            try
            {
                // Prevent any changes to the entities while we are submitting.
                foreach (Entity entity in changeSet)
                {
                    entity.IsSubmitting = true;
                }

                var domainClientTask = this.DomainClient.SubmitAsync(changeSet, cancellationToken);
                return SubmitChangesAsyncImplementation(domainClientTask);
            }
            catch
            {
                // if an exception is thrown revert our state
                foreach (Entity entity in changeSet)
                {
                    entity.IsSubmitting = false;
                }
                this.IsSubmitting = false;
                throw;
            }

            async Task<SubmitResult> SubmitChangesAsyncImplementation(Task<SubmitCompletedResult> submitTask)
            {
                try
                {
                    bool hasValidationErros = false;
                    bool hasConflict = false;
                    try
                    {
                        SubmitCompletedResult submitResults = await submitTask.ConfigureAwait(true);

                        // If the request was successful, process the results
                        ProcessSubmitResults(submitResults.ChangeSet, submitResults.Results, out hasValidationErros, out hasConflict);
                    }
                    catch (Exception ex) when (!(ex is DomainException || ex is OperationCanceledException || ex.IsFatal()))
                    {
                        string message = string.Format(CultureInfo.CurrentCulture, Resource.DomainContext_SubmitOperationFailed, ex.Message);
                        throw ex is DomainOperationException domainOperationException
                            ? new SubmitOperationException(changeSet, message, domainOperationException)
                            : new SubmitOperationException(changeSet, message, ex);
                    }

                    if (hasValidationErros)
                    {
                        throw new SubmitOperationException(changeSet, Resource.DomainContext_SubmitOperationFailed_Validation, OperationErrorStatus.ValidationFailed);
                    }
                    else if (hasConflict)
                    {
                        throw new SubmitOperationException(changeSet, Resource.DomainContext_SubmitOperationFailed_Conflicts, OperationErrorStatus.Conflicts);
                    }
                    else
                    {
                        return new SubmitResult(changeSet);
                    }
                }
                finally
                {
                    // if an exception is thrown revert our state
                    foreach (Entity entity in changeSet)
                    {
                        entity.IsSubmitting = false;
                    }
                    this.IsSubmitting = false;
                }
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
            CancellationTokenSource cts = DomainClient.SupportsCancellation ? new CancellationTokenSource() : null;

            var loadResult = LoadAsync(query, loadBehavior, cts?.Token ?? CancellationToken.None);
            return new LoadOperation<TEntity>(query, loadBehavior, callback, userState, loadResult, cts);
        }

        /// <summary>
        /// Initiates a load operation for the specified query without having to now the type at compile time.
        /// </summary>
        /// <param name="query">The query to invoke.</param>
        /// <param name="loadBehavior">The <see cref="LoadBehavior"/> to apply.</param>
        /// <param name="callback">Optional callback to be called when the load operation completes.</param>
        /// <param name="userState">Optional user state.</param>
        /// <returns>The load operation.</returns>
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
            try
            {
                // Proceed with query
                var domainClientTask = this.DomainClient.QueryAsync(query, cancellationToken);
                return LoadAsyncImplementation(domainClientTask);
            }
            catch (Exception)
            {
                DecrementLoadCount();
                throw;
            }

            async Task<LoadResult<TEntity>> LoadAsyncImplementation(Task<QueryCompletedResult> queryCompletedResult)
            {
                IReadOnlyCollection<Entity> loadedEntities = null;
                IReadOnlyCollection<Entity> loadedIncludedEntities = null;
                List<Entity> allLoadedEntities = null;
                int totalCount;

                QueryCompletedResult results = null;
                try
                {
                    // The task is known to be completed so this will never block
                    results = await queryCompletedResult.ConfigureAwait(true);
                    lock (this._syncRoot)
                    {
                        // load the entities into the entity container
                        loadedEntities = this.EntityContainer.LoadEntities(results.Entities, loadBehavior);
                        loadedIncludedEntities = this.EntityContainer.LoadEntities(results.IncludedEntities, loadBehavior);
                    }

                    allLoadedEntities = new List<Entity>(loadedEntities.Count + loadedIncludedEntities.Count);
                    allLoadedEntities.AddRange(loadedEntities);
                    allLoadedEntities.AddRange(loadedIncludedEntities);
                    totalCount = results.TotalCount;
                }
                catch (Exception ex) when (!(ex is DomainException || ex is OperationCanceledException || ex.IsFatal()))
                {
                    string message = string.Format(Resource.DomainContext_LoadOperationFailed, query.QueryName, ex.Message);

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
                    string message = string.Format(Resource.DomainContext_LoadOperationFailed_Validation, query.QueryName);
                    throw new DomainOperationException(message, results.ValidationErrors);
                }
                else
                {
                    return new LoadResult<TEntity>(query, loadBehavior, loadedEntities.Cast<TEntity>(), allLoadedEntities, totalCount);
                }
            }
        }

        /// <summary>
        /// Process the submit results by handling any validation or conflict errors, performing any required
        /// member auto-sync, etc. If there were no errors, all changes are accepted.
        /// </summary>
        /// <param name="changeSet">The submitted <see cref="EntityChangeSet"/>.</param>
        /// <param name="changeSetResults">The operation results returned from the submit request.</param>
        /// <param name="hasValidationErros">set to <c>true</c> if processing was aborted due to finding validation errors</param>
        /// <param name="hasConflict">set to <c>true</c> if processing was aborted due to finding conflicts</param>
        private static void ProcessSubmitResults(EntityChangeSet changeSet, IEnumerable<ChangeSetEntry> changeSetResults, out bool hasValidationErros, out bool hasConflict)
        {
            hasValidationErros = false;
            hasConflict = false;
            Dictionary<Entity, List<ValidationResult>> entityValidationErrorMap = new Dictionary<Entity, List<ValidationResult>>();
            foreach (ChangeSetEntry changeSetEntry in changeSetResults)
            {
                if (changeSetEntry.ValidationErrors != null && changeSetEntry.ValidationErrors.Any())
                {
                    hasValidationErros = true;
                    AddEntityErrors(changeSetEntry.ClientEntity, changeSetEntry.ValidationErrors, entityValidationErrorMap);
                }

                if (changeSetEntry.HasConflict)
                {
                    EntityConflict conflict = new EntityConflict(changeSetEntry.ClientEntity, changeSetEntry.StoreEntity, changeSetEntry.ConflictMembers, changeSetEntry.IsDeleteConflict);
                    changeSetEntry.ClientEntity.EntityConflict = conflict;
                    hasConflict = true;
                }
            }

            // If there were any errors we don't want to process any further
            if (hasValidationErros || hasConflict)
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

            List<ValidationResult> entityErrors;
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
            CancellationTokenSource cts = DomainClient.SupportsCancellation ? new CancellationTokenSource() : null;

            var invokeResult = InvokeOperationAsync<TValue>(operationName, parameters, hasSideEffects, returnType, cts?.Token ?? CancellationToken.None);

            return new InvokeOperation<TValue>(operationName, parameters, callback, userState, invokeResult, cts);
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

        /// <summary>
        /// Invokes an invoke operation.
        /// </summary>
        /// <typeparam name="TValue">The type of value that will be returned.</typeparam>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="parameters">Optional parameters to the operation. Specify null
        /// if the operation takes no parameters.</param>
        /// <param name="hasSideEffects">True if the operation has side-effects, false otherwise.</param>
        /// <param name="returnType">The return Type of the operation.</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>The invoke operation.</returns>
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
                catch (Exception ex) when (!(ex is DomainException || ex.IsFatal()))
                {
                    string message = string.Format(Resource.DomainContext_InvokeOperationFailed, operation, ex.Message);

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
                    string message = string.Format(Resource.DomainContext_InvokeOperationFailed_Validation, operation);
                    throw new DomainOperationException(message, results.ValidationErrors);
                }
            }
            , operationName
            , CancellationToken.None
            , TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.HideScheduler
            , CurrrentSynchronizationContextTaskScheduler);
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
            lock(this._syncRoot)
            {
                if (!this._requiresValidationMap.TryGetValue(methodName, out bool requiresValidation))
                {
                    MethodInfo method = ValidationUtilities.GetMethod(this, methodName, paramValues);
                    requiresValidation = ValidationUtilities.MethodRequiresValidation(method);
                    this._requiresValidationMap[methodName] = requiresValidation;
                }
                return requiresValidation;
            }
        }
    }
}
