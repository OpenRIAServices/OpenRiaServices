using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRiaServices.DomainServices.Client
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
        /// <param name="supportCancellation"><c>true</c> to enable <see cref="OperationBase.CancellationToken"/> to be cancelled when <see cref="OperationBase.Cancel"/> is called</param>
        internal SubmitOperation(EntityChangeSet changeSet,
            Action<SubmitOperation> completeAction, object userState,
            bool supportCancellation)
            : base(userState, supportCancellation)
        {
            if (changeSet == null)
            {
                throw new ArgumentNullException(nameof(changeSet));
            }
            this._completeAction = completeAction;
            this._changeSet = changeSet;
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
        /// Successfully complete the submit operation.
        /// </summary>
        internal void Complete()
        {
            // SubmitOperation doesn't have a result - all results
            // are specified on Entities in the changeset.
            base.Complete((object)null);
        }

        /// <summary>
        /// Complete the submit operation with the specified error.
        /// </summary>
        /// <param name="error">The error.</param>
        internal new void SetError(Exception error)
        {
            base.SetError(error);
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
