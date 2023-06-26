using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.Client
{
    /// <summary>
    /// Represents an asynchronous submit operation
    /// </summary>
    public sealed class SubmitOperation : OperationBase
    {
        private readonly EntityChangeSet _changeSet;
        private readonly Action<SubmitOperation> _completeAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubmitOperation"/> class.
        /// </summary>
        /// <param name="changeSet">The changeset being submitted.</param>
        /// <param name="completeAction">Optional action to invoke when the operation completes.</param>
        /// <param name="userState">Optional user state to associate with the operation.</param>
        /// <param name="sumitResultTask">Task which, when completed, will Complete the operation and set result, cancelled or error</param>
        /// <param name="cancellationTokenSource"><see cref="CancellationTokenSource"/> which will be used to request cancellation if <see cref="OperationBase.Cancel()"/> is called, if <c>null</c> then cancellation will not be possible</param>
        public SubmitOperation(EntityChangeSet changeSet,
            Action<SubmitOperation> completeAction, object userState,
            Task<SubmitResult> sumitResultTask, CancellationTokenSource cancellationTokenSource)
            : base(userState, cancellationTokenSource)
        {
            if (changeSet == null)
            {
                throw new ArgumentNullException(nameof(changeSet));
            }
            this._completeAction = completeAction;
            this._changeSet = changeSet;

            if (sumitResultTask.IsCompleted)
                CompleteTask(sumitResultTask);
            else
            {
                sumitResultTask.ContinueWith(static (task, state) =>
                {
                    var operation = (SubmitOperation)state;
                    operation.CompleteTask(task);
                }
                , (object)this
                , CancellationToken.None
                , TaskContinuationOptions.HideScheduler
                , CurrentSynchronizationContextTaskScheduler);
            }
        }

        internal void CompleteTask(Task<SubmitResult> task)
        {
            if (task.IsCanceled)
                base.SetCancelled();
            else if (task.Exception != null)
                base.SetError(ExceptionHandlingUtility.GetUnwrappedException(task.Exception));
            else
                base.Complete(null);
        }

        /// <summary>
        /// The changeset being submitted.
        /// </summary>
        public EntityChangeSet ChangeSet
        {
            get
            {
                return this._changeSet;
            }
        }

        /// <summary>
        /// Returns any entities in error after the submit operation completes.
        /// </summary>
        public IEnumerable<Entity> EntitiesInError
        {
            get
            {
                return this._changeSet.Where(p => p.EntityConflict != null || p.HasValidationErrors);
            }
        }

        /// <summary>
        /// Invoke the completion callback.
        /// </summary>
        protected override void InvokeCompleteAction()
        {
            this._completeAction?.Invoke(this);
        }
    }
}
