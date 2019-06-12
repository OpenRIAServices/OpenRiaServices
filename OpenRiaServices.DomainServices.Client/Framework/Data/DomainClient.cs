using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.DomainServices.Client
{
    /// <summary>
    /// Base class for all <see cref="DomainClient"/> implementations. A <see cref="DomainClient"/> is
    /// used to communicate with a DomainService asynchronously, providing query, method invocation
    /// and changeset submission functionality.
    /// </summary>
    public abstract class DomainClient
    {
        private ReadOnlyCollection<Type> _entityTypes;

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
                    throw new InvalidOperationException(OpenRiaServices.DomainServices.Client.Resource.DomainClient_EntityTypesAlreadyInitialized);
                }
                this._entityTypes = new ReadOnlyCollection<Type>(value.ToList());
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
                throw new InvalidOperationException(OpenRiaServices.DomainServices.Client.Resource.DomainClient_EmptyChangeSet);
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
            });
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
        /// <param name="callback">The callback to invoke when the invocation has been completed.</param>
        /// <param name="userState">Optional user state associated with this operation.</param>
        /// <returns>An asynchronous result that identifies this invocation.</returns>
        public IAsyncResult BeginInvoke(InvokeArgs invokeArgs, AsyncCallback callback, object userState)
        {
            if (invokeArgs == null)
            {
                throw new ArgumentNullException("invokeArgs");
            }
            if (callback == null)
            {
                throw new ArgumentNullException("callback");
            }

            DomainClientAsyncResult domainClientResult = DomainClientAsyncResult.CreateInvokeResult(this, invokeArgs, callback, userState);

            domainClientResult.InnerAsyncResult = this.BeginInvokeCore(
                invokeArgs,
                delegate (IAsyncResult result)
                {
                    DomainClientAsyncResult clientResult = (DomainClientAsyncResult)result.AsyncState;
                    clientResult.InnerAsyncResult = result;
                    clientResult.Complete();
                },
                domainClientResult);

            return domainClientResult;
        }

        /// <summary>
        /// Method called by the framework to begin an Invoke operation asynchronously. Overrides
        /// should not call the base method.
        /// </summary>
        /// <param name="invokeArgs">The arguments to the Invoke operation.</param>
        /// <param name="callback">The callback to invoke when the invocation has been completed.</param>
        /// <param name="userState">Optional user state associated with this operation.</param>
        /// <returns>An asynchronous result that identifies this invocation.</returns>
        protected virtual IAsyncResult BeginInvokeCore(InvokeArgs invokeArgs, AsyncCallback callback, object userState)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Attempts to cancel the invocation request specified by the <paramref name="asyncResult"/>.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> specifying what invocation operation to cancel.</param>
        /// <exception cref="ArgumentNullException"> if <paramref name="asyncResult"/> is null.</exception>
        /// <exception cref="ArgumentException"> if <paramref name="asyncResult"/> is for another operation or was not created by this <see cref="DomainClient"/> instance.</exception>
        /// <exception cref="InvalidOperationException"> if the operation associated with <paramref name="asyncResult"/> has been canceled.</exception>
        /// <exception cref="InvalidOperationException"> if the operation associated with <paramref name="asyncResult"/> has completed.</exception>
        public void CancelInvoke(IAsyncResult asyncResult)
        {
            this.VerifyCancellationSupport();

            DomainClientAsyncResult domainClientResult = this.EndAsyncResult(asyncResult, AsyncOperationType.Invoke, true /* cancel */);
            this.CancelInvokeCore(domainClientResult.InnerAsyncResult);
        }

        /// <summary>
        /// Attempts to cancel the invocation request specified by the <paramref name="asyncResult"/>.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> specifying what invocation operation to cancel.</param>
        protected virtual void CancelInvokeCore(IAsyncResult asyncResult)
        {
            // Default implementation does nothing.
            return;
        }

        /// <summary>
        /// Completes an operation invocation.
        /// </summary>
        /// <param name="asyncResult">An asynchronous result that identifies an invocation.</param>
        /// <returns>The results returned by the invocation.</returns>
        public InvokeCompletedResult EndInvoke(IAsyncResult asyncResult)
        {
            DomainClientAsyncResult domainClientResult = this.EndAsyncResult(asyncResult, AsyncOperationType.Invoke, false /* cancel */);
            return this.EndInvokeCore(domainClientResult.InnerAsyncResult);
        }

        /// <summary>
        /// Method called by the framework to complete an asynchronous invocation
        /// </summary>
        /// <param name="asyncResult">An asynchronous result that identifies an invocation.</param>
        /// <returns>The results returned by the invocation.</returns>
        protected abstract InvokeCompletedResult EndInvokeCore(IAsyncResult asyncResult);

        /// <summary>
        /// Transitions an <see cref="IAsyncResult"/> instance to a completed state.
        /// </summary>
        /// <param name="asyncResult">An asynchronous result that identifies an invocation.</param>
        /// <param name="operationType">The expected operation type.</param>
        /// <param name="cancel">Boolean indicating whether or not the operation has been canceled.</param>
        /// <returns>A <see cref="DomainClientAsyncResult"/> reference.</returns>
        /// <exception cref="ArgumentNullException"> if <paramref name="asyncResult"/> is null.</exception>
        /// <exception cref="ArgumentException"> if <paramref name="asyncResult"/> is for another operation or was not created by this <see cref="DomainClient"/> instance.</exception>
        /// <exception cref="InvalidOperationException"> if <paramref name="asyncResult"/> has been canceled.</exception>
        /// <exception cref="InvalidOperationException"> if <paramref name="asyncResult"/>'s End* method has already been invoked.</exception>
        /// <exception cref="InvalidOperationException"> if <paramref name="asyncResult"/> has not completed.</exception>
        private DomainClientAsyncResult EndAsyncResult(IAsyncResult asyncResult, AsyncOperationType operationType, bool cancel)
        {
            DomainClientAsyncResult domainClientResult = asyncResult as DomainClientAsyncResult;

            if ((domainClientResult != null) && (!object.ReferenceEquals(this, domainClientResult.DomainClient) || domainClientResult.AsyncOperationType != operationType))
            {
                throw new ArgumentException(Resources.WrongAsyncResult, "asyncResult");
            }

            return AsyncResultBase.EndAsyncOperation<DomainClientAsyncResult>(asyncResult, cancel);
        }

        /// <summary>
        /// Throws an exception if cancellation is not supported.
        /// </summary>
        private void VerifyCancellationSupport()
        {
            if (!this.SupportsCancellation)
            {
                throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, OpenRiaServices.DomainServices.Client.Resource.DomainClient_CancellationNotSupported, this.GetType().FullName));
            }
        }
    }
}
