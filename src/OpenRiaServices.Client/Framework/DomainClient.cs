using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.Client
{
    /// <summary>
    /// Base class for all <see cref="DomainClient"/> implementations. A <see cref="DomainClient"/> is
    /// used to communicate with a DomainService asynchronously, providing query, method invocation
    /// and changeset submission functionality.
    /// </summary>
    public abstract class DomainClient
    {
        private Type[] _entityTypes;

        /// <summary>
        /// Gets or sets the collection of Entity Types this <see cref="DomainClient"/> will operate on.
        /// </summary>
        public IEnumerable<Type> EntityTypes
        {
            get
            {
                return this._entityTypes;
            }
            set
            {
                if (this._entityTypes != null)
                {
                    throw new InvalidOperationException(OpenRiaServices.Client.Resource.DomainClient_EntityTypesAlreadyInitialized);
                }
                this._entityTypes = value.ToArray();
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="DomainClient"/> supports cancellation.
        /// </summary>
        public virtual bool SupportsCancellation
        {
            get
            {
                // By default cancellation is not supported.
                return false;
            }
        }

        /// <summary>
        /// Executes an asynchronous query operation.
        /// </summary>
        /// <param name="query">The query to invoke.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> which may be used to request cancellation</param>
        /// <returns>The results returned by the query.</returns>
        /// <remarks>
        /// Queries with side-effects may be invoked differently. For example, clients that invoke a DomainService 
        /// over HTTP may use POST requests for queries with side-effects, while GET may be used otherwise.
        /// </remarks>
        public Task<QueryCompletedResult> QueryAsync(EntityQuery query, CancellationToken cancellationToken) => QueryAsyncCore(query, cancellationToken);

        /// <summary>
        /// Method called by the framework to begin the asynchronous query operation.
        /// </summary>
        /// <param name="query">The query to invoke.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> which may be used to request cancellation</param>
        /// <returns>The results returned by the query.</returns>

        protected abstract Task<QueryCompletedResult> QueryAsyncCore(EntityQuery query, CancellationToken cancellationToken);


        /// <summary>
        /// Submits the specified <see cref="EntityChangeSet"/> to the DomainService asynchronously.
        /// </summary>
        /// <param name="changeSet">The <see cref="EntityChangeSet"/> to submit to the DomainService.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> which may be used to request cancellation</param>
        /// <returns>The results returned by the submit request.</returns>
        public Task<SubmitCompletedResult> SubmitAsync(EntityChangeSet changeSet, CancellationToken cancellationToken)
        {
            if (changeSet == null)
            {
                throw new ArgumentNullException(nameof(changeSet));
            }

            if (changeSet.IsEmpty)
            {
                throw new InvalidOperationException(OpenRiaServices.Client.Resource.DomainClient_EmptyChangeSet);
            }

            // call the actual implementation 
            var submitTask = SubmitAsyncCore(changeSet, cancellationToken);
            return submitTask.ContinueWith(res =>
            {
                var submitResults = res.GetAwaiter().GetResult();

                // correlate the operation results back to their actual client entity references
                Dictionary<int, Entity> submittedEntities = submitResults.ChangeSet.GetChangeSetEntries().ToDictionary(p => p.Id, p => p.Entity);
                foreach (ChangeSetEntry op in submitResults.Results)
                {
                    op.ClientEntity = submittedEntities[op.Id];
                }

                return submitResults;
            }
            , CancellationToken.None
            , TaskContinuationOptions.NotOnCanceled
            , TaskScheduler.Default);
        }

        /// <summary>
        /// Method called by the framework to asynchronously process the specified <see cref="EntityChangeSet"/>.
        /// Overrides should not call the base method.
        /// </summary>
        /// <param name="changeSet">The <see cref="EntityChangeSet"/> to submit to the DomainService.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> which may be used to request cancellation</param>
        /// <returns>The results returned by the submit request.</returns>
        protected virtual Task<SubmitCompletedResult> SubmitAsyncCore(EntityChangeSet changeSet, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }


        /// <summary>
        /// Invokes an operation asynchronously.
        /// </summary>
        /// <param name="invokeArgs">The arguments to the Invoke operation.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> which may be used to request cancellation</param>
        /// <returns>The results returned by the invocation.</returns>
        public Task<InvokeCompletedResult> InvokeAsync(InvokeArgs invokeArgs, CancellationToken cancellationToken) => InvokeAsyncCore(invokeArgs, cancellationToken);

        /// <summary>
        /// Method called by the framework to begin an Invoke operation asynchronously. Overrides
        /// should not call the base method.
        /// </summary>
        /// <param name="invokeArgs">The arguments to the Invoke operation.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> which may be used to request cancellation</param>
        /// <returns>The results returned by the invocation.</returns>
        protected abstract Task<InvokeCompletedResult> InvokeAsyncCore(InvokeArgs invokeArgs, CancellationToken cancellationToken);
    }
}
